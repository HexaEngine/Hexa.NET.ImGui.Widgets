namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.Utilities;
    using System;
    using System.IO;

    public struct FileMetadata
    {
        public StdWString Path;
        public long Size;
        public DateTime CreationTime;
        public DateTime LastAccessTime;
        public DateTime LastWriteTime;
        public FileAttributes Attributes;
    }
}