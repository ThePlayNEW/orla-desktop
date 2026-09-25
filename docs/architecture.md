# Architecture

Orla Desktop is an x64 WPF application for .NET Framework 4.8. It uses WPF for drawing, a WinForms `NotifyIcon` for the notification area, a small Win32 interop layer, and [Velopack](https://velopack.io) for installation and updates. It has no other runtime dependencies.

This document describes Orla Desktop 1.0. User-facing behaviour is covered in the [user guide](en/guide.md).

## Components

| Component | File | Responsibility |
| --- | --- | --- |
| `Program` | `Program.cs` | Entry point, process modes (`--guard`, `--smoke`, `--render`, `--settings`), single instance, Velopack install and uninstall hooks |
| `Controller` | `Controller.cs` | Application lifetime: finds the desktop, builds panels, switches overlay mode, applies settings, the startup entry |
| `Desktop`, `IconGuard` | `Shell/Desktop.cs` | Locates Explorer's icon host; hides and restores the icon list; runs the companion process |
| `MessageWindow`, `DesktopWatch` | `Shell/Watch.cs` | Global hotkey, `TaskbarCreated`, theme and display broadcasts; WinEvent hooks on the desktop windows |
| `ReferenceWatch` | `Shell/ReferenceWatch.cs` | Follows the folders that hold collection items, so references survive renames |
| `DesktopArrivals` | `Shell/DesktopArrivals.cs` | Tells **Keep organized** what arrives on the desktop and what leaves it |
| `Shell` | `Shell/Shell.cs` | Shell names, icons and thumbnails on a background thread; open, reveal, native file operations |
| `Screens` | `Shell/Screens.cs` | Monitor geometry, clamping, snapping, free-spot search, default arrangement |
| `Tray` | `Shell/Tray.cs` | Notification area icon and menu |
| `Updates` | `Shell/Updates.cs` | Daily update check and background download for the installed version |
| `PanelHost` | `Panels/PanelHost.cs` | The native window of one panel: creation per mode, placement, DPI scaling, move and resize |
| `PanelView` | `Panels/PanelView.xaml(.cs)` | Panel content: header, tiles, selection, menus, drag and drop, folder watching |
| `CentralWindow` | `Central/CentralWindow.xaml(.cs)` | The Orla window: welcome and preset choice, panels, appearance, general, about |
| `Layout`, `Group`, `Entry` | `Model/Layout.cs` | The saved data |
| `Store` | `Model/Store.cs` | Validation, atomic persistence, recovery |
| `Organizer` | `Model/Organizer.cs` | **Let Orla organize**: sorting desktop items into categories and laying the panels out |
| `Presets`, `Games` | `Model/Presets.cs` | Ready-made panels, and recognition of game and launcher shortcuts |
| `PanelMetrics` | `Model/PanelMetrics.cs` | Panel geometry in columns and rows of tiles |
| `Text` | `Strings/Text.cs` | Interface strings from embedded JSON |
| `Theme` | `Themes/Theme.cs` | Palette selection, High Contrast, panel opacity |
| `Smoke`, `Render`, `Demo` | `Smoke.cs`, `Render.cs`, `Model/Demo.cs` | Integration check and documentation images with non-personal data |
| `Orla.Tests` | `tests/Orla.Tests` | xUnit tests for the model, persistence, layout, the organizer, presets and translations |

## Desktop layer integration

### Finding the icon host

Explorer keeps the desktop icons in a `SysListView32` inside a `SHELLDLL_DefView`. Which window holds that view depends on the Windows version and on whether a wallpaper engine is running:

| Situation | Parent of `SHELLDLL_DefView` |
| --- | --- |
| Windows 10, Windows 11 up to 23H2, no wallpaper engine | `Progman` |
| Windows 10, Windows 11 up to 23H2, after a wallpaper engine has sent `0x052C` | A top-level `WorkerW` |
| Windows 11 24H2 and later | `Progman`, which is now a layered window; the wallpaper `WorkerW` is its child |

`Desktop.find()` does not branch on the Windows version. It looks for `SHELLDLL_DefView` directly under `Progman`, then under any top-level window, then under any child of `Progman`, and records the parent it finds as the host. The same code therefore handles all three layouts. The Windows 11 24H2 path has been written against the documented structure but has not yet been exercised on real hardware (see [validation](validation.md)).

### Panels as children of the host

In desktop mode, every panel is an `HwndSource` created with `WS_CHILD` and the icon host as its parent, then placed at `HWND_TOP` among the host's children. This puts it above the icon view and inside the desktop's z-order band:

- **Win+D** and the show desktop button minimize or cover application windows, never the desktop, so panels stay visible.
- Application windows are always above the desktop, so panels never cover them.
- In desktop areas without a panel, input reaches the icon view unchanged: rubber-band selection, the context menu and drops onto the desktop behave as in Windows.

Orla never sends `Progman` the undocumented `0x052C` message, never changes the wallpaper, and never reparents or resizes windows it does not own. Wallpaper Engine and Lively keep full control of their `WorkerW`. WinEvent hooks are registered out of context (`WINEVENT_OUTOFCONTEXT`), so nothing is loaded into Explorer. The manifest requests `asInvoker`.

### Following Explorer

`DesktopWatch` registers WinEvent hooks filtered to Explorer's process for `EVENT_OBJECT_DESTROY`, `EVENT_OBJECT_PARENTCHANGE` and `EVENT_OBJECT_REORDER`:

- If the host or icon view is destroyed, or the icon view changes parent (Explorer restart, a wallpaper engine starting), the controller schedules a rebuild after 800 ms. `TaskbarCreated` triggers the same rebuild.
- If the host's children are reordered, each panel checks whether it is still above the icon view and re-raises itself only if needed. Reorder events are coalesced into one dispatcher call.

A rebuild calls `Desktop.find()` again, recreates every panel window around its existing `PanelView`, and reapplies the clean desktop state.

If no icon host is found, panels fall back to top-level tool windows placed just above the shell window (`PanelMode.Fallback`), and the Orla window shows "Compatibility mode".

### The shortcut, hiding and overlay mode

The global shortcut is a `RegisterHotKey` registration with `MOD_NOREPEAT`. The combination is stored as invariant text (`Control+Alt+Space` by default, see `Shell/Shortcut.cs`) and can be recorded again in **General**; while the recorder listens, the registration is paused so the current combination reaches the window. If Windows refuses a combination because another program owns it, the previous one stays.

`Controller.onHotkey` picks the action from the foreground window. When the desktop is in view (the foreground window is `Progman`, `WorkerW`, the taskbar or Orla itself), the shortcut hides the panels (`SW_HIDE` on each panel window) or shows them again; with Clean desktop on, the icon guard lets the Windows icons show while the panels are hidden. When an application is in front, the shortcut switches the panels to `PanelMode.Overlay`. Panels always start visible; the hidden state is not saved.

A child window cannot be made topmost, so the overlay switch destroys each panel's native window and creates a new top-level `WS_POPUP` with `WS_EX_TOPMOST | WS_EX_TOOLWINDOW`, re-attaching the same `PanelView`. Switching back rebuilds the child window. Because the view survives, selection, scroll position and folder watchers are unaffected.

Overlay mode ends on the shortcut, when an item is opened or revealed, and from the tray or the Orla window. Esc also ends it, but only while a panel has keyboard focus.

### Clean desktop and the icon guard

Clean desktop hides the `SysListView32` with `ShowWindow(SW_HIDE)`. It does not change file attributes, the `HideIcons` registry value or any Explorer setting.

Before hiding, `IconGuard` starts a second copy of `Orla.exe` with `--guard <pid> <hwnd> <visible> <ready-file>`. The companion opens the main process, writes the ready file, and blocks on `WaitForExit`. When the main process ends for any reason, including a crash or Task Manager, the companion shows or hides the icon list according to the saved target and exits. The companion is started and its ready file awaited (up to three seconds) on a background task. The icons are hidden only after the dispatcher receives a positive result; if the guard does not start, the icons stay visible and the user is told.

The restore target is the user's Explorer setting (`HideIcons` under `Explorer\Advanced`), read when hiding starts. It is not the window's current visibility, because a previous crash elsewhere could have left the list hidden. When clean desktop is off, `repair()` shows an icon list that another program left hidden against that setting.

The guard cannot restore an icon list whose Explorer has already exited; a restarted Explorer creates a visible list of its own.

### Multiple monitors and DPI

The process is per-monitor DPI aware (PerMonitorV2). Positions are stored in physical pixels; sizes are stored as tile columns and rows. A child window inherits the DPI of its host, which is the primary monitor's, so `PanelHost` applies a `ScaleTransform` of `monitorScale × 96 / hostDpi` to panels on other monitors. Top-level windows in overlay mode get the correct DPI from WPF. `WM_DISPLAYCHANGE` and `WM_DPICHANGED` refresh every panel.

### Geometry

`PanelMetrics` defines a panel as fixed chrome plus whole square tiles, so resizing moves in whole columns and rows and never leaves an empty strip. The saved row count is a maximum: the height follows the content up to it and up to the room above the next panel or the bottom of the work area, then the panel scrolls.

`Screens` keeps every panel inside the work area of its monitor with a 12 px margin, snaps a dropped panel to edges, neighbouring panels and an 8 px grid, and resolves overlaps with `free()`, which tries positions adjacent to the panels in the way and falls back to a coarse scan. Resizing stops at other panels instead of pushing them.

## Presets

`Presets.All` lists the ready-made panels. Each preset has an availability check, so the welcome step and the **New panel** menu list only what exists on the computer: **Apps** needs non-game shortcuts on the desktop, **Games** needs at least one detected game, and the folder presets need their folder. Folder presets create folder panels; the others create collections of references. Building a preset never moves, copies or creates a file.

`Games.isGame` reads where a shortcut points: the `URL=` line of a `.url` file, or the target of a `.lnk` through `WScript.Shell`. A shortcut counts as a game if the target uses a store scheme (`steam://`, `com.epicgames.launcher://`, `uplay://`, `battlenet://`, `riotclient://`, `goggalaxy://`, `rockstar://` and others), lies in a known install folder (`steamapps\common`, `Epic Games`, `Riot Games`, `XboxGames` and others), or is a known launcher executable. `Games.find` scans the desktop folders and, recursively, the per-user and all-users Start menu, keeping one shortcut per name.

## Let Orla organize

`Organizer.plan` runs on a background thread. It lists the user and public desktops. A top-level folder that holds only shortcuts and scripts, directly or one level down, is treated as a way of sorting programs: its contents are classified instead of the folder, and a folder name such as `Jogos`, `Games`, `Dev` or `Utilitários` (accents and case ignored, six languages) becomes a hint that wins over guessing. Anything else is a work folder.

`Organizer.classify` decides the category of one item:

- A folder, or a shortcut to a folder, goes to **Folders**.
- A shortcut is judged by its target, through `Games.targetOf`: games and launchers first (`Games.isGameTarget`), then known executables and install folders for development, creative and utility tools, and **Apps** for the rest. Uninstallers go to **Utilities**.
- Scripts go to **Utilities** unless a folder hint says otherwise.
- Loose files are sorted by extension into **Documents**, **Pictures and videos** or **Files**. Installers and archives go to **Files**.

A category with a single item joins its fallback (**Apps** for tools, **Files** for documents and media). The panels remember their category in `Group.AutoCategory`.

`Organizer.build` makes a layout with the current settings. `Screens.arrange(left, right, iconSize, screen)` places the tool panels and **Quick access** from the top-left corner of the primary monitor and the work panels and the desktop inbox from the top-right corner. Without a clean desktop, everything goes on the right, clear of the Windows icons. It is the only layout Orla has: the first panels from presets and **Rearrange panels** go through `Organizer.arrange` too, where panels the organizer did not make count as work.

For each side it tries a few shapes: one to four columns of panels, three to five icons per row. Every panel in a side has the same width, so the columns line up. Panels keep their order and are split so the columns have about the same height. Each panel asks for as many rows as its items need, up to six. When a column is too tall, the panel with the most rows gives one up, and those items scroll. When every panel is down to one row and the column is still too tall, the last panels start collapsed.

Each shape has a cost:
- 3 for every row hidden behind scrolling.
- A little for its width, and for empty slots in last rows.
- A penalty when the free middle drops below a third of the screen: 1 per 20 px, plus 1 per 5 px below a quarter.

The pair of shapes with the lowest total wins. Panels never overlap and never leave the work area. `LayoutTests` checks this on four screen sizes, from 1366 × 768 to 4K at 200 %, with small, medium and crowded desktops. `Orla.exe --render <folder> --crowded` draws the preview for a crowded desktop on three screen sizes. The inbox is added only with a clean desktop, because visible Windows icons already show what is new. `CentralWindow` draws the plan as a map of the primary work area with the same panel geometry, then `Controller.applyOrganized` replaces the layout. The previous file is copied to `layout.json.before-organize-<timestamp>` and the previous panels are kept in memory for **Restore previous panels** until Orla closes.

With **Keep organized** (`Layout.AutoOrganize`), `DesktopArrivals` watches the desktop folders with `FileSystemWatcher`, including subfolders but acting only on the top level and two levels down. It ignores partial downloads and temporary names, and waits two seconds after the last change. `Controller.keepOrganized` then looks only at paths on the desktop itself or in a folder whose shortcuts are already in organized panels. An item that exists and is not shown by any panel is classified off the UI thread and added to the panel for its category, or its fallback; an item that is gone is removed from organized panels. A panel with automatic height gains a row when it needs one, up to six, but only when the space below it is free. Otherwise the new items scroll, so a panel never pushes its neighbours.

**Complete my panels** (`Organizer.complete`) works on a copy of the current layout, made by serializing and parsing it, so the preview never touches the live panels. Items no panel shows yet go to the panel people taught (see below), else to the panel of their category, else to a new panel for that category. It never goes past the 40-panel limit; what has no panel then stays in the inbox. New panels take the width most panels on their side have and go to the first free spot from their side's edge (`Screens.place`), keeping the usual gap. A panel that gets items grows a row at a time only into free space on its own monitor. When organizing again, the preview's switches start from the current settings, so nothing is turned back on by accident. Nothing is removed or moved.

Learning: when items move between panels, or a desktop item is dropped into a panel, `Controller.learn` records `Organizer.identity` (a shortcut's full target with its arguments, a link's URL, or else the file name, in lower case) against the Id of the panel it went to, in `Layout.Learned`. Arguments matter: Squirrel apps share `Update.exe` and browser web apps share one host. `Organizer.build` points rules at the new panel of the same category after a fresh layout; removing a panel removes its rules, and loading drops rules about panels that no longer exist. `Organizer.plan` reads identities only when there are rules, and a rule pointing to an organizer panel sets the category of a fresh plan. `Organizer.complete` and `Organizer.home` send the item to that panel directly, including panels people made.

With a second monitor, `Organizer.build` places the tools on the biggest other monitor, from the side facing the main screen, and the rest on the main screen's right. `CentralWindow` draws every monitor the plan uses, at their real positions.

The inbox, a desktop folder panel with **Show only what is not in another panel**, asks `Store.organized()` what to leave out. An item counts as shown when a panel points at it, or at something inside it, such as a folder of shortcuts whose shortcuts are in collections or a folder that has a folder panel.

## Threading

Desktop-mode panels are child windows of Explorer's desktop, so their thread's input queue is attached to Explorer's. Any stall on Orla's UI thread therefore stalls desktop input as well. The rules that follow from this:

- Shell icons, thumbnails and display names are resolved on one background STA thread (`Shell.icon`, `Shell.name`) and delivered through the dispatcher at `Background` priority. Up to 1,500 images are cached.
- File system notifications (`FileSystemWatcher`) arrive on pool threads and are marshalled to the dispatcher. Folder panel reloads are debounced by 250 ms; rebuilds by 800 ms.
- Folder listing for folder panels and the existence checks for collection items run on a background task. The result replaces the tiles in one dispatcher call.
- Presets are discovered and built on a background task, including the Start menu scan for game shortcuts.
- The icon guard companion is started off the UI thread (see [Clean desktop and the icon guard](#clean-desktop-and-the-icon-guard)).
- WinEvents are coalesced before they reach the dispatcher.
- The single-instance signal and the icon guard block on OS wait handles, not timers.
- No timer runs while Orla is idle.

### Drops onto folder panels

A drop handler runs inside the drag source's `DoDragDrop` loop, so a file operation started there would keep the source waiting and could show its dialog under the wrong owner. Orla therefore records the drop, reports `DragDropEffects.None` to the source, and starts `SHFileOperation` from the dispatcher after the drag has finished, owned by Orla's hidden message window.

Reporting no effect is deliberate. Under the optimized-move contract, a source that sees `Move` may delete the originals itself. Because Orla always reports `None` and performs the move on its own, a move that is skipped, cancelled or fails part-way can never cause the source to delete files.

## Rendering budget

- Panels use per-pixel alpha (`UsesPerPixelTransparency`) with a solid translucent brush. There is no live blur and no full-screen composition surface.
- Opacity is clamped to 75–95% so text stays readable over any wallpaper.
- The only animation is a 160 ms opacity fade when a panel appears. It is skipped when the user turns animations off or Windows disables client-area animation.
- The Orla window is created on first use and released when closed.
- Native `HBITMAP`s are deleted as soon as they are copied into a frozen `BitmapSource`.

Measured memory and CPU figures are in [validation.md](validation.md).

## Persistence

The layout lives in `%LOCALAPPDATA%\Orla\layout.json`, serialized with `JavaScriptSerializer`. It stores only names, paths, geometry and settings.

- **Saving:** write to `layout.json.tmp`, flush to disk, then `File.Replace` it over `layout.json`, which moves the previous file to `layout.json.bak`.
- **Loading:** the file is validated (version, unique IDs, known panel kinds, folder panels with a folder, at most 40 panels and 3,000 items per panel) and out-of-range values are clamped.
- **Recovery:** an unreadable file is copied to `layout.json.corrupt-<timestamp>`. If `layout.json.bak` is valid, it is restored and the user is notified; otherwise Orla reports the problem and leaves the files untouched.
- **References:** `ReferenceWatch` follows up to 64 folders that contain collection items. A rename updates the item's path; a deletion refreshes the panel so the item shows as missing. Removing a reference never touches the file.
- **Other versions:** only format `Version: 2` is read. The 0.1 preview's format 1 is treated like an unreadable file.

## Installation, startup and updates

Releases are packed by Velopack (`package.ps1`, run by the `release.yml` workflow on a `v*.*.*` tag) into two artifacts:

| Artifact | Contents |
| --- | --- |
| `OrlaDesktop-win-Setup.exe` | Per-user installer. No administrator rights. Adds a Start menu shortcut. |
| `OrlaDesktop-win-Portable.zip` | `Orla Desktop.exe` launcher at the top level, the application in `current/` |

`VelopackApp.Build().Run()` is the first call in `Main`, so install, update and uninstall hooks run before anything else. On uninstall, Orla removes its startup entry and restores the desktop icons. The layout in `%LOCALAPPDATA%\Orla` is left in place.

**Start with Windows** writes the value `Orla Desktop` under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. It is on by default until the user changes it.

The installed and the portable version check GitHub Releases one minute after they start and then every six hours, downloads a newer release in the background and applies it when Orla exits. If Windows shuts down with Orla open, `VelopackApp.Run` applies the downloaded release at the next start, before any panel is created, and restarts into it. The request only lists the repository's releases. **Automatic updates** in **General** turns this off. **Check now** on the About page runs the same check on demand, even with automatic updates off, and the card shows the state: checking, up to date, ready, failed, or a copy built from source (not installed, so nothing to update). `Layout.SeenVersion` lets the first start after an update show a notification that opens that version's release notes. There is no other network access and no telemetry.

## Localization

Interface text lives in `src/Orla/Strings/<language>.json`, embedded as resources. Six languages are complete: `pt-BR`, `en`, `es`, `fr`, `de` and `it`, with 165 strings each. `Text.get` looks up a key in the active language and falls back to English. XAML uses the `{l:T key}` markup extension. The `System` setting follows the Windows display language.

Two tests guard the translations: every language file must have exactly the same keys as English, and every key referenced in C# or XAML must exist. The **General** page lists each language by its own name, and a change applies immediately.

To add a language, copy `en.json` to `<code>.json`, translate the values, and register the code in `Text.cs` and in the language selector on the **General** page. The translation tests report any missing or extra key.

## Design system

The visual language, called Linha d'água, is defined in `src/Orla/Themes`:

| File | Contents |
| --- | --- |
| `Tokens.xaml` | Theme-independent tokens: type sizes, spacing, radii, glyph stroke, motion durations |
| `Light.xaml`, `Dark.xaml` | Colour palettes: surfaces, text, accent and the five panel tints |
| `Controls.xaml` | Styles for buttons, switches, segmented controls, sliders, menus and cards |
| `Glyphs.xaml` | Interface glyphs as geometries, generated |
| `Brand.xaml` | Brand mark and wordmark, generated |

`Theme.apply` picks the light or dark palette from the setting or from `AppsUseLightTheme`, and switches live on `WM_SETTINGCHANGE`. With High Contrast on, it builds a palette from `SystemColors` and disables translucency. The tray icon follows the taskbar theme (`SystemUsesLightTheme`).

Glyphs and brand art are authored as SVG in `assets/glyphs` and `assets/brand`. `tools/icons/export.mjs` renders them with resvg into `Orla.ico`, the tray icons, `docs/icon.png`, `Glyphs.xaml` and `Brand.xaml`:

```powershell
cd tools/icons
npm install
npm run export
```

Edit the SVG sources, never the generated files. The wordmark is outlined from the Figtree typeface; the app itself uses Segoe UI Variable on Windows 11 and Segoe UI on Windows 10.

## Platform references

- [Window features: child windows and z-order](https://learn.microsoft.com/windows/win32/winmsg/window-features)
- [SetWinEventHook](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwineventhook)
- [RegisterHotKey](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [IShellItemImageFactory](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitemimagefactory)
- [SHFileOperation](https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shfileoperationw)
- [High DPI desktop application development](https://learn.microsoft.com/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
