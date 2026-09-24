using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace Orla
{
    // `Orla.exe --smoke report.json`: puts sample panels on the real desktop for a few seconds, checks that they
    // live on the desktop layer above the icons, measures resources, and removes them. It never hides the
    // Windows icons and never touches personal files.
    public static class Smoke
    {
        public static void run(Controller controller, string report)
        {
            controller.store.data = Demo.create();
            controller.start();
            double cpuStart = Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            timer.Tick += delegate {
                timer.Stop();
                var p = Process.GetCurrentProcess();
                p.Refresh();
                Desktop d = Desktop.find();
                var panels = controller.Hosts.Select(h => new {
                    h.Group.Name, Mode = h.Mode.ToString(), Window = Native.IsWindow(h.Handle),
                    Visible = Native.IsWindowVisible(h.Handle), ParentIsDesktop = Native.GetParent(h.Handle) == d.Host,
                    AboveIcons = d.IsValid && d.isAbove(h.Handle)
                }).ToList();
                File.WriteAllText(report, Store.json().Serialize(new {
                    DesktopFound = d.IsValid, HostClass = d.IsValid ? Native.windowClass(d.Host) : null,
                    IconsVisible = d.iconsVisible, Panels = panels,
                    Passed = d.IsValid && panels.Count > 0 && panels.All(x => x.Window && x.Visible && x.ParentIsDesktop && x.AboveIcons),
                    WorkingSetMB = Math.Round(p.WorkingSet64 / 1048576.0, 1),
                    PrivateMB = Math.Round(p.PrivateMemorySize64 / 1048576.0, 1),
                    CpuMsWhileIdle = Math.Round(p.TotalProcessorTime.TotalMilliseconds - cpuStart)
                }));
                controller.quit();
            };
            timer.Start();
        }
    }
}
