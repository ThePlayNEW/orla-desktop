using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace Orla
{
    // Orla's own window: panels, appearance, general settings, about, and the welcome screen on first run.
    public partial class CentralWindow : Window
    {
        public const string Repository = "https://github.com/ThePlayNEW/orla";
        readonly Controller controller;
        PanelView preview;
        bool loading;

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

            NewCollection.Click += delegate {
                string name = Dialog.prompt(Text.get("central.newCollection"), Text.get("central.collectionDefault"));
                if (name != null)
                    controller.createPanel(PanelKind.Collection, name, null);
            };
            NewFolderPanel.Click += delegate {
                string folder = Controller.pickFolder(Text.get("central.folderPick"));
                if (folder != null)
                    controller.createPanel(PanelKind.Folder, Shell.displayName(folder), folder);
            };

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
                    Dialog.alert(Text.get("general.hotkey"), Text.get("notice.hotkeyTaken"));
                }
            };
            LockSwitch.Click += delegate { controller.setLock(LockSwitch.IsChecked == true); };
            LanguageSystem.Checked += delegate { onChange(() => setLanguage("system")); };
            LanguagePt.Checked += delegate { onChange(() => setLanguage("pt-BR")); };
            LanguageEn.Checked += delegate { onChange(() => setLanguage("en")); };
            ArrangeButton.Click += delegate { controller.resetPositions(); };

            DataButton.Click += delegate { controller.open(controller.DataDirectory); };
            GuideButton.Click += delegate { browse(Repository + (Text.Language == "pt-BR" ? "/blob/main/docs/pt-BR/guia.md" : "/blob/main/docs/en/guide.md")); };
            IssueButton.Click += delegate { browse(Repository + "/issues/new/choose"); };
            QuitButton.Click += delegate { controller.quit(); };
            StartButton.Click += delegate {
                controller.welcome(ChoiceOrganize.IsChecked == true);
                Welcome.Visibility = Visibility.Collapsed;
                Main.Visibility = Visibility.Visible;
                NavPanels.IsChecked = true;
                refresh();
            };

            VersionText.Text = Text.format("about.version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            DataPath.Text = controller.DataDirectory;
            refresh();
        }

        public void show(string page)
        {
            bool welcome = page == "welcome" || !controller.Layout.Welcomed;
            Welcome.Visibility = welcome ? Visibility.Visible : Visibility.Collapsed;
            Main.Visibility = welcome ? Visibility.Collapsed : Visibility.Visible;
            if (!welcome && Nav.Children.OfType<RadioButton>().All(r => r.IsChecked != true))
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

        void setLanguage(string value)
        {
            controller.setLanguage(value);
            LanguageHint.Text = Text.get("general.languageRestart");
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
            LanguageSystem.IsChecked = l.Language == "system";
            LanguagePt.IsChecked = l.Language == "pt-BR";
            LanguageEn.IsChecked = l.Language == "en";
            FrontLabel.Text = Text.get(controller.Overlay ? "central.back" : "central.front");
            FrontShortcut.Visibility = l.OverlayHotkey ? Visibility.Visible : Visibility.Collapsed;
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
            return Shell.resolveFolder(g.FolderPath);
        }

        // A real panel, drawn with the current settings, over a sample wallpaper.
        void buildPreview()
        {
            PreviewHost.Children.Clear();
            var sample = new Group { Name = Text.get("starter.quickAccess"), Width = 330, Height = 170, Tint = Tints.SeaGlass };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                sample.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            preview = new PanelView(controller, sample) { IsHitTestVisible = false };
            PreviewHost.Children.Add(preview);
        }
    }
}
