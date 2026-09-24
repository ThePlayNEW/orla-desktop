**English** · [Português](../pt-BR/teste-manual.md)

# Manual test checklist

This checklist confirms that Orla works well with the Windows desktop on your computer. It takes about 20 minutes. No step deletes files, but use test files, such as an empty `.txt`, instead of important documents.

The most useful tests right now are on **Windows 11 24H2 or 25H2**, with **Lively Wallpaper**, and with **monitors at different scales**, because those setups have not been tested yet.

## Before you start

Write down the following. It goes in your test report.

| Item | Where to find it |
| --- | --- |
| Windows version | Press Win+R, type `winver` and press Enter |
| Orla version | Orla window, **About** page |
| Wallpaper program | Wallpaper Engine, Lively, another one or none |
| Monitors and scale | **Settings > System > Display**, the scale of each monitor |

Install Orla and, on the welcome screen, choose **Keep my icons**. Under **Choose your first panels**, leave **Quick access** and **Downloads** ticked, also tick **Work**, and click **Start**. Create a test file on the desktop, for example `orla-test.txt`.

## Steps

- [ ] **1. Win+D and show desktop.** Open two or three windows and press Win+D. The windows disappear and the panels stay on the desktop. Press Win+D again and the windows come back, above the panels. Repeat by clicking the far right end of the taskbar, past the clock.
- [ ] **2. Selection on the desktop.** In an empty area of the desktop, away from the panels, drag the mouse to draw a rectangle over some icons. The icons are selected, as they would be without Orla.
- [ ] **3. Menu and dragging on the desktop.** Right-click an empty area of the desktop: the normal Windows menu appears, with **View**, **Sort by** and **New**. Then drag `orla-test.txt` from a File Explorer folder to an empty area of the desktop: the file appears on the desktop.
- [ ] **4. Drag into a collection.** Drag the test file from File Explorer onto the **Work** panel. Windows' drag image shows over the panel. A shortcut appears in the panel and the file stays where it was. Rename the file in File Explorer: the panel item follows the new name.
- [ ] **5. Drag into a folder panel.** Drag the test file onto the **Downloads** panel. The Windows move dialog appears if needed, and the file goes to the Downloads folder. Press Ctrl+Z in a File Explorer window to undo.
- [ ] **6. Hide and show.** Click an empty area of the desktop and press **Ctrl+Alt+Space**. The panels disappear. Press it again: they come back. Check that the notification area menu shows **Hide panels** or **Show panels** accordingly.
- [ ] **7. Panels in front.** Maximize a File Explorer window and press **Ctrl+Alt+Space**. The panels appear in front of the window. Drag a file from File Explorer onto a panel. Click a panel's title and press Esc: the panels go back behind the windows.
- [ ] **8. Change the combination.** Under **General > Key combination**, click the button and press Ctrl+Alt+O. Repeat steps 6 and 7 with the new combination. Click **Restore default** and check that **Ctrl+Alt+Space** works again.
- [ ] **9. Select and drag items.** Double-click a panel title, type a new name and press Enter. In **Quick access**, select two items with Ctrl+click and drag them onto **Work**. A preview with the number 2 follows the pointer, a line shows where the items will land, and they stay selected in the destination.
- [ ] **10. Move and resize.** Drag a panel by its title close to the screen edge and to another panel: it sticks to the margin and to the neighbour's edge, with no overlap. Drag an edge and a corner: an accent line marks the edge, the size shows in columns × rows and, when you let go, the panel fits whole columns and rows. Double-click the bottom edge: **Automatic height** is ticked again in the **···** menu.
- [ ] **11. Clean desktop and safeguard.** Under **General**, turn on **Clean desktop**. The Windows icons disappear. With the desktop in view, press **Ctrl+Alt+Space**: the panels disappear and the Windows icons come back. Press it again. Then open Task Manager (Ctrl+Shift+Esc), go to the **Details** tab, find the two `Orla.exe` processes and end the one using more memory. Within a few seconds, the Windows icons come back. Open Orla again and turn off **Clean desktop**.
- [ ] **12. Explorer restart.** In Task Manager, on the **Processes** tab, right-click **Windows Explorer** and choose **Restart**. The taskbar flickers, and in about a second the panels are back on the desktop in the same place.
- [ ] **13. Theme and opacity.** In **Settings > Personalization > Colors**, switch the app mode between Light and Dark. The panels and the Orla window change theme right away. Under **Appearance**, move **Panel opacity**: the desktop panels change as you drag.
- [ ] **14. Let Orla organize.** In **Panels**, click **Organize for me**. The preview shows the panels on a map of the screen, tools on the left and work on the right. Turn off **Hide the Windows icons** and check that **New on desktop** leaves the map; turn it back on. Click **Organize**. With **Keep organized** on, save a `.pdf` file or create a shortcut on the desktop: within a few seconds it appears in the right panel. Delete it: it leaves the panel. Finally, click **Restore previous panels**.
- [ ] **15. New panel and quit.** Under **Panels**, click **New panel** and create **Pictures**: it appears on the desktop without covering another panel. Finally, right-click the Orla icon in the notification area and choose **Quit and restore the desktop**. The panels disappear and the Windows icons are as they were before the test.

## Reporting

Open a report in [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose) with the details from "Before you start" and the numbers of any steps that failed. If every step passed, a report saying so helps too, especially on Windows 11.

Before attaching screenshots, check that they do not show user names, personal paths or private files.
