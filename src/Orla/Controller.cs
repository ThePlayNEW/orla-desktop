using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
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
        readonly DispatcherTimer rebuildTimer, referenceTimer;
        readonly ReferenceWatch references;
        readonly DesktopArrivals arrivals;
        readonly List<PanelHost> hosts = new List<PanelHost>();
        readonly bool interactive;
        Desktop desktop;
        IconGuard guard;
        Tray tray;
        CentralWindow central;
        bool exiting;

        public Layout Layout => store.data;
        public Dispatcher Dispatcher => dispatcher;
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
            messages.Hotkey += onHotkey;
            messages.ExplorerRestarted += queueRebuild;
            messages.DisplayChanged += delegate {
                dispatcher.BeginInvoke(new Action(delegate {
                    refreshAll();
                    settleAll();
                }), DispatcherPriority.Background);
            };
            Dialog.OwnerHandle = messages.Handle;
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
            references = new ReferenceWatch(dispatcher);
            references.Renamed += followRename;
            referenceTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Background, delegate {
                referenceTimer.Stop();
                foreach (PanelHost h in hosts.Where(h => !h.Group.IsFolder))
                    h.View.queueReload();
            }, dispatcher);
            referenceTimer.Stop();
            references.Changed += delegate {
                referenceTimer.Stop();
                referenceTimer.Start();
            };
            arrivals = new DesktopArrivals(dispatcher);
            arrivals.Changed += keepOrganized;
        }

        public void start()
        {
            if (interactive)
            {
                tray = new Tray(this, messages.Handle);
                if (Layout.OverlayHotkey && !messages.setHotkey(true, Shortcut))
                    tray.notify(Text.format("notice.hotkeyTaken", Shortcut.display()));
                // Also points an existing entry at this copy, so an older copy never starts with a newer layout.
                if (!Layout.StartupConfigured || StartupEnabled)
                    tryAction(() => setStartup(true));
                if (Layout.AutoUpdate)
                    Updates.start(this);
            }
            if (!Layout.Welcomed)
            {
                showCentral("welcome");
                return;
            }
            if (Organizer.retitle(Layout))
                saveLayout();
            rebuild();
            if (store.recoveryNotice != null)
                tray?.notify(store.recoveryNotice);
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
            followReferences();
            followArrivals();
            central?.refresh();
        }

        PanelMode baseMode => DesktopFound ? PanelMode.Desktop : PanelMode.Fallback;

        void showHosts()
        {
            foreach (PanelHost h in hosts)
                h.show(Overlay ? PanelMode.Overlay : baseMode, desktop, !Hidden);
            settleAll();
        }

        public bool Hidden { get; private set; }

        // The shortcut does what the moment calls for: with the desktop in view it hides or shows the panels; with an
        // application on top it brings the panels in front, and pressing it again sends them back.
        void onHotkey()
        {
            if (!Layout.Welcomed)
                return;
            if (Overlay)
                setOverlay(false);
            else if (Hidden)
                setHidden(false);
            else if (desktopInView())
                setHidden(true);
            else
                setOverlay(true);
        }

        static bool desktopInView()
        {
            // Orla's own window counts as an application in front; panels on the desktop report Explorer's windows.
            IntPtr foreground = Native.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
                return true;
            string c = Native.windowClass(foreground);
            return c == "Progman" || c == "WorkerW" || c == "Shell_TrayWnd" || c == "Shell_SecondaryTrayWnd";
        }

        // Hiding the panels shows the ordinary desktop, Windows icons included, until they come back.
        public void setHidden(bool value)
        {
            if (Hidden == value)
                return;
            Hidden = value;
            if (value && Overlay)
                setOverlay(false);
            foreach (PanelHost h in hosts)
                h.setVisible(!value);
            if (Layout.CleanDesktop && guard != null)
            {
                if (value)
                    guard.restore();
                else
                    guard.hide();
            }
            tray?.refreshMenu();
            central?.refresh();
        }

        // Panels never overlap: each one, in order, moves off any panel placed before it.
        void settleAll()
        {
            foreach (PanelHost h in hosts)
                h.settle();
        }

        void followReferences()
        {
            references.follow(Layout.Groups.Where(g => !g.IsFolder).SelectMany(g => g.Items).Select(e => e.Path));
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
                host.show(Overlay ? PanelMode.Overlay : baseMode, desktop, !Hidden);
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
            if (on && Hidden)
            {
                Hidden = false;
                applyCleanDesktop();
            }
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
            // After Explorer restarts the icon list is a new window, and it needs a new guard. The old one lets go
            // first, so a companion still starting can never hide the new icons.
            if (guard == null || guard.Target.IconList != desktop.IconList)
            {
                guard?.restore();
                guard = new IconGuard(desktop, DataDirectory, dispatcher);
                guard.Failed += delegate { tray?.notify(Text.get("error.guard")); };
            }
            // While the panels are hidden by the shortcut, the Windows icons stay visible even with a clean desktop.
            if (Layout.CleanDesktop && interactive && !Hidden)
                guard.hide();
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
            followReferences();
            central?.refresh();
        }

        // A collection item follows its file, or the folder it lives in, when it is renamed in File Explorer.
        void followRename(string oldPath, string newPath)
        {
            // An incomplete notification from an overflowing buffer carries no new name; the next reload shows the truth.
            if (String.IsNullOrEmpty(Path.GetFileName(newPath)) || String.IsNullOrEmpty(Path.GetFileName(oldPath)))
                return;
            var touched = new HashSet<Group>();
            foreach (Group g in Layout.Groups.Where(g => !g.IsFolder))
                foreach (Entry e in g.Items)
                {
                    bool same = String.Equals(e.Path, oldPath, StringComparison.OrdinalIgnoreCase);
                    bool inside = !same && Shell.within(e.Path, oldPath);
                    if (!same && !inside)
                        continue;
                    if (same && (e.Name == Path.GetFileNameWithoutExtension(oldPath) || e.Name == Path.GetFileName(oldPath)))
                        e.Name = Directory.Exists(newPath) ? Path.GetFileName(newPath) : Path.GetFileNameWithoutExtension(newPath);
                    e.Path = same ? newPath : newPath + e.Path.Substring(oldPath.Length);
                    touched.Add(g);
                }
            if (touched.Count == 0)
                return;
            saveLayout();
            foreach (PanelHost h in hosts.Where(h => touched.Contains(h.Group)))
                h.View.queueReload();
            followReferences();
        }

        // ---- items ----

        public void open(string path)
        {
            tryAction(() => Shell.open(path));
            leaveOverlay();
        }

        public void reveal(string path)
        {
            tryAction(() => Shell.reveal(path));
            leaveOverlay();
        }

        // Sends the panels back to the desktop once the input event that asked for it has finished, since that
        // event is still being handled by a window about to be rebuilt.
        public void leaveOverlay()
        {
            if (Overlay)
                dispatcher.BeginInvoke(new Action(() => setOverlay(false)));
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

        public void moveItems(string[] entryIds, Group target, int index)
        {
            bool moved = false;
            foreach (string id in entryIds)
            {
                Group from = store.owner(id);
                int before = from == target ? from.Items.FindIndex(e => e.Id == id) : -1;
                if (!store.move(id, target, index))
                    continue;
                moved = true;
                if (before < 0 || before >= index)
                    index++;
            }
            if (moved)
            {
                hosts.FirstOrDefault(h => h.Group == target)?.View.selectAfterReload(entryIds);
                changed();
            }
        }

        public void removeItems(IList<Entry> entries)
        {
            bool removed = false;
            foreach (Entry e in entries)
                removed |= store.remove(e.Id);
            if (removed)
                changed();
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
            // Runs on its own thread, owned by Orla's hidden window: Windows' progress and conflict dialogs never
            // block the panels, and a dialog owned by a panel would disable Explorer's desktop window.
            IntPtr owner = messages.Handle;
            var list = sources.ToList();
            var worker = new System.Threading.Thread(delegate() {
                try
                {
                    Shell.transfer(owner, list, folder, copy);
                }
                catch (Exception e)
                {
                    dispatcher.BeginInvoke(new Action(() => Dialog.alert(Text.get("error.title"), e.Message)));
                }
            }) { IsBackground = true, Name = "Orla file operation" };
            worker.SetApartmentState(System.Threading.ApartmentState.STA);
            worker.Start();
        }

        public void addFiles(Group g)
        {
            setOverlay(false);
            using (var dialog = new Forms.OpenFileDialog { Multiselect = true, Title = Text.get("panel.addFiles"),
                                                           DereferenceLinks = false })
                if (dialog.ShowDialog(new Owner(messages.Handle)) == Forms.DialogResult.OK)
                    addReferences(g, dialog.FileNames);
        }

        public void addFolder(Group g)
        {
            setOverlay(false);
            string folder = pickFolder(Text.get("panel.addFolderHint"));
            if (folder != null)
                addReferences(g, new[] { folder });
        }

        public string pickFolder(string description)
        {
            using (var dialog = new Forms.FolderBrowserDialog { Description = description, ShowNewFolderButton = true })
                return dialog.ShowDialog(new Owner(messages.Handle)) == Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        sealed class Owner : Forms.IWin32Window
        {
            public Owner(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }

        // ---- panels ----

        public Group createPanel(string kind, string name, string folder)
        {
            return addPanel(new Group { Kind = kind, Name = name, FolderPath = folder,
                                        Tint = Tints.All[Layout.Groups.Count % Tints.All.Length] });
        }

        // Presets that scan shortcuts do their reading off the UI thread.
        public void createFromPreset(Preset preset)
        {
            System.Threading.Tasks.Task.Run(preset.Create).ContinueWith(t => dispatcher.BeginInvoke(new Action(delegate {
                if (!t.IsFaulted)
                    addPanel(t.Result);
            })));
        }

        // A new panel appears in the middle of the main screen, or at the nearest free spot.
        Group addPanel(Group g)
        {
            if (Layout.Groups.Count >= Store.MaxGroups)
            {
                Dialog.alert(Text.get("error.title"), Text.format("error.tooManyPanels", Store.MaxGroups));
                return null;
            }
            Screen s = Screens.primary();
            int w = (int)(PanelMetrics.width(g.Columns, Layout.IconSize) * s.Scale);
            int h = (int)(PanelMetrics.height(Math.Min(g.Rows, 2), Layout.IconSize) * s.Scale);
            var spot = new RECT { Left = s.Work.Left + (s.Work.Width - w) / 2, Top = s.Work.Top + (s.Work.Height - h) / 3 };
            spot.Right = spot.Left + w;
            spot.Bottom = spot.Top + h;
            spot = Screens.free(spot, hosts.Select(x => x.Rect).ToList());
            g.X = spot.Left;
            g.Y = spot.Top;
            Layout.Groups.Add(g);
            changed(g);
            return g;
        }

        public void renamePanel(Group g, string name)
        {
            g.Name = name;
            g.TitleKey = null;
            changed(g);
        }

        public void setTint(Group g, string tint)
        {
            g.Tint = tint;
            changed(g);
        }

        public void setAutoHeight(Group g, bool value)
        {
            g.AutoHeight = value;
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
            Organizer.arrange(Layout, Screens.primary(), true);
            changed();
            settleAll();
            saveLayout();
        }

        // ---- settings ----

        // Saved when the slider is released, not on every step of a drag.
        public void setOpacity(double value)
        {
            Layout.Opacity = value;
            Theme.setOpacity(value);
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
            settleAll();
            saveLayout();
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

        public Shortcut Shortcut => Orla.Shortcut.parse(Layout.OverlayShortcut);

        // Changes the key combination; the previous one stays if Windows refuses the new one.
        public bool setShortcut(Shortcut shortcut)
        {
            if (!Layout.OverlayHotkey || messages.setHotkey(true, shortcut))
            {
                Layout.OverlayShortcut = shortcut.ToString();
                saveLayout();
                tray?.refreshMenu();
                return true;
            }
            messages.setHotkey(true, Shortcut);
            return false;
        }

        // While the shortcut recorder listens, the current combination must reach it instead of Windows.
        public void pauseHotkey(bool paused)
        {
            messages.setHotkey(!paused && Layout.OverlayHotkey, Shortcut);
        }

        public bool setHotkey(bool value)
        {
            bool ok = messages.setHotkey(value, Shortcut);
            Layout.OverlayHotkey = value && ok;
            saveLayout();
            return ok;
        }

        // Applies a new language right away: panels and this window are rebuilt with the new text.
        public void setAutoUpdate(bool value)
        {
            Layout.AutoUpdate = value;
            saveLayout();
            if (value)
                Updates.start(this);
        }

        // Closes everything like quit, for an update that restarts Orla itself.
        // Lets go of the desktop: icons back, panels and watchers closed. Quitting and restarting for an update share it.
        public void closeForUpdate()
        {
            exiting = true;
            guard?.restore();
            watch.Dispose();
            references.Dispose();
            arrivals.Dispose();
            foreach (PanelHost h in hosts)
                h.Dispose();
            hosts.Clear();
            tray?.Dispose();
            messages.Dispose();
        }

        public void notifyUpdate(string version)
        {
            tray?.notify(Text.format("notice.updateReady", version));
            central?.refresh();
        }

        public void setLanguage(string value)
        {
            Layout.Language = value;
            Text.load(value);
            Organizer.retitle(Layout);
            saveLayout();
            if (Layout.Welcomed)
                rebuild();
            if (central != null)
            {
                CentralWindow old = central;
                central = null;
                showCentral("general");
                old.Close();
            }
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", RunValue = "Orla Desktop";

        public bool StartupEnabled
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                    return key?.GetValue(RunValue) != null;
            }
        }

        public void setStartup(bool enabled)
        {
            removeStartup();
            if (enabled)
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                    key.SetValue(RunValue, "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
            Layout.StartupConfigured = true;
            Layout.StartupEnabled = enabled;
            store.save();
        }

        // Also used when Orla is uninstalled.
        public static void removeStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                key?.DeleteValue(RunValue, false);
        }

        // Applies the choice made on the welcome screen and puts the first panels on the desktop.
        public void welcome(IList<Preset> presets)
        {
            System.Threading.Tasks.Task.Run(() => {
                Layout next = Layout.copySettings();
                foreach (Preset p in presets)
                    next.Groups.Add(p.Create());
                return next;
            }).ContinueWith(t => dispatcher.BeginInvoke(new Action(delegate {
                if (t.IsFaulted)
                    return;
                store.data = t.Result;
                Organizer.arrange(Layout, Screens.primary());
                Layout.Welcomed = true;
                saveLayout();
                rebuild();
            })));
        }

        // ---- let Orla organize ----

        Layout beforeOrganize;

        // The panels from before the last organize: in memory until Orla closes, and after that from the copy kept on disk.
        public bool CanUndoOrganize => beforeOrganize != null || UndoBackup != null && File.Exists(UndoBackup);

        string UndoBackup => Layout.UndoBackup == null ? null : Path.Combine(DataDirectory, Path.GetFileName(Layout.UndoBackup));

        public bool Organized => Layout.Groups.Any(g => g.AutoCategory != null);

        // Replaces the panels with the organizer's plan. The panels it replaces stay in a copy next to the layout and,
        // until Orla closes, one click away.
        public void applyOrganized(Layout next)
        {
            beforeOrganize = null;
            if (Layout.Welcomed && Layout.Groups.Count > 0)
            {
                next.UndoBackup = Path.GetFileName(backupLayout("before-organize"));
                beforeOrganize = store.data;
            }
            store.data = next;
            saveLayout();
            rebuild();
        }

        // Straight to the organizer's preview, from the notification area.
        public void showOrganize()
        {
            showCentral(null);
            central?.showOrganize(false);
        }

        public void undoOrganize()
        {
            string backup = UndoBackup;
            Layout previous = beforeOrganize;
            if (previous == null && backup != null)
                try
                {
                    previous = Store.parse(File.ReadAllText(backup));
                }
                catch (Exception)
                {
                    // An unreadable copy offers nothing to go back to.
                }
            beforeOrganize = null;
            Layout.UndoBackup = null;
            // The copy is used once, so the button does not offer the same step again.
            if (backup != null && File.Exists(backup))
                tryAction(() => File.Delete(backup));
            if (previous == null)
            {
                central?.refresh();
                return;
            }
            // Settings changed since then stay; only the panels and how the desktop looks go back.
            Layout current = Layout;
            current.Groups = previous.Groups;
            current.CleanDesktop = previous.CleanDesktop;
            current.AutoOrganize = previous.AutoOrganize;
            current.SortedFolders = previous.SortedFolders;
            saveLayout();
            rebuild();
        }

        public void setAutoOrganize(bool on)
        {
            Layout.AutoOrganize = on;
            saveLayout();
            followArrivals();
            central?.refresh();
        }

        void followArrivals()
        {
            arrivals.follow(Layout.SortedFolders);
            if (interactive && Layout.Welcomed && Layout.AutoOrganize && Organized)
                arrivals.start();
            else if (arrivals.Running)
                arrivals.stop();
        }

        // "Keep organized": something new on the desktop joins the panel for its kind, and a desktop item that is gone
        // leaves the panels the organizer made. What fits nowhere stays in the desktop inbox. The watcher only reports
        // the desktop itself and the folders of shortcuts the organizer sorted; disk and shell work runs off the UI thread.
        void keepOrganized(List<string> paths)
        {
            if (!Layout.AutoOrganize)
                return;
            var auto = Layout.Groups.Where(g => !g.IsFolder && g.AutoCategory != null).ToList();
            Organized organized = store.organized();
            System.Threading.Tasks.Task.Run(() => paths.Select(p => {
                bool exists = Shell.exists(p);
                bool fresh = exists && !Shell.isHidden(p) && !organized.Contains(p);
                return new { Path = p, Gone = !exists, Home = fresh ? Organizer.home(p, auto) : null,
                             Name = fresh ? Shell.displayName(p) : null };
            }).ToList()).ContinueWith(t => dispatcher.BeginInvoke(new Action(delegate {
                if (t.IsFaulted || !Layout.AutoOrganize)
                    return;
                bool any = false;
                var grown = new Dictionary<Group, int>();
                foreach (var r in t.Result)
                {
                    if (r.Gone)
                    {
                        // A deleted folder of shortcuts takes the shortcuts sorted from it along.
                        foreach (Group g in Layout.Groups.Where(g => g.AutoCategory != null && !g.IsFolder))
                            any |= g.Items.RemoveAll(e => Shell.within(e.Path, r.Path)) > 0;
                    }
                    else if (r.Home != null && Layout.Groups.Contains(r.Home) && r.Home.Items.Count < Store.MaxItems &&
                             !Layout.Groups.Any(g => g.Items.Any(e => String.Equals(e.Path, r.Path, StringComparison.OrdinalIgnoreCase))))
                    {
                        r.Home.Items.Add(new Entry { Name = r.Name, Path = r.Path });
                        any = true;
                        int rows = (r.Home.Items.Count + r.Home.Columns - 1) / r.Home.Columns;
                        grown.TryGetValue(r.Home, out int already);
                        if (r.Home.AutoHeight && rows > r.Home.Rows && roomBelow(r.Home, already + 1))
                        {
                            r.Home.Rows++;
                            grown[r.Home] = already + 1;
                        }
                    }
                }
                if (any)
                {
                    changed();
                    settleAll();
                }
            })));
        }

        // Whether a panel can grow by more rows and still keep the gap to the panel below and the screen edge; if not,
        // its new items scroll.
        bool roomBelow(Group g, int rows)
        {
            PanelHost host = hosts.FirstOrDefault(h => h.Group == g);
            if (host == null || g.Collapsed || g.Rows >= 6)
                return false;
            RECT r = host.Rect;
            Screen screen = Screens.forRect(r);
            r.Bottom += (int)Math.Round(rows * PanelMetrics.tile(Layout.IconSize) * screen.Scale) + Screens.Margin;
            return r.Bottom <= screen.Work.Bottom && !hosts.Any(o => o != host && Screens.overlaps(r, o.Rect));
        }

        // The copy's path, or null when there was nothing to copy or the copy failed.
        string backupLayout(string reason)
        {
            string copy = store.filePath + "." + reason + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            try
            {
                if (!File.Exists(store.filePath))
                    return null;
                File.Copy(store.filePath, copy, true);
                return copy;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return null;
        }

        // Back to the first run, as after a fresh install: panels and settings return to their defaults and the welcome
        // screen opens. A copy of the current layout is kept next to it, and no file on the desktop is touched.
        // Starting with Windows stays as it is.
        public void resetToWelcome()
        {
            backupLayout("before-reset");
            beforeOrganize = null;
            arrivals.stop();
            Overlay = false;
            Hidden = false;
            guard?.restore();
            foreach (PanelHost h in hosts)
                h.Dispose();
            hosts.Clear();
            var fresh = new Layout { StartupConfigured = true, StartupEnabled = StartupEnabled };
            store.data = fresh;
            saveLayout();
            messages.setHotkey(fresh.OverlayHotkey, Shortcut);
            Text.load(fresh.Language);
            Theme.apply(fresh);
            followReferences();
            if (central != null)
            {
                CentralWindow old = central;
                central = null;
                old.Close();
            }
            showCentral("welcome");
        }

        // ---- windows ----

        public void showCentral(string page)
        {
            setOverlay(false);
            if (central == null)
            {
                CentralWindow window = central = new CentralWindow(this);
                window.Closed += delegate {
                    if (central == window)
                        central = null;
                };
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
            Updates.applyOnExit();
            closeForUpdate();
            Application.Current.Shutdown();
        }

        // Restores the original desktop without closing the panels' data.
        public void restoreIcons()
        {
            guard?.restore();
        }
    }
}
