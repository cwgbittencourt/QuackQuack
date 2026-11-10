# Guia de validação — Polimento do Lago

Planejamento de 03/11/2026; comandos abaixo serão executados após implementar as tasks correspondentes. Nenhuma aprovação de gameplay, teste ou desempenho é declarada aqui. Regras: [spec](spec.md), [modelo](data-model.md), [contratos](contracts/gameplay-contracts.md), [tasks](tasks.md).

## Preparação

Unity 6000.4.3f1 com Windows IL2CPP e Android ARM64, dependências do manifest atual, ADB e aparelho autorizado. Usar cópia isolada nova com Assets, Packages e ProjectSettings completos; não reutilizar cópia de outra feature nem copiar Library. Procedimento detalhado e helper Invoke-ValidationUnity: [guia existente](../001-prototipo-lago/quickstart.md), seção comandos PowerShell. Não abrir Editor da cópia durante batch.

Na cópia, usar nome QuackQuack Validation 002 e pacote Android com.cemggames.quackquack.validation002, backend IL2CPP/ARM64 e paisagem autorrotativa dos dois lados. Não copiar essas configurações ou saves de volta ao produto. Logs, XML, APKs, screenshots regeneráveis, caches e serial de aparelho não entram em commit.

Variáveis de exemplo, após criar e importar a cópia isolada:

```powershell
$repo = 'C:/Users/lucas/source/repos/QuackQuack'
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Unity.exe'
$copy = Join-Path $repo 'Temp/Polimento002Validation'
$logs = Join-Path $repo 'Logs/Polimento002'
$results = Join-Path $repo 'TestResults/Polimento002'
foreach ($required in @($unity, (Join-Path $copy 'Assets'), (Join-Path $copy 'Packages/manifest.json'), (Join-Path $copy 'ProjectSettings/ProjectVersion.txt'))) {
    if (!(Test-Path -LiteralPath $required)) { throw "Prepare a cópia isolada; falta: $required" }
}
if ((Get-Content (Join-Path $copy 'ProjectSettings/ProjectVersion.txt') -Raw) -notmatch '(?m)^m_EditorVersion: 6000\.4\.3f1\s*$') { throw 'Versão incorreta.' }
New-Item -ItemType Directory -Force -Path $logs, $results | Out-Null
function Invoke-ValidationUnity {
    param([string[]]$UnityArguments)
    $quoted = @($UnityArguments | ForEach-Object {
        if ($_.Contains('"')) { throw 'Argumento contém aspas embutidas.' }
        '"' + $_ + '"'
    })
    $process = Start-Process -FilePath $unity -ArgumentList $quoted -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity falhou: $($process.ExitCode). Consulte log." }
}
```

## Suítes e conteúdo

```powershell
foreach ($suite in @('EditMode', 'PlayMode')) {
    Invoke-ValidationUnity @('-batchmode', '-projectPath', $copy, '-buildTarget', 'Win64', '-runTests', '-testPlatform', $suite, '-testResults', (Join-Path $results "$suite.xml"), '-logFile', (Join-Path $logs "$suite.log"))
    [xml]$xml = Get-Content (Join-Path $results "$suite.xml") -Raw
    $run = $xml.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -le 0 -or [int]$run.failed -ne 0 -or [int]$run.skipped -ne 0) { throw "Suíte não aprovada: $suite" }
}
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Win64', '-executeMethod', 'QuackQuack.Editor.ContentValidationWindow.ValidateForCI', '-logFile', (Join-Path $logs 'content.log'))
if (!(Select-String -LiteralPath (Join-Path $logs 'content.log') -Pattern 'T073_CONTENT_VALIDATION_OK' -Quiet)) { throw 'Validação de conteúdo sem sucesso.' }
```

Não passar -quit a -runTests. Durante implementação, usar -testFilter com a classe da task e registrar falha esperada antes da correção quando previsto. Contagens antigas 465/258/34 não são meta fixa; todas as novas classes devem estar descobertas, sem suites vazias/ignoradas.

Após criar testes de runtime portáveis, executar nos players reais:

```powershell
$runtimeFilter = 'PlatformBuildSmokeTests;InputParityTests;IntegratedUiLayoutTests;ArenaContainmentTests;ControllerAvailabilityTests;PickupAttractionTests;SpecialEnemyBarsTests'
foreach ($target in @(
    @{ Build = 'Win64'; Platform = 'StandaloneWindows64'; Name = 'windows' },
    @{ Build = 'Android'; Platform = 'Android'; Name = 'android' }
)) {
    Invoke-ValidationUnity @('-batchmode', '-projectPath', $copy, '-buildTarget', $target.Build, '-runTests', '-testPlatform', $target.Platform, '-testFilter', $runtimeFilter, '-playerHeartbeatTimeout', '120', '-buildPlayerPath', (Join-Path $copy "Builds/Tests002/$($target.Name)"), '-testResults', (Join-Path $results "$($target.Name)-runtime.xml"), '-logFile', (Join-Path $logs "$($target.Name)-runtime.log"))
    [xml]$runtimeXml = Get-Content (Join-Path $results "$($target.Name)-runtime.xml") -Raw
    if ($runtimeXml.'test-run'.result -ne 'Passed' -or [int]$runtimeXml.'test-run'.total -le 0 -or [int]$runtimeXml.'test-run'.skipped -ne 0) { throw 'Revisar player e descoberta de todas as classes esperadas.' }
}
```

O filtro contém classes novas planejadas, não disponíveis antes das tasks. Conferir descoberta por classe além do total positivo para não aprovar execução parcial silenciosa. Setup/fixtures de runtime não podem depender de UnityEditor. Para testes Android, conectar exatamente um aparelho ADB autorizado como no guia 001. Registrar modelo/OS/entrada, nunca serial.

Na T048, a seleção ampliada também inclui CartoonActorVisualTests, CombatArtTests, LakeWaterAnimationTests, BubbleAreaTests, AuraOcclusionTests, PassiveIntegrationTests, QuackFeedbackTests e UpgradeFeedbackTests. As duas últimas usam as cenas legadas Bootstrap/Lake: preserve essas cenas habilitadas na lista do **test player**, junto de RunBootstrap/LakeRun. Não substitua a lista do test player apenas pelas cenas de produção. O PrototypeBuildPipeline continua selecionando explicitamente somente RunBootstrap/LakeRun para o produto real.

Capturas das barras especiais e da matriz de interface usam a pasta temporária da aplicação nos players (gravável no Android). No Editor, o destino pode ser isolado por execução usando a variável de ambiente `QUACKQUACK_UI_CAPTURE_DIR`; não suponha que uma variável do host Windows será herdada pelo app Android. Os totais finais da T048 e a descoberta por classe estão em [validation-report](validation-report.md).

Build de produto, separada do test player:

```powershell
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Win64', '-executeMethod', 'QuackQuack.Editor.PrototypeBuildPipeline.BuildWindows', '-logFile', (Join-Path $logs 'windows-build.log'))
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Android', '-executeMethod', 'QuackQuack.Editor.PrototypeBuildPipeline.BuildAndroid', '-logFile', (Join-Path $logs 'android-build.log'))
```

Saídas do pipeline: Builds/Windows/QuackQuack.exe e Builds/Android/QuackQuack.apk dentro da cópia. Verificar marcador de sucesso, iniciar produto real e testar RunBootstrap/LakeRun; teste de player não confirma gameplay da build final.

## Roteiro por história

| História | Ação | Resultado obrigatório |
|---|---|---|
| US1 | Horda reproduzível pressiona cada borda, canto, pedra e arena do chefe; remover parte da horda | Corpo permanece dentro; movimento volta ao surgir espaço; preservar pressão. Guardar causa, seed e condições antes/depois |
| US2 | Conectar/desconectar/desabilitar controle durante gameplay, pausa, relíquias, melhoria e resultados; navegar só por controle | Touch condicional, sem comandos presos; foco/confirmar/voltar/atalho/perfil funcionam; pausa não muda dono |
| US3 | Cada passiva por nível, repetir resolução, terreno, relíquias e evoluções; medir Bolhas antes/depois; aura atrás de parede; bumerangue ida/volta | Seis efeitos reais, Bolhas 1,16× e 1,80× raio, visual correspondente; aura dentro sim/fora não; um hit por trecho |
| US4 | Mortes comuns/miniboss/chefe; vida cheia/parcial/morta; saturação; pickup na fronteira e fora do raio; afastar/pausar | XP sempre em comuns/minibosses; chefe sem XP nem cura; cura comum só sorteio de 10%, miniboss uma cura 50%, chefe sem cura automática; sem perda/duplicação; captura 1,26 base e velocidade 7,5 base; cura já capturada é descartada se a vida ficar cheia, sem voltar ao chão |
| US5 | Múltiplos níveis, último limiar, dicas com pausa/troca de input, especiais próximos e mortos/reusados | XP/nível corretos; duas barras sem números/terreno; dicas 10s sem reset; barras individuais acima das entidades |
| US6 | Cena-amostra e depois todos os assets; jogar sem explicar silhuetas/ícones | Elenco/colisão reconhecíveis, estilo aprovado, armas diferenciadas além da cor; água não esconde perigos |

Probabilidade: usar RNG injetado para testar fronteira 0,10 e replay de derrotas. Não esperar exatamente uma cura em cada dez mortes reais, nem aprovar estatística por uma pequena amostra. Em gameplay registrar frequência/quantidade sem alterar regra para atingir quota.

## Matriz física e visual

- Windows: teclado, Xbox, PlayStation e genérico suportado. Android: toque e cada família suportada com hotplug real. Se dispositivo ausente, marcar pendente, sem extrapolar testes sintéticos.
- 16:9 (1920×1080), 18:9 (2160×1080), 19,5:9 (2340×1080), 4:3 (1440×1080). Insets sintéticos dos dois lados e Android em ambas as paisagens com rotação do sistema habilitada.
- Revisar recursos vazios/parciais/cheios, prévia Bolhas, dicas, XP/nível, especiais sobrepostos, relíquias/cartas/resultados, perfil de controle e touch ausente/presente. Marcadores de estado não dependem só da cor.

## Profiling e nova avaliação

Reproduzir protocolo [T074](../001-prototipo-lago/test-report-t074.md) e benchmark PerformanceBenchmark existente com 200/400 inimigos. Acrescentar água/animações/aura e maiores taxas autorais de XP/cura/atração, além de partida 15 minutos sem coletar para estressar ledger/views. Registrar FPS, frametime p95, CPU/GPU/memória/GC, pendências e latência de materialização. Medir baseline e final nas mesmas condições, sem supressão silenciosa de recompensas para melhorar resultado.

Windows/intermediário: alvo 60 FPS com opção 30; entrada: 30 estáveis. S23 isolado não demonstra essas classes de Android. Falta de hardware é pendência, nunca aprovação. Reportar regressão relevante e corrigir/retestar antes de aceite.

Instrumentação T049: `PolishPerformanceBuild.BuildWindows` / `BuildAndroid`, somente em cópia isolada sem `.git`, geram players Development IL2CPP com entrada própria. A build normal de PrototypeBuildPipeline não inclui essas cenas. No Windows, passe `-profileScenario benchmark60|stress60|stress30|run60|run30` e `-profileOutput <arquivo-absoluto.csv>`; benchmark30 também é aceito. No Android, grave o nome do cenário em `Application.persistentDataPath/t049-mode.txt` antes de iniciar o app `com.cemggames.quackquack.profile002` (sem arquivo, benchmark60). `run60` dura até 900 segundos de simulação sem acelerar o tempo; a carga é sintética, não um playtest normal. `stress` substitui a produção autoral da onda por reposição 200/400 e usa ledger diagnóstico de 32.768, preservando política e RNG; `run` mantém o ledger autoral. Documentar separadamente as capacidades e a interferência do observador.

Saídas Android ficam em `Application.temporaryCachePath`; no S23 desta rodada isso é `/sdcard/Android/data/com.cemggames.quackquack.profile002/cache`, não o cache interno de `run-as`. Use `adb pull` e valide cabeçalho CSV/JSON: uma mensagem de arquivo ausente recebida por `exec-out` não é captura válida. Benchmark usa `T074-profile.csv` e sufixos `.json`, `.gpu.csv`, `.markers.csv`; cenários LakeRun usam `T049-<cenário>.csv`, `.json` e `.run.csv`. O censo pós-medição acrescenta `.memory.json`, `.types.csv` e `.objects.csv`; não somar contadores de domínios diferentes. `.run.csv` distingue objetos físicos e `batched`; o pool de 272 não é mais o total de recompensas apresentadas. Conferir contagem real de inimigos (alvo não basta), duração, falta de sobrescrita, exceções, foco, `VisualOnly=false` e coerência final das recompensas. Pilotos visuais marcados são excluídos. Os números, hashes, capturas descartadas e limitações da rodada estão em [performance-report](performance-report.md); T049 permanece pendente onde não houve aceite.

Para retestar, aguarde todos os Editors/compiladores antes das medições Windows e da instalação Android: o encerramento do Editor pode parar o ADB/Gradle compartilhado. Não faça builds Android simultâneas. Deixe o S23 desbloqueado antes de abrir o player (ele mantém a tela ligada durante a captura) e sem outros apps em uso. A referência provisória aprovada de 400 MiB vale somente para Total Used Memory amostrado no Windows/S23, incluindo buffers de diagnóstico, preservando a arte; não aprova RSS completo nem Android intermediário/de entrada.

Rodada com pelo menos cinco participantes conforme spec SC-007 e [formulário existente](../001-prototipo-lago/playtest-form.md), estendido aos MEL e critérios 1–15: identificação sem explicação, ações do controle, cura/coleta, alcance real, percepção de Bolhas, justiça sob pressão e vontade de repetir. Cobrir cada arma/evolução ao longo da rodada; registrar falhas e retestar. O novo relatório não encerra T075 antiga automaticamente.

Relatórios futuros: baseline.md, arena-diagnosis.md, validation-us1.md até validation-us6.md, performance-report.md, playtest-report.md e validation-report.md nesta feature; screenshots/logs/XML são anexos locais regeneráveis referenciados por execução/commit, não assets do produto. Não criar relatórios com aprovação fictícia antes de executar.


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](decisions.md) e o relatório T030.
