# Tasks: Polimento e confiabilidade do Lago

**Input**: [spec](spec.md), [plan](plan.md), [research](research.md), [data-model](data-model.md), [contracts](contracts/gameplay-contracts.md), [decisions](decisions.md).
**Status**: T001–T013 e T015–T034 concluídas; T014 e T035–T053 pendentes (regressões T008/T009 corrigidas; aceite físico da US2, global e performance ainda pendentes). IDs locais desta feature 002; não continuam nem substituem T001–T078 da feature 001.
**Revisão T023 (05/11/2026)**: descrições vigentes de Sabão usam 16%/80%; números de testes e relatórios T015–T022 documentam o balanceamento histórico 8%/40%.
**Tests**: exigidos por FR-018/constituição. Testes de regra não aprovam gameplay, entrada física ou desempenho.
**Execution**: uma task por solicitação ou somente intervalo explícito. Validar, atualizar checkbox e commitar incremento lógico; parar para revisão/publicação. Se faltar hardware/aceite, registrar pendência sem marcar a validação concluída. Nenhuma task autoriza ampliar escopo.

## Phase 1: Setup

Objetivo: preservar baseline, workspace e referência antes de modificar gameplay.

- [x] T001 Registrar baseline de commit, versões, catálogos, testes e limitações/hardware em `specs/002-polimento-lago/baseline.md`, usando cópia completa isolada conforme `specs/002-polimento-lago/quickstart.md`; medir baseline 200/400 sem inventar resultados e manter T075 antiga aberta (FR-018).

## Phase 2: Foundational

Objetivo: viabilizar diagnóstico reproduzível sem alterar regras. Não recriar engine, pools ou infraestrutura já existentes.

- [x] T002 Preparar captura opt-in de posições/contatos/penetrações/leases e seed em `Assets/QuackQuack/Systems/Diagnostics/ArenaPressureDiagnostics.cs` e fixture auxiliar de `Assets/QuackQuack/Tests/PlayMode/ArenaContainmentTests.cs`, sem logs/alocações no combate normal; documentar ativação em `specs/002-polimento-lago/arena-diagnosis.md` (FR-016/018; depende T001).

## Phase 3: US1 — Permanecer na arena (P1, MVP)

**Goal**: impedir expulsão/aprisionamento preservando pressão. **Independent Test**: horda em cada borda, canto e pedra, incluindo arena do chefe; remover pressão e verificar retomada.

- [x] T003 [US1] Reproduzir expulsão na LakeRun real usando `Assets/QuackQuack/Tests/PlayMode/ArenaContainmentTests.cs` e registrar causa confirmada versus hipótese, seed/timestep/contatos, geometria e alternativas em `specs/002-polimento-lago/arena-diagnosis.md`; não escolher clamp/teleporte antes da evidência (FR-016; T002).
- [x] T004 [US1] Codificar regressões que falham para bordas/cantos/obstáculos, passo longo e retomada quando houver espaço em `Assets/QuackQuack/Tests/PlayMode/ArenaContainmentTests.cs`; incluir não remoção da pressão normal e arena de chefe (FR-016; T003).
- [x] T005 [US1] Corrigir a causa demonstrada em `Assets/QuackQuack/Gameplay/Enemies/EnemyUpdateScheduler.cs`, `Assets/QuackQuack/Gameplay/Player/PlayerController.cs` e, somente se justificado, `Assets/QuackQuack/Gameplay/Player/ArenaConfinement.cs`; registrar escolha/alternativas, testar eventual cálculo geométrico puro em `Assets/QuackQuack/Tests/EditMode/ArenaBoundsTests.cs` e verificar corpo inteiro sem desativar contatos globais (FR-016; T004).
- [x] T006 [US1] Integrar geometria/configuração necessária via `Assets/QuackQuack/Editor/LakeArenaSetup.cs` e `Assets/QuackQuack/Editor/LakeRunSetup.cs`, gerando `Assets/QuackQuack/Scenes/LakeRun.unity` pelo Unity e preservando .meta; manter obstáculos/água/terra e arena do chefe (FR-016; T005).
- [x] T007 [US1] Validar pressão/recuo/retomada na partida Windows/Android e regressão automatizada; registrar evidências e limitações em `specs/002-polimento-lago/validation-us1.md` (FR-016/018; T006).

## Phase 4: US2 — Controle e menus (P1)

**Goal**: hotplug sem perda de ações e navegação completa. **Independent Test**: teclado/controle/toque, troca em gameplay, pausa, relíquias, melhoria e resultados sem mouse.

- [x] T008 [P] [US2] Criar casos de disponibilidade/hotplug Added/Removed/Enabled/Disabled, última fonte antiga e comandos presos em `Assets/QuackQuack/Tests/PlayMode/ControllerAvailabilityTests.cs`, portáveis aos players; registrar falhas antes da implementação (FR-013; T007).
- [x] T009 [P] [US2] Criar casos de foco/Cancel/atalho e preservação de pausa em `Assets/QuackQuack/Tests/PlayMode/RelicLoadoutFlowTests.cs` e `Assets/QuackQuack/Tests/PlayMode/UpgradeSelectionTests.cs`, incluindo oferta obrigatória sem pulo implícito (FR-014/015; T007).
- [x] T010 [US2] Adicionar ação comum Relics, bindings Xbox/PS/genérico e borda por contexto em `Assets/QuackQuack/Platform/Input/QuackQuackInput.inputactions`, `Assets/QuackQuack/Platform/Input/ActionPlayerInput.cs` e `Assets/QuackQuack/Core/Shared/PlayerCommand.cs`; sul confirma/leste retorna na UI, nenhum vazamento de gameplay (FR-015; T008/T009).
- [x] T011 [US2] Separar disponibilidade de última atividade em `Assets/QuackQuack/Platform/Input/UnifiedPlayerInput.cs`; ocultar touch quando controle conectado/habilitado, restaurar na perda/desabilitação limpando comandos e mantendo contexto, inclusive resultados (FR-013; T010).
- [x] T012 [US2] Implementar perfis versionados de bindings/glifos reais em `Assets/QuackQuack/Platform/Input/ControlProfile.cs`, `Assets/QuackQuack/Platform/Input/ControlProfileStore.cs` e `Assets/QuackQuack/Presentation/UI/ControlBindingsView.cs`; salvar separado do progresso, permitir atribuição sem Select e impedir perda do último acesso sem alternativa focalizável; testar roundtrip/corrupção em `Assets/QuackQuack/Tests/EditMode/ControlProfileTests.cs` (FR-015; T011).
- [x] T013 [US2] Implementar grafos explícitos/foco visível/confirmar/retorno e toggle de relíquias em `Assets/QuackQuack/Presentation/UI/RelicLoadoutView.cs`, `Assets/QuackQuack/Presentation/UI/UpgradeSelectionView.cs`, `Assets/QuackQuack/Gameplay/Upgrades/LakeRelics.cs`, `Assets/QuackQuack/Editor/RelicSetup.cs` e `Assets/QuackQuack/Editor/UpgradeSelectionSetup.cs`; preservar ownsPause, limite de equipadas e escolha/PULAR válidos; integrar perfil/fallback focalizável no gerador `Assets/QuackQuack/Editor/PolishUiSetup.cs` e gerar prefabs via Unity antes do aceite físico (FR-014/015; T012).
- [ ] T014 [US2] Executar teste físico Xbox/PlayStation/genérico no Windows e Android, toque restaurado e atalhos/foco em todos os contextos; registrar modelos e pendências em `specs/002-polimento-lago/validation-us2.md` (FR-013–015/018; T013). **Em validação**: Xbox/Windows C01–C09 aprovado após correções de pausa/resultados (R1/R2); DualSense e 8BitDo/Windows também aprovados. Xbox/S23 Bluetooth aprovado em C01–C10; DualSense/S23 Bluetooth também aprovado em C01–C10. 8BitDo/S23 não reconhecido por OTG, causa ainda não isolada; validação adiada por decisão do usuário, mantendo os aceites de Xbox e DualSense. T014 permanece aberta.

## Phase 5: US3 — Armas e passivas (P2)

**Goal**: regras e alcance reais antes da arte. **Independent Test**: cada passiva/arma isolada e combinada, alvos dentro/fora/atrás de obstáculo, retorno do bumerangue.

- [x] T015 [P] [US3] Testar seis passivas, níveis/tetos, recomputação, terreno, relíquias e evolução em `Assets/QuackQuack/Tests/EditMode/EffectiveAttributesTests.cs`; conferir Bolhas 1,16×/1,80× raio e exclusão de demais armas de Sabão (FR-008; T014). **Entrega de testes**: 55 casos novos vermelhos pela ausência da resolução T016; relatório em `test-report-t015.md`. T014 segue aberta por 8BitDo/S23 adiado; avanço para T015 solicitado explicitamente pelo usuário.
- [x] T016 [US3] Criar resolução pura e idempotente em `Assets/QuackQuack/Core/Combat/EffectiveAttributes.cs`, sem mutar assets, preservando limites/ordem de relíquias e valores finitos/positivos; zero nível retorna base, Sabão mantém dados 16%/nível/80% apenas em Bolhas (FR-008; T015). **Validado**: 573/573 EditMode, incluindo 82 casos de atributos; ver `test-report-t016.md`. Integração de gameplay permanece na T017.
- [x] T017 [US3] Conectar níveis de passivas à regra de armas/jogador em `Assets/QuackQuack/Gameplay/Weapons/WeaponRunner.cs`, `Assets/QuackQuack/Gameplay/Player/PlayerController.cs` e `Assets/QuackQuack/Systems/Upgrades/UpgradeSystem.cs`; invalidar/recalcular ao alterar build/terreno/relíquias sem acumular bônus por frame (FR-008; T016). **Validado**: 579/579 EditMode e 404/404 PlayMode, incluindo integração e escolha na cena real; ver `test-report-t017.md`. Colisão/visual de Bolhas permanecem na T018.
- [x] T018 [US3] Fazer colisão e apresentação de Bolhas consumirem raio efetivo baseado em HitRadius, não AreaRadius zero, em `Assets/QuackQuack/Gameplay/Weapons/WeaponProjectile.cs` e `Assets/QuackQuack/Gameplay/Weapons/WeaponRunner.cs`; testar área real e invariância de dano/velocidade/aquisição em `Assets/QuackQuack/Tests/PlayMode/BubbleAreaTests.cs` (FR-008; T017). **Validado**: 19 casos focados; 579/579 EditMode e 423/423 PlayMode; ver `test-report-t018.md`. Prévia e aceite jogável permanecem em T022/T023.
- [x] T019 [US3] Separar oclusão da aura e criar alcance translúcido animado ligado ao raio real em `Assets/QuackQuack/Gameplay/Weapons/WeaponRunner.cs` e `Assets/QuackQuack/Presentation/Weapons/WeaponVisuals.cs`; testar parede, fronteira, múltiplos colliders e oclusão de outros ataques em `Assets/QuackQuack/Tests/PlayMode/AuraOcclusionTests.cs` (FR-007; T018). **Validado**: 579/579 EditMode e 436/436 PlayMode, incluindo 14 casos de aura; ver `test-report-t019.md`. Aceite artístico/jogável permanece separado.
- [x] T020 [P] [US3] Substituir teste antigo BoomerangTravelsOutAndBackWithoutHittingSameTargetTwice por casos um hit/trecho, retorno no mesmo passo, alvo no retorno, múltiplos colliders e reuso de alvo/projétil em `Assets/QuackQuack/Tests/PlayMode/WeaponRunnerTests.cs`; demonstrar falha antes da correção (FR-009; T014, antes T021). **Entrega de testes**: 11 casos novos; suíte PlayMode com 438 aprovados e 8 falhas esperadas exclusivamente nesses casos, zero ignorados; ver `test-report-t020.md`. Correção permanece na T021.
- [x] T021 [US3] Implementar fases Outbound/Inbound e histórico por fase/lease em `Assets/QuackQuack/Gameplay/Weapons/WeaponProjectile.cs` e `Assets/QuackQuack/Gameplay/Weapons/WeaponRunner.cs`, sem limpar a cada frame; preservar segmentação/obstáculos/pausa e reset pooled (FR-009; T019/T020). **Validado**: 579/579 EditMode e 449/449 PlayMode, incluindo 31 casos de armas; oito falhas T020 corrigidas. Ver `test-report-t021.md`.
- [x] T022 [US3] Implementar prévia de raio atual/previsto e crescimento visual real de Bolhas em `Assets/QuackQuack/Presentation/UI/UpgradeAreaPreview.cs`, `Assets/QuackQuack/Gameplay/Upgrades/UpgradeSelectionBridge.cs` e `Assets/QuackQuack/Presentation/Weapons/WeaponVisuals.cs`; integrar preview via `Assets/QuackQuack/Editor/UpgradeSelectionSetup.cs` no prefab gerado pelo Unity antes do aceite; comunicar raio, não confundir +16% de raio com +16% de área, mostrar efeitos ativos das demais passivas (FR-006/008; T021). **Validado**: prefab atualizado pelo Unity sem remover IDs existentes; 584/584 EditMode e 458/458 PlayMode; capturas das cartas nas quatro proporções revisadas. Ver `test-report-t022.md`; aceite jogável permanece em T023.
- [x] T023 [US3] Comparar arma/passiva antes/depois em gameplay e testes de integração; registrar todos os modificadores, aura e ida/volta em `specs/002-polimento-lago/validation-us3.md` (FR-006–009/018; T022). **Concluída (05/11)**: Sabão 16%/80% aprovado; feedback frontal e anel de detonação de Ovos/Incubadora corrigidos. 584/584 EditMode, 462/462 PlayMode e 97/97 Android nativo R3 aprovados; quatro builds R4 IL2CPP geradas. Comparações, seis passivas e três evoluções aprovadas nos roteiros Windows/S23; valores de relíquias/terrenos confirmados no Windows. Limites e evidências em `validation-us3.md`; T014 continua pendente por 8BitDo/S23 adiado.

## Phase 6: US4 — Cura e coleta (P2)

**Goal**: recompensas preservadas, cura probabilística e coleta fluida. **Independent Test**: derrotas/replays/saturação, proximidade/afastamento/obstáculos, pausa e cura cheia/morta.

- [x] T024 [P] [US4] Criar testes de cura, RNG fronteira 0,10, replay/lease e saturação em `Assets/QuackQuack/Tests/EditMode/RewardPolicyTests.cs` e `Assets/QuackQuack/Tests/EditMode/RewardLedgerTests.cs`; 10% não é quota, miniboss é garantido e chefe não herda cura (FR-010–012; T023). **Entrega de testes**: 39 casos novos vermelhos por ausência de Heal/RewardLedger, suíte com 584 aprovados e 39 falhas esperadas, zero ignorados; ver `test-report-t024.md`. Etapa vermelha histórica; implementação nas T025/T027.
- [x] T025 [US4] Implementar Heal sem reanimação em `Assets/QuackQuack/Core/Run/PlayerState.cs` e regras de emissão em `Assets/QuackQuack/Core/Progression/RewardLedger.cs`: applied=min(falta,MaxHealth×fração), claim só se positivo, ID por partida/origem/lease/tipo, sorteio dedicado uma vez por morte comum (FR-010/012; T024). **Validado**: núcleo puro com identidade, emissão, estados lógicos e claim positivo; 636/636 EditMode (39 casos T024 agora verdes + 13 adicionais) e 462/462 PlayMode. Ver `test-report-t025.md`. Integração com pools/RunDirector, dimensionamento e gameplay permanecem em T027–T032; Dados autorais na T026.
- [x] T026 [US4] Criar `Assets/QuackQuack/Content/Pickups/PickupPolicyDefinition.cs` e integrar `Assets/QuackQuack/Content/Encounters/LakeRunSettings.cs`, `Assets/QuackQuack/Editor/ContentValidationWindow.cs` e gerador `Assets/QuackQuack/Editor/PickupPolicySetup.cs`; gerar assets via Unity com chance 0,10 em [0,1], frações 0,15/0,50 em (0,1], captura 3×raio físico e velocidade 1,25×água; exigir XP positivo por inimigo e autorar campo positivo de XP do chefe em revisão de dados, sem inventar número no controlador (FR-010–012; T025). **Validado**: política/asset gerados via Unity, referência no settings e BossExperience=50 editável (patamar dos minibosses); validação exige XP positivo. Geração repetida sem alterar bytes/GUIDs, 66 assets sem erros (dois avisos URP existentes), 677/677 EditMode e 462/462 PlayMode. Ver `test-report-t026.md`; emissão/pools/atração permanecem em T027–T032.
- [x] T027 [US4] Integrar ledger lógico e pendências com pools em `Assets/QuackQuack/Core/Progression/RewardLedger.cs` e `Assets/QuackQuack/Systems/Run/RunDirector.cs`; remover perda por SuppressedPickups, preservar posição/valor/ID sem repetir RNG, priorizar cura de miniboss e demonstrar capacidade para partida autoral inteira antes de aceitar (FR-010–012; T026). **Validado**: XP ligado ao ledger/pool, sem supressão; filas e adaptador priorizam cura de miniboss (view real na T028). Limite autoral conservador 3.324 derrotas/6.648 slots; 6.647 obrigações sem coleta e zero bytes no trecho de emissão medido no Editor. 680/680 EditMode e 467/467 PlayMode; ver `test-report-t027.md`. Não aprova FPS/hardware nem antecipa T028–T032.
- [x] T028 [US4] Criar view de cura em `Assets/QuackQuack/Gameplay/Pickups/HealingPickup.cs` e adaptar `Assets/QuackQuack/Gameplay/Pickups/ExperiencePickup.cs`, com claim único, ciclo Due/WaitingForView/Grounded/Attracted/Claimed/Released e reset completo; gerar prefabs via `Assets/QuackQuack/Editor/PickupPolicySetup.cs` sem Instantiate/Destroy recorrente (FR-010–012; T027). **Validado**: cura por contato ligada ao pool real, claim único e reset/lease/alvo para XP/cura; 16 views de cura com duas vagas reservadas para minibosses. Prefabs/settings gerados via Unity com GUIDs estáveis, 66 assets sem erros, 685/685 EditMode e 477/477 PlayMode. Ver `test-report-t028.md`; atração permanece na T029 e aceite jogável completo em T031/T032.
- [x] T029 [US4] Implementar atração compartilhada em `Assets/QuackQuack/Core/Progression/AttractionState.cs` e `Assets/QuackQuack/Systems/Pickups/PickupAttractionSystem.cs`, com captura <=3×raio físico, velocidade 1,25×água efetiva, persistência após afastamento, obstáculo ignorado só para pickup, pausa suspensa e cura cheia no chão preservada, mas descartada se já capturada e a vida ficar cheia; criar `Assets/QuackQuack/Tests/PlayMode/PickupAttractionTests.cs` portável aos players (FR-011; T028). **Validado**: driver central integrado à partida, captura física inclusiva, velocidade de água efetiva e perseguição persistente. Revisão explícita do usuário: cura capturada com vida cheia termina em Discarded, sem cura/reenfileiramento. 685/685 EditMode, 492/492 PlayMode e 15/15 em cada test player IL2CPP Windows/S23; ver `test-report-t029.md`. T030 descrita abaixo; aceite jogável completo permanece T031/T032.
- [x] T030 [US4] Finalizar emissão comum/miniboss/chefe e teardown em `Assets/QuackQuack/Systems/Run/RunDirector.cs`, resolvendo origem pelo catálogo e sem XP/cura do chefe, mantendo moedas, vitória e registros uma vez; despawn não recompensa e itens de chão não são concedidos automaticamente no fim (FR-010–012; T029). **Validado**: chefe sem XP/cura conforme revisão explícita do usuário; moedas/vitória/registros únicos, valores do catálogo, fechamento auditável sem coleta automática. 686/686 EditMode e 499/499 PlayMode; reteste focado 35/35. Ver `test-report-t030.md`.
- [x] T031 [US4] Testar integração de mortes duplicadas, vida cheia/morta/maxHealth modificado, pool saturado, várias subidas, pausa/fim e reuso em `Assets/QuackQuack/Tests/PlayMode/PickupRewardFlowTests.cs`; registrar posições/IDs/pendências e nenhuma perda/duplicação (FR-010–012; T030). **Validado**: 34 casos novos passando por mortes reais dos sete inimigos, RNG/replay, máximos 100/200, conservação/drenagem de 294 obrigações, quatro melhorias por pickup, pausa, três finais e reuso. 533/533 PlayMode, zero falhas/ignorados. Ver `test-report-t031.md`; integração no Editor, sem antecipar o aceite jogável T032.
- [x] T032 [US4] Validar no jogo chance comum, garantia de XP/miniboss, atração rápida/persistente e cura não desperdiçada; registrar frequência observada sem exigir exatamente um drop por dez em `specs/002-polimento-lago/validation-us4.md` (FR-010–012/018; T031). **Concluída em 06/11/2026**: roteiros funcionais Windows e S23 Ultra/Android 17 por toque aprovados pelo usuário, incluindo partida normal, dois minibosses, cura comum 20→35/18 XP, cura de miniboss 20→70/50 XP, vida cheia, perseguição, pausa, descarte e reinício. Builds normais/laboratório IL2CPP; amostras seed 42 registraram 100 XP/8 curas em 100 mortes em cada plataforma, sem quota. Captura final Android sem exceções. Achados de balanceamento permanecem em incremento específico: reduzir chance comum (novo percentual a definir) e dobrar Sabão para 32%/nível, teto 160%; valores atuais não alterados. T014 continua pendente e T033 não iniciada.

## Phase 7: US5 — HUD, progressão e ameaças (P2)

**Goal**: informação correta e legível sem excesso de texto. **Independent Test**: quatro proporções, níveis múltiplos, recursos, dicas pausadas e especiais próximos.

- [x] T033 [P] [US5] Criar casos de progresso/último limiar, recursos e janela de dicas em `Assets/QuackQuack/Tests/EditMode/ExperienceStateTests.cs` e `Assets/QuackQuack/Tests/PlayMode/GameplayHudTests.cs`; exigir barra cheia no último limiar e exatamente 10 segundos de simulação sem reset por troca de entrada (FR-003–005; T032). **Entrega de testes**: 32 casos novos e um legado atualizado para FR-004. Contratos de progresso/último limiar, capacidades efetivas, produtor/renderização e dicas por relógio de simulação. 686 aprovados/12 falhas esperadas EditMode; suíte PlayMode com 538 aprovados/13 falhas esperadas antes dos dois casos finais 200/140; reteste final da classe HUD com 8 aprovados/14 falhas esperadas. Zero ignorados ou falhas fora da task. Ver `test-report-t033.md`. Fase vermelha intencional; implementação permanece T034–T036 e aceite T040.
- [x] T034 [US5] Expor progresso por limiares sem duplicar curva em `Assets/QuackQuack/Core/Progression/ExperienceState.cs` e `Assets/QuackQuack/Core/Shared/ProgressSnapshot.cs`; integrar produtor `Assets/QuackQuack/Systems/Upgrades/UpgradeSystem.cs` e recursos em `Assets/QuackQuack/Core/Shared/HudSnapshot.cs` (FR-003/004; T033). **Validado**: snapshot readonly por valor, leitura O(1) da curva existente, último limiar cheio, default seguro e recursos normalizados por capacidade efetiva. 710/710 EditMode; PlayMode com 546 aprovados e sete falhas esperadas exclusivamente em T035/T036, zero ignorados. Metas geradas via Unity e 12 testes adicionais de defaults/transporte/normalização/alocação. Ver `test-report-t034.md`. PlayerController/renderização permanecem T035; nenhuma mudança de balanceamento.
- [ ] T035 [US5] Atualizar `Assets/QuackQuack/Presentation/UI/GameplayHud.cs` e `Assets/QuackQuack/Gameplay/Player/PlayerController.cs` para duas barras com ícones/estados sem números/terreno/dicas permanentes, prontidão/aprimoramento além da cor e XP/nível inferior central; preservar avisos de pausa/fim separados (FR-003/004; T034).
- [ ] T036 [US5] Implementar `Assets/QuackQuack/Presentation/UI/ContextualHintsView.cs` ligada a 10 segundos de simulação no início, pausa suspende, família muda símbolos sem reset e sem reapresentação posterior (FR-005; T035).
- [ ] T037 [US5] Implementar registro/snapshot e view em `Assets/QuackQuack/Presentation/UI/SpecialEnemyBarsView.cs`, conectando `Assets/QuackQuack/Core/Combat/EnemyState.cs`, `Assets/QuackQuack/Gameplay/Bosses/FrogKingController.cs` e `Assets/QuackQuack/Systems/Run/RunDirector.cs`; barra individual acima por lease, remover morto/reuso, resolver rótulos sobrepostos sem painel agregado; testar em `Assets/QuackQuack/Tests/PlayMode/SpecialEnemyBarsTests.cs` portável (FR-012; T036).
- [ ] T038 [US5] Atualizar `Assets/QuackQuack/Editor/GameplayHudSetup.cs`, `Assets/QuackQuack/Editor/IntegratedUiSetup.cs` e `Assets/QuackQuack/Editor/PolishUiSetup.cs` criado em T013 para gerar cenas/prefabs da composição via Unity, preservar .meta e referências, integrar preview/perfil/dicas/XP/barras (FR-003–005/012/017; T037).
- [ ] T039 [US5] Ampliar `Assets/QuackQuack/Tests/PlayMode/IntegratedUiLayoutTests.cs` para 16:9/18:9/19,5:9/4:3, ambos insets, estados do recurso, perfil/menus, múltiplos especiais e touch condicional, sem alterar viewport só para fazer teste passar (FR-017; T038).
- [ ] T040 [US5] Executar revisão visual/jogável de XP/nível, duas barras, sinais Quack, dicas e barras de especiais nas telas/alvos suportados; registrar pendências e capturas em `specs/002-polimento-lago/validation-us5.md` (FR-003–005/012/017/018; T039).

## Phase 8: US6 — Identidade visual e cenário (P3)

**Goal**: cartoon consistente com elenco real. **Independent Test**: cena-amostra e identificação em gameplay sem explicação verbal; formas/alcances reais.

- [ ] T041 [US6] Inventariar todos os assets por ID canônico, função, fonte/licença, dimensões/pivôs/importação, animações, contraste e prioridade em `specs/002-polimento-lago/art-manifest.md`; preservar cinco comuns/dois minibosses/chefe, seis armas/três evoluções e dez relíquias, sem importar espécies da referência (FR-001/002/006; T040).
- [ ] T042 [US6] Criar cena-amostra de referência cartoon em `Assets/QuackQuack/Art/LakeCartoon/` e gerador `Assets/QuackQuack/Editor/PolishArtSetup.cs`, com Pato, ameaça aprovada, obstáculo, efeito, recompensa e HUD; obter aceite artístico registrado em `specs/002-polimento-lago/art-manifest.md` antes de produzir lote (FR-001/002/006; T041).
- [ ] T043 [US6] Produzir/integrar Pato, cinco comuns, dois minibosses, Sapo-Rei e obstáculos em `Assets/QuackQuack/Art/LakeCartoon/Characters/` e `Assets/QuackQuack/Art/LakeCartoon/Objects/`, regenerando prefabs pelo `Assets/QuackQuack/Editor/PolishArtSetup.cs`; manter geometria/regras já validadas e listar estados/animações no manifesto (FR-001/002; T042 aprovado).
- [ ] T044 [US6] Produzir/integrar seis armas/três evoluções, projéteis, recompensas e VFX em `Assets/QuackQuack/Art/LakeCartoon/Combat/` e `Assets/QuackQuack/Presentation/Weapons/WeaponVisuals.cs`; diferenciar além da cor e usar raio real da aura/Bolhas, respeitando pooling/pausa (FR-001/006–008; T043).
- [ ] T045 [US6] Aplicar skin temática a barras, menus, relíquias, cartas, perfil e controles em `Assets/QuackQuack/Art/LakeCartoon/UI/` e `Assets/QuackQuack/Editor/PolishUiSetup.cs`; manter duas barras sem números, foco visível, touch condicional e cenas/prefabs gerados via Unity (FR-001/004/014/017; T044).
- [ ] T046 [US6] Integrar margens orgânicas, vegetação, pedras e água animada em `Assets/QuackQuack/Art/LakeCartoon/Environment/`, `Assets/QuackQuack/Presentation/Terrain/LakeWaterAnimation.cs` e `Assets/QuackQuack/Editor/PolishArtSetup.cs`; decoração distinta de bloqueio, animação suave sem esconder terreno/limites/perigos (FR-002; T045).
- [ ] T047 [US6] Revisar todos os estados/assets contra manifesto, raios reais e quatro proporções, avaliar identificação sem explicação e registrar aceite artístico/jogável separado em `specs/002-polimento-lago/validation-us6.md` (FR-001/002/006/017/018; T046).

## Phase 9: Polish e validação transversal

- [ ] T048 Executar suítes completas EditMode/PlayMode, validador de conteúdo estendido e testes portáveis nos players Windows/Android IL2CPP; construir/iniciar produto real via `Assets/QuackQuack/Editor/PrototypeBuildPipeline.cs` e registrar commit/descoberta por classe/sem ignorados em `specs/002-polimento-lago/validation-report.md` (FR-018; T047).
- [ ] T049 Medir antes/depois 200/400 inimigos e partida 15 minutos com drops/atração/água/aura nos dispositivos exigidos usando `Assets/QuackQuack/Systems/Diagnostics/PerformanceBenchmark.cs` e `Assets/QuackQuack/Systems/Diagnostics/PerformanceTelemetry.cs`; registrar CPU/GPU/FPS/p95/memória/GC/capacidade/pendências/latência em `specs/002-polimento-lago/performance-report.md`, corrigindo/retestando regressões sem suprimir recompensa (FR-018; T048).
- [ ] T050 Executar matriz física Xbox/PS/genérico/teclado/toque, hotplug em todos os contextos, quatro proporções e duas paisagens reais; registrar por modelo e pendência, sem extrapolar S23 para intermediário/entrada, em `specs/002-polimento-lago/validation-report.md` (FR-013–015/017/018; T049).
- [ ] T051 Realizar nova rodada de gameplay com pelo menos cinco participantes, cobertura de cada arma/evolução e identificação de Pato/cura/XP/obstáculos/ameaças sem explicação; registrar todos os critérios 1–15, SC-001–009, dificuldades/retestes e percepção de Bolhas em `specs/002-polimento-lago/playtest-report.md`, usando `specs/001-prototipo-lago/playtest-form.md` como base (FR-018; T050).
- [ ] T052 Consolidar rastreabilidade MEL→FR→task→evidência, aceites e pendências em `specs/002-polimento-lago/validation-report.md` e atualizar `README.md`/`specs/002-polimento-lago/quickstart.md` com comandos/hardware/resultados reais, sem encerrar T075 antiga por inferência (FR-018; T051).
- [ ] T053 Reconciliar entrega com especificação, plano, tasks, manifesto e constituição em `specs/002-polimento-lago/convergence-report.md`; registrar lacunas e aprovação de escopo sem implementar tarefa extra automaticamente, preservando exclusões e .meta (FR-001–018; T052).

## Dependencies & Execution Order

Fluxo padrão: T001 → T002 → US1 T003–007 → US2 T008–014 → US3 T015–023 → US4 T024–032 → US5 T033–040 → US6 T041–047 → T048–053.

Dependências técnicas centrais: US4 usa atributos efetivos de US3 para velocidade de atração; US5 usa progressão/recompensas e contexto de US2/US4; arte final depende dos alcances reais e da composição aceita. Não há dependência de regra futura de arte para validar histórias anteriores: usar apresentação temporária legível. Cada história tem seu aceite próprio e é demonstrável com predecessoras, sem exigir concluir a feature inteira.

## Parallel Opportunities

[P] indica possibilidade somente após predecessoras. Não autoriza executar fora do intervalo solicitado nem usar vários agentes automaticamente.

- US1: reprodução/diagnóstico e regressão são sequenciais; não paralelizar escolha da solução com investigação.
- US2: T008 e T009 podem rodar juntas após T007, pois escrevem arquivos de teste distintos.
- US3: T015 (regra de atributos) e T020 (teste de bumerangue) podem rodar juntas após T014; T020 não muda runner/projétil antes de T021.
- US4: T024 é teste puro em arquivos próprios, pode ser preparado quando US3 estiver aceita; implementação T025–031 permanece em ordem.
- US5: T033 é teste em arquivos próprios; etapas que modificam view/geradores são sequenciais.
- US6: lotes de arte seguem a ordem aprovada; fontes independentes dentro do lote podem ser produzidas em paralelo após T042, mantendo integração por um responsável e manifesto atualizado.

## Implementation Strategy

MVP: setup/fundação + US1. Parar após T007 para revisar contenção e pressão antes de ampliar funcionalidades. Depois entregar US2, US3, US4, US5 e US6 em incrementos verificáveis; profiling/revisão proporcionais em cada alteração e rodada transversal ao final.

Teste-first quando a task pede regressão: registrar falha antes da correção, sem alterar regra/tolerância para mascarar falha. Manter .meta e usar Unity para serializados. Não versionar logs/caches/builds/resultados regeneráveis. Commit após task ou grupo lógico autorizado. Aceite não executado permanece pendente.

## Coverage Summary

| Requisito | Tasks |
|---|---|
| FR-001 / MEL-001 | T041–047 |
| FR-002 / MEL-002 | T041–043, T046–047 |
| FR-003 / MEL-003 | T033–035, T038–040 |
| FR-004 / MEL-004 | T033–035, T038–040, T045 |
| FR-005 / MEL-005 | T033, T036, T038–040 |
| FR-006 / MEL-006 | T022–023, T041–042, T044, T047 |
| FR-007 / MEL-007 | T019, T023, T044 |
| FR-008 / MEL-008 | T015–019, T022–023, T044 |
| FR-009 / MEL-009 | T020–021, T023 |
| FR-010 / MEL-010 | T024–028, T030–032 |
| FR-011 / MEL-011 | T024–032 |
| FR-012 / MEL-012 | T024–028, T030–032, T037–040 |
| FR-013 / MEL-013 | T008, T010–014, T050 |
| FR-014 / MEL-014 | T009–014, T045, T050 |
| FR-015 / MEL-015 | T009–014, T050 |
| FR-016 / MEL-016 | T002–007 |
| FR-017 / aceite 14 | T038–040, T045–047, T050 |
| FR-018 / aceite 15 | T001, aceites por história, T048–053 |


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](decisions.md) e o relatório T030.
