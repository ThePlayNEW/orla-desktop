using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace Orla
{
    // The Performance page: a card per metric, the chosen one in detail, and the programs using the most. Readings
    // arrive every second on the dispatcher the desktop panels share with Explorer, so the page builds its elements
    // once and only changes their text and sizes; that also keeps an open tooltip open.
    public partial class CentralWindow
    {
        sealed class MetricCard
        {
            public Metric Metric;
            public RadioButton Button;
            public TextBlock Value, Detail;
            public Chart Chart;
        }

        // The programs table: what each column shows.
        static readonly (string Key, Func<ProcessReading, double> Value)[] ProgramColumns = {
            ("cpu", p => p.Cpu), ("memory", p => p.Memory), ("gpu", p => p.Gpu)
        };

        readonly List<MetricCard> metricCards = new List<MetricCard>();
        readonly Dictionary<string, ImageSource> programIcons = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        string perfMetric = "cpu", programSort = "cpu", headingSort;
        int perfGpu, statIndex, extraIndex;
        bool perfWatching;

        void setupPerformance()
        {
            // The sensors run with the page in view, and the list of programs only then; a minimized window counts
            // as out of view.
            PagePerformance.IsVisibleChanged += delegate { watchPerformance(); };
            StateChanged += delegate { watchPerformance(); };
            PerfPanelButton.Click += delegate {
                Group panel = controller.Layout.Groups.FirstOrDefault(g => g.IsSensors);
                if (panel == null)
                    controller.createFromPreset(Presets.All.First(p => p.Key == "performance"));
                else
                    controller.setVisible(panel, true);
            };
            TaskManagerButton.Click += delegate { controller.open(System.IO.Path.Combine(Environment.SystemDirectory, "Taskmgr.exe")); };
        }

        void watchPerformance()
        {
            bool wanted = PagePerformance.IsVisible && WindowState != WindowState.Minimized;
            if (wanted == perfWatching)
                return;
            perfWatching = wanted;
            if (wanted)
            {
                Sensors.Sampled += showReading;
                Sensors.use(true);
                showReading(Sensors.Latest);
            }
            else
            {
                Sensors.Sampled -= showReading;
                Sensors.release(true);
            }
        }

        // Opens the page on a metric, such as "gpu"; null keeps the one shown last.
        public void showMetric(string key)
        {
            if (key != null)
                perfMetric = key;
            NavPerformance.IsChecked = true;
            showReading(Sensors.Latest);
        }

        void refreshPerformance()
        {
            Group panel = controller.Layout.Groups.FirstOrDefault(g => g.IsSensors);
            PerfPanelButton.Visibility = panel != null && panel.Visible ? Visibility.Collapsed : Visibility.Visible;
            PerfPanelLabel.Text = Text.get(panel == null ? "perf.addPanel" : "perf.showPanel");
        }

        void showReading(Reading r)
        {
            if (!perfWatching)
                return;
            Reading[] history = Sensors.History;
            List<Metric> shown = Metric.All.Where(m => r == null ? m.Key != "battery" : m.Available(r)).ToList();
            if (!shown.SequenceEqual(metricCards.Select(c => c.Metric)))
                buildMetricCards(shown);
            if (!shown.Any(m => m.Key == perfMetric))
                perfMetric = "cpu";
            foreach (MetricCard c in metricCards)
            {
                c.Button.IsChecked = c.Metric.Key == perfMetric;
                c.Value.Text = r == null ? "–" : c.Metric.Text(r);
                c.Detail.Text = r == null ? " " : c.Metric.Detail(r);
                c.Chart.Values = c.Metric.series(history);
                c.Chart.Second = c.Metric.secondSeries(history);
            }
            showDetail(Metric.find(perfMetric), r, history);
            showPrograms(r);
        }

        void buildMetricCards(List<Metric> shown)
        {
            metricCards.Clear();
            PerfMetrics.Children.Clear();
            foreach (Metric m in shown)
            {
                var card = new MetricCard { Metric = m, Button = new RadioButton { GroupName = "metric" } };
                card.Button.SetResourceReference(StyleProperty, "Orla.Metric");
                var body = new StackPanel();
                body.Children.Add(secondary(m.Name, 13));
                card.Value = number(22);
                card.Value.Margin = new Thickness(0, 2, 0, 0);
                card.Value.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Display");
                card.Value.TextTrimming = TextTrimming.None;
                // A long reading, such as a fast connection, shrinks to fit instead of being cut.
                body.Children.Add(new Viewbox { Child = card.Value, StretchDirection = StretchDirection.DownOnly, HorizontalAlignment = HorizontalAlignment.Left,
                                                Height = 30 });
                card.Detail = secondary("", 12);
                card.Detail.TextTrimming = TextTrimming.CharacterEllipsis;
                Typography.SetNumeralAlignment(card.Detail, FontNumeralAlignment.Tabular);
                body.Children.Add(card.Detail);
                card.Chart = new Chart { Height = 30, Max = m.Max, Tint = m.Tint, Margin = new Thickness(-2, 8, -2, 0) };
                body.Children.Add(card.Chart);
                card.Button.Content = body;
                string key = m.Key;
                card.Button.Checked += delegate {
                    if (perfMetric == key)
                        return;
                    perfMetric = key;
                    showReading(Sensors.Latest);
                };
                metricCards.Add(card);
                PerfMetrics.Children.Add(card.Button);
            }
        }

        static TextBlock secondary(string text, double size)
        {
            var t = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.NoWrap };
            t.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
            return t;
        }

        // Live numbers keep every digit the same width, so they do not shift each second.
        static TextBlock number(double size)
        {
            var t = new TextBlock { FontSize = size, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
            Typography.SetNumeralAlignment(t, FontNumeralAlignment.Tabular);
            return t;
        }

        // A tooltip replaced by an equal one would close under the pointer.
        static void tip(FrameworkElement e, string text)
        {
            if (!Equals(e.ToolTip, text))
                e.ToolTip = text;
        }

        void showDetail(Metric m, Reading r, Reading[] history)
        {
            statIndex = 0;
            extraIndex = 0;
            PerfGpuPicker.Visibility = Visibility.Collapsed;
            PerfChart.Tint = m.Tint;
            PerfChart.Max = m.Max;
            PerfChart.Format = m.Format;
            PerfChart.Values = m.series(history);
            PerfChart.Second = m.secondSeries(history);
            PerfValue.Text = r == null ? "" : m.Text(r);
            PerfHint.Text = "";
            PerfScale.Text = "";
            if (r == null)
            {
                PerfTitle.Text = m.Name;
                PerfHint.Text = Text.get("perf.waiting");
            }
            else
                showDetails(m, r, history);
            trim(PerfStats, statIndex);
            trim(PerfExtra, extraIndex);
            summary(r == null ? null : PerfChart.Values, m.Format);
        }

        void showDetails(Metric m, Reading r, Reading[] history)
        {
            switch (m.Key)
            {
            case "cpu":
                PerfTitle.Text = Hardware.CpuName.Length > 0 ? Hardware.CpuName : m.Name;
                if (Hardware.Cores > 0)
                    PerfHint.Text = Text.format("perf.cpuHint", Hardware.Cores, Hardware.Threads);
                stat("perf.speed", Units.ghz(r.Mhz));
                if (Hardware.BaseMhz > 0)
                    stat("perf.baseSpeed", Units.ghz(Hardware.BaseMhz));
                // Said plainly rather than left out, since people look for it here.
                stat("perf.temperature", "—", Text.get("perf.cpuTemperatureHint"));
                stat("perf.processes", r.Processes.ToString("N0"));
                stat("perf.threads", r.Threads.ToString("N0"));
                stat("perf.uptime", Units.uptime(r.Uptime));
                if (r.Cores.Length > 1)
                {
                    section("perf.perThread");
                    slot("threads", () => new ThreadColumns()).show(r.Cores, m.Tint);
                }
                break;
            case "gpu":
                perfGpu = Math.Min(perfGpu, r.Gpus.Count - 1);
                GpuReading g = r.Gpus[perfGpu];
                int index = perfGpu;
                PerfChart.Values = history.Select(h => h.Gpus.Count > index ? h.Gpus[index].Load : 0).ToArray();
                PerfValue.Text = Units.percent(g.Load);
                PerfTitle.Text = g.Adapter.Name.Length > 0 ? g.Adapter.Name : m.Name;
                if (!String.IsNullOrEmpty(g.Adapter.Driver))
                    PerfHint.Text = Text.format("perf.driver", g.Adapter.Driver);
                if (r.Gpus.Count > 1)
                    showGpuPicker(r);
                stat("perf.temperature", Double.IsNaN(g.Temperature) ? "—" : Units.celsius(g.Temperature),
                     Double.IsNaN(g.Temperature) ? Text.get("perf.notReported") : null);
                if (!Double.IsNaN(g.CoreClock))
                    stat("perf.coreClock", g.CoreClock.ToString("N0") + " MHz");
                if (!Double.IsNaN(g.MemoryClock))
                    stat("perf.memoryClock", g.MemoryClock.ToString("N0") + " MHz");
                if (!Double.IsNaN(g.Fan))
                    stat("perf.fan", g.Fan.ToString("N0") + " RPM");
                if (!Double.IsNaN(g.Power))
                    stat("perf.power", Units.percent(g.Power), Text.get("perf.powerHint"));
                if (g.Adapter.Dedicated > 0)
                    stat("perf.dedicated", Units.size(g.Dedicated) + " / " + Units.size(g.Adapter.Dedicated));
                stat("perf.shared", Units.size(g.Shared) + " / " + Units.size(g.Adapter.Shared));
                // Engines that worked in the last minute, in a steady order so the rows do not jump around.
                var busy = new HashSet<string>(history.SelectMany(h => h.Gpus.Count > index ? h.Gpus[index].Engines.Where(e => e.Value >= 1).Select(e => e.Key)
                                                                                           : Enumerable.Empty<string>()));
                var engines = g.Engines.Where(e => busy.Contains(e.Key)).OrderBy(e => e.Key).Take(6).ToList();
                if (engines.Count > 0)
                    section("perf.engines");
                foreach (var e in engines)
                    slot("engine:" + e.Key, () => new BarRow(engineName(e.Key))).show(e.Value / 100, m.Tint, Units.percent(e.Value));
                break;
            case "memory":
                PerfTitle.Text = (Units.size(r.MemoryTotal) + " " + Hardware.MemoryType).Trim();
                if (Hardware.MemorySpeed > 0 && Hardware.MemorySlots > 0)
                    PerfHint.Text = Text.format("perf.memoryHint", Hardware.MemorySpeed, Hardware.MemorySlotsUsed, Hardware.MemorySlots);
                stat("perf.inUse", Units.size(r.MemoryUsed));
                stat("perf.available", Units.size(r.MemoryTotal - r.MemoryUsed));
                stat("perf.committed", Units.size(r.Committed) + " / " + Units.size(r.CommitLimit));
                break;
            case "disk":
                PerfTitle.Text = Text.get("perf.disks");
                PerfHint.Text = Text.get("perf.diskHint");
                stat("perf.read", Units.rate(r.Disks.Sum(d => d.Read)));
                stat("perf.write", Units.rate(r.Disks.Sum(d => d.Write)));
                foreach (DiskReading d in r.Disks)
                    slot("disk:" + d.Instance, () => new DiskRow()).show(d, m.Tint);
                break;
            case "network":
                PerfTitle.Text = m.Name;
                PerfHint.Text = Text.get("perf.networkHint");
                stat("perf.receiving", Units.bits(r.Down));
                stat("perf.sending", Units.bits(r.Up));
                break;
            case "battery":
                PerfTitle.Text = m.Name;
                PerfHint.Text = m.Detail(r);
                break;
            }
            PerfScale.Text = m.Format(PerfChart.Top);
        }

        static void trim(Panel panel, int count)
        {
            if (panel.Children.Count > count)
                panel.Children.RemoveRange(count, panel.Children.Count - count);
        }

        // The element at the next place in the detail's extras: the same one as a second ago when it shows the
        // same thing, or a new one.
        T slot<T>(string key, Func<T> make) where T : FrameworkElement
        {
            if (extraIndex < PerfExtra.Children.Count && PerfExtra.Children[extraIndex] is T same && Equals(same.Tag, key))
            {
                extraIndex++;
                return same;
            }
            T made = make();
            made.Tag = key;
            PerfExtra.Children.Insert(extraIndex++, made);
            return made;
        }

        void section(string key)
        {
            slot("section:" + key, () => {
                var t = new TextBlock { Text = Text.get(key), FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) };
                t.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
                return t;
            });
        }

        void stat(string key, string value, string hint = null)
        {
            StackPanel box;
            if (statIndex < PerfStats.Children.Count)
                box = (StackPanel)PerfStats.Children[statIndex];
            else
            {
                box = new StackPanel { Margin = new Thickness(0, 0, 12, 14) };
                TextBlock label = secondary("", 12);
                label.TextTrimming = TextTrimming.CharacterEllipsis;
                box.Children.Add(label);
                TextBlock text = number(15);
                text.Margin = new Thickness(0, 2, 0, 0);
                box.Children.Add(text);
                PerfStats.Children.Add(box);
            }
            statIndex++;
            ((TextBlock)box.Children[0]).Text = Text.get(key);
            ((TextBlock)box.Children[1]).Text = value;
            tip(box, hint);
        }

        // The lowest, average and highest of the minute in view, what people check first in a monitoring tool.
        void summary(IList<double> values, Func<double, string> format)
        {
            PerfSummary.Visibility = values == null || values.Count == 0 ? Visibility.Hidden : Visibility.Visible;
            if (PerfSummary.Visibility != Visibility.Visible)
                return;
            string[] keys = { "perf.min", "perf.average", "perf.max" };
            double[] parts = { values.Min(), values.Average(), values.Max() };
            for (int i = 0; i < keys.Length; i++)
            {
                if (PerfSummary.Children.Count <= i)
                {
                    var pair = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 24, 0) };
                    pair.Children.Add(secondary(Text.get(keys[i]), 12));
                    TextBlock value = number(12);
                    value.Margin = new Thickness(6, 0, 0, 0);
                    pair.Children.Add(value);
                    PerfSummary.Children.Add(pair);
                }
                ((TextBlock)((StackPanel)PerfSummary.Children[i]).Children[1]).Text = format(parts[i]);
            }
        }

        // Drivers name some engines in their own words; the common ones read better in the interface language.
        static string engineName(string engine)
        {
            string key = "perf.engine." + engine;
            string name = Text.get(key);
            return name == key ? engine.Replace('_', ' ') : name;
        }

        // A thin horizontal bar with its value beside it, and a name before it when it has one.
        sealed class BarRow : DockPanel
        {
            readonly TextBlock value = number(13);
            readonly Border track = new Border { Height = 6, CornerRadius = new CornerRadius(3), VerticalAlignment = VerticalAlignment.Center };
            readonly Border fill = new Border { CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left };
            double fraction;
            string tinted;

            public BarRow(string name = null)
            {
                if (name != null)
                {
                    Margin = new Thickness(0, 6, 0, 0);
                    var label = new TextBlock { Text = name, Width = 200, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis };
                    SetDock(label, Dock.Left);
                    Children.Add(label);
                }
                value.FontWeight = FontWeights.Normal;
                value.Width = 64;
                value.TextAlignment = TextAlignment.Right;
                SetDock(value, Dock.Right);
                Children.Add(value);
                track.SetResourceReference(Border.BackgroundProperty, "Brush.Line");
                // The fill follows the track's width, which is only known after layout.
                track.SizeChanged += delegate { fill.Width = fraction * track.ActualWidth; };
                track.Child = fill;
                Children.Add(track);
            }

            public void show(double part, string tint, string text)
            {
                fraction = Math.Max(0, Math.Min(1, part));
                fill.Width = fraction * track.ActualWidth;
                if (tint != tinted)
                    fill.SetResourceReference(Border.BackgroundProperty, "Brush.Tint." + tint);
                tinted = tint;
                value.Text = text;
            }
        }

        // A row of thin columns, one per thread, filled to its use.
        sealed class ThreadColumns : UniformGrid
        {
            const double Tall = 56;

            public ThreadColumns()
            {
                Rows = 1;
                Margin = new Thickness(0, 6, 0, 0);
            }

            public void show(double[] values, string tint)
            {
                if (Children.Count != values.Length)
                {
                    Children.Clear();
                    foreach (double v in values)
                    {
                        var track = new Border { Height = Tall, Margin = new Thickness(0, 0, 4, 0), CornerRadius = new CornerRadius(3) };
                        track.SetResourceReference(Border.BackgroundProperty, "Brush.Line");
                        var fill = new Border { CornerRadius = new CornerRadius(3), VerticalAlignment = VerticalAlignment.Bottom };
                        fill.SetResourceReference(Border.BackgroundProperty, "Brush.Tint." + tint);
                        track.Child = fill;
                        Children.Add(track);
                    }
                }
                for (int i = 0; i < values.Length; i++)
                {
                    var track = (Border)Children[i];
                    ((Border)track.Child).Height = Math.Max(2, Tall * values[i] / 100);
                    tip(track, Units.percent(values[i]));
                }
            }
        }

        // A disk with its model, its active time and what it reads and writes.
        sealed class DiskRow : DockPanel
        {
            readonly BarRow active = new BarRow();
            readonly TextBlock name = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold };
            readonly TextBlock model = secondary("", 12), rates = secondary("", 12);

            public DiskRow()
            {
                Margin = new Thickness(0, 6, 0, 6);
                var right = new StackPanel { Width = 280, Margin = new Thickness(16, 0, 0, 0) };
                SetDock(right, Dock.Right);
                right.Children.Add(active);
                Typography.SetNumeralAlignment(rates, FontNumeralAlignment.Tabular);
                right.Children.Add(rates);
                Children.Add(right);
                var left = new StackPanel();
                left.Children.Add(name);
                model.TextTrimming = TextTrimming.CharacterEllipsis;
                left.Children.Add(model);
                Children.Add(left);
            }

            public void show(DiskReading d, string tint)
            {
                name.Text = Text.format("perf.diskName", d.Number) + (d.Letters.Length > 0 ? " (" + d.Letters + ")" : "");
                Hardware.Disks.TryGetValue(d.Number, out Tuple<string, string> kind);
                model.Text = kind == null ? "" : kind.Item2.Length > 0 ? kind.Item1 + ", " + kind.Item2 : kind.Item1;
                model.Visibility = model.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                active.show(d.Active / 100, tint, Units.percent(d.Active));
                rates.Text = Text.format("perf.diskRates", Units.rate(d.Read), Units.rate(d.Write));
            }
        }

        void showGpuPicker(Reading r)
        {
            PerfGpuPicker.Visibility = Visibility.Visible;
            if (PerfGpus.Children.Count == r.Gpus.Count)
                return;
            PerfGpus.Children.Clear();
            for (int i = 0; i < r.Gpus.Count; i++)
            {
                int index = i;
                var option = new RadioButton { GroupName = "gpu", Content = "GPU " + i, ToolTip = r.Gpus[i].Adapter.Name, IsChecked = i == perfGpu };
                option.SetResourceReference(StyleProperty, "Orla.Segment");
                option.Checked += delegate {
                    perfGpu = index;
                    showReading(Sensors.Latest);
                };
                PerfGpus.Children.Add(option);
            }
        }

        // The programs using the most, grouped as Task Manager groups them, with CPU, memory and GPU side by side.
        // A column heading sorts by it.
        void showPrograms(Reading r)
        {
            if (r?.Top == null)
            {
                PerfTop.Children.Clear();
                headingSort = null;
                TextBlock waiting = secondary(Text.get("perf.waiting"), 13);
                waiting.Margin = new Thickness(0, 8, 0, 8);
                PerfTop.Children.Add(waiting);
                return;
            }
            if (headingSort != programSort)
            {
                PerfTop.Children.Clear();
                PerfTop.Children.Add(programHeading());
                headingSort = programSort;
            }
            Func<ProcessReading, double> by = ProgramColumns.First(c => c.Key == programSort).Value;
            List<ProcessReading> top = r.Top.OrderByDescending(by).ThenBy(p => p.Name).Take(8).ToList();
            for (int i = 0; i < top.Count; i++)
            {
                if (PerfTop.Children.Count <= i + 1)
                    PerfTop.Children.Add(new ProgramRow());
                ((ProgramRow)PerfTop.Children[i + 1]).show(top[i], programSort, iconFor(top[i].Path));
            }
            trim(PerfTop, top.Count + 1);
        }

        Grid programHeading()
        {
            Grid heading = ProgramRow.grid();
            heading.Margin = new Thickness(0, 0, 0, 4);
            TextBlock label = secondary(Text.get("perf.program"), 12);
            label.VerticalAlignment = VerticalAlignment.Center;
            heading.Children.Add(label);
            for (int i = 0; i < ProgramColumns.Length; i++)
            {
                string key = ProgramColumns[i].Key;
                var sort = new Button { Content = Text.get("perf." + key), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, -12, 0),
                                        FontWeight = key == programSort ? FontWeights.SemiBold : FontWeights.Normal, ToolTip = Text.get("perf.sortHint") };
                sort.SetResourceReference(StyleProperty, "Orla.Button.Quiet");
                if (key != programSort)
                    sort.SetResourceReference(ForegroundProperty, "Brush.TextSecondary");
                sort.Click += delegate {
                    programSort = key;
                    showPrograms(Sensors.Latest);
                };
                Grid.SetColumn(sort, i + 1);
                heading.Children.Add(sort);
            }
            return heading;
        }

        sealed class ProgramRow : Grid
        {
            readonly Border icon = new Border { Width = 20, Height = 20, Margin = new Thickness(0, 0, 10, 0) };
            readonly TextBlock name = new TextBlock { FontSize = 14, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            readonly TextBlock[] cells = new TextBlock[ProgramColumns.Length];
            ImageSource shown;
            bool glyph;
            string sorted;

            // The name takes what is left; the three numbers line up under their headings.
            public static Grid grid() => columns(new Grid());

            static Grid columns(Grid g)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition());
                foreach (double width in new double[] { 96, 110, 96 })
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
                return g;
            }

            public ProgramRow()
            {
                columns(this);
                Margin = new Thickness(0, 4, 0, 4);
                var left = new DockPanel();
                left.Children.Add(icon);
                left.Children.Add(name);
                Children.Add(left);
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = number(14);
                    cells[i].FontWeight = FontWeights.Normal;
                    cells[i].HorizontalAlignment = HorizontalAlignment.Right;
                    cells[i].VerticalAlignment = VerticalAlignment.Center;
                    SetColumn(cells[i], i + 1);
                    Children.Add(cells[i]);
                }
            }

            public void show(ProcessReading p, string sort, ImageSource image)
            {
                if (image == null && !glyph)
                {
                    // Programs Windows keeps private show a plain program glyph.
                    var path = new System.Windows.Shapes.Path { Data = (Geometry)Application.Current.FindResource("Glyph.ItemApp") };
                    path.SetResourceReference(StyleProperty, "Orla.Glyph");
                    icon.Child = path;
                    glyph = true;
                    shown = null;
                }
                else if (image != null && image != shown)
                {
                    icon.Child = new Image { Source = image };
                    glyph = false;
                    shown = image;
                }
                name.Text = p.Label ?? p.Name;
                tip(this, p.Path ?? p.Name);
                for (int i = 0; i < cells.Length; i++)
                {
                    double value = ProgramColumns[i].Value(p);
                    cells[i].Text = ProgramColumns[i].Key == "memory" ? Units.size(value) : value.ToString("0.0") + "%";
                    if (sort != sorted)
                        cells[i].SetResourceReference(TextBlock.ForegroundProperty, ProgramColumns[i].Key == sort ? "Brush.Text" : "Brush.TextSecondary");
                }
                sorted = sort;
            }
        }

        // Program icons are asked for once; the list refreshes every second and picks them up when they arrive.
        ImageSource iconFor(string path)
        {
            if (path == null)
                return null;
            if (programIcons.TryGetValue(path, out ImageSource known))
                return known;
            programIcons[path] = null;
            Shell.icon(path, 40, Dispatcher, image => programIcons[path] = image);
            return null;
        }
    }
}
