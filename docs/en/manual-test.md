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
- [ ] **3. Desktop menu.** Right-click an empty area of the desktop. The normal Windows menu appears, with **View**, **Sort by** and **New**.
- [ ] **4. Drag to the desktop.** Drag `orla-test.txt` from a File Explorer folder to an empty area of the desktop. The file appears on the desktop.
- [ ] **5. Drag into a collection.** Drag the test file onto the **Work** panel. A shortcut appears in the panel and the file stays where it was. Rename the file in File Explorer: the panel item follows the new name.
- [ ] **6. Drag into a folder panel.** Drag the test file onto the **Downloads** panel. The Windows move dialog appears if needed, and the file goes to the Downloads folder. Press Ctrl+Z in a File Explorer window to undo.
- [ ] **7. Panels in front.** Maximize a File Explorer window and press **Ctrl+Alt+Space**. The panels appear in front of the window. Drag a file from File Explorer onto a panel. Press Esc: the panels go back behind the windows.
- [ ] **8. Rename and select.** Double-click a panel title, type a new name and press Enter. Then select several items with Ctrl+click and with a rectangle dragged over an empty area of the panel.
- [ ] **9. Move and resize.** Drag a panel by its title close to the screen edge and to another panel: it snaps into place when you let go. Drag a panel over another one: it moves to a free spot, with no overlap. Drag an edge and a corner: the size changes in whole columns and rows of icons.
- [ ] **10. Clean desktop and safeguard.** In the Orla window, under **General**, turn on **Clean desktop**. The Windows icons disappear. Open Task Manager (Ctrl+Shift+Esc), go to the **Details** tab, find the two `Orla.exe` processes and end the one using more memory. Within a few seconds, the Windows icons come back. Open Orla again and turn off **Clean desktop**.
- [ ] **11. Explorer restart.** In Task Manager, on the **Processes** tab, right-click **Windows Explorer** and choose **Restart**. The taskbar flickers, and in about a second the panels are back on the desktop in the same place.
- [ ] **12. Theme.** In **Settings > Personalization > Colors**, switch the app mode between Light and Dark. The panels and the Orla window change theme right away.
- [ ] **13. New panel.** Click the Orla icon in the notification area, go to **Panels** and click **New panel**. The list shows the available ready-made panels, **New collection** and **Folder panel**. Create **Pictures** and check that it appears on the desktop without covering another panel.
- [ ] **14. Quit.** Right-click the Orla icon in the notification area and choose **Quit and restore the desktop**. The panels disappear and the Windows icons are as they were before the test.

## Reporting

Open a report in [Issues](https://github.com/ThePlayNEW/orla/issues/new/choose) with the details from "Before you start" and the numbers of any steps that failed. If every step passed, a report saying so helps too, especially on Windows 11.

Before attaching screenshots, check that they do not show user names, personal paths or private files.
