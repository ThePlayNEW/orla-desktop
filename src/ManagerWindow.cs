using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Orla
{
    public class ManagerWindow : Window
    {
        Controller controller;
        StackPanel nav;
        StackPanel stage;
        TextBox search;
        TextBlock subtitle;
        Button activate;
        string selected;
        bool building;
        public ManagerWindow(Controller controller)
        {
            this.controller = controller;
            Title = "Orla — seu espaço, organizado";
            Width = 1120;
            Height = 760;
            MinWidth = 820;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = UI.brush("#0D131C");
            Foreground = UI.foreground;
            FontFamily = new FontFamily("Segoe UI");
            var root = new Grid { Background = UI.brush("#0D131C") };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            root.ColumnDefinitions.Add(new ColumnDefinition());
            var rail = new DockPanel { Background = UI.brush("#121A25"), Margin = new Thickness(0, 0, 1, 0) };
            var brand = new StackPanel { Margin = new Thickness(24, 27, 18, 26) };
            var logo = UI.label("orla", 32, UI.foreground);
            logo.FontWeight = FontWeights.SemiBold;
            brand.Children.Add(logo);
            brand.Children.Add(UI.label("Seu desktop, mais leve.", 11, UI.muted));
            DockPanel.SetDock(brand, Dock.Top);
            rail.Children.Add(brand);
            var bottom = new StackPanel { Margin = new Thickness(18) };
            bottom.Children.Add(UI.label("LOCAL · SEM NUVEM", 9, UI.muted));
            var startup =
                new CheckBox { Content = "Iniciar com o Windows", Foreground = UI.muted,
                               Margin = new Thickness(0, 13, 0, 10), IsChecked = controller.StartupEnabled };
            startup.Click += delegate
            {
                controller.tryAction(delegate { controller.setStartup(startup.IsChecked == true); });
                startup.IsChecked = controller.StartupEnabled;
            };
            bottom.Children.Add(startup);
            var animations =
                new CheckBox { Content = "Animações suaves", IsChecked = controller.store.data.Animations,
                               Foreground = UI.muted, Margin = new Thickness(0, 0, 0, 14) };
            animations.Click += delegate
            {
                controller.store.data.Animations = animations.IsChecked == true;
                controller.save();
            };
            bottom.Children.Add(animations);
            bottom.Children.Add(UI.label("Opacidade dos painéis", 11, UI.muted));
            var opacity = new Slider { Minimum = .55,
                                       Maximum = .95,
                                       Value = controller.store.data.Opacity,
                                       Margin = new Thickness(0, 8, 0, 16),
                                       TickFrequency = .05,
                                       IsSnapToTickEnabled = true,
                                       ToolTip = "Menor opacidade deixa mais wallpaper à vista" };
            opacity.ValueChanged += delegate
            {
                controller.store.data.Opacity = opacity.Value;
                controller.refreshPanels();
            };
            opacity.AddHandler(Thumb.DragCompletedEvent,
                               new DragCompletedEventHandler(delegate { controller.save(); }));
            opacity.PreviewMouseLeftButtonUp += delegate
            {
                controller.save();
            };
            opacity.KeyUp += delegate
            {
                controller.save();
            };
            bottom.Children.Add(opacity);
            bottom.Children.Add(UI.button("Encerrar Orla", delegate { controller.quit(); }, false));
            DockPanel.SetDock(bottom, Dock.Bottom);
            rail.Children.Add(bottom);
            nav = new StackPanel { Margin = new Thickness(14, 0, 14, 0) };
            rail.Children.Add(
                new ScrollViewer { Content = nav, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            root.Children.Add(rail);
            var main = new DockPanel { Margin = new Thickness(30, 25, 30, 20) };
            Grid.SetColumn(main, 1);
            root.Children.Add(main);
            var top = new StackPanel();
            var heading = UI.label("Organize seu espaço.", 29, UI.foreground);
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(0, 8, 0, 8);
            top.Children.Add(heading);
            subtitle =
                UI.label("Seus arquivos ficam onde estão. Aqui, você organiza como os vê.", 12, UI.muted);
            top.Children.Add(subtitle);
            var bar = new DockPanel { Margin = new Thickness(0, 23, 0, 20) };
            activate = UI.button("Ativar na área de trabalho", delegate {
                controller.tryAction(delegate { controller.toggleDesktop(); });
                refresh();
            }, true);
            DockPanel.SetDock(activate, Dock.Right);
            bar.Children.Add(activate);
            var add = UI.button("+ Grupo", delegate {
                if (controller.store.data.Groups.Count >= 40)
                {
                    MessageBox.Show("Você já tem 40 grupos. Remova um grupo antes de criar outro.", "Orla");
                    return;
                }
                string name = UI.prompt(this, "Nome do grupo", "");
                if (name != null)
                {
                    var g = new Group { Name = name, X = 32, Y = 80, Visible = false };
                    controller.store.data.Groups.Add(g);
                    selected = g.Id;
                    controller.changed();
                }
            }, false);
            DockPanel.SetDock(add, Dock.Right);
            bar.Children.Add(add);
            var searchBox = new Grid { Width = 250, HorizontalAlignment = HorizontalAlignment.Left };
            search = new TextBox { Background = UI.brush("#202B39"),
                                   Foreground = UI.foreground,
                                   CaretBrush = UI.accent,
                                   BorderThickness = new Thickness(0),
                                   Padding = new Thickness(12, 9, 12, 9),
                                   ToolTip = "Buscar itens pelo nome" };
            var placeholder = UI.label("Buscar itens…", 12, UI.muted);
            placeholder.Margin = new Thickness(12, 0, 0, 0);
            placeholder.VerticalAlignment = VerticalAlignment.Center;
            placeholder.IsHitTestVisible = false;
            search.TextChanged += delegate
            {
                placeholder.Visibility =
                    String.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed;
                if (!building)
                    refreshStage();
            };
            searchBox.Children.Add(search);
            searchBox.Children.Add(placeholder);
            bar.Children.Add(searchBox);
            top.Children.Add(bar);
            DockPanel.SetDock(top, Dock.Top);
            main.Children.Add(top);
            var tip = UI.label(
                "Arraste entre grupos · Duplo clique para abrir · Botão direito para mais opções", 11,
                UI.muted);
            tip.Margin = new Thickness(0, 13, 0, 0);
            DockPanel.SetDock(tip, Dock.Bottom);
            main.Children.Add(tip);
            stage = new StackPanel();
            main.Children.Add(
                new ScrollViewer { Content = stage, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Content = root;
            Closing += delegate(object s, System.ComponentModel.CancelEventArgs e)
            {
                if (!controller.exiting)
                {
                    controller.manager = null;
                    controller.notify("Orla continua na bandeja. Clique no ícone para reabrir.");
                }
            };
            refresh();
        }
        public void select(string id)
        {
            selected = id;
            refresh();
        }
        public void refresh()
        {
            building = true;
            nav.Children.Clear();
            var all = UI.button("Visão geral", delegate {
                selected = null;
                refresh();
            }, selected == null);
            all.Margin = new Thickness(0, 0, 0, 14);
            nav.Children.Add(all);
            foreach (var g in controller.store.data.Groups)
            {
                Group target = g;
                var b = UI.button(g.Name + "   " + g.Items.Count, delegate {
                    selected = target.Id;
                    refresh();
                }, selected == g.Id);
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(0, 0, 0, 7);
                nav.Children.Add(b);
            }
            activate.Content =
                controller.desktopActive ? "Voltar ao desktop normal" : "Ativar na área de trabalho";
            subtitle.Text = controller.desktopActive
                                ? "Painéis ativos. Feche esta central e use sua área de trabalho."
                                : "Seus arquivos ficam onde estão. Aqui, você organiza como os vê.";
            building = false;
            refreshStage();
        }
        void refreshStage()
        {
            stage.Children.Clear();
            string query = search == null ? null : search.Text;
            foreach (var g in controller.store.data.Groups.Where(x => selected == null || selected == x.Id))
            {
                if (!String.IsNullOrWhiteSpace(query) &&
                    !g.Items.Any(e => e.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    continue;
                Group target = g;
                var card = new Border { CornerRadius = new CornerRadius(14),
                                        Background = UI.surface,
                                        BorderBrush = UI.line,
                                        BorderThickness = new Thickness(1),
                                        Margin = new Thickness(0, 0, 0, 15),
                                        Padding = new Thickness(14) };
                var content = new DockPanel();
                var head = new DockPanel { Margin = new Thickness(3, 0, 3, 9) };
                var menu = new ContextMenu();
                menu.Items.Add(
                    UI.menuItem("Adicionar arquivos ou atalhos", delegate { controller.addFiles(target); }));
                menu.Items.Add(
                    UI.menuItem("Adicionar uma pasta", delegate { controller.addFolder(target); }));
                menu.Items.Add(UI.menuItem("Renomear grupo", delegate {
                    string n = UI.prompt(this, "Nome do grupo", target.Name);
                    if (n != null)
                    {
                        target.Name = n;
                        controller.changed();
                    }
                }));
                var colors = new MenuItem { Header = "Cor do grupo" };
                string[] names = { "Menta", "Azul", "Violeta", "Âmbar" };
                string[] values = { "#91DFC4", "#8BBEFF", "#B6A3F5", "#E9C58A" };
                for (int i = 0; i < names.Length; i++)
                {
                    string color = values[i];
                    colors.Items.Add(UI.menuItem(names[i], delegate {
                        target.Color = color;
                        controller.changed();
                    }));
                }
                menu.Items.Add(colors);
                menu.Items.Add(UI.menuItem("Remover grupo (manter arquivos)", delegate {
                    if (MessageBox.Show(
                            this,
                            "Remover apenas este grupo e suas referências? Os arquivos serão mantidos.",
                            "Orla", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        controller.store.data.Groups.Remove(target);
                        selected = null;
                        controller.changed();
                    }
                }));
                var more = UI.button("···", delegate {}, false);
                more.ContextMenu = menu;
                more.Click += delegate
                {
                    menu.PlacementTarget = more;
                    menu.IsOpen = true;
                };
                DockPanel.SetDock(more, Dock.Right);
                head.Children.Add(more);
                var toggle =
                    new CheckBox { Content = "No desktop", IsChecked = g.Visible, Foreground = UI.muted,
                                   VerticalAlignment = VerticalAlignment.Center,
                                   Margin = new Thickness(10, 0, 8, 0) };
                toggle.Click += delegate
                {
                    target.Visible = toggle.IsChecked == true;
                    controller.changed();
                };
                DockPanel.SetDock(toggle, Dock.Right);
                head.Children.Add(toggle);
                var title = UI.label(g.Name + "  ·  " + g.Items.Count, 15, UI.brush(g.Color));
                title.FontWeight = FontWeights.SemiBold;
                title.VerticalAlignment = VerticalAlignment.Center;
                head.Children.Add(title);
                DockPanel.SetDock(head, Dock.Top);
                content.Children.Add(head);
                var items = UI.items(controller, g, false, query);
                items.Height = selected == null ? 124 : 350;
                content.Children.Add(items);
                card.Child = content;
                stage.Children.Add(card);
            }
            if (stage.Children.Count == 0)
                stage.Children.Add(
                    UI.label("Nenhum item encontrado. Adicione um grupo ou altere sua busca.", 14, UI.muted));
        }
    }
}
