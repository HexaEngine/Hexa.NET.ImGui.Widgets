namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using System.Runtime.InteropServices;

    public static class PathValidator
    {
        private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
        private static readonly char[] dirSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        public static bool IsValidPath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            ReadOnlySpan<char> pathSpan = path.AsSpan();

            while (pathSpan.Length > 0)
            {
                int index = pathSpan.LastIndexOfAny(dirSeparators);
                if (index <= 0)
                {
                    break; // Just exit here and do the last check outside of the loop this prevents double checking the root part.
                }

                // exclude the separator
                var part = pathSpan[(index + 1)..];

                // Check the part of the path
                if (part.IndexOfAny(invalidPathChars) != -1)
                {
                    return false;
                }

                // exclude the part and separator
                pathSpan = pathSpan[..index];
            }

            // Check the last remaining segment of the path
            if (Path.IsPathRooted(pathSpan))
            {
                return CheckRoot(pathSpan);
            }

            // If its not rooted, check the last segment
            if (pathSpan.IndexOfAny(invalidPathChars) != -1)
            {
                return false;
            }

            return true;
        }

        private static bool CheckRoot(ReadOnlySpan<char> path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check if the drive letter is valid for Windows paths
                ReadOnlySpan<char> root = Path.GetPathRoot(path);
                if (root.Length > 1 && root[1] == ':')
                {
                    char driveLetter = root[0];
                    if (!(driveLetter >= 'A' && driveLetter <= 'Z' || driveLetter >= 'a' && driveLetter <= 'z'))
                    {
                        return false;
                    }
                }
            }
            else
            {
                // On Unix-based systems, root should be just '/'
                if (path.Length == 0 || path[0] != Path.DirectorySeparatorChar)
                {
                    return false;
                }
            }
            return true;
        }
    }
}