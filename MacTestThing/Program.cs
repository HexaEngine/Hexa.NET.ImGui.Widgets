// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui.Widgets.Dialogs;

foreach (var item in FileUtilities.EnumerateEntriesOSX(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.TopDirectoryOnly))
{
    Console.WriteLine(item);
}