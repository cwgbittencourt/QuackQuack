# Quickstart de Validacao: Prototipo Jogavel do Lago

## Objetivo

Validar o vertical slice contra [spec.md](spec.md), os estados de [data-model.md](data-model.md) e os
contratos de [contracts/gameplay-contracts.md](contracts/gameplay-contracts.md).

## Estado observado e limites (T077, 29/10/2026)

**Retomada T075 em 29/10:** as três falhas visuais abaixo descrevem o APK antigo.
O layout integrado foi corrigido e confirmado no S23, com botão único da partida
alternando PAUSAR/CONTINUAR. A rotação foi confirmada nas duas posições horizontais,
com autorrotação do Android habilitada. A rodada final passou 465/465 EditMode,
258/258 PlayMode e 34/34 testes em cada player IL2CPP Windows e Android.
A matriz de UI e os snapshots estão na seção de retomada do [relatório](validation-report.md).

Esta revisão documenta as execuções de 28/10/2026; não representa nova medição.
Base de gameplay T074: `54b22ca`; validação T075 e fixture portátil: `21c29b7`;
formulário T076: `5a53065`. Consulte o histórico antes de comparar uma nova build.

| Ambiente ou verificação | Resultado observado |
|---|---|
| Editor Unity 6000.4.3f1 | 465/465 EditMode e 254/254 PlayMode |
| Players IL2CPP Windows x64 e Android ARM64 | 30/30 em cada plataforma, seleção de runtime abaixo |
| Conteúdo | 63 ScriptableObjects, zero erros, dois avisos UnsupportedType do URP |
| Windows 11, Ryzen 7 5800X3D, RTX 4070 Ti, aproximadamente 64 GB RAM | Build de produto compilada/iniciada; movimento, Quack e pausa físicos confirmados com teclado e controle (modelo não informado) |
| S23 Ultra SM-S918B, Android 17/API 37, Adreno 740/Vulkan, 11044 MB RAM | Build ARM64 instalada/iniciada; joystick + Quack simultâneos e pausa/retomada confirmados por toque |
| UI Android de produto | **Reprovada**: Relíquias ausente; Quack encobre Pausar/Abandonar; pausa móvel encobre relógio |
| Benchmark, 400 inimigos, alvo 60 | Windows: 59,86 FPS, p95 16,668 ms; S23: 59,93 FPS, p95 16,673 ms; zero GC.Alloc nas fases finais |

O benchmark usou seed 71, Windows 1280×720/D3D12 e S23 3088×1440/Vulkan.
Windows registrou duas perdas de foco; p99 foi 17,095 ms no Windows e 16,760 ms no
S23. Não significa todos os frames abaixo de 16,6667 ms. Memória inclui probes e
Development; não há orçamento quantitativo de memória aprovado. S23 é flagship,
não evidência de aparelho intermediário/de entrada.

GameBootstrap solicita 60 FPS por padrão; `limitTo30Fps` no Inspector seleciona
30 na composição. É configuração autoral, sem menu de preferência do jogador,
e define um alvo, não uma garantia de desempenho.

Fontes e identidades dos artefatos: [perfil T074](test-report-t074.md) e
[matriz T075](validation-report.md). T075 continua aberta: corrigir as três falhas,
revisar todos os menus nas quatro proporções/recortes e entradas, executar partida
real de 15 minutos e medir as classes de Android restantes. O
[formulário T076](playtest-form.md) está pronto; SC-001 a SC-005 ainda não têm coleta.

## Pre-requisitos

- Unity Hub com Unity 6000.4.3f1, Windows Build Support e Android Build Support.
- Android SDK, NDK e OpenJDK instalados pelo Unity Hub.
- PowerShell 7 no Windows x64; Python 3 para resumir capturas de profiling.
- Licença Unity disponível no usuário que executa os comandos. Para IL2CPP Windows,
  toolchain C++ e Windows SDK compatíveis com o módulo instalado.
- Android conectado por cabo de dados, depuração USB e autorização RSA no aparelho.
  Para fechar as metas, também são necessários Android intermediário e de entrada.
- Clone com Assets, Packages e ProjectSettings completos, incluindo os `.meta`.
  Os assets/cenas já estão versionados; não executar geradores para apenas jogar.

## Caminho executável de validação

Os blocos PowerShell desta seção são sequenciais, na mesma sessão, iniciada na
raiz do clone. Escolha uma pasta nova para cada rodada; não reutilize binários ou
XML antigos como evidência de uma execução atual. Feche o Editor dessa cópia antes
dos comandos em lote e espere cada processo terminar. Não rode duas instâncias
Unity sobre o mesmo projeto. Não use `-nographics` para inspeção visual ou profiling.

### 1. Criar cópia isolada e definir ferramentas

O exemplo não depende de Temp/T033Validation nem do helper local T075LocalBuild,
que não são distribuídos pelo Git. Copia fontes e `.meta`, sem Library ou `.git`.

```powershell
$repo = (Get-Location).Path
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Unity.exe'
$adb = 'C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
$copy = Join-Path $repo 'Temp/T077Validation'
$logs = Join-Path $repo 'Logs/T077'
$results = Join-Path $repo 'TestResults/T077'
if (!(Test-Path -LiteralPath $unity)) { throw 'Ajuste o caminho do Unity 6000.4.3f1 instalado.' }
if (!(Test-Path -LiteralPath 'ProjectSettings/ProjectVersion.txt')) { throw 'Execute na raiz do projeto.' }
if ((Get-Content -LiteralPath 'ProjectSettings/ProjectVersion.txt' -Raw) -notmatch '(?m)^m_EditorVersion: 6000\.4\.3f1\s*$') { throw 'Versão do projeto diferente de 6000.4.3f1.' }
foreach ($path in @($copy, $logs, $results)) {
    if (Test-Path -LiteralPath $path) { throw "Escolha caminhos novos para cópia, logs e resultados; já existe: $path" }
}
New-Item -ItemType Directory -Force -Path $copy, $logs, $results | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    & robocopy (Join-Path $repo $folder) (Join-Path $copy $folder) /E /COPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS
    if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar $folder (robocopy $LASTEXITCODE)." }
}
function Invoke-ValidationUnity {
    param([string[]]$UnityArguments)
    $quoted = @($UnityArguments | ForEach-Object {
        if ($_.Contains('"')) { throw 'Passe argumentos sem aspas embutidas.' }
        '"' + $_ + '"'
    })
    $process = Start-Process -FilePath $unity -ArgumentList $quoted -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity terminou com código $($process.ExitCode); consulte o log." }
}
```

Abra **essa cópia** pelo Hub em Unity 6000.4.3f1 e aguarde os pacotes/importação.
Em Player Settings, configure **Product Name = QuackQuack Validation T077** e
**Android Application Identifier = com.cemggames.quackquack.validationt077**.
Configure Scripting Backend IL2CPP no Windows/Android e Target Architectures
somente ARM64 no Android. Salve as configurações e feche o Editor da cópia.
O pipeline de produto também aplica backend/arquitetura; os test players precisam
desses valores já configurados. Nome/pacote exclusivos isolam o save do jogo normal.
Não copiar ProjectSettings modificados, cenas temporárias ou saves de volta ao clone.

### 2. Executar suítes do Editor e validador

```powershell
Invoke-ValidationUnity @('-batchmode', '-projectPath', $copy, '-buildTarget', 'Win64', '-runTests', '-testPlatform', 'EditMode', '-testResults', (Join-Path $results 'editmode.xml'), '-logFile', (Join-Path $logs 'editmode.log'))
Invoke-ValidationUnity @('-batchmode', '-projectPath', $copy, '-buildTarget', 'Win64', '-runTests', '-testPlatform', 'PlayMode', '-testResults', (Join-Path $results 'playmode.xml'), '-logFile', (Join-Path $logs 'playmode.log'))
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Win64', '-executeMethod', 'QuackQuack.Editor.ContentValidationWindow.ValidateForCI', '-logFile', (Join-Path $logs 'content.log'))
if (!(Select-String -LiteralPath (Join-Path $logs 'content.log') -Pattern 'T073_CONTENT_VALIDATION_OK' -Quiet)) { throw 'Validador sem marcador de sucesso.' }
```

Não adicionar `-quit` aos testes: o Test Framework encerra o Editor ao concluir.
Código zero e marcador do validador devem acompanhar os XML; ausência de saída,
teste ignorado, contagem zero, falha de licença ou de compilação não é aprovação.

### 3. Conectar e identificar o Android

Ative depuração USB no aparelho e autorize este computador no diálogo exibido.
O roteiro requer **um único aparelho autorizado** no ADB para não deixar ambígua
a escolha automática do Test Framework. `unauthorized`/`offline` impedem prosseguir.
Se o Windows não detectar o aparelho, conferir cabo de dados e driver USB apropriado
antes de repetir; isso não é falha nem sucesso do jogo.

```powershell
if (!(Test-Path -LiteralPath $adb)) { throw 'Instale Android Build Support ou ajuste o caminho do ADB.' }
& $adb devices -l
$devices = @(& $adb devices | Where-Object { $_ -match '^\S+\s+device\s*$' })
if ($devices.Count -ne 1) { throw 'Conecte exatamente um Android autorizado para esta execução.' }
$device = ($devices[0] -split '\s+')[0]
& $adb -s $device shell getprop ro.product.model
& $adb -s $device shell getprop ro.build.version.release
& $adb -s $device shell getprop ro.build.version.sdk
```

Manter conectado e desbloqueado durante implantação/testes. Registrar modelo,
SO/API, entrada e condições na matriz; não publicar o serial do aparelho.

### 4. Executar a mesma seleção nos players IL2CPP

Os comandos compilam, iniciam e, no Android, instalam o **test player** na cópia
configurada. Exigem módulos/licença e aparelho; não são a build de produto.

```powershell
$filter = 'PlatformBuildSmokeTests;PerformanceTelemetryTests;ResultsSceneFlowTests;InputParityTests;FrameLimitUsesNominalOrOptionalLowRate;IntegratedUiLayoutTests'
foreach ($target in @(
    @{ Name = 'windows'; Build = 'Win64'; Platform = 'StandaloneWindows64' },
    @{ Name = 'android'; Build = 'Android'; Platform = 'Android' }
)) {
    Invoke-ValidationUnity @('-batchmode', '-projectPath', $copy, '-buildTarget', $target.Build, '-runTests', '-testPlatform', $target.Platform, '-testFilter', $filter, '-playerHeartbeatTimeout', '120', '-buildPlayerPath', (Join-Path $copy "Builds/T077Tests/$($target.Name)"), '-testResults', (Join-Path $results "$($target.Name)-runtime.xml"), '-logFile', (Join-Path $logs "$($target.Name)-runtime.log"))
}
foreach ($name in @('editmode', 'playmode', 'windows-runtime', 'android-runtime')) {
    [xml]$xml = Get-Content -LiteralPath (Join-Path $results "$name.xml") -Raw
    $run = $xml.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -le 0 -or [int]$run.failed -ne 0 -or [int]$run.skipped -ne 0) {
        throw "Revisar execução $name e seu XML."
    }
    [pscustomobject]@{ Suite = $name; Total = $run.total; Passed = $run.passed; Failed = $run.failed; Skipped = $run.skipped }
}
```

Na T075 essa seleção teve 30 casos em cada player: 15 de paridade sintética, cinco
de resultados/save, seis de telemetria, dois smoke e dois de alvo 60/30. O teste
de erro de save usa obstrução temporária do caminho próprio: bloquear um arquivo
aberto não induziu a mesma falha no Android. Não restaurar a fixture antiga nem
ignorar o caso para obter resultado verde.

### 5. Compilar, identificar e abrir o produto

Usar a cópia com nome/pacote de validação, não uma cópia já alterada pelo helper
de profiling. Os métodos abaixo existem no repositório e produzem somente
RunBootstrap/LakeRun, BuildOptions.None, Windows x64/Android ARM64 IL2CPP.

```powershell
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Win64', '-executeMethod', 'QuackQuack.Editor.PrototypeBuildPipeline.BuildWindows', '-logFile', (Join-Path $logs 'windows-product.log'))
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $copy, '-buildTarget', 'Android', '-executeMethod', 'QuackQuack.Editor.PrototypeBuildPipeline.BuildAndroid', '-logFile', (Join-Path $logs 'android-product.log'))
foreach ($name in @('windows', 'android')) {
    if (!(Select-String -LiteralPath (Join-Path $logs "$name-product.log") -Pattern 'PROTOTYPE_BUILD_OK' -Quiet)) { throw "Build $name sem marcador de sucesso." }
}
$windowsExe = Join-Path $copy 'Builds/Windows/QuackQuack.exe'
$apk = Join-Path $copy 'Builds/Android/QuackQuack.apk'
Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $copy 'Builds/Windows/GameAssembly.dll'), $apk
git -C $repo rev-parse HEAD
& $windowsExe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile (Join-Path $logs 'windows-player.log') | Out-Host
# Feche a janela Windows após a inspeção para continuar este bloco.
& $adb -s $device install -r $apk
if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar APK; não registrar execução como aprovada.' }
& $adb -s $device shell monkey -p com.cemggames.quackquack.validationt077 -c android.intent.category.LAUNCHER 1
if ($LASTEXITCODE -ne 0) { throw 'Falha ao iniciar o pacote de validação.' }
```

`install -r` preserva os dados desse pacote; registrar o perfil de save usado.
Os hashes novos não devem coincidir obrigatoriamente com os da T075, que usou outro
nome/pacote. Anotar commit, hash, seed e tipo de build por rodada. Não iniciar o
produto com identidade pessoal esperando o isolamento automático dos testes:
somente as fixtures injetam SavePathProvider temporário.

Conferir a partida com teclado (WASD/setas, Espaço, Esc), controle (stick esquerdo,
Sul/A/Cross, Start) e toque simultâneo. Revisar cartas, relíquias e resultados,
incluindo reabertura do aplicativo. Repetir HUD e modais em 16:9, 18:9, 19,5:9 e
4:3 e recortes reais, sem inferir legibilidade das asserções geométricas do HUD.
Na nova build Android, confirmar a correção de VAL-UI-01/02/03 e a rotação entre
as duas posições de paisagem, sem aprovar gates ainda sem evidência.
Para uma nova rodada de experiência, usar [playtest-form.md](playtest-form.md).

## Validacao automatizada

1. No Unity Test Runner, executar todos os testes EditMode.
2. Confirmar que regras de Folego, Quack, terreno, dano, evolucao, ofertas e transicoes de partida
   passam sem dependencia de cena.
3. Executar todos os testes PlayMode.
4. Confirmar entrada equivalente, pools, spawns, UI/Safe Area, chefe e tela final.
5. Executar o cenario de benchmark com seed fixa para 200 e 400 inimigos e exportar
   telemetria/timings/probes conforme Profiling T074. Snapshots do Memory Profiler,
   se coletados em uma rodada adicional, devem ser identificados separadamente.

Resultado esperado: zero falhas; zero alocacao recorrente atribuida a spawn, projetil, pickup ou VFX
durante a janela estavel; nenhuma instancia ativa simultaneamente no pool.

## Partida integrada disponível após T060

1. Abrir `Assets/QuackQuack/Scenes/RunBootstrap.unity` e entrar em Play. A cena abre
   LakeRun após inicializar os serviços. A lista de build já usa RunBootstrap como entrada.
2. Mover com WASD/setas, usar Quack com Espaço e pausar com Esc ou PAUSAR. A câmera
   acompanha o Pato. Inimigos entram fora da área visível, respeitando limites da onda.
3. Derrotar inimigos com armas/Quack e coletar XP ciano. Cartas congelam a simulação;
   confirmar/pular retoma a fase anterior. RELÍQUIAS abre o loadout e pausa.
4. Observar Libélula Alfa aos 5 minutos, Sapo-Cururu aos 10 e Sapo-Rei aos 15.
   O encontro final limpa a horda anterior. Os círculos verdes são refúgios contra
   ondas, destruídos na fase posterior. Marca e áudio antecedem os padrões do chefe.
5. Derrotar o chefe produz Won; vida zero produz Lost; ABANDONAR produz Abandoned.
   A tela T061 apresenta combate, build e recompensas/progresso em três colunas
   roláveis. Confira o estado do save; se falhar, use TENTAR NOVAMENTE antes de sair.
6. JOGAR NOVAMENTE inicia outra partida preservando o progresso salvo.
   Unlocks/equipamento carregam de `lake-relics.json` em Application.persistentDataPath.
   Alterações são salvas fora do combate; falha de save não bloqueia o encerramento.

O menu **QuackQuack > Build T060 complete lake run** é manutenção: atualiza bindings
nos prefabs existentes, gera settings e reconstrói LakeRun a partir do treino Lake,
preservando os GUIDs dos prefabs e mantendo Bootstrap/Lake. Usar após reconstruir
prefabs com os geradores T056/T057, que não possuem a composição T060. Os assets
estão versionados; não é necessário gerar nada para jogar um clone normal.

Testes: RunOrchestrationContractsTests (EditMode), RunDirectorFlowTests e
RunSceneFlowTests (PlayMode). Estes usam caminhos de save temporários próprios.
Capturas locais ignoradas: `Logs/t060-scene.png` e `Logs/t060-loadout.png`.
Ver [relatório T060](test-report-t060.md) para resultados e limites do aceite.

## Roteiro jogavel por marco

### Arena de treino disponível após T030

- Abrir Bootstrap e entrar em Play; usar WASD/setas, Espaço e Esc.
- O Pato inicia à esquerda. Aproximar-se do alvo vermelho e usar Quack para empurrá-lo
  contra a pedra; atravessar o lago central e verificar a diferença de mobilidade.
- Entrar no corredor à direita, usar Quack contra o alvo e os projéteis roxos, e coletar
  a recompensa dourada. A recompensa desaparece e concede recuperação de Fôlego uma vez.
- Pausar e retomar; projéteis, dano, recuperação e prazos de combate devem permanecer
  congelados na pausa. Após morte, sair de Play e iniciar novamente por Bootstrap.
- O HUD mostra vida/Fôlego e terreno no canto superior esquerdo, relógio à direita
  e mensagens na parte inferior. Dourado indica Fôlego cheio; a linha do Quack distingue
  pronto/aprimorado, recarregando e indisponível. Tentar Quack durante a recarga ou sem
  Fôlego deve mostrar o motivo. Pausa e fim do teste têm prioridade sobre mensagens temporárias.
- Quack aceito deve criar onda dourada e tom temporário. Acertos mostram impacto
  alaranjado; reflexão, pulso ciano; crítico, contorno branco que acompanha o alvo.
  Na pausa, animações e sons devem parar e depois continuar. Ao terminar o efeito,
  o objeto retorna ao pool; rejeições não devem gerar onda ou som de ativação.
  Este roteiro não representa aceite completo de US1 nem validação de plataforma.

Os assets e a cena estão versionados. O menu `QuackQuack → Build T030 lake arena`
é uma ferramenta de manutenção que reconstrói a raiz gerada da arena e atualiza os assets
temporários pelo Unity; não é necessário executá-lo para abrir um clone normal.
O menu `QuackQuack → Build T031 gameplay HUD` atualiza somente a composição do HUD
na cena existente. A ferramenta da arena também reconecta o HUD caso ele já exista.
O menu `QuackQuack → Build T032 Quack feedback` gera o prefab de efeito, os tons
temporários e a ligação dos eventos. Os assets estão versionados; não executar esses
menus em um clone normal para simplesmente jogar.

### Cartas e demonstração de progressão disponíveis após T044

- O Pato inicia com Bumerangue automático. Três pickups cianos de XP ficam em
  (-6, -4), (0, -2) e (5, -5). Cada um concede dez XP uma vez, usando o pool.
- Coletar um pickup abre até três cartas e congela o combate. Comparar tipo,
  efeito atual/próximo, sinergia e requisito de evolução antes de escolher.
- Clicar em Escolher confirma; Banir remove aquela opção da sessão. O rodapé
  informa as duas rerrolagens e o único banimento disponíveis. Pular não gasta
  esses recursos. O foco inicial permite confirmar com Enter; os botões também
  usam a navegação padrão do Input System UI.
- A resolução fecha o modal e retoma o combate; se houver níveis acumulados,
  apresenta a próxima oferta antes de retomar. Encerrar a partida oculta as cartas.
- Os três pickups comuns permitem três escolhas. Com a recompensa rara da T045,
  a demonstração totaliza até sete subidas de nível. Evoluções ainda exigem arma
  N5 e passiva compatível, e dependem das ofertas elegíveis. Este trecho não substitui
  a partida completa nem o aceite de efeitos/balanceamento de todas as passivas.

### Feedback de evolução e recompensa rara disponível após T045

- O pickup roxo em (-9, 2) concede 40 XP uma única vez e retorna ao pool.
- Após resolver as cartas, deve aparecer um losango roxo com RECOMPENSA RARA e
  um tom temporário. Uma evolução aceita mostra estrela dourada, EVOLUÇÃO! e outro
  tom. Melhorias comuns, pulo e comandos rejeitados não geram o sinal de evolução.
- Se várias notificações se acumularem durante as cartas, a evolução tem prioridade
  e os efeitos são reproduzidos um por vez. Pausar congela animação e áudio; encerrar
  a partida limpa efeitos ativos e pendentes.
- Repetir a coleta ou callbacks da mesma oferta não pode conceder XP/evolução de novo.
  Os testes também preparam uma build elegível para verificar as evoluções diretamente.

O menu `QuackQuack → Build T045 upgrade feedback` gera os prefabs, tons temporários
e ligação na cena existente. Assets já estão versionados; não executar para apenas
jogar um clone normal. A ferramenta T044 reconecta o feedback T045 quando ele já existe.

Os prefabs UpgradeSelection e ExperiencePickup e a ligação na cena estão
versionados. O menu `QuackQuack → Build T044 upgrade selection` reconstrói essa
composição pelo Editor, preservando a cena existente e suas demais raízes. Não
precisa ser executado para usar um clone normal. A prévia de manutenção usa
UpgradeSelectionSetup.Preview e não grava alterações de visualização na cena.

O catálogo de build está em `Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset`.
Contém seis armas básicas, seis passivas e três receitas; as três armas resultantes
ficam referenciadas pelas receitas e são excluídas das aquisições comuns. Os assets
já estão versionados; não é necessário executar geração para usar um clone.
O menu `QuackQuack → Build T043 prototype build catalog` é uma ferramenta de
manutenção que regrava o balanceamento inicial e reutiliza os assets existentes,
preservando GUIDs. Ele não modifica cenas nem altera a seleção de cartas.

### Relíquias disponíveis após T052

- Em Lake, clicar em RELÍQUIAS no topo: a arena pausa. Os dez itens mostram efeito,
  estado e condição de desbloqueio; até três podem ficar equipados.
- Botas do Lago, Garras de Junco e Pena de Combate começam disponíveis. Sobreviver
  15/30/45/60 segundos desbloqueia Pedra da Margem, Pérola do Lago, Apito Veloz e
  Lente de Orvalho. Coletar 10/20/30 XP libera Anel de Ondas, Concha de Fôlego e Pena Leve.
- Equipar aplica o bônus; REMOVER retira somente aquele bônus. Em três slots cheios,
  outras opções ficam indisponíveis até remover uma equipada. VOLTAR retoma se o
  próprio painel pausou o jogo; se já estava pausado, preserva a pausa.
- Equipar Garras de Junco e comparar movimento em terra; Botas do Lago funciona
  somente na água. Bônus de armas usam o terreno no disparo; projéteis já disparados
  preservam os atributos aceitos naquele disparo. Concha aumenta a recuperação de Fôlego.
- As dez definições e o catálogo estão em Content/Relics; todos os bônus iniciais
  são 5%. Esses números não representam balanceamento aprovado. Desbloqueios são
  desta sessão na arena atual; serviço de save disponível na T059, ligação automática na T060.
- O menu `QuackQuack → Build T052 relic loadout` é manutenção: gera assets e prefab
  pelo Unity, preservando GUIDs existentes e atualizando somente a raiz Relics T052
  na cena Lake. Não precisa executá-lo para jogar um clone normal.

### Catálogo de encontros disponível após T056

- Abrir `Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset` no Inspector.
  Verificar cinco comuns, Libélula Alfa, Sapo-Cururu, settings, seis ondas e três marcos.
- Prefabs temporários estão em `Prefabs/Prototype/Enemies`; as definições e as
  ondas estão em `Content/Enemies/Prototype` e `Content/Encounters/Prototype`.
- O menu `QuackQuack → Build T056 lake encounter catalog` é manutenção: regrava
  o catálogo e dados iniciais preservando GUIDs. Assets já estão versionados e
  não precisam ser gerados para abrir um clone normal.
- A prévia de manutenção `LakeEncounterCatalogSetup.Preview` rende uma galeria
  sem salvar cena. Os testes usam os assets reais para criar pools/diretor e
  verificar os dois minibosses com o scheduler compartilhado.
- A composição com ondas, chefe e encerramento está disponível em RunBootstrap
  desde T060. Bootstrap continua abrindo a arena de treino existente.

### Marco 1 - Fundamentos

- Abrir `Bootstrap`, iniciar `Lake` e mover o Pato com teclado, controle e toque.
- Alternar 16:9, 18:9, 19,5:9 e 4:3; verificar paisagem e Safe Area.
- Gerar e iniciar uma build Windows e uma Android.

### Marco 2 - Quack e terreno

- Comparar velocidade e recuperacao de Folego em agua e terra.
- Usar Quack vazio, parcial e cheio; validar rejeicao, efeito normal e efeito aprimorado.
- Empurrar, interromper e refletir alvos elegiveis; verificar feedback correspondente.

### Marco 3 - Combate e escala

- Enfrentar os cinco inimigos isoladamente e em composicoes.
- Confirmar Formiga, Aranha, Vespa, Besouro e Caramujo Aquatico e enfrentar Libelula Alfa no minuto 5
  e Sapo-Cururu no minuto 10.
- Elevar a densidade para 200 e 400 ativos; registrar FPS, frame time, GC e memoria.
- Verificar que inimigos, projeteis, pickups e VFX retornam limpos aos pools.
- Confirmar ausencia de `Update` individual por inimigo no perfil de hordas.

### Marco 4 - Builds

- Forcar subidas de nivel; validar tres cartas, duas rerrolagens, um banimento e pular.
- Maximizar cada arma e testar as tres combinacoes evolutivas.
- Confirmar que cartas explicam efeito, sinergia e requisito sem bloquear o combate apos a escolha.

### Marco 5 - Partida completa

- Jogar 15 minutos com seed registrada.
- Confirmar dois minibosses antes do chefe e todas as telemetrias do Sapo-Rei.
- Finalizar por vitoria, morte e abandono; comparar tela final e recompensas.
- Fechar e reabrir o jogo; confirmar recordes, desbloqueios e reliquias.
- Desbloquear, equipar zero a tres reliquias e confirmar aplicacao/remocao idempotente dos efeitos.

### Marco 6 - Aceite

- Executar a matriz completa de entrada, tela e plataforma.
- Conduzir playtest e calcular SC-001 a SC-008.
- Aprovar somente se os criterios tecnicos e de experiencia forem atendidos ou se desvios estiverem
  documentados com tarefa corretiva.

## Treino isolado do Sapo-Rei (T057)

Abra `Assets/QuackQuack/Scenes/FrogKingPractice.unity` no Unity 6000.4.3f1 e entre
em Play. Mova o Pato amarelo com WASD/setas. O círculo verde maior é o chefe;
os três círculos verdes escuros protegem das ondas. O salto marca a posição do
Pato antes de aterrissar, portanto sair da marca evita o dano.

Use **Aplicar 10% de dano ao chefe** para explorar as fases: saltos/ondas no
início, girinos a partir de 70% da vida e destruição de refúgios a partir de 35%.
Observe o círculo de aviso e o áudio antes de cada execução. Girinos perseguem
o Pato e somem ao contato ou por duração; ondas expandem a partir do ponto marcado.
Um refúgio destruído deixa de proteger. Vida zero do chefe mostra Won; vida zero
do Pato mostra Lost; o botão de abandono mostra Abandoned. Saia de Play para reiniciar.

Essa fixture é exclusiva do Editor e fica fora da lista de cenas da build.
O botão de dano serve para testar fases. A partida RunBootstrap integra armas/Quack
e o pedido aos 900 segundos desde T060. Bootstrap/Lake continua sendo treino.
Para regenerar os assets e a fixture, use **QuackQuack > Build T057 Frog King**.
O gerador preserva os GUIDs existentes; a cena de treino é reconstruída.

Evidência automatizada: [relatório T057](test-report-t057.md). A captura em
`Logs/t057-practice.png` mostra a geometria do salto na arena; não inclui o
overlay OnGUI. Realizar playtest humano antes de aprovar percepção e justiça.

## Resultado de domínio disponível após T058

No Test Runner, execute `RunResultTests` e `RunResultStatisticsTests` em EditMode:
os 48 casos verificam resultado único, os três finais, abandono parcial e os
campos completos. A sessão recebe fatos aceitos e captura a build no encerramento;
o resumo continua igual mesmo se a build/loadout original mudar depois.
Consulte o [relatório T058](test-report-t058.md) e o contrato de resultado para a API.

A T058 entrega o modelo e acumulador em Core. A T060 coleta os eventos reais
em RunBootstrap; a apresentação completa está disponível desde T061. O serviço T059
também está ligado ao ciclo da partida para relíquias.
O produtor deve atribuir o último dano/derrota/recompensa antes da transição terminal,
pois fatos posteriores são rejeitados. Consultar o resultado não credita carteira
nem grava progresso.

## Serviço de save disponível após T059

No Test Runner, executar `SaveServiceTests` e `JsonSaveWriteTests` em EditMode
(39 casos) e `RelicSaveRoundTripTests` em PlayMode. As fixtures usam diretórios
temporários exclusivos e removem somente seus próprios dados, com caminho validado.
Não leem nem alteram saves pessoais.

`JsonSaveService(caminho, idsConhecidos)` recebe os dez IDs do catálogo. `Load()`
retorna Data, Status e Diagnostic; Data contém versão, desbloqueios e equipamento.
Reaplicar os IDs usa Unlock/Equip, respeitando os três slots e a idempotência do
loadout. `Save(loadout)` grava um snapshot, preservando a ordem equipada.
`RestoreBackup()` promove explicitamente o backup validado; Load apenas o consulta.

O schema atual é 2; versões 0/1 migram relíquias com perfil de progresso vazio.
Versão 0 usa equipamento vazio se ausente. Versão futura no principal impede gravação/restauração por esta versão.
Sem principal/backup utilizável, Load retorna padrões vazios para o jogo continuar.
Falhas de escrita retornam Failed com diagnóstico, sem expor caminho ou conteúdo.
O serviço não faz I/O por frame nem determina sozinho o caminho pessoal.

O treino Bootstrap mantém relíquias por sessão. RunBootstrap define o caminho local
e os momentos de carregar/salvar desde T060; carteira, recordes e desafios estão no schema 2 desde T061. Ver [relatório T059](test-report-t059.md).

## Evidencias a conservar

- Relatorio do Test Runner para EditMode e PlayMode.
- Capturas das quatro proporcoes e da Safe Area em Android.
- Perfis de CPU, GPU, memoria e GC nos picos de 200 e 400 inimigos.
- Identificador da build, aparelho, configuracao, seed e resultado de cada playtest.
- Planilha ou formulario consolidando compreensao, justica percebida e repeticao espontanea.

## Tela final e progresso (T061)

O menu **QuackQuack > Build T061 results screen** gera os assets pelo Unity;
o gerador T060 reconecta a tela quando seu asset de progressão existe. Os assets
estão versionados; não é necessário regenerar para jogar um clone normal.
ResultsProgression.asset contém metas provisórias editáveis: 100 inimigos acumulados,
15 minutos em uma partida e uma vitória contra o Sapo-Rei. Não concedem moedas extras.

Execute ResultsContractTests, RunProgressionTests e ProgressSaveTests em EditMode;
ResultsViewTests e ResultsSceneFlowTests em PlayMode. Cobrem três finais, retry e
reinício preservando saldo. Capturas Logs/t061-*.png são fixtures de UI a 1280×720,
não aceite de gameplay, Safe Area ou plataformas. Ver [relatório T061](test-report-t061.md).

## Testes de paridade disponíveis após T062

No Test Runner PlayMode, filtrar InputParityTests. São 15 casos. Após T066, todos devem passar com os adaptadores reais das três entradas.
Esses casos não substituem o aceite de entrada física e interface da US4.

Os testes usam teclado/gamepad sintéticos e callbacks dos adaptadores móveis de produção;
não exigem dispositivos físicos. Comparam comandos e checkpoints do PlayerController
real em passos controlados, incluindo pausa, terreno, Fôlego e os três finais.
O contrato de callbacks e os limites constam em contracts/gameplay-contracts.md.
Depois de T065/T066, repetir os 15 casos e a suíte completa. T063/T064 continuam
responsáveis por UI responsiva e smoke tests de plataforma.

## Testes de UI responsiva disponíveis após T063

No Test Runner PlayMode, filtrar ResponsiveUiTests. Os 15 casos usam o HUD autoral
em 1920×1080, 2160×1080, 2340×1080 e 1440×1080, com câmera/render target próprios.
Não alteram a resolução do desktop nem gravam cenas. A fixture espera o layout e
confirma a projeção do Canvas antes de medir os oito elementos essenciais.

Quatro casos verificam o viewport sem recortes. Sete verificam o controlador T068:
normalização nas quatro proporções, mudança de resolução/recorte, idempotência,
remoção de margens, geometria inválida, reativação e retorno às fontes Screen.
Quatro verificam o CanvasScaler autoral 1920×1080 da T069 e a
hierarquia segura com os oito elementos essenciais dentro dos recortes.
Nenhum teste é ignorado por faltar implementação.

Para usar SafeAreaController, coloque-o em um RectTransform filho direto de um
Canvas de tela inteira e mova o conteúdo protegido para baixo dele. O componente
lê Screen.safeArea e atualiza âncoras/offsets automaticamente. O HUD de Lake e LakeRun já está configurado desde a T069. SetViewportProvider permite fontes de teste; null restaura
Screen. Ver contrato e [relatório T068](test-report-t068.md).

Depois de T068/T069, repetir a matriz e a suíte completa. Geometria do HUD e altura
de texto não substituem inspeção humana de todas as telas, recortes Android reais,
Safe Area de aparelho ou aceite de legibilidade da T075.

## Smoke tests de build e inicialização (T064)

No Test Runner PlayMode, executar PlatformBuildConfigurationSmokeTests e
PlatformBuildSmokeTests. Desde T070, os nove casos de configuração validam o pipeline de produção. Os dois de runtime verificam as
cenas de produção e inicialização de RunBootstrap/LakeRun com save temporário.
Passar no Editor não comprova inicialização no Windows Player ou no Android.

Para compilar e executar somente a fixture de runtime em test players, usar a
mesma versão Unity e uma cópia isolada do projeto, com os módulos e toolchains do
alvo instalados. Os players devem usar IL2CPP; Android deve estar configurado ARM64.
Exemplos de argumentos para Unity.exe (aguardar o processo e conferir XML/ExitCode):

~~~text
-batchmode -projectPath <copia-isolada> -runTests -testPlatform StandaloneWindows64 -testCategory PlatformRuntimeSmoke -buildPlayerPath <pasta-de-build> -testResults <windows.xml> -logFile <windows.log>
-batchmode -projectPath <copia-isolada> -runTests -testPlatform Android -testCategory PlatformRuntimeSmoke -buildPlayerPath <pasta-de-build> -testResults <android.xml> -logFile <android.log>
~~~

No Android, o segundo comando requer aparelho autorizado e conectado e constrói,
instala e executa o test player nele. O Test Framework inclui sua própria cena de
entrada; a ordem de cenas da build de produto é verificada pelo contrato da T070.
Artefatos de build, XML e logs devem ficar nas pastas ignoradas. Não incluir -quit
antes do término dos testes. Não transformar ausência de módulo/aparelho ou falha
de build em teste aprovado. Ver [relatório T064](test-report-t064.md).

## Adaptador de controle disponível após T065

Execute InputParityTests e GamepadInputAdapterTests no Test Runner PlayMode.
O adaptador usa stick esquerdo, botão Sul para Quack e Start para pausa; aplica a
deadzone configurada no Input System, preserva intensidade parcial e consome bordas
uma vez. Os testes usam gamepad sintético, incluindo reset e remoção/reconexão.

GamepadInputAdapter implementa IPlayerInput e pode ser injetado em
PlayerController.Initialize; quem o cria deve descartá-lo com Dispose. Desde T067, as cenas usam UnifiedPlayerInput e selecionam a fonte mais recente.
Na T075, comandos básicos com controle físico foram confirmados pelo usuário;
modelo não informado e navegação completa de menus ainda pendente. Ver
[relatório T065](test-report-t065.md) e [matriz T075](validation-report.md).

## Controles móveis disponíveis após T066

O prefab Assets/QuackQuack/Prefabs/Prototype/MobileControls.prefab contém joystick,
QUACK e uma pausa de fallback, ligados ao MobileInputAdapter. Na partida integrada,
o fallback amarelo fica oculto e o botão da partida alterna PAUSAR/CONTINUAR.
Pode ser instanciado para revisar
os controles no Editor. Desde T067, a entrada padrão instancia o prefab quando
há Touchscreen ou plataforma móvel, em Bootstrap/Lake e RunBootstrap/LakeRun.
Injetar o adapter em PlayerController.Initialize permite consumir seus comandos.
O mapa unificado está disponível desde T067 e o HUD tem Safe Area desde T069.
A composição completa com controles móveis foi reprovada no S23 na T075;
os testes isolados de HUD/prefab não cobrem as sobreposições entre eles.

Arrastar a base move o knob; soltura retorna ao centro. Cada controle captura seu
dedo, permitindo movimento e botões simultâneos. Cancelar, ocultar controles,
perder foco ou suspender descarta entradas pendentes. O menu **QuackQuack > Build
T066 mobile controls** é manutenção; os assets já estão versionados.

Execute InputParityTests, MobileInputAdapterTests e MobileTouchscreenTests em
PlayMode. O último usa Touchscreen sintético e raycasts reais no prefab e gera a
captura ignorada Logs/t066-controls.png. Os testes de eventos/Editor não representam
aceite em aparelho Android. Ver [relatório T066](test-report-t066.md).

## Entrada unificada disponível após T067

QuackQuackInput.inputactions define o mapa Gameplay e as ações Move, Quack e Pause,
com grupos/esquemas Keyboard, Gamepad e Touch. Teclado usa WASD/setas, Espaço e Esc;
controle usa stick esquerdo, Sul e Start; toque usa o prefab móvel. A configuração
Resources/PlayerInput.asset referencia esse asset e MobileControls.prefab. Não é
necessário executar geradores em um clone normal.

O Pato das cenas padrão usa UnifiedPlayerInput. A última atividade não neutra
escolhe uma fonte; vetores não são somados, e um dispositivo mais antigo mantido
não volta a mover sozinho quando a fonte recente é solta. Quack/Move são desativados
na pausa; Pause continua disponível para retomar. Durante cartas e resultados,
ações de gameplay são desativadas e controles móveis ocultos. Após menus/foco,
movimento precisa voltar ao neutro; botões precisam de nova ativação.

O EventSystem continua ativo com os controles móveis ocultos. A entrada UI do Input
System permanece independente das ações de gameplay. O menu **QuackQuack > Build
T067 unified input** cria os assets ausentes e reconecta a configuração, preservando
bindings/IDs do asset existente. Os adaptadores usam clones em runtime.

Execute UnifiedInputContractTests e UnifiedPlayerInputTests, além das fixtures de
paridade e adaptadores. A categoria UnifiedInputRuntime também pode ser usada nos
test players. Ver [relatório T067](test-report-t067.md) para evidências Windows e
limites. A T075 confirmou comandos básicos em Android real; Safe Area e composição
integral ainda não têm aceite, e três falhas visuais foram registradas.

Na composição T067, navegação/Submit/Cancel da UI ficam habilitados somente nos
menus, enquanto cliques e toque continuam disponíveis no gameplay. Assim, Sul
não confirma acidentalmente um botão UI ao usar Quack. Perda de foco limpa entradas
antigas e respeita a política de background do Input System; suspensão do app
continua bloqueando entrada mesmo se a política permitir input em background.

## HUD responsivo disponível após T069

Lake e LakeRun já contêm CanvasScaler com referência 1920×1080 e correspondência
pela altura. Vitals, relógio e mensagens ficam sob HUD Safe Area: o controlador
reposiciona seus limites automaticamente, sem alterar as regras do jogo.
O menu **QuackQuack > Build T069 responsive HUD** reaplica a configuração nas duas
cenas existentes. Reconstruir o HUD por GameplayHudSetup também mantém esse layout.

Execute ResponsiveUiTests para a matriz 16:9, 18:9, 19,5:9 e 4:3, com recortes
assimétricos. Os quatro casos autorais geram capturas ignoradas
Logs/t069-hud-<largura>x<altura>.png na cópia de validação. RunSceneFlowTests confere
a presença da mesma hierarquia no fluxo de produção, com save temporário isolado.
Consulte o [relatório T069](test-report-t069.md); o aceite integral de UI/aparelhos
continua na T075.

## Builds de produto disponíveis após T070

O menu **QuackQuack > Build prototype** contém Windows x64 e Android ARM64 APK.
Selecione o alvo correspondente no Editor antes de executar. Para automatizar,
use Unity 6000.4.3f1 em uma cópia isolada do projeto:

~~~text
-batchmode -quit -buildTarget Win64 -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildWindows -logFile <windows-build.log>
-batchmode -quit -buildTarget Android -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildAndroid -logFile <android-build.log>
~~~

As saídas relativas à cópia são Builds/Windows/QuackQuack.exe e
Builds/Android/QuackQuack.apk. Ambos empacotam somente RunBootstrap e LakeRun e usam
IL2CPP; Android usa apenas ARM64. ConfigureTarget não troca o alvo ativo. A build
Android gera APK, não App Bundle, e precisa de SDK/NDK/JDK e dependências Gradle.
O comando registra PROTOTYPE_BUILD_OK somente após BuildResult.Succeeded; confira
código de saída e log. Build/ConfigureTarget deixam as configurações aplicadas no
projeto; nenhum desses comandos altera a lista global de cenas do Editor.

O pipeline não instala nem inicia o jogo. Smoke tests de runtime usam as entradas
separadas do Test Framework descritas acima, com save isolado. As configurações
podem ser validadas por PlatformBuildConfigurationSmokeTests, sem compilar ou
instalar players. Ver [relatório T070](test-report-t070.md) para resultados observados.

## Cenário de benchmark disponível após T071

Abra Assets/QuackQuack/Scenes/PerformanceBenchmark.unity e entre em Play. A cena
é independente de GameBootstrap/RunDirector, não recebe input e não lê/grava save.
Seed padrão: 71. O painel mostra a etapa, seed e tick; recarregue a cena para repetir.
Com passo de simulação de 0,02 s, a sequência é:

1. Pré-aquecimento dos pools e criação de 200 inimigos (40 de cada espécie comum).
2. Warmup de 250 ticks (5 s simulados) com todos os pools auxiliares saturados.
3. Carga de 200 até tick 1750 (30 s simulados após warmup).
4. Carga de 400 (80 de cada espécie) até tick 3250; depois a cena conserva a carga
   visível e encerra os passos do workload.

Há 512 projéteis, 512 pickups e 128 anéis Quack, além de até 24 campos do scheduler.
Cada pool auxiliar sofre tentativa adicional de aquisição para verificar esgotamento.
Projéteis retornam após 1 s, efeitos após 0,5 s e 16 pickups são reciclados por tick.
Todos os 400 inimigos já estão pré-aquecidos antes da primeira etapa. O scheduler
de inimigos é o de produção, com steering budget 64 e as cinco definições autorais.
O alvo é neutro, sem collider e protegido contra morte para não interromper a carga.

É um cenário de saturação: projéteis são movidos diretamente pelo driver, sem o
WeaponRunner nem resolução de dano; pickups exercitam o pool, sem conceder XP.
Não substitui um replay de partida completa. A automação cobre estado e reutilização;
FPS, GC e memória tiveram capturas reais na T074; o aceite de outras classes de
aparelho e da partida completa continua na T075.

O menu **QuackQuack > Build T071 performance benchmark scene** recria a cena pelo
Unity. A cena não entra na lista global de builds nem no pipeline de produto T070.
Para uma build de profiling, inclua-a como cena inicial de uma cópia de validação;
use IL2CPP no Windows x64/Android ARM64 e preserve as cenas de produto do projeto.
Execute PerformanceBenchmarkTests no PlayMode: geram capturas ignoradas
Logs/t071-200.png e Logs/t071-400.png. Ver [relatório T071](test-report-t071.md).

## Telemetria disponível após T072

PerformanceBenchmark e LakeRun já têm PerformanceTelemetry ligado aos pools e ao
scheduler quando a composição termina. Não é necessário reconstruir as cenas.
O menu **QuackQuack > Build T072 performance telemetry** adiciona o componente às
cenas existentes pelo Unity; os geradores de benchmark e LakeRun também o preservam.

No Profiler, procure QuackQuack.EnemyScheduler.Advance e QuackQuack.Telemetry.Capture.
O buffer registra frame time observado, tempo/quantidade de passos do scheduler,
steering, leases ativos/capacidade/recusas por família, GC alocado, memória usada
pelo Unity e heap GC usado. Contadores não disponíveis são -1 e têm seu bit ausente
em available_mask: MainThread=1, GcAllocated=2, UnityUsedMemory=4, GcUsedMemory=8.
Frame time observado não equivale a tempo de CPU; memória Unity não equivale a RSS.

Após a janela desejada, pause o Editor e use o menu de contexto do componente
**Export telemetry CSV to temporary cache**. O Console informa o caminho de
QuackQuackTelemetry.csv. A exportação aloca e faz I/O, portanto execute fora da janela
que pretende medir. Em player, uma ferramenta de validação pode chamar ExportCsv
com caminho gravável; o jogo não exporta automaticamente nem toca no save pessoal.

O buffer padrão retém 8192 amostras (cerca de 136 s a 60 quadros/s); ao preencher,
substitui as mais antigas e incrementa Overwritten. Para outro tamanho, use o
parâmetro capacity de Bind na composição. No benchmark, filtre warmup=0, seed,
enemy_target e benchmark_tick. O driver para a coleta automática ao concluir.
Contadores do Profiler podem refletir o quadro anterior, enquanto scheduler/pools
são snapshots atuais; descarte as bordas ao comparar janelas de carga. As recusas
dos pools auxiliares do benchmark são intencionais, uma por família por passo.

Execute PerformanceTelemetryTests, PerformanceBenchmarkTests, PoolServiceTests,
EnemyUpdateSchedulerTests e RunSceneFlowTests. A categoria PerformanceTelemetryRuntime
pode ser executada em test player, junto com PlatformRuntimeSmoke. Os testes medem
alocação gerenciada somente no sampler aquecido, sem prometer zero GC no jogo.
Resultados do sampler estão no [relatório T072](test-report-t072.md). Medições
reais de FPS/GC/memória estão no [relatório T074](test-report-t074.md), com seus
limites; não encerram o aceite de hardware e experiência da T075.

## Validador de conteúdo disponível após T073

Use **QuackQuack > Validate content** no Unity. A janela mostra quantos assets foram
checados, erros e avisos. **Validar novamente** atualiza o relatório; **Selecionar**
leva ao asset indicado. IDs duplicados, vínculos inválidos, limites e referências
quebradas são diagnósticos de leitura: a ferramenta não corrige nem salva dados.

Para execução em lote com Unity 6000.4.3f1:

~~~text
-batchmode -nographics -quit -projectPath <copia-ou-projeto> -executeMethod QuackQuack.Editor.ContentValidationWindow.ValidateForCI -logFile <content-validation.log>
~~~

Confira código de saída e T073_CONTENT_VALIDATION_OK. Erros lançam BuildFailedException;
avisos não bloqueiam. O escopo padrão é Assets/QuackQuack, com subassets incluídos.
No snapshot T073: 63 ScriptableObjects, zero erros e dois avisos UnsupportedType
para as configurações nativas LakeRenderer2D/LakeURP. Esses assets recebem a checagem
estrutural de referências quebradas e números finitos; os limites internos do URP
continuam sob as ferramentas da engine.

Os IDs de armas/passivas/inimigos precisam estar no conjunto validado e ter tipos
corretos. Sapo-Rei é referência externa explícita do seu encontro. Os limites são
os definidos nas classes de domínio e catálogos, não valores duplicados no Editor.
Execute ContentValidationTests em EditMode para os casos válidos e inválidos;
fixtures temporárias são criadas/removidas pelo AssetDatabase em pasta exclusiva.
Ver [relatório T073](test-report-t073.md). A validação não inicia build nem substitui
os testes de cena ou profiling da T074/T075.

## Profiling T074

O código abaixo cria **outra cópia nova**, `Temp/T077Profile`, reservada só para
profiling. Mantenha `$repo`, `$unity`, `$adb`, `$device`, `$logs`, `$results` e a
função Invoke-ValidationUnity definidos no roteiro anterior; não redefina `$copy`.
O helper recusa `.git` (pasta ou arquivo) e altera configurações locais para
Development IL2CPP, frame timings e identidade exclusiva do APK. Não copiar
configurações ou a cena temporária de volta ao clone nem ao projeto de produto.

```powershell
$profileCopy = Join-Path $repo 'Temp/T077Profile'
if (Test-Path -LiteralPath $profileCopy) { throw 'Escolha uma pasta nova para profiling.' }
New-Item -ItemType Directory -Path $profileCopy | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    & robocopy (Join-Path $repo $folder) (Join-Path $profileCopy $folder) /E /COPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS
    if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar $folder para profiling." }
}
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $profileCopy, '-buildTarget', 'Win64', '-executeMethod', 'QuackQuack.Editor.PerformanceProfileBuild.BuildWindows', '-logFile', (Join-Path $logs 'windows-profile-build.log'))
$windowsCapture = Join-Path $results 'windows-profile.csv'
& (Join-Path $profileCopy 'Builds/T074Windows/QuackQuackProfile.exe') -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -profileOutput $windowsCapture -logFile (Join-Path $logs 'windows-profile-player.log') | Out-Host
# Aguardar T074_PROFILE_COMPLETE e encerramento do player antes de compilar Android.
Invoke-ValidationUnity @('-batchmode', '-quit', '-projectPath', $profileCopy, '-buildTarget', 'Android', '-executeMethod', 'QuackQuack.Editor.PerformanceProfileBuild.BuildAndroid', '-logFile', (Join-Path $logs 'android-profile-build.log'))
& $adb -s $device install -r (Join-Path $profileCopy 'Builds/T074Android/QuackQuackProfile.apk')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar o APK de profiling.' }
& $adb -s $device shell monkey -p com.cemggames.quackquack.profilet074 -c android.intent.category.LAUNCHER 1
if ($LASTEXITCODE -ne 0) { throw 'Falha ao iniciar o profiling.' }
```

Execute o player visível, com foco, sem compilação concorrente. Alvo padrão 60;
`-profileTargetFps 120` no Windows permite diagnóstico de throughput. No Android,
o padrão do helper também é 60. O benchmark dura aproximadamente 65 s simulados;
aguarde o aplicativo terminar e confirmar a exportação no log do seu processo.
Não confundir interrupção/crash com término bem-sucedido. Só depois copie:

```powershell
$remote = '/sdcard/Android/data/com.cemggames.quackquack.profilet074/cache/T074-profile.csv'
$androidCapture = Join-Path $results 'android-profile.csv'
foreach ($suffix in @('', '.json', '.gpu.csv', '.markers.csv')) {
    & $adb -s $device pull ($remote + $suffix) ($androidCapture + $suffix)
    if ($LASTEXITCODE -ne 0) { throw "Falha ao copiar captura $suffix." }
}
python (Join-Path $repo 'specs/001-prototipo-lago/tools/profile_summary.py') $windowsCapture $androidCapture
if ($LASTEXITCODE -ne 0) { throw 'Captura inválida ou incompleta; consultar diagnóstico.' }
```

Antes de resumir, conferir nos dois `.csv.json` Unity, BuildGuid, TargetFrameRate,
resolução, API gráfica, Samples, Overwritten e FocusLosses. Android mantém o cache
entre instalações; timestamps e BuildGuid devem corresponder à rodada recém-executada.
Instalação/abertura sozinhas não provam nova captura. Preservar CSVs com seus três
arquivos auxiliares, hash do APK/player e log de encerramento como evidência local.

O helper gera T074Profile.unity pelo Unity somente na cópia. Não modifica a lista
produtiva de cenas. PerformanceProfileCapture pré-aloca buffers de 8192 entradas,
registra frame timings e probes nativos de Physics2D/render/GPU e exporta ao final.
Os probes acrescentam memória e custo de instrumentação. Valores indisponíveis
não representam custo zero; as unidades dos probes constam nos metadados.
O analisador stdlib rejeita CSV incompleto ou buffer sobrescrito, verifica carga
saturada, recorta bordas e calcula média/percentis; não concede aceite automático.
Preserve apenas o relatório revisado no Git, sem CSVs, logs e builds regeneráveis.
Veja [resultados e limitações T074](test-report-t074.md).
