using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Orla
{
    // Application lifetime: finds the desktop, builds the panels, switches them between the desktop and the front,
    // follows Explorer and Windows settings, and applies every change people make.
    public class Controller
    {
        public readonly Store store;
        readonly Dispatcher dispatcher;
        readonly MessageWindow messages;
        readonly DesktopWatch watch;
        readonly DispatcherTimer rebuildTimer;
        readonly List<PanelHost> hosts = new List<PanelHost>();
        readonly bool interactive;
        Desktop desktop;
        IconGuard guard;
        Tray tray;
        CentralWindow central;
        bool exiting;

        public Layout Layout => store.data;
        public IReadOnlyList<PanelHost> Hosts => hosts;
        public bool Overlay { get; private set; }
        public bool DesktopFound => (desktop ?? (desktop = Desktop.find())).IsValid;
        public string DataDirectory => Path.GetDirectoryName(store.filePath);

        public Controller(Store store, bool interactive)
        {
            this.store = store;
            this.interactive = interactive;
            dispatcher = Dispatcher.CurrentDispatcher;
            messages = new MessageWindow();
            messages.Hotkey += delegate { setOverlay(!Overlay); };
            messages.ExplorerRestarted += queueRebuild;
            messages.DisplayChanged += delegate { dispatcher.BeginInvoke(new Action(refreshAll), DispatcherPriority.Background); };
            messages.SettingsChanged += delegate {
                Theme.apply(Layout);
                tray?.refreshIcon();
            };
            watch = new DesktopWatch(dispatcher);
            watch.Lost += queueRebuild;
            watch.Reordered += delegate {
                foreach (PanelHost h in hosts)
                    h.keepAbove();
            };
            rebuildTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(800), DispatcherPriority.Background, delegate {
                rebuildTimer.Stop();
                rebuild();
            }, dispatcher);
            rebuildTimer.Stop();
        }

        public void start()
        {
            if (interactive)
            {
                tray = new Tray(this, messages.Handle);
                if (Layout.OverlayHotkey && !messages.setHotkey(true))
                    tray.notify(Text.get("notice.hotkeyTaken"));
                // Also points an existing shortcut at this copy, so an older version never starts with a newer layout.
                if (!Layout.StartupConfigured || StartupEnabled)
                    tryAction(() => setStartup(true));
            }
            if (!Layout.Welcomed)
            {
                showCentral("welcome");
                return;
            }
            rebuild();
            if (store.recoveryNotice != null)
                tray?.notify(store.recoveryNotice);
            else if (store.migrated)
                tray?.notify(Text.get("notice.migrated"));
        }

        // ---- panels on the desktop ----

        void queueRebuild()
        {
            rebuildTimer.Stop();
            rebuildTimer.Start();
        }

        // (Re)creates every panel window. Runs at start, after Explorer restarts, and when a wallpaper app moves the
        // desktop icon view to another window.
        void rebuild()
        {
            if (exiting)
                return;
            desktop = Desktop.find();
            watch.follow(desktop);
            foreach (PanelHost h in hosts)
                h.Dispose();
            hosts.Clear();
            foreach (Group g in Layout.Groups.Where(g => g.Visible))
                hosts.Add(new PanelHost(this, g));
            showHosts();
            applyCleanDesktop();
            central?.refresh();
        }

        PanelMode baseMode => DesktopFound ? PanelMode.Desktop : PanelMode.Fallback;

        void showHosts()
        {
            foreach (PanelHost h in hosts)
                h.show(Overlay ? PanelMode.Overlay : baseMode, desktop);
        }

        void syncHosts()
        {
            foreach (PanelHost h in hosts.Where(h => !h.Group.Visible || !Layout.Groups.Contains(h.Group)).ToList())
            {
                h.Dispose();
                hosts.Remove(h);
            }
            foreach (Group g in Layout.Groups.Where(g => g.Visible && hosts.All(h => h.Group != g)))
            {
                var host = new PanelHost(this, g);
                hosts.Add(host);
                host.show(Overlay ? PanelMode.Overlay : baseMode, desktop);
            }
        }

        void refreshAll()
        {
            foreach (PanelHost h in hosts)
                h.refresh();
        }

        public void setOverlay(bool on)
        {
            if (on == Overlay || !Layout.Welcomed)
                return;
            Overlay = on;
            showHosts();
            if (on)
                hosts.FirstOrDefault()?.focus();
            tray?.refreshMenu();
        }

        public void focusPanel(PanelView view)
        {
            hosts.FirstOrDefault(h => h.View == view)?.focus();
        }

        void applyCleanDesktop()
        {
            if (!DesktopFound)
                return;
            if (guard == null || !Native.IsWindow(desktop.IconList))
                guard = new IconGuard(desktop, DataDirectory);
            if (Layout.CleanDesktop && interactive)
                tryAction(guard.hide);
            else
            {
                guard.restore();
                if (interactive)
                    guard.repair();
            }
        }

        // ---- saving and refreshing ----

        public void saveLayout()
        {
            tryAction(store.save);
        }

        public void changed(Group group = null)
        {
            saveLayout();
            syncHosts();
            foreach (PanelHost h in hosts)
                if (group == null || h.Group == group || h.Group.IsFolder && h.Group.OnlyUnorganized)
                    h.refresh();
            central?.refresh();
        }

        // ---- items ----

        public void open(string path)
        {
            tryAction(() => Shell.open(path));
            if (Overlay)
                setOverlay(false);
        }

        public void reveal(string path)
        {
            tryAction(() => Shell.reveal(path));
            if (Overlay)
                setOverlay(false);
        }

        public void addReferences(Group g, IEnumerable<string> paths, int index = -1)
        {
            int before = g.Items.Count;
            foreach (string p in paths)
                store.add(g, p);
            if (index >= 0 && index < before)
            {
                var added = g.Items.Skip(before).ToList();
                g.Items.RemoveRange(before, added.Count);
                g.Items.InsertRange(index, added);
            }
            changed(g);
        }

        public void moveItem(string entryId, Group target, int index)
        {
            Group from = store.owner(entryId);
            if (store.move(entryId, target, index))
                changed(from == target ? target : null);
        }

        public void moveItem(Entry entry, Group target)
        {
            moveItem(entry.Id, target, target.Items.Count);
        }

        public void removeItem(Entry entry)
        {
            Group from = store.owner(entry.Id);
            if (store.remove(entry.Id))
                changed(from);
        }

        public void renameItem(Entry entry, string name)
        {
            entry.Name = name;
            changed(store.owner(entry.Id));
        }

        public void transfer(Group g, string[] paths, bool copy)
        {
            string folder = Shell.resolveFolder(g.FolderPath);
            var sources = paths.Where(p => !String.Equals(Path.GetDirectoryName(p), folder, StringComparison.OrdinalIgnoreCase));
            IntPtr owner = hosts.FirstOrDefault(h => h.Group == g)?.Handle ?? IntPtr.Zero;
            tryAction(() => Shell.transfer(owner, sources, folder, copy));
        }

        public void addFiles(Group g)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Title = Text.get("panel.addFiles"),
                                                              DereferenceLinks = false };
            if (dialog.ShowDialog() == true)
                addReferences(g, dialog.FileNames);
        }

        public void addFolder(Group g)
        {
            string folder = pickFolder(Text.get("panel.addFolderHint"));
            if (folder != null)
                addReferences(g, new[] { folder });
        }

        public static string pickFolder(string description)
        {
            using (var dialog = new Forms.FolderBrowserDialog { Description = description, ShowNewFolderButton = true })
                return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        // ---- panels ----

        public Group createPanel(string kind, string name, string folder)
        {
            if (Layout.Groups.Count >= Store.MaxGroups)
            {
                Dialog.alert(Text.get("error.title"), Text.format("error.tooManyPanels", Store.MaxGroups));
                return null;
            }
            var g = new Group { Kind = kind, Name = name, FolderPath = folder,
                                Tint = Tints.All[Layout.Groups.Count % Tints.All.Length] };
            Screen s = Screens.primary();
            g.X = s.Work.Left + (s.Work.Width - g.Width * s.Scale) / 2;
            g.Y = s.Work.Top + (s.Work.Height - g.Height * s.Scale) / 3;
            Layout.Groups.Add(g);
            changed(g);
            return g;
        }

        public void renamePanel(Group g, string name)
        {
            g.Name = name;
            changed(g);
        }

        public void setTint(Group g, string tint)
        {
            g.Tint = tint;
            changed(g);
        }

        public void setCollapsed(Group g, bool collapsed)
        {
            g.Collapsed = collapsed;
            changed(g);
        }

        public void setVisible(Group g, bool visible)
        {
            g.Visible = visible;
            changed(g);
            if (!visible)
                tray?.notify(Text.format("notice.hidden", g.Name));
        }

        public void setOnlyUnorganized(Group g, bool value)
        {
            g.OnlyUnorganized = value;
            changed(g);
        }

        public void removePanel(Group g)
        {
            string message = Text.get(g.IsFolder ? "panel.removeFolderMessage" : "panel.removeMessage");
            if (!Dialog.confirm(Text.format("panel.removeTitle", g.Name), message, Text.get("panel.removeAction")))
                return;
            Layout.Groups.Remove(g);
            changed();
        }

        public void resetPositions()
        {
            Screens.arrange(Layout.Groups);
            changed();
        }

        // ---- settings ----

        public void setOpacity(double value, bool save)
        {
            Layout.Opacity = value;
            Theme.setOpacity(value);
            if (save)
                saveLayout();
        }

        public void setTheme(string value)
        {
            Layout.Theme = value;
            Theme.apply(Layout);
            saveLayout();
        }

        public void setIconSize(string value)
        {
            Layout.IconSize = value;
            changed();
        }

        public void setAnimations(bool value)
        {
            Layout.Animations = value;
            saveLayout();
        }

        public void setLock(bool value)
        {
            Layout.LockLayout = value;
            changed();
            tray?.refreshMenu();
        }

        public void setCleanDesktop(bool value)
        {
            Layout.CleanDesktop = value;
            saveLayout();
            applyCleanDesktop();
            tray?.refreshMenu();
            central?.refresh();
        }

        public bool setHotkey(bool value)
        {
            bool ok = messages.setHotkey(value);
            Layout.OverlayHotkey = value && ok;
            saveLayout();
            return ok;
        }

        public void setLanguage(string value)
        {
            Layout.Language = value;
            saveLayout();
        }

        public static string StartupShortcut =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Orla.lnk");

        public bool StartupEnabled => File.Exists(StartupShortcut);

        public void setStartup(bool enabled)
        {
            if (enabled)
            {
                Type type = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(type);
                dynamic shortcut = shell.CreateShortcut(StartupShortcut);
                try
                {
                    string exe = Process.GetCurrentProcess().MainModule.FileName;
                    shortcut.TargetPath = exe;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exe);
                    shortcut.Description = "Orla Desktop";
                    shortcut.Save();
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
            }
            else if (File.Exists(StartupShortcut))
                File.Delete(StartupShortcut);
            Layout.StartupConfigured = true;
            Layout.StartupEnabled = enabled;
            store.save();
        }

        // Applies the choice made on the welcome screen and puts the first panels on the desktop.
        public void welcome(bool organize)
        {
            store.data = organize ? Starter.organized(Layout) : Starter.alongside(Layout);
            Screens.arrange(Layout.Groups);
            Layout.Welcomed = true;
            saveLayout();
            rebuild();
        }

        // ---- windows ----

        public void showCentral(string page)
        {
            if (central == null)
            {
                central = new CentralWindow(this);
                central.Closed += delegate { central = null; };
            }
            central.show(page);
        }

        public static void applyWindowTheme(Window w)
        {
            IntPtr h = new WindowInteropHelper(w).Handle;
            Native.setDwm(h, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, Theme.IsDark ? 1 : 0);
        }

        public void tryAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Dialog.alert(Text.get("error.title"), e.Message);
            }
        }

        public void quit()
        {
            if (exiting)
                return;
            exiting = true;
            guard?.restore();
            watch.Dispose();
            foreach (PanelHost h in hosts)
                h.Dispose();
            hosts.Clear();
            tray?.Dispose();
            messages.Dispose();
            Application.Current.Shutdown();
        }

        // Restores the original desktop without closing the panels' data.
        public void restoreIcons()
        {
            guard?.restore();
        }
    }
}
