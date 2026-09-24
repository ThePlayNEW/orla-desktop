using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Orla
{
    // One tile in a panel: a reference (collection) or a file in the panel's folder.
    public class TileItem : INotifyPropertyChanged
    {
        string label;
        ImageSource icon;
        bool selected;

        public Entry Entry { get; set; }
        public string Path { get; set; }
        public bool IsMissing { get; set; }
        public string Key => Entry?.Id ?? Path;
        public string Tooltip => IsMissing ? Text.format("tile.missing", Path) : Shell.isVirtual(Path) ? Label : Path;

        public string Label
        {
            get => label;
            set
            {
                label = value;
                notify(nameof(Label));
            }
        }

        public ImageSource Icon
        {
            get => icon;
            set
            {
                icon = value;
                notify(nameof(Icon));
            }
        }

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                notify(nameof(Selected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class PanelView : UserControl
    {
        public const string EntryFormat = "Orla.Entries", SourceFormat = "Orla.Source";

        readonly Controller controller;
        readonly ObservableCollection<TileItem> tiles = new ObservableCollection<TileItem>();
        readonly DispatcherTimer reloadTimer;
        FileSystemWatcher[] watchers = new FileSystemWatcher[0];
        string watchedFolder;
        Point pressPoint, marqueeStart;
        TileItem pressed, anchor;
        bool moving, selecting, dropImageShown, folderAvailable = true;
        int reloadVersion;
        HashSet<TileItem> marqueeBase;
        HashSet<string> pendingSelection;

        public Group Group { get; }
        public double Scale { get; set; } = 1;
        // The most rows the host lets this panel grow to without covering another panel or leaving the screen.
        public int RoomRows { get; set; } = Group.MaxRows;
        // While the host is resizing, it sets the size directly.
        public bool Resizing { get; set; }

        public event Action MoveStarted, MoveUpdated, MoveEnded, SizeNeeded;
        public event Action<string> ResizeStarted;
        public event Action ResizeUpdated, ResizeEnded;

        public PanelView(Controller controller, Group group)
        {
            this.controller = controller;
            Group = group;
            InitializeComponent();
            Items.ItemsSource = tiles;
            applyTheme();
            Unloaded += delegate {
                Theme.Changed -= applyTheme;
                Theme.GlassChanged -= applyGlass;
            };
            Loaded += delegate {
                Theme.Changed -= applyTheme;
                Theme.GlassChanged -= applyGlass;
                Theme.Changed += applyTheme;
                Theme.GlassChanged += applyGlass;
                applyTheme();
            };
            reloadTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, delegate {
                reloadTimer.Stop();
                reload();
            }, Dispatcher);
            reloadTimer.Stop();

            Header.MouseLeftButtonDown += headerDown;
            Header.MouseMove += delegate {
                if (moving)
                    MoveUpdated?.Invoke();
            };
            Header.MouseLeftButtonUp += delegate { endMove(); };
            Header.LostMouseCapture += delegate { endMove(); };
            Header.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e) {
                openPanelMenu(null);
                e.Handled = true;
            };
            CollapseButton.Click += delegate { controller.setCollapsed(Group, !Group.Collapsed); };
            MenuButton.Click += delegate { openPanelMenu(MenuButton); };
            TitleEditor.KeyDown += titleKey;
            TitleEditor.LostKeyboardFocus += delegate { commitRename(); };
            foreach (Thumb edge in Edges.Children.OfType<Thumb>())
            {
                string side = (string)edge.Tag;
                edge.DragStarted += delegate { ResizeStarted?.Invoke(side); };
                edge.DragDelta += delegate { ResizeUpdated?.Invoke(); };
                edge.DragCompleted += delegate { ResizeEnded?.Invoke(); };
                // Double-clicking the top or bottom edge fits the height to the content again.
                if (side == "Top" || side == "Bottom")
                    edge.MouseDoubleClick += delegate { controller.setAutoHeight(Group, true); };
            }

            Items.MouseLeftButtonDown += tileDown;
            Items.PreviewMouseMove += tileMove;
            Items.MouseLeftButtonUp += tileUp;
            Items.MouseRightButtonUp += tileMenu;
            Items.KeyDown += tileKey;
            Body.MouseLeftButtonDown += marqueeDown;
            Body.MouseMove += marqueeMove;
            Body.MouseLeftButtonUp += delegate { endMarquee(); };
            Body.LostMouseCapture += delegate { endMarquee(); };
            Body.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e) {
                if (!e.Handled)
                    openPanelMenu(null);
                e.Handled = true;
            };

            // WPF reports leave and enter each time the pointer crosses a tile; the panel counts as left only when the
            // pointer is really outside it, so Windows' drag image does not flicker.
            Frame.DragEnter += (s, e) => {
                dragOver(s, e);
                if (!dropImageShown)
                    DropImages.enter(this, e.Data, e.Effects);
                dropImageShown = true;
            };
            Frame.DragOver += (s, e) => {
                dragOver(s, e);
                DropImages.over(e.Effects);
            };
            Frame.DragLeave += (s, e) => {
                Point p = e.GetPosition(Frame);
                if (p.X > 0 && p.Y > 0 && p.X < Frame.ActualWidth && p.Y < Frame.ActualHeight)
                    return;
                setDropHighlight(false);
                InsertMark.Visibility = Visibility.Collapsed;
                DropImages.leave();
                dropImageShown = false;
            };
            Frame.Drop += drop;
            MouseEnter += delegate { HeaderButtons.Opacity = 1; };
            MouseLeave += delegate {
                if (!IsKeyboardFocusWithin)
                    HeaderButtons.Opacity = 0.55;
            };
            // The view is detached and re-attached when its window is rebuilt; follow the folder only while shown.
            Loaded += delegate {
                watch();
                queueReload();
            };
            Unloaded += delegate { stopWatching(); };
            PreviewKeyDown += (s, e) => {
                if (e.Key == Key.Escape && controller.Overlay && !TitleEditor.IsVisible)
                {
                    controller.leaveOverlay();
                    e.Handled = true;
                }
                else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control && !TitleEditor.IsVisible)
                {
                    foreach (TileItem t in tiles)
                        t.Selected = true;
                    e.Handled = true;
                }
            };
            refresh();
        }

        string IconSize => controller.Layout.IconSize;

        // Brings the current palette and panel opacity into this panel's own resources.
        void applyTheme()
        {
            if (Theme.Palette == null)
                return;
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(Theme.Palette);
            applyGlass();
        }

        void applyGlass()
        {
            Resources["Brush.PanelGlass"] = Theme.Glass;
        }

        // Re-reads everything the panel shows from the layout.
        public void refresh()
        {
            Title.Text = Group.Name;
            KindGlyph.Visibility = Group.IsFolder ? Visibility.Visible : Visibility.Collapsed;
            Shoreline.SetResourceReference(Shape.StrokeProperty, "Brush.Tint." + Group.Tint);
            CollapseGlyph.Data = (Geometry)FindResource(Group.Collapsed ? "Glyph.ChevronDown" : "Glyph.ChevronUp");
            CollapseButton.ToolTip = Text.get(Group.Collapsed ? "panel.expand" : "panel.collapse");
            MenuButton.ToolTip = Text.get("panel.menu");
            Body.Visibility = Group.Collapsed ? Visibility.Collapsed : Visibility.Visible;
            Shoreline.Margin = new Thickness(6, 0, 6, Group.Collapsed ? 0 : 4);
            Edges.Visibility = controller.Layout.LockLayout ? Visibility.Collapsed : Visibility.Visible;
            Header.Cursor = controller.Layout.LockLayout ? Cursors.Arrow : Cursors.SizeAll;
            double icon = PanelMetrics.icon(IconSize);
            Resources["Tile.Icon"] = icon;
            Resources["Tile.Body"] = PanelMetrics.tile(IconSize) - 2;
            watch();
            reload();
        }

        // Items that just arrived by drag and drop stay selected, so people see where they went.
        public void selectAfterReload(IEnumerable<string> keys)
        {
            pendingSelection = new HashSet<string>(keys);
        }

        public void showSize(int columns, int rows)
        {
            SizeHint.Visibility = columns > 0 ? Visibility.Visible : Visibility.Collapsed;
            SizeText.Text = rows > 0 ? columns + " × " + rows : columns.ToString();
        }

        // Width is whole columns; height fits the content (automatic) or is the rows people chose, within the room available.
        public void applySize()
        {
            if (Resizing)
                return;
            int contentRows = Math.Max(1, (int)Math.Ceiling(tiles.Count / (double)Group.Columns));
            int rows = Math.Max(1, Math.Min(Group.AutoHeight ? contentRows : Group.Rows, Math.Min(Group.Rows, RoomRows)));
            double width = PanelMetrics.width(Group.Columns, IconSize);
            double height = Group.Collapsed ? PanelMetrics.CollapsedHeight : PanelMetrics.height(rows, IconSize);
            if (width == Width && height == Height)
                return;
            Width = width;
            Height = height;
            SizeNeeded?.Invoke();
        }

        // Reading folders and checking files happens off the UI thread: panels share their input with Explorer's
        // desktop, so a slow network drive must never make the desktop stop responding.
        void reload()
        {
            int version = ++reloadVersion;
            bool folder = Group.IsFolder, only = Group.OnlyUnorganized;
            string folderPath = Group.FolderPath;
            HashSet<string> organized = folder && only ? controller.store.organizedPaths() : null;
            List<Entry> entries = folder ? null : Group.Items.ToList();
            Task.Run(() => {
                var found = new List<TileItem>();
                bool available = true;
                if (folder)
                {
                    available = Shell.folderDirectories(folderPath).Length > 0;
                    foreach (string path in Shell.list(folderPath))
                        if (organized == null || !organized.Contains(path))
                            found.Add(new TileItem { Path = path, Label = System.IO.Path.GetFileNameWithoutExtension(path) });
                }
                else
                    foreach (Entry e in entries)
                        found.Add(new TileItem { Entry = e, Path = e.Path, Label = e.Name, IsMissing = !Shell.exists(e.Path) });
                Dispatcher.BeginInvoke(new Action(delegate {
                    if (version == reloadVersion)
                        apply(found, available);
                }));
            });
        }

        void apply(List<TileItem> next, bool available)
        {
            folderAvailable = available;
            var previous = new Dictionary<string, TileItem>(StringComparer.OrdinalIgnoreCase);
            foreach (TileItem t in tiles)
                previous[t.Key] = t;
            tiles.Clear();
            // Twice the size on screen: the shell's own downscaling is coarse, WPF's high-quality filter is not.
            int pixels = Math.Min(256, (int)Math.Ceiling(PanelMetrics.icon(IconSize) * Scale * 2));
            foreach (TileItem tile in next)
            {
                previous.TryGetValue(tile.Key, out TileItem old);
                if (old != null)
                {
                    if (old.Path == tile.Path)
                        tile.Icon = old.Icon;
                    if (Group.IsFolder)
                        tile.Label = old.Label;
                    tile.Selected = old.Selected;
                }
                if (pendingSelection != null)
                    tile.Selected = pendingSelection.Contains(tile.Key);
                tiles.Add(tile);
                TileItem target = tile;
                if (!tile.IsMissing && tile.Icon == null)
                    Shell.icon(tile.Path, pixels, Dispatcher, image => {
                        if (image != null)
                            target.Icon = image;
                    });
                if (Group.IsFolder && old == null)
                    Shell.name(tile.Path, Dispatcher, name => target.Label = name);
            }
            pendingSelection = null;
            Count.Text = tiles.Count.ToString();
            Empty.Text = Text.get(Group.IsFolder ? available ? "panel.folderEmpty" : "panel.folderMissing" : "panel.empty");
            Empty.Visibility = tiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            applySize();
        }

        // Folder panels follow their folder through file system notifications, never by polling.
        void watch()
        {
            string key = Group.IsFolder ? Group.FolderPath : null;
            if (key == watchedFolder && (key == null || watchers.Length > 0))
                return;
            stopWatching();
            watchedFolder = key;
            if (key == null)
                return;
            System.Threading.Tasks.Task.Run(() => Shell.folderDirectories(key).Where(Directory.Exists).ToList())
                .ContinueWith(t => Dispatcher.BeginInvoke(new Action(delegate {
                    if (watchedFolder == key && !t.IsFaulted)
                        startWatchers(t.Result);
                })));
        }

        void startWatchers(List<string> directories)
        {
            watchers = directories.Select(directory => {
                var w = new FileSystemWatcher(directory) {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes
                };
                FileSystemEventHandler changed = delegate { Dispatcher.BeginInvoke(new Action(queueReload)); };
                w.Created += changed;
                w.Deleted += changed;
                // A file rewritten in place (a new screenshot with the same name) needs a fresh thumbnail.
                w.Changed += (s, e) => {
                    Shell.forget(e.FullPath);
                    Dispatcher.BeginInvoke(new Action(queueReload));
                };
                w.Renamed += (s, e) => Dispatcher.BeginInvoke(new Action(queueReload));
                // A lost network connection or an overflowing buffer: start over on the next refresh.
                w.Error += delegate {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        stopWatching();
                        watch();
                        queueReload();
                    }));
                };
                w.EnableRaisingEvents = true;
                return w;
            }).ToArray();
        }

        void stopWatching()
        {
            foreach (FileSystemWatcher w in watchers)
                w.Dispose();
            watchers = new FileSystemWatcher[0];
            watchedFolder = null;
        }

        public void queueReload()
        {
            reloadTimer.Stop();
            reloadTimer.Start();
        }

        // ---- moving and renaming the panel ----

        void headerDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject d && isInside(d, HeaderButtons) || TitleEditor.IsVisible)
                return;
            if (e.ClickCount == 2)
            {
                startRename();
                e.Handled = true;
                return;
            }
            if (controller.Layout.LockLayout)
                return;
            moving = Header.CaptureMouse();
            if (moving)
                MoveStarted?.Invoke();
            e.Handled = true;
        }

        void endMove()
        {
            if (!moving)
                return;
            moving = false;
            Header.ReleaseMouseCapture();
            MoveEnded?.Invoke();
        }

        public void startRename()
        {
            TitleEditor.Text = Group.Name;
            Title.Visibility = Visibility.Collapsed;
            TitleEditor.Visibility = Visibility.Visible;
            controller.focusPanel(this);
            TitleEditor.Focus();
            TitleEditor.SelectAll();
        }

        void titleKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                commitRename();
            else if (e.Key == Key.Escape)
            {
                TitleEditor.Text = Group.Name;
                commitRename();
                e.Handled = true;
            }
        }

        void commitRename()
        {
            if (!TitleEditor.IsVisible)
                return;
            TitleEditor.Visibility = Visibility.Collapsed;
            Title.Visibility = Visibility.Visible;
            string name = TitleEditor.Text.Trim();
            if (name.Length > 0 && name != Group.Name)
                controller.renamePanel(Group, name);
        }

        // ---- selection ----

        static TileItem tileAt(object source)
        {
            for (var d = source as DependencyObject; d != null; d = VisualTreeHelper.GetParent(d))
                if (d is FrameworkElement f && f.DataContext is TileItem t)
                    return t;
            return null;
        }

        static bool insideScrollBar(DependencyObject d)
        {
            for (; d != null; d = VisualTreeHelper.GetParent(d))
                if (d is ScrollBar)
                    return true;
            return false;
        }

        static bool isInside(DependencyObject d, DependencyObject ancestor)
        {
            for (; d != null; d = VisualTreeHelper.GetParent(d))
                if (d == ancestor)
                    return true;
            return false;
        }

        List<TileItem> Selection => tiles.Where(t => t.Selected).ToList();

        void selectOnly(TileItem tile)
        {
            foreach (TileItem t in tiles)
                t.Selected = t == tile;
            anchor = tile;
        }

        // Explorer's rules: click selects one, Ctrl toggles, Shift extends from the last clicked tile.
        void clickSelect(TileItem tile)
        {
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if (shift && anchor != null && tiles.Contains(anchor))
            {
                int a = tiles.IndexOf(anchor), b = tiles.IndexOf(tile);
                for (int i = 0; i < tiles.Count; i++)
                    tiles[i].Selected = i >= Math.Min(a, b) && i <= Math.Max(a, b) || ctrl && tiles[i].Selected;
            }
            else if (ctrl)
            {
                tile.Selected = !tile.Selected;
                anchor = tile;
            }
            else if (!tile.Selected)
                selectOnly(tile);
        }

        void tileDown(object sender, MouseButtonEventArgs e)
        {
            TileItem tile = tileAt(e.OriginalSource);
            if (tile == null)
                return;
            controller.focusPanel(this);
            containerOf(tile)?.Focus();
            if (e.ClickCount == 2)
            {
                pressed = null;
                selectOnly(tile);
                controller.open(tile.Path);
            }
            else
            {
                clickSelect(tile);
                pressed = tile;
                pressPoint = e.GetPosition(this);
            }
            e.Handled = true;
        }

        void tileUp(object sender, MouseButtonEventArgs e)
        {
            // A plain click on a tile that was part of a larger selection narrows it to that tile.
            if (pressed != null && Keyboard.Modifiers == ModifierKeys.None && Selection.Count > 1)
                selectOnly(pressed);
            pressed = null;
        }

        void marqueeDown(object sender, MouseButtonEventArgs e)
        {
            if (tileAt(e.OriginalSource) != null || insideScrollBar(e.OriginalSource as DependencyObject))
                return;
            controller.focusPanel(this);
            marqueeBase = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? new HashSet<TileItem>(Selection) : new HashSet<TileItem>();
            if (marqueeBase.Count == 0)
                foreach (TileItem t in tiles)
                    t.Selected = false;
            marqueeStart = e.GetPosition(Body);
            selecting = Body.CaptureMouse();
            e.Handled = true;
        }

        void marqueeMove(object sender, MouseEventArgs e)
        {
            if (!selecting)
                return;
            Point now = e.GetPosition(Body);
            var area = new Rect(marqueeStart, now);
            Canvas.SetLeft(Marquee, area.X);
            Canvas.SetTop(Marquee, area.Y);
            Marquee.Width = area.Width;
            Marquee.Height = area.Height;
            Marquee.Visibility = Visibility.Visible;
            foreach (TileItem t in tiles)
            {
                FrameworkElement c = containerOf(t);
                if (c == null || !c.IsVisible)
                    continue;
                Rect bounds = c.TransformToAncestor(Body).TransformBounds(new Rect(0, 0, c.ActualWidth, c.ActualHeight));
                t.Selected = marqueeBase.Contains(t) || bounds.IntersectsWith(area);
            }
        }

        void endMarquee()
        {
            if (!selecting)
                return;
            selecting = false;
            Marquee.Visibility = Visibility.Collapsed;
            Body.ReleaseMouseCapture();
        }

        void tileMove(object sender, MouseEventArgs e)
        {
            if (pressed == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            Point now = e.GetPosition(this);
            if (Math.Abs(now.X - pressPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(now.Y - pressPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;
            pressed = null;
            List<TileItem> dragged = Selection;
            var data = new DataObject();
            data.SetData(SourceFormat, Group.Id);
            if (!Group.IsFolder)
                data.SetData(EntryFormat, dragged.Select(t => t.Entry.Id).ToArray());
            var files = new StringCollection();
            files.AddRange(dragged.Where(t => !Shell.isVirtual(t.Path) && Shell.exists(t.Path)).Select(t => t.Path).ToArray());
            if (files.Count > 0)
                data.SetFileDropList(files);
            // A reference never lets another program move the file away, and folders or special places can only be
            // linked, so a slip onto the desktop never copies a whole folder. Only a folder panel's own files move.
            bool filesOnly = dragged.All(t => !Shell.isVirtual(t.Path) && File.Exists(t.Path));
            DragDropEffects allowed = Group.IsFolder ? DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link
                                      : filesOnly    ? DragDropEffects.Copy | DragDropEffects.Link
                                                     : DragDropEffects.Link;
            TileItem first = dragged[0];
            var ghost = new DragGhost(first.Icon, dragged.Count > 1 ? Text.format("central.itemsCount", dragged.Count) : first.Label,
                                      dragged.Count);
            GiveFeedbackEventHandler follow = delegate { ghost.follow(); };
            GiveFeedback += follow;
            try
            {
                DragDrop.DoDragDrop(this, data, allowed);
            }
            finally
            {
                GiveFeedback -= follow;
                ghost.close();
            }
        }

        // The focusable tile border inside the generated item container.
        FrameworkElement containerOf(TileItem tile)
        {
            var presenter = Items.ItemContainerGenerator.ContainerFromItem(tile) as DependencyObject;
            return presenter != null && VisualTreeHelper.GetChildrenCount(presenter) > 0
                       ? VisualTreeHelper.GetChild(presenter, 0) as FrameworkElement
                       : null;
        }

        void tileKey(object sender, KeyEventArgs e)
        {
            TileItem tile = tileAt(e.OriginalSource);
            if (tile == null)
                return;
            List<TileItem> selection = Selection.Count > 0 ? Selection : new List<TileItem> { tile };
            switch (e.Key)
            {
            case Key.Enter:
                foreach (TileItem t in selection.Take(12))
                    controller.open(t.Path);
                break;
            case Key.F2:
                if (tile.Entry != null)
                    renameItem(tile);
                break;
            case Key.Delete:
                controller.removeItems(selection.Where(t => t.Entry != null).Select(t => t.Entry).ToList());
                break;
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                var direction = e.Key == Key.Left    ? FocusNavigationDirection.Left
                                : e.Key == Key.Right ? FocusNavigationDirection.Right
                                : e.Key == Key.Up    ? FocusNavigationDirection.Up
                                                     : FocusNavigationDirection.Down;
                if (e.OriginalSource is UIElement u && u.MoveFocus(new TraversalRequest(direction)))
                {
                    TileItem next = tileAt(Keyboard.FocusedElement);
                    if (next != null)
                        clickSelect(next);
                }
                break;
            case Key.Apps:
                showMenu(tileMenuFor(tile), e.OriginalSource as UIElement);
                break;
            default:
                return;
            }
            e.Handled = true;
        }

        void renameItem(TileItem tile)
        {
            string name = Dialog.prompt(Text.get("tile.renameTitle"), tile.Entry.Name, Text.get("tile.renameHint"));
            if (name != null)
                controller.renameItem(tile.Entry, name);
        }

        void tileMenu(object sender, MouseButtonEventArgs e)
        {
            TileItem tile = tileAt(e.OriginalSource);
            if (tile == null)
                return;
            if (!tile.Selected)
                selectOnly(tile);
            showMenu(tileMenuFor(tile), null);
            e.Handled = true;
        }

        ContextMenu tileMenuFor(TileItem tile)
        {
            List<TileItem> selection = Selection.Count > 0 ? Selection : new List<TileItem> { tile };
            bool many = selection.Count > 1;
            var entries = selection.Where(t => t.Entry != null).Select(t => t.Entry).ToList();
            var menu = new ContextMenu();
            MenuItem open = Menus.item("tile.open", "Glyph.Open", () => {
                foreach (TileItem t in selection.Take(12))
                    controller.open(t.Path);
            }, "Enter");
            if (many)
                open.Header = Text.format("tile.openMany", selection.Count);
            menu.Items.Add(open);
            if (!many && !Shell.isVirtual(tile.Path))
                menu.Items.Add(Menus.item("tile.reveal", "Glyph.Reveal", () => controller.reveal(tile.Path)));
            var collections = controller.Layout.Groups.Where(g => !g.IsFolder && g != Group).ToList();
            if (entries.Count > 0)
            {
                menu.Items.Add(new Separator());
                if (!many)
                    menu.Items.Add(Menus.item("tile.rename", "Glyph.Rename", () => renameItem(tile), "F2"));
                if (collections.Count > 0)
                {
                    MenuItem move = Menus.item("tile.moveTo", "Glyph.MoveTo", null);
                    foreach (Group g in collections)
                        move.Items.Add(Menus.plain(g.Name, () => controller.moveItems(entries.Select(x => x.Id).ToArray(), g, g.Items.Count)));
                    menu.Items.Add(move);
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("tile.remove", "Glyph.Remove", () => controller.removeItems(entries), Text.get("key.delete")));
            }
            else if (collections.Count > 0)
            {
                menu.Items.Add(new Separator());
                MenuItem add = Menus.item("tile.addTo", "Glyph.Add", null);
                foreach (Group g in collections)
                    add.Items.Add(Menus.plain(g.Name, () => controller.addReferences(g, selection.Select(t => t.Path).ToArray())));
                menu.Items.Add(add);
            }
            return menu;
        }

        void openPanelMenu(UIElement anchorElement)
        {
            var menu = new ContextMenu();
            if (Group.IsFolder)
            {
                menu.Items.Add(Menus.item("panel.openFolder", "Glyph.Open", () => controller.open(Shell.resolveFolder(Group.FolderPath))));
                if (Group.FolderPath == Shell.DesktopFolder)
                    menu.Items.Add(Menus.check("panel.onlyUnorganized", Group.OnlyUnorganized,
                                               () => controller.setOnlyUnorganized(Group, !Group.OnlyUnorganized)));
            }
            else
            {
                menu.Items.Add(Menus.item("panel.addFiles", "Glyph.Add", () => controller.addFiles(Group)));
                menu.Items.Add(Menus.item("panel.addFolder", "Glyph.PanelFolder", () => controller.addFolder(Group)));
            }
            if (tiles.Count > 0)
                menu.Items.Add(Menus.item("panel.selectAll", "Glyph.Check", () => {
                    foreach (TileItem t in tiles)
                        t.Selected = true;
                }, Text.get("key.selectAll")));
            menu.Items.Add(new Separator());
            menu.Items.Add(Menus.item("panel.rename", "Glyph.Rename", startRename));
            MenuItem tint = Menus.item("panel.tint", "Glyph.PanelCollection", null);
            foreach (string t in Tints.All)
            {
                string value = t;
                MenuItem option = Menus.check("tint." + t, Group.Tint == t, () => controller.setTint(Group, value));
                option.Icon = Menus.swatch("Brush.Tint." + t);
                tint.Items.Add(option);
            }
            menu.Items.Add(tint);
            menu.Items.Add(Menus.check("panel.autoHeight", Group.AutoHeight, () => controller.setAutoHeight(Group, !Group.AutoHeight)));
            menu.Items.Add(Menus.item(Group.Collapsed ? "panel.expand" : "panel.collapse",
                                      Group.Collapsed ? "Glyph.ChevronDown" : "Glyph.ChevronUp",
                                      () => controller.setCollapsed(Group, !Group.Collapsed)));
            menu.Items.Add(new Separator());
            menu.Items.Add(Menus.item("panel.hide", "Glyph.EyeOff", () => controller.setVisible(Group, false)));
            menu.Items.Add(Menus.item("panel.settings", "Glyph.Settings", () => controller.showCentral(null)));
            menu.Items.Add(Menus.item("panel.remove", "Glyph.Remove", () => controller.removePanel(Group)));
            showMenu(menu, anchorElement);
        }

        void showMenu(ContextMenu menu, UIElement anchorElement)
        {
            controller.focusPanel(this);
            menu.PlacementTarget = anchorElement ?? this;
            menu.Placement = anchorElement != null ? PlacementMode.Bottom : PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        // ---- drag and drop ----

        DragDropEffects effectFor(DragEventArgs e)
        {
            bool fromHere = e.Data.GetDataPresent(SourceFormat) && (string)e.Data.GetData(SourceFormat) == Group.Id;
            if (!Group.IsFolder)
            {
                if (e.Data.GetDataPresent(EntryFormat) || e.Data.GetDataPresent(DataFormats.FileDrop))
                    return (e.AllowedEffects & DragDropEffects.Link) != 0 ? DragDropEffects.Link
                                                                          : e.AllowedEffects & DragDropEffects.Copy;
                return DragDropEffects.None;
            }
            if (fromHere || !e.Data.GetDataPresent(DataFormats.FileDrop) || !folderAvailable)
                return DragDropEffects.None;
            bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) != 0;
            bool move = (e.KeyStates & DragDropKeyStates.ShiftKey) != 0;
            if (!copy && !move)
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                string folder = Shell.resolveFolder(Group.FolderPath);
                copy = paths.Length > 0 && !Shell.sameVolume(paths[0], folder);
            }
            DragDropEffects wanted = copy ? DragDropEffects.Copy : DragDropEffects.Move;
            if ((e.AllowedEffects & wanted) != 0)
                return wanted;
            return e.AllowedEffects & DragDropEffects.Copy;
        }

        void dragOver(object sender, DragEventArgs e)
        {
            e.Effects = effectFor(e);
            setDropHighlight(e.Effects != DragDropEffects.None);
            showInsertMark(e);
            autoScroll(e);
            e.Handled = true;
        }

        // A thin line between tiles shows where the items will go in a collection.
        void showInsertMark(DragEventArgs e)
        {
            InsertMark.Visibility = Visibility.Collapsed;
            if (Group.IsFolder || Group.Collapsed || e.Effects == DragDropEffects.None || tiles.Count == 0)
                return;
            int index = dropIndex(e);
            bool after = index >= tiles.Count || index >= Group.Items.Count;
            TileItem anchorTile = after ? tiles[tiles.Count - 1] : tiles.FirstOrDefault(t => t.Entry == Group.Items[index]);
            FrameworkElement c = anchorTile == null ? null : containerOf(anchorTile);
            if (c == null || !c.IsVisible)
                return;
            Rect bounds = c.TransformToAncestor(Body).TransformBounds(new Rect(0, 0, c.ActualWidth, c.ActualHeight));
            Canvas.SetLeft(InsertMark, (after ? bounds.Right : bounds.Left) - 1.5);
            Canvas.SetTop(InsertMark, bounds.Top + 8);
            InsertMark.Height = Math.Max(0, bounds.Height - 16);
            InsertMark.Visibility = Visibility.Visible;
        }

        // Dragging near the top or bottom of a panel scrolls it, so items can reach any position.
        void autoScroll(DragEventArgs e)
        {
            if (Group.Collapsed || Scroller.ScrollableHeight <= 0)
                return;
            double y = e.GetPosition(Scroller).Y, edge = 28;
            if (y < edge)
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - (edge - y) / 2);
            else if (y > Scroller.ActualHeight - edge)
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + (y - Scroller.ActualHeight + edge) / 2);
        }

        void setDropHighlight(bool on)
        {
            Frame.SetResourceReference(Border.BorderBrushProperty, on ? "Brush.Focus" : "Brush.PanelBorder");
        }

        void drop(object sender, DragEventArgs e)
        {
            setDropHighlight(false);
            InsertMark.Visibility = Visibility.Collapsed;
            DragDropEffects effect = effectFor(e);
            DropImages.drop(e.Data, effect);
            dropImageShown = false;
            e.Effects = effect;
            e.Handled = true;
            if (effect == DragDropEffects.None)
                return;
            if (!Group.IsFolder)
            {
                int index = dropIndex(e);
                if (e.Data.GetDataPresent(EntryFormat))
                    controller.moveItems((string[])e.Data.GetData(EntryFormat), Group, index);
                else
                    controller.addReferences(Group, (string[])e.Data.GetData(DataFormats.FileDrop), index);
                return;
            }
            // Orla performs the move or copy itself, after the drag has finished, and reports no effect to the source,
            // so the source never deletes originals for a move that was skipped or cancelled.
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = DragDropEffects.None;
            Dispatcher.BeginInvoke(new Action(() => controller.transfer(Group, paths, effect == DragDropEffects.Copy)));
        }

        int dropIndex(DragEventArgs e)
        {
            TileItem target = tileAt(Items.InputHitTest(e.GetPosition(Items)));
            if (target?.Entry == null)
                return Group.Items.Count;
            int index = Group.Items.IndexOf(target.Entry);
            FrameworkElement f = containerOf(target);
            if (f != null && e.GetPosition(f).X > f.ActualWidth / 2)
                index++;
            return index;
        }
    }
}
