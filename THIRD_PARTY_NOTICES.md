# Third-party notices

Orla Desktop is released under the [MIT license](LICENSE). It includes or uses the following third-party work.

## Distributed with Orla

### Velopack

Installation, updates and the portable package.

- Project: https://github.com/velopack/velopack
- License: MIT
- Copyright (c) Velopack Ltd.

### Newtonsoft.Json

A dependency of Velopack, distributed with it.

- Project: https://github.com/JamesNK/Newtonsoft.Json
- License: MIT
- Copyright (c) 2007 James Newton-King

### Figtree

The Orla wordmark in `assets/brand` and in the app is outlined from the Figtree typeface. The font files themselves are not distributed.

- Project: https://github.com/erikdkennedy/figtree
- License: SIL Open Font License, Version 1.1 (https://openfontlicense.org)
- Copyright 2022 The Figtree Project Authors (https://github.com/erikdkennedy/figtree)

## Used only to build or test

These packages are not part of the application that users run.

| Package | Use | License |
| --- | --- | --- |
| [xunit](https://github.com/xunit/xunit) and xunit.runner.visualstudio | Unit tests | Apache License 2.0 |
| [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) | Test runner | MIT |
| [Microsoft.NETFramework.ReferenceAssemblies](https://github.com/microsoft/dotnet) | Building .NET Framework 4.8 with the .NET SDK | MIT |
| [@resvg/resvg-js](https://github.com/yisibl/resvg-js) | `tools/icons`: renders the SVG sources into icons and PNGs | MPL 2.0 |
| [vpk](https://github.com/velopack/velopack) | `package.ps1`: builds the installer and update packages | MIT |

The full license texts are available at the project links above.
