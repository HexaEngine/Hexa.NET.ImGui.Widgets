using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Hexa.NET.ImGui.Widgets.Text;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<FormatterUTF8IntBenchmark>();
    }
}

public class FormatterUTF8IntBenchmark
{
    private const int stackSize = 2048;
    private uint value = 0;
    public static string s;
    private Random random = new Random(1321);

    [Benchmark]
    public unsafe void Convert()
    {
        value = (uint)(random.Next() + int.MaxValue / 2);
        byte* stack = stackalloc byte[stackSize];
        Utf8Formatter.Format(value, stack, stackSize);

        stack[0] = (byte)'a';
    }

    [Benchmark(Baseline = true)]
    public unsafe void ConvertDefault()
    {
        value = (uint)(random.Next() + int.MaxValue / 2);
        s = value.ToString();
        fixed (char* ps = s)
        {
            ps[0] = 'a';
        }
    }
}

public class FormatterUTF8ConvBenchmark
{
    private const int stackSize = 2048;
    private const string str = "Hello World!";

    [Benchmark(Baseline = true)]
    public unsafe void ConvertDefault()
    {
        byte* stack = stackalloc byte[stackSize];
        fixed (char* c = str)
            Encoding.UTF8.GetBytes(c, str.Length, stack, stackSize);
    }

    [Benchmark]
    public unsafe void Convert()
    {
        byte* stack = stackalloc byte[stackSize];
        fixed (char* c = str)
            Utf8Formatter.ConvertUtf16ToUtf8(c, str.Length, stack, stackSize);
    }
}

public class FormatterBenchmark
{
    private const int stackSize = 2048;
    private string s;

    [Benchmark]
    public unsafe void FormatStandardDate()
    {
        byte* stack = stackalloc byte[stackSize];
        DateTime time = DateTime.Now;
        Utf8Formatter.Format(time, stack, stackSize, "yyyy-MM-dd HH:mm:ss");
    }

    [Benchmark]
    public unsafe void FormatComplexDate()
    {
        byte* stack = stackalloc byte[stackSize];
        DateTime time = DateTime.Now;
        Utf8Formatter.Format(time, stack, stackSize, "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
    }

    [Benchmark]
    public unsafe void FormatStandardDateDefault()
    {
        DateTime time = DateTime.Now;
        s = time.ToString("yyyy-MM-dd HH:mm:ss");
    }

    [Benchmark]
    public unsafe void FormatComplexDateDefault()
    {
        DateTime time = DateTime.Now;
        s = time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
    }
}