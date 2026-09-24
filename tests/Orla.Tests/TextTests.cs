using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Orla.Tests
{
    public class TextTests
    {
        [Fact]
        public void everyLanguageHasTheSameKeys()
        {
            var english = Text.read("en").Keys.OrderBy(k => k).ToList();
            foreach (string language in Text.Languages)
                Assert.Equal(english, Text.read(language).Keys.OrderBy(k => k).ToList());
        }

        [Fact]
        public void everyKeyUsedInTheSourceExists()
        {
            var keys = Text.read("en").Keys.ToList();
            string source = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Orla"));
            var pattern = new Regex(@"Text\.(?:get|format)\(""([\w.]+)""|\{l:T ([\w.]+)\}|Menus\.(?:item|check)\(""([\w.]+)""");
            var used = Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
                           .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
                           .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                           .SelectMany(f => pattern.Matches(File.ReadAllText(f)).Cast<Match>())
                           .Select(m => m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value)
                           .Distinct();
            Assert.Equal(new string[0], used.Where(k => !k.EndsWith(".")).Except(keys).ToArray());
        }
    }
}
