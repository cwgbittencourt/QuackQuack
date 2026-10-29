# T075 — matriz de validação Windows/Android

## Estado

Validação parcial, com **interface Android reprovada**. A T075 permanece aberta
pelas falhas reproduzidas abaixo e pelas linhas obrigatórias sem evidência.
Testes automatizados não concedem aceite de gameplay, legibilidade, entrada física
ou desempenho em outra classe de aparelho. Nenhuma execução automatizada ficou ativa.

Base: commit `54b22ca`, Unity **6000.4.3f1**, seed de produção **42** e benchmark
**71**. Execução local iniciada em 28/10/2026 (America/Sao_Paulo; XML em UTC).
Nenhuma alteração de regras ou de assets de produto nesta validação.

## Ambiente e isolamento

- Windows 11, Ryzen 7 5800X3D, RTX 4070 Ti, aproximadamente 64 GB RAM.
- Android real Samsung S23 Ultra SM-S918B, Android 17/API 37, Adreno 740/Vulkan.
- Usuário confirmou movimento, Quack e pausa/retomada com controle no Windows;
  modelo não informado. Isso não aprova navegação de todos os menus por controle.
- Android intermediário e de entrada indisponíveis nesta sessão.
- Unity executado em `Temp/T033Validation`, cópia isolada sem `.git`.
- Saves dos testes em diretórios exclusivos de Application.temporaryCachePath;
  cleanup verifica o diretório antes de removê-lo. Saves pessoais não são usados.
- Build jogável isolada usa nome `QuackQuack T075` e pacote Android
  `com.cemggames.quackquack.validationt075`. Test players e builds de produto são
  evidências distintas. Logs, XML, capturas e binários permanecem locais ignorados.

## Execuções automatizadas

| Execução | Resultado | Artefato local |
|---|---|---|
| EditMode completo | 465/465; zero falhas/ignorados | TestResults/t075-editmode.xml |
| PlayMode completo | 254/254; zero falhas/ignorados | TestResults/t075-playmode.xml |
| Resultados após remover restrição Editor | 5/5 | TestResults/t075-results-editor.xml |
| Player Windows x64 IL2CPP, seleção inicial | 15/15 | TestResults/t075-windows-runtime.xml |
| Player Windows, fixture portátil e paridade | 30/30; zero falhas/ignorados | TestResults/t075-windows-runtime-final.xml |
| Player Android ARM64 IL2CPP, tentativa inicial | 29/30; falha na indução de erro da fixture | TestResults/t075-android-runtime.xml |
| Player Android, fixture portátil | 30/30; zero falhas/ignorados | TestResults/t075-android-runtime-final.xml |
| PlayMode completo após correção da fixture | 254/254; zero falhas/ignorados | TestResults/t075-playmode-final.xml |
| Validador de conteúdo | 63 assets, zero erros, dois avisos URP UnsupportedType | Logs/t075-content-validation.log |

Build Windows de produto: compilação aprovada, BuildOptions.None, cenas
RunBootstrap/LakeRun, sem Test Runner. Caminho local:
`Temp/T033Validation/Builds/T075WindowsProduct/QuackQuack.exe`.
SHA-256 de GameAssembly.dll:
`73574A7D8B25723B0DC3FBDB3BFFA6EC673E50AA5F15AE14C133C1BFCE48227F`.
Janela iniciada e cena jogável observada, HUD, terreno, inimigos e pausa presentes.

Build Android de produto: compilação aprovada, APK de 35294444 bytes, apenas ABI
`arm64-v8a` (inspeção ZIP). Caminho local:
`Temp/T033Validation/Builds/T075AndroidProduct/QuackQuack.apk`.
SHA-256: `D49A1D71AF311D6F50BB8F50E55C1BA8195A567C6F241F9C04B524D10FBDA2BF`.
As builds de produto usam BuildOptions.None; a instrumentação de benchmark T074
não foi adicionada às cenas de produção. APK instalado e iniciado via ADB no S23.

Os cinco ResultsSceneFlowTests agora compilam nos players. Cobrem vitória,
derrota, abandono, falha de gravação com retry idempotente e nova partida com
carteira preservada. A vitória usa relógio acelerado e dano de teste: não conta
como partida humana de 15 minutos. A seleção final é igual nas duas plataformas:
15 casos de paridade, cinco de resultados, seis de telemetria, dois smoke de
produção e dois de configuração do alvo 60/30. Os avisos de conteúdo são os tipos
nativos URP sem validador de domínio, já documentados na T073; não são erros ocultos.

## Matriz funcional

“Automatizado” descreve o alcance da evidência; não significa aceite humano.

| Cenário / requisito | Evidência executada | Revisão restante |
|---|---|---|
| Movimento, Quack e pausa; FR-002/018 | KeyboardPlayerInput 4, GamepadInputAdapter 9, MobileInputAdapter 9, InputParity 15, UnifiedPlayerInput 7 no Editor | Teclado/controle Windows e toque Android físicos |
| Água/terra, afinidade e obstáculo; FR-003/010 | TerrainAndKnockback 8, LakeArena 2 | Mobilidade e leitura das rotas em jogo |
| Fôlego vazio/parcial/cheio e reflexão; FR-004/005 | Regras EditMode, PlayerController 7, QuackFeedback 6 | Resposta percebida e distinção audiovisual |
| XP, cartas e recursos; FR-007/008 | UpgradeFlow 9, UpgradeSelection 6 | Navegação física e texto das cartas |
| Seis armas/passivas e três evoluções; FR-006/009 | Catálogos EditMode, AuthoredBuildCatalog 2, WeaponRunner 19, UpgradeFeedback 7 | Todas as combinações em jogo representativo |
| Cinco inimigos e dois minibosses; FR-011/012 | EnemyUpdateScheduler 16, LakeEncounterCatalogFlow 2, RunDirectorFlow 13 | Composição e legibilidade sob pressão |
| Chefe e perigos; FR-013/014 | FrogKingEncounter 14, FrogKingTelegraph 9 | Audição e antecipação percebida |
| Feedback; FR-015 | QuackFeedback 6 e UpgradeFeedback 7 | Distinção física no combate |
| Três finais e resultado; FR-001/016 | ResultsSceneFlow 5 também no player Windows | Partida real e revisão da tela final |
| Dez relíquias, máximo três; FR-017 | Regras EditMode, RelicLoadoutFlow 3, RelicSaveRoundTrip 1 | Equipar/remover e reabrir build de produto |
| Pools e pressão 200/400 | PoolService 4, PooledEnemy 12, SpawnDirector 16, benchmark 3; perfil T074 | Sessão longa no hardware alvo |
| Layout/Safe Area; FR-019 | ResponsiveUi 15, quatro proporções e insets sintéticos | Todas as telas/modais e recorte real |
| Escopo; FR-020 | Catálogos fechados cobertos por testes | Auditoria específica T078 |

Os números na tabela são casos PlayMode aprovados na suíte completa; grupos
podem sustentar mais de uma linha e não devem ser somados como testes exclusivos.

## Matriz física e visual

| Plataforma / tela / entrada | Estado |
|---|---|
| Windows, teclado: movimento/Quack/pausa | Aprovados pelo usuário no teclado físico |
| Windows, teclado: cartas/relíquias/resultado | Navegação completa por teclado pendente |
| Windows, controle: movimento/Quack/pausa | Aprovados pelo usuário; modelo não informado |
| Windows, controle: cartas/relíquias/resultado | Pendente |
| S23 Ultra, toque: joystick + Quack simultâneos e pausa/retomada | Aprovados pelo usuário; sem comandos presos |
| S23 Ultra, relíquias e ações de partida | Reprovado: botões ausentes/encobertos |
| HUD 16:9 / 18:9 / 19,5:9 / 4:3 | Geometria automatizada aprovada; revisão visual pendente |
| Cartas, relíquias e resultados nas quatro proporções | Pendente |
| Android em paisagem, duas orientações do recorte real | Sem aceite; falhas de composição impedem aprovar a revisão |
| Save de produto após fechar/reabrir | Windows: relíquias persistidas observadas; Android pendente |
| Partida de 15 minutos, encontros 5/10/15 e três finais | Linha temporal acelerada aprovada; partida real pendente |
| Intermediário: 400 inimigos a 60 FPS | Sem aparelho |
| Entrada: 400 inimigos a 30 FPS | Sem aparelho |
| Operação offline da build de produto | Não foi feita alternância de rede durante esta sessão |

Capturas `Logs/t069-hud-{1920x1080,2160x1080,2340x1080,1440x1080}.png`
regeneradas pela suíte T075 e inspecionadas: os oito elementos essenciais do HUD
aparecem sem corte nas margens simuladas. São renders isolados do HUD, sem cenário,
modais ou recorte real; não equivalem a aprovação integral SC-008.

Na build Windows de produto, inspeção pela skill computer-use: cartas com três
opções visíveis, confirmação por clique e retorno ao combate; pausa pelo botão;
painel das dez relíquias, equipar/remover atualizando 0/3→1/3→0/3 e preservação
da pausa anterior. Abandono aos 45 s, seed 42: tela mostrou 22 inimigos, 22 moedas
ganhas, desconto 11, recebidas/saldo 11 e “Progresso salvo”. Fechar e reabrir o
processo preservou Apito Veloz, Pedra da Margem, Pérola do Lago e Anel de Ondas
desbloqueados na primeira sessão. As telas observadas estavam sem cortes aparentes
no viewport Windows usado; isso não cobre as quatro proporções de todos os modais.
Injeção automática de Return/Escape não confirmou ação; em teste posterior o usuário
confirmou movimento, Espaço e Esc no teclado físico. Não foi atribuído defeito ao
produto a partir da injeção inconclusiva.

Na primeira execução Android, somente FailedSaveRemainsReviewableAndRetryCommitsOnce
falhou: o arquivo aberto com FileShare.Read não impediu sua substituição, e o save
concluiu. Corrigida a fixture para preservar o arquivo próprio e ocupar seu caminho
com um diretório vazio durante a tentativa; restauração em finally e retry mantêm
as mesmas asserções. Nenhuma regra de save foi modificada e nenhum caso foi ignorado.
Reexecução Android aprovada: 30/30, incluindo os 15 casos de InputParityTests,
cinco de resultados, seis de telemetria, dois smoke e dois de alvo 60/30.

## Falhas reproduzidas na build Android de produto

Captura real `Logs/t075-android-product.png`, 3088×1440, S23 em paisagem, seed 42.
O usuário confirmou independentemente os três problemas na mesma sessão.

| ID / prioridade | Reprodução e observado | Esperado / gate afetado |
|---|---|---|
| VAL-UI-01 / alta | Iniciar LakeRun no S23: botão RELÍQUIAS não aparece | Acesso ao loadout por toque; FR-017/018, SC-007 |
| VAL-UI-02 / alta | Canto inferior direito: botão móvel QUACK encobre PAUSAR e ABANDONAR da partida | Ações de partida visíveis e acionáveis sem sobreposição; FR-002/019, SC-007/008 |
| VAL-UI-03 / média | Canto superior direito: botão móvel PAUSAR encobre o relógio | Tempo legível durante combate; FR-019, SC-008 |

São falhas da composição completa: ResponsiveUiTests cobre os oito elementos do
HUD com insets sintéticos e não verifica interseções com o Canvas móvel nem todos
os modais. Portanto, seu sucesso não contradiz esta reprovação. Causa raiz ainda
não confirmada; não foram alterados layout, prefabs ou regras de produção para
mascarar a falha. Corrigir a composição e repetir estes casos antes do aceite.

## Desempenho e critérios de sucesso

Evidência reutilizada, sem alterações de regras desde a T074:
[relatório T074](test-report-t074.md), capturas reais a alvo 60 com 400 inimigos:
Windows 59,86 FPS/p95 16,668 ms/p99 17,095 ms; S23 59,93 FPS/p95 16,673 ms/
p99 16,760 ms; zero GC.Alloc nas fases finais. Windows registrou duas perdas de
foco. Memória inclui instrumentação, sem orçamento quantitativo aprovado.

SC-001 a SC-005: sem amostra de participantes; não aprovados por testes.
O formulário é escopo da T076 e não foi produzido nesta task.
SC-006: parcial (benchmark Windows/S23); falta input percebido sob pico, sessão longa
e aparelhos intermediário/entrada. SC-007: paridade sintética e comandos físicos
básicos aprovados, cenários completos bloqueados pela UI Android. SC-008: reprovado
na composição real Android; quatro proporções de todas as telas ainda pendentes.

## Reprodução

Com Unity.exe 6000.4.3f1 e projeto isolado, usar os argumentos:

```text
-batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform EditMode -testResults <repo>/TestResults/t075-editmode.xml -logFile <repo>/Logs/t075-editmode.log
-batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t075-playmode.xml -logFile <repo>/Logs/t075-playmode.log
-batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform StandaloneWindows64 -testCategory FrameRateRuntime;PlatformRuntimeSmoke;PerformanceTelemetryRuntime;ResultsRuntime -playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T075WindowsTests -testResults <repo>/TestResults/t075-windows-runtime.xml -logFile <repo>/Logs/t075-windows-runtime.log
-batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform StandaloneWindows64 -testFilter PlatformBuildSmokeTests;PerformanceTelemetryTests;ResultsSceneFlowTests;InputParityTests;FrameLimitUsesNominalOrOptionalLowRate -playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T075WindowsTestsFinal -testResults <repo>/TestResults/t075-windows-runtime-final.xml -logFile <repo>/Logs/t075-windows-runtime-final.log
```

Para Android, configurar ARM64/IL2CPP e pacote isolado pelo Editor, conectar ADB
autorizado e selecionar `-buildTarget Android -testPlatform Android`. Seleção:
`-testFilter PlatformBuildSmokeTests;PerformanceTelemetryTests;ResultsSceneFlowTests;InputParityTests;FrameLimitUsesNominalOrOptionalLowRate`.
Produção usa PrototypeBuildPipeline, cenas RunBootstrap/LakeRun e BuildOptions.None;
aplicar nome/pacote isolados na cópia antes de compilar. Não reutilizar o save pessoal.

Checklist requirements.md: 16/16, mantido sem edição. `.specify/extensions.yml`
ausente; nenhum hook aplicável. Pendências acima impedem marcar T075 como concluída.
