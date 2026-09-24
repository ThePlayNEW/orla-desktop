using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace Orla
{
    // Tells "Keep organized" what arrives on the desktop and what leaves it: items at the top level and inside the
    // folders of shortcuts the organizer sorted. It waits a moment after the last change, so a download or a copy has
    // finished and a new folder has its name. Event-driven: no polling, and nothing reaches the UI thread until a batch
    // is ready, so a busy folder on the desktop never slows the desktop down.
    public class DesktopArrivals : IDisposable
    {
        const int MaxPending = 500;
        static readonly string[] partial = { ".crdownload", ".part", ".partial", ".tmp", ".download", ".opdownload" };

        readonly Dispatcher dispatcher;
        readonly Func<string[]> roots;
        readonly Timer settle;
        readonly HashSet<string> pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        volatile bool wanted, disposed;

        // The paths that changed. Each one may exist or be gone; the receiver looks.
        public event Action<List<string>> Changed;

        public DesktopArrivals(Dispatcher dispatcher, Func<string[]> roots = null)
        {
            this.dispatcher = dispatcher;
            this.roots = roots ?? Shell.desktopDirectories;
            settle = new Timer(delegate { deliver(); });
        }

        public bool Running => wanted;

        // Besides the desktops themselves, the folders whose new items count as arriving on the desktop.
        public void follow(IEnumerable<string> sortedFolders)
        {
            folders = new HashSet<string>(sortedFolders.Select(f => f.TrimEnd('\\')), StringComparer.OrdinalIgnoreCase);
        }

        // Starting a watcher can block on a redirected or network desktop, so it happens off the UI thread, and the
        // UI thread never waits for it. The task completes once the watchers are listening.
        public System.Threading.Tasks.Task start()
        {
            if (wanted)
                return System.Threading.Tasks.Task.FromResult(0);
            wanted = true;
            return System.Threading.Tasks.Task.Run(() => {
                var created = new List<FileSystemWatcher>();
                foreach (string root in roots())
                {
                    try
                    {
                        var w = new FileSystemWatcher(root) { IncludeSubdirectories = true,
                                                              NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName };
                        string top = root.TrimEnd('\\');
                        w.Created += (s, e) => note(top, e.FullPath);
                        w.Deleted += (s, e) => note(top, e.FullPath);
                        w.Renamed += (s, e) => {
                            note(top, e.OldFullPath);
                            note(top, e.FullPath);
                        };
                        w.EnableRaisingEvents = true;
                        created.Add(w);
                    }
                    catch (Exception)
                    {
                        // A desktop that cannot be watched simply is not kept organized.
                    }
                }
                lock (pending)
                {
                    if (wanted && !disposed && watchers.Count == 0)
                    {
                        watchers = created;
                        return;
                    }
                }
                foreach (FileSystemWatcher w in created)
                    w.Dispose();
            });
        }

        public void stop()
        {
            wanted = false;
            List<FileSystemWatcher> old;
            lock (pending)
            {
                old = watchers;
                watchers = new List<FileSystemWatcher>();
                pending.Clear();
            }
            // Disposing waits for the watcher's own thread, never for the disk, but keep it off the UI thread anyway.
            if (old.Count > 0)
                System.Threading.Tasks.Task.Run(() => old.ForEach(w => w.Dispose()));
        }

        void note(string root, string path)
        {
            string parent = Path.GetDirectoryName(path)?.TrimEnd('\\');
            if (parent == null || !wanted || !(parent.Equals(root, StringComparison.OrdinalIgnoreCase) || folders.Contains(parent)))
                return;
            string name = Path.GetFileName(path);
            if (name.Length == 0 || name.StartsWith("~$") || name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                partial.Any(p => name.EndsWith(p, StringComparison.OrdinalIgnoreCase)))
                return;
            lock (pending)
                if (pending.Count < MaxPending)
                    pending.Add(path);
            settle.Change(2000, Timeout.Infinite);
        }

        void deliver()
        {
            List<string> paths;
            lock (pending)
            {
                paths = pending.ToList();
                pending.Clear();
            }
            if (paths.Count > 0 && wanted)
                dispatcher.BeginInvoke(new Action(() => Changed?.Invoke(paths)));
        }

        public void Dispose()
        {
            disposed = true;
            stop();
            settle.Dispose();
        }
    }
}
