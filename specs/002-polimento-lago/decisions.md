# Decisões de produto — 03/11/2026

> Revisão D7 aprovada em 05/11/2026 durante T023: Sabão passa a +16% de raio por nível, teto +80%, somente Bolhas. Os valores 8%/40% abaixo registram a decisão anterior, substituída por esta revisão.

Respostas e complementos aprovados pelo usuário em 03/11/2026. As alternativas históricas não são requisitos. Nenhum parâmetro foi aprovado por silêncio.

## Histórico anterior às respostas — substituído pelo Conjunto A

As recomendações desta primeira tabela são histórico, não o plano vigente. Em particular, dicas contextuais adicionais, barras em painel e Sabão em várias armas não foram aprovados. As escolhas recebidas e a proposta atual estão nas seções seguintes.

| ID | Decisão necessária | Recomendação inicial |
|---|---|---|
| D1 | Chance de cura comum, intervalo e teto ativo | Parâmetros editáveis, RNG independente das ofertas; confirmar valores e limites globais/por origem. |
| D2 | Frações finais de cura | Propor 15% comum e 50% miniboss sobre vida máxima, limitadas à falta; sem herdar ao chefe. |
| D3 | Raio/velocidade/afastamento do XP | Raio curto e velocidade editáveis; propor continuar após captura, atravessando obstáculos; suspender em pausa e cancelar em morte/fim. Números pendentes. |
| D4 | Duração/reapresentação de dicas | Propor uma vez por ação por partida; repetir ao trocar família quando comando muda; tempo de jogo, sem empilhar. Duração pendente. |
| D5 | Barras simultâneas | Propor individuais compactas, chefe primeiro, minibosses em ordem estável; validar coexistência em 4:3. |
| D6 | Famílias e equivalências | Propor Xbox Menu/Start pausa, View/Back relíquias; PlayStation Options e Share/Create quando expostos pelo layout; genérico usa binding real. Direcional/analógico navegam, sul confirma, leste retorna; Cancel não pula escolha obrigatória. Confirmar dispositivos alvo e fechamento permitido. |
| D7 | Sabão Mágico | Propor aura, áreas de ovos/foguetes/bolhas e cone da arma Quack frontal; confirmar evoluções e grandeza afetada por arma. Não aumentar automaticamente Quack ativo ou collider do bumerangue. Mostrar efeito equivalente à área real. |
| D8 | Referência/produção visual | Propor cartoon da conversa como linguagem; elenco real do catálogo. Duas barras sem números prevalecem sobre corações discretos da referência. Priorizar Pato/ameaças/bloqueios, armas/pickups, HUD, cenário/água; aprovar amostra antes de lote. |

## Respostas confirmadas

| Decisão | Escolha recebida |
|---|---|
| D1 | XP sempre cai de inimigos; cura comum tem chance de 1 para 10. |
| D2 | Cura comum de 15% da vida máxima; cura de 50% sempre cai do miniboss. Limitar à vida faltante. |
| D3 | XP e cura atraídos a um Pato de distância; velocidade rápida, um pouco acima do Pato na água; após atrair seguem até o Pato independentemente da distância posterior. |
| D4 | Instruções visíveis nos primeiros 10 segundos do início da partida. |
| D5 | Barras individuais acima de cada miniboss/chefe. |
| D6 | Cobrir Xbox, PlayStation e controles genéricos. Usuário aceita receber proposta de equivalências. |
| D7 | Problema principal relatado: Sabão Mágico não torna perceptível a ampliação de Bolhas. Usuário pede proposta de elegibilidade. |
| D8 | Partida cartoon da conversa confirmada como referência, conforme Melhorias.md; usuário pede proposta da ordem dos assets. |

## Conjunto A — aprovado em 03/11/2026

| Tema | Complemento proposto | Consequência/alternativa para comparação |
|---|---|---|
| XP | Cada derrota elegível gera XP, sem sorteio nem descarte por saturação; manter concessão única por lease. Expiração/despawn não é derrota. Chefe final não concede XP nem cura (revisão T030 aprovada). | Requer rever supressão atual no pool; não significa adicionar chance de XP nem duplicar recompensa. |
| Cura | Sorteio independente de 10% por derrota comum, sem cooldown adicional e sem teto de gameplay que reduza essa chance. XP e cura podem cair juntos. Miniboss gera exatamente uma cura de 50%, sem sorteio extra de comum; chefe não gera cura automática. | Cooldown/teto reduziriam chance efetiva; se desejados precisam de decisão posterior. Capacidade técnica dos pools deve preservar recompensas, com fallback agrupado/pendente testável e medido. |
| Raio | Um Pato significa um diâmetro físico de espaço livre entre sua borda e o centro do pickup. Com raio atual do Pato 0,42, isso dá espaço de 0,84 e distância de captura centro-centro de 1,26 unidades. Recalcular por escala/collider, não pelo desenho da animação. | Alternativa mais curta: 0,84 centro-centro, equivalente a meio diâmetro de espaço livre. Nenhuma das duas inicia atração de longe. |
| Velocidade | 1,25 × velocidade efetiva do Pato na água, incluindo bônus relevantes; com base atual 6, atração base 7,5 unidades/s. Reavaliar referência ao mudar atributos. | 1,10 × seria mais suave, mas demoraria mais para alcançar o Pato em fuga. Sem teleporte e sem duração máxima de atração enquanto jogador vive/partida simula. |
| Ciclo de coleta (histórico: regra de cura cheia substituída na revisão D3/T029 abaixo) | XP e cura ignoram obstáculos durante atração, pausam com simulação, retomam após pausa e não concedem após morte/fim. Cura com vida cheia fica no chão sem iniciar atração; se encher durante atração, o pickup acompanha sem consumir e pode curar quando faltar vida. | Evita desperdiçar cura sem alterar sua fração. Alternativa: consumir mesmo com vida cheia; não recomendada. Ao finalizar partida, tratar explicitamente XP já devido e itens não coletados. |
| Instruções | 10 segundos de simulação no início de cada partida, pausa suspende contagem; trocar entrada atualiza símbolos durante essa janela sem reiniciar contador. Depois não reapresentar automaticamente nesta entrega. | Preserva os 10 segundos solicitados sem dicas permanentes; alternativa futura é reapresentação contextual configurável. |
| Barras | Uma por miniboss/chefe, ancorada acima da entidade, cor/ícone distintos e progresso normalizado. Pequenos deslocamentos de rótulos/linhas quando sobrepõem; não agrupar barras nem deslocar para painel central. Alvo fora de tela não cria barra flutuando sobre outro alvo. | Validar vários alvos próximos, bordas e Safe Area; ligação instância+lease impede barra reaparecer no inimigo reciclado. |
| Xbox | Menu/Start pausa; View/Back relíquias; direcional/analógico esquerdo navega; A confirma; B retorna. | Ícones por binding/família detectada; teclado e touch preservados. |
| PlayStation | Options pausa; Share/Create relíquias quando layout expuser; direcional/analógico esquerdo navega; Cross confirma; Circle retorna. | Validar no modelo físico e plataforma; fallback textual/seleção de perfil quando layout não informar identificação suficiente. |
| Genérico | Start pausa; Select/Back relíquias conforme binding; botão sul confirma e leste retorna. Exibir nome real do binding ou perfil selecionado, sem adivinhar glifos. | Se Select inexistente, permitir atribuir atalho de relíquias em perfil de controle e manter acesso visível/focalizável pelo menu; nenhum dispositivo fica sem acesso. |
| Retorno | Em relíquias, retorno/atalho fecha e restaura pausa/foco anteriores. Na seleção obrigatória de melhoria, retorno fecha detalhe/submenu e devolve foco à carta; tela principal só encerra por escolha ou PULAR válido, sem confirmar/pular implicitamente. | Evita navegação travar e também evita fechar a escolha deixando nível pendente sem tratamento. |
| Sabão | Elegibilidade proposta: somente Bolhas nesta entrega. Manter +8% de raio por nível, teto +40%, já autorados, ampliando raio de acerto e diâmetro visual juntos; sem mudar dano, velocidade ou alcance de aquisição. Na seleção, prévia antes/depois e descrição da grandeza; no jogo, bolhas realmente maiores. | Alternativa B: aplicar também a áreas de Ovos, Aura e explosões de Foguetes, Incubadora e alcance do cone Quack frontal/supersônico com ângulo preservado. Isso amplia balanceamento; Bumerangue/Tornado e Quack ativo continuariam excluídos. Não aprovado. |
| Percentual Sabão | O percentual é sobre raio, não área em unidades quadradas. +8% no raio corresponde a +16,64% na área; +40% no raio a +96% na área. Explicar na descrição e verificar no visual sem exagerar o bônus. | Alterar para percentual de área seria outro balanceamento. Dados atuais representam AreaRadius. |
| Assets | 1) Pato, elenco aprovado e obstáculos; 2) armas/projéteis, aura/Sabão e recompensas; 3) HUD/menus/controles; 4) margens, vegetação, pedras e água animada. Aprovar uma cena-amostra antes de produzir lote; inventariar origem/licença/importação. | Priorizar primeiro o que determina leitura de ameaça/alcance; cenário não deve encobrir combate. Referência aprovada não equivale a cada asset aprovado. |

## Aprovação final e precedência

- Coleta: usuário respondeu “a cura comum deve ter uma regra de chance de cair, não pode ser 100% por morte. o Restante pode manter como o sugerido.” Portanto prevalece 10% de chance por morte comum, conforme resposta anterior de 1 para 10, e o restante do Conjunto A foi aprovado. Não é cura garantida nem quota de um drop por dez mortes.
- Interface/controle: “Aprovar detalhes”. Dicas em tempo de simulação, sem repetição; barras individuais acima; todos os mapeamentos e fallback descritos no Conjunto A.
- Sabão/arte: “Somente Bolhas e ordem proposta”. Alternativa B de áreas globais NÃO foi aprovada. Manter 8% por nível/40% no raio real e visual de Bolhas; demais armas não recebem Sabão nesta entrega.
- D8: referência cartoon confirmada; duas barras sem números conforme texto; ordem personagens/ameaças/obstáculos → armas/efeitos/recompensas → HUD/menus/controles → cenário/água; cena-amostra antes do lote.

As oito decisões estão resolvidas. A revisão artística da amostra e as medições são gates futuros de implementação, não decisões de produto ainda pendentes. Nenhum código foi alterado no planejamento.

## Fontes e precedência

- Melhorias.md externo lido em C:/Users/lucas/Desktop/Melhorias.md, não copiado/versionado. Caminhos E: nele são contexto histórico; o workspace atual é válido.
- [Imagem anexada](references/decisoes-anexadas.jpeg) contém decisões, não referência artística.
- [Partida cartoon candidata](references/direcao-cartoon-candidata.png) e [corações/penas](references/hud-coracoes-penas-referencia.png) vieram da conversa. Não são assets finais nem autorizam espécies novas; requisitos escritos prevalecem.

## Revisão D7 — 05/11/2026, aceite comparativo T023

O usuário aprovou explicitamente: “Dobrar somente o bônus do Sabão: base 0,20; 16% por nível e teto de 80%, dando N1 0,232 e N5 0,36”.

Mantém-se o raio base 0,20; o multiplicador é `1 + min(0,16 × nível, 0,80)`. N0/N1/N5: raios 0,20/0,232/0,36 e diâmetros 0,40/0,464/0,72. O aumento de área correspondente é 34,56% em N1 e 224% em N5. Visual e colisão usam o mesmo raio; dano, velocidade, aquisição e demais armas não mudam por Sabão. As outras cinco passivas mantêm seus dados anteriores. Os relatórios T015–T022 são evidências históricas do balanceamento anterior; o reteste vigente pertence à T023.

Durante a comparação, o usuário também relatou ausência de feedback visível do ataque da arma Quack frontal. T023 acrescenta um pulso temporário do setor nominal, com alcance/ângulo autorados e relógio de simulação; não altera resolução de dano ou oclusão e não substitui a futura direção de arte.

## Revisão D3 — 05/11/2026, implementação T029

O usuário corrigiu explicitamente a proposta de cura acompanhando o Pato com vida cheia: “caso a cura seja capturada e o pato estiver com a vida cheia, a cura capturada deve ser descarta”. A partir desta revisão, cura já capturada é descartada ao encontrar vida cheia durante simulação, sem aplicar cura, sem claim positivo e sem reenfileirar o pickup. Cura ainda no chão não inicia captura com vida cheia e permanece disponível. Pausa/seleção suspendem o processamento; o descarte ocorre na retomada. Os trechos anteriores de D3 que propunham acompanhar até haver falta são históricos e foram substituídos.


## Revisão T030 — chefe final sem XP (05/11/2026)

Decisão explícita do usuário: “Remover o XP do chefe (recomendado)”. O chefe final não gera XP nem cura, pois encerra a partida. Mantêm-se moedas, bônus de vitória e registros uma única vez. As instruções anteriores sobre XP positivo/editável/liquidado do chefe são históricas e estão substituídas por esta revisão. BossExperience foi removido dos dados e geradores. Pickups pendentes no encerramento são cancelados, sem coleta automática. Capacidade vigente: 3.323 derrotas elegíveis e 6.646 registros pré-alocados; relatórios anteriores preservam os números daquela versão.
