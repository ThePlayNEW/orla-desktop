using System.Globalization;
using System.Linq;
using System.Threading;
using Xunit;

namespace Orla.Tests
{
    public class PerformanceTests
    {
        public PerformanceTests()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        }

        [Fact]
        public void sizesAndRatesReadLikeWindows()
        {
            Assert.Equal("512 B", Units.size(512));
            Assert.Equal("1,5 KB", Units.size(1536));
            Assert.Equal("16 GB", Units.size(16.0 * (1L << 30)));
            Assert.Equal("27,5 GB", Units.size(27.5 * (1L << 30)));
            Assert.Equal("2,5 MB/s", Units.rate(2.5 * (1 << 20)));
            // Connections count bits in thousands.
            Assert.Equal("20 Mbps", Units.bits(2.5e6));
            Assert.Equal("800 bps", Units.bits(100));
            Assert.Equal("3%", Units.percent(2.5));
        }

        [Fact]
        public void upTimeShowsDaysOnlyAfterTheFirstDay()
        {
            Assert.Equal("3:25:59", Units.uptime(3 * 3600 + 25 * 60 + 59));
            Assert.Equal("2:01:00:00", Units.uptime(2 * 86400 + 3600));
        }

        [Fact]
        public void chartsOfRatesScaleToRoundNumbers()
        {
            Assert.Equal(1000, Units.ceiling(10, 1000));
            Assert.Equal(2000, Units.ceiling(1500, 1));
            Assert.Equal(5e6, Units.ceiling(3.1e6, 1));
            Assert.Equal(10, Units.ceiling(10, 1));
        }

        [Fact]
        public void metricsAppearOnlyWhereTheComputerHasThem()
        {
            var desktop = new Reading();
            Assert.DoesNotContain(Metric.All.Where(m => m.Available(desktop)), m => m.Key == "gpu" || m.Key == "battery");
            var laptop = new Reading { Battery = 80 };
            laptop.Gpus.Add(new GpuReading { Adapter = new GpuAdapter { Name = "GPU" }, Load = 40, Temperature = 61 });
            Assert.Equal(6, Metric.All.Count(m => m.Available(laptop)));
            Assert.Equal("61 °C", Metric.find("gpu").Detail(laptop));
        }

        [Fact]
        public void theSensorsReadThisComputer()
        {
            Sensors.use(true);
            try
            {
                for (int i = 0; i < 150 && Sensors.Latest == null; i++)
                    Thread.Sleep(100);
                Reading r = Sensors.Latest;
                Assert.NotNull(r);
                Assert.InRange(r.Cpu, 0, 100);
                Assert.True(r.MemoryTotal > 0 && r.MemoryUsed > 0 && r.MemoryUsed <= r.MemoryTotal);
                Assert.NotEmpty(r.Cores);
            }
            finally
            {
                Sensors.release(true);
            }
        }

        [Fact]
        public void performancePanelsHoldNoFilesAndSurviveStartingOver()
        {
            Group panel = Presets.performance();
            Assert.True(panel.IsSensors);
            Assert.Equal(Metric.Usual > panel.Columns ? 2 : 1, PanelMetrics.plannedRows(panel));
            var store = new Store(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "orla-perf-" + System.Guid.NewGuid().ToString("N"), "layout.json"));
            store.data = new Layout();
            store.data.Groups.Add(panel);
            Assert.False(store.add(panel, System.Environment.SystemDirectory));
            Layout fresh = Organizer.build(new Organizer.Plan(), store.data, false, false,
                                           new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 1920, Bottom = 1040 } });
            Assert.Contains(fresh.Groups, g => g.Id == panel.Id);
        }
    }
}
