namespace Hexa.NET.ImGui.Widgets.IO
{
    using Hexa.NET.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    public static unsafe partial class FileUtils
    {
        public static long GetFileSize(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Win.GetFileMetadata(filePath).Size;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX.GetFileMetadata(filePath).Size;
            }
            else
            {
                return Unix.GetFileMetadata(filePath).Size;
            }
        }

        public static FileMetadata GetFileMetadata(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Win.GetFileMetadata(filePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX.GetFileMetadata(filePath);
            }
            else
            {
                return Unix.GetFileMetadata(filePath);
            }
        }

        public static IEnumerable<FileMetadata> EnumerateEntries(string path, string pattern, SearchOption option)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Win.EnumerateEntries(path, pattern, option);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX.EnumerateEntries(path, pattern, option);
            }
            else
            {
                return Unix.EnumerateEntries(path, pattern, option);
            }
        }

        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

        public static void CorrectPath(StdString str)
        {
            byte* ptr = str.Data;
            byte* end = ptr + str.Size;
            while (ptr != end)
            {
                byte c = *ptr;
                if (c == '/' || c == '\\')
                {
                    *ptr = (byte)DirectorySeparatorChar;
                }
                ptr++;
            }
        }

        public static void CorrectPath(StdWString str)
        {
            char* ptr = str.Data;
            char* end = ptr + str.Size;
            while (ptr != end)
            {
                char c = *ptr;
                if (c == '/' || c == '\\')
                {
                    *ptr = DirectorySeparatorChar;
                }
                ptr++;
            }
        }
    }
}