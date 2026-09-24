using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Orla
{
    public class Controller
    {
        public Store store;
        public bool desktopActive, exiting;
        public IntPtr shellOwner = IntPtr.Zero;
        public ManagerWindow manager;
        public List<PanelWindow> panels = new List<PanelWindow>();
        Forms.NotifyIcon tray;
        Native.EventProc eventProc;
        IntPtr hook, desktop;
        bool originalVisible, guardStarted, refreshQueued;
        string exe;
        public Controller(Store store, bool withTray)
        {
            this.store = store;
            exe = Process.GetCurrentProcess().MainModule.FileName;
            if (withTray)
            {
                tray = new Forms.NotifyIcon { Text = "Orla · Organizador de desktop",
                                              Icon = System.Drawing.Icon.ExtractAssociatedIcon(exe),
                                              Visible = true };
                var menu = new Forms.ContextMenuStrip();
                menu.Items.Add("Abrir central", null, delegate { showManager(null); });
                menu.Items.Add("Ativar / restaurar desktop", null, delegate { tryAction(toggleDesktop); });
                menu.Items.Add("Reposicionar painéis", null, delegate { resetPositions(); });
                menu.Items.Add(new Forms.ToolStripSeparator());
                menu.Items.Add("Sair e restaurar ícones", null, delegate { quit(); });
                tray.ContextMenuStrip = menu;
                tray.DoubleClick += delegate
                {
                    showManager(null);
                };
            }
        }
        public void tryAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Orla", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public void save()
        {
            tryAction(delegate { store.save(); });
        }
        public void changed()
        {
            save();
            if (refreshQueued)
                return;
            refreshQueued = true;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate {
                                                           refreshQueued = false;
                                                           refreshPanels();
                                                           if (manager != null)
                                                               manager.refresh();
                                                       }),
                                                       DispatcherPriority.Background);
        }
        public void notify(string text)
        {
            if (tray != null)
            {
                tray.BalloonTipTitle = "Orla";
                tray.BalloonTipText = text;
                tray.ShowBalloonTip(2500);
            }
        }
        public void showManager(string id)
        {
            if (manager == null)
                manager = new ManagerWindow(this);
            if (id != null)
                manager.select(id);
            manager.Show();
            manager.WindowState = WindowState.Normal;
            manager.Activate();
        }
        public void open(Entry e)
        {
            tryAction(delegate { Native.open(e.Path); });
        }
        public void addFiles(Group g)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true,
                                                              Title = "Adicionar referências ao grupo" };
            if (dialog.ShowDialog() == true)
            {
                foreach (string p in dialog.FileNames)
                    store.add(g, p);
                changed();
            }
        }
        public void addFolder(Group g)
        {
            using (var dialog = new Forms.FolderBrowserDialog {
                Description = "Escolha a pasta. Ela continuará no local original.",
                ShowNewFolderButton = false
            })
            {
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    store.add(g, dialog.SelectedPath);
                    changed();
                }
            }
        }
        public bool StartupEnabled
        {
            get {
                return File.Exists(StartupPath);
            }
        }
        string StartupPath
        {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Orla.lnk");
            }
        }
        public void setStartup(bool enabled)
        {
            if (!enabled)
            {
                if (File.Exists(StartupPath))
                    File.Delete(StartupPath);
                store.data.StartupConfigured = true;
                store.data.StartupEnabled = false;
                store.save();
                return;
            }
            Type type = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(type);
            dynamic shortcut = shell.CreateShortcut(StartupPath);
            try
            {
                shortcut.TargetPath = exe;
                shortcut.Arguments = "--desktop";
                shortcut.WorkingDirectory = Path.GetDirectoryName(exe);
                shortcut.Description = "Orla — organizador visual";
                shortcut.Save();
                store.data.StartupConfigured = true;
                store.data.StartupEnabled = true;
                store.save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
        }
        void startGuard()
        {
            if (guardStarted)
                return;
            string ready = Path.Combine(Path.GetDirectoryName(store.filePath),
                                        "guard-" + Process.GetCurrentProcess().Id + ".ready");
            if (File.Exists(ready))
                File.Delete(ready);
            var p = Process.Start(new ProcessStartInfo(
                exe, "--guard " + Process.GetCurrentProcess().Id + " " + desktop.ToInt64() + " " +
                         (originalVisible ? "1" : "0") + " \"" + ready +
                         "\"") { UseShellExecute = false, CreateNoWindow = true,
                                   WindowStyle = ProcessWindowStyle.Hidden });
            for (int i = 0; i < 60 && !File.Exists(ready) && !p.HasExited; i++)
                Thread.Sleep(50);
            if (!File.Exists(ready))
                throw new InvalidOperationException(
                    "A proteção de restauração não iniciou. Seus ícones foram mantidos.");
            File.Delete(ready);
            guardStarted = true;
        }
        public void toggleDesktop()
        {
            if (desktopActive)
                disableDesktop();
            else
                enableDesktop();
            if (manager != null)
                manager.refresh();
        }
        public void enableDesktop()
        {
            if (desktopActive)
                return;
            IntPtr current = Native.desktopList();
            if (current == IntPtr.Zero)
                throw new InvalidOperationException("O Explorer não expôs a área de trabalho. A central " +
                                                    "continua disponível; nada foi ocultado.");
            if (desktop == IntPtr.Zero)
            {
                desktop = current;
                originalVisible = Native.IsWindowVisible(desktop);
            }
            else if (desktop != current)
                throw new InvalidOperationException("O Explorer foi reiniciado. Encerre e abra o Orla " +
                                                    "novamente para reconectar os painéis.");
            startGuard();
            try
            {
                eventProc =
                    delegate(IntPtr h, uint ev, IntPtr hwnd, int obj, int child, uint thread, uint time)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(updateVisibility));
                };
                hook = Native.SetWinEventHook(3, 3, IntPtr.Zero, eventProc, 0, 0, 0);
                if (hook == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Não foi possível acompanhar o desktop. Nenhum ícone foi ocultado.");
                desktopActive = true;
                refreshPanels();
                Native.ShowWindow(desktop, 0);
                updateVisibility();
            }
            catch
            {
                disableDesktop();
                throw;
            }
        }
        public void disableDesktop()
        {
            desktopActive = false;
            if (hook != IntPtr.Zero)
            {
                Native.UnhookWinEvent(hook);
                hook = IntPtr.Zero;
            }
            foreach (var panel in panels)
                panel.Hide();
            if (desktop != IntPtr.Zero && Native.IsWindow(desktop))
                Native.ShowWindow(desktop, originalVisible ? 5 : 0);
        }
        public void refreshPanels()
        {
            foreach (var p in panels.ToArray())
            {
                Group g = store.data.Groups.FirstOrDefault(x => x.Id == p.GroupId);
                if (g == null || !g.Visible)
                {
                    p.Close();
                    panels.Remove(p);
                }
                else
                    p.refresh();
            }
            if (desktopActive)
            {
                foreach (var g in store.data.Groups.Where(x => x.Visible))
                {
                    if (!panels.Any(p => p.GroupId == g.Id))
                        panels.Add(new PanelWindow(this, g));
                }
                updateVisibility();
            }
        }
        public void updateVisibility()
        {
            if (!desktopActive || exiting)
                return;
            if (!Native.IsWindow(desktop))
            {
                disableDesktop();
                notify("O Explorer foi reiniciado. Reabra o Orla para reconectar os painéis.");
                return;
            }
            bool show = Native.desktopOrUs();
            foreach (var p in panels)
            {
                if (show)
                    p.showQuietly();
                else if (p.IsVisible)
                    p.Hide();
            }
        }
        public void resetPositions()
        {
            Discovery.position(store.data);
            changed();
        }
        public void quit()
        {
            if (exiting)
                return;
            exiting = true;
            disableDesktop();
            foreach (var p in panels.ToArray())
                p.Close();
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
            }
            Application.Current.Shutdown();
        }
    }
}
