namespace Hexa.NET.ImGui.Widgets.Text
{
    using Hexa.NET.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;

#if NET5_0_OR_GREATER
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
#endif

    /// <summary>
    /// Will be moved to Hexa.NET.Utilities later.
    /// </summary>
    public static class Utf8Formatter
    {
#if NET5_0_OR_GREATER
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
#endif

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

        public static unsafe int Format(float value, byte* buffer, int bufSize, int digits = -1)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture, digits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(float value, byte* buffer, int bufSize, CultureInfo cultureInfo, int digits = -1)
        {
            var format = cultureInfo.NumberFormat;
            var start = buffer;
            var end = buffer + bufSize;
            if (float.IsNaN(value))
            {
                buffer += ConvertUtf16ToUtf8(format.NaNSymbol, buffer, bufSize);
                goto end;
            }
            if (float.IsPositiveInfinity(value))
            {
                buffer += ConvertUtf16ToUtf8(format.PositiveInfinitySymbol, buffer, bufSize);
                goto end;
            }
            if (float.IsNegativeInfinity(value))
            {
                buffer += ConvertUtf16ToUtf8(format.NegativeInfinitySymbol, buffer, bufSize);
                goto end;
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

            int number = (int)value;
            double fraction = value - number;

            if (fraction < 0)
            {
                fraction = -fraction;
            }

            buffer += Format(number, buffer, bufSize, cultureInfo);

            if (buffer + 1 == end)
            {
                return (int)(buffer - start);
            }

            buffer += ConvertUtf16ToUtf8(format.CurrencyDecimalSeparator, buffer, (int)(end - buffer));

            digits = digits >= 0 ? digits : 7;

            for (int j = 0; buffer != end && j < digits; j++)
            {
                fraction *= 10;
                int fractionalDigit = (int)fraction;
                *buffer++ = (byte)('0' + fractionalDigit);
                fraction -= fractionalDigit;

                if (fraction < 1e-14) break;
            }

            while (*(buffer - 1) == '0' || *(buffer - 1) == '.')
            {
                buffer--;
            }

        end:
            *buffer = 0;
            return (int)(buffer - start);
        }

        public static unsafe int Format(double value, byte* buffer, int bufSize, int digits = -1)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture, digits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(double value, byte* buffer, int bufSize, CultureInfo cultureInfo, int digits = -1)
        {
            var format = cultureInfo.NumberFormat;

            var start = buffer;
            var end = buffer + bufSize;
            if (double.IsNaN(value))
            {
                buffer += ConvertUtf16ToUtf8(format.NaNSymbol, buffer, bufSize);
                goto end;
            }
            if (double.IsPositiveInfinity(value))
            {
                buffer += ConvertUtf16ToUtf8(format.PositiveInfinitySymbol, buffer, bufSize);
                goto end;
            }
            if (double.IsNegativeInfinity(value))
            {
                buffer += ConvertUtf16ToUtf8(format.NegativeInfinitySymbol, buffer, bufSize);
                goto end;
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

            buffer += Format(number, buffer, bufSize, cultureInfo);

            if (buffer == end)
            {
                return (int)(buffer - start);
            }

            buffer += ConvertUtf16ToUtf8(format.CurrencyDecimalSeparator, buffer, (int)(end - buffer));

            digits = digits >= 0 ? digits : 7;

            for (int j = 0; buffer != end && j < digits; j++)
            {
                fraction *= 10;
                int fractionalDigit = (int)fraction;
                *buffer++ = (byte)('0' + fractionalDigit);
                fraction -= fractionalDigit;

                if (fraction < 1e-14) break;
            }

            while (*(buffer - 1) == '0' || *(buffer - 1) == '.')
            {
                buffer--;
            }

        end:
            *buffer = 0;
            return (int)(buffer - start);
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

        public static unsafe int Format(sbyte value, byte* buffer, int bufSize)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(sbyte value, byte* buffer, int bufSize, CultureInfo cultureInfo)
        {
            var format = cultureInfo.NumberFormat;

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

            EncodeNegativeSign(&buffer, &bufSize, negative, format);

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

        public static unsafe int Format(short value, byte* buffer, int bufSize)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(short value, byte* buffer, int bufSize, CultureInfo cultureInfo)
        {
            var format = cultureInfo.NumberFormat;

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

            EncodeNegativeSign(&buffer, &bufSize, negative, format);

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

        public static unsafe int Format(int value, byte* buffer, int bufSize)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(int value, byte* buffer, int bufSize, CultureInfo cultureInfo)
        {
            var format = cultureInfo.NumberFormat;

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

            if (negative)
            {
                int size = ConvertUtf16ToUtf8(format.NegativeSign, buffer, bufSize);
                bufSize -= size; // Reserve space for the negative sign
                buffer += size; // Move the buffer pointer to the right
            }

            return Format(abs, buffer, bufSize) + (negative ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(uint value, byte* buffer, in int bufSize)
        {
            byte* start = buffer;
            byte* end = buffer + bufSize - 1;

            // Quickly estimate number of digits based on the position of the MSB
            int estimatedDigits = (value < 10) ? 1 :
                                  (value < 100) ? 2 :
                                  (value < 1000) ? 3 :
                                  (value < 10000) ? 4 :
                                  (value < 100000) ? 5 :
                                  (value < 1000000) ? 6 :
                                  (value < 10000000) ? 7 :
                                  (value < 100000000) ? 8 :
                                  (value < 1000000000) ? 9 : 10;

            // Position buffer at the end, to back-fill
            buffer += estimatedDigits;

            if (buffer > end)
                buffer = end;

            *buffer = 0; // Null-terminate

            for (int i = 0; i < estimatedDigits; i++)
            {
                uint oldValue = value;
                value /= 10;
                uint mod = oldValue - value * 10;
                *--buffer = (byte)('0' + mod);
            }

            return estimatedDigits;

            /*
            byte* end = buffer + bufSize - 1;
            byte* start = buffer;

            while (value > 0)
            {
                uint oldValue = value;
                value /= 10; // Exact value / 10
                uint mod = oldValue - value * 10; // Calculate value % 10, avoids additional div. (div is slow on CPU compared to v - v1 * 10)
                *buffer++ = (byte)('0' + mod);
                if (buffer == end) // Break early if we run out of space
                    break;
            }

            //*buffer = 0; // Null-terminate

            int len = (int)(buffer - start);

            // Reverse the digits for correct order
            for (byte* left = start, right = buffer - 1; left < right; left++, right--)
            {
                byte tmp = *left;
                *left = *right;
                *right = tmp;
            }

            return len;*/
        }

        public static unsafe int Format(long value, byte* buffer, int bufSize)
        {
            return Format(value, buffer, bufSize, CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Format(long value, byte* buffer, int bufSize, CultureInfo cultureInfo)
        {
            var format = cultureInfo.NumberFormat;

            bool negative = value < 0;
            if (!negative && bufSize < 2 || negative && bufSize < 3)
            {
                if (bufSize > 1)
                {
                    buffer[0] = 0; // Null-terminate
                }

                return 0;  // Buffer too small to hold even "0\0" or "-0\0"
            }

            EncodeNegativeSign(&buffer, &bufSize, negative, format);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void EncodeNegativeSign(byte** buffer, int* bufSize, bool negative, NumberFormatInfo format)
        {
            if (*bufSize == 0)
            {
                return;
            }

            if (negative)
            {
                int size = ConvertUtf16ToUtf8(format.NegativeSign, *buffer, *bufSize);
                *bufSize -= size; // Reserve space for the negative sign
                *buffer += size; // Move the buffer pointer to the right
            }
        }

        public static unsafe int EncodeUnicodeChar(char c, byte* buf, int bufSize)
        {
            return Encoding.UTF8.GetBytes(&c, 1, buf, bufSize);
        }

        public static unsafe int ConvertUtf16ToUtf8(char c, byte* utf8Bytes, int utf8Length)
        {
            return ConvertUtf16ToUtf8(&c, 1, utf8Bytes, utf8Length);
        }

        public static unsafe int ConvertUtf16ToUtf8(string str, int offset, int length, byte* utf8Bytes, int utf8Length)
        {
            fixed (char* pStr = str)
            {
                return ConvertUtf16ToUtf8(pStr + offset, length, utf8Bytes, utf8Length);
            }
        }

        public static unsafe int ConvertUtf16ToUtf8(ReadOnlySpan<char> span, byte* utf8Bytes, int utf8Length)
        {
            fixed (char* pStr = span)
            {
                return ConvertUtf16ToUtf8(pStr, span.Length, utf8Bytes, utf8Length);
            }
        }

        public static unsafe int ConvertUtf16ToUtf8(string str, byte* utf8Bytes, int utf8Length)
        {
            return ConvertUtf16ToUtf8(str, 0, str.Length, utf8Bytes, utf8Length);
        }

        public static unsafe int ConvertUtf16ToUtf8(string str, int offset, byte* utf8Bytes, int utf8Length)
        {
            return ConvertUtf16ToUtf8(str, offset, str.Length - offset, utf8Bytes, utf8Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ConvertUtf16ToUtf8(char* utf16Chars, int utf16Length, byte* utf8Bytes, int utf8Length)
        {
            byte* start = utf8Bytes;
            byte* utf8BytesEnd = utf8Bytes + utf8Length;
            for (int i = 0; i < utf16Length; i++)
            {
                if (utf8Bytes >= utf8BytesEnd)
                    return (int)(utf8Bytes - start);

                char utf16Char = utf16Chars[i];

                int codePoint = utf16Char;

                switch (codePoint)
                {
                    case <= 0x7F:
                        *utf8Bytes = (byte)codePoint;
                        utf8Bytes++;
                        break;

                    case <= 0x7FF:
                        // 2-byte UTF-8
                        if (utf8Bytes + 1 >= utf8BytesEnd)
                            return (int)(utf8Bytes - start);

                        *utf8Bytes = (byte)(0xC0 | (codePoint >> 6));
                        utf8Bytes++;
                        *utf8Bytes = (byte)(0x80 | (codePoint & 0x3F));
                        utf8Bytes++;
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
                                if (utf8Bytes + 3 >= utf8BytesEnd)
                                    return (int)(utf8Bytes - start);

                                *utf8Bytes = (byte)(0xF0 | (codePointSurrogate >> 18));
                                utf8Bytes++;
                                *utf8Bytes = (byte)(0x80 | ((codePointSurrogate >> 12) & 0x3F));
                                utf8Bytes++;
                                *utf8Bytes = (byte)(0x80 | ((codePointSurrogate >> 6) & 0x3F));
                                utf8Bytes++;
                                *utf8Bytes = (byte)(0x80 | (codePointSurrogate & 0x3F));
                                utf8Bytes++;

                                // Skip the low surrogate as it has already been processed
                                i++;
                                continue;
                            }
                        }

                        return (int)(utf8Bytes - start);

                    default:
                        // 3-byte UTF-8
                        if (utf8Bytes + 2 >= utf8BytesEnd)
                            return (int)(utf8Bytes - start);

                        *utf8Bytes = (byte)(0xE0 | (codePoint >> 12));
                        utf8Bytes++;
                        *utf8Bytes = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                        utf8Bytes++;
                        *utf8Bytes = (byte)(0x80 | (codePoint & 0x3F));
                        utf8Bytes++;
                        break;
                }
            }

            return (int)(utf8Bytes - start);
        }

        public static string DefaultPattern { get; } = CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns('G')[0];

        public static unsafe int Format(DateTime dateTime, Span<byte> buf)
        {
            fixed (byte* pBuf = buf)
                return Format(dateTime, pBuf, buf.Length, DefaultPattern, CultureInfo.CurrentCulture);
        }

        public static unsafe int Format(DateTime dateTime, Span<byte> buf, string format)
        {
            fixed (byte* pBuf = buf)
                return Format(dateTime, pBuf, buf.Length, format, CultureInfo.CurrentCulture);
        }

        public static unsafe int Format(DateTime dateTime, Span<byte> buf, string format, CultureInfo cultureInfo)
        {
            fixed (byte* pBuf = buf)
                return Format(dateTime, pBuf, buf.Length, format, cultureInfo);
        }

        public static unsafe int Format(DateTime dateTime, byte* buf, int bufSize)
        {
            return Format(dateTime, buf, bufSize, DefaultPattern, CultureInfo.CurrentCulture);
        }

        public static unsafe int Format(DateTime dateTime, byte* buf, int bufSize, string format)
        {
            return Format(dateTime, buf, bufSize, format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats a <see cref="DateTime"/> to it's
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="buf"></param>
        /// <param name="bufSize"></param>
        /// <param name="format"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static unsafe int Format(DateTime dateTime, byte* buf, int bufSize, string format, CultureInfo cultureInfo)
        {
            if (bufSize == 0)
            {
                return 0;
            }

            if (bufSize == 1)
            {
                *buf = 0;
                return 0;
            }

            var dateFormat = cultureInfo.DateTimeFormat;
            int idx = 0;
            const int maxPrecision = 7;

            fixed (char* pFixedFormat = format)
            {
                char* pFormat = pFixedFormat;
                char* formatEnd = pFormat + format.Length;
                while (pFormat != formatEnd)
                {
                    if (idx >= bufSize) break;
                    char c = pFormat[0];
                    pFormat++;

                    switch (c)
                    {
                        case 'd':
                            int dCount = CountAhead(&pFormat, formatEnd, 'd', 3) + 1;
                            int day = dateTime.Day;
                            switch (dCount)
                            {
                                case 1:
                                    if (day >= 10) goto case 2;
                                    if (idx + 1 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + day % 10);
                                    break;

                                case 2:
                                    if (idx + 2 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + day / 10);
                                    buf[idx++] = (byte)('0' + day % 10);
                                    break;

                                case 3:
                                    {
                                        var dayString = dateFormat.GetAbbreviatedDayName(dateTime.DayOfWeek);
                                        idx += ConvertUtf16ToUtf8(dayString, buf + idx, bufSize - idx);
                                    }
                                    break;

                                case 4:
                                    {
                                        var dayString = dateFormat.GetDayName(dateTime.DayOfWeek);
                                        idx += ConvertUtf16ToUtf8(dayString, buf + idx, bufSize - idx);
                                    }
                                    break;
                            }

                            break;

                        case 'f':

                            int fCount = CountAhead(&pFormat, formatEnd, 'f', 6) + 1;
                            if (idx + fCount > bufSize) goto end;
                            Format(dateTime, buf, &idx, maxPrecision, fCount, false);
                            break;

                        case 'F':
                            int FCount = CountAhead(&pFormat, formatEnd, 'F', 6) + 1;
                            if (idx + FCount > bufSize) goto end;
                            Format(dateTime, buf, &idx, maxPrecision, FCount, true);
                            break;

                        case 'M':
                            int MCount = CountAhead(&pFormat, formatEnd, 'M', 3) + 1;
                            int month = dateTime.Month;
                            switch (MCount)
                            {
                                case 1:
                                    if (month >= 10) goto case 2;
                                    if (idx + 1 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + dateTime.Month % 10);
                                    break;

                                case 2:
                                    if (idx + 2 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + dateTime.Month / 10);
                                    buf[idx++] = (byte)('0' + dateTime.Month % 10);
                                    break;

                                case 3:
                                    {
                                        var monthString = dateFormat.GetAbbreviatedMonthName(month);
                                        idx += ConvertUtf16ToUtf8(monthString, buf + idx, bufSize - idx);
                                    }
                                    break;

                                case 4:
                                    {
                                        var monthString = dateFormat.GetMonthName(month);
                                        idx += ConvertUtf16ToUtf8(monthString, buf + idx, bufSize - idx);
                                    }
                                    break;
                            }
                            break;

                        case 'y':
                            int yCount = CountAhead(&pFormat, formatEnd, 'y', 4) + 1;
                            int year = dateTime.Year;
                            switch (yCount)
                            {
                                case 1:
                                    if (year >= 10) goto case 2;
                                    if (idx + 1 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + year % 10);
                                    break;

                                case 2:
                                    if (idx + 2 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + (year / 10 % 10));
                                    buf[idx++] = (byte)('0' + year % 10);
                                    break;

                                case 3:
                                    if (year >= 1000) goto case 4;
                                    if (idx + 3 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + (year / 100 % 10));
                                    buf[idx++] = (byte)('0' + (year / 10 % 10));
                                    buf[idx++] = (byte)('0' + year % 10);
                                    break;

                                case 4:
                                    if (idx + 4 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + (year / 1000) % 10);
                                    buf[idx++] = (byte)('0' + (year / 100 % 10));
                                    buf[idx++] = (byte)('0' + (year / 10 % 10));
                                    buf[idx++] = (byte)('0' + year % 10);
                                    break;

                                case 5:
                                    if (idx + 5 > bufSize) goto end;
                                    buf[idx++] = (byte)('0' + (year / 10000) % 10);
                                    buf[idx++] = (byte)('0' + (year / 1000) % 10);
                                    buf[idx++] = (byte)('0' + (year / 100 % 10));
                                    buf[idx++] = (byte)('0' + (year / 10 % 10));
                                    buf[idx++] = (byte)('0' + year % 10);
                                    break;
                            }
                            break;

                        case 'H':
                            {
                                int HCount = CountAhead(&pFormat, formatEnd, 'H', 1) + 1;
                                int hour = dateTime.Hour;
                                switch (HCount)
                                {
                                    case 1:
                                        if (hour >= 10) goto case 2;
                                        if (idx + 1 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hour % 10);
                                        break;

                                    case 2:
                                        if (idx + 2 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hour / 10);
                                        buf[idx++] = (byte)('0' + hour % 10);
                                        break;
                                }
                            }
                            break;

                        case 'h':
                            {
                                int hCount = CountAhead(&pFormat, formatEnd, 'h', 1) + 1;
                                int hour = dateTime.Hour;
                                if (hour == 0) hour = 12;
                                else if (hour > 12) hour -= 12;
                                switch (hCount)
                                {
                                    case 1:
                                        if (hour >= 10) goto case 2;
                                        if (idx + 1 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hour % 10);
                                        break;

                                    case 2:
                                        if (idx + 2 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hour / 10);
                                        buf[idx++] = (byte)('0' + hour % 10);
                                        break;
                                }
                            }
                            break;

                        case 't':
                            {
                                int hour = dateTime.Hour;
                                string designator = hour >= 12 ? dateFormat.PMDesignator : dateFormat.AMDesignator;
                                bool full = pFormat != formatEnd && *pFormat == 't';
                                if (full)
                                {
                                    pFormat++;
                                }

                                if (string.IsNullOrEmpty(designator))
                                {
                                    break;
                                }

                                idx += ConvertUtf16ToUtf8(designator, 0, full ? designator.Length : 1, buf + idx, bufSize - idx);
                            }
                            break;

                        case 'm':
                            {
                                int mCount = CountAhead(&pFormat, formatEnd, 'm', 1) + 1;
                                int minute = dateTime.Minute;
                                switch (mCount)
                                {
                                    case 1:
                                        if (minute >= 10) goto case 2;
                                        if (idx + 1 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + minute % 10);
                                        break;

                                    case 2:
                                        if (idx + 2 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + minute / 10);
                                        buf[idx++] = (byte)('0' + minute % 10);
                                        break;
                                }
                            }
                            break;

                        case 's':
                            {
                                int sCount = CountAhead(&pFormat, formatEnd, 's', 1) + 1;
                                int second = dateTime.Second;
                                switch (sCount)
                                {
                                    case 1:
                                        if (second >= 10) goto case 2;
                                        if (idx + 1 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + second % 10);
                                        break;

                                    case 2:
                                        if (idx + 2 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + second / 10);
                                        buf[idx++] = (byte)('0' + second % 10);
                                        break;
                                }
                            }
                            break;

                        case 'g':
                            {
                                int gCount = CountAhead(&pFormat, formatEnd, 'g', 1) + 1;
                                int era = dateFormat.Calendar.GetEra(dateTime);
                                string eraString = dateFormat.GetEraName(era);
                                idx += ConvertUtf16ToUtf8(eraString, buf + idx, bufSize - idx);
                            }
                            break;

                        case 'z':
                            {
                                int zCount = CountAhead(&pFormat, formatEnd, 'z', 2) + 1;
                                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
                                if (idx + 1 > bufSize) goto end;
                                buf[idx++] = (byte)(offset.Ticks >= 0 ? '+' : '-');
                                int hours = Math.Abs(offset.Hours);
                                int minutes = Math.Abs(offset.Minutes);
                                switch (zCount)
                                {
                                    case 1:
                                        if (idx + 1 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hours);
                                        break;

                                    case 2:
                                        if (idx + 2 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hours / 10);
                                        buf[idx++] = (byte)('0' + hours % 10);
                                        break;

                                    case 3:
                                        if (idx + 5 > bufSize) goto end;
                                        buf[idx++] = (byte)('0' + hours / 10);
                                        buf[idx++] = (byte)('0' + hours % 10);
                                        buf[idx++] = (byte)':';
                                        buf[idx++] = (byte)('0' + minutes / 10);
                                        buf[idx++] = (byte)('0' + minutes % 10);
                                        break;
                                }
                            }
                            break;

                        case 'K':
                            {
                                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
                                if (dateTime.Kind == DateTimeKind.Utc)
                                {
                                    buf[idx++] = (byte)'Z';
                                }
                                else
                                {
                                    buf[idx++] = (byte)(offset.Ticks >= 0 ? '+' : '-');

                                    int hours = Math.Abs(offset.Hours);
                                    int minutes = Math.Abs(offset.Minutes);

                                    buf[idx++] = (byte)('0' + hours / 10);
                                    buf[idx++] = (byte)('0' + hours % 10);
                                    buf[idx++] = (byte)':';
                                    buf[idx++] = (byte)('0' + minutes / 10);
                                    buf[idx++] = (byte)('0' + minutes % 10);
                                }
                            }
                            break;

                        case ':':
                            idx += ConvertUtf16ToUtf8(dateFormat.TimeSeparator, buf + idx, bufSize - idx);
                            break;

                        case '/':
                            idx += ConvertUtf16ToUtf8(dateFormat.DateSeparator, buf + idx, bufSize - idx);
                            break;

                        case '\'':
                            {
                                int end = IndexOf(pFormat, formatEnd, '\'');
                                if (end == -1) goto default;
                                idx += ConvertUtf16ToUtf8(pFormat, end, buf + idx, bufSize - idx);
                                pFormat += end + 1;
                            }
                            break;

                        case '"':
                            {
                                int end = IndexOf(pFormat, formatEnd, '"');
                                if (end == -1) goto default;
                                idx += ConvertUtf16ToUtf8(pFormat, end, buf + idx, bufSize - idx);
                                pFormat += end + 1;
                            }
                            break;

                        case '\\':
                            if (pFormat != formatEnd) goto end;
                            c = *pFormat;
                            pFormat++;
                            goto default;

                        default: // literal
                            if (idx + 1 > bufSize) goto end;
                            idx += ConvertUtf16ToUtf8(c, buf + idx, bufSize - idx);
                            break;
                    }
                }
            }

        end:

            if (idx >= bufSize)
            {
                buf[bufSize - 1] = 0;
                idx = bufSize - 1;
            }
            else
            {
                buf[idx] = 0;
            }

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static int IndexOf(char* str, char* strEnd, char target)
        {
            char* start = str;
            while (str != strEnd && *str != target)
            {
                str++;
            }

            if (str == strEnd)
            {
                return -1;
            }
            else
            {
                return (int)(str - start);
            }
        }

        private static unsafe void Format(DateTime dateTime, byte* buf, int* idx, int maxPrecision, int precision, bool removeTail)
        {
            int milliseconds = dateTime.Millisecond;
#if NET6_0_OR_GREATER
            int microseconds = dateTime.Microsecond;
            int nanoseconds = dateTime.Nanosecond;
#else
            int microseconds = (int)((dateTime.Ticks % TimeSpan.TicksPerSecond) / 10);
            int nanoseconds = (int)((dateTime.Ticks % TimeSpan.TicksPerSecond) * 100);
#endif

            ulong value = (ulong)milliseconds * 1_000_000 + (ulong)microseconds * 1_000 + (ulong)nanoseconds;

            ulong divisor = (ulong)Math.Pow(10, maxPrecision + 1);

            int ix = *idx;
            for (int i = 0; i < precision; i++)
            {
                byte digit = (byte)(value / divisor % 10);
                buf[ix++] = (byte)('0' + digit);
                divisor /= 10;
            }

            if (removeTail)
            {
                while (buf[ix] == '0')
                {
                    buf[ix] = 0;
                    ix--;
                }
            }

            *idx = ix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static int CountAhead(char** format, char* formatEnd, char target, int max)
        {
            int count = 0;
            char* pChar = *format;

            while (pChar != formatEnd && *pChar == target && count < max)
            {
                count++;
                pChar++;
            }
            *format = pChar;

            return count;
        }
    }
}