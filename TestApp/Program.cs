namespace TestApp
{
    using Hexa.NET.ImGui.Widgets.Text;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection.Emit;
    using System.Reflection;
    using System.Diagnostics;

    unsafe internal class Program
    {
        private static void Main(string[] args)
        {
            const int bufSize = 256;
            byte* buf = stackalloc byte[bufSize];

            const int warmup = 1000000;
            for (int i = 0; i < warmup; i++)
            {
                Printf(buf, 256, "%d Hello world %f.3 %u", __arglist(213, 3.14f, 93123u));
            }

            for (int i = 0; i < 16; i++)
            {
                int gen0 = GC.CollectionCount(0);
                int gen1 = GC.CollectionCount(1);
                int gen2 = GC.CollectionCount(2);

                const int iterations = 1000000;

                long start = Stopwatch.GetTimestamp();

                for (int j = 0; j < iterations; j++)
                {
                    Printf(buf, 256, "%d Hello world %f.3 %u", __arglist(213, 3.14f, 93123u));
                }

                long end = Stopwatch.GetTimestamp();

                int gen0End = GC.CollectionCount(0);
                int gen1End = GC.CollectionCount(1);
                int gen2End = GC.CollectionCount(2);

                double seconds = (end - start) / (double)Stopwatch.Frequency;
                double milliseconds = seconds * 1000.0;
                double microseconds = milliseconds * 1000.0;
                double nanoseconds = microseconds * 1000.0;

                Console.WriteLine($"Iterations: {iterations}");
                Console.WriteLine($"Time: {seconds} s");

                // throughput
                Console.WriteLine($"Throughput: {iterations / seconds} calls/s");

                Console.WriteLine($"Average time per call: {(nanoseconds / (iterations))} ns");

                Console.WriteLine($"Gen0: {gen0End - gen0}");
                Console.WriteLine($"Gen1: {gen1End - gen1}");
                Console.WriteLine($"Gen2: {gen2End - gen2}");
            }
        }

        // Move to Hexa.NET.Utilities later

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
    }

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
        public unsafe static bool Is<T>(this TypedReference reference, [MaybeNullWhen(false)] out T t)
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
}