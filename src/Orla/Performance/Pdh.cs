using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Orla
{
    // Windows performance counters, the same ones Task Manager and Performance Monitor read. Counter paths are the
    // English names, so they work on every display language. One query collects all its counters at once.
    public sealed class PdhQuery : IDisposable
    {
        const uint PDH_FMT_DOUBLE = 0x200, PDH_FMT_NOCAP100 = 0x8000, PDH_MORE_DATA = 0x800007D2;

        [StructLayout(LayoutKind.Explicit)]
        struct Value
        {
            [FieldOffset(0)] public uint Status;
            [FieldOffset(8)] public double Double;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Item
        {
            public IntPtr Name;
            public Value Value;
        }

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        static extern uint PdhOpenQuery(string source, IntPtr user, out IntPtr query);
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        static extern uint PdhAddEnglishCounter(IntPtr query, string path, IntPtr user, out IntPtr counter);
        [DllImport("pdh.dll")]
        static extern uint PdhCollectQueryData(IntPtr query);
        [DllImport("pdh.dll")]
        static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, IntPtr type, out Value value);
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        static extern uint PdhGetFormattedCounterArray(IntPtr counter, uint format, ref uint size, out uint count, IntPtr items);
        [DllImport("pdh.dll")]
        static extern uint PdhCloseQuery(IntPtr query);

        IntPtr query;

        public PdhQuery()
        {
            PdhOpenQuery(null, IntPtr.Zero, out query);
        }

        // A counter this computer does not have (no GPU counters in a virtual machine, say) gives IntPtr.Zero, and
        // reading it gives nothing.
        public IntPtr add(string path) =>
            query != IntPtr.Zero && PdhAddEnglishCounter(query, path, IntPtr.Zero, out IntPtr counter) == 0 ? counter : IntPtr.Zero;

        public void collect()
        {
            if (query != IntPtr.Zero)
                PdhCollectQueryData(query);
        }

        // Rates need two collections; before the second, and for a missing counter, the value is NaN.
        public static double value(IntPtr counter)
        {
            if (counter == IntPtr.Zero || PdhGetFormattedCounterValue(counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, IntPtr.Zero, out Value v) != 0 ||
                v.Status > 1)
                return Double.NaN;
            return v.Double;
        }

        // Every instance of a wildcard counter, such as each disk or each GPU engine, by instance name.
        public static List<KeyValuePair<string, double>> values(IntPtr counter)
        {
            var list = new List<KeyValuePair<string, double>>();
            if (counter == IntPtr.Zero)
                return list;
            uint size = 0;
            if (PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out uint count, IntPtr.Zero) != PDH_MORE_DATA)
                return list;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out count, buffer) != 0)
                    return list;
                int step = Marshal.SizeOf(typeof(Item));
                for (int i = 0; i < count; i++)
                {
                    var item = (Item)Marshal.PtrToStructure(buffer + i * step, typeof(Item));
                    if (item.Value.Status <= 1)
                        list.Add(new KeyValuePair<string, double>(Marshal.PtrToStringUni(item.Name), item.Value.Double));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return list;
        }

        public void Dispose()
        {
            if (query != IntPtr.Zero)
                PdhCloseQuery(query);
            query = IntPtr.Zero;
        }
    }
}
