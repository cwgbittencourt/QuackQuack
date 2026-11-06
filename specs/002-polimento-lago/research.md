# Pesquisa local — 03/11/2026

> Revisão D7 aprovada em 05/11/2026 durante T023: Sabão passa a +16% de raio por nível, teto +80%, somente Bolhas. Os valores 8%/40% abaixo registram a decisão anterior, substituída por esta revisão.

Inspeção estática somente; nenhum teste, gameplay ou profiling executado nesta rodada documental. Respostas D1–D8 e complementos aprovados em [decisions.md](decisions.md); Phase 0 concluída para o design, sem alegar causa física reproduzida. Pesquisa paralela anterior realizada conforme speckit-plan.

## Arena: causa ainda não confirmada

**Evidência**: PlayerController usa Rigidbody2D dinâmico/contínuo, atribui velocidade em FixedUpdate e não define área caminhável explícita. LakeArenaSetup cria paredes de espessura 0,5 e jogador de raio 0,42. EnemyUpdateScheduler.Move desloca Body.position e Shape.Cast apenas camada 8. PooledEnemy preserva contato jogador/inimigo e suprime inimigo/inimigo.

**Decisão preliminar**: reprodução determinística com posições/contatos/penetração nas bordas/cantos/pedras antes de escolher solução. Acúmulo de cinemáticos sobre jogador dinâmico é hipótese, não causa provada. TerrainAndKnockbackTests cobre QuackTarget, não jogador cercado.
**Alternativas**: clamp, teleporte ou remover contatos antecipadamente podem ocultar causa/eliminar pressão. Comparar correção de movimento/pressão, contrato de área e recuperação excepcional após reprodução.

## Aura e passivas

**Evidência**: WeaponRunner.AreaHit faz Linecast camada 8 para todas as áreas; aura é bloqueada. WeaponRunner e PlayerController aplicam relíquias, não níveis de passivas. UpgradeSelectionBridge descreve GetModifier; UpgradeSystem registra aquisição. PrototypeBuildCatalogSetup associa Sabão a AreaRadius.

**Decisão**: política de oclusão por comportamento, aura atravessa obstáculo para dano sem alterar movimento ou outras armas. Resolver atributos efetivos base/passivas/relíquias/terreno sem mutar assets ou acumular novamente por frame; visual usa os mesmos valores. D7 define elegibilidade.
**Alternativas**: aumentar só sprite ou retirar oclusão global quebra regra. Testar seis passivas, níveis/tetos, recomputação, evolução, terreno, relíquias e equivalência área visual/real.

**Detalhe após resposta do usuário**: sabao-magico.asset confirma CompatibleWeaponId=bolhas, ModifierPerLevel=0,08, ModifierLimit=0,4. PrototypeBuildCatalogSetup gera Bolhas com AreaRadius=0 e HitRadius=0,2. WeaponRunner.StepSegment faz CircleCast com Definition.HitRadius fixo. Portanto aplicar multiplicador apenas sobre AreaRadius zero não resolve Bolhas; contrato precisa definir raio efetivo de acerto e escala visual. Vínculo compatibleWeaponId confirma associação autoral, mas não prova elegibilidade exclusiva/global: essa é a proposta submetida ao usuário.

## Bumerangue

**Evidência**: WeaponProjectile mantém hitIds único por vida; WeaponRunner troca direção na metade do lifetime, sem reiniciar fase de hits. WeaponRunnerTests.BoomerangTravelsOutAndBackWithoutHittingSameTargetTwice exige um único acerto total: contrato antigo conflitante.

**Decisão**: fases Ida/Volta e deduplicação por fase/lease; substituir teste conflitante. Cobrir passo atravessando retorno, alvo no ponto de retorno, múltiplos colliders, pooling e alvo reciclado.
**Alternativas**: limpar a cada frame duplica dano; instanceId sozinho não distingue reuso.

## Controle e menus

**Evidência**: UnifiedPlayerInput ativa mobileRoot pelo contexto, não pela disponibilidade de Gamepad. Source é última atividade e pode ficar antigo após desconexão. ActionPlayerInput limpa bordas; inputactions tem Move/Quack/Pause sem Relics. RelicLoadoutView não define foco/Cancel. UpgradeSelectionView seleciona primeira carta e depende de navegação automática. LakeRelics já preserva ownsPause.

**Decisão**: separar disponibilidade de última fonte; reagir conexão/reconexão/habilitação e perda/desabilitação; limpar comandos e restaurar fallback. Adicionar Relics e grafo de foco, preservando propriedade da pausa. D6 define equivalências; Cancel não escolhe/pula implicitamente.
**Alternativas**: Source sozinho não atende controle conectado inativo; reiniciar em hotplug perde partida. Validar hardware físico além de testes sintéticos.

## Cura e XP

**Evidência**: ExperiencePickup coleta apenas por trigger, com claimed/pool. PlayerState não oferece Heal. RunDirector.Reward gera XP/moedas. EnemyReward possui LeaseId/DefinitionId/Position; catálogo resolve papel.

**Decisão**: cura pura limitada à falta, sem reanimar morto; drop por origem/lease e RNG determinístico independente de ofertas. Atração centralizada, pausada e limpa no reuso. Reservar capacidade para cura garantida de miniboss, sem perda silenciosa por pool cheio. D1–D3 condicionam comportamento/dados finais.
**Alternativas**: Instantiate por morte, sorteio global compartilhado e cooldown oculto prejudicam desempenho/reprodutibilidade. Testar vida cheia/morta, maxHealth modificado, duplicações, saturação, atração dentro/fora do raio, obstáculo, pausa e afastamento.

**Garantia nova de XP**: usuário exige XP em toda derrota. RunDirector.Reward atualmente retorna se Experience<=0 e incrementa SuppressedPickups quando pool cheio. OnCombat trata morte de chefe com moedas/estatística separadas, sem XP. Planejar validação de XP positivo por inimigo elegível e garantir recompensa sob saturação, excluindo o chefe final por decisão T030, antes de declarar atendimento; despawn não é derrota. Quantidades não informadas não devem ser inventadas.

## HUD

**Evidência**: ExperienceState mantém XP acumulado/limiares privados; UpgradeSystem expõe XP/Level/PendingLevels, sem progresso do intervalo. HudSnapshot não possui XP/nível. GameplayHud, GameplayHudSetup e PlayerController.Snapshot produzem números/dicas permanentes. EnemyState e FrogKingController têm ciclos de vida distintos; não localizada view de barras especiais.

**Decisão**: snapshot de progresso produzido pela regra, sem curva duplicada na UI; tratar múltiplos níveis e último limiar. Recursos normalizados com ícones/estados; dicas separadas; registro de especiais por instância+lease. Reusar SafeAreaController, ReferencePanelFit, IntegratedUiSetup e ampliar IntegratedUiLayoutTests. D4/D5 definem composição.
**Alternativas**: ocultar texto só no prefab deixa geradores reintroduzirem conteúdo; ler por nome/instanceId pode exibir lease errado.

## Conteúdo e arte

**Evidência**: PrototypeBuildCatalogSetup/LakeEncounterCatalogSetup fecham conteúdo:

- Armas: Bumerangue, Ovos, Quack frontal, Aura de energia, Foguetes, Bolhas.
- Passivas: Pena aerodinâmica, Ninho reforçado, Pulmão poderoso, Amuleto do lago, Óculos de aviador, Sabão mágico.
- Evoluções: Tornado de penas, Incubadora de combate, Quack supersônico.
- Comuns: Formiga, Aranha, Vespa, Besouro, Caramujo Aquático.
- Minibosses: Libélula Alfa e Sapo-Cururu; chefe Sapo-Rei; dez relíquias existentes preservadas.

**Decisão**: manifesto por asset com ID, referência, autoria/licença, prioridade, animação/importação e aceite. Separar arte de colisão; D8 confirma direção e ordem.
**Alternativas**: screenshot não é conjunto de assets animáveis; tartarugas ilustradas não autorizam mudar elenco.

## Decisões consolidadas após aprovação

- D1/D2: cura comum sorteada com 10%, fração 15%, sem cooldown/teto de gameplay; miniboss gera uma cura 50% sempre, sem segundo sorteio comum. XP independente e garantido; chefe sem cura herdada.
- D3: raio de captura 3 × raio físico escalado do Pato, equivalente a um diâmetro além da borda; base 1,26. Velocidade 1,25 × velocidade efetiva na água, base 7,5. Mesmo contrato XP/cura; captura persiste após afastamento, pausa suspende, morte/fim cancelam. Cura cheia ainda não capturada permanece no chão; cura já capturada é descartada se a vida ficar cheia (revisão explícita D3/T029 em 05/11/2026).
- D4/D5: dicas só nos primeiros 10 segundos de simulação; símbolos atualizam sem reset. Barras individuais ancoradas acima do especial, sem painel agrupado.
- D6: Xbox/PlayStation/genérico, mapeamentos reais, perfil configurável e fallback focalizável; Cancel não pula nível obrigatório; preservar ownsPause.
- D7: somente Bolhas, +8%/nível até +40% no raio efetivo e visual, prévia antes/depois. Não aplicar AreaRadius sobre zero; definir raio efetivo a partir do HitRadius de Bolhas e não alterar aquisição/dano/velocidade.
- D8: cartoon confirmado; ordem de assets em decisions.md. Assets precisam de autoria/licença/importação e aceite de amostra antes do lote.

**Arquitetura de saturação escolhida**: separar RewardLedger lógico das views pooled; registrar cada derrota uma vez antes de tentar apresentação. Pool cheio nunca descarta XP/cura ou dispara novo sorteio. Preservar posição, quantidade/fração e identidade em pendências até obter view. Capacidade e drenagem devem ser demonstradas para uma partida completa com maior taxa autoral de mortes e benchmark; falha de capacidade impede aceite, não aplica teto de chance. Não conceder à distância para contornar falta de view. Miniboss tem prioridade de materialização; Chefe final não concede XP nem cura (revisão T030 aprovada).

**Alternativa de saturação rejeitada**: manter SuppressedPickups ou conceder XP imediatamente na morte mudaria as garantias de drop/proximidade. Pending ledger deve ser observável em testes e no diagnóstico de desempenho. Dimensionamento numérico dos pools é resultado de profiling/catálogo, não nova decisão de chance.

## Ambiente e limites

Manifest local: Input System 1.19.0, URP 17.4.0, uGUI 2.0.0, Test Framework 1.6.0; Unity 6000.4.3f1. Nenhuma atualização proposta. Pesquisa local fundamenta estes achados, sem novas alegações sobre APIs externas. Resultados anteriores 465 EditMode/258 PlayMode/34 por player são históricos, não execução desta rodada nem contagem fixa futura.

**Revisão explícita D3 na T029 (05/11/2026):** cura já capturada que encontra vida cheia é descartada, sem cura nem retorno ao chão. Substitui a recomendação anterior de aguardar falta de vida acompanhando. Cura ainda não capturada continua no chão com vida cheia; ver decisions.md.


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](decisions.md) e o relatório T030.
