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
- [ ] **3. Menu do desktop.** Clique com o botão direito em uma área vazia do desktop. O menu normal do Windows aparece, com **Exibir**, **Classificar por** e **Novo**.
- [ ] **4. Arrastar para o desktop.** Arraste `teste-orla.txt` de uma pasta do Explorador para uma área vazia do desktop. O arquivo aparece no desktop.
- [ ] **5. Arrastar para uma coleção.** Arraste o arquivo de teste para o painel **Trabalho**. Um atalho aparece no painel e o arquivo continua no lugar de origem. Renomeie o arquivo no Explorador: o item do painel acompanha o novo nome.
- [ ] **6. Arrastar para um painel de pasta.** Arraste o arquivo de teste para o painel **Downloads**. A janela de movimentação do Windows aparece, se for preciso, e o arquivo vai para a pasta Downloads. Aperte Ctrl+Z em uma janela do Explorador para desfazer.
- [ ] **7. Painéis na frente.** Maximize uma janela do Explorador e aperte **Ctrl+Alt+Espaço**. Os painéis aparecem na frente da janela. Arraste um arquivo do Explorador para um painel. Aperte Esc: os painéis voltam para trás das janelas.
- [ ] **8. Renomear e selecionar.** Clique duas vezes no título de um painel, digite um nome novo e aperte Enter. Depois selecione vários itens com Ctrl+clique e com um retângulo arrastado sobre uma área vazia do painel.
- [ ] **9. Mover e redimensionar.** Arraste um painel pelo título até perto da borda da tela e de outro painel: ele se encaixa ao soltar. Arraste um painel por cima de outro: ele vai para um espaço livre, sem sobreposição. Arraste uma borda e um canto: o tamanho muda em colunas e linhas inteiras de ícones.
- [ ] **10. Desktop limpo e proteção.** Na janela do Orla, em **Geral**, ligue **Desktop limpo**. Os ícones do Windows somem. Abra o Gerenciador de Tarefas (Ctrl+Shift+Esc), vá na aba **Detalhes**, encontre os dois processos `Orla.exe` e finalize o que usa mais memória. Em poucos segundos, os ícones do Windows voltam. Abra o Orla de novo e desligue **Desktop limpo**.
- [ ] **11. Reinício do Explorador.** No Gerenciador de Tarefas, aba **Processos**, clique com o botão direito em **Windows Explorer** e escolha **Reiniciar**. A barra de tarefas pisca, e em cerca de um segundo os painéis voltam ao desktop no mesmo lugar.
- [ ] **12. Tema.** Em **Configurações > Personalização > Cores**, troque o modo dos aplicativos entre Claro e Escuro. Os painéis e a janela do Orla mudam de tema na hora.
- [ ] **13. Novo painel.** Clique no ícone do Orla na bandeja, vá em **Painéis** e clique em **Novo painel**. A lista mostra os painéis prontos disponíveis, **Nova coleção** e **Painel de pasta**. Crie **Imagens** e confira que ele aparece no desktop sem cobrir outro painel.
- [ ] **14. Sair.** Clique com o botão direito no ícone do Orla na bandeja e escolha **Sair e restaurar o desktop**. Os painéis somem e os ícones do Windows estão como antes do teste.

## Como relatar

Abra um relato em [Issues](https://github.com/ThePlayNEW/orla-desktop/issues/new/choose) com as informações de "Antes de começar" e o número dos passos que falharam. Se todos passaram, um relato dizendo isso também ajuda, principalmente no Windows 11.

Antes de anexar capturas de tela, confira se elas não mostram nomes de usuário, caminhos pessoais ou arquivos privados.
