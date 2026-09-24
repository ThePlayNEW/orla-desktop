# Architecture

Orla is a portable x64 WPF application targeting .NET Framework 4.8. It uses the standard library, WPF, a WinForms tray icon and a small Win32 interoperability layer.

| Component | Responsibility |
| --- | --- |
| `Program` | Process modes, single-instance activation and application lifetime |
| `Controller` | Desktop lifecycle, tray actions and startup preference |
| `PanelWindow` | One translucent desktop group, dragging, resizing and collapse |
| `ManagerWindow` | On-demand group management, search and appearance settings |
| `UI` | Shared controls, native icon tiles and short interaction transitions |
| `Model` | Layout, references, validation and atomic persistence |
| `Discovery` | Shallow import of visible desktop items |
| `Native` | Shell icons, foreground events, z-order and icon restoration |
| `Tests` | Standalone model and persistence checks |
| `Demo` | Non-personal fixtures for documentation previews |

## Desktop integration

Orla locates Explorer's desktop icon list through its shell view. It does not change file attributes or move desktop files. Activating panels temporarily hides that icon list, preserving its previous visibility state.

Panels are ordinary non-topmost tool windows. Their z-order is placed just above the desktop shell. A foreground event hook shows them for desktop interaction and hides them while another application has focus. The code does not attach a window to a wallpaper renderer or modify its settings.

Explorer's internal shell-view hierarchy is not a formal application API. If it cannot be found, activation fails without hiding icons. If Explorer restarts, Orla suspends desktop mode and asks the user to reopen the app.

Before hiding icons, the main process starts a companion and waits for its readiness acknowledgement. The companion waits for main-process exit and restores the original icon visibility. Normal shutdown restores it immediately as well. The companion cannot restore a destroyed Explorer window; a restarted shell owns its new visibility state.

## Persistence

Each item stores an ID, display label and target path. Groups hold ordered references and bounded layout values. Moving or removing a reference never moves or deletes the underlying file.

Changes are written to a temporary file, flushed, then atomically replace the configuration while retaining its previous version. Invalid configuration is preserved as a timestamped copy. A valid backup is recovered; otherwise startup reports the problem rather than replacing user data.

## Rendering budget

Panels use alpha transparency rather than a full-screen blur surface. Opacity is limited to keep text readable. Animations are finite 140 ms opacity transitions, skipped when Windows disables client-area animation. There are no continuously running visual effects.

Desktop operation does not instantiate the organization window until it is needed. Native icon handles are released immediately after their bitmap is cached. The companion and single-instance activation listener block on OS wait handles instead of waking at intervals.

## Platform references

- [WPF transparency](https://learn.microsoft.com/dotnet/api/system.windows.window.allowstransparency)
- [Win32 foreground event hooks](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook)
- [Native shell icons](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow)
