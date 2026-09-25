using System;
using System.ComponentModel;
using System.Linq;

namespace Orla
{
    // Numbers the way Windows writes them, in the regional format.
    public static class Units
    {
        // Three significant digits at most: 1,23 · 12,3 · 123.
        public static string number(double v) =>
            v >= 100 ? v.ToString("0") : v >= 10 ? v.ToString("0.#") : v.ToString("0.##");

        // Binary multiples named KB, MB and GB, as File Explorer and Task Manager do.
        public static string size(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            while (bytes >= 1000 && i < units.Length - 1)
            {
                bytes /= 1024;
                i++;
            }
            return number(bytes) + " " + units[i];
        }

        public static string rate(double bytesPerSecond) => size(bytesPerSecond) + "/s";

        // Connections are measured in bits, in thousands, written as Windows does: Kbps, Mbps.
        public static string bits(double bytesPerSecond) => bitRate(bytesPerSecond * 8);

        public static string bitRate(double b)
        {
            string[] units = { "bps", "Kbps", "Mbps", "Gbps" };
            int i = 0;
            while (b >= 1000 && i < units.Length - 1)
            {
                b /= 1000;
                i++;
            }
            return number(b) + " " + units[i];
        }

        public static string percent(double v) => Math.Round(v, MidpointRounding.AwayFromZero) + "%";

        public static string ghz(double mhz) => (mhz / 1000).ToString("0.00") + " GHz";

        public static string celsius(double t) => t.ToString("0") + " °C";

        // Task Manager's form: days:hours:minutes:seconds, without the days on the first day.
        public static string uptime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return t.Days > 0 ? t.Days + ":" + t.ToString(@"hh\:mm\:ss") : ((int)t.TotalHours) + ":" + t.ToString(@"mm\:ss");
        }

        // A round top for a chart of rates: the next 1, 2 or 5 times a power of ten, never below the floor.
        public static double ceiling(double max, double floor)
        {
            max = Math.Max(max, floor);
            double power = Math.Pow(10, Math.Floor(Math.Log10(max)));
            foreach (double m in new double[] { 1, 2, 5, 10 })
                if (m * power >= max)
                    return m * power;
            return 10 * power;
        }
    }

    // One thing the Performance page and panel follow, with its colour, its chart and how it reads.
    public class Metric
    {
        public string Key, Tint;
        // 100 for percentages; 0 lets the chart scale to the largest value in view.
        public double Max = 100;
        public Func<Reading, double> Value;
        // A second line on the same chart, such as sending next to receiving.
        public Func<Reading, double> Second;
        public Func<Reading, string> Text, Detail;
        public Func<Reading, bool> Available = r => true;
        // How a single value of the chart reads, for the readout and the lowest, average and highest.
        public Func<double, string> Format = Units.percent;

        public string Name => Orla.Text.get("perf." + Key);

        public static readonly Metric[] All = {
            new Metric { Key = "cpu", Tint = Tints.Sky, Value = r => r.Cpu, Text = r => Units.percent(r.Cpu), Detail = r => Units.ghz(r.Mhz) },
            new Metric { Key = "gpu", Tint = Tints.SeaGlass, Value = r => r.Gpu?.Load ?? 0, Text = r => Units.percent(r.Gpu?.Load ?? 0),
                         Detail = r => Double.IsNaN(r.Gpu?.Temperature ?? Double.NaN) ? Units.size(r.Gpu?.Dedicated ?? 0) : Units.celsius(r.Gpu.Temperature),
                         Available = r => r.Gpus.Count > 0 },
            new Metric { Key = "memory", Tint = Tints.Moss, Value = r => r.Memory, Text = r => Units.percent(r.Memory),
                         Detail = r => Units.size(r.MemoryUsed) },
            new Metric { Key = "disk", Tint = Tints.Sand, Value = r => r.DiskActive, Text = r => Units.percent(r.DiskActive),
                         Detail = r => Units.rate(r.Disks.Sum(d => d.Read + d.Write)) },
            // Charted in bits, so the scale rounds to 10, 20 or 50 Mbps.
            new Metric { Key = "network", Tint = Tints.Coral, Max = 0, Value = r => r.Down * 8, Second = r => r.Up * 8, Format = Units.bitRate,
                         Text = r => "↓ " + Units.bits(r.Down), Detail = r => "↑ " + Units.bits(r.Up) },
            new Metric { Key = "battery", Tint = Tints.Moss, Value = r => Math.Max(0, r.Battery), Text = r => Units.percent(r.Battery),
                         Detail = r => Orla.Text.get(r.Charging ? "perf.charging" : "perf.onBattery"), Available = r => r.Battery >= 0 },
        };

        // What a panel shows before the first reading arrives: everything this computer usually has.
        public static int Usual => Sensors.HasBattery ? 6 : 5;

        public static Metric find(string key) => All.FirstOrDefault(m => m.Key == key) ?? All[0];

        // The last minute of this metric, oldest first.
        public double[] series(Reading[] history) => history.Select(Value).ToArray();

        public double[] secondSeries(Reading[] history) => Second == null ? null : history.Select(Second).ToArray();
    }

    // A metric as a tile on a performance panel.
    public class SensorTile : INotifyPropertyChanged
    {
        public Metric Metric { get; set; }
        public string Name => Metric.Name;
        public string Tint => Metric.Tint;
        public double Max => Metric.Max;
        public string Text { get; private set; }
        public string Detail { get; private set; }
        public double[] Values { get; private set; }
        public double[] Second { get; private set; }
        public string Tooltip => Orla.Text.format("perf.tileTip", Name);

        public void update(Reading r, Reading[] history)
        {
            Text = r == null ? "–" : Metric.Text(r);
            Detail = r == null ? "" : Metric.Detail(r);
            Values = Metric.series(history);
            Second = Metric.secondSeries(history);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
