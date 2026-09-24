[English](README.en.md) · **Português**

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="assets/brand/logo-dark.svg">
    <img src="assets/brand/logo-light.svg" width="260" alt="Orla Desktop">
  </picture>
</p>

<p align="center">
  Painéis translúcidos que organizam o desktop do Windows 10 e 11 sem tirar nenhum arquivo do lugar.
</p>

<p align="center">
  <a href="https://github.com/ThePlayNEW/orla/releases/latest"><img src="https://img.shields.io/github/v/release/ThePlayNEW/orla?label=vers%C3%A3o" alt="Versão mais recente"></a>
  <a href="https://github.com/ThePlayNEW/orla/releases"><img src="https://img.shields.io/github/downloads/ThePlayNEW/orla/total?label=downloads" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/ThePlayNEW/orla?label=licen%C3%A7a" alt="Licença MIT"></a>
  <a href="https://github.com/ThePlayNEW/orla/actions/workflows/build.yml"><img src="https://github.com/ThePlayNEW/orla/actions/workflows/build.yml/badge.svg" alt="Build"></a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/hero-dark.png">
  <img src="docs/images/hero-light.png" alt="Quatro painéis do Orla sobre um papel de parede: Entrada, Projetos, Acesso rápido e Documentos">
</picture>

## O que muda

- **Painéis na camada do desktop.** Eles ficam junto dos ícones do Windows, então **Win+D** e o botão de mostrar a área de trabalho não os escondem, e nenhum aplicativo fica coberto por eles.
- **O desktop continua funcionando.** Nas áreas livres, a seleção com o mouse, o menu do botão direito e o arrastar de arquivos para o desktop se comportam como no Windows.
- **Coleções.** Guardam atalhos para arquivos e pastas de qualquer lugar. Se você renomear o arquivo no Explorador, o atalho acompanha. Tirar um item da coleção nunca apaga o arquivo.
- **Painéis de pasta.** Mostram uma pasta real, como Downloads, e se atualizam sozinhos quando algo muda nela.
- **Ctrl+Alt+Espaço** traz os painéis para a frente das janelas, para você soltar arquivos do Explorador sem minimizar nada. Aperte de novo, ou Esc, para devolvê-los ao desktop.
- **Painéis organizados sozinhos.** Um painel nunca fica por cima de outro, e o tamanho avança em colunas e linhas inteiras de ícones.
- **Desktop limpo, se você quiser.** Uma opção esconde os ícones do Windows e deixa só os painéis. Um pequeno processo de proteção devolve os ícones se o Orla fechar ou travar.
- **Convive com Wallpaper Engine e Lively.** O Orla não troca o papel de parede, não injeta código no Explorador e não pede direitos de administrador.

## Instalação

Baixe na página de [versões](https://github.com/ThePlayNEW/orla/releases/latest):

| Arquivo | Para quem |
| --- | --- |
| [`OrlaDesktop-win-Setup.exe`](https://github.com/ThePlayNEW/orla/releases/latest/download/OrlaDesktop-win-Setup.exe) | A maioria das pessoas. Instala só para o seu usuário, sem pedir administrador, cria um atalho no menu Iniciar e se atualiza sozinho. |
| [`OrlaDesktop-win-Portable.zip`](https://github.com/ThePlayNEW/orla/releases/latest/download/OrlaDesktop-win-Portable.zip) | Quem prefere não instalar. Extraia em qualquer pasta e abra `Orla Desktop.exe`. Não se atualiza sozinho: para atualizar, baixe um ZIP novo. |

Os executáveis ainda não têm assinatura digital, então o Windows SmartScreen pode mostrar um aviso na primeira vez. Clique em **Mais informações** e depois em **Executar assim mesmo**. A assinatura pelo SignPath Foundation está planejada.

Requisitos: Windows 10 22H2 ou Windows 11, 64 bits, com o .NET Framework 4.8 (já incluído no Windows).

## Primeiros passos

1. Abra o Orla. Na tela **Bem-vindo ao Orla Desktop**, escolha como começar e clique em **Começar**:
   - **Manter meus ícones** (já vem marcada): os ícones do Windows continuam onde estão, e você escolhe os primeiros painéis no passo seguinte.
   - **Organizar meu desktop**: o que está no desktop vira coleções, os ícones do Windows ficam ocultos e um painel **Na área de trabalho** mostra o que ainda não foi organizado.
2. Se escolheu **Manter meus ícones**, marque os painéis que quer em **Escolha seus primeiros painéis** e clique em **Começar**. Nenhum arquivo muda de lugar em nenhuma das opções.
3. Arraste um painel pelo título para posicioná-lo. Ele se alinha às bordas da tela e aos outros painéis.
4. Para mudar o tamanho, arraste qualquer borda ou canto do painel.
5. Arraste arquivos do Explorador para um painel. Com o Explorador aberto, aperte **Ctrl+Alt+Espaço** para os painéis aparecerem na frente.
6. Para criar mais painéis, clique no ícone do Orla na bandeja e use **Novo painel**.

O Orla começa com **Iniciar com o Windows** ligado. Você pode desligar em **Geral**.

## Painéis prontos

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/presets-dark.png">
  <img src="docs/images/presets-light.png" width="720" alt="Tela Escolha seus primeiros painéis, com cartões de painéis prontos marcáveis">
</picture>

Na primeira vez, e depois em **Novo painel**, o Orla oferece painéis prontos. Só aparecem os que fazem sentido no seu computador.

| Painel | O que mostra |
| --- | --- |
| **Acesso rápido** | Este Computador, Downloads, Documentos, Imagens e Lixeira |
| **Downloads** | A pasta Downloads, sempre atualizada |
| **Aplicativos** | Os atalhos de programas do desktop, se houver |
| **Jogos** | Jogos e lançadores encontrados no desktop e no menu Iniciar: Steam, Epic, Riot, EA, Ubisoft, Battle.net, GOG, Rockstar e Xbox |
| **Documentos**, **Imagens**, **Capturas de tela** | As pastas correspondentes, sempre atualizadas |
| **Na área de trabalho** | O que está no desktop e ainda não foi para nenhum painel |
| **Trabalho**, **Estudos** | Coleções vazias para você preencher |

**Acesso rápido**, **Downloads** e **Aplicativos** já vêm marcados. Montar um painel pronto nunca move nem copia arquivos.

## Atalhos e gestos

| Onde | Ação | Resultado |
| --- | --- | --- |
| Qualquer lugar | **Ctrl+Alt+Espaço** | Traz os painéis para a frente das janelas, ou devolve ao desktop |
| Painel na frente | Esc | Devolve os painéis ao desktop |
| Título do painel | Arrastar | Move o painel, com encaixe nas bordas e nos outros painéis |
| Título do painel | Clique duplo | Renomeia o painel ali mesmo |
| Borda ou canto | Arrastar | Redimensiona o painel |
| Item | Clique duplo ou Enter | Abre o item |
| Item | Ctrl+clique, Shift+clique, Ctrl+A | Seleciona vários itens |
| Área vazia do painel | Arrastar | Seleciona os itens dentro do retângulo |
| Item de coleção | F2 | Muda o nome mostrado no painel |
| Item de coleção | Del | Tira do painel, sem apagar o arquivo |
| Item | Setas | Move a seleção entre os itens |
| Item | Clique direito | **Abrir**, **Mostrar no Explorador**, **Renomear no painel**, **Mover para**, **Tirar do painel**, **Adicionar a** |
| Ícone na bandeja | Clique | Abre o Orla |

## Perguntas rápidas

O [guia de uso](docs/pt-BR/guia.md) explica cada recurso e traz soluções para os problemas mais comuns: ícones que não voltaram, atalho ocupado por outro programa, aviso do SmartScreen, painéis fora da tela e reinício do Explorador.

## Privacidade

O Orla não coleta dados e não tem telemetria. Os painéis ficam em `%LOCALAPPDATA%\Orla\layout.json`, com uma cópia anterior em `layout.json.bak`.

O único acesso à rede é a busca por atualizações da versão instalada: no máximo uma vez por dia, o Orla pede ao GitHub a lista de versões deste repositório, sem enviar dados pessoais. Você pode desligar em **Geral > Atualizações automáticas**. Links como **Guia de uso** e **Relatar um problema** abrem no seu navegador só quando você clica.

## Compatibilidade

| Cenário | Situação |
| --- | --- |
| Windows 10 22H2, 64 bits | Testado |
| Windows 11 24H2 e 25H2 | Suportado pelo código, que encontra o desktop pela estrutura das janelas. Teste manual ainda pendente. |
| Wallpaper Engine | Testado com ele em execução no Windows 10 |
| Lively Wallpaper | Mesma abordagem do Wallpaper Engine. Teste manual pendente. |
| Vários monitores | Cada painel usa a escala do monitor onde está. Monitores com escalas diferentes ainda não foram testados. |
| Alto Contraste | Usa as cores do sistema, sem transparência |
| Idiomas | Português (Brasil), English, Español, Français, Deutsch, Italiano |
| Windows 32 bits ou ARM | Não suportado |

Os detalhes estão em [docs/validation.md](docs/validation.md). Se você usar o Orla no Windows 11, um relato em [Issues](https://github.com/ThePlayNEW/orla/issues/new/choose) ajuda bastante.

## Desinstalar

Fechar, sair ou desinstalar o Orla nunca deixa o desktop alterado: os ícones do Windows voltam a seguir a configuração do Explorador.

- **Versão instalada:** abra **Configurações > Aplicativos**, procure **Orla Desktop** e clique em **Desinstalar**. A inicialização com o Windows é removida junto.
- **Versão portátil:** no Orla, em **Geral**, desligue **Iniciar com o Windows**. Depois clique com o botão direito no ícone da bandeja, escolha **Sair e restaurar o desktop** e apague a pasta onde você extraiu o ZIP.

Nos dois casos, seus painéis continuam salvos em `%LOCALAPPDATA%\Orla`, caso você volte a usar o Orla. Para apagá-los, digite `%LOCALAPPDATA%` na barra de endereço do Explorador e exclua a pasta `Orla`. Seus arquivos não são afetados.

## Para desenvolvedores

O Orla é um aplicativo WPF para .NET Framework 4.8, compilado com o .NET SDK 10 ou mais recente.

```powershell
./build.ps1     # compila em Release
./test.ps1      # roda os testes xUnit
./package.ps1   # gera os pacotes em artifacts/
```

O instalador e o ZIP portátil são gerados pelo [Velopack](https://velopack.io) e exigem a ferramenta `vpk` (`dotnet tool install -g vpk`). Sem ela, `package.ps1` gera só um ZIP simples. As versões publicadas saem do workflow `release.yml` quando uma tag `v*.*.*` é enviada.

Opções de linha de comando:

```text
Orla.exe                      uso normal
Orla.exe --settings           abre a janela do Orla
Orla.exe --smoke report.json  verificação de integração com dados temporários, sem esconder ícones
Orla.exe --render <pasta>     gera as imagens da documentação com dados de demonstração
```

| Pasta | Conteúdo |
| --- | --- |
| `src/Orla` | Aplicativo: `Shell` (integração com o Windows), `Panels`, `Central` (janela do Orla), `Model`, `Strings`, `Themes` |
| `tests/Orla.Tests` | Testes de persistência, migração e tradução |
| `assets` | SVGs da marca e dos ícones da interface |
| `tools/icons` | Gera `.ico`, `Glyphs.xaml` e `Brand.xaml` a partir dos SVGs (`npm install` e `npm run export`) |
| `docs` | Guias, arquitetura, validação e imagens |

Leia [docs/architecture.md](docs/architecture.md) antes de mexer na integração com o desktop, e [CONTRIBUTING.md](CONTRIBUTING.md) antes de abrir um pull request. Para relatar uma falha de segurança, veja [SECURITY.md](SECURITY.md).

## Licença

[MIT](LICENSE) © Eduardo Torres. Componentes de terceiros estão listados em [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
