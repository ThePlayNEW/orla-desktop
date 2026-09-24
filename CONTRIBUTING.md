# Contributing to Orla Desktop

[Português abaixo](#português)

Thank you for helping. Bug reports, test results, translations and code are all welcome.

## Ways to help

- **Test on Windows 11.** Orla has not been tested on Windows 11 24H2 or 25H2 yet. Run the [manual checklist](docs/en/manual-test.md) and open an issue with the results, even if everything passed.
- **Report a bug.** Use the [bug form](https://github.com/ThePlayNEW/orla/issues/new/choose). Include your Windows version, Orla version, wallpaper program and monitor setup.
- **Translate.** Interface text lives in `src/Orla/Strings/<language>.json`. See [Translations](#translations).
- **Change code.** For anything larger than a small fix, open an issue first so we can agree on the approach.

Security problems go through [SECURITY.md](SECURITY.md), never a public issue. Everyone taking part follows the [Code of Conduct](CODE_OF_CONDUCT.md).

## Building

You need Windows 10 or 11 and the [.NET SDK](https://dotnet.microsoft.com/download) 10 or newer. The SDK builds the .NET Framework 4.8 app; the 4.8 runtime ships with Windows.

```powershell
./build.ps1     # Release build
./test.ps1      # xUnit tests
./package.ps1   # packages in artifacts/ (the installer also needs: dotnet tool install -g vpk)
```

You can also open `Orla.slnx` in Visual Studio 2022 17.13 or newer with the .NET desktop development workload.

To try a build on the real desktop without touching your own layout:

```powershell
src/Orla/bin/Release/net48/Orla.exe --smoke report.json
```

This places four demo panels for a few seconds, checks that they are on the desktop layer above the icons, writes `report.json` and quits. It uses temporary data and never hides the Windows icons.

## Rules for changes

Read [docs/architecture.md](docs/architecture.md) before changing anything under `src/Orla/Shell` or `src/Orla/Panels`. In short:

- **Keep the UI thread free.** Desktop-mode panels share Explorer's input queue, so a slow call on Orla's UI thread freezes the desktop. File system, shell and network work belongs on a background thread.
- **Stay a guest on the desktop.** Do not send `0x052C` to `Progman`, change the wallpaper, inject into Explorer, reparent windows Orla does not own, or require administrator rights.
- **Never lose the icons.** Any path that hides the Windows icons must go through `IconGuard`, so they come back if Orla exits.
- **Never touch user files without a clear action.** Collections store references only. Moving or copying files happens only when the user drops onto a folder panel, through `SHFileOperation` with undo.
- **No polling.** Use WinEvents, file system notifications and wait handles.
- **Every string in every language.** Add each new key to all files in `src/Orla/Strings`. The tests fail otherwise.
- **Icons come from SVG.** Add or edit glyphs in `assets/glyphs` and brand art in `assets/brand`, then run `npm install` and `npm run export` in `tools/icons`. Do not edit `Glyphs.xaml`, `Brand.xaml` or the `.ico` files by hand.
- **Screenshots come from demo data.** Regenerate documentation images with `Orla.exe --render docs/images`. Never commit screenshots that show personal paths, user names or files.

## Code style

- Formatting follows `.editorconfig` and `.clang-format`: four spaces, Allman braces, LF line endings, 110 columns.
- Identifiers are in English. Methods, fields, parameters and locals use camelCase; types and serialized public properties use PascalCase; Win32 names keep their original spelling.
- Comments explain the reason behind the code. Keep them short.
- Add a test in `tests/Orla.Tests` for any change to the model, persistence or migration.
- Do not add a dependency without discussing it in an issue first.

## Translations

1. Copy `src/Orla/Strings/en.json` to `<code>.json`, for example `nl.json`.
2. Translate the values. Keep `{0}` placeholders and keyboard shortcuts as they are.
3. Register the language code as described in [docs/architecture.md](docs/architecture.md#localization).
4. Run `./test.ps1`. The translation tests report missing or extra keys.

Use the terms Windows itself uses in your language, such as the local names for File Explorer and the notification area.

## Pull requests

- Keep each pull request to one change, and describe what it changes and how you tested it.
- Run `./build.ps1` and `./test.ps1` before opening it. The `build.yml` workflow runs both on every push.
- If the change affects the desktop, say which Windows version, wallpaper program and monitor setup you tested on.
- Update the user guides in `docs/pt-BR` and `docs/en` when behaviour changes. Portuguese is the primary language; both versions should say the same thing.

By contributing, you agree that your contribution is licensed under the [MIT license](LICENSE).

## Português

Contribuições são bem-vindas: relatos de problemas, resultados de teste, traduções e código.

- **Teste no Windows 11.** O Orla ainda não foi testado no Windows 11 24H2 ou 25H2. Siga o [roteiro de teste manual](docs/pt-BR/teste-manual.md) e abra uma issue com o resultado.
- **Relate um problema** pelo [formulário](https://github.com/ThePlayNEW/orla/issues/new/choose), com a versão do Windows e do Orla, o programa de papel de parede e os monitores.
- **Traduza** a partir de `src/Orla/Strings/en.json`, seguindo os passos em [Translations](#translations).
- **Mude o código** depois de ler as regras acima. Para mudanças grandes, abra uma issue antes.

Issues e pull requests podem ser escritos em português ou em inglês. O código e os comentários ficam em inglês. Quando o comportamento mudar, atualize os dois guias, em `docs/pt-BR` e `docs/en`.
