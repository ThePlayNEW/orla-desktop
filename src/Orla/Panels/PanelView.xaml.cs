using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
        public const string EntryFormat = "Orla.Entry", SourceFormat = "Orla.Source";
        public const double CollapsedHeight = 50;

        readonly Controller controller;
        readonly ObservableCollection<TileItem> tiles = new ObservableCollection<TileItem>();
        readonly DispatcherTimer reloadTimer;
        FileSystemWatcher[] watchers = new FileSystemWatcher[0];
        string watchedFolder;
        Point pressPoint;
        TileItem pressed;
        bool moving;

        public Group Group { get; }
        public double Scale { get; set; } = 1;

        public event Action MoveStarted, MoveUpdated, MoveEnded;
        public event Action<double, double> Resized;
        public event Action ResizeEnded;

        public PanelView(Controller controller, Group group)
        {
            this.controller = controller;
            Group = group;
            InitializeComponent();
            Items.ItemsSource = tiles;
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
            Grip.DragDelta += (s, e) => Resized?.Invoke(e.HorizontalChange, e.VerticalChange);
            Grip.DragCompleted += delegate { ResizeEnded?.Invoke(); };

            Items.MouseLeftButtonDown += tileDown;
            Items.PreviewMouseMove += tileMove;
            Items.MouseLeftButtonUp += delegate { pressed = null; };
            Items.MouseRightButtonUp += tileMenu;
            Items.KeyDown += tileKey;
            Body.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e) {
                if (!e.Handled)
                    openPanelMenu(null);
                e.Handled = true;
            };

            Frame.DragEnter += dragOver;
            Frame.DragOver += dragOver;
            Frame.DragLeave += delegate { setDropHighlight(false); };
            Frame.Drop += drop;
            MouseEnter += delegate { fadeHeader(1); };
            MouseLeave += delegate {
                if (!IsKeyboardFocusWithin)
                    fadeHeader(0.55);
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
                    controller.setOverlay(false);
                    e.Handled = true;
                }
            };
            refresh();
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
            Grip.Visibility = Group.Collapsed || controller.Layout.LockLayout ? Visibility.Collapsed : Visibility.Visible;
            Header.Cursor = controller.Layout.LockLayout ? Cursors.Arrow : Cursors.SizeAll;
            double icon = controller.Layout.IconSize == "small" ? 32 : controller.Layout.IconSize == "large" ? 48 : 40;
            Resources["Tile.Icon"] = icon;
            Resources["Tile.Width"] = icon + 52;
            Width = Group.Width;
            Height = Group.Collapsed ? CollapsedHeight : Group.Height;
            watch();
            reload();
        }

        void reload()
        {
            string selected = tiles.FirstOrDefault(t => t.Selected)?.Key;
            var next = new List<TileItem>();
            if (Group.IsFolder)
            {
                HashSet<string> organized = Group.OnlyUnorganized ? controller.store.organizedPaths() : null;
                foreach (string path in Shell.list(Group.FolderPath))
                    if (organized == null || !organized.Contains(path))
                        next.Add(new TileItem { Path = path, Label = System.IO.Path.GetFileNameWithoutExtension(path) });
            }
            else
                foreach (Entry e in Group.Items)
                    next.Add(new TileItem { Entry = e, Path = e.Path, Label = e.Name, IsMissing = !Shell.exists(e.Path) });

            var previous = tiles.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
            tiles.Clear();
            int pixels = (int)Math.Ceiling((double)Resources["Tile.Icon"] * Scale);
            foreach (TileItem tile in next)
            {
                if (previous.TryGetValue(tile.Key, out TileItem old))
                {
                    tile.Icon = old.Icon;
                    if (Group.IsFolder)
                        tile.Label = old.Label;
                }
                tile.Selected = tile.Key == selected;
                tiles.Add(tile);
                TileItem target = tile;
                if (!tile.IsMissing)
                    Shell.icon(tile.Path, pixels, Dispatcher, image => {
                        if (image != null)
                            target.Icon = image;
                    });
                if (Group.IsFolder && old == null)
                    Shell.name(tile.Path, Dispatcher, name => target.Label = name);
            }
            Count.Text = tiles.Count.ToString();
            Empty.Text = Text.get(Group.IsFolder ? Shell.folderDirectories(Group.FolderPath).Length == 0
                                                     ? "panel.folderMissing"
                                                     : "panel.folderEmpty"
                                                 : "panel.empty");
            Empty.Visibility = tiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Folder panels follow their folder through file system notifications, never by polling.
        void watch()
        {
            string key = Group.IsFolder ? Group.FolderPath : null;
            if (key == watchedFolder)
                return;
            stopWatching();
            watchedFolder = key;
            if (key == null)
                return;
            watchers = Shell.folderDirectories(key).Select(directory => {
                var w = new FileSystemWatcher(directory) {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes
                };
                FileSystemEventHandler changed = delegate { Dispatcher.BeginInvoke(new Action(queueReload)); };
                w.Created += changed;
                w.Deleted += changed;
                w.Changed += changed;
                w.Renamed += (s, e) => Dispatcher.BeginInvoke(new Action(queueReload));
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

        void queueReload()
        {
            reloadTimer.Stop();
            reloadTimer.Start();
        }

        void fadeHeader(double opacity)
        {
            HeaderButtons.Opacity = opacity;
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

        // ---- tiles ----

        static TileItem tileAt(object source)
        {
            for (var d = source as DependencyObject; d != null; d = VisualTreeHelper.GetParent(d))
                if (d is FrameworkElement f && f.DataContext is TileItem t)
                    return t;
            return null;
        }

        static bool isInside(DependencyObject d, DependencyObject ancestor)
        {
            for (; d != null; d = VisualTreeHelper.GetParent(d))
                if (d == ancestor)
                    return true;
            return false;
        }

        void select(TileItem tile)
        {
            foreach (TileItem t in tiles)
                t.Selected = t == tile;
        }

        void tileDown(object sender, MouseButtonEventArgs e)
        {
            TileItem tile = tileAt(e.OriginalSource);
            if (tile == null)
                return;
            select(tile);
            controller.focusPanel(this);
            containerOf(tile)?.Focus();
            if (e.ClickCount == 2)
            {
                pressed = null;
                controller.open(tile.Path);
            }
            else
            {
                pressed = tile;
                pressPoint = e.GetPosition(this);
            }
            e.Handled = true;
        }

        void tileMove(object sender, MouseEventArgs e)
        {
            if (pressed == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            Point now = e.GetPosition(this);
            if (Math.Abs(now.X - pressPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(now.Y - pressPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;
            TileItem tile = pressed;
            pressed = null;
            var data = new DataObject();
            data.SetData(SourceFormat, Group.Id);
            if (tile.Entry != null)
                data.SetData(EntryFormat, tile.Entry.Id);
            if (!Shell.isVirtual(tile.Path) && Shell.exists(tile.Path))
                data.SetFileDropList(new System.Collections.Specialized.StringCollection { tile.Path });
            // A reference never lets another program move the file away; only a folder panel's own files can move.
            DragDropEffects allowed = Group.IsFolder
                                          ? DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link
                                          : DragDropEffects.Copy | DragDropEffects.Link;
            DragDrop.DoDragDrop(this, data, allowed);
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
            switch (e.Key)
            {
            case Key.Enter:
                controller.open(tile.Path);
                break;
            case Key.F2:
                if (tile.Entry != null)
                    renameItem(tile);
                break;
            case Key.Delete:
                if (tile.Entry != null)
                    controller.removeItem(tile.Entry);
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
                    select(tileAt(Keyboard.FocusedElement));
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
            select(tile);
            showMenu(tileMenuFor(tile), null);
            e.Handled = true;
        }

        ContextMenu tileMenuFor(TileItem tile)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Menus.item("tile.open", "Glyph.Open", () => controller.open(tile.Path), "Enter"));
            if (!Shell.isVirtual(tile.Path))
                menu.Items.Add(Menus.item("tile.reveal", "Glyph.Reveal", () => controller.reveal(tile.Path)));
            var collections = controller.Layout.Groups.Where(g => !g.IsFolder && g != Group).ToList();
            if (tile.Entry != null)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("tile.rename", "Glyph.Rename", () => renameItem(tile), "F2"));
                if (collections.Count > 0)
                {
                    MenuItem move = Menus.item("tile.moveTo", "Glyph.MoveTo", null);
                    foreach (Group g in collections)
                        move.Items.Add(Menus.plain(g.Name, () => controller.moveItem(tile.Entry, g)));
                    menu.Items.Add(move);
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("tile.remove", "Glyph.Remove", () => controller.removeItem(tile.Entry), "Del"));
            }
            else if (collections.Count > 0)
            {
                menu.Items.Add(new Separator());
                MenuItem add = Menus.item("tile.addTo", "Glyph.Add", null);
                foreach (Group g in collections)
                    add.Items.Add(Menus.plain(g.Name, () => controller.addReferences(g, new[] { tile.Path })));
                menu.Items.Add(add);
            }
            return menu;
        }

        void openPanelMenu(UIElement anchor)
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
            menu.Items.Add(Menus.item(Group.Collapsed ? "panel.expand" : "panel.collapse",
                                      Group.Collapsed ? "Glyph.ChevronDown" : "Glyph.ChevronUp",
                                      () => controller.setCollapsed(Group, !Group.Collapsed)));
            menu.Items.Add(new Separator());
            menu.Items.Add(Menus.item("panel.hide", "Glyph.EyeOff", () => controller.setVisible(Group, false)));
            menu.Items.Add(Menus.item("panel.settings", "Glyph.Settings", () => controller.showCentral(null)));
            menu.Items.Add(Menus.item("panel.remove", "Glyph.Remove", () => controller.removePanel(Group)));
            showMenu(menu, anchor);
        }

        void showMenu(ContextMenu menu, UIElement anchor)
        {
            controller.focusPanel(this);
            menu.PlacementTarget = anchor ?? this;
            menu.Placement = anchor != null ? PlacementMode.Bottom : PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        // ---- drag and drop ----

        DragDropEffects effectFor(DragEventArgs e)
        {
            bool fromOrla = e.Data.GetDataPresent(SourceFormat);
            if (fromOrla && (string)e.Data.GetData(SourceFormat) == Group.Id && Group.IsFolder)
                return DragDropEffects.None;
            if (!Group.IsFolder)
            {
                if (e.Data.GetDataPresent(EntryFormat) || e.Data.GetDataPresent(DataFormats.FileDrop))
                    return (e.AllowedEffects & DragDropEffects.Link) != 0 ? DragDropEffects.Link
                                                                          : e.AllowedEffects & DragDropEffects.Copy;
                return DragDropEffects.None;
            }
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || Shell.folderDirectories(Group.FolderPath).Length == 0)
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
            e.Handled = true;
        }

        void setDropHighlight(bool on)
        {
            if (on)
                Frame.SetResourceReference(Border.BorderBrushProperty, "Brush.Focus");
            else
                Frame.SetResourceReference(Border.BorderBrushProperty, "Brush.PanelBorder");
        }

        void drop(object sender, DragEventArgs e)
        {
            setDropHighlight(false);
            DragDropEffects effect = effectFor(e);
            e.Effects = effect;
            e.Handled = true;
            if (effect == DragDropEffects.None)
                return;
            if (!Group.IsFolder)
            {
                int index = dropIndex(e);
                if (e.Data.GetDataPresent(EntryFormat))
                    controller.moveItem((string)e.Data.GetData(EntryFormat), Group, index);
                else
                    controller.addReferences(Group, (string[])e.Data.GetData(DataFormats.FileDrop), index);
                return;
            }
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            controller.transfer(Group, paths, effect == DragDropEffects.Copy);
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
