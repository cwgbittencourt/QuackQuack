# Feature Specification: Polimento e confiabilidade do Lago

**Feature Branch**: diretório `002-polimento-lago`, independente da branch Git `main`.
**Created**: 2026-11-03
**Status**: Ready for implementation planning — decisões aprovadas em 03/11/2026; execução depende de solicitação posterior.
**Input**: planejamento com Spec Kit baseado em Melhorias.md externo e imagem anexada.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Permanecer na arena sob pressão (Priority: P1)

Como jogador, quero resistir à aglomeração junto às bordas e voltar a mover-me quando houver espaço.
**Why this priority**: expulsão e aprisionamento impedem continuar a partida.
**Independent Test**: reproduzir pressão em cada borda, canto e encontro com obstáculo e retirar a pressão.
**Acceptance Scenarios**:

1. **Given** Pato junto ao limite, **When** inimigos o pressionam, **Then** seu corpo permanece na área jogável.
2. **Given** Pato cercado, **When** surge espaço e recebe movimento, **Then** retoma deslocamento sem aprisionamento permanente nem remoção da pressão normal.
3. **Given** defeito relatado, **When** selecionar correção, **Then** primeiro registrar causa reproduzível e regressão.

### User Story 2 — Jogar e navegar com controle (Priority: P1)

Como jogador, quero conectar/desconectar controle no smartphone e operar menus sem depender do toque.
**Why this priority**: bloqueio de ações inviabiliza a plataforma.
**Independent Test**: alternar controle/toque durante jogo, pausa, armas e relíquias.
**Acceptance Scenarios**:

1. **Given** controle disponível no smartphone, **When** reconhecido, **Then** joystick/Quack touch desaparecem e retornam na indisponibilidade sem reinício ou comandos presos.
2. **Given** armas ou relíquias abertas, **When** navegar, confirmar e retornar, **Then** todas as opções têm foco visível e ações funcionam sem mouse, respeitando escolhas obrigatórias.
3. **Given** controle compatível, **When** usar botão oposto à pausa, **Then** acessa relíquias e vê indicação correspondente à família do dispositivo.

### User Story 3 — Compreender armas e passivas (Priority: P2)

Como jogador, quero distinguir armas, alcance e modificadores ativos.
**Independent Test**: cada arma/passiva contra alvos dentro/fora do alcance e atrás de obstáculos.
**Acceptance Scenarios**:

1. **Given** armas simultâneas, **When** atacam, **Then** forma, animação ou trajetória as distinguem além da cor.
2. **Given** aura, **When** alvo está dentro do raio mesmo atrás de obstáculo, **Then** recebe dano; fora não recebe e o obstáculo continua bloqueando movimento.
3. **Given** Sabão Mágico e Bolhas, **When** aplicar bônus, **Then** raio de acerto e tamanho visual aumentam juntos em 16% por nível, até 80%, sem acumulação indevida; outras armas não recebem esse bônus nesta entrega.
4. **Given** mesmo bumerangue e alvo, **When** atravessa ida e volta, **Then** permite um acerto em cada trecho e nenhum duplicado na mesma passagem.

### User Story 4 — Sobreviver e coletar (Priority: P2)

Como jogador, quero oportunidades de cura e coleta de XP próximo com fluidez.
**Independent Test**: derrotar comuns/minibosses, coletar em vida cheia/parcial e atrair XP junto a obstáculos.
**Acceptance Scenarios**:

1. **Given** inimigo comum ou miniboss derrotado, **When** processar recompensa, **Then** sempre deixa XP; morte comum também tem chance independente de 1 em 10 de deixar cura de 15% da vida máxima, limitada à falta.
2. **Given** miniboss derrotado, **When** recompensa é emitida, **Then** sempre deixa uma cura de 50% da vida máxima limitada à falta; chefe final não deixa XP nem cura.
3. **Given** XP ou cura dentro do raio de captura, **When** inicia atração, **Then** segue até o Pato independentemente da distância posterior, a 1,25 vez sua velocidade efetiva na água, sem ficar preso em obstáculos e concede uma vez. O raio é um diâmetro de espaço livre além da borda física do Pato (hoje 1,26 unidades centro-centro).
4. **Given** cura e vida cheia, **When** há proximidade, **Then** cura não é consumida nem inicia atração; se vida encher depois de capturada, é descartada sem cura nem retorno ao chão (revisão T029).
5. **Given** saturação de apresentação de pickups, **When** ocorre derrota, **Then** XP e curas devidas são preservados sem reduzir chance, apagar recompensa ou conceder duas vezes.

### User Story 5 — Ler progressão e ameaças (Priority: P2)

Como jogador, quero acompanhar recursos, nível, XP e encontros sem excesso textual.
**Independent Test**: consumir recursos, subir vários níveis, usar Quack aprimorado e enfrentar encontros nas quatro proporções.
**Acceptance Scenarios**:

1. **Given** combate, **When** recursos mudam, **Then** duas barras identificáveis mostram vida/Fôlego sem números, textos de terreno e instruções associados.
2. **Given** Quack pronto/aprimorado, **When** estado muda, **Then** há sinal visual inequívoco que não depende só da cor.
3. **Given** coleta atravessando níveis, **When** aplicada, **Then** nível atual e progresso até próximo nível atualizam na região inferior central.
4. **Given** início da partida, **When** transcorrem os primeiros 10 segundos de simulação, **Then** instruções ficam visíveis nesse período e depois desaparecem; pausa suspende contador e troca de entrada atualiza símbolos sem reiniciar ou reapresentar a janela.
5. **Given** minibosses/chefe ativos, **When** recebem dano ou morrem, **Then** cada barra individual acima da entidade acompanha sua vida e é removida ao derrotá-la.

### User Story 6 — Reconhecer o mundo (Priority: P3)

Como jogador, quero reconhecer personagens, armas, recompensas e bloqueios na direção cartoon.
**Independent Test**: revisar cena representativa e jogar sem explicação verbal sob carga.
**Acceptance Scenarios**:

1. **Given** cena ilustrada, **When** observada, **Then** distingue elenco aprovado, recompensas, perigos, decoração e obstáculos.
2. **Given** água animada, **When** há combate na margem, **Then** água/terra/limites continuam legíveis sem encobrir perigos.

### Edge Cases

- Pressão em dois eixos, passo físico longo, canto com obstáculo e transição à arena do chefe.
- Desconexão com botão/analógico pressionado, dois controles, perda de foco e modal aberto durante pausa.
- Vida cheia, falta menor que cura, vida máxima modificada, morte/coleta simultâneas, pool esgotado e notificação duplicada de derrota.
- XP atraído durante pausa, jogador se afastando, múltiplas subidas, coleta concorrente e reutilização pooled.
- Bumerangue muda de fase dentro do alvo; alvo derrotado/reutilizado e passo que cruza retorno.
- Múltiplos especiais, morte fora da tela, 4:3, Safe Area de ambos os lados, baixa qualidade e opção 30 FPS.

## Requirements *(mandatory)*

### Functional Requirements

| ID | Origem | Obrigação |
|---|---|---|
| FR-001 | MEL-001 | Substituir representações provisórias por assets reconhecíveis no elenco e direção visual aprovados. |
| FR-002 | MEL-002 | Diferenciar decoração/bloqueios e animar água sem ocultar terreno/perigos/limites. |
| FR-003 | MEL-003 | Exibir XP até próximo nível e nível atual na região inferior central, inclusive múltiplas subidas. |
| FR-004 | MEL-004 | Usar somente duas barras de recursos sem números/textos de terreno, com identificação visual e sinais de prontidão/aprimoramento do Quack junto às barras superiores; sem tarja inferior de status. XP/nível ocupa o centro inferior liberado (revisão T040). |
| FR-005 | MEL-005 | Mostrar instruções nos primeiros 10 segundos de simulação; pausa suspende contador, troca de entrada atualiza símbolos sem reiniciar, sem repetição automática após a janela. |
| FR-006 | MEL-006 | Distinguir armas/projéteis por forma, animação ou trajetória além da cor. |
| FR-007 | MEL-007 | Animar aura translúcida no alcance real; dano ignora obstáculos nesse alcance e preserva sua colisão de movimento. |
| FR-008 | MEL-008 | Aplicar e comunicar as seis passivas. Sabão somente em Bolhas: +16% no raio por nível, até +80%; raio real de acerto e tamanho visual crescem juntos, com prévia antes/depois; não aumenta dano, velocidade ou alcance de aquisição. |
| FR-009 | MEL-009 | Permitir um acerto por alvo por trecho de ida/volta do mesmo bumerangue. |
| FR-010 | MEL-010 + resposta | Cada derrota comum tem chance independente de 10% de deixar cura de 15% da vida máxima, limitada à falta, sem cooldown ou teto de gameplay que reduza a chance. Não são 100% das mortes nem garantia de um drop a cada dez. Cura pode cair junto do XP. |
| FR-011 | MEL-011 + resposta | Toda derrota de inimigo comum ou miniboss deixa XP sem sorteio, sem supressão por pool cheio. XP e cura capturam a um diâmetro de espaço livre além da borda física do Pato (raio centro-centro de 3 vezes seu raio físico; hoje 1,26 unidades), movem a 1,25 vezes sua velocidade efetiva na água, continuam após afastamento, ignoram obstáculos na atração e concedem uma vez. Cura ainda não capturada é preservada com vida cheia; cura já capturada é descartada se a vida ficar cheia (revisão T029, 05/11/2026); pausa suspende e morte/fim cancelam atração. O chefe final não concede XP nem cura; mantém moedas, vitória e registros uma vez. |
| FR-012 | MEL-012 + resposta | Barra individual acima de cada miniboss/chefe; cada miniboss sempre deixa cura de 50% da vida máxima limitada à falta, sem extensão automática ao chefe. |
| FR-013 | MEL-013 | Alternar touch pela disponibilidade do controle sem reiniciar/perder ações. |
| FR-014 | MEL-014 | Navegar armas/relíquias por controle com foco, confirmação, retorno e fechamento válidos. |
| FR-015 | MEL-015 + resposta | Cobrir Xbox Menu/View (Start/Back), PlayStation Options/Share ou Create conforme layout, genérico Start/Select conforme binding; direcional/analógico navegam, sul (A/Cross) confirma, leste (B/Circle) retorna. Perfil configurável e opção focalizável mantêm acesso se faltar Select. Indicar binding/família real sem adivinhar glifos. |
| FR-016 | MEL-016 | Investigar antes de corrigir expulsão/aprisionamento coletivo preservando pressão normal. |
| FR-017 | Aceite 14 | HUD/menus/controles/efeitos legíveis, sem sobreposição nas quatro proporções e Safe Areas. |
| FR-018 | Aceite 15 | Revalidar regras, integração, entradas físicas, gameplay e desempenho Windows/Android com evidências distintas. |

### Key Entities

- Política de cura: chance, frequência, limite ativo e frações por origem sobre vida máxima.
- Recompensa: origem, identidade de concessão, posição e ciclo de coleta.
- Política de atração: raio, velocidade, persistência, pausa e obstáculos.
- Estado visual: recursos normalizados, nível/XP, prontidão, especiais e modificadores reais.
- Contexto de entrada: dispositivos disponíveis, família, foco e comandos por contexto.
- Manifesto de arte: elenco, referências, prioridade, autoria/licença e aceite por asset.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: zero expulsões/aprisionamentos permanentes na matriz reproduzível de bordas/cantos/obstáculos; movimento retorna quando há espaço.
- **SC-002**: todas as opções de armas/relíquias acessíveis por entradas suportadas; hotplug no celular sem reinício/comandos presos.
- **SC-003**: aura respeita alcance com/sem obstáculo; Sabão corresponde à área real; bumerangue concede exatamente os acertos elegíveis.
- **SC-004**: curas respeitam política aprovada, vida máxima e concessão única por derrota/coleta elegível.
- **SC-005**: XP respeita raio/obstáculos e atualiza nível/progresso corretamente após cada concessão.
- **SC-006**: zero informações essenciais cortadas ou sobreposição na matriz 16:9, 18:9, 19,5:9 e 4:3 com recortes dos dois lados.
- **SC-007**: na nova rodada com pelo menos cinco participantes, todos identificam Pato, cura, XP, obstáculos e ameaça apresentada sem explicação verbal; cada arma/evolução e bônus de Bolhas é mostrado na rodada e cada falha fica registrada. Falha exige correção/reteste, não aprovação pela média; revisão artística isolada não encerra aceite de combate.
- **SC-008**: manter metas constitucionais 60 FPS Windows/intermediário com opção 30, Android de entrada 30 estáveis, sob 200/400 inimigos e custos novos medidos.
- **SC-009**: nova avaliação de gameplay registra resultados dos critérios 1–15 do material; não encerra automaticamente pendências da T075 antiga.

## Assumptions

- Workspace atual C:/Users/lucas/source/repos/QuackQuack é a referência válida; caminhos E: do material são contexto histórico.
- Melhorias.md fica externo aos commits. Estes requisitos são derivados e rastreáveis, sem copiar o original para o projeto.
- A imagem anexada lista decisões, não arte: [cópia durável](references/decisoes-anexadas.jpeg).
- [Partida cartoon](references/direcao-cartoon-candidata.png) confirmada pelo usuário como referência visual; [corações/penas](references/hud-coracoes-penas-referencia.png) documenta sua origem estilística. Não são assets finais. Texto prevalece: duas barras sem números e touch condicional. Não adicionar tartarugas ou espécies fora do catálogo.
- Unity 6000.4.3f1, regras compartilhadas, elenco fechado, pooling e constituição 2.0.0 permanecem obrigatórios.
- Esta solicitação autoriza planejamento; implementação futura somente por task/intervalo solicitado.

## Clarifications

Respostas e aprovações recebidas em 03/11/2026: XP garantido, cura comum probabilística de 10%/15%, miniboss 50% garantido, Conjunto A de coleta mantido; detalhes de interface/controle aprovados; somente Bolhas e ordem de arte aprovadas. A ênfase final do usuário confirma que cura comum NÃO é garantida por morte e não altera a escolha anterior de 1 em 10.

Decisões D1–D8 resolvidas em [decisions.md](decisions.md), com alternativas rejeitadas claramente separadas. Cura nunca ressuscita morto. Relíquias preservam dono da pausa; Cancel na melhoria fecha detalhe/retorna foco, sem escolher/pular implicitamente; encerrar oferta exige escolha ou PULAR válido.

Ordem de arte aprovada: personagens/ameaças/obstáculos → armas/efeitos/recompensas → HUD/menus/controles → cenário/água; aprovar cena-amostra antes de lote. Revisões e tarefas de diagnóstico/tuning técnico continuam necessárias, sem novas decisões de balanceamento implícitas.


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](decisions.md) e o relatório T030.
