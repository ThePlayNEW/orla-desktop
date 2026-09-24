using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Forms = System.Windows.Forms;

namespace Orla
{
    // Notification area icon. Left click opens Orla; right click shows the menu, drawn with the app's own styles.
    public class Tray : IDisposable
    {
        readonly Controller controller;
        readonly IntPtr owner;
        readonly Forms.NotifyIcon icon;
        ContextMenu menu;

        public Tray(Controller controller, IntPtr owner)
        {
            this.controller = controller;
            this.owner = owner;
            icon = new Forms.NotifyIcon { Text = "Orla Desktop", Visible = true };
            refreshIcon();
            icon.MouseUp += (s, e) => {
                if (e.Button == Forms.MouseButtons.Left)
                    controller.showCentral(null);
                else if (e.Button == Forms.MouseButtons.Right)
                    showMenu();
            };
        }

        public void refreshIcon()
        {
            string name = Theme.taskbarUsesLightTheme() ? "tray-light.ico" : "tray-dark.ico";
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Orla;component/Assets/" + name));
            if (resource == null)
                return;
            using (resource.Stream)
            {
                var previous = icon.Icon;
                icon.Icon = new System.Drawing.Icon(resource.Stream, Forms.SystemInformation.SmallIconSize);
                previous?.Dispose();
            }
        }

        void showMenu()
        {
            menu = new ContextMenu { Placement = PlacementMode.MousePoint };
            menu.Items.Add(Menus.item("tray.open", "Glyph.Settings", () => controller.showCentral(null)));
            if (controller.Layout.Welcomed)
            {
                MenuItem front = Menus.check("tray.front", controller.Overlay, () => controller.setOverlay(!controller.Overlay));
                front.InputGestureText = controller.Layout.OverlayHotkey ? Text.get("shortcut.overlay") : "";
                menu.Items.Add(front);
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.check("tray.lock", controller.Layout.LockLayout, () => controller.setLock(!controller.Layout.LockLayout)));
                menu.Items.Add(Menus.check("tray.clean", controller.Layout.CleanDesktop,
                                           () => controller.setCleanDesktop(!controller.Layout.CleanDesktop)));
            }
            menu.Items.Add(new Separator());
            menu.Items.Add(Menus.item("tray.quit", "Glyph.Close", controller.quit));
            // The menu closes on outside clicks only while one of our windows is in the foreground.
            Native.SetForegroundWindow(owner);
            menu.IsOpen = true;
        }

        public void refreshMenu()
        {
            if (menu != null && menu.IsOpen)
                menu.IsOpen = false;
        }

        public void notify(string text)
        {
            icon.BalloonTipTitle = "Orla Desktop";
            icon.BalloonTipText = text;
            icon.ShowBalloonTip(3000);
        }

        public void Dispose()
        {
            icon.Visible = false;
            icon.Icon?.Dispose();
            icon.Dispose();
        }
    }
}
