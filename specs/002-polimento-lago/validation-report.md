# Validação transversal — Polimento 002

Data: 10/11/2026. Escopo exclusivo: T048 da feature 002, concluída. Suítes completas, conteúdo, players IL2CPP e inicialização dos dois produtos reais aprovados nos limites abaixo.

## Origem e isolamento

- Código de runtime avaliado: `4c29ac81f989cc832f0e808dec152dd0e38cb7a5` (T047), mais a correção do caminho de captura de SpecialEnemyBarsTests incluída neste incremento T048. Nenhuma regra ou asset de produção alterado.
- Unity 6000.4.3f1. Cópia nova em `Builds/T048/ValidationProject`, criada somente com Assets, Packages e ProjectSettings; sem copiar Library ou harness T047.
- Identidade isolada: QuackQuack Validation 002; Android `com.cemggames.quackquack.validation002`. Configuração feita via API do Editor; não retorna ao projeto de produção.
- Windows x64 e Android ARM64 configurados para IL2CPP. `--burst-disable-compilation` usado como argumento do processo Editor, sem alterar assets/configurações de Burst.
- Android conectado: Samsung Galaxy S23 Ultra (SM-S918B), Android 17/API 37. Serial não registrado neste relatório.
- Logs, XML, capturas e produtos regeneráveis permanecem ignorados pelo Git.

## Suítes completas e conteúdo

| Execução | Classes | Aprovados/total | Falhas | Ignorados |
|---|---:|---:|---:|---:|
| EditMode | 43 | 725/725 | 0 | 0 |
| PlayMode | 55 | 613/613 | 0 | 0 |

Reexecução final após a correção de portabilidade: `TestResults/T048/EditMode-r3.xml` e `TestResults/T048/PlayMode-r3.xml`, com os mesmos totais e descoberta por classe das primeiras execuções. Logs correspondentes em `Logs/Polimento002/T048`. Matriz final: 96 capturas de interface e uma de barras especiais em `Logs/Polimento002/T048/Matrix-r3`.

Validador estendido: `QuackQuack.Editor.ContentValidationWindow.ValidateForCI`, marcador `T073_CONTENT_VALIDATION_OK assets=67 warnings=2`, saída 0. Os avisos `UnsupportedType` referem-se a LakeRenderer2D e LakeURP, tipos de infraestrutura URP sem validador de domínio registrado; não são referências ausentes de conteúdo.

## Players portáveis e produtos

A seleção portátil inclui as sete classes do quickstart e oito classes adicionais de regressão visual/combate: 15 classes e 167 casos esperados, conferidos contra o XML PlayMode. Não há marcações Ignore/Explicit no código dos testes.

Windows x64 IL2CPP R3 e Android ARM64 IL2CPP R3: 167/167 aprovados em cada plataforma, 15 classes, zero falhas/ignorados; contagem de cada classe idêntica à seleção no Editor. Evidências: `TestResults/T048/windows-runtime-r3.xml`, `TestResults/T048/android-runtime-r3.xml` e logs correspondentes. Produtos reais construídos e iniciados conforme seção própria. Windows também gerou 97 capturas na pasta `windows-matrix-r3`.

Primeira tentativa Windows: 155 aprovados e 12 falhas, zero ignorados. A configuração isolada havia incluído somente as cenas de produção, mas QuackFeedbackTests e UpgradeFeedbackTests carregam Bootstrap/Lake. Corrigida a lista de cenas do test player para incluir essas duas cenas legadas e repetidos todos os 167 casos, sem modificar assertions nem código de runtime. O quickstart documenta esse requisito. A seleção explícita de cenas do pipeline de produto permanece RunBootstrap/LakeRun.

Primeira tentativa Android (R2): 166 aprovados e uma falha, zero ignorados. CaptureIndividualOverlappingSpecialsForVisualReview tentava gravar em caminho derivado de Application.dataPath, sem permissão no sandbox Android. A fixture agora usa Application.temporaryCachePath nos players, mantém o destino histórico no Editor, aceita QUACKQUACK_UI_CAPTURE_DIR e verifica a existência do PNG. Captura e assertions visuais permanecem ativas. Reexecução integral de Editor e players aprovada (R3). SHA-256 da fixture avaliada: `0EE2B9D53C01CC83AF7F96718B2E70D7CCA499FE8426382E7C19D2F783AB3A66`.

## Limites do aceite

Esta validação automatizada não substitui medições de desempenho (T049), matriz física de entrada (T050) nem participantes (T051). O 8BitDo no S23 continua adiado conforme decisão do usuário. T049–T053 não executadas nesta solicitação.

## Inicialização dos produtos reais

Windows: BuildWindows do PrototypeBuildPipeline, marcador `PROTOTYPE_BUILD_OK target=StandaloneWindows64`, saída 0. Produto em `Builds/T048/ValidationProject/Builds/Windows/QuackQuack.exe`; SHA-256 de GameAssembly.dll: `5909B7F16B787AE3B5205BD3F87673E87496414378C98422B7F3D0377CFAEF65`. Produto normal, sem harness de revisão e sem BuildOptions de teste.

Janela observada por computer-use: HUD, Pato, inimigos e pickups presentes; relógio avançou, oferta do nível 2 apareceu, carta Ovos escolhida por mouse e simulação retomada. Pausa confirmada pelo botão CONTINUAR em 00:36. A tentativa de injetar Escape não confirmou pausa, portanto este smoke não aprova teclado físico. Log local `windows-product-player.log`, sem exceção gerenciada observada na sessão; aviso nativo D3D12 sobre consulta da interface de info queue não impediu renderização. Não equivale à matriz física T050.

Android: BuildAndroid do PrototypeBuildPipeline, marcador `PROTOTYPE_BUILD_OK target=Android`, saída 0. APK ARM64 IL2CPP em `Builds/T048/ValidationProject/Builds/Android/QuackQuack.apk`; SHA-256: `1EB8050EE30A6FBBC4ED1854DD0C967EC3650B685882E1F613301D56691E84BB`. Instalado por ADB com sucesso e iniciado na identidade isolada. A primeira captura mostrou a tela apagada; o usuário desbloqueou o S23 antes da observação final.

Capturas finais locais `android-product-visible.png`, `android-product-running.png` e `android-product-paused.png`, em `Logs/Polimento002/T048`: oferta do nível 2 visível, escolha de Ovos por toque, retorno à simulação em 00:17 com HUD/joystick/Quack e pausa em 00:34 com CONTINUAR. Log `android-product-player.log` sem exceção gerenciada/FATAL EXCEPTION observada no recorte da sessão. Confirma inicialização/fluxo básico do produto; não aprova desempenho, rotação completa ou matriz física. As duas partidas ficaram pausadas ao final do smoke.

## Descoberta por classe — suítes completas

### editmode

| Classe | Casos | Resultado | Ignorados |
|---|---:|---|---:|
| BreathStateTests | 24 | Passed | 0 |
| BuildStateTests | 27 | Passed | 0 |
| CartoonUiAssetsTests | 7 | Passed | 0 |
| CharacterDefinitionTests | 12 | Passed | 0 |
| ContentValidationTests | 9 | Passed | 0 |
| ControlProfileTests | 24 | Passed | 0 |
| EffectiveAttributesTests | 87 | Passed | 0 |
| EncounterDefinitionTests | 20 | Passed | 0 |
| EncounterScheduleTests | 12 | Passed | 0 |
| EnemyBehaviorSettingsTests | 7 | Passed | 0 |
| EnemyDefinitionTests | 31 | Passed | 0 |
| EnemyStateTests | 10 | Passed | 0 |
| EvolutionDefinitionTests | 17 | Passed | 0 |
| ExperienceStateTests | 20 | Passed | 0 |
| FrogKingSettingsTests | 10 | Passed | 0 |
| GameplayHudCompositionTests | 4 | Passed | 0 |
| JsonSaveWriteTests | 28 | Passed | 0 |
| LakeEncounterCatalogTests | 16 | Passed | 0 |
| LakeEnvironmentAssetsTests | 4 | Passed | 0 |
| PassiveDefinitionTests | 14 | Passed | 0 |
| PickupPolicyDefinitionTests | 42 | Passed | 0 |
| PlayerCommandTests | 3 | Passed | 0 |
| ProgressSaveTests | 4 | Passed | 0 |
| ProgressSnapshotTests | 12 | Passed | 0 |
| PrototypeBuildCatalogTests | 11 | Passed | 0 |
| QuackDefinitionTests | 30 | Passed | 0 |
| QuackRulesTests | 9 | Passed | 0 |
| RelicDefinitionTests | 13 | Passed | 0 |
| RelicLoadoutTests | 11 | Passed | 0 |
| ResultsContractTests | 1 | Passed | 0 |
| RewardCapacityTests | 1 | Passed | 0 |
| RewardLedgerTests | 36 | Passed | 0 |
| RewardPolicyTests | 23 | Passed | 0 |
| RunOrchestrationContractsTests | 5 | Passed | 0 |
| RunProgressionTests | 10 | Passed | 0 |
| RunResultStatisticsTests | 30 | Passed | 0 |
| RunResultTests | 18 | Passed | 0 |
| RunStateTests | 5 | Passed | 0 |
| SaveServiceTests | 11 | Passed | 0 |
| SpawnRequestTests | 10 | Passed | 0 |
| UpgradeCatalogTests | 6 | Passed | 0 |
| UpgradeOfferTests | 21 | Passed | 0 |
| WeaponDefinitionTests | 30 | Passed | 0 |

### playmode

| Classe | Casos | Resultado | Ignorados |
|---|---:|---|---:|
| ArenaContainmentTests | 10 | Passed | 0 |
| ArenaLakeRunReproductionTests | 25 | Passed | 0 |
| AuraOcclusionTests | 14 | Passed | 0 |
| AuthoredBuildCatalogTests | 2 | Passed | 0 |
| BootstrapTests | 3 | Passed | 0 |
| BubbleAreaTests | 21 | Passed | 0 |
| CartoonActorVisualTests | 4 | Passed | 0 |
| CartoonUiThemeTests | 6 | Passed | 0 |
| CombatArtTests | 4 | Passed | 0 |
| ControllerAvailabilityTests | 30 | Passed | 0 |
| ControlProfileRuntimeTests | 8 | Passed | 0 |
| ControlSettingsFlowTests | 12 | Passed | 0 |
| EnemyUpdateSchedulerTests | 18 | Passed | 0 |
| FrogKingEncounterTests | 14 | Passed | 0 |
| FrogKingPracticeTests | 1 | Passed | 0 |
| FrogKingTelegraphTests | 9 | Passed | 0 |
| GamepadInputAdapterTests | 9 | Passed | 0 |
| GameplayHudCompositionRuntimeTests | 3 | Passed | 0 |
| GameplayHudTests | 31 | Passed | 0 |
| InputParityTests | 15 | Passed | 0 |
| IntegratedUiLayoutTests | 16 | Passed | 0 |
| KeyboardPlayerInputTests | 4 | Passed | 0 |
| LakeArenaTests | 2 | Passed | 0 |
| LakeEncounterCatalogFlowTests | 2 | Passed | 0 |
| LakeWaterAnimationTests | 3 | Passed | 0 |
| MobileInputAdapterTests | 9 | Passed | 0 |
| MobileTouchscreenTests | 1 | Passed | 0 |
| PassiveIntegrationTests | 4 | Passed | 0 |
| PerformanceBenchmarkTests | 3 | Passed | 0 |
| PerformanceTelemetryTests | 6 | Passed | 0 |
| PickupAttractionTests | 15 | Passed | 0 |
| PickupRewardFlowTests | 34 | Passed | 0 |
| PlatformBuildConfigurationSmokeTests | 9 | Passed | 0 |
| PlatformBuildSmokeTests | 2 | Passed | 0 |
| PlayerControllerTests | 7 | Passed | 0 |
| PooledEnemyTests | 12 | Passed | 0 |
| PoolServiceTests | 4 | Passed | 0 |
| QuackFeedbackTests | 6 | Passed | 0 |
| RelicLoadoutFlowTests | 16 | Passed | 0 |
| RelicSaveRoundTripTests | 1 | Passed | 0 |
| RelicsInputTests | 24 | Passed | 0 |
| ResponsiveUiTests | 15 | Passed | 0 |
| ResultsSceneFlowTests | 11 | Passed | 0 |
| ResultsViewTests | 6 | Passed | 0 |
| RunDirectorFlowTests | 35 | Passed | 0 |
| RunSceneFlowTests | 1 | Passed | 0 |
| SpawnDirectorTests | 16 | Passed | 0 |
| SpecialEnemyBarsTests | 16 | Passed | 0 |
| TerrainAndKnockbackTests | 8 | Passed | 0 |
| UnifiedInputContractTests | 1 | Passed | 0 |
| UnifiedPlayerInputTests | 7 | Passed | 0 |
| UpgradeFeedbackTests | 7 | Passed | 0 |
| UpgradeFlowTests | 9 | Passed | 0 |
| UpgradeSelectionTests | 27 | Passed | 0 |
| WeaponRunnerTests | 35 | Passed | 0 |

## Descoberta por classe — players IL2CPP R3

| Classe | Editor selecionado | Windows | Android | Ignorados em cada player |
|---|---:|---:|---:|---:|
| ArenaContainmentTests | 10 | 10 | 10 | 0 |
| AuraOcclusionTests | 14 | 14 | 14 | 0 |
| BubbleAreaTests | 21 | 21 | 21 | 0 |
| CartoonActorVisualTests | 4 | 4 | 4 | 0 |
| CombatArtTests | 4 | 4 | 4 | 0 |
| ControllerAvailabilityTests | 30 | 30 | 30 | 0 |
| InputParityTests | 15 | 15 | 15 | 0 |
| IntegratedUiLayoutTests | 16 | 16 | 16 | 0 |
| LakeWaterAnimationTests | 3 | 3 | 3 | 0 |
| PassiveIntegrationTests | 4 | 4 | 4 | 0 |
| PickupAttractionTests | 15 | 15 | 15 | 0 |
| PlatformBuildSmokeTests | 2 | 2 | 2 | 0 |
| QuackFeedbackTests | 6 | 6 | 6 | 0 |
| SpecialEnemyBarsTests | 16 | 16 | 16 | 0 |
| UpgradeFeedbackTests | 7 | 7 | 7 | 0 |

## T050 — matriz física e visual (10/11/2026)

**Parcial: matriz disponível executada; encerramento técnico pendente.** Base `0ee1ac9` (T049). Avanço para T050 solicitado explicitamente pelo usuário; T049 permanece aberta e não fornece aceite de entrada, memória ou plataforma por inferência.

### Preparação e limites

Cópia nova `Builds/T050/ValidationProject`, somente Assets/Packages/ProjectSettings, sem copiar Library. Runtime/regras/UI de produção preservados. Laboratório privado apenas na cópia: save/identidade separados, alvo protegido contra morte e dez relíquias liberadas para navegar; inicia pausado. F8 gera exatamente o XP até o próximo nível pelo fluxo real; F3 reinicia a cena; F6 prepara recursos; F7 antecipa especiais. Esses recursos não aprovam regras de combate nem progressão, somente preparam os contextos de entrada/UI. Nenhuma navegação/confirmar/retornar física é injetada pelo laboratório.

Windows F2 alterna 1280×720, 1440×720, 1560×720 e 960×720 (16:9, 18:9, 19,5:9, 4:3); registrar tamanho efetivo, não apenas solicitado. F4 alterna Safe Area nativa/recorte esquerdo/direito sintético. A matriz automatizada complementar usa as quatro dimensões padrão do quickstart. No Android, manter Screen/Safe Area nativos e girar fisicamente para as duas paisagens; não simular rotação via ADB nem alegar quatro aparelhos a partir do S23.

Os helpers não entram no código/cenas do produto nem no Git. `QUACKQUACK_T050_SKIP_LAB=1` desativa sua criação na execução automatizada do Editor para não pausar/alterar fixtures. Logs de disponibilidade de controles registram layout/nome comercial/contexto, sem serial, endereço Bluetooth ou identificador de instância.

Usuário confirmou disponibilidade atual: Windows, S23, Xbox Series e DualSense; também 8BitDo no Windows/XInput. 8BitDo/S23 continua adiado por decisão anterior; não repetir diagnóstico de OTG sem novo pedido.

### Critérios físicos

- P01: movimento, Quack e pausa/retomada; uma ação por pressionamento, sem comandos presos.
- P02: Relíquias por atalho e menu; fechar retoma apenas quando abriu durante simulação e conserva a pausa anterior.
- P03: navegação/foco em cartas, BANIR, RERROLAR e PULAR; voltar de BANIR, Cancel não pula escolha obrigatória; confirmar uma vez sem Quack ao voltar.
- P04: CONTROLES, família/rótulos, trocar/persistir/restaurar atalho; acesso por opção focalizável mesmo sem Select.
- P05–P09: desconectar/reconectar em partida, pausa, Relíquias, melhoria e resultados, incluindo analógico/Quack pressionado; foco/contexto preservados, sem resolver escolha ou reiniciar sozinho.
- P10: S23 oculta touch com controle disponível; restaura joystick/Quack funcional ao desligar e oculta ao reconectar. Desligar pelo controle sem sair do app evita confundir hotplug com pausa de sistema.
- V01: quatro proporções no Windows com estados de recursos, XP/nível, dicas, especiais, cartas, Relíquias, CONTROLES e resultados legíveis e sem sobreposição; recortes sintéticos dos dois lados.
- V02: toque e as duas paisagens reais no S23; recorte da câmera/barras do sistema respeitados nos mesmos elementos essenciais.

### Matriz desta rodada

| Combinação | Comandos/menus P01–P04 | Hotplug P05–P09 | Touch P10 | Evidência |
|---|---|---|---|---|
| Teclado físico / Windows (modelo não informado) | Aprovado; P04 navegação | Não se aplica ao roteiro de controles | Não se aplica | Relato atual dos dois roteiros |
| Xbox Series / Windows Bluetooth | Aprovado | Aprovado nos cinco contextos | Não se aplica | Relato atual P01–P09 |
| DualSense / Windows Bluetooth | Aprovado; família automática | Aprovado nos cinco contextos | Não se aplica | Relato atual P01–P09 |
| 8BitDo Ultimate C / Windows 2,4 GHz/XInput | Aprovado; perfil Xbox/XInput adequado | Aprovado nos cinco contextos | Não se aplica | Relato atual P01–P09 |
| Toque / S23 Ultra | Aprovado; P04 navegação | Não se aplica | Aprovado sem controle conectado | Relato atual R2; duas paisagens |
| Xbox Series / S23 Bluetooth | Aprovado; família automática | Aprovado nos cinco contextos | Aprovado: restaura/oculta toque | Relato atual R2 P01–P10 |
| DualSense / S23 Bluetooth | Aprovado pelo usuário; família automática | Funcional nos cinco contextos; exceções no log sob investigação | Aprovado: restaura/oculta toque | Relato atual R2 P01–P10; duas paisagens; ressalva técnica abaixo |
| 8BitDo / S23 OTG | Pendente, adiado | Pendente, adiado | Pendente, adiado | Decisão anterior mantida |

Pendente não equivale a falha do jogo. Evidências anteriores de T014/T040 são históricas e não preenchem automaticamente as células desta rodada. S23 não representa Android intermediário/de entrada. T051 não iniciada.

### Evidência complementar T050

PlayMode focado: **132/132, nove classes, zero falhas/ignorados**. ControllerAvailability 30, GamepadInputAdapter 9, InputParity 15, IntegratedUiLayout 16, KeyboardPlayerInput 4, MobileInputAdapter 9, RelicLoadoutFlow 16, ResultsView 6 e UpgradeSelection 27. XML `TestResults/T050/ui-input.xml`, log `Logs/Polimento002/T050/ui-input.log`, 96 capturas novas em `Logs/Polimento002/T050/Matrix`. Ausência de T050_READY no log confirma que o laboratório não foi criado nas fixtures. Essa execução não substitui entrada física.

Windows: build IL2CPP pelo PrototypeBuildPipeline, saída 0/PROTOTYPE_BUILD_OK, produto `Builds/T050/ValidationProject/Builds/T050/Windows/QuackQuack T050.exe`. GameAssembly.dll SHA-256 `55F1F0707FAF9C96858D4ECB4F650F6077711D2E893FE627D3BA10CA13BEC928`. Janela observada em pausa, viewport 1280×720, laboratório visível apenas em pausa fora dos modais. Logs locais de build/player em `Logs/Polimento002/T050`.

**Teclado/Windows:** usuário respondeu “Tudo funciona” ao roteiro WASD/setas, Espaço, Esc, R, propriedade da pausa e navegação em pausa/Relíquias/CONTROLES. Respondeu “Todos os passos funcionam” ao roteiro F8, foco em cartas/ações, Esc em BANIR sem resolver oferta, confirmação única sem Quack, resultados com barras/ação e reinício único. Modelo não informado; nenhuma inferência de hotplug de teclado.

**Xbox Series/Windows Bluetooth:** “Todos os passos funcionam” para P01–P04, incluindo menus, ambos os casos de propriedade da pausa em Relíquias, foco/retorno/confirmar sem Quack e edição/reabertura/restauração do atalho em CONTROLES. “Todos os cinco contextos funcionam” para desconectar/reconectar em partida, pausa, Relíquias, melhoria e resultados, incluindo analógico/Quack pressionado e navegação/reinício único nos resultados. Não relatou caso não coberto.

**DualSense/Windows Bluetooth:** “Tudo funciona; família automática” para P01–P04 e “Todos os cinco contextos funcionam” para P05–P09, no roteiro equivalente com Cross/Circle, Options/Create, remapeamento/restauração e resultados. Sem seleção manual de família ou caso não coberto relatado.

**8BitDo/Windows, receptor 2,4 GHz em XInput:** “Tudo funciona; perfil Xbox/XInput adequado” para P01–P04. “Todos os cinco contextos funcionam” ao remover/recolocar o receptor nos cinco contextos P05–P09, incluindo analógico/Quack pressionado, foco, barras e reinício único. Não foi preciso selecionar Genérico manualmente. Evidência vale para esse dispositivo/modo, não para todo HID genérico nem para Android/OTG.

**Visual Windows, rodada inicial:** “Os quatro formatos estão corretos” para HUD/recursos/dicas/relógio/XP, após F2/F3/F6. Barras dos três especiais aprovadas em 16:9 e 4:3. No roteiro de modais, usuário relatou sobreposição nos resultados, semelhante ao problema anterior das cartas; não aprovar esse roteiro integralmente. Falha reproduzida em 4:3: conteúdo das três colunas começa dentro da moldura ilustrada, que cobre letras e bordas do texto. Correção e reteste R2 descritos abaixo. Interrupção por Esc foi esclarecida como acidental e a retomada autorizada pelo usuário; aceites anteriores preservados.

**Correção de resultados:** regressão nova compara os recuos reais do viewport com as bordas do sprite ilustrado (pixels por unidade/multiplicador), exige separação do scrollbar e altura completa do conteúdo. Antes da correção: 12 aprovados/4 falhas esperadas em 16 casos IntegratedUiLayout, um caso por proporção; zero ignorados. `Results text must clear the left illustrated frame`, esperado pelo menos 31, encontrado 20. XML `TestResults/T050/results-red.xml`.

`PolishUiSetup.ApplyIllustratedLayout` agora aplica recuos (44/40 à esquerda/base, 62/40 à direita/topo) nas três colunas ResultsView; scrollbar recuado 36, largura 14 e intervalo vertical 80. `ResultsSetup.UpdateIllustratedLayout` atualiza o prefab existente via Unity. Somente seis RectTransforms foram reserializados, preservando IDs, referências, .meta, ações e grafo de navegação; nenhuma regra/input alterado. Reexecução **132/132, zero falhas/ignorados**, XML `TestResults/T050/ui-input-r2.xml`, 96 capturas em `Logs/Polimento002/T050/Matrix-r2`; captura 4:3 dos resultados inspecionada e texto afastado das molduras. Aceites físicos R2 registrados abaixo, com ressalva técnica no fechamento.

**Reteste físico Windows R2 aprovado:** “Resultados corretos nos quatro formatos e recortes”, incluindo títulos/primeiras letras, rolagem até o fim, linhas acessíveis e botões. Com 8BitDo: “Tudo funciona”, confirmando barras/ações, rolagem, retorno às ações, desconectar/reconectar nos resultados sem ativação e reinício único. Usuário esclareceu “A falha estava somente nos resultados”: Relíquias, CONTROLES e cartas já estavam corretos nas quatro proporções/recortes da rodada anterior. Prévia fica limitada ao conteúdo que apareceu, com cobertura automatizada complementar; não inferir uma combinação de carta não observada. Logs registram os quatro tamanhos efetivos (1280×720, 1440×720, 1560×720, 960×720).

Build Windows R2: `Builds/T050/ValidationProject/Builds/T050/R2/Windows/QuackQuack T050.exe`, pipeline/exit 0, GameAssembly SHA-256 `8148D7EA49B18D09494208A99403A6CAB8E8668B5E98993E6EB042F7A7B44B7F`. Janela antiga fechada, R2 aberta e observada pausada em 1280×720. Perfil/saves do laboratório preservados pela mesma identidade. Resultados funcionais R1 ficam delimitados acima; reteste R2 verifica a área alterada.

Android: build ARM64 IL2CPP pelo mesmo pipeline, saída 0/PROTOTYPE_BUILD_OK, instalado com sucesso no S23 SM-S918B/Android 17; nome QuackQuack T050, pacote `com.cemggames.quackquack.validation002.t050`, separado das versões anteriores e do produto. APK `Builds/T050/ValidationProject/Builds/T050/Android/QuackQuack T050.apk`, SHA-256 `F49BDF7BC051E64DE52A64EA1396FA9991CB907AD8358EF9D691F9D4008D915F`. Instalação não é aceite de toque/controle; aceites da R2 registrados abaixo.

APK R2 corrigido: `Builds/T050/ValidationProject/Builds/T050/R2/Android/QuackQuack T050.apk`, SHA-256 `3BC02AD3CD9FE3A902199949B56B95CD1D6CC901ED930FA0A7EDF941D813E3B9`, instalado por atualização sem apagar perfil e iniciado no S23 antes da retomada. Prefab de resultados na fonte e na cópia tem SHA-256 `F9F1F60DA46156DC8B355B4F8C7CDBA9524249951873408ACD7DCEAB6ED7E4E5`. Testes físicos Android R2 registrados abaixo.

**Toque/S23 R2 aprovado:** “Todos os passos funcionam por toque” no roteiro de movimento/Quack simultâneos, pausa, propriedade da pausa em Relíquias, melhoria/ações/confirmação única e resultados corrigidos com rolagem/reinício. “Tudo correto nas duas paisagens” para giro físico com rotação automática, HUD/dicas/XP/nível, modais, recursos e especiais, legibilidade e respeito ao recorte/barras do sistema. Não relatou estado/tela não testado. Evidência restrita a esse modelo e APK; nenhuma rotação sintética usada no aparelho.

**Xbox Series/S23 Bluetooth R2 aprovado:** “Tudo funciona; família automática” no roteiro P01–P04 (touch oculto, controles/rótulos, propriedade da pausa, melhoria/retorno/confirmação única, remapeamento/reabertura/restauração). “Todos os cinco contextos e a troca para toque funcionam” para P05–P10, incluindo analógico/Quack pressionado, ausência de ativação indevida, retorno funcional do toque ao desligar e ocultação ao reconectar, rolagem e reinício único nos resultados. Não relatou caso não coberto.

Validador de conteúdo pós-correção: `T073_CONTENT_VALIDATION_OK assets=67 warnings=2`, saída 0, log `Logs/Polimento002/T050/content-r2.log`. Mesmos dois avisos URP UnsupportedType conhecidos; nenhuma dependência/pacote/arte/catálogo de gameplay alterado.


**DualSense/S23 Bluetooth R2 — aceite funcional:** usuário respondeu “Tudo funciona; família automática” para P01–P04 e “Todos os casos funcionam nas duas paisagens” para P05–P10: cinco contextos, analógico/Quack pressionado, foco/contexto sem ativação indevida, retorno/ocultação do toque, rolagem e reinício único. Confirmou HUD/rótulos/menus nas duas paisagens físicas. Nenhum caso não coberto relatado. Esse aceite funcional não elimina as exceções encontradas na auditoria final.

### Ressalva técnica e pendências para encerrar T050

A auditoria final do log Android R2 encontrou duas `NullReferenceException` em `InputManager.FireStateChangeNotifications`, chamadas de `InputSystem.onBeforeUpdate`, imediatamente antes do evento Removed do DualSense durante Playing. O backend informa layout `DualShock4GamepadAndroid` e produto `DualSense Wireless Controller`; o controle físico é o DualSense informado pelo usuário. Outra exceção aparece em `T050Lab.Update`, no laboratório privado. Evidências locais: `Logs/Polimento002/T050/android-player-r2.log` e `android-exception-detail.log`. Não há linhas de código nos stacks IL2CPP; proximidade temporal não prova causalidade nem atribui a falha ao pacote.

Inspeção do código instalado do Input System e da composição touch identificou alterações síncronas de contexto durante eventos de disponibilidade como hipótese a investigar. Não houve reprodução mínima nem prova suficiente para alterar pacote ou input de produção. O laboratório também precisa de isolamento de seu ciclo de recarga; a exceção nele não demonstra falha equivalente no produto. Os 132 testes aprovados não reproduzem estes registros nativos e não permitem declará-los corrigidos.

**Próximo passo técnico:** reproduzir hotplug do DualSense em uma build sem laboratório, com log novo e símbolos/stack suficientes; separar a falha de instrumentação, criar regressão para a causa confirmada, corrigir e repetir os contextos afetados. Preservar todos os aceites humanos acima, sem reclassificar a sessão como livre de exceções. Nenhuma atualização de pacote foi realizada por suposição.

T050 permanece desmarcada por essa investigação/reteste e pela combinação 8BitDo/S23 explicitamente adiada pelo usuário. As validações disponíveis foram registradas; não solicitar novamente diagnóstico de OTG. T051 não iniciada. Builds, helpers, logs e capturas permanecem locais/ignorados; somente correção de layout, regressão e evidências textuais são versionados.
