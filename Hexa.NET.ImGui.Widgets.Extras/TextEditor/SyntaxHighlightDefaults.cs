namespace Hexa.NET.ImGui.Widgets.Extras.TextEditor
{
    using Hexa.NET.ImGui.Widgets.Extras.TextEditor.Highlight.CSharp;

    public static class SyntaxHighlightDefaults
    {
        static SyntaxHighlightDefaults()
        {
            CSharp = new CSharpSyntaxHighlight();
        }

        public static SyntaxHighlight CSharp { get; }
    }
}