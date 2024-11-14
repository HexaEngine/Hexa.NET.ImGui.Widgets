namespace Hexa.NET.ImGui.Widgets.IO
{
    using Hexa.NET.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A Utility for working with file systems. Will be moved to Hexa.NET.Utilities.
    /// </summary>
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

        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
        {
            int length = path.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                char c = path[i];
                if (c == '.')
                {
                    if (i == length - 1) // Last character is a dot
                        return ReadOnlySpan<char>.Empty;
                    return path.Slice(i);
                }
                if (c == '/' || c == '\\')
                    break; // Stop if a directory separator is found
            }
            return ReadOnlySpan<char>.Empty;
        }

        public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            int length = path.Length;

            // Trim any trailing slashes
            while (length > 0 && (path[length - 1] == '/' || path[length - 1] == '\\'))
                length--;

            // Find the last directory separator
            for (int i = length - 1; i >= 0; i--)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    // If we find a separator at the start of the path, handle root cases (e.g., "/")
                    return i == 0 ? path.Slice(0, 1) : path.Slice(0, i);
                }
            }

            // No separator found, meaning there's no directory part in the path
            return ReadOnlySpan<char>.Empty;
        }

        public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            int length = path.Length;

            // Trim trailing slashes
            while (length > 0 && (path[length - 1] == '/' || path[length - 1] == '\\'))
                length--;

            if (length == 0)
                return ReadOnlySpan<char>.Empty;

            // Find the last directory separator
            for (int i = length - 1; i >= 0; i--)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    // Return the part after the last directory separator
                    return path.Slice(i + 1, length - (i + 1));
                }
            }

            // No separator found, the whole trimmed path is the file name
            return path.Slice(0, length);
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return false;

            // Check if the path starts with a directory separator (Unix-style rooted path)
            if (path[0] == '/' || path[0] == '\\')
                return true;

            // Check for Windows-style rooted path (e.g., "C:\")
            if (path.Length > 1 && char.IsLetter(path[0]) && path[1] == ':')
                return true;

            return false;
        }

        public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            // Check for Unix-style root (e.g., "/")
            if (path[0] == '/' || path[0] == '\\')
                return path.Slice(0, 1);

            // Check for Windows-style root (e.g., "C:\")
            if (path.Length > 1 && char.IsLetter(path[0]) && path[1] == ':')
            {
                // Include the optional backslash after the drive letter, if present
                if (path.Length > 2 && (path[2] == '/' || path[2] == '\\'))
                    return path.Slice(0, 3);
                return path.Slice(0, 2);
            }

            // No root found
            return ReadOnlySpan<char>.Empty;
        }
    }
}