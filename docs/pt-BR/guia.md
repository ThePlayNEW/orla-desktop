[English](../en/guide.md) · **Português**

# Guia de uso do Orla Desktop

O Orla coloca painéis translúcidos no desktop do Windows. Cada painel mostra atalhos ou o conteúdo de uma pasta. Seus arquivos continuam onde estão; o Orla muda só a forma como você os vê.

- [Como os painéis funcionam](#como-os-painéis-funcionam)
- [Primeira vez](#primeira-vez)
- [Painéis prontos](#painéis-prontos)
- [Coleções e painéis de pasta](#coleções-e-painéis-de-pasta)
- [Mover, redimensionar e organizar](#mover-redimensionar-e-organizar)
- [Itens nos painéis](#itens-nos-painéis)
- [Arrastar e soltar](#arrastar-e-soltar)
- [Mostrar os painéis na frente](#mostrar-os-painéis-na-frente)
- [Desktop limpo](#desktop-limpo)
- [Janela do Orla](#janela-do-orla)
- [Teclado](#teclado)
- [Vários monitores](#vários-monitores)
- [Atualizar da versão 1](#atualizar-da-versão-1)
- [Onde ficam seus dados](#onde-ficam-seus-dados)
- [Solução de problemas](#solução-de-problemas)
- [Perguntas frequentes](#perguntas-frequentes)
- [Desinstalar](#desinstalar)

## Como os painéis funcionam

Os painéis ficam na mesma camada dos ícones do desktop, dentro da janela do Explorador que guarda esses ícones. Por isso:

- **Win+D** e o botão de mostrar a área de trabalho, no canto da barra de tarefas, deixam os painéis à vista.
- Um painel nunca cobre um aplicativo aberto. Quando uma janela está por cima do desktop, ela também está por cima dos painéis.
- Nas áreas do desktop sem painel, tudo funciona como no Windows: seleção com o mouse, menu do botão direito e arrastar arquivos para o desktop.

Quando você precisa de um painel com janelas abertas, use **Ctrl+Alt+Espaço**. Veja [Mostrar os painéis na frente](#mostrar-os-painéis-na-frente).

O ícone do Orla fica na bandeja, ao lado do relógio. Um clique abre a janela do Orla. O clique direito mostra o menu com **Abrir o Orla**, **Mostrar painéis na frente**, **Travar painéis**, **Desktop limpo** e **Sair e restaurar o desktop**.

## Primeira vez

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/welcome-dark.png">
  <img src="../images/welcome-light.png" width="720" alt="Tela de boas-vindas do Orla com as duas opções de início">
</picture>

Na primeira vez que o Orla abre, a tela **Bem-vindo ao Orla Desktop** oferece duas formas de começar. Escolha uma e clique em **Começar**.

| Opção | O que acontece |
| --- | --- |
| **Manter meus ícones** (já vem marcada) | Os ícones do Windows continuam como estão. No passo seguinte, **Escolha seus primeiros painéis**, você marca os [painéis prontos](#painéis-prontos) que quer e clica em **Começar**. **Voltar** retorna ao primeiro passo. |
| **Organizar meu desktop** | Os itens do desktop viram coleções: **Aplicativos** para atalhos, **Pastas** para pastas e **Arquivos** para o resto. O Orla também cria o painel **Acesso rápido** e um painel **Na área de trabalho**, que mostra só o que ainda não está em outro painel. O **Desktop limpo** fica ligado. |

Nenhuma das opções move, renomeia ou apaga arquivos. Você pode mudar tudo depois.

## Painéis prontos

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/presets-dark.png">
  <img src="../images/presets-light.png" width="720" alt="Tela Escolha seus primeiros painéis, com cartões de painéis prontos para marcar">
</picture>

Painéis prontos ajudam a começar rápido. Eles aparecem no segundo passo da primeira vez e, depois, no botão **Novo painel** da página **Painéis**. O Orla só mostra os que fazem sentido no seu computador.

| Painel | Tipo | O que mostra |
| --- | --- | --- |
| **Acesso rápido** | Coleção | Este Computador, Downloads, Documentos, Imagens e Lixeira |
| **Downloads** | Painel de pasta | A pasta Downloads |
| **Aplicativos** | Coleção | Os atalhos de programas do desktop. Só aparece se houver algum. |
| **Jogos** | Coleção | Jogos e lançadores encontrados no desktop e no menu Iniciar |
| **Documentos** | Painel de pasta | A pasta Documentos |
| **Imagens** | Painel de pasta | A pasta Imagens, com miniaturas |
| **Capturas de tela** | Painel de pasta | A pasta de capturas de tela dentro de Imagens. Só aparece se ela existir. |
| **Na área de trabalho** | Painel de pasta | O que está no desktop e ainda não foi para nenhum painel |
| **Trabalho** | Coleção | Vazia, para você preencher |
| **Estudos** | Coleção | Vazia, para você preencher |

Na primeira vez, **Acesso rápido**, **Downloads** e **Aplicativos** já vêm marcados.

O painel **Jogos** reconhece um atalho pelo lugar para onde ele aponta: links da Steam (`steam://`) e da Epic (`com.epicgames.launcher://`), links da Riot, da EA, da Ubisoft, da Battle.net, da GOG e da Rockstar, ou as pastas onde essas lojas e o Xbox instalam os jogos. Se um jogo não aparecer, arraste o atalho dele para o painel.

Montar um painel pronto nunca move nem copia arquivos. As coleções guardam atalhos, e os painéis de pasta mostram a pasta no lugar onde ela está.

## Coleções e painéis de pasta

| | Coleção | Painel de pasta |
| --- | --- | --- |
| Mostra | Atalhos para arquivos, pastas e locais de qualquer lugar | O conteúdo real de uma pasta |
| Ao soltar arquivos | Cria atalhos; os arquivos ficam onde estão | Move ou copia os arquivos para a pasta |
| Ao tirar um item | Remove só o atalho | Não se aplica; o painel sempre reflete a pasta |
| Atualização | O atalho acompanha o arquivo se você o renomear no Explorador | Automática, quando algo muda na pasta |

### Coleções

Uma coleção junta atalhos para o que você usa, venham de onde vierem: uma pasta de projeto em outro disco, uma planilha nos Documentos, um aplicativo. O arquivo nunca sai do lugar.

Se o arquivo for renomeado no Explorador, o item da coleção acompanha. Se ele for apagado, ou se o disco for desconectado, o item aparece esmaecido, com um sinal de aviso. Ao passar o mouse, a dica diz "Este item não está mais neste lugar." Use o menu do item para tirá-lo do painel.

Para adicionar itens sem arrastar, abra o menu **···** do painel e escolha **Adicionar arquivos…** ou **Adicionar pasta…**.

### Painéis de pasta

Um painel de pasta mostra o que está dentro de uma pasta, com pastas primeiro e em ordem alfabética, como no Explorador. Arquivos ocultos e de sistema não aparecem. O painel se atualiza sozinho quando algo é criado, apagado ou renomeado na pasta.

O painel **Na área de trabalho**, criado pela opção **Organizar meu desktop**, mostra a sua área de trabalho e a área de trabalho pública do Windows. No menu **···** dele, **Mostrar só o que não está em outro painel** esconde os itens que já estão em alguma coleção. Assim, o que você salvar no desktop depois aparece ali até você organizar.

Se a pasta de um painel não for encontrada, por exemplo porque o disco foi desconectado, o painel avisa: "A pasta deste painel não foi encontrada. Verifique se o disco está conectado."

### Criar, ocultar e remover painéis

Na janela do Orla, em **Painéis**, o botão **Novo painel** abre uma lista com:

- os [painéis prontos](#painéis-prontos) disponíveis no seu computador;
- **Nova coleção**, que pede um nome e cria uma coleção vazia;
- **Painel de pasta**, que pede a pasta que o painel vai mostrar.

Na lista de painéis:

- Cada painel de pasta mostra o nome da pasta. Pare o mouse sobre ele para ver o caminho completo.
- A chave **No desktop** mostra ou oculta cada painel sem apagá-lo.
- O menu **···** de cada linha tem **Renomear painel**, **Cor da linha**, **Abrir pasta** (em painéis de pasta) e **Remover painel…**.

No próprio painel, o menu **···** (**Mais opções**) oferece:

- Em coleções: **Adicionar arquivos…** e **Adicionar pasta…**.
- Em painéis de pasta: **Abrir pasta**.
- Em todos: **Renomear painel**, **Cor da linha**, **Recolher** ou **Expandir**, **Ocultar painel**, **Abrir o Orla** e **Remover painel…**.

Remover um painel nunca apaga arquivos. Em uma coleção, somem só os atalhos. Em um painel de pasta, a pasta continua intacta.

A **Cor da linha** muda a linha fina abaixo do título: **Vidro do mar**, **Areia**, **Coral**, **Céu** ou **Musgo**.

## Mover, redimensionar e organizar

- **Mover:** arraste o painel pelo título. Ao soltar, ele se alinha às bordas da tela e aos painéis vizinhos. Durante o arraste, o painel fica dentro da área útil da tela, com uma pequena margem.
- **Redimensionar:** arraste qualquer borda ou canto. A largura e a altura avançam em colunas e linhas inteiras de ícones, então não sobra faixa vazia. A altura acompanha o conteúdo até o tamanho que você definiu; a partir daí, o painel ganha rolagem.
- **Sem sobreposição:** um painel nunca fica por cima de outro. Se você soltar ou aumentar um painel sobre outro, ele vai para o espaço livre mais próximo.
- **Renomear:** clique duas vezes no título, digite o novo nome e aperte Enter. Esc cancela.
- **Recolher:** a seta no lado direito da barra de título deixa só a barra de título à vista. Clique de novo para expandir.
- **Travar:** em **Geral**, **Travar posição e tamanho** evita mover ou redimensionar sem querer. O mesmo ajuste está no menu da bandeja como **Travar painéis**.
- **Reorganizar:** em **Geral**, **Reorganizar painéis** alinha todos os painéis visíveis no canto superior direito da tela principal.

## Itens nos painéis

| Para | Faça |
| --- | --- |
| Abrir | Clique duplo ou Enter |
| Selecionar vários | Ctrl+clique, Shift+clique, Ctrl+A, ou arraste um retângulo sobre uma área vazia do painel |
| Mudar o nome mostrado (coleções) | F2, ou **Renomear no painel** no menu do item |
| Tirar do painel (coleções) | Del, ou **Tirar do painel** no menu do item |
| Levar para outra coleção | Arraste até ela, ou use **Mover para** no menu do item |
| Pôr um arquivo de pasta em uma coleção | **Adicionar a** no menu do item, ou arraste até a coleção |
| Ver o arquivo no Explorador | **Mostrar no Explorador** no menu do item |

**Renomear no painel** muda só o nome exibido. O arquivo continua com o nome original.

Com vários itens selecionados, você pode arrastar, abrir ou tirar todos de uma vez.

## Arrastar e soltar

| De | Para | Resultado |
| --- | --- | --- |
| Explorador ou desktop | Coleção | Cria atalhos. Os arquivos ficam onde estão. |
| Coleção | Outra coleção | Leva o atalho para a outra coleção |
| Coleção | A mesma coleção | Muda a ordem dos itens |
| Coleção | Explorador ou desktop | Copia o arquivo, ou cria um atalho se você segurar Alt. O original nunca sai do lugar. |
| Coleção | Painel de pasta | Copia o arquivo para a pasta. O original nunca sai do lugar. |
| Explorador ou desktop | Painel de pasta | Move o arquivo para a pasta, se estiver no mesmo disco, ou copia, se estiver em outro disco |
| Painel de pasta | Coleção | Cria um atalho para o arquivo |
| Painel de pasta | Explorador ou desktop | Segue as regras normais do Windows, porque o arquivo é real |

Ao soltar em um painel de pasta, segure **Ctrl** para copiar ou **Shift** para mover, como no Explorador. A operação usa a janela do próprio Windows, com progresso e aviso de conflito de nomes, e pode ser desfeita com Ctrl+Z no Explorador.

## Mostrar os painéis na frente

Normalmente, as janelas abertas ficam por cima dos painéis. Para soltar um arquivo do Explorador em um painel sem minimizar nada:

1. Aperte **Ctrl+Alt+Espaço**. Os painéis aparecem na frente de todas as janelas.
2. Arraste o arquivo do Explorador até o painel.
3. Aperte **Ctrl+Alt+Espaço** de novo, ou Esc, para devolver os painéis ao desktop.

Abrir um item ou usar **Mostrar no Explorador** também devolve os painéis ao desktop.

O mesmo recurso está no menu da bandeja, como **Mostrar painéis na frente**, e na janela do Orla, no botão **Mostrar na frente**. Enquanto os painéis estão na frente, esse botão vira **Voltar ao desktop**.

Se outro programa já usa **Ctrl+Alt+Espaço**, o Orla avisa e o recurso continua disponível pela bandeja. Veja [O atalho não funciona](#o-atalho-não-funciona).

## Desktop limpo

**Desktop limpo** esconde os ícones do Windows e deixa só os painéis. Ele vem desligado, a não ser que você escolha **Organizar meu desktop** na primeira vez. Para ligar ou desligar, use **Geral > Desktop limpo** ou o item **Desktop limpo** no menu da bandeja.

Com ele ligado:

- Os ícones do Windows e a seleção com o mouse no desktop ficam indisponíveis.
- Os arquivos continuam na pasta da área de trabalho. Para vê-los, use o painel **Na área de trabalho**, criado por **Organizar meu desktop**, ou crie um **Painel de pasta** da área de trabalho.

### Como o Orla protege seus ícones

Antes de esconder os ícones, o Orla inicia um pequeno processo de proteção, uma segunda cópia do próprio Orla sem janela. Ele espera o Orla terminar, de qualquer forma que isso aconteça, e então devolve os ícones.

Os ícones voltam quando você:

- desliga **Desktop limpo**;
- escolhe **Sair e restaurar o desktop** ou **Sair do Orla**;
- desinstala o Orla;
- ou quando o Orla trava ou é encerrado pelo Gerenciador de Tarefas.

O estado restaurado é a sua configuração do Explorador, em **Exibir > Mostrar ícones da área de trabalho** no menu do botão direito do desktop. Se você mesmo deixou os ícones desligados ali, eles continuam desligados.

Se o Orla não conseguir iniciar a proteção, ele não esconde os ícones e avisa: "Não foi possível iniciar a proteção que devolve os ícones. Os ícones do Windows continuam visíveis."

## Janela do Orla

Abra com um clique no ícone da bandeja, pelo menu **···** de um painel (**Abrir o Orla**) ou executando o Orla de novo.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/central-dark.png">
  <img src="../images/central-light.png" width="720" alt="Página Painéis da janela do Orla, com quatro painéis de demonstração">
</picture>

A janela tem quatro páginas:

- **Painéis:** cria, mostra, oculta e remove painéis. Veja [Criar, ocultar e remover painéis](#criar-ocultar-e-remover-painéis).
- **Aparência:** tema, opacidade, ícones e animações, com uma pré-visualização ao vivo.
- **Geral:** inicialização, **Desktop limpo**, atalho, trava, idioma e **Reorganizar painéis**.
- **Sobre:** versão, pasta dos seus dados, **Guia de uso**, **Relatar um problema** e **Sair do Orla**.

No rodapé da barra lateral, **Integrado ao desktop do Windows** indica que os painéis estão na camada do desktop. Se aparecer **Modo compatível: não encontrei o desktop do Windows.**, veja [Solução de problemas](#o-orla-mostra-modo-compatível).

### Aparência

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="../images/appearance-dark.png">
  <img src="../images/appearance-light.png" width="720" alt="Página Aparência com a pré-visualização de um painel sobre um papel de parede">
</picture>

| Ajuste | Opções |
| --- | --- |
| **Tema** | **Sistema** acompanha o tema dos apps do Windows na hora em que você o muda. **Claro** e **Escuro** fixam um tema. |
| **Opacidade dos painéis** | De 75% a 95%. O mínimo mantém o texto legível sobre qualquer papel de parede. |
| **Tamanho dos ícones** | **Pequenos**, **Médios** ou **Grandes** |
| **Animações** | Transições curtas quando os painéis aparecem. Se as animações estiverem desligadas no Windows, o Orla também não anima. |

Com o Alto Contraste do Windows ligado, o Orla usa as cores do sistema e desliga a transparência.

### Geral

| Ajuste | O que faz |
| --- | --- |
| **Iniciar com o Windows** | Abre os painéis quando você entra no Windows. Vem ligado. |
| **Desktop limpo** | Esconde os ícones do Windows. Veja [Desktop limpo](#desktop-limpo). |
| **Atalho Ctrl+Alt+Espaço** | Liga ou desliga o atalho que traz os painéis para a frente |
| **Travar posição e tamanho** | Impede mover ou redimensionar painéis |
| **Idioma** | **Sistema** segue o idioma de exibição do Windows. Você também pode escolher Português (Brasil), English, Español, Français, Deutsch ou Italiano. A troca vale na hora. |
| **Reorganizar painéis** | O botão **Reorganizar** alinha os painéis visíveis no canto superior direito da tela principal |
| **Atualizações automáticas** | Só na versão instalada. A chave começa ligada. Veja [Atualizações](#atualizações). |

### Atualizações

A versão instalada procura uma versão nova no GitHub no máximo uma vez por dia. Quando encontra, baixa em segundo plano e aplica na próxima vez que o Orla fechar. A busca pede ao GitHub a lista de versões deste repositório e não envia dados pessoais. Quando uma versão nova estiver pronta, a página **Sobre** mostra o botão **Reiniciar e atualizar**, para instalar na hora. Para desligar as atualizações, use **Atualizações automáticas** em **Geral**.

A versão portátil não se atualiza sozinha. Para atualizar, saia do Orla, baixe o ZIP novo da [página de versões](https://github.com/ThePlayNEW/orla/releases/latest) e extraia no lugar da pasta antiga. Seus painéis ficam em `%LOCALAPPDATA%\Orla` e não são afetados.

## Teclado

| Tecla | Onde | Ação |
| --- | --- | --- |
| Ctrl+Alt+Espaço | Qualquer lugar | Traz os painéis para a frente, ou devolve ao desktop |
| Esc | Painel na frente | Devolve os painéis ao desktop |
| Enter | Item | Abre |
| Setas | Item | Move a seleção |
| Ctrl+A | Painel | Seleciona todos os itens |
| F2 | Item de coleção | **Renomear no painel** |
| Del | Item de coleção | **Tirar do painel** |
| Tecla de menu | Item | Abre o menu do item |
| Enter ou Esc | Título em edição | Confirma ou cancela o novo nome |

## Vários monitores

Você pode pôr painéis em qualquer monitor. Cada painel usa a escala do monitor em que está e fica dentro da área útil dele. Se um monitor for desconectado, os painéis que estavam nele passam para o monitor mais próximo. **Reorganizar painéis** leva todos os painéis visíveis para a tela principal.

Monitores com escalas diferentes, como um notebook em 150% e um monitor externo em 100%, ainda não foram testados. Se algo parecer fora do lugar nessa situação, [relate o problema](https://github.com/ThePlayNEW/orla/issues/new/choose).

## Atualizar da versão 1

Na primeira vez que a versão 2 abre, ela converte o arquivo da versão 1:

- Cada grupo vira uma coleção, com os mesmos itens, um tamanho parecido e uma cor equivalente.
- Os painéis são reorganizados a partir do canto superior direito da tela principal, porque na versão 1 eles podiam ficar sobre a coluna de ícones do Windows, que agora volta a aparecer.
- Os ícones do Windows voltam a aparecer, porque o **Desktop limpo** fica desligado. Se você preferia o desktop sem ícones da versão 1, ligue **Desktop limpo** em **Geral**.
- O atalho da versão 1 na pasta Inicializar do Windows é removido. A versão 2 inicia com o Windows por uma entrada de inicialização do seu usuário.
- O Orla mostra o aviso "Seus grupos da versão anterior agora são coleções, e os ícones do Windows voltaram a aparecer. Para escondê-los de novo, ligue Desktop limpo."

O arquivo original da versão 1 fica guardado, sem alterações, como `layout.json.v1`, ao lado do `layout.json`. A versão 1 não lê o arquivo da versão 2, então, para voltar a ela, saia do Orla, apague `layout.json` e renomeie `layout.json.v1` para `layout.json`. As duas versões não rodam ao mesmo tempo.

## Onde ficam seus dados

Tudo fica em `%LOCALAPPDATA%\Orla`, tanto na versão instalada quanto na portátil. Em **Sobre**, o botão **Abrir pasta** abre essa pasta.

| Arquivo | Conteúdo |
| --- | --- |
| `layout.json` | Painéis, itens, posições e ajustes |
| `layout.json.bak` | A versão anterior, criada a cada salvamento |
| `layout.json.v1` | O arquivo da versão 1, guardado sem alterações na atualização, se você usava a versão 1 |
| `layout.json.corrupt-<data>` | Uma cópia de um arquivo que não pôde ser lido, guardada para você não perder nada |

O Orla salva em um arquivo temporário e só depois troca pelo definitivo, então uma queda de energia no meio do salvamento não corrompe o layout. Se o `layout.json` não puder ser lido, o Orla usa o `.bak` e avisa: "Seus painéis foram recuperados da última cópia salva."

O layout guarda só caminhos e nomes. Para fazer backup, copie `layout.json`.

O Orla não tem telemetria. O único acesso à rede é a busca por atualizações da versão instalada, descrita em [Atualizações](#atualizações).

## Solução de problemas

### Os ícones do Windows sumiram

1. Abra o Orla e veja se **Desktop limpo**, em **Geral**, está ligado. Se estiver, desligue.
2. Se o Orla não estiver aberto, clique com o botão direito em uma área vazia do desktop e escolha **Exibir > Mostrar ícones da área de trabalho**.
3. Se ainda assim os ícones não voltarem, reinicie o Explorador: abra o Gerenciador de Tarefas (Ctrl+Shift+Esc), procure **Windows Explorer** na aba **Processos**, clique com o botão direito e escolha **Reiniciar**.

Seus arquivos nunca saem da pasta da área de trabalho, mesmo com os ícones ocultos.

### O atalho não funciona

Se outro programa já usa **Ctrl+Alt+Espaço**, o Orla mostra o aviso "Outro programa já usa Ctrl+Alt+Espaço" e a chave **Atalho Ctrl+Alt+Espaço** fica desligada. Use **Mostrar painéis na frente** no menu da bandeja, ou libere o atalho no outro programa e ligue a chave de novo em **Geral**.

### O Windows mostrou um aviso do SmartScreen

Os executáveis do Orla ainda não têm assinatura digital, e o SmartScreen avisa sobre programas sem reputação. Confira se você baixou o arquivo da [página de versões](https://github.com/ThePlayNEW/orla/releases) deste repositório, clique em **Mais informações** e depois em **Executar assim mesmo**. A assinatura pelo SignPath Foundation está planejada.

### O Explorador reiniciou

Quando o Explorador reinicia, a camada do desktop é recriada. O Orla percebe isso e recoloca os painéis em cerca de um segundo, com o **Desktop limpo** no estado em que estava. Se os painéis não voltarem, saia pelo menu da bandeja e abra o Orla de novo.

### Um painel está fora da tela

Em **Geral**, clique em **Reorganizar** ao lado de **Reorganizar painéis**. Todos os painéis visíveis vão para o canto superior direito da tela principal.

### Um painel sumiu

Ele pode ter sido ocultado. Abra o Orla, vá em **Painéis** e ligue a chave **No desktop** do painel.

### O Orla mostra "Modo compatível"

O Orla não encontrou a janela do desktop do Explorador e mostra os painéis em janelas comuns logo acima do desktop. Isso pode acontecer se o Explorador não estiver rodando ou se outro programa substituir o desktop. Reinicie o Explorador como descrito acima. Se o aviso continuar, [relate o problema](https://github.com/ThePlayNEW/orla/issues/new/choose) dizendo qual versão do Windows e qual programa de papel de parede você usa.

### Um item aparece como ausente

O arquivo foi apagado ou está em um disco desconectado. Reconecte o disco ou use **Tirar do painel** no menu do item.

## Perguntas frequentes

**O Orla move ou apaga meus arquivos?**
Não. Coleções só guardam atalhos. O único caso em que um arquivo muda de lugar é quando você o solta em um painel de pasta, e isso passa pela janela de cópia do próprio Windows, que permite desfazer.

**Posso usar o Orla com outro organizador de desktop?**
Use só um programa que esconde ou gerencia os ícones do desktop por vez. Dois programas fazendo isso ao mesmo tempo podem deixar os ícones em um estado inesperado.

**O Orla funciona com Wallpaper Engine?**
Sim. Os testes foram feitos com o Wallpaper Engine rodando no Windows 10. O Orla não mexe no papel de parede nem nas janelas do Wallpaper Engine. O Lively usa a mesma estrutura, mas ainda não foi testado.

**O Orla funciona no Windows 11?**
O código foi escrito para as estruturas de desktop do Windows 10 e do Windows 11, inclusive a do 24H2. O teste manual no Windows 11 ainda está pendente. Se você usar, conte como foi em [Issues](https://github.com/ThePlayNEW/orla/issues/new/choose).

**Quanta memória o Orla usa?**
Cerca de 100 MB com quatro painéis, incluindo o runtime do WPF. Parado, o uso de processador fica perto de zero, porque o Orla só trabalha quando algo muda.

**Posso levar meus painéis para outro computador?**
Pode copiar `layout.json` para `%LOCALAPPDATA%\Orla` no outro computador com o Orla fechado. Itens cujo caminho não existir lá aparecem como ausentes.

**Quantos painéis posso ter?**
Até 40 painéis, com até 3.000 itens em cada coleção.

## Desinstalar

Fechar, sair ou desinstalar o Orla nunca deixa o desktop alterado: os ícones do Windows voltam a seguir a configuração do Explorador.

- **Versão instalada:** abra **Configurações > Aplicativos**, procure **Orla Desktop** e clique em **Desinstalar**. A inicialização com o Windows é removida junto, e os ícones do Windows voltam.
- **Versão portátil:** em **Geral**, desligue **Iniciar com o Windows**. Depois escolha **Sair e restaurar o desktop** no menu da bandeja e apague a pasta onde você extraiu o ZIP.

Seus painéis continuam em `%LOCALAPPDATA%\Orla`, caso você volte a usar o Orla. Para apagá-los, digite `%LOCALAPPDATA%` na barra de endereço do Explorador, aperte Enter e exclua a pasta `Orla`.
