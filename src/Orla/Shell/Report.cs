using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Orla
{
    // Problems stay on this computer: unexpected errors go to orla.log in the data folder, and "Report a problem"
    // opens GitHub's form with the versions and the last error filled in, for people to review before sending.
    // Paths under the user profile are written as %USERPROFILE%, so a report does not carry the user name.
    public static class Report
    {
        const long MaxLog = 256 * 1024;

        static string LogPath => Path.Combine(Program.DataDirectory, "orla.log");

        public static void log(Exception e)
        {
            try
            {
                // One older file is kept, so the log never grows past twice this size.
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLog)
                {
                    File.Delete(LogPath + ".old");
                    File.Move(LogPath, LogPath + ".old");
                }
                File.AppendAllText(LogPath, privateless(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + Version + "\r\n" + e + "\r\n\r\n"));
            }
            catch (Exception)
            {
                // Logging must never become the problem.
            }
        }

        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static string issueUrl(string repository)
        {
            string url = repository + "/issues/new?template=bug.yml" +
                         "&windows=" + Uri.EscapeDataString(windows()) +
                         "&orla=" + Uri.EscapeDataString(Version) +
                         "&monitors=" + Uri.EscapeDataString(monitors());
            string last = lastError();
            return last == null ? url : url + "&extra=" + Uri.EscapeDataString(last);
        }

        public static string privateless(string text)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!String.IsNullOrEmpty(home))
                text = Regex.Replace(text, Regex.Escape(home), "%USERPROFILE%", RegexOptions.IgnoreCase);
            // Other spellings of a profile path, such as another drive or a short name.
            return Regex.Replace(text, @"\\Users\\[^\\\r\n]+", @"\Users\%USERNAME%", RegexOptions.IgnoreCase);
        }

        // Windows 11 still calls itself Windows 10 in the registry; the build number tells them apart.
        static string windows()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    int build = Int32.TryParse(key?.GetValue("CurrentBuild") as string, out int b) ? b : 0;
                    return (build >= 22000 ? "Windows 11 " : "Windows 10 ") + key?.GetValue("DisplayVersion") + " (" + build + "." + key?.GetValue("UBR") + ")";
                }
            }
            catch (Exception)
            {
                return Environment.OSVersion.VersionString;
            }
        }

        static string monitors() =>
            String.Join(", ", Screens.all().Select(s => s.Bounds.Width + "×" + s.Bounds.Height + " " + Math.Round(s.Scale * 100) + "%" +
                                                         (s.Primary ? " (" + Text.get("report.primary") + ")" : "")));

        // The first line of the newest entry in the log, if there is one.
        static string lastError()
        {
            try
            {
                if (!File.Exists(LogPath))
                    return null;
                string[] entries = File.ReadAllText(LogPath).Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string[] lines = entries.Last().Split(new[] { "\r\n" }, StringSplitOptions.None);
                return lines.Length > 1 ? Text.get("report.lastError") + " " + lines[0] + ": " + lines[1] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
