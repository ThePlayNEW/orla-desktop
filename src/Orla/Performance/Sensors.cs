using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Orla
{
    public class GpuReading
    {
        public GpuAdapter Adapter;
        // The busiest engine, as Task Manager counts GPU use.
        public double Load;
        // Engine kinds ("3D", "Copy", "VideoDecode", "VideoEncode", "Compute_0"...) and their busiest instance.
        public Dictionary<string, double> Engines = new Dictionary<string, double>();
        public double Dedicated, Shared;
        public double Temperature = Double.NaN, Fan = Double.NaN, Power = Double.NaN, MemoryClock = Double.NaN, CoreClock = Double.NaN;
    }

    public class DiskReading
    {
        // The counter instance, such as "0 C:": the disk number and its drive letters.
        public string Instance;
        public double Active, Read, Write;
        public string Number => Instance.Split(' ')[0];
        public string Letters => Instance.Contains(" ") ? Instance.Substring(Instance.IndexOf(' ') + 1) : "";
    }

    public class ProcessReading
    {
        public string Name;
        public int Pid;
        public double Cpu, Memory, Gpu;
        // What the program calls itself, such as "Google Chrome", and its file, for the icon; empty when Windows keeps it private.
        public string Label, Path;
    }

    // One moment of the whole computer. Rates are per second; sizes are bytes.
    public class Reading
    {
        public double Cpu, Mhz;
        public double[] Cores = new double[0];
        public int Processes, Threads;
        public double Uptime;
        public double MemoryUsed, MemoryTotal, Committed, CommitLimit;
        public List<GpuReading> Gpus = new List<GpuReading>();
        public List<DiskReading> Disks = new List<DiskReading>();
        public double Down, Up;
        public int Battery = -1;
        public bool Charging;
        // Filled only while someone looks at the list of processes, since reading every process costs more.
        public List<ProcessReading> Top;

        public GpuReading Gpu => Gpus.FirstOrDefault();
        public double Memory => MemoryTotal > 0 ? MemoryUsed / MemoryTotal * 100 : 0;
        public double DiskActive => Disks.Count == 0 ? 0 : Disks.Max(d => d.Active);
    }

    // Reads the computer once a second on its own thread, only while something shows the numbers: the Performance
    // page or a performance panel. The last minute is kept for the charts.
    public static class Sensors
    {
        public const int HistoryLength = 60;

        // Settled by the first definite answer: a battery does not come or go while Orla runs, and the unknown status
        // Windows reports for a moment at sign-in or after sleep must neither hide it nor flicker it.
        static bool? battery;

        public static bool HasBattery
        {
            get
            {
                if (battery == null)
                {
                    Forms.BatteryChargeStatus s = Forms.SystemInformation.PowerStatus.BatteryChargeStatus;
                    if (s != Forms.BatteryChargeStatus.Unknown)
                        battery = (s & Forms.BatteryChargeStatus.NoSystemBattery) == 0;
                }
                return battery ?? false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MemoryStatus
        {
            public uint Length, Load;
            public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr process, uint flags, System.Text.StringBuilder name, ref int size);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        // Raised on the UI thread after each reading.
        public static event Action<Reading> Sampled;

        static readonly object gate = new object();
        static readonly LinkedList<Reading> history = new LinkedList<Reading>();
        static int users, processUsers;
        static Thread thread;
        static Dispatcher dispatcher;
        // Set for documentation images: sample readings stand still and the real sensors stay off.
        static bool frozen;

        public static Reading Latest { get; private set; }

        // The last minute, oldest first.
        public static Reading[] History
        {
            get
            {
                lock (gate)
                    return history.ToArray();
            }
        }

        // Every consumer calls use when it appears and release when it goes, with processes when it lists them.
        public static void use(bool processes = false)
        {
            lock (gate)
            {
                users++;
                if (processes)
                    processUsers++;
                if (dispatcher == null)
                    dispatcher = Dispatcher.CurrentDispatcher;
                if (thread == null && !frozen)
                {
                    // Normal priority: a game holding the processor at 100% is exactly when people look, and a reading
                    // costs well under 1% of one core.
                    thread = new Thread(run) { IsBackground = true, Name = "Orla sensors" };
                    thread.Start();
                }
            }
        }

        public static void release(bool processes = false)
        {
            lock (gate)
            {
                users = Math.Max(0, users - 1);
                if (processes)
                    processUsers = Math.Max(0, processUsers - 1);
            }
        }

        internal static void demo(Reading[] readings)
        {
            lock (gate)
            {
                frozen = true;
                history.Clear();
                foreach (Reading r in readings)
                    history.AddLast(r);
                Latest = readings.LastOrDefault();
            }
        }

        static void run()
        {
            try
            {
                Hardware.load();
                using (var sampler = new Sampler())
                    while (true)
                    {
                        // Rates compare two collections, so the first reading waits a second too.
                        Thread.Sleep(1000);
                        bool processes;
                        lock (gate)
                        {
                            // Nobody is looking, or sample readings took over: stop, and start clean next time instead of
                            // charting the gap as a line.
                            if (users == 0 || frozen)
                            {
                                stop();
                                return;
                            }
                            processes = processUsers > 0;
                        }
                        Reading r = sampler.read(processes);
                        lock (gate)
                        {
                            if (frozen)
                            {
                                stop();
                                return;
                            }
                            history.AddLast(r);
                            while (history.Count > HistoryLength)
                                history.RemoveFirst();
                            Latest = r;
                        }
                        dispatcher.BeginInvoke(new Action(() => Sampled?.Invoke(r)), DispatcherPriority.Background);
                    }
            }
            catch (Exception e)
            {
                // A sensor that fails must never take the panels down with it; the next consumer starts again.
                Report.log(e);
                lock (gate)
                    stop();
            }
        }

        // Called with the lock held, in the same step as the decision to stop, so a consumer arriving meanwhile starts
        // a new thread instead of counting on this one.
        static void stop()
        {
            thread = null;
            if (!frozen)
            {
                history.Clear();
                Latest = null;
            }
        }

        // The counters themselves, opened once per run of the sensor thread.
        sealed class Sampler : IDisposable
        {
            static readonly Regex Engine = new Regex(@"pid_(\d+)_(luid_0x[0-9A-F]+_0x[0-9A-F]+)_phys_(\d+)_eng_(\d+)_engtype_(.+)$",
                                                     RegexOptions.IgnoreCase);

            readonly PdhQuery query = new PdhQuery(), processQuery = new PdhQuery();
            readonly IntPtr cpu, performance, frequency, cores, processCount, threadCount, uptime, engines, dedicated, shared,
                            diskIdle, diskRead, diskWrite, received, sent, processCpu, processMemory, processId;
            bool processesPrimed;

            public Sampler()
            {
                cpu = query.add(@"\Processor Information(_Total)\% Processor Utility");
                performance = query.add(@"\Processor Information(_Total)\% Processor Performance");
                frequency = query.add(@"\Processor Information(_Total)\Processor Frequency");
                cores = query.add(@"\Processor Information(*)\% Processor Utility");
                processCount = query.add(@"\System\Processes");
                threadCount = query.add(@"\System\Threads");
                uptime = query.add(@"\System\System Up Time");
                engines = query.add(@"\GPU Engine(*)\Utilization Percentage");
                dedicated = query.add(@"\GPU Adapter Memory(*)\Dedicated Usage");
                shared = query.add(@"\GPU Adapter Memory(*)\Shared Usage");
                diskIdle = query.add(@"\PhysicalDisk(*)\% Idle Time");
                diskRead = query.add(@"\PhysicalDisk(*)\Disk Read Bytes/sec");
                diskWrite = query.add(@"\PhysicalDisk(*)\Disk Write Bytes/sec");
                received = query.add(@"\Network Interface(*)\Bytes Received/sec");
                sent = query.add(@"\Network Interface(*)\Bytes Sent/sec");
                processCpu = processQuery.add(@"\Process(*)\% Processor Time");
                processMemory = processQuery.add(@"\Process(*)\Working Set - Private");
                processId = processQuery.add(@"\Process(*)\ID Process");
                // Rates need a first collection to compare with.
                query.collect();
            }

            public Reading read(bool processes)
            {
                query.collect();
                var r = new Reading();
                r.Cpu = clamp(PdhQuery.value(cpu));
                r.Mhz = PdhQuery.value(frequency) * PdhQuery.value(performance) / 100;
                // "0,3" is core 3 of processor group 0; totals end in _Total.
                r.Cores = PdhQuery.values(cores).Where(v => !v.Key.EndsWith("_Total"))
                                  .OrderBy(v => group(v.Key)).ThenBy(v => index(v.Key)).Select(v => clamp(v.Value)).ToArray();
                r.Processes = (int)zero(PdhQuery.value(processCount));
                r.Threads = (int)zero(PdhQuery.value(threadCount));
                r.Uptime = zero(PdhQuery.value(uptime));

                var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf(typeof(MemoryStatus)) };
                if (GlobalMemoryStatusEx(ref memory))
                {
                    r.MemoryTotal = memory.TotalPhys;
                    r.MemoryUsed = memory.TotalPhys - memory.AvailPhys;
                    r.CommitLimit = memory.TotalPageFile;
                    r.Committed = memory.TotalPageFile - memory.AvailPageFile;
                }

                var perProcessGpu = new Dictionary<int, double>();
                readGpus(r, perProcessGpu);

                var idle = map(PdhQuery.values(diskIdle).Where(v => v.Key != "_Total"));
                var reads = map(PdhQuery.values(diskRead));
                var writes = map(PdhQuery.values(diskWrite));
                foreach (var d in idle.OrderBy(v => index(v.Key.Split(' ')[0])))
                    r.Disks.Add(new DiskReading { Instance = d.Key, Active = clamp(100 - d.Value), Read = get(reads, d.Key), Write = get(writes, d.Key) });

                r.Down = PdhQuery.values(received).Sum(v => v.Value);
                r.Up = PdhQuery.values(sent).Sum(v => v.Value);

                if (HasBattery)
                {
                    Forms.PowerStatus power = Forms.SystemInformation.PowerStatus;
                    r.Battery = (int)Math.Round(Math.Max(0, Math.Min(1, power.BatteryLifePercent)) * 100);
                    r.Charging = power.PowerLineStatus == Forms.PowerLineStatus.Online;
                }

                if (processes)
                    r.Top = readProcesses(perProcessGpu);
                else
                    processesPrimed = false;
                return r;
            }

            // Task Manager's arithmetic: add up the processes on each engine, then the busiest engine is the GPU's use.
            void readGpus(Reading r, Dictionary<int, double> perProcess)
            {
                var perEngine = new Dictionary<string, double>();
                foreach (var v in PdhQuery.values(engines))
                {
                    Match m = Engine.Match(v.Key);
                    if (!m.Success)
                        continue;
                    string engine = m.Groups[2].Value + "|" + m.Groups[3].Value + "|" + m.Groups[4].Value + "|" + m.Groups[5].Value;
                    perEngine[engine] = get(perEngine, engine) + v.Value;
                    int pid = Int32.Parse(m.Groups[1].Value);
                    perProcess[pid] = Math.Max(get(perProcess, pid), v.Value);
                }
                var dedicatedUse = PdhQuery.values(dedicated);
                var sharedUse = PdhQuery.values(shared);
                foreach (GpuAdapter a in Hardware.Gpus)
                {
                    var g = new GpuReading { Adapter = a };
                    foreach (var e in perEngine.Where(e => e.Key.StartsWith(a.Luid, StringComparison.OrdinalIgnoreCase)))
                    {
                        string kind = e.Key.Substring(e.Key.LastIndexOf('|') + 1);
                        g.Engines[kind] = clamp(Math.Max(get(g.Engines, kind), e.Value));
                    }
                    g.Load = g.Engines.Count == 0 ? 0 : g.Engines.Values.Max();
                    g.Dedicated = dedicatedUse.Where(v => v.Key.StartsWith(a.Luid, StringComparison.OrdinalIgnoreCase)).Sum(v => v.Value);
                    g.Shared = sharedUse.Where(v => v.Key.StartsWith(a.Luid, StringComparison.OrdinalIgnoreCase)).Sum(v => v.Value);
                    // Zero means the driver does not report it.
                    Kmt.Sensors s = Kmt.sensors(a);
                    if (s.Temperature > 0)
                        g.Temperature = s.Temperature / 10.0;
                    if (s.Fan > 0)
                        g.Fan = s.Fan;
                    if (s.Power > 0)
                        g.Power = s.Power / 10.0;
                    if (s.MemoryClock > 0)
                        g.MemoryClock = s.MemoryClock / 1e6;
                    if (s.CoreClock > 0)
                        g.CoreClock = s.CoreClock / 1e6;
                    r.Gpus.Add(g);
                }
            }

            // Processes grouped by program, as people think of them: every chrome.exe together.
            List<ProcessReading> readProcesses(Dictionary<int, double> gpu)
            {
                processQuery.collect();
                if (!processesPrimed)
                {
                    processesPrimed = true;
                    return null;
                }
                var cpuUse = map(PdhQuery.values(processCpu));
                var memoryUse = map(PdhQuery.values(processMemory));
                var groups = new Dictionary<string, ProcessReading>(StringComparer.OrdinalIgnoreCase);
                int threads = Math.Max(1, Hardware.Threads);
                foreach (var id in PdhQuery.values(processId))
                {
                    if (id.Key == "_Total" || id.Key == "Idle" || id.Value == 0)
                        continue;
                    int hash = id.Key.IndexOf('#');
                    string name = hash > 0 ? id.Key.Substring(0, hash) : id.Key;
                    if (!groups.TryGetValue(name, out ProcessReading p))
                        groups[name] = p = new ProcessReading { Name = name, Pid = (int)id.Value };
                    p.Cpu += get(cpuUse, id.Key) / threads;
                    p.Memory += get(memoryUse, id.Key);
                    p.Gpu = Math.Max(p.Gpu, get(gpu, (int)id.Value));
                }
                // Only the leaders by each measure are shown, so only they are named.
                const int shown = 8;
                var top = groups.Values.OrderByDescending(p => p.Cpu).Take(shown)
                                .Union(groups.Values.OrderByDescending(p => p.Memory).Take(shown))
                                .Union(groups.Values.OrderByDescending(p => p.Gpu).Take(shown)).ToList();
                foreach (ProcessReading p in top)
                {
                    Tuple<string, string> known = program(p.Name, p.Pid);
                    p.Label = known.Item1;
                    p.Path = known.Item2;
                }
                return top;
            }

            // Program names by process name, found once: its description and file.
            static readonly Dictionary<string, Tuple<string, string>> programs = new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase);

            static Tuple<string, string> program(string name, int pid)
            {
                if (programs.TryGetValue(name, out Tuple<string, string> known))
                    return known;
                string path = null, label = name;
                IntPtr process = OpenProcess(0x1000, false, pid);
                if (process != IntPtr.Zero)
                {
                    var text = new System.Text.StringBuilder(1024);
                    int size = text.Capacity;
                    if (QueryFullProcessImageName(process, 0, text, ref size))
                        path = text.ToString();
                    CloseHandle(process);
                }
                if (path != null)
                    try
                    {
                        string description = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileDescription?.Trim();
                        if (!String.IsNullOrEmpty(description))
                            label = description;
                    }
                    catch (Exception)
                    {
                    }
                return programs[name] = Tuple.Create(label, path);
            }

            // Instance names can repeat for a moment while processes come and go; the last one wins.
            static Dictionary<string, double> map(IEnumerable<KeyValuePair<string, double>> values)
            {
                var d = new Dictionary<string, double>();
                foreach (var v in values)
                    d[v.Key] = v.Value;
                return d;
            }

            static double get<K>(Dictionary<K, double> values, K key) => values.TryGetValue(key, out double v) && !Double.IsNaN(v) ? v : 0;

            static double clamp(double v) => Double.IsNaN(v) ? 0 : Math.Max(0, Math.Min(100, v));

            static double zero(double v) => Double.IsNaN(v) ? 0 : v;

            static int group(string instance) => Int32.TryParse(instance.Split(',')[0], out int g) ? g : 0;

            static int index(string instance) => Int32.TryParse(instance.Split(',').Last(), out int i) ? i : 0;

            public void Dispose()
            {
                query.Dispose();
                processQuery.Dispose();
            }
        }
    }
}
