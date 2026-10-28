# T071 — Cena determinística de benchmark

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T071. Próxima task: T072.

## Entrega

PerformanceBenchmark.unity é gerada pelo Unity via PerformanceBenchmarkSetup.Build,
com câmera ortográfica, painel de identificação e referências ao conteúdo existente.
Não contém GameBootstrap/RunDirector, input de usuário ou acesso ao save. A cena é
isolada e não altera EditorBuildSettings nem a lista de produto da T070.
O .meta da cena, scripts e pasta Diagnostics foi gerado pelo Unity e preservado.

PerformanceBenchmark usa PRNG local xorshift e seed serializada 71. Um único driver
FixedUpdate avança passos de 0,02 s. Os cinco inimigos comuns autorais passam pelo
SpawnDirector e EnemyUpdateScheduler de produção. O pool reserva 80 por espécie
antes do primeiro passo: 40 por espécie estão ativos no início (200), e mais 40
são ativados no tick 1750 (400), sem nova criação de objetos na transição.

| Janela | Ticks | Carga |
|---|---|---|
| Warmup | Até 250 | 200 inimigos, pools auxiliares saturados |
| Primeira carga | 250–1750 | 200 inimigos |
| Segunda carga | 1750–3250 | 400 inimigos |
| Fim | A partir de 3250 | Step não avança mais; carga visual permanece |

O scheduler mantém steering budget 64 e pools de 8 campos por tipo especial,
até 24 adicionais. O alvo usa o Pato com entrada neutra, collider desligado e
invulnerabilidade de benchmark, aplicada ao estado sem alterar dados autorais.
Não há morte/menu de XP interrompendo a sequência.

Pools auxiliares mantêm 512 WeaponProjectile, 512 ExperiencePickup e 128
PooledQuackFeedback ativos após cada passo. Projéteis se movem pelo driver e
retornam após 1 s; anéis visuais avançam pelo controlador real e retornam após
0,5 s; 16 pickups são devolvidos e reinicializados por passo. Aquisição extra em
cada pool saturado deve retornar null. Todos os ciclos passam pelo PoolService.

A carga de projéteis não usa WeaponRunner ou resolução de dano. A carga de pickups
exercita aquisição/Initialize/Reset, sem conceder XP. É um cenário de saturação,
com scheduler real de inimigos, e não um replay completo de combate/recompensas.
Ao destruir o cenário, director, upgrades, scheduler e pools são liberados.

## Validação

| Execução | Total | Aprovados | Falhas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| Benchmark após correção de reciclagem | 2 | 2 | 0 | 0 | 0 |
| PlayMode completo com execução automática/capturas | 245 | 245 | 0 | 0 | 0 |
| Benchmark final após ajustar painel e contador | 3 | 3 | 0 | 0 | 0 |

Os testes verificam 200/400 ativos, saturação dos três pools auxiliares, esgotamento
sem expansão, mesmo número de objetos primários criados após warmup/transição,
mais de 50000 devoluções/reciclagens, conclusão idempotente e Dispose dos pools.
Recarregar com a mesma seed e número de passos reproduz as posições dos 200
inimigos com tolerância 0,00001. A fixture congela o relógio automático durante
comparações manuais e restaura Time.timeScale; um terceiro teste habilita avanço
real em FixedUpdate e verifica que a cena continua saturada.

A primeira compilação identificou um using ausente para PlayerCommand, corrigido.
A primeira execução dos dois testes falhou ao esgotar VFX: referências de leases
expirados se confundiam após readquirir em ordem LIFO. O driver agora limpa todas
as referências expiradas antes de readquirir qualquer efeito. Os testes passaram
após essa correção. Nenhum teste foi ignorado ou enfraquecido.

As capturas de 200 e 400 foram inspecionadas. A carga é visível e intensa nas duas
etapas. O painel final mantém seed, tick, fase e capacidades legíveis sobre os
sprites, com fundo e ordenação explícitos. As capturas são evidência visual do
cenário, sem medição ou aprovação de FPS.

Artefatos regeneráveis ignorados:

- Logs/t071-authoring-final.log: cena salva pelo Unity, código 0.
- TestResults/t071-playmode.xml e Logs/t071-playmode.log: suíte completa.
- TestResults/t071-final.xml e Logs/t071-final.log: três testes finais.
- Logs/t071-200.png e Logs/t071-400.png: capturas finais.
- TestResults/t071-benchmark.xml registra a falha inicial de reciclagem;
  t071-benchmark-final.xml registra os dois casos corrigidos.

Comandos Unity executados na cópia isolada Temp/T033Validation:

~~~text
-batchmode -quit -projectPath <copia> -executeMethod QuackQuack.Editor.PerformanceBenchmarkSetup.Build -logFile <repo>/Logs/t071-authoring-final.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t071-playmode.xml -logFile <repo>/Logs/t071-playmode.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testFilter PerformanceBenchmarkTests -testResults <repo>/TestResults/t071-final.xml -logFile <repo>/Logs/t071-final.log
~~~

## Limites

Não foram medidos frame time, GC, memória, termal ou FPS; não há aceite de 60/30 FPS,
Windows Player ou Android físico. Nenhuma build de profiling foi executada nesta
task. Repetibilidade foi verificada no Editor nesta versão, sem promessa de física
idêntica bit a bit entre plataformas. A carga de objetos e as janelas estão prontas
para instrumentação T072 e comparação/correção em hardware T074/T075.

EditMode não repetido, pois Core não foi alterado. Checklist requirements.md 16/16,
sem alterações. .specify/extensions.yml ausente antes/depois, sem hooks a executar.
Constituição preservada: nenhuma adoção de DOTS/ECS/Jobs/Burst no código novo;
nenhuma alteração de catálogo, gameplay de produto ou versão do Unity.
T072 e tarefas posteriores continuam pendentes.
