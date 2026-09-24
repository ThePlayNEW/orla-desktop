# Releasing

Notes for maintainers. Releases are built by `.github/workflows/release.yml` and packaged with
[Velopack](https://velopack.io).

## 1. Bump the version

Update the version in both files, in the same commit:

- `src/Orla/AssemblyInfo.cs`: `AssemblyVersion` and `AssemblyFileVersion`, for example `2.1.0.0`.
- `src/Orla/app.manifest`: the `version` attribute of `assemblyIdentity`, for example `2.1.0.0`.

`package.ps1` and the release workflow read the version from `AssemblyVersion`, using the first
three parts.

## 2. Try the package locally (optional)

```powershell
dotnet tool install -g vpk
./build.ps1; ./test.ps1; ./package.ps1
```

This writes `artifacts/orla-<version>-windows-x64.zip` and the Velopack files in
`artifacts/releases`, and prints the SHA-256 of each one. Without `vpk`, only the plain zip is built.

## 3. Tag

```powershell
git tag v2.1.0
git push origin v2.1.0
```

The tag must match `AssemblyVersion` (`v2.1.0` for `2.1.0.0`), or the workflow stops. A tag with a
suffix, such as `v2.1.0-beta.1`, creates a pre-release.

## 4. What the workflow produces

On a `v*.*.*` tag the workflow builds, runs the tests, downloads the previous release to build a
delta package, runs `vpk pack` through `package.ps1`, and runs `vpk upload github`, which creates a
**draft** release with:

| File | Purpose |
| --- | --- |
| `OrlaDesktop-win-Setup.exe` | Per-user installer, no administrator rights. Installs to `%LocalAppData%\OrlaDesktop`. |
| `OrlaDesktop-win-Portable.zip` | Portable copy. |
| `OrlaDesktop-<version>-full.nupkg` | Full update package. |
| `OrlaDesktop-<version>-delta.nupkg` | Delta from the previous release (not on the first release). |
| `releases.win.json`, `RELEASES` | Update feed read by Velopack. |

The installed app runs from `%LocalAppData%\OrlaDesktop\current\Orla.exe`. Velopack replaces the
contents of `current` on update but keeps the path, so the startup shortcut the app creates keeps
working. User data stays in `%LocalAppData%\Orla`, outside the install folder.

The app runs the Velopack install hooks at startup, but it does not check for updates yet. Updating
an installed copy on its own needs a call to Velopack's `UpdateManager` with a GitHub source.

## 5. Publish the draft

1. Open the draft under Releases on GitHub.
2. Write the release notes and check that the files above are attached.
3. Download `OrlaDesktop-win-Setup.exe` and test it on a clean machine or in Windows Sandbox.
4. Publish. The links in the README (`releases/latest/download/...`) point to the newest published
   release that is not a pre-release.

The next release workflow downloads the latest published release for the delta, so publish (or
delete) a draft before tagging the next version.

## 6. winget

After the release is published, submit or update the winget manifest. See
[packaging/README.md](packaging/README.md).

## Code signing

The release files are not signed yet, so Windows SmartScreen shows a warning when the installer is
run for the first time.

The plan is to apply to [SignPath Foundation](https://signpath.org), which provides free code signing
for open source projects built on a public CI system. After the project is accepted:

1. Create the project, signing policy and artifact configuration in SignPath.
2. Add the repository secret `SIGNPATH_API_TOKEN` and the variable `SIGNPATH_ORGANIZATION_ID`.
3. Uncomment the signing steps in `release.yml`. They sign `Orla.exe` and `Velopack.dll` before
   packing. `Setup.exe`, `Update.exe` and the portable launcher are created by `vpk pack` and need
   their own signing request.

A signature does not remove the SmartScreen warning right away. SmartScreen builds reputation for a
certificate and for each file over time, based on downloads, so the first signed releases may still
show a warning.
