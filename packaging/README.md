# Packaging

Orla Desktop is not in the Windows Package Manager (winget) repository yet. This folder holds the
manifest templates for the first submission. After that, `wingetcreate update` produces new versions
from the previous manifest, and the templates only serve as a reference.

## winget

`winget/` holds a manifest set for schema 1.9 with the identifier `ThePlayNEW.OrlaDesktop`:

| File | Content |
| --- | --- |
| `ThePlayNEW.OrlaDesktop.yaml` | Version manifest |
| `ThePlayNEW.OrlaDesktop.installer.yaml` | Installer: `OrlaDesktop-win-Setup.exe` from the GitHub release |
| `ThePlayNEW.OrlaDesktop.locale.en-US.yaml` | Default locale |
| `ThePlayNEW.OrlaDesktop.locale.pt-BR.yaml` | Portuguese (Brazil) |

The installer is the Velopack `Setup.exe`, so the manifest uses `InstallerType: exe`,
`Scope: user` and the `--silent` switch. Velopack installs to `%LocalAppData%\OrlaDesktop` and
registers the uninstall entry under the pack id, which is why `ProductCode` is `OrlaDesktop`.

### First submission

Do this only after the GitHub release is published (not a draft), because winget downloads the
installer from the release URL.

1. Copy `winget/` to a temporary folder and replace the placeholders:
   - `__VERSION__` with the version without the `v`, for example `1.0.0`.
   - `__SHA256__` with the SHA-256 of `OrlaDesktop-win-Setup.exe`. `package.ps1` prints it, or run
     `Get-FileHash OrlaDesktop-win-Setup.exe -Algorithm SHA256`.
2. Validate the manifests: `winget validate --manifest <folder>`.
3. Test the install in Windows Sandbox or a virtual machine:
   `winget install --manifest <folder>` and then `winget uninstall ThePlayNEW.OrlaDesktop`.
4. Install [wingetcreate](https://github.com/microsoft/winget-create) (`winget install Microsoft.WingetCreate`)
   and submit with a GitHub token that has the `public_repo` scope:

   ```powershell
   wingetcreate submit --prtitle "New package: ThePlayNEW.OrlaDesktop version 1.0.0" --token <token> <folder>
   ```

This opens a pull request in [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
The package is available only after that pull request is merged.

### Later versions

Once the package exists in winget-pkgs:

```powershell
wingetcreate update ThePlayNEW.OrlaDesktop --version 1.1.0 `
    --urls https://github.com/ThePlayNEW/orla/releases/download/v2.1.0/OrlaDesktop-win-Setup.exe `
    --release-notes-url https://github.com/ThePlayNEW/orla/releases/tag/v2.1.0 `
    --submit --token <token>
```

### Known issue

winget 1.29 can fail to run Velopack installers because of a directory sharing conflict
([microsoft/winget-cli#6527](https://github.com/microsoft/winget-cli/issues/6527)). If the validation
pipeline of the pull request fails at install time, check that issue before changing the manifest.
