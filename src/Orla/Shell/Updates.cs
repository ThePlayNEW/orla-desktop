using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;

namespace Orla
{
    // For the installed version: every few hours, asks GitHub Releases whether there is a newer version, downloads it
    // in the background and installs it when Orla closes. If Windows shuts down first, Velopack installs the downloaded
    // version at the next start, before Main runs. This is Orla's only network access, and it
    // sends nothing about the person or their files.
    public static class Updates
    {
        static readonly TimeSpan interval = TimeSpan.FromHours(6);
        static DispatcherTimer timer;
        static UpdateManager manager;
        static UpdateInfo pending;
        static bool checking;

        public static string Ready { get; private set; }

        // Safe to call more than once: there is a single timer, and each tick checks only when the interval has passed.
        public static void start(Controller controller)
        {
            if (timer == null)
            {
                timer = new DispatcherTimer(TimeSpan.FromHours(1), DispatcherPriority.Background, delegate { tick(controller); },
                                            controller.Dispatcher);
                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => controller.Dispatcher.BeginInvoke(new Action(() => tick(controller))));
            }
            timer.Start();
        }

        static void tick(Controller controller)
        {
            if (!controller.Layout.AutoUpdate || checking || pending != null)
                return;
            if (DateTime.TryParse(controller.Layout.LastUpdateCheck, null, System.Globalization.DateTimeStyles.RoundtripKind,
                                  out DateTime last) && DateTime.UtcNow - last < interval)
                return;
            checking = true;
            check(controller).ContinueWith(_ => controller.Dispatcher.BeginInvoke(new Action(() => checking = false)));
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
                if (info == null || !controller.Layout.AutoUpdate)
                    return;
                await m.DownloadUpdatesAsync(info);
                string version = info.TargetFullRelease.Version.ToString();
                await controller.Dispatcher.InvokeAsync(delegate {
                    manager = m;
                    pending = info;
                    Ready = version;
                    controller.notifyUpdate(version);
                });
            }
            catch (Exception)
            {
                // Offline or GitHub unavailable: try again on a later day.
            }
        }

        // Called when Orla quits: the downloaded version is installed after this process exits, without restarting.
        public static void applyOnExit()
        {
            if (manager != null && pending != null)
                manager.WaitExitThenApplyUpdates(pending.TargetFullRelease, true, false);
        }

        public static void restartNow(Controller controller)
        {
            if (manager == null || pending == null)
                return;
            controller.closeForUpdate();
            manager.ApplyUpdatesAndRestart(pending.TargetFullRelease);
        }
    }
}
