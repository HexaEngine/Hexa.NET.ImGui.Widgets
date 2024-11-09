namespace Hexa.NET.ImGui.Widgets.IO
{
    using Hexa.NET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public static unsafe partial class FileUtils
    {
        public static unsafe partial class Win
        {
            public static IEnumerable<FileMetadata> EnumerateEntries(string path, string pattern, SearchOption option)
            {
                UnsafeStack<StdWString> walkStack = new();

                {
                    StdWString str = path;
                    CorrectPath(str);
                    if (str[str.Size - 1] != '\\')
                    {
                        str.Append('\\');
                    }
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

            public static nint StartSearch(StdWString st, out WIN32_FIND_DATA data)
            {
                return FindFirstFileW(st.Data, out data);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static FileMetadata Convert(WIN32_FIND_DATA data, StdWString path)
            {
                int length = StrLen(data.cFileName);
                StdWString str;

                str = new(length + path.Size);
                str.Append(path.Data, path.Size - 1);
                str.Append(data.cFileName, length);

                *(str.Data + str.Size) = '\0';

                FileMetadata metadata = new();
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

            public const uint FILE_READ_ATTRIBUTES = 0x80;
            public const uint FILE_SHARE_READ = 0x1;
            public const uint OPEN_EXISTING = 3;
            public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
            public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

            public static FileMetadata GetFileMetadata(string filePath)
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
                        result = !PatternMatcher.IsMatch(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p), pattern, StringComparison.CurrentCulture);
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
        }
    }
}