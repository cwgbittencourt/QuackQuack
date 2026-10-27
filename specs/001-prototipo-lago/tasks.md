# Tasks: Prototipo Jogavel do Lago

**Input**: Design documents from `/specs/001-prototipo-lago/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Obrigatorios pela constituicao para regras puras, integracao Unity e validacao jogavel.

**Organization**: Tarefas agrupadas por historia para permitir implementacao e teste incremental.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode executar em paralelo porque usa arquivos distintos e nao depende de tarefa incompleta.
- **[Story]**: Historia atendida (`US1`, `US2`, `US3`, `US4`).
- Cada tarefa indica o caminho exato do principal arquivo alterado ou criado.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicializar o projeto Unity e a estrutura compartilhada.

- [X] T001 Criar o projeto Unity 6000.4.3f1 2D URP na raiz e registrar a versao fixada em `ProjectSettings/ProjectVersion.txt`
- [X] T002 Configurar URP 2D, Input System, Test Framework e Memory Profiler em `Packages/manifest.json`
- [X] T003 [P] Configurar paisagem, Windows x64, Android ARM64, IL2CPP e referencia 1920x1080 em `ProjectSettings/ProjectSettings.asset`
- [X] T004 [P] Criar regras de versionamento Unity e exclusoes de build/cache em `.gitignore`
- [X] T005 Criar os quatro assemblies nos caminhos `Assets/QuackQuack/QuackQuack.Runtime.asmdef`, `Assets/QuackQuack/Core/QuackQuack.Core.asmdef`, `Assets/QuackQuack/Platform/QuackQuack.Platform.asmdef` e `Assets/QuackQuack/Presentation/QuackQuack.Presentation.asmdef`
- [X] T006 [P] Definir assembly de testes EditMode em `Assets/QuackQuack/Tests/EditMode/QuackQuack.EditModeTests.asmdef`
- [X] T007 [P] Definir assembly de testes PlayMode em `Assets/QuackQuack/Tests/PlayMode/QuackQuack.PlayModeTests.asmdef`

**Checkpoint**: Projeto abre sem erros, pacotes resolvem e Test Runner reconhece os dois assemblies.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Contratos e infraestrutura que bloqueiam todas as historias.

**CRITICAL**: Nenhuma historia inicia antes desta fase.

- [X] T008 [P] Implementar `PlayerCommand` com `move` normalizado de 0 a 1 e flags de borda para Quack/pausa em `Assets/QuackQuack/Core/Shared/PlayerCommand.cs`
- [X] T009 [P] Implementar contratos `CombatEvent` e `PresentationEvent` com tick, correlacao e prioridade em `Assets/QuackQuack/Core/Combat/CombatEvent.cs`
- [X] T010 [P] Implementar estados e transicoes `Preparing`, `Playing`, `Paused`, `ChoosingUpgrade`, `Boss`, `Won`, `Lost`, `Abandoned` e `Results` em `Assets/QuackQuack/Core/Run/RunState.cs`
- [X] T011 Criar testes EditMode que rejeitem transicoes invalidas e garantam estados terminais idempotentes em `Assets/QuackQuack/Tests/EditMode/RunStateTests.cs`
- [X] T012 [P] Criar definicoes base imutaveis em runtime e com IDs estaveis em `Assets/QuackQuack/Content/Shared/ContentDefinition.cs`
- [X] T013 [P] Implementar barramento de eventos em que consumidores nao reapliquem regras em `Assets/QuackQuack/Systems/Combat/GameplayEventBus.cs`
- [X] T014 [P] Implementar `PoolService` com estado ativo/inativo exclusivo e reset obrigatorio em `Assets/QuackQuack/Systems/Pooling/PoolService.cs`
- [X] T015 Criar testes PlayMode para inimigos, projeteis, pickups e VFX, impedindo instancia simultaneamente ativa/no pool e validando `ResetState` no retorno em `Assets/QuackQuack/Tests/PlayMode/PoolServiceTests.cs`
- [X] T016 [P] Criar cena de inicializacao e composicao dos servicos compartilhados em `Assets/QuackQuack/Scenes/Bootstrap.unity`
- [X] T017 Integrar carregamento da cena do Lago e inicio deterministico por seed em `Assets/QuackQuack/Systems/Run/GameBootstrap.cs`

**Checkpoint**: Estados, eventos, pooling e bootstrap funcionam sem conteudo especifico de historia.

---

## Phase 3: User Story 1 - Sobreviver usando movimento e Quack (Priority: P1) MVP

**Goal**: Mover o Pato entre agua e terra e usar Folego/Quack para controlar inimigos e projeteis.

**Independent Test**: Em uma arena curta, validar movimento, terreno, consumo/recuperacao de Folego,
empurrao, interrupcao, reflexao e Quack aprimorado sem builds ou chefes.

### Tests for User Story 1

- [X] T018 [P] [US1] Criar testes EditMode para Folego entre zero/capacidade, recuperacao por terreno e uma ativacao aceita mesmo ao zerar em `Assets/QuackQuack/Tests/EditMode/BreathStateTests.cs`
- [X] T019 [P] [US1] Criar testes EditMode para dano, critico, empurrao, interrupcao, reflexao leve e colisao aplicada uma unica vez por onda/alvo em `Assets/QuackQuack/Tests/EditMode/QuackRulesTests.cs`
- [X] T020 [P] [US1] Criar testes PlayMode para transicao sem oscilacao, afinidade de equipamento por terreno, recompensa em area de risco e obstaculos apos empurrao em `Assets/QuackQuack/Tests/PlayMode/TerrainAndKnockbackTests.cs`
- [X] T021 [P] [US1] Criar testes PlayMode para movimento e Quack equivalentes via teclado em `Assets/QuackQuack/Tests/PlayMode/KeyboardPlayerInputTests.cs`

### Implementation for User Story 1

- [x] T022 [P] [US1] Implementar `PlayerState` garantindo Folego entre zero e capacidade, dano com invulnerabilidade e terreno unico por instante em `Assets/QuackQuack/Core/Run/PlayerState.cs`
- [x] T023 [P] [US1] Implementar `CharacterDefinition` com ID unico, vida/velocidades positivas e Pato como unica selecao em `Assets/QuackQuack/Content/Characters/CharacterDefinition.cs`
- [x] T024 [P] [US1] Implementar `QuackDefinition` com custo valido, empurrao, interrupcao, reflexao e multiplicador de carga cheia em `Assets/QuackQuack/Content/Characters/QuackDefinition.cs`
- [x] T025 [US1] Implementar regras deterministicas de consumo, recuperacao e estado cheio do Folego em `Assets/QuackQuack/Core/Combat/BreathSystem.cs`
- [x] T026 [US1] Implementar resolucao deterministica do Quack e emissao unica de eventos por alvo em `Assets/QuackQuack/Core/Combat/QuackSystem.cs`
- [x] T027 [P] [US1] Implementar adaptador de teclado para `PlayerCommand` sem regras de gameplay em `Assets/QuackQuack/Platform/Input/KeyboardInputAdapter.cs`
- [x] T028 [US1] Implementar movimento, colisao e aplicacao de comandos comuns no Pato em `Assets/QuackQuack/Gameplay/Player/PlayerController.cs`
- [x] T029 [US1] Implementar volumes de agua/terra com histerese e `TerrainAffinity` para movimento, Folego e equipamentos aplicaveis em `Assets/QuackQuack/Gameplay/Terrain/TerrainZone.cs`
- [x] T030 [US1] Montar arena curta com Pato, agua, terra, obstaculos, inimigo-alvo, projetil leve e recompensa em rota de risco em `Assets/QuackQuack/Scenes/Lake.unity`
- [x] T031 [US1] Implementar HUD de vida, Folego, terreno atual, estado pronto e rejeicao do Quack em `Assets/QuackQuack/Presentation/UI/GameplayHud.cs`
- [x] T032 [US1] Integrar via pool ondas visiveis, impacto, reflexao, contorno branco de critico e audio temporario aos eventos do Quack em `Assets/QuackQuack/Presentation/VFX/QuackFeedbackController.cs`

**Checkpoint**: US1 jogavel e testavel como MVP isolado.

---

## Phase 4: User Story 2 - Formar uma build durante a partida (Priority: P2)

**Goal**: Coletar XP, escolher cartas e combinar seis armas com seis passivas em tres evolucoes.

**Independent Test**: Em modo acelerado, conceder niveis e validar ofertas, limites de escolha,
cinco niveis por arma e as tres evolucoes sem minibosses ou chefe.

### Tests for User Story 2

- [x] T033 [P] [US2] Criar testes EditMode para seis slots de arma/passiva, duas rerrolagens, um banimento e evolucao sem exceder slots em `Assets/QuackQuack/Tests/EditMode/BuildStateTests.cs`
- [x] T034 [P] [US2] Criar testes EditMode para ofertas de uma a tres escolhas elegiveis, sem duplicatas e idempotentes por `offerId` em `Assets/QuackQuack/Tests/EditMode/UpgradeOfferTests.cs`
- [x] T035 [P] [US2] Criar testes PlayMode para coleta de XP, pausa de combate, selecao e retorno sem duplicar eventos em `Assets/QuackQuack/Tests/PlayMode/UpgradeFlowTests.cs`

### Implementation for User Story 2

- [x] T036 [P] [US2] Implementar `WeaponDefinition` com exatamente cinco niveis, passiva compativel, evolucao valida e modificadores opcionais de afinidade por terreno em `Assets/QuackQuack/Content/Weapons/WeaponDefinition.cs`
- [x] T037 [P] [US2] Implementar `PassiveDefinition` com modificadores limitados e referencia de arma compativel em `Assets/QuackQuack/Content/Passives/PassiveDefinition.cs`
- [x] T038 [P] [US2] Implementar `EvolutionDefinition` com combinacao unica, arma nivel cinco e referencias existentes em `Assets/QuackQuack/Content/Weapons/EvolutionDefinition.cs`
- [x] T039 [US2] Implementar `BuildState` sem duplicatas invalidas e com limites de slots/recursos em `Assets/QuackQuack/Core/Progression/BuildState.cs`
- [x] T040 [US2] Implementar geracao, rerrolagem, banimento, pulo e confirmacao idempotente de ofertas em `Assets/QuackQuack/Systems/Upgrades/UpgradeSystem.cs`
- [x] T041 [P] [US2] Implementar coleta de XP exclusivamente via `PoolService` e progressao de nivel em `Assets/QuackQuack/Gameplay/Pickups/ExperiencePickup.cs`
- [x] T042 [US2] Implementar os seis comportamentos de arma por composicao e obter/devolver todo projetil exclusivamente pelo `PoolService` em `Assets/QuackQuack/Gameplay/Weapons/WeaponRunner.cs`
- [x] T043 [US2] Criar catalogo de seis armas, seis passivas e tres evolucoes iniciais em `Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset`
- [x] T044 [US2] Implementar tela de cartas com tipo, efeito atual/proximo, sinergia, requisito e recursos restantes em `Assets/QuackQuack/Presentation/UI/UpgradeSelectionView.cs`
- [x] T045 [US2] Integrar via `PoolService` o feedback de evolucao, VFX e recompensa rara em `Assets/QuackQuack/Presentation/VFX/UpgradeFeedbackController.cs`

**Checkpoint**: US2 funciona em uma arena acelerada sem depender do ciclo completo da partida.

---

## Phase 5: User Story 3 - Concluir a jornada de 15 minutos (Priority: P3)

**Goal**: Entregar ondas crescentes, cinco inimigos, dois minibosses, Sapo-Rei e resultado persistido.

**Independent Test**: Executar partida completa ou acelerada e validar cronograma, chefe, tres finais e
todos os campos da tela de resultado.

### Tests for User Story 3

- [x] T046 [P] [US3] Criar testes EditMode para cronograma, dois minibosses antes de 15 minutos e Sapo-Rei solicitado uma vez em `Assets/QuackQuack/Tests/EditMode/EncounterScheduleTests.cs`
- [x] T047 [P] [US3] Criar testes EditMode para resultado unico, abandono parcial e vitoria nunca registrada no abandono em `Assets/QuackQuack/Tests/EditMode/RunResultTests.cs`
- [x] T048 [P] [US3] Criar testes EditMode para desbloquear somente IDs conhecidos, equipar zero a tres reliquias, aplicar/remover efeitos sem duplicacao e recuperar SaveData invalido em `Assets/QuackQuack/Tests/EditMode/SaveServiceTests.cs`
- [x] T049 [P] [US3] Criar testes PlayMode para sinais visuais/sonoros anteriores aos ataques do Sapo-Rei em `Assets/QuackQuack/Tests/PlayMode/FrogKingTelegraphTests.cs`

### Implementation for User Story 3

- [x] T050 [P] [US3] Implementar definicoes de Formiga, Aranha, Vespa, Besouro e Caramujo Aquatico com funcoes taticas, pool e telemetria em `Assets/QuackQuack/Content/Enemies/EnemyDefinition.cs`
- [x] T051 [P] [US3] Implementar encontros de Libelula Alfa no minuto 5, Sapo-Cururu no minuto 10 e Sapo-Rei no minuto 15 sem lacunas de progressao em `Assets/QuackQuack/Content/Encounters/EncounterDefinition.cs`
- [x] T052 [P] [US3] Implementar dez `RelicDefinition`, `RelicLoadout` limitado a tres, desbloqueio, aplicacao idempotente e tela de equipar/desequipar em `Assets/QuackQuack/Content/Relics/RelicDefinition.cs`, `Assets/QuackQuack/Core/Progression/RelicLoadout.cs` e `Assets/QuackQuack/Presentation/UI/RelicLoadoutView.cs`
- [ ] T053 [US3] Implementar `EnemyState` com recompensa unica e ciclo de aquisicao/limpeza exclusivamente pelo `PoolService` em `Assets/QuackQuack/Core/Combat/EnemyState.cs`
- [ ] T054 [US3] Implementar `SpawnDirector` com limite ativo, area de exclusao, expiracao e reducao de densidade sem criar inimigos fora do pool em `Assets/QuackQuack/Systems/Spawning/SpawnDirector.cs`
- [ ] T055 [US3] Implementar comportamentos taticos e atualizacao agrupada sem `Update` individual por inimigo em `Assets/QuackQuack/Gameplay/Enemies/EnemyUpdateScheduler.cs`
- [ ] T056 [US3] Criar catalogo de Formiga, Aranha, Vespa, Besouro, Caramujo Aquatico, Libelula Alfa e Sapo-Cururu com cronograma fechado em `Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset`
- [ ] T057 [US3] Implementar fases do Sapo-Rei com salto marcado, ondas, girinos e destruicao de areas seguras em `Assets/QuackQuack/Gameplay/Bosses/FrogKingController.cs`
- [ ] T058 [US3] Implementar `RunResult` com tempo, chefe, derrotas, dano por arma, sequencia, build, sinergias, moedas, itens e desafios em `Assets/QuackQuack/Core/Run/RunResult.cs`
- [ ] T059 [US3] Implementar JSON versionado para desbloqueios/loadout de reliquias, escrita atomica, backup e valores padrao em `Assets/QuackQuack/Systems/Save/JsonSaveService.cs`
- [ ] T060 [US3] Implementar orquestracao de 15 minutos e encerramento por vitoria, morte ou abandono em `Assets/QuackQuack/Systems/Run/RunDirector.cs`
- [ ] T061 [US3] Implementar tela final completa e progresso de recordes/desafios em `Assets/QuackQuack/Presentation/UI/ResultsView.cs`

**Checkpoint**: US3 fecha o loop completo e preserva resultado/progresso local.

---

## Phase 6: User Story 4 - Jogar em computador ou celular (Priority: P4)

**Goal**: Garantir comandos e regras equivalentes em teclado, controle e toque, com UI responsiva.

**Independent Test**: Repetir o mesmo cenario com tres entradas e quatro proporcoes de tela, sem
diferenca de estado final e sem informacao essencial fora da Safe Area.

### Tests for User Story 4

- [ ] T062 [P] [US4] Criar testes PlayMode comparando sequencias equivalentes de teclado, controle e toque em `Assets/QuackQuack/Tests/PlayMode/InputParityTests.cs`
- [ ] T063 [P] [US4] Criar testes PlayMode para 16:9, 18:9, 19,5:9 e 4:3 com elementos essenciais dentro da Safe Area em `Assets/QuackQuack/Tests/PlayMode/ResponsiveUiTests.cs`
- [ ] T064 [P] [US4] Criar smoke test de build e inicializacao Windows/Android em `Assets/QuackQuack/Tests/PlayMode/PlatformBuildSmokeTests.cs`

### Implementation for User Story 4

- [ ] T065 [P] [US4] Implementar adaptador de controle para `PlayerCommand` sem regras especificas de dispositivo em `Assets/QuackQuack/Platform/Input/GamepadInputAdapter.cs`
- [ ] T066 [P] [US4] Implementar joystick e botoes de Quack/pausa para `PlayerCommand` em `Assets/QuackQuack/Platform/Input/MobileInputAdapter.cs`
- [ ] T067 [US4] Criar mapa de acoes unificado Move/Quack/Pause para teclado, controle e toque em `Assets/QuackQuack/Platform/Input/QuackQuackInput.inputactions`
- [ ] T068 [US4] Implementar controlador de Safe Area e atualizacao por orientacao/recorte em `Assets/QuackQuack/Presentation/UI/SafeAreaController.cs`
- [ ] T069 [US4] Configurar Canvas Scaler 1920x1080 e ancoras responsivas no HUD em `Assets/QuackQuack/Scenes/Lake.unity`
- [ ] T070 [US4] Criar configuracoes de build compartilhadas para Windows x64 e Android ARM64 em `Assets/QuackQuack/Editor/PrototypeBuildPipeline.cs`

**Checkpoint**: US4 comprova paridade de regras e legibilidade nas plataformas-alvo.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Desempenho, validacao integral e evidencias de aceite.

- [ ] T071 [P] Criar cenario deterministico de benchmark com 200/400 inimigos e saturacao de projeteis, pickups e VFX pooled em `Assets/QuackQuack/Scenes/PerformanceBenchmark.unity`
- [ ] T072 Instrumentar frame time, scheduler de hordas, pools por familia, GC e memoria sem alocacao recorrente em `Assets/QuackQuack/Systems/Diagnostics/PerformanceTelemetry.cs`
- [ ] T073 [P] Criar validador Editor para IDs unicos, referencias e limites dos ScriptableObjects em `Assets/QuackQuack/Editor/ContentValidationWindow.cs`
- [ ] T074 Comparar o perfil com as metas; se houver violacao, corrigir e reperfilar, senao registrar aprovacao sem mudanca, sempre sem DOTS/ECS/Jobs/Burst em `Assets/QuackQuack/Systems/Spawning/SpawnDirector.cs`
- [ ] T075 Executar toda a matriz de validacao e registrar resultados Windows/Android em `specs/001-prototipo-lago/validation-report.md`
- [ ] T076 [P] Preparar formulario de playtest para SC-001 a SC-005 e captura de seeds/builds em `specs/001-prototipo-lago/playtest-form.md`
- [ ] T077 Atualizar instrucoes executaveis com comandos, aparelhos e resultados observados em `specs/001-prototipo-lago/quickstart.md`
- [ ] T078 Revisar o escopo e remover qualquer loja, anuncio, compra, bioma ou personagem extra em `specs/001-prototipo-lago/scope-audit.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependencias.
- **Foundational (Phase 2)**: depende do Setup e bloqueia todas as historias.
- **US1 (Phase 3)**: depende da Fundacao e entrega o MVP.
- **US2 (Phase 4)**: depende da Fundacao; integra ao Pato para demonstracao, mas possui arena acelerada independente.
- **US3 (Phase 5)**: depende da Fundacao; para aceite completo integra US1 e US2.
- **US4 (Phase 6)**: depende da Fundacao; pode avancar em paralelo, com teste final apos US1.
- **Polish (Phase 7)**: depende das historias desejadas; T075 e T078 dependem das quatro historias.

### User Story Dependencies

```text
Setup -> Foundation -> US1 (MVP)
                    ├-> US2
                    ├-> US3
                    └-> US4

US1 + US2 + US3 + US4 -> Polish/Acceptance
```

- **US1**: independente apos Fundacao.
- **US2**: independente por modo acelerado; usa contratos de estado/eventos da Fundacao.
- **US3**: cronograma, chefe e resultado sao isoladamente testaveis; a partida final integra US1/US2.
- **US4**: adaptadores e UI sao isoladamente testaveis; paridade ponta a ponta usa US1.

### Within Each User Story

- Escrever testes primeiro e confirmar falha antes da implementacao.
- Implementar definicoes/estado antes dos sistemas.
- Implementar sistemas antes de controladores de cena e UI.
- Completar o teste independente antes de integrar a historia seguinte.

### Parallel Opportunities

- T003/T004, T006/T007 podem rodar em paralelo no Setup.
- T008/T009/T010/T012/T013/T014/T016 podem ser divididas na Fundacao.
- Testes marcados [P] podem ser escritos simultaneamente antes da implementacao de sua historia.
- Definicoes de conteudo marcadas [P] podem ser criadas em paralelo.
- Apos a Fundacao, US1, US2, US3 e US4 podem iniciar em paralelo, preservando seus checkpoints.
- T071, T073 e T076 podem avancar em paralelo no Polish.

---

## Parallel Examples

### User Story 1

```text
T018 BreathStateTests.cs
T019 QuackRulesTests.cs
T020 TerrainAndKnockbackTests.cs
T021 KeyboardPlayerInputTests.cs

Depois dos testes falharem:
T023 CharacterDefinition.cs
T024 QuackDefinition.cs
T027 KeyboardInputAdapter.cs
```

### User Story 2

```text
T033 BuildStateTests.cs
T034 UpgradeOfferTests.cs
T035 UpgradeFlowTests.cs

Depois dos testes falharem:
T036 WeaponDefinition.cs
T037 PassiveDefinition.cs
T038 EvolutionDefinition.cs
```

### User Story 3

```text
T046 EncounterScheduleTests.cs
T047 RunResultTests.cs
T048 SaveServiceTests.cs
T049 FrogKingTelegraphTests.cs

Depois dos testes falharem:
T050 EnemyDefinition.cs
T051 EncounterDefinition.cs
T052 RelicDefinition.cs
```

### User Story 4

```text
T062 InputParityTests.cs
T063 ResponsiveUiTests.cs
T064 PlatformBuildSmokeTests.cs

Depois dos testes falharem:
T065 GamepadInputAdapter.cs
T066 MobileInputAdapter.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Concluir Setup.
2. Concluir Foundation.
3. Escrever os testes T018-T021 e confirmar que falham.
4. Implementar T022-T032.
5. Parar e validar a arena curta conforme o teste independente de US1.

### Incremental Delivery

1. Setup + Foundation -> projeto e contratos prontos.
2. US1 -> nucleo de movimento/Quack jogavel.
3. US2 -> variedade de build testavel.
4. US3 -> partida completa de 15 minutos.
5. US4 -> paridade Windows/Android e UI responsiva.
6. Polish -> profiling, playtest e evidencias de aceite.

### Suggested Commits

- `chore: initialize Unity 6000.4.3f1 prototype`
- `feat: add shared runtime foundation`
- `feat: deliver movement terrain and quack MVP`
- `feat: add build progression and evolutions`
- `feat: complete lake run and frog king`
- `feat: add cross-platform input and responsive UI`
- `test: validate performance and prototype acceptance`

## Notes

- Tarefas `[P]` atuam em arquivos distintos e nao dependem de tarefas ainda incompletas.
- Cada tarefa de historia possui `[USn]` para rastreabilidade.
- Nao iniciar DOTS, ECS, Jobs ou Burst sem evidencia registrada no profiling.
- Arte e audio temporarios sao aceitaveis se estados e perigos permanecerem legiveis.
- Fazer commit apos cada tarefa ou grupo logico pequeno.
