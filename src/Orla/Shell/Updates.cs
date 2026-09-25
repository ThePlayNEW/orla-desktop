using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;

namespace Orla
{
    // For the installed and the portable version: every few hours, and whenever people ask, looks on GitHub Releases
    // for a newer version, downloads it in the background and installs it when Orla closes. If Windows shuts down first,
    // Velopack installs the downloaded version at the next start, before Main runs. This is Orla's only network access,
    // and it sends nothing about the person or their files.
    public static class Updates
    {
        public enum State { Idle, Checking, UpToDate, Ready, Failed, NotInstalled }

        static readonly TimeSpan interval = TimeSpan.FromHours(6);
        static DispatcherTimer timer;
        static UpdateManager manager;
        static UpdateInfo pending;

        public static State Status { get; private set; }
        public static string Ready { get; private set; }

        // Raised on the UI thread whenever Status changes.
        public static event Action Changed;

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
            if (!controller.Layout.AutoUpdate)
                return;
            if (DateTime.TryParse(controller.Layout.LastUpdateCheck, null, System.Globalization.DateTimeStyles.RoundtripKind,
                                  out DateTime last) && DateTime.UtcNow - last < interval)
                return;
            checkNow(controller);
        }

        // Also the "Check now" button: it runs even with automatic updates off, because people asked.
        public static void checkNow(Controller controller)
        {
            if (Status == State.Checking || Status == State.Ready)
                return;
            set(State.Checking);
            check(controller).ContinueWith(t => controller.Dispatcher.BeginInvoke(new Action(() => set(t.Result))));
        }

        static void set(State state)
        {
            Status = state;
            Changed?.Invoke();
        }

        static async Task<State> check(Controller controller)
        {
            try
            {
                var m = new UpdateManager(new GithubSource(CentralWindow.Repository, null, false));
                // A copy built from the source has nothing to update from.
                if (!m.IsInstalled)
                    return State.NotInstalled;
                UpdateInfo info = await m.CheckForUpdatesAsync();
                await controller.Dispatcher.InvokeAsync(delegate {
                    controller.Layout.LastUpdateCheck = DateTime.UtcNow.ToString("o");
                    controller.saveLayout();
                });
                if (info == null)
                    return State.UpToDate;
                await m.DownloadUpdatesAsync(info);
                string version = info.TargetFullRelease.Version.ToString();
                await controller.Dispatcher.InvokeAsync(delegate {
                    manager = m;
                    pending = info;
                    Ready = version;
                    controller.notifyUpdate(version);
                });
                return State.Ready;
            }
            catch (Exception)
            {
                // Offline, GitHub unavailable, or a portable copy in a folder it cannot write to: try again later.
                return State.Failed;
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
