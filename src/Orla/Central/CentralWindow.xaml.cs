using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Orla
{
    // Orla's own window: panels, appearance, general settings, about, and the welcome screen on first run.
    public partial class CentralWindow : Window
    {
        public const string Repository = "https://github.com/ThePlayNEW/orla-desktop";
        readonly Controller controller;
        PanelView preview;
        bool loading, recording;
        Organizer.Plan plan;
        bool organizeFromWelcome;

        public CentralWindow(Controller controller)
        {
            this.controller = controller;
            InitializeComponent();
            SourceInitialized += delegate { Controller.applyWindowTheme(this); };

            NavPanels.Checked += delegate { showPage(PagePanels); };
            NavAppearance.Checked += delegate { showPage(PageAppearance); };
            NavGeneral.Checked += delegate { showPage(PageGeneral); };
            NavAbout.Checked += delegate { showPage(PageAbout); };
            FrontButton.Click += delegate { controller.setOverlay(!controller.Overlay); refresh(); };

            NewPanel.Click += delegate { openPresetMenu(); };
            ThemeSystem.Checked += delegate { onChange(() => controller.setTheme("system")); };
            ThemeLight.Checked += delegate { onChange(() => controller.setTheme("light")); };
            ThemeDark.Checked += delegate { onChange(() => controller.setTheme("dark")); };
            OpacitySlider.ValueChanged += delegate {
                OpacityValue.Text = Math.Round(OpacitySlider.Value * 100) + "%";
                onChange(() => controller.setOpacity(OpacitySlider.Value, false));
            };
            OpacitySlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(delegate { controller.saveLayout(); }));
            OpacitySlider.LostKeyboardFocus += delegate { controller.saveLayout(); };
            IconSmall.Checked += delegate { onChange(() => setIconSize("small")); };
            IconMedium.Checked += delegate { onChange(() => setIconSize("medium")); };
            IconLarge.Checked += delegate { onChange(() => setIconSize("large")); };
            AnimationsSwitch.Click += delegate { controller.setAnimations(AnimationsSwitch.IsChecked == true); };

            StartupSwitch.Click += delegate {
                controller.tryAction(() => controller.setStartup(StartupSwitch.IsChecked == true));
                StartupSwitch.IsChecked = controller.StartupEnabled;
            };
            CleanSwitch.Click += delegate { controller.setCleanDesktop(CleanSwitch.IsChecked == true); };
            HotkeySwitch.Click += delegate {
                if (!controller.setHotkey(HotkeySwitch.IsChecked == true))
                {
                    HotkeySwitch.IsChecked = false;
                    Dialog.alert(Text.get("general.hotkey"), Text.format("notice.hotkeyTaken", controller.Shortcut.display()));
                }
            };
            LockSwitch.Click += delegate { controller.setLock(LockSwitch.IsChecked == true); };
            ShortcutButton.Click += delegate { startRecording(); };
            ShortcutButton.LostKeyboardFocus += delegate { stopRecording(); };
            ShortcutReset.Click += delegate {
                stopRecording();
                if (!controller.setShortcut(Shortcut.parse(Shortcut.Default)))
                    Dialog.alert(Text.get("general.shortcut"), Text.format("notice.hotkeyTaken", Shortcut.parse(Shortcut.Default).display()));
                refresh();
            };
            PreviewKeyDown += recordKey;
            LanguageButton.Click += delegate {
                var menu = new ContextMenu { PlacementTarget = LanguageButton, Placement = PlacementMode.Bottom };
                menu.Items.Add(Menus.check("language.system", controller.Layout.Language == "system", () => controller.setLanguage("system")));
                menu.Items.Add(new Separator());
                foreach (string language in Text.Languages)
                {
                    string value = language;
                    MenuItem item = Menus.plain(Text.nativeName(language), () => controller.setLanguage(value));
                    item.IsChecked = controller.Layout.Language == language;
                    menu.Items.Add(item);
                }
                menu.IsOpen = true;
            };
            UpdateSwitch.Click += delegate { controller.setAutoUpdate(UpdateSwitch.IsChecked == true); };
            UpdateButton.Click += delegate { Updates.restartNow(controller); };
            ArrangeButton.Click += delegate { controller.resetPositions(); };
            ResetButton.Click += delegate {
                if (Dialog.confirm(Text.get("general.resetConfirmTitle"), Text.get("general.resetConfirmMessage"),
                                   Text.get("general.resetConfirmAction")))
                    controller.resetToWelcome();
            };

            DataButton.Click += delegate { controller.open(controller.DataDirectory); };
            GuideButton.Click += delegate { browse(Repository + (Text.Language == "pt-BR" ? "/blob/main/docs/pt-BR/guia.md" : "/blob/main/docs/en/guide.md")); };
            IssueButton.Click += delegate { browse(Repository + "/issues/new/choose"); };
            QuitButton.Click += delegate { controller.quit(); };
            StartButton.Click += delegate {
                if (ChoiceOrganize.IsChecked == true)
                    showOrganize(true);
                else
                    showPresetChoices();
            };
            OrganizeButton.Click += delegate { showOrganize(false); };
            OrganizeClean.Click += delegate { drawPlan(false); };
            OrganizeStage.SizeChanged += delegate {
                bool animate = pendingAnimation;
                pendingAnimation = false;
                drawPlan(animate);
            };
            OrganizeKeep.Click += delegate { drawPlan(false); };
            OrganizeBack.Click += delegate {
                OrganizePreview.Visibility = Visibility.Collapsed;
                if (organizeFromWelcome)
                    Welcome.Visibility = Visibility.Visible;
            };
            OrganizeApply.Click += delegate {
                Layout next = planned();
                plan = null;
                OrganizePreview.Visibility = Visibility.Collapsed;
                Welcome.Visibility = Visibility.Collapsed;
                controller.applyOrganized(next);
                Main.Visibility = Visibility.Visible;
                NavPanels.IsChecked = true;
                refresh();
            };
            KeepSwitch.Click += delegate { controller.setAutoOrganize(KeepSwitch.IsChecked == true); };
            OrganizeUndo.Click += delegate {
                controller.undoOrganize();
                refresh();
            };
            BackButton.Click += delegate {
                WelcomePresets.Visibility = Visibility.Collapsed;
                Welcome.Visibility = Visibility.Visible;
            };
            FinishButton.Click += delegate {
                var chosen = PresetChoices.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => (Preset)c.Tag).ToList();
                finishWelcome(chosen);
            };

            VersionText.Text = Text.format("about.version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            DataPath.Text = controller.DataDirectory;
            refresh();
        }

        public void show(string page)
        {
            // Opening a page from the tray or the panels leaves the preview; its Back button would lead nowhere useful.
            if (page != null)
                OrganizePreview.Visibility = Visibility.Collapsed;
            bool welcome = page == "welcome" || !controller.Layout.Welcomed;
            if (page == "welcome")
                WelcomePresets.Visibility = Visibility.Collapsed;
            if (WelcomePresets.Visibility != Visibility.Visible)
                Welcome.Visibility = welcome ? Visibility.Visible : Visibility.Collapsed;
            Main.Visibility = welcome ? Visibility.Collapsed : Visibility.Visible;
            if (page == "general")
                NavGeneral.IsChecked = true;
            else if (!welcome && Nav.Children.OfType<RadioButton>().All(r => r.IsChecked != true))
                NavPanels.IsChecked = true;
            Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }

        void showPage(UIElement page)
        {
            foreach (UIElement p in new UIElement[] { PagePanels, PageAppearance, PageGeneral, PageAbout })
                p.Visibility = p == page ? Visibility.Visible : Visibility.Collapsed;
            if (page == PageAppearance)
                buildPreview();
        }

        void finishWelcome(List<Preset> presets)
        {
            controller.welcome(presets);
            Welcome.Visibility = Visibility.Collapsed;
            WelcomePresets.Visibility = Visibility.Collapsed;
            Main.Visibility = Visibility.Visible;
            NavPanels.IsChecked = true;
            refresh();
        }

        // ---- let Orla organize ----

        internal void showOrganize(bool fromWelcome, Func<Organizer.Plan> source = null)
        {
            organizeFromWelcome = fromWelcome;
            // A plan is used once: after Apply its panels are the live ones, and drawing it again would move them.
            plan = null;
            Welcome.Visibility = Visibility.Collapsed;
            OrganizePreview.Visibility = Visibility.Visible;
            OrganizeClean.IsChecked = true;
            OrganizeKeep.IsChecked = true;
            OrganizeMap.Children.Clear();
            OrganizeApply.IsEnabled = false;
            OrganizeLead.Text = Text.get("organize.reading");
            OrganizeNote.Text = !fromWelcome && controller.Layout.Groups.Count > 0 ? Text.get("organize.replaceNote") : "";
            Task.Run(source ?? Organizer.plan).ContinueWith(t => Dispatcher.BeginInvoke(new Action(delegate {
                if (t.IsFaulted)
                {
                    OrganizeLead.Text = Text.get("organize.failed");
                    return;
                }
                plan = t.Result;
                OrganizeApply.IsEnabled = true;
                drawPlan(true);
            })));
        }

        Layout planned() => Organizer.build(plan, controller.Layout, OrganizeClean.IsChecked == true, OrganizeKeep.IsChecked == true);

        // The primary screen in miniature with every planned panel where it will be. The panels arrive one by one, the
        // single moment of motion in the flow.
        void drawPlan(bool animate)
        {
            if (plan == null)
                return;
            Layout next = planned();
            OrganizeLead.Text = plan.Count == 0 ? Text.get("organize.nothing") : Text.format("organize.lead", plan.Count, next.Groups.Count);
            Screen screen = Screens.primary();
            RECT work = screen.Work;
            double roomWidth = OrganizeStage.ActualWidth - 26, roomHeight = OrganizeStage.ActualHeight - 26;
            if (roomWidth <= 0 || roomHeight <= 0)
            {
                // Not laid out yet; the size change that follows draws it.
                pendingAnimation = animate;
                return;
            }
            double k = Math.Min(roomWidth / work.Width, roomHeight / work.Height);
            OrganizeMap.Width = Math.Floor(work.Width * k);
            OrganizeMap.Height = Math.Floor(work.Height * k);
            OrganizeMap.Children.Clear();
            int order = 0;
            foreach (Group g in next.Groups)
            {
                // Edges are rounded, not sizes, so the gaps between neighbours stay even.
                int rows = g.IsFolder ? g.Rows : Math.Max(1, Math.Min(g.Rows, (int)Math.Ceiling(g.Items.Count / (double)g.Columns)));
                double left = (g.X - work.Left) * k, top = (g.Y - work.Top) * k;
                double x0 = Math.Round(left), x1 = Math.Round(left + PanelMetrics.width(g.Columns, next.IconSize) * k * screen.Scale);
                double y0 = Math.Round(top), y1 = Math.Round(top + PanelMetrics.height(rows, next.IconSize) * k * screen.Scale);
                FrameworkElement mini = miniPanel(g, rows, x1 - x0, y1 - y0, k * screen.Scale, next.IconSize);
                Canvas.SetLeft(mini, x0);
                Canvas.SetTop(mini, y0);
                OrganizeMap.Children.Add(mini);
                if (animate && controller.Layout.Animations)
                {
                    var delay = TimeSpan.FromMilliseconds(60 * order++);
                    mini.Opacity = 0;
                    var shift = new TranslateTransform(0, 6);
                    mini.RenderTransform = shift;
                    mini.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { BeginTime = delay });
                    shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(320)) {
                        BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
            }
        }

        bool pendingAnimation;

        // One planned panel drawn with the real panel's geometry at map scale: glass, title, the tinted shoreline and
        // one mark per icon where the icon will be.
        FrameworkElement miniPanel(Group g, int rows, double width, double height, double k, string iconSize)
        {
            var card = new Border { Width = width, Height = height, CornerRadius = new CornerRadius(Math.Max(3, 12 * k)),
                                    BorderThickness = new Thickness(1), ClipToBounds = true };
            card.SetResourceReference(Border.BackgroundProperty, "Brush.PanelGlass");
            card.SetResourceReference(Border.BorderBrushProperty, "Brush.PanelBorder");
            var face = new Canvas();
            var title = new TextBlock { Text = g.Name, FontSize = 9, FontWeight = FontWeights.SemiBold, Width = width - 16 * k - 4,
                                        TextTrimming = TextTrimming.CharacterEllipsis };
            title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            Canvas.SetLeft(title, 14 * k);
            Canvas.SetTop(title, 2);
            face.Children.Add(title);
            var shore = new Path { Data = Geometry.Parse("M 0 3 C 40 0 70 6 110 3 S 190 0 230 3 S 280 6 300 3"), Stretch = Stretch.Fill,
                                   StrokeThickness = 1, Width = width - 30 * k, Height = Math.Max(2, 6 * k),
                                   StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            shore.SetResourceReference(Shape.StrokeProperty, "Brush.Tint." + g.Tint);
            Canvas.SetLeft(shore, 14 * k);
            Canvas.SetTop(shore, 15);
            face.Children.Add(shore);
            // The title needs a few pixels more than the scaled header, so the rows share what is left evenly.
            double tile = PanelMetrics.tile(iconSize) * k, icon = Math.Round(PanelMetrics.icon(iconSize) * k);
            double top = 19, pitch = (height - top - 3) / rows;
            // A folder panel's content is only known later, so it shows one faint row, as a panel that fills up.
            int count = g.IsFolder ? g.Columns : Math.Min(g.Items.Count, g.Columns * rows);
            for (int i = 0; i < count; i++)
            {
                var mark = new Border { Width = icon, Height = icon, CornerRadius = new CornerRadius(icon / 4),
                                        Opacity = g.IsFolder ? 0.25 : 0.75 };
                mark.SetResourceReference(Border.BackgroundProperty, "Brush.Tint." + g.Tint);
                Canvas.SetLeft(mark, Math.Round(9 * k + (i % g.Columns) * tile + (tile - icon) / 2));
                Canvas.SetTop(mark, Math.Round(top + (i / g.Columns) * pitch + (pitch - icon) / 2));
                face.Children.Add(mark);
            }
            card.Child = face;
            return card;
        }

        // Second welcome step: the presets that make sense on this computer, the most useful ones already ticked.
        internal void showPresetChoices()
        {
            Welcome.Visibility = Visibility.Collapsed;
            WelcomePresets.Visibility = Visibility.Visible;
            PresetChoices.Children.Clear();
            PresetsLoading.Visibility = Visibility.Visible;
            FinishButton.IsEnabled = false;
            Task.Run(() => Presets.available().ToList()).ContinueWith(t => Dispatcher.BeginInvoke(new Action(delegate {
                PresetsLoading.Visibility = Visibility.Collapsed;
                FinishButton.IsEnabled = true;
                foreach (Preset p in t.Result)
                    PresetChoices.Children.Add(presetCard(p));
            })));
        }

        FrameworkElement presetCard(Preset p)
        {
            var card = new CheckBox { Tag = p, IsChecked = p.Recommended };
            card.SetResourceReference(StyleProperty, "Orla.PresetCard");
            var body = new StackPanel();
            var glyph = Menus.glyph(p.Glyph);
            glyph.HorizontalAlignment = HorizontalAlignment.Left;
            glyph.SetResourceReference(Shape.StrokeProperty, "Brush.Tint." + p.Tint);
            body.Children.Add(glyph);
            var name = new TextBlock { Text = p.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) };
            name.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            var hint = new TextBlock { Text = p.Hint, FontSize = 12, TextWrapping = TextWrapping.Wrap };
            hint.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
            body.Children.Add(name);
            body.Children.Add(hint);
            card.Content = body;
            return card;
        }

        // Everyday entry point for new panels: presets first, then an empty collection or any folder.
        void openPresetMenu()
        {
            NewPanel.IsEnabled = false;
            Task.Run(() => Presets.available().ToList()).ContinueWith(t => Dispatcher.BeginInvoke(new Action(delegate {
                NewPanel.IsEnabled = true;
                var menu = new ContextMenu { PlacementTarget = NewPanel, Placement = PlacementMode.Bottom };
                foreach (Preset p in t.Result)
                {
                    Preset preset = p;
                    var item = new MenuItem { Header = p.Name, ToolTip = p.Hint, Icon = Menus.glyph(p.Glyph) };
                    item.Click += delegate { controller.createFromPreset(preset); };
                    menu.Items.Add(item);
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("central.newCollection", "Glyph.PanelCollection", delegate {
                    string name = Dialog.prompt(Text.get("central.newCollection"), Text.get("central.collectionDefault"));
                    if (name != null)
                        controller.createPanel(PanelKind.Collection, name, null);
                }));
                menu.Items.Add(Menus.item("central.newFolderPanel", "Glyph.PanelFolder", delegate {
                    string folder = controller.pickFolder(Text.get("central.folderPick"));
                    if (folder != null)
                        controller.createPanel(PanelKind.Folder, Shell.displayName(folder), folder);
                }));
                menu.IsOpen = true;
            })));
        }

        // The next key combination pressed becomes the shortcut. Esc cancels.
        void startRecording()
        {
            recording = true;
            controller.pauseHotkey(true);
            ShortcutButton.Content = Text.get("general.shortcutRecording");
            ShortcutHint.Text = Text.get("general.shortcutHint");
        }

        void stopRecording()
        {
            if (!recording)
                return;
            recording = false;
            controller.pauseHotkey(false);
            ShortcutButton.Content = controller.Shortcut.display();
        }

        void recordKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!recording)
                return;
            e.Handled = true;
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                stopRecording();
                return;
            }
            Shortcut? pressed = Shortcut.fromKeyPress(e);
            if (pressed == null)
                return;
            if (!pressed.Value.IsValid)
            {
                ShortcutHint.Text = Text.get("general.shortcutInvalid");
                return;
            }
            recording = false;
            bool ok = controller.setShortcut(pressed.Value);
            controller.pauseHotkey(false);
            ShortcutButton.Content = controller.Shortcut.display();
            ShortcutHint.Text = ok ? Text.get("general.shortcutHint") : Text.format("notice.hotkeyTaken", pressed.Value.display());
            refresh();
        }

        void onChange(Action action)
        {
            if (!loading)
                action();
        }

        void setIconSize(string value)
        {
            controller.setIconSize(value);
            buildPreview();
        }

        static void browse(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public void refresh()
        {
            loading = true;
            Layout l = controller.Layout;
            ThemeSystem.IsChecked = l.Theme == "system";
            ThemeLight.IsChecked = l.Theme == "light";
            ThemeDark.IsChecked = l.Theme == "dark";
            OpacitySlider.Value = l.Opacity;
            OpacityValue.Text = Math.Round(l.Opacity * 100) + "%";
            IconSmall.IsChecked = l.IconSize == "small";
            IconMedium.IsChecked = l.IconSize == "medium";
            IconLarge.IsChecked = l.IconSize == "large";
            AnimationsSwitch.IsChecked = l.Animations;
            StartupSwitch.IsChecked = controller.StartupEnabled;
            CleanSwitch.IsChecked = l.CleanDesktop;
            HotkeySwitch.IsChecked = l.OverlayHotkey;
            LockSwitch.IsChecked = l.LockLayout;
            LanguageLabel.Text = l.Language == "system"
                                     ? Text.format("language.systemCurrent", Text.nativeName(Text.Language))
                                     : Text.nativeName(l.Language);
            UpdateSwitch.IsChecked = l.AutoUpdate;
            UpdateCard.Visibility = Updates.Ready != null ? Visibility.Visible : Visibility.Collapsed;
            UpdateTitle.Text = Updates.Ready != null ? Text.format("about.updateReady", Updates.Ready) : "";
            FrontLabel.Text = Text.get(controller.Overlay ? "central.back" : "central.front");
            string shortcut = controller.Shortcut.display();
            FrontShortcut.Text = shortcut;
            FrontShortcut.Visibility = l.OverlayHotkey ? Visibility.Visible : Visibility.Collapsed;
            PanelsTip.Text = Text.format("central.panelsTip", shortcut);
            WelcomeTip.Text = Text.format("welcome.tip", shortcut);
            HotkeyHint.Text = Text.format("general.hotkeyHint", shortcut);
            if (!recording)
                ShortcutButton.Content = shortcut;
            ShortcutButton.IsEnabled = l.OverlayHotkey;
            KeepSwitch.IsChecked = l.AutoOrganize;
            KeepSwitch.Visibility = controller.Organized ? Visibility.Visible : Visibility.Collapsed;
            OrganizeUndo.Visibility = controller.CanUndoOrganize ? Visibility.Visible : Visibility.Collapsed;
            OrganizeKeepRow.Visibility = controller.Organized || controller.CanUndoOrganize ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = Text.get(controller.DesktopFound ? "status.integrated" : "status.fallback");
            StatusDot.SetResourceReference(Shape.FillProperty, controller.DesktopFound ? "Brush.Accent" : "Brush.Warning");
            buildPanelList();
            loading = false;
        }

        void buildPanelList()
        {
            PanelList.Children.Clear();
            foreach (Group g in controller.Layout.Groups)
                PanelList.Children.Add(panelRow(g));
            PanelsEmpty.Visibility = controller.Layout.Groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        UIElement panelRow(Group g)
        {
            var card = new Border();
            card.SetResourceReference(StyleProperty, "Orla.Card");
            var row = new DockPanel();

            var more = new Button { Content = Menus.glyph("Glyph.More"), ToolTip = Text.get("panel.menu"), Margin = new Thickness(8, 0, 0, 0) };
            more.SetResourceReference(StyleProperty, "Orla.IconButton");
            more.Click += delegate {
                var menu = new ContextMenu { PlacementTarget = more, Placement = PlacementMode.Bottom };
                menu.Items.Add(Menus.item("panel.rename", "Glyph.Rename", delegate {
                    string name = Dialog.prompt(Text.get("panel.rename"), g.Name);
                    if (name != null)
                        controller.renamePanel(g, name);
                }));
                MenuItem tint = Menus.item("panel.tint", "Glyph.PanelCollection", null);
                foreach (string t in Tints.All)
                {
                    string value = t;
                    MenuItem option = Menus.check("tint." + t, g.Tint == t, () => controller.setTint(g, value));
                    option.Icon = Menus.swatch("Brush.Tint." + t);
                    tint.Items.Add(option);
                }
                menu.Items.Add(tint);
                if (g.IsFolder)
                    menu.Items.Add(Menus.item("panel.openFolder", "Glyph.Open", () => controller.open(Shell.resolveFolder(g.FolderPath))));
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("panel.remove", "Glyph.Remove", () => controller.removePanel(g)));
                menu.IsOpen = true;
            };
            DockPanel.SetDock(more, Dock.Right);
            row.Children.Add(more);

            var visible = new CheckBox { IsChecked = g.Visible, Content = Text.get("central.onDesktop"), VerticalAlignment = VerticalAlignment.Center,
                                         FontSize = 13 };
            visible.SetResourceReference(StyleProperty, "Orla.Switch");
            visible.Click += delegate { controller.setVisible(g, visible.IsChecked == true); };
            DockPanel.SetDock(visible, Dock.Right);
            row.Children.Add(visible);

            var marker = new Grid { Width = 36, Height = 36, Margin = new Thickness(0, 0, 14, 0) };
            var plate = new Border { CornerRadius = new CornerRadius(8), Opacity = 0.18 };
            plate.SetResourceReference(Border.BackgroundProperty, "Brush.Tint." + g.Tint);
            var glyph = Menus.glyph(g.IsFolder ? "Glyph.PanelFolder" : "Glyph.PanelCollection");
            glyph.SetResourceReference(Shape.StrokeProperty, "Brush.Tint." + g.Tint);
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            glyph.VerticalAlignment = VerticalAlignment.Center;
            marker.Children.Add(plate);
            marker.Children.Add(glyph);
            row.Children.Add(marker);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = new TextBlock { Text = g.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
            title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            var detail = new TextBlock { FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis,
                                         Text = g.IsFolder ? Text.format("central.folderDetail", folderLabel(g))
                                                           : Text.format(g.Items.Count == 1 ? "central.itemCount" : "central.itemsCount", g.Items.Count) };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
            if (g.IsFolder)
                detail.ToolTip = Shell.resolveFolder(g.FolderPath);
            text.Children.Add(title);
            text.Children.Add(detail);
            row.Children.Add(text);

            card.Child = row;
            return card;
        }

        static string folderLabel(Group g)
        {
            if (g.FolderPath == Shell.DesktopFolder)
                return Text.get(g.OnlyUnorganized ? "central.desktopUnorganized" : "central.desktopFolder");
            string folder = Shell.resolveFolder(g.FolderPath);
            string name = System.IO.Path.GetFileName(folder.TrimEnd(System.IO.Path.DirectorySeparatorChar));
            return name.Length > 0 ? name : folder;
        }

        // A real panel, drawn with the current settings, over a sample wallpaper.
        void buildPreview()
        {
            PreviewHost.Children.Clear();
            var sample = new Group { Name = Text.get("starter.quickAccess"), Columns = 3, Rows = 1, Tint = Tints.SeaGlass };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                sample.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            preview = new PanelView(controller, sample) { IsHitTestVisible = false };
            PreviewHost.Children.Add(preview);
        }
    }
}
