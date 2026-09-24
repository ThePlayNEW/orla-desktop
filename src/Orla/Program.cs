using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Orla
{
    public static class Program
    {
        static Mutex mutex;
        static EventWaitHandle openSettings;
        static RegisteredWaitHandle settingsRegistration;
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--guard")
            {
                Native.guard(Int32.Parse(args[1]), Int64.Parse(args[2]), args[3] == "1", args[4]);
                return 0;
            }
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (args.Length > 0 && args[0] == "--inspect")
            {
                IntPtr h = Native.desktopList();
                File.WriteAllText(args[1],
                                  Store.json().Serialize(new { Found = h != IntPtr.Zero,
                                                               IconsVisible = Native.IsWindowVisible(h) }));
                return 0;
            }
            bool render = args.Length > 0 && args[0] == "--render";
            bool smoke = args.Length > 0 && args[0] == "--smoke";
            bool crash = args.Length > 0 && args[0] == "--crash-test";
            bool created = true;
            if (!render && !smoke && !crash)
            {
                mutex = new Mutex(true, "Local\\OrlaDesktopOrganizer", out created);
                if (!created)
                {
                    try
                    {
                        using (var signal = EventWaitHandle.OpenExisting("Local\\Orla.OpenSettings"))
                            signal.Set();
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                        MessageBox.Show("Orla está iniciando. Tente novamente em um instante.", "Orla");
                    }
                    mutex.Dispose();
                    return 0;
                }
                openSettings =
                    new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Orla.OpenSettings");
            }
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            UI.theme();
            Controller controller = null;
            try
            {
                string dataDir =
                    (render || smoke || crash)
                        ? Path.Combine(Path.GetTempPath(), "Orla-test-" + Guid.NewGuid().ToString("N"))
                        : Path.Combine(
                              Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                              "Orla");
                var store = new Store(Path.Combine(dataDir, "layout.json"));
                store.load(Path.Combine(dir, "seed.json"));
                if (render && args.Contains("--demo"))
                    store.data = Demo.create();
                controller = new Controller(store, !render);

                if (openSettings != null)
                    settingsRegistration = ThreadPool.RegisterWaitForSingleObject(openSettings, delegate {
                        app.Dispatcher.BeginInvoke(new Action(delegate { controller.showManager(null); }));
                    }, null, Timeout.Infinite, false);
                if (!render && !smoke && !crash && !store.data.StartupConfigured)
                    controller.tryAction(delegate { controller.setStartup(true); });
                app.DispatcherUnhandledException +=
                    delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    controller.disableDesktop();
                    MessageBox.Show("Os ícones normais foram restaurados.\n" + e.Exception.Message, "Orla");
                    e.Handled = true;
                };
                app.Exit += delegate
                {
                    controller.disableDesktop();
                };
                bool desktopMode = !render && !smoke && !crash && !args.Contains("--settings");
                if (!desktopMode && !crash)
                {
                    controller.manager = new ManagerWindow(controller);
                    controller.manager.Show();
                }
                if (store.recoveryNotice != null)
                    controller.notify(store.recoveryNotice);
                if (desktopMode)
                    controller.tryAction(delegate {
                        controller.enableDesktop();
                        controller.notify("Painéis ativos. Use a bandeja para organizar os grupos ou " +
                                          "restaurar os ícones.");
                    });
                if (crash)
                {
                    controller.enableDesktop();
                    File.WriteAllText(
                        args[1], Store.json().Serialize(
                                     new { Pid = Process.GetCurrentProcess().Id,
                                           IconsHidden = !Native.IsWindowVisible(Native.desktopList()) }));
                }
                if (render)
                {
                    controller.manager.ContentRendered += delegate
                    {
                        app.Dispatcher.BeginInvoke(
                            new Action(delegate {
                                renderWindow(controller.manager, args[1]);
                                Group g = store.data.Groups.First(x => x.Visible);
                                var panel = new PanelWindow(controller, g);
                                panel.Show();
                                panel.UpdateLayout();
                                renderWindow(panel,
                                             Path.Combine(Path.GetDirectoryName(args[1]), "painel.png"));
                                panel.Close();
                                controller.quit();
                            }),
                            DispatcherPriority.ApplicationIdle);
                    };
                }
                if (smoke)
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    int step = 0;
                    bool before = false, hidden = false, restored = false, above = false;
                    double start = Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
                    timer.Tick += delegate
                    {
                        try
                        {
                            if (step == 0)
                            {
                                before = Native.IsWindowVisible(Native.desktopList());
                                controller.enableDesktop();
                                hidden = !Native.IsWindowVisible(Native.desktopList());
                                foreach (var panel in controller.panels)
                                    panel.showQuietly();
                                above = controller.panels.All(
                                    panel => Native.aboveDesktop(new WindowInteropHelper(panel).Handle));
                                controller.disableDesktop();
                                restored = Native.IsWindowVisible(Native.desktopList()) == before;
                            }
                            if (step == 4)
                            {
                                timer.Stop();
                                var p = Process.GetCurrentProcess();
                                File.WriteAllText(args[1], Store.json().Serialize(new {
                                    Groups = store.data.Groups.Count,
                                    Items = store.data.Groups.Sum(g => g.Items.Count),
                                    panels = controller.panels.Count, HiddenDuringTest = hidden,
                                    Restored = restored, PanelsAboveWallpaper = above,
                                    WorkingSetMB = p.WorkingSet64 / 1048576.0,
                                    PrivateMB = p.PrivateMemorySize64 / 1048576.0,
                                    CpuMs = p.TotalProcessorTime.TotalMilliseconds - start,
                                    ElapsedSeconds = 10
                                }));
                                controller.quit();
                            }
                            step++;
                        }
                        catch (Exception e)
                        {
                            timer.Stop();
                            controller.disableDesktop();
                            File.WriteAllText(args[1], "FAILED: " + e.ToString());
                            controller.quit();
                        }
                    };
                    timer.Start();
                }
                app.Run();
                return 0;
            }
            catch (Exception e)
            {
                if (controller != null)
                    controller.disableDesktop();
                if (render || smoke || crash)
                    File.WriteAllText(args[1] + ".error.txt", e.ToString());
                else
                    MessageBox.Show(e.Message, "Orla", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            finally
            {
                if (settingsRegistration != null)
                    settingsRegistration.Unregister(null);
                if (openSettings != null)
                    openSettings.Dispose();
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
        static void renderWindow(Window w, string path)
        {
            w.UpdateLayout();
            var content = (FrameworkElement)w.Content;
            var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96,
                                                PixelFormats.Pbgra32);
            bitmap.Render(content);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));
            using (var fs = File.Create(path)) png.Save(fs);
        }
    }
}
