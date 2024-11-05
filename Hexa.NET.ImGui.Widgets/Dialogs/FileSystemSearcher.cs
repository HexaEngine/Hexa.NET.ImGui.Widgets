namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using System;
    using System.Text;

    public unsafe class FileSystemSearcher
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

        public static bool IsMatch(ReadOnlySpan<byte> fileName, string pattern, StringComparison comparison)
        {
            var charCount = Encoding.UTF8.GetCharCount(fileName);
            Span<char> chars = stackalloc char[charCount];
            Encoding.UTF8.GetChars(fileName, chars);

            return IsMatch(chars, pattern, comparison);
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
}