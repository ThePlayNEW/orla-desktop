# Security policy

[Português abaixo](#português)

## Supported versions

Security fixes are made for the latest release only. The installed version updates itself; portable copies should be replaced with the latest download.

| Version | Supported |
| --- | --- |
| Latest 2.x release | Yes |
| 1.x | No |

## Reporting a vulnerability

Please do not open a public issue for a security problem.

Report it privately through GitHub: go to the repository's [Security tab](https://github.com/ThePlayNEW/orla/security) and choose **Report a vulnerability**, or open the [private reporting form](https://github.com/ThePlayNEW/orla/security/advisories/new) directly. Only the maintainer can see the report.

Include:

- the Orla version and your Windows version;
- what an attacker could do, and what they would need first;
- steps to reproduce, or a proof of concept;
- whether the problem is already public.

You can expect a first reply within seven days. Once the problem is confirmed, a fix is prepared in a private fork, released, and then described in a published advisory that credits you, unless you prefer to stay anonymous.

## Scope

In scope: `Orla.exe` and its companion process, the installer and update packages published on the [releases page](https://github.com/ThePlayNEW/orla/releases), and the handling of `%LOCALAPPDATA%\Orla\layout.json`.

Out of scope: problems that need an attacker who already runs code as the same Windows user, since that user can change the desktop directly; issues in Windows, Explorer or third-party wallpaper programs; and the absence of code signing, which is already known and planned.

## Português

Não abra uma issue pública para relatar uma falha de segurança. Use o [formulário de relato privado](https://github.com/ThePlayNEW/orla/security/advisories/new) do GitHub, que só o mantenedor vê. Informe a versão do Orla e do Windows, o que um atacante conseguiria fazer e os passos para reproduzir. A primeira resposta chega em até sete dias. Correções saem apenas para a versão mais recente.
