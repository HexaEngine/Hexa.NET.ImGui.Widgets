using Kitty.UI.Dialogs;

namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    public readonly struct CompareByDateModifiedComparer : IComparer<FileSystemItem>
    {
        public int Compare(FileSystemItem a, FileSystemItem b)
        {
            int cmp = FileSystemItem.CompareByBase(a, b);
            if (cmp != 0)
            {
                return cmp;
            }
            cmp = a.DateModified.CompareTo(b.DateModified);
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }
    }
}