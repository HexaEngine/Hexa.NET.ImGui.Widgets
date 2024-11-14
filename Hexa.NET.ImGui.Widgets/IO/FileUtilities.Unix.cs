namespace Hexa.NET.ImGui.Widgets.IO
{
    using Hexa.NET.ImGui.Widgets.Extensions;
    using Hexa.NET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using Utils = HexaGen.Runtime.Utils;

    public static unsafe partial class FileUtils
    {
        public static unsafe partial class Unix
        {
            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "stat", SetLastError = true)]
            private static extern unsafe int FileStat(byte* path, out Stat buf);

            [StructLayout(LayoutKind.Sequential)]
            private struct Stat
            {
                public ulong StDev;
                public ulong StIno;
                public ulong StNlink;
                public int StMode;
                public uint StUid;
                public uint StGid;
                public int Pad0;
                public ulong StRdev;
                public long StSize;
                public long StBlksize;
                public long StBlocks;
                public Timespec StAtim; /* Time of last access.  */
                public Timespec StMtim; /* Time of last modification.  */
                public Timespec StCtim; /* Time of last status change.  */
                public long GlibcReserved0;
                public long GlibcReserved1;
                public long GlibcReserved2;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct Timespec
            {
                public long tv_sec;             // time_t: Seconds
                public long tv_nsec;            // long: Nanoseconds

                public static implicit operator DateTime(Timespec timespec)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(timespec.tv_sec).LocalDateTime.AddTicks(timespec.tv_nsec / 100);
                }
            }

            public static FileMetadata GetFileMetadata(string filePath)
            {
                byte* str0;
                int strSize0 = Utils.GetByteCountUTF8(filePath);
                if (strSize0 >= Utils.MaxStackallocSize)
                {
                    str0 = Utils.Alloc<byte>(strSize0 + 1);
                }
                else
                {
                    byte* strStack0 = stackalloc byte[strSize0 + 1];
                    str0 = strStack0;
                }
                Utils.EncodeStringUTF8(filePath, str0, strSize0);
                str0[strSize0] = 0;

                var result = FileStat(str0, out Stat fileStat);
                FileMetadata metadata = new();
                metadata.Path = filePath;

                metadata.CreationTime = fileStat.StCtim;
                metadata.LastAccessTime = fileStat.StAtim;
                metadata.LastWriteTime = fileStat.StMtim;
                metadata.Size = fileStat.StSize;
                metadata.Attributes = ConvertStatModeToAttributes(fileStat.StMode, filePath.AsSpan());

                if (strSize0 >= Utils.MaxStackallocSize)
                {
                    Utils.Free(str0);
                }

                if (result == 0)
                {
                    return metadata;
                }
                else
                {
                    return default;
                }
            }

            public const int S_IFMT = 0xF000; /* These bits determine file type.  */

            public const int S_IFDIR = 0x4000;   // Directory
            public const int S_IFCHR = 0x2000;   // Character device
            public const int S_IFBLK = 0x6000;   // Block device
            public const int S_IFREG = 0x8000;   // Regular file
            public const int S_IFIFO = 0x1000;   // FIFO
            public const int S_IFLNK = 0xA000;   // Symbolic link
            public const int S_IFSOCK = 0xC000;  // Socket

            public const int S_ISUID = 0x0800;   // Set user ID on execution
            public const int S_ISGID = 0x0400;   // Set group ID on execution
            public const int S_ISVTX = 0x0200;   // Sticky bit
            public const int S_IREAD = 0x0100;   // Read by owner
            public const int S_IWRITE = 0x0080;  // Write by owner
            public const int S_IEXEC = 0x0040;   // Execute by owner

            public static FileAttributes ConvertStatModeToAttributes(int st_mode, ReadOnlySpan<char> fileName)
            {
                FileAttributes attributes = 0;

                // File type determination
                if ((st_mode & S_IFDIR) == S_IFDIR)
                {
                    attributes |= FileAttributes.Directory;
                }
                else if ((st_mode & S_IFREG) == S_IFREG)
                {
                    attributes |= FileAttributes.Normal;
                }
                else if ((st_mode & S_IFLNK) == S_IFLNK)
                {
                    attributes |= FileAttributes.ReparsePoint;  // Symbolic links in Unix can be mapped to ReparsePoint in Windows
                }

                // Permission handling - If no write permission for the owner, mark as ReadOnly
                if ((st_mode & S_IWRITE) == 0)
                {
                    attributes |= FileAttributes.ReadOnly;
                }

                // Hidden file detection (Unix files that start with '.' are treated as hidden)
                if (fileName.Length > 0 && fileName[0] == '.')
                {
                    attributes |= FileAttributes.Hidden;
                }

                // Add other attributes as necessary, but keep in mind Unix-like systems may not have equivalents for:
                // - FileAttributes.Compressed
                // - FileAttributes.Encrypted
                // - FileAttributes.Offline
                // - FileAttributes.NotContentIndexed

                return attributes;
            }

            private enum DType : byte
            {
                DT_UNKNOWN = 0,
                DT_FIFO = 1,
                DT_CHR = 2,
                DT_DIR = 4,
                DT_BLK = 6,
                DT_REG = 8,
                DT_LNK = 10,
                DT_SOCK = 12,
                DT_WHT = 14
            };

            public const int MAX_PATH = 256;

            [StructLayout(LayoutKind.Sequential)]
            private unsafe struct DirEnt
            {
                public ulong d_ino;         // Inode number
                public long d_off;          // Offset to the next dirent
                public ushort d_reclen;     // Length of this record
                public DType d_type;         // Type of file
                public fixed byte d_name[MAX_PATH]; // Filename (null-terminated)

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool ShouldIgnore(string pattern, out bool result)
                {
                    if (d_name[0] == '.' && d_name[1] == '\0' || d_name[0] == '.' && d_name[1] == '.' && d_name[2] == '\0')
                    {
                        return result = true;
                    }

                    fixed (byte* p = d_name)
                    {
                        result = !PatternMatcher.IsMatch(SpanHelper.CreateReadOnlySpanFromNullTerminated(p), pattern, StringComparison.CurrentCulture);
                    }

                    if (d_type == DType.DT_DIR)
                    {
                        return false;
                    }

                    return result;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool ShouldIgnore()
                {
                    return d_name[0] == '.' && d_name[1] == '\0' || d_name[0] == '.' && d_name[1] == '.' && d_name[2] == '\0';
                }
            }

            // P/Invoke for opendir
            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "opendir", SetLastError = true)]
            private static extern unsafe nint OpenDir(byte* name);

            // P/Invoke for readdir
            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "readdir", SetLastError = true)]
            private static extern unsafe DirEnt* ReadDir(nint dir);

            // P/Invoke for closedir
            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "closedir", SetLastError = true)]
            private static extern unsafe int CloseDir(nint dir);

            public static IEnumerable<FileMetadata> EnumerateEntries(string path, string pattern, SearchOption option)
            {
                StdString str = path;
                CorrectPath(str);
                UnsafeStack<StdString> walkStack = new();
                walkStack.Push(str);

                while (walkStack.TryPop(out var dir))
                {
                    var dirHandle = OpenDir(dir);

                    if (dirHandle == 0)
                    {
                        dir.Release();
                        continue;
                    }

                    while (TryReadDir(dirHandle, out var dirEnt))
                    {
                        if (!dirEnt.ShouldIgnore(pattern, out var ignore))
                        {
                            var meta = Convert(dirEnt, dir);
                            if ((meta.Attributes & FileAttributes.Directory) != 0 && option == SearchOption.AllDirectories)
                            {
                                walkStack.Push(meta.Path.ToUTF8String());
                            }

                            if (!ignore)
                            {
                                yield return meta;
                                meta.Path.Release();
                            }
                        }
                    }

                    CloseDir(dirHandle);
                    dir.Release();
                }

                walkStack.Release();
            }

            private static nint OpenDir(StdString str)
            {
                return OpenDir(str.Data);
            }

            private static bool TryReadDir(nint dirHandle, out DirEnt dirEnt)
            {
                var entry = ReadDir(dirHandle);
                if (entry == null)
                {
                    dirEnt = default;
                    return false;
                }
                dirEnt = *entry;
                return true;
            }

            private static FileMetadata Convert(DirEnt entry, StdString path)
            {
                int length = NET.Utilities.Utils.StrLen(entry.d_name);
                StdWString str;
                if (path.Data[path.Size - 1] != '/')
                {
                    str = new(path.Size + 1 + length);
                    str.Append(path);
                    str.Append('/');
                    str.Append(entry.d_name);
                }
                else
                {
                    str = new(path.Size + length);
                    str.Append(path);
                    str.Append(entry.d_name);
                }
                *(str.Data + str.Size) = '\0';

                FileMetadata meta = default;
                FileStat(str, out var stat);
                meta.Path = str;
                meta.CreationTime = stat.StCtim;
                meta.LastAccessTime = stat.StAtim;
                meta.LastWriteTime = stat.StMtim;
                meta.Size = stat.StSize;
                meta.Attributes = ConvertStatModeToAttributes(stat.StMode, str);
                return meta;
            }

            private static void FileStat(StdWString str, out Stat stat)
            {
                int strSize0 = Encoding.UTF8.GetByteCount(str.Data, str.Size);
                byte* pStr0;
                if (strSize0 >= Utils.MaxStackallocSize)
                {
                    pStr0 = Utils.Alloc<byte>(strSize0 + 1);
                }
                else
                {
                    byte* pStrStack0 = stackalloc byte[strSize0 + 1];
                    pStr0 = pStrStack0;
                }
                Encoding.UTF8.GetBytes(str.Data, str.Size, pStr0, strSize0);
                pStr0[strSize0] = 0;

                FileStat(pStr0, out stat);

                if (strSize0 >= Utils.MaxStackallocSize)
                {
                    Utils.Free(pStr0);
                }
            }
        }
    }
}