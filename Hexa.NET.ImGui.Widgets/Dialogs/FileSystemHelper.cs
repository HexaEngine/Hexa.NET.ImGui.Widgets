namespace Kitty.UI.Dialogs
{
    using Hexa.NET.ImGui.Widgets.Extensions;
    using HexaEngine.Editor;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.AccessControl;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class FileSystemSearcher
    {
        public static IEnumerable<string> Search(string folder, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(folder, searchPattern, searchOption);
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

    public struct FileSystemItem : IEquatable<FileSystemItem>
    {
        public string Path;
        public string Icon;
        public string Name;
        public FileSystemItemFlags Flags;
        public DateTime DateModified;
        public string Type;
        public long Size;
        public CommonFilePermissions Permissions;

        public FileSystemItem(string path, string icon, string name, string type, FileSystemItemFlags flags)
        {
            Path = path;
            Icon = icon;
            Name = name;
            Flags = flags;
            Type = type;

            DateModified = File.GetLastWriteTime(path);

            if (IsFile)
            {
                Size = new FileInfo(path).Length;
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
            Path = path;
            Icon = icon;
            Name = name;
            Flags = flags;
            DateModified = File.GetLastWriteTime(path);

            if (IsFile)
            {
                Size = new FileInfo(path).Length;
                Type = DetermineFileType(System.IO.Path.GetExtension(path.AsSpan()));
            }
            else
            {
                Type = "File Folder";
            }
        }

        public FileSystemItem(string path, string icon, FileSystemItemFlags flags)
        {
            Path = path;
            Icon = icon;
            Name = System.IO.Path.GetFileName(path);
            Flags = flags;

            var mode = File.GetAttributes(path);

            DateModified = File.GetLastWriteTime(path);
            if (IsFile)
            {
                Size = new FileInfo(path).Length;
                Type = DetermineFileType(System.IO.Path.GetExtension(path.AsSpan()));
            }
            else
            {
                Type = "File Folder";
            }
        }

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
            fileTypes.Add(hash, type);

            return type;
        }

        private static void LoadFileTypes()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("fileTypes.json"));

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                var fileTypeDict = JsonSerializer.Deserialize(stream, SourceGenerationContextDictionary.Default.DictionaryStringString);

                if (fileTypeDict != null)
                {
                    foreach (var kvp in fileTypeDict)
                    {
                        fileTypes[GetSpanHash(kvp.Key.AsSpan()[1..])] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static readonly Dictionary<ulong, string> fileTypes = [];

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

        public readonly bool IsFile => (Flags & FileSystemItemFlags.Folder) == 0;

        public readonly bool IsFolder => (Flags & FileSystemItemFlags.Folder) != 0;

        public readonly bool IsHidden => (Flags & FileSystemItemFlags.Hidden) != 0;

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
        IgnoreHidden = 8,
    }

    public class FileSystemHelper
    {
        private static FileSystemItem[] specialDirs;
        private static FileSystemItem[] logicalDrives = Directory.GetLogicalDrives().Select(x => new FileSystemItem(x, string.Empty, x, FileSystemItemFlags.Folder)).ToArray();

        static FileSystemHelper()
        {
            try
            {
                specialDirs = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                }.Select(x => new FileSystemItem(x, string.Empty, FileSystemItemFlags.Folder)).ToArray();
            }
            catch
            {
                specialDirs = [];
            }
        }

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

        public static void Refresh(string folder, List<FileSystemItem> entries, RefreshFlags refreshFlags, List<string>? allowedExtensions, Func<string, string> fileDecorator, char? folderDecorator)
        {
            bool ignoreHidden = (refreshFlags & RefreshFlags.IgnoreHidden) != 0;
            bool onlyAllowFilteredExtensions = (refreshFlags & RefreshFlags.OnlyAllowFilteredExtensions) != 0;
            entries.Clear();
            if ((refreshFlags & RefreshFlags.Folders) != 0)
            {
                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    var flags = File.GetAttributes(dir);
                    if ((flags & FileAttributes.System) != 0)
                        continue;
                    if ((flags & FileAttributes.Hidden) != 0 && ignoreHidden)
                        continue;
                    if ((flags & FileAttributes.Device) != 0)
                        continue;

                    entries.Add(new(dir, $"{folderDecorator}", Path.GetFileName(dir), FileSystemItemFlags.Folder));
                }
            }

            if (onlyAllowFilteredExtensions && allowedExtensions is null)
            {
                throw new ArgumentNullException(nameof(allowedExtensions));
            }

            if ((refreshFlags & RefreshFlags.Files) != 0)
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    var flags = File.GetAttributes(file);
                    if ((flags & FileAttributes.System) != 0)
                        continue;
                    if ((flags & FileAttributes.Hidden) != 0 && ignoreHidden)
                        continue;
                    if ((flags & FileAttributes.Device) != 0)
                        continue;

                    if (onlyAllowFilteredExtensions)
                    {
                        var ext = Path.GetExtension(file.AsSpan());
                        if (allowedExtensions.Contains(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            entries.Add(new(file, fileDecorator(file), Path.GetFileName(file), FileSystemItemFlags.None));
                        }
                    }
                    else
                    {
                        entries.Add(new(file, fileDecorator(file), Path.GetFileName(file), FileSystemItemFlags.None));
                    }
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
                        if (allowedExtensions.Contains(ext, StringComparison.OrdinalIgnoreCase))
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