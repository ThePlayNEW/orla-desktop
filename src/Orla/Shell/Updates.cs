using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Orla
{
    // For the installed version: at most once a day, a minute after start, asks GitHub Releases whether there is a
    // newer version, downloads it in the background and applies it when Orla closes. This is Orla's only network
    // access, and it sends nothing about the person or their files.
    public static class Updates
    {
        public static string Ready { get; private set; }
        static UpdateManager manager;
        static UpdateInfo pending;

        public static void start(Controller controller)
        {
            if (DateTime.TryParse(controller.Layout.LastUpdateCheck, null, System.Globalization.DateTimeStyles.RoundtripKind,
                                  out DateTime last) && DateTime.UtcNow - last < TimeSpan.FromHours(20))
                return;
            Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => check(controller));
        }

        static async Task check(Controller controller)
        {
            try
            {
                var m = new UpdateManager(new GithubSource(CentralWindow.Repository, null, false));
                if (!m.IsInstalled || m.IsPortable)
                    return;
                UpdateInfo info = await m.CheckForUpdatesAsync();
                await controller.Dispatcher.InvokeAsync(delegate {
                    controller.Layout.LastUpdateCheck = DateTime.UtcNow.ToString("o");
                    controller.saveLayout();
                });
                if (info == null)
                    return;
                await m.DownloadUpdatesAsync(info);
                m.WaitExitThenApplyUpdates(info.TargetFullRelease, true, false);
                manager = m;
                pending = info;
                string version = info.TargetFullRelease.Version.ToString();
                await controller.Dispatcher.InvokeAsync(delegate {
                    Ready = version;
                    controller.notifyUpdate(version);
                });
            }
            catch (Exception)
            {
                // Offline or GitHub unavailable: try again another day.
            }
        }

        public static void restartNow()
        {
            if (manager != null && pending != null)
                manager.ApplyUpdatesAndRestart(pending.TargetFullRelease);
        }
    }
}
