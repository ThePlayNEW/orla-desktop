using Xunit;

namespace Orla.Tests
{
    public class SearchTests
    {
        [Fact]
        public void namesThatStartWithTheTextComeFirstThenWordsThenAnywhere()
        {
            Assert.Equal(0, Organizer.rank(Organizer.plain("Steam"), "st"));
            Assert.Equal(1, Organizer.rank(Organizer.plain("Epic Games Launcher"), "ga"));
            Assert.Equal(2, Organizer.rank(Organizer.plain("Postman"), "st"));
            Assert.Equal(-1, Organizer.rank(Organizer.plain("Discord"), "st"));
            // Accents and case do not matter.
            Assert.Equal(0, Organizer.rank(Organizer.plain("Área de trabalho"), Organizer.plain("AREA")));
        }
    }
}
