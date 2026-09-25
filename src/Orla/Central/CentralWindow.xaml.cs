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
        bool loading, recording;
        Organizer.Plan plan;
        bool organizeFromWelcome;



        public CentralWindow(Controller controller)
        {
            this.controller = controller;
            InitializeComponent();
            setupPerformance();
            SourceInitialized += delegate { Controller.applyWindowTheme(this); };
            MinimizeButton.Click += delegate { SystemCommands.MinimizeWindow(this); };
            MaximizeButton.Click += delegate {
                if (WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(this);
                else
                    SystemCommands.MaximizeWindow(this);
            };
            CloseButton.Click += delegate { SystemCommands.CloseWindow(this); };
            // A maximized window with its own chrome reaches past the screen by the resize border; pull the content in.
            StateChanged += delegate {
                bool max = WindowState == WindowState.Maximized;
                // The frame Windows keeps around a maximized window: the sizing border plus the padded border.
                double frame = (Native.GetSystemMetrics(32) + Native.GetSystemMetrics(92)) /
                               (PresentationSource.FromVisual(this)?.CompositionTarget.TransformToDevice.M11 ?? 1);
                Root.Margin = max ? new Thickness(frame) : new Thickness(0);
                MaximizeButton.Content = FindResource(max ? "Caption.Restore" : "Caption.Maximize");
                MaximizeButton.ToolTip = Text.get(max ? "window.restore" : "window.maximize");
            };

            NavPanels.Checked += delegate { showPage(PagePanels); };
            NavPerformance.Checked += delegate { showPage(PagePerformance); };
            NavAppearance.Checked += delegate { showPage(PageAppearance); };
            NavGeneral.Checked += delegate { showPage(PageGeneral); };
            NavAbout.Checked += delegate { showPage(PageAbout); };
            FrontButton.Click += delegate { controller.setOverlay(!controller.Overlay); refresh(); };

            NewPanel.Click += delegate { openPresetMenu(); };
            EmptyCollection.Click += delegate {
                string name = Dialog.prompt(Text.get("central.newCollection"), Text.get("central.collectionDefault"));
                if (name != null)
                    controller.createPanel(PanelKind.Collection, name, null);
            };
            EmptyFolder.Click += delegate {
                string folder = controller.pickFolder(Text.get("central.folderPick"));
                if (folder != null)
                    controller.createPanel(PanelKind.Folder, Shell.displayName(folder), folder);
            };
            ThemeSystem.Checked += delegate { onChange(() => controller.setTheme("system")); };
            ThemeLight.Checked += delegate { onChange(() => controller.setTheme("light")); };
            ThemeDark.Checked += delegate { onChange(() => controller.setTheme("dark")); };
            OpacitySlider.ValueChanged += delegate {
                OpacityValue.Text = Math.Round(OpacitySlider.Value * 100) + "%";
                onChange(() => controller.setOpacity(OpacitySlider.Value));
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
            UpdateButton.Click += delegate {
                if (Updates.Status == Updates.State.Ready)
                    Updates.restartNow(controller);
                else
                    Updates.checkNow(controller);
            };
            Action updatesChanged = () => showUpdates();
            Updates.Changed += updatesChanged;
            Closed += delegate { Updates.Changed -= updatesChanged; };
            ArrangeButton.Click += delegate { controller.resetPositions(); };
            ResetButton.Click += delegate {
                if (Dialog.confirm(Text.get("general.resetConfirmTitle"), Text.get("general.resetConfirmMessage"),
                                   Text.get("general.resetConfirmAction"), true))
                    controller.resetToWelcome();
            };

            DataButton.Click += delegate { controller.open(controller.DataDirectory); };
            TourButton.Click += delegate { controller.startTour(); };
            GuideButton.Click += delegate { browse(Repository + (Text.Language == "pt-BR" ? "/blob/main/docs/pt-BR/guia.md" : "/blob/main/docs/en/guide.md")); };
            IssueButton.Click += delegate { browse(Report.issueUrl(Repository)); };
            QuitButton.Click += delegate { controller.quit(); };
            StartButton.Click += delegate {
                if (ChoiceOrganize.IsChecked == true)
                    showOrganize(true);
                else
                    showPresetChoices();
            };
            OrganizeButton.Click += delegate { showOrganize(false); };
            OrganizeClean.Click += delegate { drawPlan(false); };
            OrganizeSecond.Click += delegate { drawPlan(false); };
            OrganizePerformance.Click += delegate { drawPlan(false); };
            OrganizeComplete.Checked += delegate { drawPlan(true); };
            OrganizeRedo.Checked += delegate { drawPlan(true); };
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
                Layout next = planned(Completing ? new Organizer.Completion() : null);
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
            else if (page == "performance" && !welcome)
                NavPerformance.IsChecked = true;
            else if (!welcome && Nav.Children.OfType<RadioButton>().All(r => r.IsChecked != true))
                NavPanels.IsChecked = true;
            Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }

        void showPage(UIElement page)
        {
            foreach (UIElement p in new UIElement[] { PagePanels, PagePerformance, PageAppearance, PageGeneral, PageAbout })
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
            // Organizing again starts from how the desktop is set up now; a first organize suggests both on.
            bool again = !fromWelcome && controller.Organized;
            OrganizeClean.IsChecked = !again || controller.Layout.CleanDesktop;
            OrganizeKeep.IsChecked = !again || controller.Layout.AutoOrganize;
            OrganizeSecond.IsChecked = true;
            OrganizeSecond.Visibility = AllScreens.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            OrganizePerformance.IsChecked = true;
            OrganizePerformanceCard.Visibility = controller.Layout.Groups.Any(g => g.IsSensors) ? Visibility.Collapsed : Visibility.Visible;
            OrganizeModes.Visibility = !fromWelcome && controller.Organized ? Visibility.Visible : Visibility.Collapsed;
            OrganizeComplete.IsChecked = true;
            OrganizeMap.Children.Clear();
            OrganizeApply.IsEnabled = false;
            OrganizeLead.Text = Text.get("organize.reading");
            OrganizeNote.Text = "";
            Dictionary<string, string> learned = controller.learnedCategories();
            Task.Run(source ?? (() => Organizer.plan(learned))).ContinueWith(t => Dispatcher.BeginInvoke(new Action(delegate {
                if (t.IsFaulted)
                {
                    OrganizeLead.Text = Text.get("organize.failed");
                    return;
                }
                plan = t.Result;
                drawPlan(true);
            })));
        }

        // The screens the organizer plans for: every monitor, unless the documentation renderer picks them.
        internal IList<Screen> PreviewScreens { get; set; }

        IList<Screen> AllScreens => PreviewScreens ?? Screens.all();

        bool Completing => OrganizeModes.Visibility == Visibility.Visible && OrganizeComplete.IsChecked == true;

        Layout planned(Organizer.Completion done)
        {
            bool clean = OrganizeClean.IsChecked == true, keep = OrganizeKeep.IsChecked == true;
            bool performance = OrganizePerformanceCard.Visibility == Visibility.Visible && OrganizePerformance.IsChecked == true;
            IList<Screen> screens = AllScreens;
            return done != null ? Organizer.complete(plan, controller.Layout, clean, keep, screens, done, performance)
                                : Organizer.build(plan, controller.Layout, clean, keep, screens, OrganizeSecond.IsChecked == true, performance);
        }

        // The screens in miniature with every planned panel where it will be. The panels arrive one by one, the single
        // moment of motion in the flow. When completing, what is new stands out and the rest steps back.
        void drawPlan(bool animate)
        {
            if (plan == null)
                return;
            var done = Completing ? new Organizer.Completion() : null;
            Layout next = planned(done);
            IList<Screen> screens = AllScreens;
            // Organizing again keeps panels where they are, so the monitor choice only belongs to a fresh layout. When it
            // appears or goes, the map is drawn again once the room it leaves is measured.
            Visibility second = done == null && screens.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (OrganizeSecond.Visibility != second)
            {
                OrganizeSecond.Visibility = second;
                Dispatcher.BeginInvoke(new Action(() => drawPlan(false)), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            if (done == null)
            {
                OrganizeLead.Text = plan.Count == 0 ? Text.get("organize.nothing") : Text.format("organize.lead", plan.Count, next.Groups.Count);
                OrganizeNote.Text = !organizeFromWelcome && controller.Layout.Groups.Count > 0 ? Text.get("organize.replaceNote") : "";
                OrganizeApply.IsEnabled = true;
            }
            else
            {
                int intoNew = done.Grew.Where(g => done.Fresh.Contains(g.Key)).Sum(g => g.Value);
                OrganizeLead.Text = done.Added == 0 ? Text.get("organize.completeNone")
                                                    : Text.format("organize.completeLead", done.Added, done.Added - intoNew, intoNew);
                OrganizeNote.Text = Text.get("organize.completeNote");
                OrganizeApply.IsEnabled = done.Added > 0 || done.Fresh.Count > 0 || next.CleanDesktop != controller.Layout.CleanDesktop ||
                                          next.AutoOrganize != controller.Layout.AutoOrganize;
            }

            Screen main = screens.FirstOrDefault(s => s.Primary) ?? screens[0];
            Screen screenOf(Group g) => Screens.nearest(g, screens);
            var used = screens.Where(s => s == main || next.Groups.Any(g => g.Visible && screenOf(g) == s)).ToList();
            var box = new RECT { Left = used.Min(s => s.Work.Left), Top = used.Min(s => s.Work.Top),
                                 Right = used.Max(s => s.Work.Right), Bottom = used.Max(s => s.Work.Bottom) };
            if (OrganizeSecond.Visibility == Visibility.Visible && OrganizeSecond.ActualHeight == 0)
            {
                // The switch above the map is not measured yet; draw once it is.
                Dispatcher.BeginInvoke(new Action(() => drawPlan(animate)), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }
            double roomWidth = OrganizeStage.ActualWidth - 26,
                   roomHeight = OrganizeStage.ActualHeight - 26 - (OrganizeSecond.Visibility == Visibility.Visible ? OrganizeSecond.ActualHeight + 12 : 0);
            if (roomWidth <= 0 || roomHeight <= 0)
            {
                // Not laid out yet; the size change that follows draws it.
                pendingAnimation = animate;
                return;
            }
            double k = Math.Min(roomWidth / box.Width, roomHeight / box.Height);
            OrganizeMap.Width = Math.Floor(box.Width * k);
            OrganizeMap.Height = Math.Floor(box.Height * k);
            OrganizeMap.Children.Clear();
            foreach (Screen s in used)
            {
                var face = new Border { Width = Math.Round(s.Work.Width * k), Height = Math.Round(s.Work.Height * k), CornerRadius = new CornerRadius(8),
                                        BorderThickness = new Thickness(1) };
                face.SetResourceReference(Border.BackgroundProperty, "Orla.Shore");
                face.SetResourceReference(Border.BorderBrushProperty, "Brush.Line");
                Canvas.SetLeft(face, Math.Round((s.Work.Left - box.Left) * k));
                Canvas.SetTop(face, Math.Round((s.Work.Top - box.Top) * k));
                OrganizeMap.Children.Add(face);
                // With more than one screen, say which one is the main screen, on the sand where no panel sits.
                if (used.Count > 1 && s == main)
                {
                    var label = new TextBlock { Text = Text.get("organize.mainScreen"), FontSize = 10,
                                                Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x4A, 0x30)) };
                    Canvas.SetLeft(label, Math.Round((s.Work.Left - box.Left) * k) + 10);
                    Canvas.SetTop(label, Math.Round((s.Work.Bottom - box.Top) * k) - 20);
                    Panel.SetZIndex(label, 1);
                    OrganizeMap.Children.Add(label);
                }
            }
            int order = 0;
            foreach (Group g in next.Groups.Where(g => g.Visible))
            {
                // Edges are rounded, not sizes, so the gaps between neighbours stay even.
                double scale = screenOf(g).Scale;
                int rows = PanelMetrics.plannedRows(g);
                double left = (g.X - box.Left) * k, top = (g.Y - box.Top) * k;
                double x0 = Math.Round(left), x1 = Math.Round(left + PanelMetrics.width(g.Columns, next.IconSize) * k * scale);
                double y0 = Math.Round(top), y1 = Math.Round(top + PanelMetrics.height(g, rows, next.IconSize) * k * scale);
                int added = done != null && done.Grew.TryGetValue(g.Id, out int n) ? n : 0;
                bool fresh = done != null && done.Fresh.Contains(g.Id);
                var mini = (Border)miniPanel(g, rows, x1 - x0, y1 - y0, k * scale, next.IconSize, done == null ? -1 : fresh ? 0 : added, fresh);
                if (fresh)
                {
                    mini.BorderThickness = new Thickness(1.5);
                    mini.SetResourceReference(Border.BorderBrushProperty, "Brush.Accent");
                }
                else if (done != null && added == 0)
                    mini.Child.Opacity = 0.4;
                Canvas.SetLeft(mini, x0);
                Canvas.SetTop(mini, y0);
                OrganizeMap.Children.Add(mini);
                if (animate && controller.Layout.Animations && (done == null || fresh || added > 0))
                {
                    double end = mini.Opacity;
                    var delay = TimeSpan.FromMilliseconds(60 * order++);
                    mini.Opacity = 0;
                    var shift = new TranslateTransform(0, 6);
                    mini.RenderTransform = shift;
                    mini.BeginAnimation(OpacityProperty, new DoubleAnimation(0, end, TimeSpan.FromMilliseconds(260)) { BeginTime = delay });
                    shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(320)) {
                        BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
            }
        }

        bool pendingAnimation;

        // One planned panel drawn with the real panel's geometry at map scale: glass, title, the tinted shoreline and
        // one mark per icon where the icon will be.
        // added: -1 for a fresh layout (the header shows the item count), otherwise how many items organizing again
        // brings; fresh marks a panel it creates.
        FrameworkElement miniPanel(Group g, int rows, double width, double height, double k, string iconSize, int added, bool fresh)
        {
            var card = new Border { Width = width, Height = height, CornerRadius = new CornerRadius(Math.Max(3, 12 * k)),
                                    BorderThickness = new Thickness(1), ClipToBounds = true };
            card.SetResourceReference(Border.BackgroundProperty, "Brush.PanelGlass");
            card.SetResourceReference(Border.BorderBrushProperty, "Brush.PanelBorder");
            var face = new Canvas();
            // The header's right side says what matters, and only the name gives way when space is short: the item
            // count on a fresh layout, as on the real panel; "new" or "+N" when organizing again.
            var header = new DockPanel { Width = Math.Max(0, width - 32 * k) };
            string note = fresh ? Text.get("organize.fresh") : added > 0 ? "+" + added : added < 0 && g.IsCollection ? g.Items.Count.ToString() : null;
            if (note != null)
            {
                var side = new TextBlock { Text = note, FontSize = 9, Margin = new Thickness(4, 0, 0, 0),
                                           FontWeight = added < 0 ? FontWeights.Normal : FontWeights.SemiBold };
                side.SetResourceReference(TextBlock.ForegroundProperty, added < 0 ? "Brush.TextSecondary" : "Brush.Accent");
                DockPanel.SetDock(side, Dock.Right);
                header.Children.Add(side);
            }
            var title = new TextBlock { Text = g.Name, FontSize = 9, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
            title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            header.Children.Add(title);
            Canvas.SetLeft(header, 14 * k);
            Canvas.SetTop(header, 2);
            face.Children.Add(header);
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
            int count = g.Collapsed ? 0 : g.IsFolder ? g.Columns : Math.Min(PanelMetrics.count(g), g.Columns * rows);
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
            refreshPerformance();
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
            showUpdates();
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
            ShortcutRow.Visibility = l.OverlayHotkey ? Visibility.Visible : Visibility.Collapsed;
            // Once there are panels, organizing is an option among others, not the page's main action.
            OrganizeButton.SetResourceReference(StyleProperty, l.Groups.Count > 0 ? "Orla.Button" : "Orla.Button.Accent");
            KeepSwitch.IsChecked = l.AutoOrganize;
            KeepSwitch.Visibility = controller.Organized ? Visibility.Visible : Visibility.Collapsed;
            OrganizeUndo.Visibility = controller.CanUndoOrganize ? Visibility.Visible : Visibility.Collapsed;
            OrganizeKeepRow.Visibility = controller.Organized || controller.CanUndoOrganize ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = Text.get(controller.DesktopFound ? "status.integrated" : "status.fallback");
            StatusDot.SetResourceReference(Shape.FillProperty, controller.DesktopFound ? "Brush.Accent" : "Brush.Warning");
            buildPanelList();
            loading = false;
        }

        void showUpdates()
        {
            Updates.State state = Updates.Status;
            bool ready = state == Updates.State.Ready;
            string last = DateTime.TryParse(controller.Layout.LastUpdateCheck, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime when)
                              ? Text.format("about.lastCheck", when.ToLocalTime().ToString("g", new System.Globalization.CultureInfo(Text.Language)))
                              : Text.get("about.neverChecked");
            UpdateTitle.Text = ready ? Text.format("about.updateReady", Updates.Ready)
                             : state == Updates.State.Checking ? Text.get("about.checking")
                             : state == Updates.State.UpToDate ? Text.get("about.upToDate")
                             : state == Updates.State.Failed ? Text.get("about.checkFailed")
                             : state == Updates.State.NotInstalled ? Text.get("about.notInstalled")
                             : Text.get("about.updates");
            UpdateHint.Text = ready ? Text.get("about.updateHint")
                            : state == Updates.State.Failed ? Text.get("about.checkFailedHint")
                            : state == Updates.State.NotInstalled ? Text.get("about.notInstalledHint")
                            : last;
            UpdateButton.Content = Text.get(ready ? "about.updateAction" : "about.checkNow");
            UpdateButton.SetResourceReference(StyleProperty, ready ? "Orla.Button.Accent" : "Orla.Button");
            UpdateButton.IsEnabled = state != Updates.State.Checking;
            UpdateButton.Visibility = state == Updates.State.NotInstalled ? Visibility.Collapsed : Visibility.Visible;
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
                menu.Items.Add(Menus.tints(g, controller));
                if (g.IsFolder)
                    menu.Items.Add(Menus.item("panel.openFolder", "Glyph.Open", () => controller.open(Shell.resolveFolder(g.FolderPath))));
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("panel.remove", "Glyph.Remove", () => controller.removePanel(g)));
                menu.IsOpen = true;
            };
            DockPanel.SetDock(more, Dock.Right);
            row.Children.Add(more);

            // One switch per row says it all; its name is for screen readers and the tooltip.
            var visible = new CheckBox { IsChecked = g.Visible, VerticalAlignment = VerticalAlignment.Center,
                                         ToolTip = Text.format("central.showPanel", g.Name) };
            System.Windows.Automation.AutomationProperties.SetName(visible, Text.format("central.showPanel", g.Name));
            visible.SetResourceReference(StyleProperty, "Orla.Switch");
            visible.Click += delegate { controller.setVisible(g, visible.IsChecked == true); };
            DockPanel.SetDock(visible, Dock.Right);
            row.Children.Add(visible);

            var marker = new Grid { Width = 36, Height = 36, Margin = new Thickness(0, 0, 14, 0) };
            var plate = new Border { CornerRadius = new CornerRadius(8), Opacity = 0.18 };
            plate.SetResourceReference(Border.BackgroundProperty, "Brush.Tint." + g.Tint);
            var glyph = Menus.glyph(g.IsFolder ? "Glyph.PanelFolder" : g.IsSensors ? "Glyph.Pulse" : "Glyph.PanelCollection");
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
                                                : g.IsSensors ? Text.get("preset.performanceHint")
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
            var sample = new Group { Name = Text.get("preset.quickAccess"), Columns = 3, Rows = 1, Tint = Tints.SeaGlass };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                sample.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            PreviewHost.Children.Add(new PanelView(controller, sample) { IsHitTestVisible = false });
        }
    }
}
