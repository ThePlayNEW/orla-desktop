**English** · [Português](../pt-BR/guia.md)

# Orla Desktop user guide

Orla places translucent panels on the Windows desktop. Each panel shows shortcuts or the contents of a folder. Your files stay where they are; Orla only changes how you see them.

- [How panels work](#how-panels-work)
- [First run](#first-run)
- [Ready-made panels](#ready-made-panels)
- [Collections and folder panels](#collections-and-folder-panels)
- [Moving, resizing and arranging](#moving-resizing-and-arranging)
- [Items in panels](#items-in-panels)
- [Drag and drop](#drag-and-drop)
- [Showing panels in front](#showing-panels-in-front)
- [Clean desktop](#clean-desktop)
- [The Orla window](#the-orla-window)
- [Keyboard](#keyboard)
- [Multiple monitors](#multiple-monitors)
- [Upgrading from the 0.1 preview](#upgrading-from-the-01-preview)
- [Where your data lives](#where-your-data-lives)
- [Troubleshooting](#troubleshooting)
- [Frequently asked questions](#frequently-asked-questions)
- [Uninstall](#uninstall)

## How panels work

Panels sit on the same layer as the desktop icons, inside the Explorer window that holds those icons. As a result:

- **Win+D** and the show desktop button at the end of the taskbar leave the panels in view.
- A panel never covers an open application. When a window is above the desktop, it is also above the panels.
- In desktop areas without a panel, everything works as it does in Windows: rubber-band selection, the right-click menu and dragging files onto the desktop.

When you need a panel while windows are open, use **Ctrl+Alt+Space**. See [Showing panels in front](#showing-panels-in-front).

The Orla icon sits in the notification area, next to the clock. A click opens the Orla window. A right-click shows a menu with **Open Orla**, **Show panels in front**, **Lock panels**, **Clean desktop** and **Quit and restore the desktop**.

## First run

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/welcome-dark.png">
  <img src="../images/welcome-light.png" width="720" alt="Orla's welcome screen with the two ways to start">
</picture>

The first time Orla opens, the **Welcome to Orla Desktop** screen offers two ways to start. Pick one and click **Start**. The screenshots show the Portuguese interface; the options are the same in every language.

| Option | What happens |
| --- | --- |
| **Keep my icons** (selected by default) | The Windows icons stay as they are. In the next step, **Choose your first panels**, you tick the [ready-made panels](#ready-made-panels) you want and click **Start**. **Back** returns to the first step. |
| **Organize my desktop** | Desktop items become collections: **Apps** for shortcuts, **Folders** for folders and **Files** for everything else. Orla also creates the **Quick access** panel and an **On the desktop** panel that shows only what is not in another panel yet. **Clean desktop** is turned on. |

Neither option moves, renames or deletes a file. You can change everything later.

## Ready-made panels

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/presets-dark.png">
  <img src="../images/presets-light.png" width="720" alt="The Choose your first panels screen, with ready-made panel cards to tick">
</picture>

Ready-made panels help you start quickly. They appear in the second step of the first run and, later, under the **New panel** button on the **Panels** page. Orla lists only the ones that make sense on your computer.

| Panel | Type | What it shows |
| --- | --- | --- |
| **Quick access** | Collection | This PC, Downloads, Documents, Pictures and Recycle Bin |
| **Downloads** | Folder panel | Your Downloads folder |
| **Apps** | Collection | The program shortcuts on your desktop. Listed only if there are any. |
| **Games** | Collection | Games and launchers found on the desktop and in the Start menu |
| **Documents** | Folder panel | Your Documents folder |
| **Pictures** | Folder panel | Your Pictures folder, with thumbnails |
| **Screenshots** | Folder panel | The Screenshots folder inside Pictures. Listed only if it exists. |
| **On the desktop** | Folder panel | What is on the desktop and not in any panel yet |
| **Work** | Collection | Empty, for you to fill |
| **Study** | Collection | Empty, for you to fill |

On first run, **Quick access**, **Downloads** and **Apps** come ticked.

The **Games** panel recognizes a shortcut by where it points: Steam links (`steam://`) and Epic links (`com.epicgames.launcher://`), Riot, EA, Ubisoft, Battle.net, GOG and Rockstar links, or the folders where those stores and Xbox install games. If a game is missing, drag its shortcut onto the panel.

Building a ready-made panel never moves or copies a file. Collections hold shortcuts, and folder panels show each folder where it already is.

## Collections and folder panels

| | Collection | Folder panel |
| --- | --- | --- |
| Shows | Shortcuts to files, folders and locations anywhere | The real contents of one folder |
| When you drop files | Creates shortcuts; the files stay where they are | Moves or copies the files into the folder |
| When you remove an item | Removes only the shortcut | Not applicable; the panel always mirrors the folder |
| Updates | A shortcut follows its file when you rename it in File Explorer | Automatically, when something in the folder changes |

### Collections

A collection gathers shortcuts to what you use, wherever it lives: a project folder on another drive, a spreadsheet in Documents, an application. The file never moves.

If the file is renamed in File Explorer, the collection item follows it. If the file is deleted, or its drive is disconnected, the item is dimmed and marked with a warning sign. Its tooltip says "This item is no longer here." Use the item's menu to take it out of the panel.

To add items without dragging, open the panel's **···** menu and choose **Add files…** or **Add folder…**.

### Folder panels

A folder panel shows what is inside a folder, folders first and in alphabetical order, as File Explorer sorts them. Hidden and system files are not shown. The panel updates on its own when something in the folder is created, deleted or renamed.

The **On the desktop** panel, created by **Organize my desktop**, shows your desktop and the Windows public desktop. In its **···** menu, **Show only what is not in another panel** hides items that are already in a collection. Anything you save to the desktop later appears there until you organize it.

If a panel's folder cannot be found, for example because the drive is disconnected, the panel says: "This panel's folder was not found. Check that the drive is connected."

### Creating, hiding and removing panels

In the Orla window, under **Panels**, the **New panel** button opens a list with:

- the [ready-made panels](#ready-made-panels) available on your computer;
- **New collection**, which asks for a name and creates an empty collection;
- **Folder panel**, which asks for the folder the panel will show.

In the panel list:

- Each folder panel shows its folder's name. Hover over it to see the full path.
- The **On desktop** switch shows or hides each panel without deleting it.
- Each row's **···** menu has **Rename panel**, **Line colour**, **Open folder** (for folder panels) and **Remove panel…**.

On the panel itself, the **···** menu (**More options**) offers:

- In collections: **Add files…** and **Add folder…**.
- In folder panels: **Open folder**.
- In every panel: **Rename panel**, **Line colour**, **Collapse** or **Expand**, **Hide panel**, **Open Orla** and **Remove panel…**.

Removing a panel never deletes files. For a collection, only the shortcuts go away. For a folder panel, the folder stays intact.

**Line colour** changes the thin line under the title: **Sea glass**, **Sand**, **Coral**, **Sky** or **Moss**.

## Moving, resizing and arranging

- **Move:** drag the panel by its title. When you let go, it lines up with the screen edges and neighbouring panels. While you drag, the panel stays inside the screen's work area, with a small margin.
- **Resize:** drag any edge or corner. Width and height move in whole columns and rows of icons, so there is never an empty strip. The height fits the content up to the size you set; beyond that, the panel scrolls.
- **No overlap:** a panel never sits on top of another. If you drop or grow a panel onto another one, it moves to the nearest free spot.
- **Rename:** double-click the title, type the new name and press Enter. Esc cancels.
- **Collapse:** the chevron on the right of the title bar leaves only the title bar in view. Click it again to expand.
- **Lock:** in **General**, **Lock position and size** prevents accidental moves and resizes. The same setting is in the notification area menu as **Lock panels**.
- **Rearrange:** in **General**, **Rearrange panels** lines up every visible panel in the top-right corner of the main screen.

## Items in panels

| To | Do this |
| --- | --- |
| Open | Double-click or Enter |
| Select several | Ctrl+click, Shift+click, Ctrl+A, or drag a rectangle over an empty area of the panel |
| Change the displayed name (collections) | F2, or **Rename in panel** in the item menu |
| Take it out of the panel (collections) | Del, or **Remove from panel** in the item menu |
| Send it to another collection | Drag it there, or use **Move to** in the item menu |
| Put a folder panel's file in a collection | **Add to** in the item menu, or drag it to the collection |
| See the file in File Explorer | **Show in File Explorer** in the item menu |

**Rename in panel** changes only the displayed name. The file keeps its original name.

With several items selected, you can drag, open or remove them together.

## Drag and drop

| From | To | Result |
| --- | --- | --- |
| File Explorer or desktop | Collection | Creates shortcuts. The files stay where they are. |
| Collection | Another collection | Moves the shortcut to the other collection |
| Collection | The same collection | Changes the order of the items |
| Collection | File Explorer or desktop | Copies the file, or creates a shortcut if you hold Alt. The original never moves. |
| Collection | Folder panel | Copies the file into the folder. The original never moves. |
| File Explorer or desktop | Folder panel | Moves the file into the folder if it is on the same drive, or copies it if it is on another drive |
| Folder panel | Collection | Creates a shortcut to the file |
| Folder panel | File Explorer or desktop | Follows the normal Windows rules, because the file is real |

When dropping onto a folder panel, hold **Ctrl** to copy or **Shift** to move, as in File Explorer. The operation uses Windows' own dialog, with progress and name-conflict handling, and can be undone with Ctrl+Z in File Explorer.

## Showing panels in front

Normally, open windows sit above the panels. To drop a file from File Explorer onto a panel without minimizing anything:

1. Press **Ctrl+Alt+Space**. The panels appear in front of every window.
2. Drag the file from File Explorer onto the panel.
3. Press **Ctrl+Alt+Space** again, or Esc, to send the panels back to the desktop.

Opening an item or using **Show in File Explorer** also sends the panels back.

The same feature is in the notification area menu as **Show panels in front**, and in the Orla window as the **Show in front** button. While the panels are in front, that button reads **Back to the desktop**.

If another program already uses **Ctrl+Alt+Space**, Orla tells you and the feature stays available from the notification area. See [The shortcut does not work](#the-shortcut-does-not-work).

## Clean desktop

**Clean desktop** hides the Windows icons and leaves only the panels. It is off unless you choose **Organize my desktop** on first run. To turn it on or off, use **General > Clean desktop** or the **Clean desktop** item in the notification area menu.

While it is on:

- The Windows icons and rubber-band selection on the desktop are unavailable.
- Your files stay in the desktop folder. To see them, use the **On the desktop** panel created by **Organize my desktop**, or create a **Folder panel** for the desktop.

### How Orla protects your icons

Before hiding the icons, Orla starts a small safeguard process, a second copy of Orla without a window. It waits for Orla to end, however that happens, and then brings the icons back.

The icons come back when you:

- turn off **Clean desktop**;
- choose **Quit and restore the desktop** or **Quit Orla**;
- uninstall Orla;
- or when Orla crashes or is ended from Task Manager.

The state restored is your File Explorer setting, under **View > Show desktop icons** in the desktop's right-click menu. If you turned the icons off there yourself, they stay off.

If Orla cannot start the safeguard, it does not hide the icons and says: "Orla could not start the safeguard that brings the icons back. The Windows icons stay visible."

## The Orla window

Open it with a click on the notification area icon, from a panel's **···** menu (**Open Orla**), or by running Orla again.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/central-dark.png">
  <img src="../images/central-light.png" width="720" alt="The Panels page of the Orla window, with four demo panels">
</picture>

The window has four pages:

- **Panels:** create, show, hide and remove panels. See [Creating, hiding and removing panels](#creating-hiding-and-removing-panels).
- **Appearance:** theme, opacity, icons and animations, with a live preview.
- **General:** startup, **Clean desktop**, the shortcut, locking, language and **Rearrange panels**.
- **About:** version, your data folder, **User guide**, **Report a problem** and **Quit Orla**.

At the bottom of the sidebar, **Part of the Windows desktop** means the panels are on the desktop layer. If it says **Compatibility mode: the Windows desktop was not found.**, see [Troubleshooting](#orla-shows-compatibility-mode).

### Appearance

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/appearance-dark.png">
  <img src="../images/appearance-light.png" width="720" alt="The Appearance page with a panel preview over a wallpaper">
</picture>

| Setting | Options |
| --- | --- |
| **Theme** | **System** follows the Windows app theme as soon as you change it. **Light** and **Dark** fix a theme. |
| **Panel opacity** | From 75% to 95%. The minimum keeps text readable over any wallpaper. |
| **Icon size** | **Small**, **Medium** or **Large** |
| **Animations** | Short transitions when panels appear. If animations are off in Windows, Orla does not animate either. |

With Windows High Contrast on, Orla uses the system colours and turns off transparency.

### General

| Setting | What it does |
| --- | --- |
| **Start with Windows** | Opens your panels when you sign in. On by default. |
| **Clean desktop** | Hides the Windows icons. See [Clean desktop](#clean-desktop). |
| **Ctrl+Alt+Space shortcut** | Turns the shortcut that brings panels to the front on or off |
| **Lock position and size** | Prevents moving or resizing panels |
| **Language** | **System** follows the Windows display language. You can also pick Português (Brasil), English, Español, Français, Deutsch or Italiano. The change applies immediately. |
| **Rearrange panels** | The **Rearrange** button lines up the visible panels in the top-right corner of the main screen |
| **Automatic updates** | Installed version only. On by default. See [Updates](#updates). |

### Updates

The installed version looks for a new release on GitHub at most once a day. When it finds one, it downloads it in the background and applies it the next time Orla closes. The check asks GitHub for this repository's list of releases and sends no personal data. When a new version is ready, the **About** page shows a **Restart and update** button to install it right away. To turn updates off, use **Automatic updates** in **General**.

The portable version does not update itself. To update it, quit Orla, download the new ZIP from the [releases page](https://github.com/ThePlayNEW/orla-desktop/releases/latest) and extract it in place of the old folder. Your panels live in `%LOCALAPPDATA%\Orla` and are not affected.

## Keyboard

| Key | Where | Action |
| --- | --- | --- |
| Ctrl+Alt+Space | Anywhere | Brings the panels to the front, or sends them back |
| Esc | Panels in front | Sends the panels back to the desktop |
| Enter | Item | Opens it |
| Arrow keys | Item | Moves the selection |
| Ctrl+A | Panel | Selects every item |
| F2 | Collection item | **Rename in panel** |
| Del | Collection item | **Remove from panel** |
| Menu key | Item | Opens the item menu |
| Enter or Esc | Title being edited | Confirms or cancels the new name |

## Multiple monitors

You can place panels on any monitor. Each panel uses the scale of the monitor it is on and stays inside that monitor's work area. If a monitor is disconnected, its panels move to the nearest remaining monitor. **Rearrange panels** brings every visible panel to the main screen.

Monitors with different scales, such as a laptop at 150% with an external monitor at 100%, have not been tested yet. If something looks out of place in that setup, [report it](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose).

## Upgrading from the 0.1 preview

If you used the 0.1 preview, version 1.0 converts its file the first time it opens:

- Each group becomes a collection, with the same items, a similar size and a matching colour.
- Panels are rearranged from the top-right corner of the main screen, because in the preview they could sit over the Windows icon column, which is visible again now.
- The Windows icons are shown again, because **Clean desktop** is off. If you liked the preview's desktop without icons, turn on **Clean desktop** in **General**.
- The preview's shortcut in the Windows Startup folder is removed. Version 1.0 starts with Windows through a per-user startup entry instead.
- Orla shows the notice "Your groups from the previous version are now collections, and the Windows icons are visible again. To hide them, turn on Clean desktop."

The preview's original file is kept, unchanged, as `layout.json.preview` next to `layout.json`. The preview cannot read the 1.0 file, so to go back to it, quit Orla, delete `layout.json` and rename `layout.json.preview` to `layout.json`. The two versions never run at the same time.

## Where your data lives

Everything is in `%LOCALAPPDATA%\Orla`, for both the installed and the portable version. Under **About**, the **Open folder** button opens it.

| File | Contents |
| --- | --- |
| `layout.json` | Panels, items, positions and settings |
| `layout.json.bak` | The previous version, created on every save |
| `layout.json.preview` | The 0.1 preview file, kept unchanged during the upgrade, if you used the preview |
| `layout.json.corrupt-<date>` | A copy of a file that could not be read, kept so nothing is lost |

Orla writes to a temporary file first and then swaps it in, so a power cut during a save does not corrupt the layout. If `layout.json` cannot be read, Orla uses the `.bak` file and says: "Your panels were recovered from the last saved copy."

The layout stores only paths and names. To back it up, copy `layout.json`.

Orla has no telemetry. Its only network access is the installed version's update check, described in [Updates](#updates).

## Troubleshooting

### The Windows icons are gone

1. Open Orla and check whether **Clean desktop** is on under **General**. If it is, turn it off.
2. If Orla is not running, right-click an empty area of the desktop and choose **View > Show desktop icons**.
3. If the icons still do not appear, restart Explorer: open Task Manager (Ctrl+Shift+Esc), find **Windows Explorer** on the **Processes** tab, right-click it and choose **Restart**.

Your files never leave the desktop folder, even while the icons are hidden.

### The shortcut does not work

If another program already uses **Ctrl+Alt+Space**, Orla shows "Another program already uses Ctrl+Alt+Space" and the **Ctrl+Alt+Space shortcut** switch stays off. Use **Show panels in front** from the notification area menu, or free the shortcut in the other program and turn the switch on again in **General**.

### Windows showed a SmartScreen warning

Orla's executables are not code-signed yet, and SmartScreen warns about programs without a reputation. Check that you downloaded the file from this repository's [releases page](https://github.com/ThePlayNEW/orla-desktop/releases), then click **More info** and **Run anyway**. Signing through the SignPath Foundation is planned.

### Explorer restarted

When Explorer restarts, the desktop layer is recreated. Orla notices and puts the panels back in about a second, with **Clean desktop** as it was. If the panels do not return, quit from the notification area menu and open Orla again.

### A panel is off screen

Under **General**, click **Rearrange** next to **Rearrange panels**. Every visible panel moves to the top-right corner of the main screen.

### A panel disappeared

It may be hidden. Open Orla, go to **Panels** and turn on the panel's **On desktop** switch.

### Orla shows "Compatibility mode"

Orla could not find Explorer's desktop window, so it shows the panels as ordinary windows just above the desktop. This can happen when Explorer is not running or another program replaces the desktop. Restart Explorer as described above. If the message stays, [report it](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose) with your Windows version and wallpaper program.

### An item shows as missing

The file was deleted or is on a disconnected drive. Reconnect the drive, or use **Remove from panel** in the item menu.

## Frequently asked questions

**Does Orla move or delete my files?**
No. Collections only hold shortcuts. The only time a file changes place is when you drop it onto a folder panel, and that goes through Windows' own copy dialog, which you can undo.

**Can I use Orla with another desktop organizer?**
Use only one program that hides or manages desktop icons at a time. Two programs doing it at once can leave the icons in an unexpected state.

**Does Orla work with Wallpaper Engine?**
Yes. Testing was done with Wallpaper Engine running on Windows 10. Orla does not touch the wallpaper or Wallpaper Engine's windows. Lively uses the same structure but has not been tested yet.

**Does Orla work on Windows 11?**
The code handles the desktop structures of Windows 10 and Windows 11, including the one in 24H2. Manual testing on Windows 11 is still pending. If you use it, tell us how it went in [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose).

**How much memory does Orla use?**
About 100 MB with four panels, including the WPF runtime. When idle, processor use stays near zero, because Orla only does work when something changes.

**Can I take my panels to another computer?**
Yes. With Orla closed, copy `layout.json` into `%LOCALAPPDATA%\Orla` on the other computer. Items whose path does not exist there show as missing.

**How many panels can I have?**
Up to 40 panels, with up to 3,000 items in each collection.

## Uninstall

Closing, quitting or uninstalling Orla never leaves your desktop changed: the Windows icons go back to following your File Explorer setting.

- **Installed version:** open **Settings > Apps**, find **Orla Desktop** and click **Uninstall**. Starting with Windows is removed as well, and the Windows icons come back.
- **Portable version:** under **General**, turn off **Start with Windows**. Then choose **Quit and restore the desktop** from the notification area menu and delete the folder you extracted the ZIP into.

Your panels stay in `%LOCALAPPDATA%\Orla` in case you come back. To delete them, type `%LOCALAPPDATA%` in File Explorer's address bar, press Enter and delete the `Orla` folder.
