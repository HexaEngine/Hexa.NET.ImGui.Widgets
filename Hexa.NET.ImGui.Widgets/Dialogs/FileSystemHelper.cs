namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui.Widgets.Extensions;
    using Hexa.NET.Utilities;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.AccessControl;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Utils = HexaGen.Runtime.Utils;

    public struct FileMetadata
    {
        public StdWString Path;
        public long Size;
        public DateTime CreationTime;
        public DateTime LastAccessTime;
        public DateTime LastWriteTime;
        public FileAttributes Attributes;
    }

    public unsafe struct FileNameString
    {
        public fixed char Data[260];
        public int Length;

        public FileNameString(char* data, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Data[i] = data[i];
            }

            Length = length;
        }

        public readonly Span<char> AsSpan()
        {
            fixed (char* data = Data)
                return new Span<char>(data, Length);
        }

        public readonly ReadOnlySpan<char> AsReadOnlySpan()
        {
            fixed (char* data = Data)
                return new ReadOnlySpan<char>(data, Length);
        }

        public static implicit operator Span<char>(FileNameString str)
        {
            return str.AsSpan();
        }

        public static implicit operator ReadOnlySpan<char>(FileNameString str)
        {
            return str.AsReadOnlySpan();
        }
    }

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

        public static IEnumerable<FileMetadata> EnumerateEntries(string path, SearchOption option)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return EnumerateEntriesWin(path, option);
            }
            else
            {
                return EnumerateEntriesUnix(path, option);
            }
        }

        public static IEnumerable<FileMetadata> EnumerateEntriesWin(string path, SearchOption option)
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

                if (!findData.ShouldIgnore())
                {
                    FileMetadata meta = Convert(findData, current);

                    if ((meta.Attributes & FileAttributes.Directory) == 0 && option == SearchOption.AllDirectories)
                    {
                        var folder = meta.Path.Clone();
                        folder.Append('\\');
                        folder.Append('*');
                        walkStack.Push(folder);
                    }

                    yield return meta;
                    meta.Path.Release();
                }

                while (FindNextFileW(findHandle, out findData))
                {
                    if (!findData.ShouldIgnore())
                    {
                        FileMetadata meta = Convert(findData, current);

                        if ((meta.Attributes & FileAttributes.Directory) != 0 && option == SearchOption.AllDirectories)
                        {
                            var folder = meta.Path.Clone();
                            folder.Append('\\');
                            folder.Append('*');
                            walkStack.Push(folder);
                        }

                        yield return meta;
                        meta.Path.Release();
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
            public bool ShouldIgnore()
            {
                return cFileName[0] == '.' && cFileName[1] == '\0' || cFileName[0] == '.' && cFileName[1] == '.' && cFileName[2] == '\0';
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

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct DirEnt
        {
            public ulong d_ino;         // Inode number
            public long d_off;          // Offset to the next dirent
            public ushort d_reclen;     // Length of this record
            public byte d_type;         // Type of file
            public fixed byte d_name[256]; // Filename (null-terminated)

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

        public static IEnumerable<FileMetadata> EnumerateEntriesUnix(string path, SearchOption option)
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
                    if (!dirEnt.ShouldIgnore())
                    {
                        var meta = Convert(dirEnt, dir);
                        if ((meta.Attributes & FileAttributes.Directory) != 0 && option == SearchOption.AllDirectories)
                        {
                            walkStack.Push(meta.Path.ToUTF8String());
                        }

                        yield return meta;
                        meta.Path.Release();
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
            StdWString str = new(path.Size + length);
            str.Append(path);
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

    public class FileSystemSearcher
    {
        public static bool IsMatch(ReadOnlySpan<char> fileName, string pattern, StringComparison comparison)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return true;

            // If the pattern is "*", it matches everything.
            if (pattern == "*")
                return true;

            if (!pattern.Contains('.'))
            {
                return fileName.Contains(pattern, comparison);
            }

            int f = 0, p = 0;
            int starIndex = -1, match = 0;

            while (f < fileName.Length)
            {
                // If the current characters match, or the pattern has a '?'
                if (p < pattern.Length && (pattern[p] == '?' || fileName[f] == pattern[p]))
                {
                    f++;
                    p++;
                }
                // If we encounter a '*', mark the star position and try matching the rest
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    starIndex = p;
                    match = f;
                    p++;
                }
                // If mismatch, but there was a previous '*', retry from the last star
                else if (starIndex != -1)
                {
                    p = starIndex + 1;
                    match++;
                    f = match;
                }
                else
                {
                    return false;
                }
            }

            // Handle remaining '*' in the pattern
            while (p < pattern.Length && pattern[p] == '*')
                p++;

            return p == pattern.Length;
        }

        public static bool IsPatternEmpty(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return true;

            if (pattern == "*")
                return true;

            return false;
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public partial class SourceGenerationContextDictionary : JsonSerializerContext
    {
    }

    [Flags]
    public enum CommonFilePermissions
    {
        None = 0,
        OwnerRead = 1 << 0,
        OwnerWrite = 1 << 1,
        OwnerExecute = 1 << 2,
        GroupRead = 1 << 3,
        GroupWrite = 1 << 4,
        GroupExecute = 1 << 5,
        OtherRead = 1 << 6,
        OtherWrite = 1 << 7,
        OtherExecute = 1 << 8,
        OwnerFullControl = OwnerRead | OwnerWrite | OwnerExecute,
        GroupFullControl = GroupRead | GroupWrite | GroupExecute,
        OtherFullControl = OtherRead | OtherWrite | OtherExecute
    }

    public struct FileSystemItem : IEquatable<FileSystemItem>, IFileSystemItem
    {
        private string path;
        private string icon;
        private string name;
        private FileSystemItemFlags flags;
        private DateTime dateModified;
        private string type;
        private long size;
        private CommonFilePermissions permissions;

        public FileSystemItem(string path, string icon, string name, string type, FileSystemItemFlags flags)
        {
            this.path = path;
            this.icon = icon;
            this.name = name;
            this.flags = flags;
            this.type = type;

            this.dateModified = File.GetLastWriteTime(path);

            if (IsFile)
            {
                size = new FileInfo(path).Length;
            }
            /*
            if (!OperatingSystem.IsWindows())
            {
                var access = File.GetUnixFileMode(path);
                if ((access & UnixFileMode.UserExecute) != 0 || (access & UnixFileMode.GroupExecute) != 0 || (access & UnixFileMode.OtherExecute) != 0)
                {
                }
            }
            else
            {
                FileSecurity security = new(path, AccessControlSections.All);
            }*/
        }

        private static CommonFilePermissions ConvertUnixPermissions(UnixFileMode permissions)
        {
            CommonFilePermissions result = CommonFilePermissions.None;

            if ((permissions & UnixFileMode.UserRead) != 0)
                result |= CommonFilePermissions.OwnerRead;
            if ((permissions & UnixFileMode.UserWrite) != 0)
                result |= CommonFilePermissions.OwnerWrite;
            if ((permissions & UnixFileMode.UserExecute) != 0)
                result |= CommonFilePermissions.OwnerExecute;
            if ((permissions & UnixFileMode.GroupRead) != 0)
                result |= CommonFilePermissions.GroupRead;
            if ((permissions & UnixFileMode.GroupWrite) != 0)
                result |= CommonFilePermissions.GroupWrite;
            if ((permissions & UnixFileMode.GroupExecute) != 0)
                result |= CommonFilePermissions.GroupExecute;
            if ((permissions & UnixFileMode.OtherRead) != 0)
                result |= CommonFilePermissions.OtherRead;
            if ((permissions & UnixFileMode.OtherWrite) != 0)
                result |= CommonFilePermissions.OtherWrite;
            if ((permissions & UnixFileMode.OtherExecute) != 0)
                result |= CommonFilePermissions.OtherExecute;

            return result;
        }

        [SupportedOSPlatform("windows")]
        private static CommonFilePermissions ConvertWindowsPermissions(FileSecurity security)
        {
            CommonFilePermissions result = CommonFilePermissions.None;

            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            string currentUser = Environment.UserName;
            string usersGroup = "Users";

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    string identity = rule.IdentityReference.Value;

                    if (identity.Equals(currentUser, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Owner permissions
                        if ((rule.FileSystemRights & FileSystemRights.ReadData) != 0)
                            result |= CommonFilePermissions.OwnerRead;
                        if ((rule.FileSystemRights & FileSystemRights.WriteData) != 0)
                            result |= CommonFilePermissions.OwnerWrite;
                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) != 0)
                            result |= CommonFilePermissions.OwnerExecute;
                    }
                    else if (identity.Equals(usersGroup, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Other permissions
                        if ((rule.FileSystemRights & FileSystemRights.ReadData) != 0)
                            result |= CommonFilePermissions.OtherRead;
                        if ((rule.FileSystemRights & FileSystemRights.WriteData) != 0)
                            result |= CommonFilePermissions.OtherWrite;
                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) != 0)
                            result |= CommonFilePermissions.OtherExecute;
                    }
                    else
                    {
                        // Group permissions (simplified for example purposes)
                        if ((rule.FileSystemRights & FileSystemRights.ReadData) != 0)
                            result |= CommonFilePermissions.GroupRead;
                        if ((rule.FileSystemRights & FileSystemRights.WriteData) != 0)
                            result |= CommonFilePermissions.GroupWrite;
                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) != 0)
                            result |= CommonFilePermissions.GroupExecute;
                    }
                }
            }

            return result;
        }

        public FileSystemItem(string path, string icon, string name, FileSystemItemFlags flags)
        {
            this.path = path;
            this.icon = icon;
            this.name = name;
            this.flags = flags;
            this.dateModified = File.GetLastWriteTime(path);

            if (IsFile)
            {
                this.size = FileUtilities.GetFileSize(path);
                this.type = DetermineFileType(System.IO.Path.GetExtension(path.AsSpan()));
            }
            else
            {
                this.type = "File Folder";
            }
        }

        public FileSystemItem(FileMetadata metadata, string icon, string name, FileSystemItemFlags flags)
        {
            this.path = metadata.Path.ToString();
            this.icon = icon;
            this.name = name;
            this.flags = flags;
            this.dateModified = metadata.LastWriteTime;

            if (IsFile)
            {
                this.size = metadata.Size;
                this.type = DetermineFileType(System.IO.Path.GetExtension(path.AsSpan()));
            }
            else
            {
                this.type = "File Folder";
            }
        }

        public FileSystemItem(string path, string icon, FileSystemItemFlags flags)
        {
            this.path = path;
            this.icon = icon;
            this.name = System.IO.Path.GetFileName(path);
            this.flags = flags;

            var mode = File.GetAttributes(path);

            dateModified = File.GetLastWriteTime(path);
            if (IsFile)
            {
                size = FileUtilities.GetFileSize(path);
                type = DetermineFileType(System.IO.Path.GetExtension(path.AsSpan()));
            }
            else
            {
                type = "File Folder";
            }
        }

        public readonly bool IsFile => (flags & FileSystemItemFlags.Folder) == 0;

        public readonly bool IsFolder => (flags & FileSystemItemFlags.Folder) != 0;

        public readonly bool IsHidden => (flags & FileSystemItemFlags.Hidden) != 0;

        public string Path { readonly get => path; set => path = value; }

        public string Icon { readonly get => icon; set => icon = value; }

        public string Name { readonly get => name; set => name = value; }

        public FileSystemItemFlags Flags { readonly get => flags; set => flags = value; }

        public DateTime DateModified { readonly get => dateModified; set => dateModified = value; }

        public string Type { readonly get => type; set => type = value; }

        public long Size { readonly get => size; set => size = value; }

        public CommonFilePermissions Permissions { readonly get => permissions; set => permissions = value; }

        static FileSystemItem()
        {
            LoadFileTypes();
        }

        public static string DetermineFileType(ReadOnlySpan<char> extension)
        {
            if (extension.IsEmpty)
            {
                return "File";
            }

            ulong hash = GetSpanHash(extension[1..]);

            if (fileTypes.TryGetValue(hash, out var type))
            {
                return type;
            }

            type = $"{extension} File"; // generic type name
            fileTypes.TryAdd(hash, type);

            return type;
        }

        private static void LoadFileTypes()
        {
            Stream? stream = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName = "Hexa.NET.ImGui.Widgets.assets.fileTypes.json";

                stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return;
                }

                var fileTypeDict = JsonSerializer.Deserialize(stream, SourceGenerationContextDictionary.Default.DictionaryStringString);

                if (fileTypeDict != null)
                {
                    foreach (var kvp in fileTypeDict)
                    {
                        fileTypes[GetSpanHash(kvp.Key.AsSpan()[1..])] = kvp.Value;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                stream?.Close();
            }
        }

        private static readonly ConcurrentDictionary<ulong, string> fileTypes = [];

        public static void RegisterFileType(string extension, string type)
        {
            fileTypes[GetSpanHash(extension.AsSpan())] = type; // overwrite existing type
        }

        public static ulong GetSpanHash(ReadOnlySpan<char> span, bool caseSensitive = false)
        {
            ulong hash = 7;
            foreach (var c in span)
            {
                var cc = c;
                if (!caseSensitive)
                {
                    cc = char.ToLower(c);
                }
                hash = hash * 31 + cc;
            }
            return hash;
        }

        public static int CompareByBase(FileSystemItem a, FileSystemItem b)
        {
            if (a.IsFolder && !b.IsFolder)
            {
                return -1;
            }
            if (!a.IsFolder && b.IsFolder)
            {
                return 1;
            }
            return 0;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is FileSystemItem item && Equals(item);
        }

        public readonly bool Equals(FileSystemItem other)
        {
            return Path == other.Path;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Path);
        }

        public static bool operator ==(FileSystemItem left, FileSystemItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FileSystemItem left, FileSystemItem right)
        {
            return !(left == right);
        }
    }

    public enum FileSystemItemFlags : byte
    {
        None = 0,
        Folder = 1,
        Hidden = 2,
    }

    public enum RefreshFlags
    {
        None = 0,
        Folders = 1,
        Files = 2,
        OnlyAllowFilteredExtensions = 4,
        Hidden = 8,
        SystemFiles = 16,
    }

    public enum SearchOptionsFlags
    {
        None = 0,
        Subfolders = 1,
        Hidden = 2,
        SystemFiles = 4,
        FilterDate = 8,
        FilterSize = 16,
    }

    public enum SearchFilterDate
    {
        None,
        Today,
        Yesterday,
        Week,
        Month,
        LastMonth,
        Year,
        LastYear
    }

    public enum SearchFilterSize
    {
        None,
        Empty,
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Gigantic
    }

    public struct SearchOptions
    {
        public bool Enabled;
        public string Pattern = string.Empty;
        public SearchOptionsFlags Flags;
        public SearchFilterDate DateModified;
        public SearchFilterSize FileSize;

        public SearchOptions()
        {
        }

        public readonly bool Filter(in FileMetadata metadata)
        {
            if ((metadata.Attributes & FileAttributes.Directory) != 0)
            {
                return true;
            }

            if ((Flags & SearchOptionsFlags.FilterDate) != 0)
            {
                DateTime now = DateTime.Now;
                DateTime startDate = DateTime.MinValue;

                switch (DateModified)
                {
                    case SearchFilterDate.Today:
                        startDate = now.Date; // Start of today
                        break;

                    case SearchFilterDate.Yesterday:
                        startDate = now.Date.AddDays(-1);
                        break;

                    case SearchFilterDate.Week:
                        startDate = now.AddDays(-7);
                        break;

                    case SearchFilterDate.Month:
                        startDate = now.AddMonths(-1);
                        break;

                    case SearchFilterDate.LastMonth:
                        startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                        break;

                    case SearchFilterDate.Year:
                        startDate = now.AddYears(-1);
                        break;

                    case SearchFilterDate.LastYear:
                        startDate = new DateTime(now.Year - 1, 1, 1);
                        break;
                }

                // If the item doesn't match the date filter, return false
                if (metadata.LastWriteTime < startDate)
                {
                    return false;
                }
            }

            if ((Flags & SearchOptionsFlags.FilterSize) != 0)
            {
                long minSize = 0;
                long maxSize = long.MaxValue;

                switch (FileSize)
                {
                    case SearchFilterSize.Empty:
                        maxSize = 0;
                        break;

                    case SearchFilterSize.Tiny:
                        minSize = 1;
                        maxSize = 1024; // Up to 1 KB
                        break;

                    case SearchFilterSize.Small:
                        minSize = 1024;
                        maxSize = 1024 * 1024; // 1 KB to 1 MB
                        break;

                    case SearchFilterSize.Medium:
                        minSize = 1024 * 1024;
                        maxSize = 1024 * 1024 * 10; // 1 MB to 10 MB
                        break;

                    case SearchFilterSize.Large:
                        minSize = 1024 * 1024 * 10;
                        maxSize = 1024 * 1024 * 100; // 10 MB to 100 MB
                        break;

                    case SearchFilterSize.Huge:
                        minSize = 1024 * 1024 * 100;
                        maxSize = 1024 * 1024 * 1000; // 100 MB to 1 GB
                        break;

                    case SearchFilterSize.Gigantic:
                        minSize = 1024 * 1024 * 1000; // Greater than 1 GB
                        break;
                }

                // If the item doesn't match the size filter, return false
                if (metadata.Size < minSize || metadata.Size > maxSize)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class FileSystemHelper
    {
        private static FileSystemItem[] specialDirs = null!;
        private static FileSystemItem[] logicalDrives = null!;

        static FileSystemHelper()
        {
            ClearCache();
            IsCaseSensitive = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static bool IsCaseSensitive { get; }

        public static FileSystemItem[] SpecialDirs => specialDirs;

        public static FileSystemItem[] LogicalDrives => logicalDrives;

        public static string GetDownloadsFolderPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else
            {
                throw new PlatformNotSupportedException("This platform is not supported.");
            }
        }

        public static void ClearCache()
        {
            List<FileSystemItem> drives = new();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady && drive.RootDirectory != null)
                    {
                        string driveIcon = string.Empty;
                        switch (drive.DriveType)
                        {
                            case DriveType.NoRootDirectory:
                                continue;

                            case DriveType.Removable:
                                driveIcon = $"{MaterialIcons.HardDrive}";
                                break;

                            case DriveType.Fixed:
                                driveIcon = $"{MaterialIcons.HardDrive}";
                                break;

                            case DriveType.Network:
                                driveIcon = $"{MaterialIcons.SmbShare}";
                                break;

                            case DriveType.CDRom:
                                driveIcon = $"{MaterialIcons.Album}";
                                break;

                            case DriveType.Ram:
                                driveIcon = $"{MaterialIcons.Database}";
                                break;

                            default:
                            case DriveType.Unknown:
                                driveIcon = $"{MaterialIcons.DeviceUnknown}";
                                break;
                        }

                        string name = drive.VolumeLabel;
                        if (string.IsNullOrEmpty(name))
                        {
                            name = "Local Disk";
                        }

                        name += $" ({drive.Name})";

                        drives.Add(new FileSystemItem(drive.RootDirectory.FullName, driveIcon, name, FileSystemItemFlags.Folder));
                    }
                }
                catch (Exception)
                {
                }
            }

            logicalDrives = [.. drives];
            try
            {
                List<FileSystemItem> items =
                [
                    new(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{MaterialIcons.DesktopWindows}", FileSystemItemFlags.Folder),
                    new(GetDownloadsFolderPath(), $"{MaterialIcons.Download}", FileSystemItemFlags.Folder),
                    new(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{MaterialIcons.Description}", FileSystemItemFlags.Folder),
                    new(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), $"{MaterialIcons.LibraryMusic}", FileSystemItemFlags.Folder),
                    new(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), $"{MaterialIcons.Image}", FileSystemItemFlags.Folder),
                    new(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), $"{MaterialIcons.VideoLibrary}", FileSystemItemFlags.Folder),
                ];

                specialDirs = [.. items];
            }
            catch
            {
                specialDirs = [];
            }
            cache.Clear();
        }

        public static IEnumerable<FileSystemItem> Refresh(string folder, RefreshFlags refreshFlags, List<string>? allowedExtensions, SearchOptions searchOptions, Func<FileMetadata, string> fileDecorator, char? folderDecorator)
        {
            StringComparison comparison = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            bool ignoreHidden = (!searchOptions.Enabled && (refreshFlags & RefreshFlags.Hidden) == 0) || (searchOptions.Enabled && (searchOptions.Flags & SearchOptionsFlags.Hidden) == 0);
            bool onlyAllowFilteredExtensions = (refreshFlags & RefreshFlags.OnlyAllowFilteredExtensions) != 0;

            SearchOption option = searchOptions.Enabled && (searchOptions.Flags & SearchOptionsFlags.Subfolders) != 0 ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (onlyAllowFilteredExtensions && allowedExtensions is null)
            {
                throw new ArgumentNullException(nameof(allowedExtensions));
            }

            int id = 0;
            bool emitFolders = (refreshFlags & RefreshFlags.Folders) != 0;
            bool emitFiles = (refreshFlags & RefreshFlags.Files) != 0;
            foreach (var metadata in FileUtilities.EnumerateEntries(folder, option))
            {
                var flags = metadata.Attributes;
                var isDir = (flags & FileAttributes.Directory) != 0;
                if (!isDir && !emitFiles)
                    continue;
                if (isDir && !emitFolders)
                    continue;
                if ((flags & FileAttributes.System) != 0)
                    continue;
                if ((flags & FileAttributes.Hidden) != 0 && ignoreHidden)
                    continue;
                if ((flags & FileAttributes.Device) != 0)
                    continue;

                var span = metadata.Path.AsSpan();
                var name = Path.GetFileName(span);

                if (!FileSystemSearcher.IsMatch(name, searchOptions.Pattern, comparison))
                {
                    continue;
                }

                if (searchOptions.Filter(metadata))
                {
                    if (onlyAllowFilteredExtensions && !isDir)
                    {
                        var ext = Path.GetExtension(name);
                        if (!allowedExtensions!.Contains(ext, comparison))
                        {
                            continue;
                        }
                    }

                    var itemName = option == SearchOption.AllDirectories ? $"{name}##{id++}" : name.ToString();
                    var decorator = isDir ? $"{folderDecorator}" : fileDecorator(metadata);
                    FileSystemItem item = new(metadata, decorator, itemName, isDir ? FileSystemItemFlags.Folder : FileSystemItemFlags.None);
                    yield return item;
                }
            }
        }

        private static readonly Dictionary<string, List<FileSystemItem>> cache = new();

        public static List<FileSystemItem> GetFileSystemEntries(string folder, RefreshFlags refreshFlags, List<string>? allowedExtensions)
        {
            if (cache.TryGetValue(folder, out var cached))
            {
                return cached;
            }

            List<FileSystemItem> items = new();

            bool folders = (refreshFlags & RefreshFlags.Folders) != 0;
            bool files = (refreshFlags & RefreshFlags.Files) != 0;
            bool onlyAllowFilteredExtensions = (refreshFlags & RefreshFlags.OnlyAllowFilteredExtensions) != 0;

            if (onlyAllowFilteredExtensions && allowedExtensions is null)
            {
                throw new ArgumentNullException(nameof(allowedExtensions));
            }

            foreach (var fse in Directory.GetFileSystemEntries(folder, string.Empty))
            {
                var flags = File.GetAttributes(fse);
                if ((flags & FileAttributes.System) != 0)
                    continue;
                if ((flags & FileAttributes.Hidden) != 0)
                    continue;
                if ((flags & FileAttributes.Device) != 0)
                    continue;

                if ((flags & FileAttributes.Directory) != 0)
                {
                    if (folders)
                    {
                        items.Add(new(fse, $"{MaterialIcons.Folder}", FileSystemItemFlags.Folder));
                    }

                    continue;
                }
                else if (files)
                {
                    if (onlyAllowFilteredExtensions)
                    {
                        var ext = Path.GetExtension(fse.AsSpan());
                        if (allowedExtensions!.Contains(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            items.Add(new FileSystemItem(fse, string.Empty, FileSystemItemFlags.None));
                        }
                    }
                    else
                    {
                        items.Add(new FileSystemItem(fse, string.Empty, FileSystemItemFlags.None));
                    }
                }
            }

            cache.Add(folder, items);
            return items;
        }
    }
}