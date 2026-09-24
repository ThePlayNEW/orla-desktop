# Validation

## Local environment

Windows 10 Pro 22H2, build 19045, x64. Tested on 24 September 2026.

## Automated checks

All 16 model/persistence checks pass:

- Unicode paths and duplicate-reference protection.
- Missing-path rejection.
- Cross-group moves and within-group ordering.
- Preservation of the original file's contents.
- Atomic replacement and backup creation.
- Configuration round-trip and recovery after corruption.
- Recovery preserved across another application restart.
- Startup enabled by default with first-run tracking.
- Duplicate-target protection for cross-group moves.
- Panel-size bounds.
- Empty-layout parsing, unsupported-version rejection and duplicate-ID rejection.

The interactive shell smoke test confirmed that icons are hidden only during desktop mode, restored afterwards, and all four panel windows are above the desktop shell in z-order. The restoration companion was also checked by forcibly ending the main process during desktop mode: the original icons returned.

UI captures were rendered from the native WPF controls and reviewed for layout, text clipping, contrast and scrolling. Documentation previews use non-personal demonstration data.

## Background sample

| Measurement | Result |
| --- | --- |
| Saved references | 52 |
| Processes | 2, including the restoration companion |
| Organization window | Closed |
| Combined working set | 86.5 MB |
| Combined private memory | 88.83 MB |
| Warm-up | 5 seconds |
| Sample interval | 10 seconds |
| Additional process CPU time | 0.0 seconds at the sampling resolution |
| Startup shortcut | Enabled and present |

This is a background sample on one computer, not a fixed memory guarantee or a visible-panel animation benchmark. Startup, opening the organization window, shell icon extraction and active interaction can temporarily consume more memory and CPU. CPU measurements below the sampling resolution do not prove that no instructions ran.

## Compatibility status

| Scenario | Status |
| --- | --- |
| Windows 10 22H2 x64 | Local build, rendering, shell integration and restoration tested |
| Windows 11 x64 | Supported API target; manual desktop testing pending |
| Wallpaper Engine | No wallpaper replacement or window reparenting; product-specific testing pending |
| Multiple monitors / mixed DPI | Primary work-area bounds implemented; broader testing pending |
| Explorer restart | Desktop mode suspends when the original icon-list handle disappears |
| Headless CI | Model tests only; no interactive desktop claims |

## Current limits

- Designed for ordinary collections of shortcuts; item grids are not virtualized.
- Panels use alpha transparency, not live background blur.
- Layout is constrained to the primary monitor.
- Newly created desktop items are not watched continuously. Add them by drag-and-drop or through the group menu.
- For external drops from Explorer, use the organization window. Desktop panels yield while another application has focus.
- If another tool also hides or manages Explorer's icons, use only one desktop-icon manager at a time.
- The portable binary is not code-signed.
