using Xunit;

namespace Orla.Tests
{
    public class ReportTests
    {
        [Fact]
        public void theReportOpensTheBugFormWithTheVersionsFilledIn()
        {
            string url = Report.issueUrl("https://github.com/owner/repo");
            Assert.StartsWith("https://github.com/owner/repo/issues/new?template=bug.yml&windows=Windows", url);
            Assert.Contains("&orla=" + Report.Version, url);
            Assert.Contains("&monitors=", url);
        }

        [Fact]
        public void theLogNeverCarriesTheUserName()
        {
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string text = Report.privateless(home.ToUpperInvariant() + @"\Desktop\a.txt and D:\Users\someone\b.txt");
            Assert.Equal(@"%USERPROFILE%\Desktop\a.txt and D:\Users\%USERNAME%\b.txt", text);
        }
    }
}
