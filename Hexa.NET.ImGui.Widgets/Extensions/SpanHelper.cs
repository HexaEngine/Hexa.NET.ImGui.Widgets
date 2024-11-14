namespace Hexa.NET.ImGui.Widgets.Extensions
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public static unsafe class SpanHelper
    {
        public static ReadOnlySpan<char> CreateReadOnlySpanFromNullTerminated(char* pointer)
        {
            int len = StrLen(pointer);
            return new ReadOnlySpan<char>(pointer, len);
        }

        public static ReadOnlySpan<byte> CreateReadOnlySpanFromNullTerminated(byte* pointer)
        {
            int len = StrLen(pointer);
            return new ReadOnlySpan<byte>(pointer, len);
        }

#if NETSTANDARD2_0
        public static bool StartsWith(this ReadOnlySpan<char> span, char c)
        {
            return span.Length > 0 && span[0] == c;
        }

        public static bool StartsWith(this string span, char c)
        {
            return span.Length > 0 && span[0] == c;
        }

        public static void Append(this StringBuilder sb, ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
                return;

            // Ensure the StringBuilder has enough capacity to avoid resizing during append
            sb.EnsureCapacity(sb.Length + span.Length);

            // Manually append each character from the span to the StringBuilder
            foreach (char c in span)
            {
                sb.Append(c);
            }
        }

        public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            fixed (char* pChars = chars)
            {
                fixed (byte* pBytes = bytes)
                {
                    return encoding.GetBytes(pChars, chars.Length, pBytes, bytes.Length);
                }
            }
        }

        public static int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            fixed (byte* pBytes = bytes)
            {
                fixed (char* pChars = chars)
                {
                    return encoding.GetChars(pBytes, bytes.Length, pChars, chars.Length);
                }
            }
        }

        public static int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            fixed (byte* pBytes = bytes)
            {
                return encoding.GetCharCount(pBytes, bytes.Length);
            }
        }

        public static int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
        {
            fixed (char* pChars = chars)
            {
                return encoding.GetByteCount(pChars, chars.Length);
            }
        }

#endif
    }

    public static unsafe class CollectionsExtensions
    {
        public static void AddRange<T>(this IList<T> list, Span<T> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                list.Add(values[i]);
            }
        }
    }
}