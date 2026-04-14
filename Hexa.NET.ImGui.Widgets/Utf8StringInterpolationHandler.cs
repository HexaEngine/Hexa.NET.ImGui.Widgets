#if NET5_0_OR_GREATER

namespace Hexa.NET.ImGui.Widgets;

using Hexa.NET.Utilities.Text;
using System.Runtime.CompilerServices;

[InterpolatedStringHandler]
public unsafe ref struct Utf8StringInterpolationHandler
{
    private StrBuilder builder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8StringInterpolationHandler(int literalLength, int formattedCount)
    {
        builder = TextHelper.Builder;
    }

    public StrBuilder Builder => builder;

    public void AppendLiteral(string text)
    {
        builder.Append(text);
    }

    public void AppendLiteral(ReadOnlySpan<byte> text)
    {
        builder.Append(text);
    }

    public void AppendFormatted(int value)
    {
        builder.Append(value);
    }

    public void AppendFormatted(DateTime dateTime, string? format)
    {
        if (format != null)
        {
            builder.Append(dateTime, format);
        }
        else
        {
            builder.Append(dateTime);
        }
    }

    public void AppendFormatted(string value)
    {
        builder.Append(value);
    }

    public void AppendFormatted(char value)
    {
        builder.Append(value);
    }

    public void AppendFormatted(ReadOnlySpan<byte> value)
    {
        builder.Append(value);
    }

    public static implicit operator byte*(Utf8StringInterpolationHandler handler) => handler.builder;
}

#endif