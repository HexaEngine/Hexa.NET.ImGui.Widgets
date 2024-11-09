namespace Hexa.NET.ImGui.Widgets.IO
{
    using Hexa.NET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using Utils = HexaGen.Runtime.Utils;

    public static unsafe partial class FileUtils
    {
        public static unsafe partial class OSX
        {
            private const string LibName = "libSystem.B.dylib";

            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "stat")]
            private static extern unsafe int FileStat(byte* path, out Stat buf);

            [StructLayout(LayoutKind.Sequential)]
            private struct Stat
            {
                public int st_dev;                 /* [XSI] ID of device containing file */
                public ushort st_mode;                /* [XSI] Mode of file (see below) */
                public ushort st_nlink;               /* [XSI] Number of hard links */
                public ulong st_ino;                /* [XSI] File serial number */
                public uint st_uid;                 /* [XSI] User ID of the file */
                public uint st_gid;                 /* [XSI] Group ID of the file */
                public int st_rdev;                /* [XSI] Device ID */
                public Timespec st_atimespec;           /* time of last access */
                public Timespec st_mtimespec;           /* time of last data modification */
                public Timespec st_ctimespec;           /* time of last status change */
                public Timespec st_birthtimespec;       /* time of file creation(birth) */
                public long st_size;                /* [XSI] file size, in bytes */
                public long st_blocks;              /* [XSI] blocks allocated for file */
                public int st_blksize;             /* [XSI] optimal blocksize for I/O */
                public uint st_flags;               /* user defined flags for file */
                public uint st_gen;                 /* file generation number */
                public int st_lspare;              /* RESERVED: DO NOT USE! */
                public fixed long st_qspare[2];           /* RESERVED: DO NOT USE! */
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

                metadata.CreationTime = fileStat.st_ctimespec;
                metadata.LastAccessTime = fileStat.st_atimespec;
                metadata.LastWriteTime = fileStat.st_mtimespec;
                metadata.Size = fileStat.st_size;
                metadata.Attributes = ConvertStatModeToAttributes(fileStat.st_mode, filePath);

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

            public const int S_IFMT = 0xF000;      // type of file
            public const int S_IFIFO = 0x1000;     // named pipe (fifo)
            public const int S_IFCHR = 0x2000;     // character special
            public const int S_IFDIR = 0x4000;     // directory
            public const int S_IFBLK = 0x6000;     // block special
            public const int S_IFREG = 0x8000;     // regular file
            public const int S_IFLNK = 0xA000;     // symbolic link
            public const int S_IFSOCK = 0xC000;    // socket
            public const int S_IFWHT = 0xE000;     // whiteout

            public const int S_ISUID = 0x0800;     // set user ID on execution
            public const int S_ISGID = 0x0400;     // set group ID on execution
            public const int S_ISVTX = 0x0200;     // save swapped text even after use

            public const int S_IRUSR = 0x0100;     // read permission, owner
            public const int S_IWUSR = 0x0080;     // write permission, owner
            public const int S_IXUSR = 0x0040;     // execute/search permission, owner

            public static bool IsBlockSpecial(int mode) => (mode & S_IFMT) == S_IFBLK;

            public static bool IsCharSpecial(int mode) => (mode & S_IFMT) == S_IFCHR;

            public static bool IsDirectory(int mode) => (mode & S_IFMT) == S_IFDIR;

            public static bool IsFifo(int mode) => (mode & S_IFMT) == S_IFIFO;

            public static bool IsRegularFile(int mode) => (mode & S_IFMT) == S_IFREG;

            public static bool IsSymbolicLink(int mode) => (mode & S_IFMT) == S_IFLNK;

            public static bool IsSocket(int mode) => (mode & S_IFMT) == S_IFSOCK;

            public static bool IsWhiteout(int mode) => (mode & S_IFMT) == S_IFWHT;

            // Helper methods to check permissions
            public static bool IsSetUserID(int mode) => (mode & S_ISUID) != 0;

            public static bool IsSetGroupID(int mode) => (mode & S_ISGID) != 0;

            public static bool IsStickyBitSet(int mode) => (mode & S_ISVTX) != 0;

            public static bool CanRead(int mode) => (mode & S_IRUSR) != 0;

            public static bool CanWrite(int mode) => (mode & S_IWUSR) != 0;

            public static bool CanExecute(int mode) => (mode & S_IXUSR) != 0;

            public static FileAttributes ConvertStatModeToAttributes(int st_mode, ReadOnlySpan<char> fileName)
            {
                FileAttributes attributes = FileAttributes.None;

                // File type determination
                if (IsDirectory(st_mode))
                {
                    attributes |= FileAttributes.Directory;
                }
                else if (IsRegularFile(st_mode))
                {
                    attributes |= FileAttributes.Normal; // Used for symbolic links
                }
                else if (IsSymbolicLink(st_mode))
                {
                    attributes |= FileAttributes.ReparsePoint; // Used for symbolic links
                }

                // Permission handling - If no write permission for the owner, mark as ReadOnly
                if ((st_mode & S_IRUSR) == 0)
                {
                    attributes |= FileAttributes.ReadOnly; // If the owner has no read permission, mark it as read-only
                }

                // Hidden file detection (Unix files that start with '.' are treated as hidden)
                if (fileName.Length > 0 && fileName[0] == '.')
                {
                    attributes |= FileAttributes.Hidden;
                }

                return attributes;
            }

            public const int DT_UNKNOWN = 0;
            public const int DT_FIFO = 1;
            public const int DT_CHR = 2;
            public const int DT_DIR = 4;
            public const int DT_BLK = 6;
            public const int DT_REG = 8;
            public const int DT_LNK = 10;
            public const int DT_SOCK = 12;
            public const int DT_WHT = 14;

            public const int DARWIN_MAXPATHLEN = 1024;

            [StructLayout(LayoutKind.Sequential)]
            private unsafe struct Dirent
            {
                public ulong d_ino;            // __uint64_t (64-bit file number of entry)
                public ulong d_seekoff;        // __uint64_t (64-bit seek offset, optional)
                public ushort d_reclen;        // __uint16_t (length of this record)
                public ushort d_namlen;        // __uint16_t (length of string in d_name)
                public byte d_type;            // __uint8_t (file type)
                public fixed byte d_name[DARWIN_MAXPATHLEN]; // Filename (null-terminated)

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool ShouldIgnore(string pattern, out bool result)
                {
                    if (d_name[0] == '.' && d_name[1] == '\0' || d_name[0] == '.' && d_name[1] == '.' && d_name[2] == '\0')
                    {
                        return result = true;
                    }

                    fixed (byte* p = d_name)
                    {
                        result = !PatternMatcher.IsMatch(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p), pattern, StringComparison.CurrentCulture);
                    }

                    if (d_type == DT_DIR)
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
            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "opendir")]
            private static extern unsafe nint OpenDir(byte* name);

            // P/Invoke for readdir
            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "readdir")]
            private static extern unsafe Dirent* ReadDir(nint dir);

            // P/Invoke for closedir
            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "closedir")]
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

            private static bool TryReadDir(nint dirHandle, out Dirent dirEnt)
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

            private static FileMetadata Convert(Dirent entry, StdString path)
            {
                int length = entry.d_namlen;
                StdWString str;
                if (path.Data[path.Size - 1] != '/')
                {
                    str = new(path.Size + 1 + length);
                    str.Append(path);
                    str.Append('/');
                    str.Append(entry.d_name, length);
                }
                else
                {
                    str = new(path.Size + length);
                    str.Append(path);
                    str.Append(entry.d_name, length);
                }

                *(str.Data + str.Size) = '\0';

                FileMetadata meta = default;

                FileStat(str, out var stat);

                meta.Path = str;
                meta.CreationTime = stat.st_ctimespec;
                meta.LastAccessTime = stat.st_atimespec;
                meta.LastWriteTime = stat.st_mtimespec;
                meta.Size = stat.st_size;
                meta.Attributes = ConvertStatModeToAttributes(stat.st_mode, str);

                return meta;
            }

            private static int FileStat(StdWString str, out Stat stat)
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
                int ret = FileStat(pStr0, out stat);
                if (strSize0 >= Utils.MaxStackallocSize)
                {
                    Utils.Free(pStr0);
                }
                return ret;
            }
        }
    }
}