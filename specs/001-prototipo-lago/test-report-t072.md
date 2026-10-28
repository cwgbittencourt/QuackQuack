# T072 — Instrumentação de desempenho sem alocação recorrente no sampler

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T072. Próxima task: T073.

## Entrega

PerformanceTelemetry captura em LateUpdate para um buffer circular de 8192 structs
por padrão. Bind permite capacidade de 1 a 65536, com alocação só na ligação.
TryGet retorna cópias cronológicas; Count, Written e Overwritten identificam o
conteúdo retido e substituições. Não há log, LINQ, lista crescente, formatação ou
I/O na captura. A coleta automática do benchmark para ao completar a sequência;
registra seed, tick, carga e warmup para separar suas janelas.

Métricas: duração observada do quadro (Time.unscaledDeltaTime), Main Thread via
ProfilerRecorder, tempo e número de passos do scheduler desde a coleta anterior,
steering do último passo, pools por família (leases ativos/capacidade/esgotamentos),
GC Allocated In Frame, Total Used Memory e GC Used Memory. AvailableCounters/máscara
CSV distingue contadores disponíveis; -1 sinaliza indisponível ou sem amostra nativa.

EnemyUpdateScheduler.Advance possui marcador QuackQuack.EnemyScheduler.Advance e
acumuladores Stopwatch, sem mudar suas regras. O sampler usa deltas para contar
múltiplos passos por quadro, sem repetir tempo em capturas sem avanço. O marcador
QuackQuack.Telemetry.Capture separa seu próprio custo no Profiler. PoolService
adiciona snapshots de valor por família e contagem de aquisições recusadas por
esgotamento; a semântica das leases permanece a mesma.

OnDisable/OnDestroy fecham recorders; OnEnable reabre e restabelece baseline do
scheduler. Componentes sem ligação, desligados ou com pools descartados não coletam.
ExportCsv faz formatação invariável e I/O apenas por chamada explícita; o menu de
contexto exporta no temporaryCachePath e informa o caminho. Não há autoexportação
nem acesso ao save pessoal.

PerformanceBenchmark e RunDirector ligam seus serviços ao componente autoral depois
da inicialização. PerformanceTelemetrySetup adicionou o componente às cenas
PerformanceBenchmark e LakeRun pela API nativa do Editor. As alterações serializadas
são somente a adição do componente, preservando objetos, referências e GUIDs. Os
geradores das duas cenas também adicionam a instrumentação em reconstruções.
Nenhum ProjectSettings, versão, catálogo ou regra Core foi alterado.

## Validação

A fase vermelha confirmou a ausência do componente de produção: 1 teste falhou,
saída Unity 2. Foi substituído pelos testes de comportamento após implementação.

| Execução | Total | Aprovados | Falhas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| Telemetria, benchmark, pools, scheduler e LakeRun focados | 30 | 30 | 0 | 0 | 0 |
| PlayMode completo | 251 | 251 | 0 | 0 | 0 |
| Test player Windows x64 IL2CPP | 8 | 8 | 0 | 0 | 0 |

Os seis testes PerformanceTelemetryTests cobrem ocupação/capacidade/esgotamento,
ordem e overwrite do ring, cópias de valor, entradas inválidas, recorders nativos,
desativação/reativação/descarte, alocação e exportação CSV sob cultura pt-BR.
O contador nativo Total Used Memory entregou valor positivo no Editor; contadores
não disponíveis são verificados contra os sentinelas e sua máscara.

O teste de alocação aquece o sampler com 64 capturas e mede outras 2048 por
GC.GetAllocatedBytesForCurrentThread: **0 bytes gerenciados**. A medição inclui
Capture e GetMetrics, sem asserts/log/formatação dentro da janela. Passou tanto
no Editor quanto no test player. Não mede inicialização dos recorders/buffer,
exportação, Editor, simulação inteira ou todas as threads.

A integração do benchmark confere metadata seed 71/tick/carga, 200/400 de capacidade
do pool de inimigos, 512 projéteis e deltas reais de passos em FixedUpdate. O fluxo
LakeRun confere ligação aos serviços reais. O smoke do player também ganhou essa
asserção e foi exercitado no executável. Seus dois casos verificam bootstrap de
produção, clock, serviços vivos, processo 64-bit e ENABLE_IL2CPP, usando save
isolado temporário. Os outros seis casos do player são a fixture de telemetria.

Uma primeira execução Windows passou 8/8; a execução final repetiu 8/8 após
ajustar o teste de exportação para temporaryCachePath no player, evitando derivar
um caminho do pacote da aplicação (potencialmente somente leitura no Android).
No Editor, o CSV controlado permanece em TestResults. A build do test player terminou
com Build Finished Result: Success. Seu executável
inclui a entrada do Test Runner e não é uma build de produto nem aceite de release.

Artefatos regeneráveis ignorados:

- TestResults/t072-red.xml e Logs/t072-red.log: contrato inicialmente ausente.
- Logs/t072-authoring.log: cenas salvas nativamente, código 0, T072_TELEMETRY_SCENES_SAVED.
- TestResults/t072-focused.xml e Logs/t072-focused.log: 30 casos focados.
- TestResults/t072-playmode.xml e Logs/t072-playmode.log: 251 casos completos.
- TestResults/t072-windows-final.xml e Logs/t072-windows-final.log: 8 casos finais em Windows.
- TestResults/t072-telemetry.csv: exportação controlada da fixture, não perfil de FPS.
- Temp/T033Validation/Builds/T072WindowsFinal: test player final e intermediários.

Comandos Unity executados na cópia isolada Temp/T033Validation:

~~~text
-batchmode -quit -projectPath <copia> -executeMethod QuackQuack.Editor.PerformanceTelemetrySetup.Build -logFile <repo>/Logs/t072-authoring.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t072-playmode.xml -logFile <repo>/Logs/t072-playmode.log
-batchmode -projectPath <copia> -runTests -testPlatform StandaloneWindows64 -testCategory PerformanceTelemetryRuntime;PlatformRuntimeSmoke -playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T072WindowsFinal -testResults <repo>/TestResults/t072-windows-final.xml -logFile <repo>/Logs/t072-windows-final.log
~~~

## Interpretação e limites

FrameMilliseconds é duração de quadro observada, não CPU puro. Main Thread pode
incluir esperas. UnityUsedBytes não é RSS/memória total do aparelho. Contadores
Profiler refletem a última amostra concluída, enquanto o snapshot dos pools e o
delta do scheduler são atuais: Frame identifica a captura, e as bordas entre etapas
precisam ser descartadas ao comparar janelas. Há máscaras explícitas para contadores
que variem por plataforma/build. O contexto seed/tick/warmup é do benchmark; fora
dele, BenchmarkTick é -1 e não há classificação automática de fases da partida.

O ring é limitado: exporte no fim da janela e confira Overwritten. A exportação
aloca e pode contaminar o próximo quadro; execute fora da medição. As recusas de
pools do benchmark T071 são intencionais, não falhas de gameplay.

Não houve profiling de uma execução inteira de 65 s em hardware nem validação de
metas 60/30 FPS, memória total ou GC total do jogo. Android/aparelho real não foi
executado nesta task. Comparação com metas e eventuais otimizações continuam na
T074/T075. EditMode não repetido, pois não houve alteração de Core.

Checklist requirements.md 16/16, sem alterações. .specify/extensions.yml ausente
antes e depois: nenhum hook a executar. Constituição preservada, sem introduzir
DOTS/ECS/Jobs/Burst. T073 e posteriores continuam pendentes.
