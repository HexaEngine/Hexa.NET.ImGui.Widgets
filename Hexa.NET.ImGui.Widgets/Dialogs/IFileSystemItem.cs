namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    public interface IFileSystemItem
    {
        string Path { get; }

        string Icon { get; }

        string Name { get; }

        FileSystemItemFlags Flags { get; }

        DateTime DateModified { get; }

        string Type { get; }

        long Size { get; }

        CommonFilePermissions Permissions { get; }

        public bool IsFile => (Flags & FileSystemItemFlags.Folder) == 0;

        public bool IsFolder => (Flags & FileSystemItemFlags.Folder) != 0;

        public bool IsHidden => (Flags & FileSystemItemFlags.Hidden) != 0;

        public static int CompareByBase(IFileSystemItem a, IFileSystemItem b)
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
    }
}