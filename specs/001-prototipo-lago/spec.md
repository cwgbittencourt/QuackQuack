# Feature Specification: Prototipo Jogavel do Lago

**Feature Branch**: `001-prototipo-lago`

**Created**: 2026-10-24

**Status**: Draft

**Input**: User description: "Criar o primeiro prototipo jogavel de Quack-quack Survive a partir do documento funcional, concentrado no Lago e planejado com Spec Kit."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sobreviver usando movimento e Quack (Priority: P1)

Como jogador, quero mover o Pato entre agua e terra e usar o Quack com uma reserva de Folego para
controlar hordas, de modo que posicionamento e momento de ativacao determinem minha sobrevivencia.

**Why this priority**: E o nucleo diferenciador do jogo e permite validar diversao antes de produzir
conteudo adicional.

**Independent Test**: Uma arena curta com inimigos basicos permite validar movimento, diferenca de
terreno, consumo/recuperacao de Folego, empurrao, interrupcao e reflexao sem depender do sistema de
build ou de chefes.

**Acceptance Scenarios**:

1. **Given** o Pato em terra com Folego disponivel, **When** o jogador ativa o Quack, **Then** inimigos no alcance sao empurrados, ataques interrompiveis param, projeteis leves sao refletidos e a reserva diminui.
2. **Given** o Pato na agua, **When** ele se movimenta e aguarda sem usar o Quack, **Then** sua mobilidade e recuperacao de Folego sao maiores que em terra e a mudanca e perceptivel.
3. **Given** a barra de Folego cheia, **When** o jogador ativa o Quack, **Then** o efeito aprimorado e comunicado e aplicado uma unica vez.

---

### User Story 2 - Formar uma build durante a partida (Priority: P2)

Como jogador, quero coletar experiencia, escolher entre tres cartas e combinar armas com passivas para
criar evolucoes, de modo que partidas sucessivas oferecam estrategias diferentes.

**Why this priority**: Variedade de build sustenta a repeticao e transforma o controle de hordas em
decisoes de progressao.

**Independent Test**: Uma partida acelerada pode conceder niveis e validar seis armas, seis passivas,
as tres evolucoes do prototipo e os recursos de rerrolar, banir e pular sem exigir chefes.

**Acceptance Scenarios**:

1. **Given** experiencia suficiente para subir de nivel, **When** a selecao abre, **Then** tres cartas elegiveis exibem tipo, efeito, sinergias e requisito de evolucao.
2. **Given** uma arma no nivel maximo e sua passiva compativel, **When** uma recompensa de evolucao e obtida, **Then** a arma e substituida por sua forma evoluida e o evento recebe feedback exclusivo.
3. **Given** uma partida nova, **When** o jogador usa rerrolagens, banimento ou pular, **Then** os limites de duas rerrolagens, um banimento e pulos permitidos sao aplicados e mostrados.

---

### User Story 3 - Concluir a jornada de 15 minutos (Priority: P3)

Como jogador, quero enfrentar ondas crescentes, dois minibosses e o Sapo-Rei em uma partida de 15
minutos, recebendo um resultado claro ao vencer ou morrer.

**Why this priority**: Fecha o ciclo do prototipo e prova ritmo, legibilidade, balanceamento e vontade
de jogar novamente.

**Independent Test**: Uma partida completa no Lago valida cronometro, composicao de cinco inimigos,
dois minibosses, chefe final, condicoes de encerramento e tela de resultados.

**Acceptance Scenarios**:

1. **Given** uma partida iniciada, **When** o tempo avanca, **Then** ondas e encontros aumentam a pressao e os dois minibosses aparecem antes do minuto 15.
2. **Given** o minuto 15 alcancado, **When** o Sapo-Rei entra, **Then** seus saltos, ondas e invocacoes sao telegrafados por cor, som e marcacao no chao.
3. **Given** o jogador derrota o Sapo-Rei ou perde toda a vida, **When** a partida termina, **Then** a tela final apresenta resultado, tempo, inimigos, dano por arma, build, sinergias, moedas, itens, recordes e progresso de desafios.

---

### User Story 4 - Jogar em computador ou celular (Priority: P4)

Como jogador, quero controlar a mesma partida com teclado, controle ou toque e ler a interface em
diferentes formatos de tela, sem que as regras mudem entre dispositivos.

**Why this priority**: O prototipo precisa validar os dois lancamentos iniciais sem bifurcar a
experiencia de jogo.

**Independent Test**: O mesmo cenario pode ser executado com cada metodo de entrada e em quatro
proporcoes de tela, comparando comandos, regras, legibilidade e area segura.

**Acceptance Scenarios**:

1. **Given** qualquer metodo de entrada suportado, **When** o jogador move, usa Quack e pausa, **Then** os mesmos comandos de jogo produzem as mesmas regras e resultados.
2. **Given** uma tela 16:9, 18:9, 19,5:9 ou 4:3, **When** a interface e exibida em paisagem, **Then** informacoes essenciais permanecem visiveis, legiveis e fora de recortes do aparelho.

### Edge Cases

- Se o Folego terminar durante uma ativacao, apenas a ativacao ja aceita conclui; uma nova ativacao e rejeitada com feedback imediato.
- Se nao houver tres cartas elegiveis, a selecao usa todas as opcoes validas restantes sem oferecer duplicatas invalidas ou bloquear a partida.
- Se uma entidade for empurrada contra obstaculo, ela nao atravessa a geometria nem fica presa; efeitos de colisao aplicaveis ocorrem uma vez.
- Se o jogador estiver exatamente na transicao agua/terra, um unico tipo de terreno governa cada instante e a interface comunica a troca sem oscilacao.
- Se centenas de inimigos estiverem ativos, comandos e avisos de perigo continuam responsivos e legiveis.
- Pausar durante selecao de carta, aparicao de chefe ou tela final nao duplica recompensas, encontros ou transicoes.
- Abandono encerra a partida e concede apenas a fracao de moedas definida para o prototipo, sem registrar vitoria.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O jogo MUST oferecer uma partida do bioma Lago com duracao-alvo de 15 minutos e encerramento por vitoria, morte ou abandono.
- **FR-002**: O jogador MUST controlar o Pato por movimento, Quack e pausa com teclado, controle e toque.
- **FR-003**: Agua e terra MUST alterar mobilidade, recuperacao de Folego e eficiencia dos equipamentos aplicaveis, com transicoes legiveis.
- **FR-004**: O Quack MUST consumir Folego, causar pouco dano inicial, empurrar inimigos, interromper ataques permitidos e refletir projeteis leves.
- **FR-005**: O Folego MUST recuperar automaticamente, mais rapido na agua, e um Quack com barra cheia MUST produzir efeito aprimorado.
- **FR-006**: O prototipo MUST disponibilizar seis armas, seis passivas e exatamente tres combinacoes evoluidas validaveis.
- **FR-007**: Cada nivel MUST oferecer tres cartas elegiveis e comunicar melhoria numerica, nova funcao, sinergia e requisito de evolucao.
- **FR-008**: Cada partida MUST permitir duas rerrolagens, um banimento e a opcao de pular escolhas, com disponibilidade visivel.
- **FR-009**: Uma arma MUST possuir cinco niveis e somente evoluir quando estiver no nivel maximo e sua passiva compativel estiver presente.
- **FR-010**: O Lago MUST oferecer rotas por agua e terra, obstaculos, margens ou ilhas e locais de recompensa associados a risco.
- **FR-011**: O prototipo MUST incluir cinco tipos de inimigos com funcoes taticas distintas e composicoes que combinem pressao, restricao de movimento e reposicionamento.
- **FR-012**: Dois minibosses MUST ocorrer antes do chefe final e testar a build e o posicionamento do jogador.
- **FR-013**: O Sapo-Rei MUST saltar sobre areas marcadas, criar ondas, invocar girinos e destruir areas seguras em uma fase posterior.
- **FR-014**: Ataques perigosos MUST ser anunciados por cor, som e marcacao no chao antes de causarem dano.
- **FR-015**: Acertos, criticos, Quack pronto, evolucoes e recompensas raras MUST possuir feedback distinguivel durante o combate.
- **FR-016**: A tela final MUST mostrar resultado, tempo, chefe alcancado, inimigos derrotados, dano por arma, maior sequencia, build, sinergias, moedas, itens, recordes e progresso de desafios.
- **FR-017**: O prototipo MUST oferecer dez reliquias desbloqueaveis e permitir equipar no maximo tres por partida.
- **FR-018**: A partida MUST manter as mesmas regras, conteudo e resultados independentemente do metodo de entrada ou dispositivo.
- **FR-019**: A interface MUST funcionar em paisagem e manter informacoes essenciais legiveis e dentro da area segura em 16:9, 18:9, 19,5:9 e 4:3.
- **FR-020**: Loja, anuncios, compras, outros biomas e outros personagens MUST NOT estar disponiveis neste prototipo.

### Key Entities *(include if feature involves data)*

- **Partida**: Sessao de sobrevivencia com tempo, estado, dificuldade, encontros, build e resultado.
- **Personagem**: Pato controlado pelo jogador, com atributos, vida, mobilidade e comportamento do Quack.
- **Folego**: Reserva consumida pelo Quack, com limite, recuperacao e estado de carga completa.
- **Terreno**: Regiao de agua ou terra que modifica movimento, recuperacao e efeitos associados.
- **Arma**: Fonte automatica ou ativa de combate com nivel, efeitos e combinacao de evolucao.
- **Passiva**: Modificador de build que altera atributos e habilita evolucoes compativeis.
- **Evolucao**: Forma avancada de uma arma obtida por nivel maximo e passiva correspondente.
- **Inimigo**: Ameaca com funcao tatica, atributos, comportamento, recompensas e sinais de ataque.
- **Encontro**: Onda, miniboss ou chefe programado no ritmo da partida.
- **Reliquia**: Modificador metaprogressivo desbloqueavel, limitado a tres escolhas equipadas.
- **Resultado**: Resumo persistivel da partida e do progresso de recordes e desafios.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Pelo menos 80% dos participantes de teste entendem movimento, diferenca de terreno e uso do Quack sem explicacao verbal apos dois minutos.
- **SC-002**: Pelo menos 70% dos participantes relatam ter tomado uma decisao consciente entre usar e guardar Folego em uma partida.
- **SC-003**: Em dez partidas de teste por participante, pelo menos tres configuracoes de build distintas chegam ao segundo miniboss sem uma unica combinacao dominar mais de 60% das escolhas.
- **SC-004**: Pelo menos 80% das mortes observadas podem ser explicadas pelo jogador como consequencia de um perigo previamente sinalizado.
- **SC-005**: Pelo menos 60% dos participantes iniciam espontaneamente uma segunda partida na mesma sessao de teste.
- **SC-006**: Durante o pico de 400 inimigos, a resposta percebida dos controles permanece continua e a apresentacao sustenta 60 quadros por segundo no computador e celular intermediario ou 30 em celular de entrada.
- **SC-007**: Cem por cento dos cenarios criticos podem ser concluidos com teclado, controle e toque sem alterar regras ou conteudo.
- **SC-008**: Cem por cento das telas essenciais passam revisao de legibilidade e area segura nas quatro proporcoes definidas.

## Assumptions

- O prototipo usa o Pato como unico personagem jogavel e o Lago como unico bioma.
- A selecao exata dos cinco inimigos e dos dois minibosses sera definida no planejamento, preservando funcoes taticas distintas.
- As tres evolucoes iniciais sao Bumerangue + Pena aerodinamica, Ovos + Ninho reforcado e Quack frontal + Pulmao poderoso.
- O modo infinito, narrativa completa, classificacao alem da tela final e monetizacao ficam para features posteriores.
- Arte, audio e animacao podem usar conteudo temporario, desde que comuniquem claramente estados e perigos.
- Testes de retencao e compreensao serao conduzidos com um pequeno grupo de jogadores representativos do publico-alvo.
