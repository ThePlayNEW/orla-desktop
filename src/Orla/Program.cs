using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Velopack;

namespace Orla
{
    public static class Program
    {
        // Shared with the 0.1 preview, so the two never run at the same time.
        const string MutexName = @"Local\OrlaDesktopOrganizer", SettingsEvent = @"Local\Orla.OpenSettings";

        public static string DataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orla");

        [STAThread]
        public static int Main(string[] args)
        {
            // Installer hooks run before anything else. Uninstalling removes the start-with-Windows entry; the icon
            // guard of a running copy brings the Windows icons back when the uninstaller closes it.
            VelopackApp.Build().OnBeforeUninstallFastCallback(v => Controller.removeStartup()).Run();
            string mode = args.Length > 0 ? args[0] : "";
            if (mode == "--guard" && args.Length >= 5)
            {
                IconGuard.guard(Int32.Parse(args[1]), Int64.Parse(args[2]), args[3] == "1", args[4]);
                return 0;
            }
            if (mode == "--sensors" && args.Length >= 2)
                return Diagnostics.sensors(args[1]);
            bool render = mode == "--render", smoke = mode == "--smoke";
            bool tool = render || smoke;

            Mutex mutex = null;
            EventWaitHandle openSettings = null;
            RegisteredWaitHandle registration = null;
            if (!tool)
            {
                mutex = new Mutex(true, MutexName, out bool created);
                if (!created)
                {
                    try
                    {
                        using (var signal = EventWaitHandle.OpenExisting(SettingsEvent))
                            signal.Set();
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                    }
                    mutex.Dispose();
                    return 0;
                }
                openSettings = new EventWaitHandle(false, EventResetMode.AutoReset, SettingsEvent);
            }

            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            Controller controller = null;
            try
            {
                Text.load("system");
                Theme.install(app);
                string dataDirectory = tool ? Path.Combine(Path.GetTempPath(), "Orla-" + Guid.NewGuid().ToString("N")) : DataDirectory;
                var store = new Store(Path.Combine(dataDirectory, "layout.json"));
                store.load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed.json"));
                Text.load(store.data.Language);
                Theme.apply(store.data);
                controller = new Controller(store, !tool);

                if (openSettings != null)
                    registration = ThreadPool.RegisterWaitForSingleObject(openSettings, delegate {
                        app.Dispatcher.BeginInvoke(new Action(() => controller.showCentral(null)));
                    }, null, Timeout.Infinite, false);
                app.DispatcherUnhandledException += (s, e) => {
                    controller.restoreIcons();
                    if (!tool)
                        Report.log(e.Exception);
                    if (tool)
                    {
                        File.WriteAllText(args[1] + ".error.txt", e.Exception.ToString());
                        controller.quit();
                    }
                    else
                        Dialog.alert(Text.get("error.title"), Text.format("error.unexpected", e.Exception.Message));
                    e.Handled = true;
                };
                app.Exit += delegate { controller.restoreIcons(); };

                if (render)
                    app.Dispatcher.BeginInvoke(new Action(() => Render.run(controller, args.Skip(1).ToArray())));
                else if (smoke)
                    Smoke.run(controller, args[1]);
                else
                {
                    controller.start();
                    if (args.Contains("--settings"))
                        controller.showCentral(null);
                }
                app.Run();
                return 0;
            }
            catch (Exception e)
            {
                controller?.restoreIcons();
                if (!tool)
                    Report.log(e);
                if (tool && args.Length > 1)
                    File.WriteAllText(args[1] + ".error.txt", e.ToString());
                else
                    MessageBox.Show(e.Message, "Orla Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            finally
            {
                registration?.Unregister(null);
                openSettings?.Dispose();
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
    }
}
