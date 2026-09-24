using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using Xunit;

namespace Orla.Tests
{
    public sealed class ArrivalsTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "Orla-arrivals-" + Guid.NewGuid().ToString("N"));
        readonly Dispatcher dispatcher;
        readonly Thread thread;

        public ArrivalsTests()
        {
            Directory.CreateDirectory(Path.Combine(root, "Shortcuts", "Games"));
            Directory.CreateDirectory(Path.Combine(root, "Work", "Deep"));
            Dispatcher d = null;
            var ready = new ManualResetEventSlim();
            thread = new Thread(() => {
                d = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            }) { IsBackground = true };
            thread.Start();
            ready.Wait();
            dispatcher = d;
        }

        public void Dispose()
        {
            dispatcher.InvokeShutdown();
            Directory.Delete(root, true);
        }

        [Fact]
        public void reportsNewItemsOnTheDesktopAndInSortedFoldersOnly()
        {
            var batches = new List<List<string>>();
            var got = new ManualResetEventSlim();
            var arrivals = new DesktopArrivals(dispatcher, () => new[] { root });
            arrivals.follow(new[] { Path.Combine(root, "Shortcuts", "Games") });
            arrivals.Changed += paths => {
                lock (batches)
                    batches.Add(paths);
                got.Set();
            };
            Assert.True(arrivals.start().Wait(10000));
            File.WriteAllText(Path.Combine(root, "Notas.txt"), "");
            File.WriteAllText(Path.Combine(root, "Shortcuts", "Games", "Jogo.url"), "");
            File.WriteAllText(Path.Combine(root, "Work", "Deep", "build.log"), "");
            File.WriteAllText(Path.Combine(root, "video.mp4.crdownload"), "");
            Assert.True(got.Wait(20000));
            arrivals.Dispose();
            List<string> first;
            lock (batches)
                first = batches[0];
            Assert.Contains(Path.Combine(root, "Notas.txt"), first);
            Assert.Contains(Path.Combine(root, "Shortcuts", "Games", "Jogo.url"), first);
            Assert.DoesNotContain(Path.Combine(root, "Work", "Deep", "build.log"), first);
            Assert.DoesNotContain(Path.Combine(root, "video.mp4.crdownload"), first);
        }
    }
}
