# Validação transversal — T048

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
