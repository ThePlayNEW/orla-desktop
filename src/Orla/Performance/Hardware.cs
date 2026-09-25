using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Orla
{
    // A graphics adapter as the Windows display driver sees it: the same list, names and live data Task Manager uses.
    public class GpuAdapter
    {
        public uint Handle;
        // As performance counter instances name it: luid_0x<high>_0x<low>.
        public string Luid;
        public string Name;
        public string Driver;
        public ulong Dedicated, Shared;
    }

    // What this computer is made of. Read once, off the UI thread, the first time performance is shown.
    public static class Hardware
    {
        public static string CpuName { get; private set; } = "";
        public static int Cores { get; private set; }
        public static int Threads { get; private set; }
        public static double BaseMhz { get; private set; }
        // "DDR4 · 3200 MT/s · 2 of 4 slots" style facts, empty where Windows does not know.
        public static string MemoryType { get; private set; } = "";
        public static int MemorySpeed { get; private set; }
        public static int MemorySlotsUsed { get; private set; }
        public static int MemorySlots { get; private set; }
        // Installed memory, as on the label of the modules; Windows can use a little less.
        public static double MemoryInstalled { get; private set; }
        public static List<GpuAdapter> Gpus { get; private set; } = new List<GpuAdapter>();
        // Physical disk number to its model and kind, such as "0" → ("Samsung SSD 980", "SSD").
        public static Dictionary<string, Tuple<string, string>> Disks { get; private set; } = new Dictionary<string, Tuple<string, string>>();

        static bool loaded;
        static readonly object gate = new object();

        // Sample hardware for documentation images, so they never show the computer that made them.
        internal static void demo()
        {
            lock (gate)
            {
                loaded = true;
                CpuName = "AMD Ryzen 7 7800X3D 8-Core Processor";
                Cores = 8;
                Threads = 16;
                BaseMhz = 4200;
                MemoryType = "DDR5";
                MemorySpeed = 6000;
                MemorySlotsUsed = 2;
                MemorySlots = 4;
                Gpus = new List<GpuAdapter> { new GpuAdapter { Name = "NVIDIA GeForce RTX 4070", Luid = "luid_0x00000000_0x00001234",
                                                               Driver = "32.0.15.6614", Dedicated = 12UL << 30, Shared = 16UL << 30 } };
                Disks = new Dictionary<string, Tuple<string, string>> { ["0"] = Tuple.Create("Samsung SSD 990 PRO 2TB", "NVMe"),
                                                                        ["1"] = Tuple.Create("WDC WD20EZBX", "HDD") };
            }
        }

        public static void load()
        {
            lock (gate)
            {
                if (loaded)
                    return;
                loaded = true;
                attempt(readCpu);
                attempt(() => Gpus = Kmt.adapters());
                attempt(() => MemoryInstalled = Kmt.GetPhysicallyInstalledSystemMemory(out ulong kb) ? kb * 1024.0 : 0);
            }
            // WMI can take seconds, or hang on a broken install; the readings do not wait for it.
            System.Threading.Tasks.Task.Run(() => {
                attempt(readMemory);
                attempt(readDisks);
            });
        }

        // One missing source, such as WMI turned off, must not hide what the others know.
        static void attempt(Action read)
        {
            try
            {
                read();
            }
            catch (Exception)
            {
            }
        }

        static void readCpu()
        {
            Threads = (int)Kmt.GetActiveProcessorCount(0xFFFF);
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
            {
                CpuName = String.Join(" ", ((key?.GetValue("ProcessorNameString") as string) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                BaseMhz = Convert.ToDouble(key?.GetValue("~MHz") ?? 0);
            }
            Cores = Kmt.physicalCores();
        }

        static void readMemory()
        {
            var modules = new List<ManagementBaseObject>();
            using (var search = new ManagementObjectSearcher("SELECT SMBIOSMemoryType, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory"))
                foreach (ManagementBaseObject m in search.Get())
                    modules.Add(m);
            MemorySlotsUsed = modules.Count;
            ManagementBaseObject first = modules.FirstOrDefault();
            if (first != null)
            {
                MemoryType = memoryType(Convert.ToInt32(first["SMBIOSMemoryType"] ?? 0));
                MemorySpeed = Convert.ToInt32(first["ConfiguredClockSpeed"] ?? 0);
                if (MemorySpeed == 0)
                    MemorySpeed = Convert.ToInt32(first["Speed"] ?? 0);
            }
            using (var search = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray"))
                MemorySlots = search.Get().Cast<ManagementBaseObject>().Sum(a => Convert.ToInt32(a["MemoryDevices"] ?? 0));
        }

        // SMBIOS memory type numbers for the kinds still in use.
        internal static string memoryType(int smbios)
        {
            switch (smbios)
            {
            case 24: return "DDR3";
            case 26: return "DDR4";
            case 27: return "LPDDR";
            case 28: return "LPDDR2";
            case 29: return "LPDDR3";
            case 30: return "LPDDR4";
            case 34: return "DDR5";
            case 35: return "LPDDR5";
            default: return "";
            }
        }

        static void readDisks()
        {
            var disks = new Dictionary<string, Tuple<string, string>>();
            using (var search = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT DeviceId, FriendlyName, MediaType, BusType FROM MSFT_PhysicalDisk"))
                foreach (ManagementBaseObject d in search.Get())
                {
                    int media = Convert.ToInt32(d["MediaType"] ?? 0), bus = Convert.ToInt32(d["BusType"] ?? 0);
                    string kind = bus == 17 ? "NVMe" : media == 4 ? "SSD" : media == 3 ? "HDD" : "";
                    disks[Convert.ToString(d["DeviceId"])] = Tuple.Create(Convert.ToString(d["FriendlyName"]), kind);
                }
            Disks = disks;
        }
    }

    // The display driver interface in gdi32 that Task Manager reads for GPU names, memory sizes and sensors.
    static class Kmt
    {
        const int QueryUmdDriverVersion = 18, QueryAdapterType = 15, QueryRegistryInfo = 8, QuerySegmentSize = 3, QueryNodePerfData = 61, QueryPerfData = 62;

        [StructLayout(LayoutKind.Sequential)]
        struct EnumAdapters2
        {
            public uint Count;
            public IntPtr Adapters;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AdapterInfo
        {
            public uint Handle;
            public uint LuidLow;
            public int LuidHigh;
            public uint Sources;
            public int PrecisePresentRegions;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct QueryAdapterInfo
        {
            public uint Handle;
            public int Type;
            public IntPtr Data;
            public uint Size;
        }

        // What the driver reports live, in its own units: tenths of a degree, RPM, tenths of a percent, Hz.
        public struct Sensors
        {
            public uint Temperature, Fan, Power;
            public ulong MemoryClock, CoreClock;
        }

        [DllImport("gdi32.dll")]
        static extern int D3DKMTEnumAdapters2(ref EnumAdapters2 e);
        [DllImport("gdi32.dll")]
        static extern int D3DKMTQueryAdapterInfo(ref QueryAdapterInfo q);
        [DllImport("gdi32.dll")]
        static extern int D3DKMTCloseAdapter(ref uint handle);

        [DllImport("kernel32.dll")]
        static extern bool GetLogicalProcessorInformation(IntPtr buffer, ref int length);
        [DllImport("kernel32.dll")]
        public static extern uint GetActiveProcessorCount(ushort group);
        [DllImport("kernel32.dll")]
        public static extern bool GetPhysicallyInstalledSystemMemory(out ulong kilobytes);

        static byte[] query(uint handle, int type, int size)
        {
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                for (int i = 0; i < size; i++)
                    Marshal.WriteByte(buffer, i, 0);
                var q = new QueryAdapterInfo { Handle = handle, Type = type, Data = buffer, Size = (uint)size };
                if (D3DKMTQueryAdapterInfo(ref q) != 0)
                    return null;
                var bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Real graphics adapters, largest dedicated memory first, so the gaming card leads on a laptop with two.
        // ponytail: the handles stay open for the life of Orla; a driver update makes their sensors read empty until restart.
        public static List<GpuAdapter> adapters()
        {
            var list = new List<GpuAdapter>();
            var e = new EnumAdapters2();
            if (D3DKMTEnumAdapters2(ref e) != 0 || e.Count == 0)
                return list;
            int step = Marshal.SizeOf(typeof(AdapterInfo));
            e.Adapters = Marshal.AllocHGlobal(step * (int)e.Count);
            try
            {
                if (D3DKMTEnumAdapters2(ref e) != 0)
                    return list;
                for (int i = 0; i < e.Count; i++)
                {
                    var info = (AdapterInfo)Marshal.PtrToStructure(e.Adapters + i * step, typeof(AdapterInfo));
                    byte[] type = query(info.Handle, QueryAdapterType, 4);
                    // Bit 0: renders; bit 2: the software renderer Windows falls back to.
                    uint flags = type == null ? 0 : BitConverter.ToUInt32(type, 0);
                    if ((flags & 1) == 0 || (flags & 4) != 0)
                    {
                        D3DKMTCloseAdapter(ref info.Handle);
                        continue;
                    }
                    byte[] registry = query(info.Handle, QueryRegistryInfo, 4 * 260 * 2);
                    byte[] segments = query(info.Handle, QuerySegmentSize, 24);
                    byte[] version = query(info.Handle, QueryUmdDriverVersion, 8);
                    var gpu = new GpuAdapter {
                        Handle = info.Handle,
                        Luid = String.Format("luid_0x{0:X8}_0x{1:X8}", info.LuidHigh, info.LuidLow),
                        Name = registry == null ? "" : System.Text.Encoding.Unicode.GetString(registry, 0, 520).TrimEnd('\0').Trim(),
                        Dedicated = segments == null ? 0 : BitConverter.ToUInt64(segments, 0),
                        Shared = segments == null ? 0 : BitConverter.ToUInt64(segments, 16),
                    };
                    if (version != null)
                    {
                        ulong v = BitConverter.ToUInt64(version, 0);
                        gpu.Driver = String.Format("{0}.{1}.{2}.{3}", v >> 48, (v >> 32) & 0xFFFF, (v >> 16) & 0xFFFF, v & 0xFFFF);
                    }
                    list.Add(gpu);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(e.Adapters);
            }
            return list.OrderByDescending(g => g.Dedicated).ToList();
        }

        // Temperature, fan, power and clocks, on WDDM 2.4 drivers and later; zero where the driver keeps quiet.
        public static Sensors sensors(GpuAdapter gpu)
        {
            var s = new Sensors();
            // D3DKMT_ADAPTER_PERFDATA (64 bytes): memory clock at 8; fan, power and temperature at 48, 52 and 56.
            byte[] adapter = query(gpu.Handle, QueryPerfData, 64);
            if (adapter != null)
            {
                s.MemoryClock = BitConverter.ToUInt64(adapter, 8);
                s.Fan = BitConverter.ToUInt32(adapter, 48);
                s.Power = BitConverter.ToUInt32(adapter, 52);
                s.Temperature = BitConverter.ToUInt32(adapter, 56);
            }
            // D3DKMT_NODE_PERFDATA (56 bytes) of engine 0, the 3D engine: its clock at 8.
            byte[] node = query(gpu.Handle, QueryNodePerfData, 56);
            if (node != null)
                s.CoreClock = BitConverter.ToUInt64(node, 8);
            return s;
        }

        // Physical cores: one RelationProcessorCore entry each.
        // ponytail: counts the calling processor group only, so above 64 threads it undercounts; GetLogicalProcessorInformationEx if that matters.
        public static int physicalCores()
        {
            int length = 0;
            GetLogicalProcessorInformation(IntPtr.Zero, ref length);
            if (length == 0)
                return 0;
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                if (!GetLogicalProcessorInformation(buffer, ref length))
                    return 0;
                // SYSTEM_LOGICAL_PROCESSOR_INFORMATION is 32 bytes on 64-bit Windows: a mask, the relationship, then a union.
                const int size = 32;
                int cores = 0;
                for (int offset = 0; offset + size <= length; offset += size)
                    if (Marshal.ReadInt32(buffer, offset + 8) == 0)
                        cores++;
                return cores;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
