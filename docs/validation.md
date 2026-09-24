# Validation

This page records what has been tested, how, and what has not been tested yet. Numbers come from one computer and are not guarantees.

## Version 2

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
| Migration | A version 1 file becomes collections with mapped tints, scaled positions and sizes in columns and rows; the Windows icons are shown again (Clean desktop off) |
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

Reports for any of these are welcome in [Issues](https://github.com/ThePlayNEW/orla/issues/new/choose).

## Version 1

Version 1 used top-level tool windows above the desktop instead of child windows. It was tested on Windows 10 Pro 22H2 with 11 xUnit tests and an interactive smoke test. A background sample with 52 saved references and the organization window closed measured 86.5 MB combined working set for the main process and the restoration companion, with no measurable CPU time over ten seconds. Those figures come from a different window model and a different number of panels, so they are not directly comparable with version 2.
