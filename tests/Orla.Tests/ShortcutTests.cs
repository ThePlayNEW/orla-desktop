using System.Windows.Input;
using Xunit;

namespace Orla.Tests
{
    public class ShortcutTests
    {
        [Fact]
        public void readsAndWritesTheStoredForm()
        {
            Shortcut s = Shortcut.parse("Control+Shift+O");
            Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, s.Modifiers);
            Assert.Equal(Key.O, s.Key);
            Assert.Equal("Control+Shift+O", s.ToString());
        }

        [Fact]
        public void fallsBackToTheDefaultWhenUnusable()
        {
            Assert.Equal(Shortcut.Default, Shortcut.parse("Shift+O").ToString());
            Assert.Equal(Shortcut.Default, Shortcut.parse("nonsense").ToString());
            Assert.Equal(Shortcut.Default, Shortcut.parse(null).ToString());
        }
    }
}
