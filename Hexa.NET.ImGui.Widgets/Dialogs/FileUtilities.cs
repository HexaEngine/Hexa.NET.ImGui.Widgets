namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using Utils = HexaGen.Runtime.Utils;

    public static unsafe partial class FileUtilities
    {
        public static long GetFileSize(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetFileMetadataWindows(filePath).Size;
            }
            else
            {
                return GetFileMetadataUnix(filePath).Size;
            }
        }

        public static FileMetadata GetFileMetadata(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetFileMetadataWindows(filePath);
            }
            else
            {
                return GetFileMetadataUnix(filePath);
            }
        }

        public static IEnumerable<FileMetadata> EnumerateEntries(string path, string pattern, SearchOption option)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return EnumerateEntriesWin(path, pattern, option);
            }
            else
            {
                return EnumerateEntriesUnix(path, pattern, option);
            }
        }

        public static IEnumerable<FileMetadata> EnumerateEntriesWin(string path, string pattern, SearchOption option)
        {
            UnsafeStack<StdWString> walkStack = new();

            {
                StdWString str = path;
                str.Append('\\');
                str.Append('*');
                walkStack.Push(str);
            }

            while (walkStack.TryPop(out var current))
            {
                nint findHandle;
                findHandle = StartSearch(current, out WIN32_FIND_DATA findData);
                if (findHandle == INVALID_HANDLE_VALUE)
                {
                    current.Release();
                    continue;
                }

                if (!findData.ShouldIgnore(pattern, out var ignore))
                {
                    FileMetadata meta = Convert(findData, current);

                    if ((meta.Attributes & FileAttributes.Directory) == 0 && option == SearchOption.AllDirectories)
                    {
                        var folder = meta.Path.Clone();
                        folder.Append('\\');
                        folder.Append('*');
                        walkStack.Push(folder);
                    }

                    if (!ignore)
                    {
                        yield return meta;
                        meta.Path.Release();
                    }
                }

                while (FindNextFileW(findHandle, out findData))
                {
                    if (!findData.ShouldIgnore(pattern, out ignore))
                    {
                        FileMetadata meta = Convert(findData, current);

                        if ((meta.Attributes & FileAttributes.Directory) != 0 && option == SearchOption.AllDirectories)
                        {
                            var folder = meta.Path.Clone();
                            folder.Append('\\');
                            folder.Append('*');
                            walkStack.Push(folder);
                        }

                        if (!ignore)
                        {
                            yield return meta;
                            meta.Path.Release();
                        }
                    }
                }

                FindClose(findHandle);
                current.Release();
            }

            walkStack.Release();
        }

        private static nint StartSearch(StdWString st, out WIN32_FIND_DATA data)
        {
            return FindFirstFileW(st.Data, out data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FileMetadata Convert(WIN32_FIND_DATA data, StdWString path)
        {
            Dump(&data);
            FileMetadata metadata = new();
            int length = StrLen(data.cFileName);
            StdWString str = new(length + path.Size + 1);
            for (int i = 0; i < path.Size - 1; i++)
            {
                str.Append(path[i]);
            }

            for (int i = 0; i < length; i++)
            {
                str.Append(data.cFileName[i]);
            }

            metadata.Path = str;
            metadata.CreationTime = DateTime.FromFileTime(data.ftCreationTime);
            metadata.LastAccessTime = DateTime.FromFileTime(data.ftLastAccessTime);
            metadata.LastWriteTime = DateTime.FromFileTime(data.ftLastWriteTime);
            metadata.Size = ((long)data.nFileSizeHigh << 32) + data.nFileSizeLow;
            metadata.Attributes = (FileAttributes)data.dwFileAttributes;
            return metadata;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int StrLen(char* str)
        {
            if (str == null)
            {
                return 0;
            }

            int num = 0;
            while (*str != 0)
            {
                str++;
                num++;
            }

            return num;
        }

        private const uint FILE_READ_ATTRIBUTES = 0x80;
        private const uint FILE_SHARE_READ = 0x1;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        private static FileMetadata GetFileMetadataWindows(string filePath)
        {
            nint fileHandle;
            fixed (char* str0 = filePath)
            {
                fileHandle = CreateFile(str0, FILE_READ_ATTRIBUTES, FILE_SHARE_READ, nint.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_BACKUP_SEMANTICS, nint.Zero);
            }

            if (fileHandle == nint.Zero || fileHandle == INVALID_HANDLE_VALUE)
            {
                return default;
            }

            try
            {
                if (GetFileInformationByHandle(fileHandle, out var lpFileInformation))
                {
                    FileMetadata metadata = new();
                    metadata.Path = filePath;
                    metadata.CreationTime = DateTime.FromFileTime(lpFileInformation.ftCreationTime);
                    metadata.LastAccessTime = DateTime.FromFileTime(lpFileInformation.ftLastAccessTime);
                    metadata.LastWriteTime = DateTime.FromFileTime(lpFileInformation.ftLastWriteTime);
                    metadata.Size = ((long)lpFileInformation.nFileSizeHigh << 32) + lpFileInformation.nFileSizeLow;
                    metadata.Attributes = (FileAttributes)lpFileInformation.dwFileAttributes;
                    return metadata;
                }

                return default;
            }
            finally
            {
                CloseHandle(fileHandle);
            }
        }

        private static FileMetadata GetFileMetadataUnix(string filePath)
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

            metadata.CreationTime = DateTimeOffset.FromUnixTimeSeconds(fileStat.StCtime).LocalDateTime.AddTicks((long)(fileStat.StCtimensec / 100));
            metadata.LastAccessTime = DateTimeOffset.FromUnixTimeSeconds(fileStat.StAtime).LocalDateTime.AddTicks((long)(fileStat.StAtimensec / 100));
            metadata.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(fileStat.StMtime).LocalDateTime.AddTicks((long)(fileStat.StMtimensec / 100));
            metadata.Size = fileStat.StSize;
            metadata.Attributes = ConvertStatModeToAttributes(fileStat.StMode, filePath);

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

        private const int S_IFDIR = 0x4000;   // Directory
        private const int S_IFREG = 0x8000;   // Regular file
        private const int S_IFLNK = 0xA000;   // Symbolic link (Unix)
        private const int S_IRUSR = 0x0100;   // Owner read permission
        private const int S_IWUSR = 0x0080;   // Owner write permission
        private const int S_IXUSR = 0x0040;   // Owner execute permission

        public static FileAttributes ConvertStatModeToAttributes(int st_mode, ReadOnlySpan<char> fileName)
        {
            FileAttributes attributes = FileAttributes.None;

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
            if ((st_mode & S_IWUSR) == 0)
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

        // Windows API P/Invoke declarations
        private static readonly nint INVALID_HANDLE_VALUE = new(-1);

        [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
        private static partial nint CreateFile(char* lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

        [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetFileInformationByHandle(nint hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [LibraryImport("kernel32.dll", EntryPoint = "GetFileAttributesW", SetLastError = true)]
        private static partial uint GetFileAttributes(char* lpFileName);

        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint dwVolumeSerialNumber;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint nNumberOfLinks;
            public uint nFileIndexHigh;
            public uint nFileIndexLow;
        }

        public struct FILETIME
        {
            public uint Low;
            public uint High;

            public readonly long Value => this;

            public static implicit operator long(FILETIME filetime)
            {
                return ((long)filetime.High << 32) + filetime.Low;
            }

            public static implicit operator DateTime(FILETIME filetime)
            {
                return DateTime.FromFileTime(filetime);
            }
        }

        private const int MAX_PATH = 260;
        private const int MAX_ALTERNATE = 14;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            public fixed char cFileName[MAX_PATH];
            public fixed char cAlternateFileName[MAX_ALTERNATE];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ShouldIgnore(string pattern, out bool result)
            {
                if (cFileName[0] == '.' && cFileName[1] == '\0' || cFileName[0] == '.' && cFileName[1] == '.' && cFileName[2] == '\0')
                {
                    return result = true;
                }

                fixed (char* p = cFileName)
                {
                    result = !FileSystemSearcher.IsMatch(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p), pattern, StringComparison.CurrentCulture);
                }

                if ((dwFileAttributes & (uint)FileAttributes.Directory) != 0)
                {
                    return false;
                }

                return result;
            }
        }

        [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(nint hObject);

        // FindFirstFileW declaration (native call)
        [LibraryImport("kernel32.dll", EntryPoint = "FindFirstFileW", SetLastError = true)]
        public static partial nint FindFirstFileW(char* lpFileName, out WIN32_FIND_DATA lpFindFileData);

        // FindNextFileW declaration (native call)
        [LibraryImport("kernel32.dll", EntryPoint = "FindNextFileW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FindNextFileW(nint hFindFile, out WIN32_FIND_DATA lpFindFileData);

        // FindClose declaration (native call)
        [LibraryImport("kernel32.dll", EntryPoint = "FindClose", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FindClose(nint hFindFile);

        // Unix-based stat method
        [LibraryImport("libc", EntryPoint = "stat", SetLastError = true)]
        private static unsafe partial int FileStat(byte* path, out Stat buf);

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
            public long StAtime;
            public ulong StAtimensec;
            public long StMtime;
            public ulong StMtimensec;
            public long StCtime;
            public ulong StCtimensec;
            public long GlibcReserved0;
            public long GlibcReserved1;
            public long GlibcReserved2;
        }

        public const int DT_DIR = 4;

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct DirEnt
        {
            public ulong d_ino;         // Inode number
            public long d_off;          // Offset to the next dirent
            public ushort d_reclen;     // Length of this record
            public byte d_type;         // Type of file
            public fixed byte d_name[256]; // Filename (null-terminated)

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ShouldIgnore(string pattern, out bool result)
            {
                if (d_name[0] == '.' && d_name[1] == '\0' || d_name[0] == '.' && d_name[1] == '.' && d_name[2] == '\0')
                {
                    return result = true;
                }

                fixed (byte* p = d_name)
                {
                    result = !FileSystemSearcher.IsMatch(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p), pattern, StringComparison.CurrentCulture);
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
        [DllImport("libc", EntryPoint = "opendir", SetLastError = true)]
        private static extern unsafe nint OpenDir(byte* name);

        // P/Invoke for readdir
        [DllImport("libc", EntryPoint = "readdir", SetLastError = true)]
        private static extern unsafe DirEnt* ReadDir(nint dir);

        // P/Invoke for closedir
        [DllImport("libc", EntryPoint = "closedir", SetLastError = true)]
        private static extern unsafe int CloseDir(nint dir);

        public static IEnumerable<FileMetadata> EnumerateEntriesUnix(string path, string pattern, SearchOption option)
        {
            UnsafeStack<StdString> walkStack = new();
            walkStack.Push(path);

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
            Dump(&entry);
            int length = NET.Utilities.Utils.StrLen(entry.d_name);
            StdWString str = new(path.Size + length);
            str.Append(path);
            str.Append('/');
            str.Append(entry.d_name);
            FileMetadata meta = new();
            meta.Path = str;

            FileStat(str, out var stat);
            meta.CreationTime = DateTimeOffset.FromUnixTimeSeconds(stat.StCtime).LocalDateTime.AddTicks((long)(stat.StCtimensec / 100));
            meta.LastAccessTime = DateTimeOffset.FromUnixTimeSeconds(stat.StAtime).LocalDateTime.AddTicks((long)(stat.StAtimensec / 100));
            meta.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(stat.StMtime).LocalDateTime.AddTicks((long)(stat.StMtimensec / 100));
            meta.Size = stat.StSize;
            meta.Attributes = ConvertStatModeToAttributes(stat.StMode, str);
            return meta;
        }

        [RequiresDynamicCode("")]
        public static void Dump<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(T* ptr) where T : unmanaged
        {
            byte* p = (byte*)ptr;
            int sizeInBytes = sizeof(T);
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            (FieldInfo info, int offset, int size)[] offsetData = new (FieldInfo info, int offset, int size)[fields.Length];

            int currentOffset = 0;
            for (int j = 0; j < fields.Length; j++)
            {
                FieldInfo field = fields[j];
                int size = Marshal.SizeOf(field.FieldType);
                offsetData[j] = (field, currentOffset, size);
                Console.WriteLine($"Field: {field.Name}, Offset: {currentOffset:X8}, Size: {size}, Type: {field.FieldType}");
                currentOffset += size;

            }

            int startInfo = 0;
            for (int i = 0; i < sizeInBytes; i++)
            {
                (FieldInfo info, int offset, int size)? info = default;
                for (int k = startInfo; k < offsetData.Length; k++)
                {
                    var x = offsetData[k];
                    if (x.offset == i)
                    {
                        startInfo = k;
                        info = x;
                        break;
                    }
                    if (x.offset > i)
                    {
                        break;
                    }
                }
                if (info.HasValue)
                {
                    Console.WriteLine($"{((nint)p + i):X8}: {p[i]:X} : {info.Value.info.Name} {info.Value.size}");
                }
                else
                {
                    Console.WriteLine($"{((nint)p + i):X8}: {p[i]:X}");
                }
               
            }
        }

        private static void FileStat(StdWString str, out Stat stat)
        {
            int strSize0 = Encoding.UTF8.GetByteCount(str.Data, str.Size);
            byte* pStr0;
            if (strSize0 >= Utils.MaxStackallocSize)
            {
                pStr0 = Utils.Alloc<byte>(strSize0);
            }
            else
            {
                byte* pStrStack0 = stackalloc byte[strSize0];
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