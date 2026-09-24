# Validation

This page records what has been tested, how, and what has not been tested yet. Numbers come from one computer and are not guarantees.

## 1.1.2

- **Let Orla organize** fits crowded desktops. Panels on each side share a width. The organizer picks how many columns of panels and icons per row to use, keeps the middle of the screen free, and lets big panels scroll instead of overlapping. On a small screen, the last panels start collapsed.
- The preview shows each panel's item count and draws collapsed panels as headers.
- With **Keep organized**, a panel grows only when the space below it is free.

`./test.ps1` runs 45 xUnit tests. The new ones check the layout on 1366 × 768, 1920 × 1080, 2560 × 1080 and 2880 × 1800 at 200 %, with 4, 6 and 8 panels per side holding up to 120 items each:
- Every panel stays inside the work area.
- No two panels overlap.
- Panels on one side share a width.
- A tidy desktop on 1920 × 1080 hides nothing and leaves a third of the screen free.
- Laying out again, as the preview does when an option changes, gives the same result as a fresh layout.
- A short screen uses more columns before panels run off the bottom.
- When the two sides cannot share a small, high-DPI screen, they still never overlap.

The preview was also rendered with 225 items on three screen sizes.

## 1.1.1

- The installed version looks for updates every six hours instead of once a day.
- German texts for the organizer use the same informal address as the rest of the interface.
- The guides now say that a downloaded update is also installed at the next start, which Velopack already did.

The 29 automated tests from 1.1 are unchanged and pass.

Update from 1.0 to 1.1, checked by hand on Windows 10 22H2 with the published releases:

1. The installed 1.0.0 asked GitHub for releases about a minute after it started.
2. It downloaded the 1.1.0 delta in the background and rebuilt the full package.
3. When Orla was started again, Velopack applied the downloaded version before the app ran and restarted it.
4. The installed version was then 1.1.0, with the same layout and settings.

## 1.1

### What changed

- **Let Orla organize** sorts the desktop into category panels, with a preview on a map of the screen, and **Keep organized** places new desktop items in the right panel. It is the recommended first-run choice and is available at any time from **Panels**.
- The desktop inbox is called **New on desktop**. It is created only with a clean desktop, treats folders of organized shortcuts as organized and says so when it is empty.
- The tile focus ring shows only while the keyboard is in use, and a tile no longer stays highlighted after a drag, a menu or a dialog.
- The keyboard shortcut is configurable, and it hides or shows the panels when the desktop is in view, or brings them to the front when an application window is.
- Resizing follows the pointer, shows the size in columns × rows and settles on whole tiles on release. New panels use automatic height.
- Moving sticks to screen margins and neighbouring panel edges.
- Dragging items shows a preview, an insertion line in collections and scrolls panels near their edges.
- Icons are requested at twice their size and scaled down with high-quality filtering; their alpha channel is read correctly.
- Theme and opacity changes reach the panels without a restart.

### Automated tests

`./test.ps1` runs 29 xUnit tests, and all of them pass locally. The new tests cover the shortcut and the organizer:

| Area | What is checked |
| --- | --- |
| Shortcut storage | A combination such as `Control+Shift+O` is read and written back in the same form |
| Shortcut fallback | A combination without Ctrl, Alt or Win, an unreadable value and a missing value all fall back to the default, `Control+Alt+Space` |
| Sorting files | Folders, documents, images, scripts, archives and installers land in their categories |
| Sorting shortcuts | Store links, code editors, streaming, peripheral software, FiveM and browsers are recognized from their targets |
| Folder hints | Folder names such as `Jogos`, `Utilitários` and `Dev tools` name a category, and the hint wins over guessing |
| Keep organized | A new item goes to its category's panel or to the fallback panel, and stays out when nothing fits |
| Inbox | An item counts as organized when a panel shows it or something inside it, and not for a similar name |
| Older layouts | A layout saved before the organizer loads, with **Keep organized** off |
| Desktop watcher | New items on a test desktop and in a sorted folder of shortcuts are reported in one batch; files deeper in work folders and partial downloads are not |

The 20 tests listed under [1.0](#10) are unchanged.

On the development computer, the organizer was run read-only against a real desktop with 32 shortcuts and scripts sorted in folders by the user. It produced **Apps**, **Development**, **Utilities**, **Games** and **Folders** matching the user's own grouping, using the folder names as hints.

### Manual checklist

Manual results for 1.1 will be recorded here. The checklist in [docs/en/manual-test.md](en/manual-test.md) ([Português](pt-BR/teste-manual.md)) now covers hiding and showing panels, changing the combination, drag previews and insertion lines, resizing and automatic height, live opacity, and organizing the desktop. The open items listed under 1.0 still apply.

## 1.0

### Environment

| | |
| --- | --- |
| Operating system | Windows 10 Pro 22H2, build 19045, x64 |
| Wallpaper | Wallpaper Engine running during every desktop test |
| Monitors | One |
| Date | 24 September 2026 |

### Automated tests

`./test.ps1` runs 20 xUnit tests, and all of them pass locally. The `build.yml` workflow runs the same tests on `windows-2022` and `windows-2025` runners.

| Area | What is checked |
| --- | --- |
| References | Unicode paths added once; missing paths rejected; moves between collections and reordering within one; moves that would duplicate a reference rejected; removing a reference leaves the file's contents intact |
| Folder panels | References cannot be added to a folder panel; a folder panel without a folder and unknown panel kinds are rejected |
| Persistence | Atomic save with backup and reload; recovery from a corrupt file using the backup, across restarts; empty layouts; unknown versions and duplicate IDs rejected; panel size and opacity clamped |
| Panel metrics | Panel sizes are whole columns and rows of tiles |
| Startup | Start with Windows is on until the user configures it |
| Migration | A 0.1 preview layout becomes collections with mapped tints, scaled positions and sizes in columns and rows; the Windows icons are shown again (Clean desktop off) |
| Presets | Store links such as `steam://` and `com.epicgames.launcher://` and known install folders are recognized as games; every preset has text in every language and builds a valid panel |
| Translations | All six language files (`pt-BR`, `en`, `es`, `fr`, `de`, `it`) have the same keys as English; every key used in C# or XAML exists |

Headless CI runs only these tests. It makes no claim about the interactive desktop.

### Smoke test

`Orla.exe --smoke report.json` loads four demo panels (two collections, a folder panel and Quick access) with temporary data, waits six seconds, writes a report and quits. It never hides the Windows icons and never reads personal files.

| Check | Result |
| --- | --- |
| Desktop icon host found | Yes |
| Panels created | 4 |
| Panels that are children of the icon host | 4 of 4 |
| Panels above the icon view in z-order | 4 of 4 |
| Windows icons left visible | Yes |
| Working set | About 100 MB, including the WPF runtime |
| CPU time while idle | About 0 ms during the sample |

Memory rises temporarily while the Orla window is open or while many shell icons load. A CPU reading near zero at the sampling resolution does not prove that no instructions ran; it shows that nothing polls.

### Crash restore

With **Clean desktop** on, the main `Orla.exe` process was ended from Task Manager. The companion process detected the exit and showed the Windows icons again, matching the Explorer setting.

### Manual checklist

The checklist for testers is in [docs/en/manual-test.md](en/manual-test.md) ([Português](pt-BR/teste-manual.md)). It covers Win+D, the show desktop button, selection and the context menu on the empty desktop, drag and drop into both panel types, overlay mode, resizing and snapping, the crash safeguard, an Explorer restart and theme switching. Results from other computers will be added here as they arrive.

### Not tested yet

| Scenario | Status |
| --- | --- |
| Windows 11 24H2 and 25H2 | The code handles the 24H2 desktop structure, where the icon view stays under a layered `Progman`. It has not been run on Windows 11 hardware yet. |
| Windows 11 23H2 and earlier | Same structure as Windows 10; not run |
| Multiple monitors with different scales | Per-monitor scaling is implemented; not tested |
| Lively Wallpaper | Uses the same `WorkerW` approach as Wallpaper Engine; not tested |
| Other desktop organizers running at the same time | Not supported; use one icon manager at a time |
| Folder panels on slow network shares | Listing runs on a background task; not tested on a real network share |
| Game detection | Tested with store links and install folders in unit tests; not tested against every launcher version |

Reports for any of these are welcome in [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose).

## 0.1 preview

The 0.1 preview used top-level tool windows above the desktop instead of child windows. It was tested on Windows 10 Pro 22H2 with 11 xUnit tests and an interactive smoke test. A background sample with 52 saved references and the organization window closed measured 86.5 MB combined working set for the main process and the restoration companion, with no measurable CPU time over ten seconds. Those figures come from a different window model and a different number of panels, so they are not directly comparable with 1.0.
