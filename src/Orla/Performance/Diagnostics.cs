using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Orla
{
    // `Orla.exe --sensors <file>`: ten seconds of readings and what reading them cost, for checking a computer's
    // sensors or attaching to a problem report.
    static class Diagnostics
    {
        public static int sensors(string file)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var text = new StringBuilder();
            Process self = Process.GetCurrentProcess();
            TimeSpan before = self.TotalProcessorTime;
            var clock = Stopwatch.StartNew();
            Sensors.use(true);
            Thread.Sleep(10500);
            Reading r = Sensors.Latest;
            Sensors.release(true);
            self.Refresh();
            double cost = (self.TotalProcessorTime - before).TotalMilliseconds / clock.Elapsed.TotalMilliseconds * 100 / Environment.ProcessorCount;
            text.AppendLine("cpu " + Hardware.CpuName + " cores " + Hardware.Cores + " threads " + Hardware.Threads + " base " + Hardware.BaseMhz);
            text.AppendLine("memory " + Hardware.MemoryType + " " + Hardware.MemorySpeed + " slots " + Hardware.MemorySlotsUsed + "/" + Hardware.MemorySlots);
            foreach (GpuAdapter g in Hardware.Gpus)
                text.AppendLine("gpu " + g.Name + " " + g.Luid + " dedicated " + g.Dedicated / 1048576 + " MB shared " + g.Shared / 1048576 + " MB driver " + g.Driver);
            foreach (var d in Hardware.Disks)
                text.AppendLine("disk " + d.Key + " " + d.Value.Item1 + " " + d.Value.Item2);
            if (r != null)
            {
                text.AppendLine(String.Format("reading cpu {0:0.0}% {1:0} MHz cores [{2}] processes {3} threads {4} uptime {5:0}s",
                                              r.Cpu, r.Mhz, String.Join(" ", r.Cores.Select(c => c.ToString("0"))), r.Processes, r.Threads, r.Uptime));
                text.AppendLine(String.Format("memory {0:0.0}/{1:0.0} GB committed {2:0.0}/{3:0.0} GB", r.MemoryUsed / 1e9, r.MemoryTotal / 1e9,
                                              r.Committed / 1e9, r.CommitLimit / 1e9));
                foreach (GpuReading g in r.Gpus)
                    text.AppendLine(String.Format("gpu {0} load {1:0.0}% [{2}] dedicated {3:0} MB shared {4:0} MB temp {5} fan {6} power {7} memclock {8} clock {9}",
                                                  g.Adapter.Name, g.Load, String.Join(" ", g.Engines.Select(e => e.Key + "=" + e.Value.ToString("0"))),
                                                  g.Dedicated / 1048576, g.Shared / 1048576, g.Temperature, g.Fan, g.Power, g.MemoryClock, g.CoreClock));
                foreach (DiskReading d in r.Disks)
                    text.AppendLine(String.Format("disk {0} active {1:0.0}% read {2:0} KB/s write {3:0} KB/s", d.Instance, d.Active, d.Read / 1024, d.Write / 1024));
                text.AppendLine(String.Format("network down {0:0} KB/s up {1:0} KB/s battery {2} charging {3}", r.Down / 1024, r.Up / 1024, r.Battery, r.Charging));
                if (r.Top != null)
                    foreach (ProcessReading p in r.Top.OrderByDescending(p => p.Cpu).Take(8))
                        text.AppendLine(String.Format("process {0} cpu {1:0.0}% memory {2:0} MB gpu {3:0}%", p.Name, p.Cpu, p.Memory / 1048576, p.Gpu));
            }
            text.AppendLine(String.Format("cost {0:0.00}% of the whole processor, {1:0} MB working set", cost, self.WorkingSet64 / 1048576.0));
            File.WriteAllText(file, text.ToString());
            return 0;
        }
    }
}
