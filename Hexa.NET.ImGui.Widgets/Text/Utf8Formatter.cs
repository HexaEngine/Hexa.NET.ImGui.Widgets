﻿namespace Hexa.NET.ImGui.Widgets.Text
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Helper class to work with the hidden C# Feature TypedReference.
    /// </summary>
    public static class RuntimeTypeHandleHelper
    {
        public static bool Is<T>(this RuntimeTypeHandle typeHandle, TypedReference reference, [MaybeNullWhen(false)] out T t)
        {
            // This is a workaround for the lack of support for TypedReference in C#.
            t = default;
            if (typeHandle.Value == typeof(T).TypeHandle.Value)
            {
                t = __refvalue(reference, T);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pattern matching for TypedReference.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static unsafe bool Is<T>(this TypedReference reference, [MaybeNullWhen(false)] out T t)
        {
            // This is a workaround for the lack of support for TypedReference in C#.
            t = default;
            if (__reftype(reference) == typeof(T))
            {
                t = __refvalue(reference, T);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Will be moved to Hexa.NET.Utilities later.
    /// </summary>
    public static class Utf8Formatter
    {
        unsafe static int Printf(byte* buf, int bufSize, string format, __arglist)
        {
            ArgIterator args = new(__arglist);
            int j = 0;
            for (int i = 0; i < format.Length;)
            {
                var c = format[i];
                if (c == '%' && i < format.Length - 1)
                {
                    i++; // Skip '%'
                    c = format[i]; // Get the format specifier
                    i++; // Move to the next character

                    // parse options
                    int width = -1; // Width of the field
                    int precision = -1; // Number of digits after the decimal point (precision)

                    bool leftAlign = false;
                    bool forceSign = false;
                    bool spaceSign = false;
                    bool alternateForm = false;
                    bool zeroPad = false;

                    while (i < format.Length)
                    {
                        char temp = format[i];

                        // Parse flags
                        if (temp == '-')
                        {
                            leftAlign = true;
                        }
                        else if (temp == '+')
                        {
                            forceSign = true;
                        }
                        else if (temp == ' ')
                        {
                            spaceSign = true;
                        }
                        else if (temp == '#')
                        {
                            alternateForm = true;
                        }
                        else if (temp == '0' && width == -1) // Only consider '0' as zeroPad if width is not set yet
                        {
                            zeroPad = true;
                        }
                        // Parse width
                        else if (temp >= '1' && temp <= '9')
                        {
                            if (width == -1)
                            {
                                width = 0;
                            }
                            width = width * 10 + (temp - '0');
                        }
                        // Parse precision (digits after the decimal point)
                        else if (temp == '.')
                        {
                            precision = 0; // Start counting digits
                            i++;
                            while (i < format.Length && (temp = format[i]) >= '0' && temp <= '9')
                            {
                                precision = precision * 10 + (temp - '0');
                                i++;
                            }
                            i--; // Step back to account for the next increment
                        }
                        else
                        {
                            break; // Exit loop when an unknown character is found
                        }
                        i++;
                    }

                    // Backtrack to the last character of the format specifier
                    if (i != format.Length)
                    {
                        while (format[i - 1] == ' ')
                        {
                            i--;
                        }
                    }

                    var type = args.GetNextArgType();
                    var arg = args.GetNextArg(type);

                    switch (c)
                    {
                        case 'd':
                            {
                                if (arg.Is<int>(out var value))
                                {
                                    j += Utf8Formatter.Format(value, buf + j, bufSize - j);
                                }
                                else if (arg.Is<uint>(out var uintValue))
                                {
                                    j += Utf8Formatter.Format(uintValue, buf + j, bufSize - j);
                                }
                                else
                                {
                                    throw new NotSupportedException("Unsupported integer type");
                                }
                            }
                            break;

                        case 'u':
                            {
                                j += Utf8Formatter.Format(__refvalue(arg, uint), buf + j, bufSize - j - 1);
                            }
                            break;

                        case 'f':
                            {
                                if (precision == -1)
                                {
                                    precision = 6; // Default precision for floating-point
                                }
                                if (arg.Is<double>(out var doubleVar))
                                {
                                    j += Utf8Formatter.Format(doubleVar, buf + j, bufSize - j - 1, precision);
                                }
                                else if (arg.Is<float>(out var floatVar))
                                {
                                    j += Utf8Formatter.Format(floatVar, buf + j, bufSize - j - 1, precision);
                                }
                                else
                                {
                                    throw new NotSupportedException("Unsupported floating-point type");
                                }
                            }
                            break;

                        case 'c':
                            {
                                j += Utf8Formatter.EncodeUnicodeChar(__refvalue(arg, char), buf + j, bufSize - j);
                            }
                            break;

                        case '%':
                            buf[j++] = (byte)'%';
                            break;
                    }
                }
                else
                {
                    buf[j++] = (byte)c;
                    i++;
                }

                if (j == bufSize - 1) // -1 to leave room for null terminator
                {
                    break;
                }
            }

            buf[j] = 0;

            return j;
        }

        public static unsafe int StrLen(byte* str)
        {
            int len = 0;
            while (str[len] != 0)
            {
                len++;
            }
            return len;
        }

        public static unsafe int FormatByteSize(byte* buf, int bufSize, long byteSize, bool addSuffixSpace, int digits = -1)
        {
            const int suffixes = 7;
            int suffixIndex = 0;
            float size = byteSize;
            while (size >= 1024 && suffixIndex < suffixes)
            {
                size /= 1024;
                suffixIndex++;
            }

            int suffixSize = suffixIndex == 0 ? 1 : 2;  // 'B' or 'KB', 'MB', etc.

            if (addSuffixSpace)
            {
                suffixSize++;
            }

            // Early exit if the buffer is too small
            if (bufSize - suffixSize <= 0)
            {
                if (bufSize > 0)
                {
                    buf[0] = 0; // Null-terminate
                }
                return 0;
            }

            int i = Format(size, buf, bufSize - suffixSize, digits); // overwrite terminator from FormatFloat.

            if (addSuffixSpace)
            {
                buf[i++] = (byte)' ';
            }

            byte suffix = suffixIndex switch
            {
                1 => (byte)'K',
                2 => (byte)'M',
                3 => (byte)'G',
                4 => (byte)'T',
                5 => (byte)'P',
                6 => (byte)'E',
                _ => 0,
            };

            if (suffix != 0)
            {
                buf[i++] = suffix;
            }

            buf[i++] = (byte)'B';
            buf[i] = 0; // Null-terminate

            return i;
        }

        public static unsafe uint FractionToInt(float fraction, int precision)
        {
            fraction -= (uint)fraction;
            return (uint)(fraction * MathF.Pow(10, precision));
        }

        public static unsafe uint FractionToIntLimit(float fraction, int maxPrecision)
        {
            uint result = 0;
            fraction -= (uint)fraction;
            for (int i = 0; i < maxPrecision; i++)
            {
                fraction *= 10;
                int digit = (int)fraction;
                result = result * 10 + (uint)digit;
                fraction -= digit;
                if (fraction == 0)
                {
                    break;
                }
            }

            return result;
        }

        public static unsafe ulong FractionToInt(double fraction, int precision)
        {
            fraction -= (uint)fraction;
            return (uint)(fraction * MathF.Pow(10, precision));
        }

        public static unsafe ulong FractionToIntLimit(double fraction, int maxPrecision)
        {
            uint result = 0;
            fraction -= (uint)fraction;
            for (int i = 0; i < maxPrecision; i++)
            {
                fraction *= 10;
                int digit = (int)fraction;
                result = result * 10 + (uint)digit;
                fraction -= digit;
                if (fraction == 0)
                {
                    break;
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(float value, byte* buffer, int bufSize, int digits = -1)
        {
            if (float.IsNaN(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'N';
                buffer[1] = (byte)'a';
                buffer[2] = (byte)'N';
                buffer[3] = 0;
                return 3;
            }
            if (float.IsPositiveInfinity(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'i';
                buffer[1] = (byte)'n';
                buffer[2] = (byte)'f';
                buffer[3] = 0;
                return 3;
            }
            if (float.IsNegativeInfinity(value))
            {
                if (bufSize < 5)
                {
                    return 0;
                }
                buffer[0] = (byte)'-';
                buffer[1] = (byte)'i';
                buffer[2] = (byte)'n';
                buffer[3] = (byte)'f';
                buffer[4] = 0;
                return 4;
            }
            if (value == 0)
            {
                if (bufSize < 2)
                {
                    return 0;
                }
                buffer[0] = (byte)'0';
                buffer[1] = 0;
                return 1;
            }

            int number = (int)value; // Get the integer part of the number
            float fraction = value - number; // Get the fractional part of the number

            if (fraction < 0)
            {
                fraction = -fraction;
            }

            int offset = Format(number, buffer, bufSize);
            buffer += offset; // Move the buffer pointer to the right
            bufSize -= offset; // Adjust the buffer size

            if (bufSize == 0)
            {
                return offset;
            }

            buffer[0] = (byte)'.';
            buffer++; // Move the buffer pointer to the right
            bufSize--; // Adjust the buffer size
            offset++; // Increment the offset

            uint factionInt;
            if (digits >= 0)
            {
                factionInt = FractionToInt(fraction, Math.Min(bufSize - 1, digits));
            }
            else
            {
                factionInt = FractionToIntLimit(fraction, bufSize - 1);
            }

            offset += Format(factionInt, buffer, bufSize);

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(double value, byte* buffer, int bufSize, int digits = -1)
        {
            if (double.IsNaN(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'N';
                buffer[1] = (byte)'a';
                buffer[2] = (byte)'N';
                buffer[3] = 0;
                return 3;
            }
            if (double.IsPositiveInfinity(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'i';
                buffer[1] = (byte)'n';
                buffer[2] = (byte)'f';
                buffer[3] = 0;
                return 3;
            }
            if (double.IsNegativeInfinity(value))
            {
                if (bufSize < 5)
                {
                    return 0;
                }
                buffer[0] = (byte)'-';
                buffer[1] = (byte)'i';
                buffer[2] = (byte)'n';
                buffer[3] = (byte)'f';
                buffer[4] = 0;
                return 4;
            }
            if (value == 0)
            {
                if (bufSize < 2)
                {
                    return 0;
                }
                buffer[0] = (byte)'0';
                buffer[1] = 0;
                return 1;
            }

            long number = (long)value; // Get the integer part of the number
            double fraction = value - number; // Get the fractional part of the number

            if (fraction < 0)
            {
                fraction = -fraction;
            }

            int offset = Format(number, buffer, bufSize);
            buffer += offset; // Move the buffer pointer to the right
            bufSize -= offset; // Adjust the buffer size

            if (bufSize == 0)
            {
                return offset;
            }

            buffer[0] = (byte)'.';
            buffer++; // Move the buffer pointer to the right
            bufSize--; // Adjust the buffer size
            offset++; // Increment the offset

            ulong factionInt;
            if (digits >= 0)
            {
                factionInt = FractionToInt(fraction, Math.Min(bufSize - 1, digits));
            }
            else
            {
                factionInt = FractionToIntLimit(fraction, bufSize - 1);
            }

            offset += Format(factionInt, buffer, bufSize);

            return offset;
        }

        public static unsafe int Format(nint value, byte* buffer, int bufSize)
        {
            if (sizeof(nint) == sizeof(int))
            {
                return Format((int)value, buffer, bufSize);
            }
            if (sizeof(nint) == sizeof(long))
            {
                return Format((long)value, buffer, bufSize);
            }
            return 0;
        }

        public static unsafe int Format(nuint value, byte* buffer, int bufSize)
        {
            if (sizeof(nuint) == sizeof(uint))
            {
                return Format((uint)value, buffer, bufSize);
            }
            if (sizeof(nuint) == sizeof(ulong))
            {
                return Format((ulong)value, buffer, bufSize);
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(sbyte value, byte* buffer, int bufSize)
        {
            bool negative = value < 0;
            if (!negative && bufSize < 2 || negative && bufSize < 3)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            byte abs = (byte)(negative ? -value : value); // Handle int.MinValue case

            EncodeNegativeSign(&buffer, &bufSize, negative);

            return Format(abs, buffer, bufSize) + (negative ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(byte value, byte* buffer, int bufSize)
        {
            if (bufSize < 2)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            if (value == 0)
            {
                buffer[0] = (byte)'0';
                buffer[1] = 0; // Null-terminate
                return 1; // Include null terminator
            }

            int i = 0;
            while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
            {
                byte oldValue = value;
                value = (byte)((value * 205) >> 11); // Approximate value / 10
                byte mod = (byte)(oldValue - value * 10); // Calculate value % 10
                buffer[i++] = (byte)('0' + mod);
            }

            // Reverse the digits for correct order
            for (int j = 0, k = i - 1; j < k; j++, k--)
            {
                (buffer[k], buffer[j]) = (buffer[j], buffer[k]);
            }

            buffer[i] = 0; // Null-terminate

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(short value, byte* buffer, int bufSize)
        {
            bool negative = value < 0;
            if (!negative && bufSize < 2 || negative && bufSize < 3)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            ushort abs = (ushort)(negative ? -value : value); // Handle int.MinValue case

            EncodeNegativeSign(&buffer, &bufSize, negative);

            return Format(abs, buffer, bufSize) + (negative ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(ushort value, byte* buffer, int bufSize)
        {
            if (bufSize < 2)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            if (value == 0)
            {
                buffer[0] = (byte)'0';
                buffer[1] = 0; // Null-terminate
                return 1;
            }

            int i = 0;
            if (value < 1029) // Fast path for small values
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    ushort oldValue = value;
                    value = (ushort)((value * 205) >> 11); // Approximate value / 10
                    ushort mod = (ushort)(oldValue - value * 10); // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }
            else
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    ushort oldValue = value;
                    value /= 10; // Exact value / 10
                    ushort mod = (ushort)(oldValue - value * 10); // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }

            // Reverse the digits for correct order
            for (int j = 0, k = i - 1; j < k; j++, k--)
            {
                (buffer[k], buffer[j]) = (buffer[j], buffer[k]);
            }

            buffer[i] = 0; // Null-terminate

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe int Format(int value, byte* buffer, int bufSize)
        {
            bool negative = value < 0;
            if (!negative && bufSize < 2 || negative && bufSize < 3)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            uint abs = (uint)(negative ? -value : value); // Handle int.MinValue case

            EncodeNegativeSign(&buffer, &bufSize, negative);

            return Format(abs, buffer, bufSize) + (negative ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe int Format(uint value, byte* buffer, int bufSize)
        {
            if (bufSize < 2)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            if (value == 0)
            {
                buffer[0] = (byte)'0';
                buffer[1] = 0; // Null-terminate
                return 1;
            }

            int i = 0;
            if (value < 1029) // Fast path for small values
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    uint oldValue = value;
                    value = (value * 205) >> 11; // Approximate value / 10
                    uint mod = oldValue - value * 10; // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }
            else
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    uint oldValue = value;
                    value /= 10; // Exact value / 10
                    uint mod = oldValue - value * 10; // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }

            // Reverse the digits for correct order
            for (int j = 0, k = i - 1; j < k; j++, k--)
            {
                (buffer[k], buffer[j]) = (buffer[j], buffer[k]);
            }

            buffer[i] = 0; // Null-terminate

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(long value, byte* buffer, int bufSize)
        {
            bool negative = value < 0;
            if (!negative && bufSize < 2 || negative && bufSize < 3)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            EncodeNegativeSign(&buffer, &bufSize, negative);

            ulong abs = (ulong)(negative ? -value : value); // Handle int.MinValue case

            return Format(abs, buffer, bufSize) + (negative ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(ulong value, byte* buffer, int bufSize)
        {
            if (bufSize < 2)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            if (value == 0)
            {
                buffer[0] = (byte)'0';
                buffer[1] = 0; // Null-terminate
                return 1;
            }

            int i = 0;
            if (value < 1029) // 1029 is the largest number that can be divided by 10 and still fit in 32 bits
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    ulong oldValue = value;
                    value = (value * 205) >> 11; // Approximate value / 10
                    ulong mod = oldValue - value * 10; // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }
            else
            {
                while (value > 0 && i < bufSize - 1) // -1 to leave room for null terminator
                {
                    ulong oldValue = value;
                    value /= 10; // Exact value / 10
                    ulong mod = oldValue - value * 10; // Calculate value % 10
                    buffer[i++] = (byte)('0' + mod);
                }
            }

            // Reverse the digits for correct order
            for (int j = 0, k = i - 1; j < k; j++, k--)
            {
                (buffer[k], buffer[j]) = (buffer[j], buffer[k]);
            }

            buffer[i] = 0; // Null-terminate

            return i;
        }

        public static unsafe int FormatHex(nint value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            if (sizeof(nint) == sizeof(int))
            {
                return FormatHex((int)value, buffer, bufSize, leadingZeros, uppercase);
            }
            if (sizeof(nint) == sizeof(long))
            {
                return FormatHex((long)value, buffer, bufSize, leadingZeros, uppercase);
            }
            return 0;
        }

        public static unsafe int FormatHex(nuint value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            if (sizeof(nuint) == sizeof(uint))
            {
                return FormatHex((uint)value, buffer, bufSize, leadingZeros, uppercase);
            }
            if (sizeof(nuint) == sizeof(ulong))
            {
                return FormatHex((ulong)value, buffer, bufSize, leadingZeros, uppercase);
            }
            return 0;
        }

        public static unsafe int FormatHex(byte value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(byte);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount(&value, size);
                return FormatHex(&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex(&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(sbyte value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(sbyte);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(short value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(short);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(ushort value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(ushort);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(int value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(int);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(uint value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(uint);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(long value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(long);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        public static unsafe int FormatHex(ulong value, byte* buffer, int bufSize, bool leadingZeros, bool uppercase)
        {
            const int size = sizeof(ulong);
            if (!leadingZeros)
            {
                (int start, int digits) = DigitsCount((byte*)&value, size);
                return FormatHex((byte*)&value, digits, buffer, bufSize, start, uppercase);
            }
            return FormatHex((byte*)&value, size, buffer, bufSize, 0, uppercase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int FormatHex(byte* value, int width, byte* buffer, int bufSize, int digitOffset = 0, bool uppercase = true)
        {
            int bytesNeeded = width * 2 - digitOffset;
            // Check if the buffer is large enough to hold the hex value
            if (bufSize < bytesNeeded + 1)
            {
                if (bufSize > 0)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small
            }

            char hexLower = uppercase ? 'A' : 'a';

            // Write unaligned hex values
            int baseOffset = digitOffset >> 1; // digitOffset / 2
            int mod = digitOffset & 1; // digitOffset % 2
            if (mod != 0)
            {
                buffer[0] = (byte)((value[baseOffset] >> 4) < 10 ? '0' + (value[baseOffset] >> 4) : hexLower + (value[baseOffset] >> 4) - 10);
                buffer++;
                bufSize--;
            }
            value += baseOffset;
            width -= baseOffset;

            // Write aligned hex values
            for (int i = 0; i < width; i++)
            {
                byte b = value[i];
                buffer[i * 2] = (byte)((b >> 4) < 10 ? '0' + (b >> 4) : hexLower + (b >> 4) - 10);
                buffer[i * 2 + 1] = (byte)((b & 0xF) < 10 ? '0' + (b & 0xF) : hexLower + (b & 0xF) - 10);
            }

            buffer[bytesNeeded] = 0; // Null-terminate

            return bytesNeeded; // Return the number of bytes written
        }

        private static unsafe (int start, int digits) DigitsCount(byte* value, int width)
        {
            int start = 0;
            for (int i = 0; i < width; i++)
            {
                byte b = value[i];
                var nibble1 = (byte)(b >> 4);
                var nibble2 = (byte)(b & 0xF);

                if (nibble1 == 0)
                    start++;
                else
                    break;

                if (nibble2 == 0)
                    start++;
                else
                    break;
            }
            return (start, width * 2 - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe void EncodeNegativeSign(byte** buffer, int* bufSize, bool negative)
        {
            if (*bufSize == 0)
            {
                return;
            }

            if (negative)
            {
                *buffer[0] = (byte)'-';
                bufSize--; // Reserve space for the negative sign
                buffer++; // Move the buffer pointer to the right
            }
        }

        public static unsafe int EncodeUnicodeChar(char c, byte* buf, int bufSize)
        {
            return Encoding.UTF8.GetBytes(&c, 1, buf, bufSize);
        }

        public static unsafe int ConvertUtf16ToUtf8(char* utf16Chars, int utf16Length, byte* utf8Bytes, int utf8Length)
        {
            int utf8Index = 0;

            for (int i = 0; i < utf16Length; i++)
            {
                if (utf8Index >= utf8Length)
                    return utf8Index; // Soft exception: buffer too small, return the number of bytes written so far

                // Read the UTF-16 character (char is 2 bytes)
                char utf16Char = utf16Chars[i];

                // Determine the UTF-16 code point
                int codePoint = utf16Char;

                switch (codePoint)
                {
                    case <= 0x7F:
                        // 1-byte UTF-8 (ASCII)
                        utf8Bytes[utf8Index++] = (byte)codePoint;
                        break;

                    case <= 0x7FF:
                        // 2-byte UTF-8
                        if (utf8Index + 1 >= utf8Length)
                            return utf8Index; // Soft exception: buffer too small, return the number of bytes written so far

                        utf8Bytes[utf8Index++] = (byte)(0xC0 | (codePoint >> 6));
                        utf8Bytes[utf8Index++] = (byte)(0x80 | (codePoint & 0x3F));
                        break;

                    case >= 0xD800 and <= 0xDFFF:
                        if (i + 1 < utf16Length)
                        {
                            char lowSurrogate = utf16Chars[i + 1];
                            if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF) // Low surrogate
                            {
                                // Combine the high surrogate and low surrogate to form the full code point
                                int codePointSurrogate = 0x10000 + ((utf16Char - 0xD800) << 10) + (lowSurrogate - 0xDC00);

                                // This results in a 4-byte UTF-8 sequence
                                if (utf8Index + 3 >= utf8Length)
                                    return utf8Index; // Soft exception: buffer too small, return the number of bytes written so far

                                utf8Bytes[utf8Index++] = (byte)(0xF0 | (codePointSurrogate >> 18));
                                utf8Bytes[utf8Index++] = (byte)(0x80 | ((codePointSurrogate >> 12) & 0x3F));
                                utf8Bytes[utf8Index++] = (byte)(0x80 | ((codePointSurrogate >> 6) & 0x3F));
                                utf8Bytes[utf8Index++] = (byte)(0x80 | (codePointSurrogate & 0x3F));

                                // Skip the low surrogate as it has already been processed
                                i++;
                                continue;
                            }
                        }

                        return utf8Index; // Soft exception: missing low surrogate

                    default:
                        // 3-byte UTF-8
                        if (utf8Index + 2 >= utf8Length)
                            return utf8Index; // Soft exception: buffer too small, return the number of bytes written so far

                        utf8Bytes[utf8Index++] = (byte)(0xE0 | (codePoint >> 12));
                        utf8Bytes[utf8Index++] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                        utf8Bytes[utf8Index++] = (byte)(0x80 | (codePoint & 0x3F));
                        break;
                }
            }

            return utf8Index; // Return the number of bytes written to the utf8Bytes buffer
        }
    }
}