[English](../en/manual-test.md) · **Português**

# Roteiro de teste manual

Este roteiro confere se o Orla convive bem com o desktop do Windows no seu computador. Leva uns 20 minutos. Nenhum passo apaga arquivos, mas use arquivos de teste, como um `.txt` vazio, em vez de documentos importantes.

Os testes mais úteis agora são no **Windows 11 24H2 ou 25H2**, com **Lively Wallpaper** e com **monitores de escalas diferentes**, porque esses cenários ainda não foram testados.

## Antes de começar

Anote estas informações. Elas vão no relato do teste.

| Item | Onde encontrar |
| --- | --- |
| Versão do Windows | Aperte Win+R, digite `winver` e aperte Enter |
| Versão do Orla | Janela do Orla, página **Sobre** |
| Programa de papel de parede | Wallpaper Engine, Lively, outro ou nenhum |
| Monitores e escala | **Configurações > Sistema > Tela**, escala de cada monitor |

Instale o Orla e, na tela de boas-vindas, escolha **Manter meus ícones**. Em **Escolha seus primeiros painéis**, deixe **Acesso rápido** e **Downloads** marcados, marque também **Trabalho** e clique em **Começar**. Crie um arquivo de teste no desktop, por exemplo `teste-orla.txt`.

## Passos

- [ ] **1. Win+D e mostrar a área de trabalho.** Abra duas ou três janelas quaisquer e aperte Win+D. As janelas somem e os painéis continuam no desktop. Aperte Win+D de novo e as janelas voltam, por cima dos painéis. Repita clicando no canto direito da barra de tarefas, depois do relógio.
- [ ] **2. Seleção no desktop.** Em uma área vazia do desktop, fora dos painéis, arraste o mouse para desenhar um retângulo sobre alguns ícones. Os ícones ficam selecionados, como sem o Orla.
- [ ] **3. Menu e arrastar no desktop.** Clique com o botão direito em uma área vazia do desktop: o menu normal do Windows aparece, com **Exibir**, **Classificar por** e **Novo**. Depois arraste `teste-orla.txt` de uma pasta do Explorador para uma área vazia do desktop: o arquivo aparece no desktop.
- [ ] **4. Arrastar para uma coleção.** Arraste o arquivo de teste do Explorador para o painel **Trabalho**. A imagem de arraste do Windows aparece sobre o painel. Um atalho aparece no painel e o arquivo continua no lugar de origem. Renomeie o arquivo no Explorador: o item do painel acompanha o novo nome.
- [ ] **5. Arrastar para um painel de pasta.** Arraste o arquivo de teste para o painel **Downloads**. A janela de movimentação do Windows aparece, se for preciso, e o arquivo vai para a pasta Downloads. Aperte Ctrl+Z em uma janela do Explorador para desfazer.
- [ ] **6. Esconder e mostrar.** Clique em uma área vazia do desktop e aperte **Ctrl+Alt+Espaço**. Os painéis somem. Aperte de novo: eles voltam. Confira que o menu da bandeja mostra **Esconder painéis** ou **Mostrar painéis**, conforme o caso.
- [ ] **7. Painéis na frente.** Maximize uma janela do Explorador e aperte **Ctrl+Alt+Espaço**. Os painéis aparecem na frente da janela. Arraste um arquivo do Explorador para um painel. Clique no título de um painel e aperte Esc: os painéis voltam para trás das janelas.
- [ ] **8. Trocar a combinação.** Em **Geral > Combinação de teclas**, clique no botão e pressione Ctrl+Alt+O. Repita os passos 6 e 7 com a nova combinação. Clique em **Restaurar padrão** e confira que **Ctrl+Alt+Espaço** volta a funcionar.
- [ ] **9. Selecionar e arrastar itens.** Clique duas vezes no título de um painel, digite um nome novo e aperte Enter. Em **Acesso rápido**, selecione dois itens com Ctrl+clique e arraste-os para **Trabalho**. Uma prévia com o número 2 acompanha o ponteiro, uma linha mostra onde os itens vão entrar, e eles continuam selecionados no destino.
- [ ] **10. Mover e redimensionar.** Arraste um painel pelo título perto da borda da tela e de outro painel: ele gruda na margem e na borda do vizinho, sem sobreposição. Arraste uma borda e um canto: uma linha de destaque marca a borda, o tamanho aparece em colunas × linhas e, ao soltar, o painel se ajusta a colunas e linhas inteiras. Clique duas vezes na borda de baixo: **Altura automática** volta a ficar marcada no menu **···**.
- [ ] **11. Desktop limpo e proteção.** Em **Geral**, ligue **Desktop limpo**. Os ícones do Windows somem. Com o desktop à vista, aperte **Ctrl+Alt+Espaço**: os painéis somem e os ícones do Windows voltam. Aperte de novo. Depois abra o Gerenciador de Tarefas (Ctrl+Shift+Esc), vá na aba **Detalhes**, encontre os dois processos `Orla.exe` e finalize o que usa mais memória. Em poucos segundos, os ícones do Windows voltam. Abra o Orla de novo e desligue **Desktop limpo**.
- [ ] **12. Reinício do Explorador.** No Gerenciador de Tarefas, aba **Processos**, clique com o botão direito em **Windows Explorer** e escolha **Reiniciar**. A barra de tarefas pisca, e em cerca de um segundo os painéis voltam ao desktop no mesmo lugar.
- [ ] **13. Tema e opacidade.** Em **Configurações > Personalização > Cores**, troque o modo dos aplicativos entre Claro e Escuro. Os painéis e a janela do Orla mudam de tema na hora. Em **Aparência**, mova **Opacidade dos painéis**: os painéis do desktop mudam enquanto você arrasta.
- [ ] **14. Deixe o Orla organizar.** Em **Painéis**, clique em **Organizar para mim**. A prévia mostra os painéis sobre um mapa da tela, com as ferramentas à esquerda e o trabalho à direita. Desligue **Esconder os ícones do Windows** e confira que **Novos no desktop** some do mapa; ligue de novo. Clique em **Organizar**. Com **Manter organizado** ligado, salve um arquivo `.pdf` ou crie um atalho no desktop: em poucos segundos ele aparece no painel certo. Apague-o: ele sai do painel. Por fim, clique em **Voltar aos painéis anteriores**.
- [ ] **15. Novo painel e sair.** Em **Painéis**, clique em **Novo painel** e crie **Imagens**: ele aparece no desktop sem cobrir outro painel. Por fim, clique com o botão direito no ícone do Orla na bandeja e escolha **Sair e restaurar o desktop**. Os painéis somem e os ícones do Windows estão como antes do teste.

## Como relatar

Abra um relato em [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose) com as informações de "Antes de começar" e o número dos passos que falharam. Se todos passaram, um relato dizendo isso também ajuda, principalmente no Windows 11.

Antes de anexar capturas de tela, confira se elas não mostram nomes de usuário, caminhos pessoais ou arquivos privados.
