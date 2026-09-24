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
  <a href="https://github.com/ThePlayNEW/orla-desktop/releases/latest"><img src="https://img.shields.io/github/v/release/ThePlayNEW/orla-desktop?label=vers%C3%A3o" alt="Versão mais recente"></a>
  <a href="https://github.com/ThePlayNEW/orla-desktop/releases"><img src="https://img.shields.io/github/downloads/ThePlayNEW/orla-desktop/total?label=downloads" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/ThePlayNEW/orla-desktop?label=licen%C3%A7a" alt="Licença MIT"></a>
  <a href="https://github.com/ThePlayNEW/orla-desktop/actions/workflows/build.yml"><img src="https://github.com/ThePlayNEW/orla-desktop/actions/workflows/build.yml/badge.svg" alt="Build"></a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/hero-dark.png">
  <img src="docs/images/hero-light.png" alt="Quatro painéis do Orla sobre um papel de parede: Entrada, Projetos, Acesso rápido e Documentos">
</picture>

## O que muda

- **Deixe o Orla organizar.** Ele lê o seu desktop e monta painéis por categoria (programas, desenvolvimento, jogos, utilitários, pastas, documentos), com as ferramentas à esquerda, o trabalho à direita e o centro livre para o papel de parede. Com **Manter organizado**, cada item novo no desktop entra sozinho no painel certo.
- **Painéis na camada do desktop.** Eles ficam junto dos ícones do Windows, então **Win+D** e o botão de mostrar a área de trabalho não os escondem, e nenhum aplicativo fica coberto por eles.
- **O desktop continua funcionando.** Nas áreas livres, a seleção com o mouse, o menu do botão direito e o arrastar de arquivos para o desktop se comportam como no Windows.
- **Coleções.** Guardam atalhos para arquivos e pastas de qualquer lugar. Se você renomear o arquivo no Explorador, o atalho acompanha. Tirar um item da coleção nunca apaga o arquivo.
- **Painéis de pasta.** Mostram uma pasta real, como Downloads, e se atualizam sozinhos quando algo muda nela.
- **Um atalho que faz o que o momento pede.** Com o desktop à vista, **Ctrl+Alt+Espaço** (padrão, dá para trocar) esconde ou mostra os painéis. Com uma janela na frente, traz os painéis para cima dela, para você soltar arquivos do Explorador sem minimizar nada.
- **Painéis organizados sozinhos.** Um painel nunca fica por cima de outro, e o tamanho avança em colunas e linhas inteiras de ícones.
- **Desktop limpo, se você quiser.** Uma opção esconde os ícones do Windows e deixa só os painéis. Um pequeno processo de proteção devolve os ícones se o Orla fechar ou travar.
- **Convive com Wallpaper Engine e Lively.** O Orla não troca o papel de parede, não injeta código no Explorador e não pede direitos de administrador.

## Instalação

Baixe na página de [versões](https://github.com/ThePlayNEW/orla-desktop/releases/latest):

| Arquivo | Para quem |
| --- | --- |
| [`OrlaDesktop-win-Setup.exe`](https://github.com/ThePlayNEW/orla-desktop/releases/latest/download/OrlaDesktop-win-Setup.exe) | A maioria das pessoas. Instala só para o seu usuário, sem pedir administrador, cria um atalho no menu Iniciar e se atualiza sozinho. |
| [`OrlaDesktop-win-Portable.zip`](https://github.com/ThePlayNEW/orla-desktop/releases/latest/download/OrlaDesktop-win-Portable.zip) | Quem prefere não instalar. Extraia em qualquer pasta e abra `Orla Desktop.exe`. Não se atualiza sozinho: para atualizar, baixe um ZIP novo. |

Os executáveis ainda não têm assinatura digital, então o Windows SmartScreen pode mostrar um aviso na primeira vez. Clique em **Mais informações** e depois em **Executar assim mesmo**. A assinatura pelo SignPath Foundation está planejada.

Requisitos: Windows 10 22H2 ou Windows 11, 64 bits, com o .NET Framework 4.8 (já incluído no Windows).

## Primeiros passos

1. Abra o Orla. Na tela **Bem-vindo ao Orla Desktop**, escolha como começar e clique em **Começar**:
   - **Deixe o Orla organizar** (recomendado, já vem marcada): o Orla monta os painéis por categoria e mostra uma prévia antes de aplicar. Veja [Deixe o Orla organizar](#deixe-o-orla-organizar).
   - **Manter meus ícones**: os ícones do Windows continuam onde estão, e você escolhe os primeiros painéis no passo seguinte.
2. Se escolheu **Manter meus ícones**, marque os painéis que quer em **Escolha seus primeiros painéis** e clique em **Começar**. Nenhum arquivo muda de lugar em nenhuma das opções.
3. Arraste um painel pelo título para posicioná-lo. Ele gruda nas margens da tela e nas bordas dos outros painéis.
4. Para mudar o tamanho, arraste qualquer borda ou canto do painel. Ao soltar, ele se ajusta a colunas e linhas inteiras de ícones.
5. Arraste arquivos do Explorador para um painel. Com o Explorador na frente, aperte **Ctrl+Alt+Espaço** para os painéis aparecerem por cima dele.
6. Para criar mais painéis, clique no ícone do Orla na bandeja e use **Novo painel**.

O Orla começa com **Iniciar com o Windows** ligado. Você pode desligar em **Geral**.

## Deixe o Orla organizar

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/organize-dark.png">
  <img src="docs/images/organize-light.png" width="720" alt="Prévia de Seu desktop, organizado: um mapa da tela com os painéis Apps, Desenvolvimento, Utilitários e Jogos à esquerda e Pastas, Documentos e Novos no desktop à direita">
</picture>

Na primeira vez, ou depois em **Painéis > Organizar para mim**, o Orla lê o desktop e separa o que encontra:

| Painel | O que entra |
| --- | --- |
| **Apps** | Navegadores, comunicação, música e os demais programas |
| **Desenvolvimento** | Editores de código, Git, Docker, bancos de dados, terminais |
| **Criação** | Edição de imagem e vídeo, design, transmissão |
| **Utilitários** | Scripts (`.bat`, `.cmd`, `.ps1`), periféricos, drivers e ferramentas do sistema |
| **Jogos** | Jogos e lançadores (Steam, Epic, EA, Riot, Rockstar, FiveM, Minecraft e outros) |
| **Pastas**, **Documentos**, **Imagens e vídeos**, **Arquivos** | Pastas e arquivos soltos no desktop, por tipo |

- Uma pasta que só guarda atalhos, como `Atalhos\Jogos`, é lida por dentro, e o nome dela vale como categoria.
- Uma categoria com um item só entra na mais próxima, para não sobrar painel com um ícone.
- As ferramentas ficam à esquerda da tela principal, o trabalho à direita, e o centro fica livre.
- Você vê a prévia antes. Nada muda de lugar no disco: os painéis guardam atalhos para os arquivos.
- Se você já tinha painéis, eles são substituídos. Uma cópia fica na pasta de dados, e **Voltar aos painéis anteriores** desfaz enquanto o Orla estiver aberto.

Com **Esconder os ícones do Windows** ligado, o painel **Novos no desktop** mostra o que ainda não está em nenhum painel. Com **Manter organizado** ligado, cada item novo no desktop entra sozinho no painel da categoria dele, e o que for apagado sai do painel. O que não se encaixar em nada fica em **Novos no desktop**.

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
| **Novos no desktop** | O que está no desktop e ainda não foi para nenhum painel. Combina com os ícones do Windows ocultos |
| **Trabalho**, **Estudos** | Coleções vazias para você preencher |

**Acesso rápido**, **Downloads** e **Aplicativos** já vêm marcados. Montar um painel pronto nunca move nem copia arquivos.

## Atalhos e gestos

| Onde | Ação | Resultado |
| --- | --- | --- |
| Desktop à vista | **Ctrl+Alt+Espaço** | Esconde ou mostra os painéis |
| Janela de aplicativo na frente | **Ctrl+Alt+Espaço** | Traz os painéis para a frente, ou devolve ao desktop |
| Painel na frente, com foco | Esc | Devolve os painéis ao desktop |
| Título do painel | Arrastar | Move o painel, que gruda nas margens da tela e nos outros painéis |
| Título do painel | Clique duplo | Renomeia o painel ali mesmo |
| Borda ou canto | Arrastar | Redimensiona o painel em colunas e linhas de ícones |
| Borda de cima ou de baixo | Clique duplo | Liga de novo a **Altura automática** |
| Item | Clique duplo ou Enter | Abre o item |
| Item | Ctrl+clique, Shift+clique, Ctrl+A | Seleciona vários itens |
| Área vazia do painel | Arrastar | Seleciona os itens dentro do retângulo |
| Item de coleção | F2 | Muda o nome mostrado no painel |
| Item de coleção | Del | Tira do painel, sem apagar o arquivo |
| Item | Setas | Move a seleção entre os itens |
| Item | Clique direito | **Abrir**, **Mostrar no Explorador**, **Renomear no painel**, **Mover para**, **Tirar do painel**, **Adicionar a** |
| Ícone na bandeja | Clique | Abre o Orla |

**Ctrl+Alt+Espaço** é o atalho padrão. Você pode trocá-lo em **Geral > Combinação de teclas**.

## Perguntas rápidas

O [guia de uso](docs/pt-BR/guia.md) explica cada recurso e traz soluções para os problemas mais comuns: ícones que não voltaram, atalho ocupado por outro programa, aviso do SmartScreen, painéis fora da tela e reinício do Explorador.

## Privacidade

O Orla não coleta dados e não tem telemetria. Os painéis ficam em `%LOCALAPPDATA%\Orla\layout.json`, com uma cópia anterior em `layout.json.bak`.

O único acesso à rede é a busca por atualizações da versão instalada: a cada poucas horas, o Orla pede ao GitHub a lista de versões deste repositório, sem enviar dados pessoais. Você pode desligar em **Geral > Atualizações automáticas**. Links como **Guia de uso** e **Relatar um problema** abrem no seu navegador só quando você clica.

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

Os detalhes estão em [docs/validation.md](docs/validation.md). Se você usar o Orla no Windows 11, um relato em [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose) ajuda bastante.

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
