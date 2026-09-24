using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace Orla
{
    // Follows the folders that hold collection items, so a reference keeps pointing at its file when the file is
    // renamed in File Explorer and shows as missing when it is deleted. Event-driven: no polling.
    public class ReferenceWatch : IDisposable
    {
        const int MaxFolders = 64;
        readonly Dispatcher dispatcher;
        bool disposed;
        readonly Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        public event Action<string, string> Renamed;
        public event Action Changed;

        public ReferenceWatch(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        // Checking folders can block on a network path, so it happens off the UI thread.
        public void follow(IEnumerable<string> paths)
        {
            var candidates = paths.Where(p => !Shell.isVirtual(p)).Select(p => Path.GetDirectoryName(p))
                                 .Where(d => !String.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxFolders).ToList();
            System.Threading.Tasks.Task.Run(() => {
                var existing = candidates.Where(Directory.Exists).ToList();
                lock (watchers)
                    if (!disposed)
                        apply(existing);
            });
        }

        void apply(List<string> folders)
        {
            foreach (string gone in watchers.Keys.Except(folders, StringComparer.OrdinalIgnoreCase).ToList())
            {
                watchers[gone].Dispose();
                watchers.Remove(gone);
            }
            foreach (string folder in folders.Where(f => !watchers.ContainsKey(f)))
            {
                try
                {
                    var w = new FileSystemWatcher(folder) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName };
                    w.Renamed += (s, e) => dispatcher.BeginInvoke(new Action(() => Renamed?.Invoke(e.OldFullPath, e.FullPath)));
                    w.Deleted += delegate { dispatcher.BeginInvoke(new Action(() => Changed?.Invoke())); };
                    w.Created += delegate { dispatcher.BeginInvoke(new Action(() => Changed?.Invoke())); };
                    w.EnableRaisingEvents = true;
                    watchers[folder] = w;
                }
                catch (Exception)
                {
                    // A folder on a disconnected drive or without permission is simply not followed.
                }
            }
        }

        public void Dispose()
        {
            lock (watchers)
            {
                disposed = true;
                foreach (FileSystemWatcher w in watchers.Values)
                    w.Dispose();
                watchers.Clear();
            }
        }
    }
}
