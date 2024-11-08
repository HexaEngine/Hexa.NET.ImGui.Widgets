namespace Hexa.NET.ImGui.Widgets.Tests
{
    using Hexa.NET.ImGui.Widgets.Dialogs;

    [TestFixture]
    public class FileUtilitiesTests
    {
        [Test]
        [Platform(Include = "MacOsX", Reason = "This test is only applicable on macOS.")]
        public void EnumerateEntriesOSXTest()
        {
            // Arrange
            string testDirectory = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var entry in FileUtilities.EnumerateEntriesOSX(testDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var path = entry.Path.ToString();
                string fileName = Path.GetFileName(path);
                Assert.Multiple(() =>
                {
                    Assert.That(string.IsNullOrEmpty(path), Is.False, "Path should not be empty");
                    Assert.That(string.IsNullOrEmpty(fileName), Is.False, "File name should not be empty");
                });

                // Optional: Print the path and file name for verification
                Console.WriteLine($"Path: {entry.Path}, File Name: {fileName}");
            }
        }

        [Test]
        [Platform(Include = "Win", Reason = "This test is only applicable on Windows.")]
        public void EnumerateEntriesWinTest()
        {
            // Arrange
            string testDirectory = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var entry in FileUtilities.EnumerateEntriesWin(testDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var path = entry.Path.ToString();
                string fileName = Path.GetFileName(path);
                Assert.Multiple(() =>
                {
                    Assert.That(string.IsNullOrEmpty(path), Is.False, "Path should not be empty");
                    Assert.That(string.IsNullOrEmpty(fileName), Is.False, "File name should not be empty");
                });

                // Optional: Print the path and file name for verification
                Console.WriteLine($"Path: {entry.Path}, File Name: {fileName}");
            }
        }
    }
}