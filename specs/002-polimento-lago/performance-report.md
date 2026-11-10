# Desempenho — T049

Data: 10/11/2026. Capturas Windows/S23 realizadas e resultados salvos. **T049 permanece aberta**: faltam Android intermediário/de entrada (indisponíveis, confirmado pelo usuário), atribuição/orçamento do aumento de memória e aceite de latência/capacidade de apresentação. Windows/S23 não autorizam extrapolação às plataformas ausentes. T050 não iniciada.

## Fonte e método

Base de runtime: `2064ace` (T048), com captura opt-in adicionada nesta task. Unity 6000.4.3f1; cópia isolada nova `Builds/T049/ValidationProject`, criada com Assets, Packages e ProjectSettings, sem Library. Nenhum serial de dispositivo integra o relatório.

- Comparação histórica: benchmark da T001, commit `820229d2335d1265ee96d89015d776c8a7614a56`, seed 71, fixed step 0,02 s, warmup 5 s e fases de 30 s com 200/400 inimigos, 512 projéteis, 512 pickups e pelo menos 128 VFX. SHA-256 de PerformanceBenchmark.cs permanece `A6F0E48DA7914660CAC01B79D003BAB56C239C15A593AB546B1998304177076E`.
- Players Development IL2CPP renderizados; Windows 1280×720, Android na resolução nativa. Frame timings e PerformanceTelemetry existentes. Nada de batchmode/nographics no player. Exportação somente ao terminar; buffers não podem sobrescrever amostras.
- Cenários novos: LakeRun (seed 42) com seis armas e seis passivas N5. Stress de 65 s com alvo 200 até 35 s e 400 depois, movimento sintético e Quack periódico, água/aura/recompensas reais. Apenas nesse modo, a produção da onda inicial é substituída por reposição diagnóstica; o mesmo diretor, scheduler, física, armas, política e RNG de recompensas continuam ativos. Acesso reflexivo ocorre uma vez na preparação e falha explicitamente se a ligação não existir. Não é cadência normal da partida.
- Sessão de 900 s de simulação, em tempo real e sem coleta planejada: Pato estacionário, inimigos que chegam a menos de 3 unidades recebem dano pelo diagnóstico, antes de alcançar o raio de captura. Sem alterar chance/quantidade/identidade dos drops. Proteção contra morte mantém a exposição. XP efetivamente coletado deve ser conferido no resultado, não presumido zero.
- Nos dois cenários LakeRun, catálogo da cópia tem capacidade diagnóstica de 400 e pools comuns de pelo menos 80 por espécie; quantidades/cadência das ondas, atributos de combate e recompensas autorais permanecem iguais. Isso não é alteração de dados de produto nem medição da memória exata do produto normal.
- No stress, o ledger tem orçamento diagnóstico de 32.768 derrotas (reposição artificial de até oito inimigos por frame, alvo máximo 60 FPS, por 65 s). A política CreateLedger e o delegate de RNG dedicado são preservados antes da primeira derrota. A sessão de 15 minutos mantém o orçamento autoral de 3.323; nenhum desses ajustes é aplicado ao produto normal.
- Observador mantém uma lista pré-alocada de pendências. Materialização na mesma primeira observação tem resolução limitada a um frame; tempos de espera observados têm essa incerteza e não são tempos internos exatos do renderer. Pendências ainda abertas são relatadas separadamente, nunca tratadas como latência zero.
- Metadados incluem capacidade do ledger, derrotas, recompensas, pendências máximas/finais, esperas observadas, proteção sintética, coletas e amostras com atração. Varredura de atração ocorre uma vez por segundo e pode não contar atrações muito curtas.
- Rotinas de profiling ficam fora das cenas de produto. Builder recusa execução em checkout com `.git`; cenas/settings/capacidades alterados apenas na cópia. Logs, CSVs, XMLs, APKs e caches não são versionados.

## Equipamentos e condições

Windows: Ryzen 7 5800X3D / RTX 4070 Ti, mesma máquina do baseline. S23 Ultra SM-S918B: Android 17/API 37, conectado por USB. Inventário inicial desta rodada: bateria 62%, carregando (status 2), sensor de bateria 29,3°C; baseline T001 começou a 36,1°C e 100% carregado. Essa diferença térmica/energética limita comparação causal entre rodadas, mesmo com o mesmo protocolo; não ocultar nem extrapolar estabilidade térmica a partir de FPS médio.

## Resultados

FPS médio = 1000 / média dos frametimes; percentis nearest rank. Benchmark: ticks 260–1740 e 1760–3240, como T001. Stress: segundos 5,2–34,8 e 35,2–64,8; a segunda janela inclui a rampa de reposição. Sessão longa: 5,2–899,8. Não remover frames lentos dentro desses recortes. Todos os contadores principais tiveram mask 15 nas janelas analisadas.

### Benchmark comparável com T001, alvo 60

| Plataforma / inimigos constantes | FPS T001 → T049 | Frame p95 ms | Frame p99 ms | Scheduler p95 ms | GPU p95 ms | Unity máxima MiB T001 → T049 |
|---|---:|---:|---:|---:|---:|---:|
| Windows / 200 | 60,00 → 60,00 | 16,667 | 16,668 | 0,937 | indisponível | 245,95 → 284,22 |
| Windows / 400 | 59,99 → 59,98 | 16,667 | 16,706 | 2,912 | indisponível | 246,95 → 285,13 |
| S23 / 200 | 59,99 → 59,91 | 16,678 | 16,754 | 3,232 | 7,579 | 239,19 → 314,96 |
| S23 / 400 | 58,68 → 59,79 | 16,676 | 16,756 | 5,491 | 10,675 | 239,45 → 317,05 |

Windows: 1.777 amostras em cada fase, zero frames acima de 25 ms e GC.Alloc zero. S23: 1.775/1.771 amostras; seis frames acima de 25 ms por fase, nenhum acima de 40 ms; na fase 200, sete frames de 104 bytes de GC.Alloc (728 bytes totais), na 400 zero. Não houve perda de foco nem sobrescrita nessas capturas. Populações mínimas e máximas foram exatamente 200/400; 512 projéteis, 512 pickups e VFX até 152.

Main Thread p95 Windows: 16,660/16,660 ms; S23: 18,752/20,914 ms. Esses contadores incluem espera/sincronização do player Development e não representam somente custo de gameplay. GPU Windows retornou zero tanto em FrameTimingManager quanto no fallback ProfilerRecorder GPU Frame Time: indisponível, não custo zero. No S23 houve 1.710/1.768 timings GPU positivos.

Memória nas tabelas é Total Used Memory do Unity, em MiB, não RSS completo do processo nem orçamento total de GPU. O sensor de bateria do benchmark atual variou de 28,5 a 29,4°C (66%, carregando); a diferença frente à T001 impede atribuir variações pequenas somente ao código.

A cadência deste benchmark foi mantida; isso não aprova todos os estados do produto. A memória residente aumentou cerca de 38 MiB no Windows e 76–78 MiB no S23. O buffer de probes passou de 335 a 339 marcadores, acréscimo calculável de apenas 0,25 MiB; ele não explica todo o aumento. O restante ainda não foi decomposto por asset/tipo com snapshot de memória. Sem orçamento de RAM aprovado e sem aparelhos intermediário/de entrada, o aumento **não recebe aceite global**. Não presumir vazamento nem atribuir tudo à arte sem decomposição.

### Sessões de 15 minutos sem coleta

| Plataforma | Simulação / tempo real s | Frames retidos / analisados | FPS | p95 / p99 ms | Main Thread / scheduler p95 ms | GPU p95 ms | Unity máxima MiB |
|---|---:|---:|---:|---:|---:|---:|---:|
| Windows | 900,016 / 900,282 | 53.995 / 53.673 | 60,00 | 16,667 / 16,668 | 16,666 / 0,720 | indisponível | 306,08 |
| S23 | 900,015 / 900,524 | 53.993 / 53.672 | 59,99 | 16,677 / 16,748 | 17,840 / 3,766 | 6,492 | 324,38 |

Zero sobrescritas, zero XP coletado e nenhuma exceção registrada em ambas. Windows: uma perda de foco, um frame acima de 40 ms, sem causalidade demonstrada entre esses dois fatos. S23: zero perdas de foco e zero frames acima de 25 ms na janela analisada. Sensor de bateria no S23: 30,0 → 31,4°C, carga 66 → 69%, USB/status 2. Não é temperatura interna CPU/GPU nem prova de ausência de throttling.

GC.Alloc: Windows 1.466 frames positivos, 241.950 bytes totais, máximo/frame 18.800; S23 1.411 frames positivos, 202.128 bytes totais, máximo/frame 18.444. Não declarar GC zero nesta carga. Nos terços inicial/central/final, p95 Windows permaneceu 16,667 ms; S23 16,678/16,677/16,676 ms. Memória máxima nos mesmos terços: Windows 305,71/305,96/306,08 MiB; S23 323,74/324,12/324,38 MiB. Não apareceu degradação progressiva de frametime nesta rodada; esses valores não demonstram ausência absoluta de vazamento.

| Plataforma | Derrotas / capacidade autoral | Recompensas | Materializadas | Pendentes finais | Mais antiga ainda pendente | Maior espera observada entre as materializadas |
|---|---:|---:|---:|---:|---:|---:|
| Windows | 1.199 / 3.323 | 1.317 | 270 | 1.047 | 697,902 s | 16,671 ms |
| S23 | 425 / 3.323 | 473 | 271 | 202 | 676,141 s | 16,866 ms |

Materializadas + pendentes = registros em ambas. Pool combinado XP/cura: capacidade 272, picos 270/271. Nenhum esgotamento do ledger; pools de apresentação saturaram e preservaram obrigações, não as suprimiram. As diferenças de mortes mostram que as trajetórias físicas/resultados das duas execuções não são um replay idêntico; não comparar custo entre GPUs como se a carga fosse igual. A captura termina na chegada ao chefe, sem vitória forçada nem avaliação da luta final. Não atingiu todas as 3.323 derrotas conservadoras possíveis; a prova lógica desse teto continua nos testes T027.

**Pendência relevante de materialização:** as esperas abertas de mais de onze minutos são consequência observada da ausência de coleta com pool finito; não foram drenadas nesta sessão nem aprovadas como UX aceitável. Preservar o ledger não equivale a apresentar todos os drops imediatamente. Ainda é necessário avaliar orçamento de views/memória e latência aceitável, preservando recompensas e sem reduzir chance de cura ou conceder XP remotamente.

## Qualidade da instrumentação e regressão corrigida

Primeiro piloto descartado: reposição diagnóstica chamava SpawnDirector.Process com Time.frameCount e violava o tick monotônico usado pelo RunDirector. O diagnóstico agora controla Advance explicitamente e usa a propriedade readonly SimulationTick; não muda o passo de simulação nem as regras do diretor. A carga foi repetida antes da coleta válida.

O fechamento do piloto também demonstrou NullReferenceException em RunDirector.OnEnded quando Scheduler ou WeaponRunner já tinham sido destruídos pelo Unity. Dois casos novos reproduziram as ordens de teardown: execução vermelha 36 aprovados/2 falhas esperadas em RunDirectorFlowTests. OnEnded agora verifica a existência desses componentes antes de desabilitá-los; mantém fechamento de recompensas e resultado único. Um terceiro caso verifica o relógio compartilhado e sua suspensão na pausa.

Reexecução focada: **53/53 aprovados, zero falhas/ignorados**, incluindo RunDirectorFlowTests, PerformanceBenchmarkTests, PerformanceTelemetryTests e ResultsViewTests. Evidências locais em `TestResults/T049/director-red.xml`, `TestResults/T049/diagnostic-green.xml` e logs correspondentes. Piloto corrigido completou 65 s, sem exceções e com amostras de atração; foi excluído dos números de desempenho porque havia compilação concorrente.

A instrumentação LakeRun pré-aloca buffers para 65.536 frames; a memória medida inclui esses buffers. Nenhum componente de profiling foi adicionado às cenas normais, e nenhum dado de balanceamento da cópia retorna ao checkout de produto.

Rodada inicial ampliada (`stress60`/`stress30`, sem sufixo): a chamada extra de Process no mesmo tick era idempotente e a onda autoral mantinha cerca de 32 inimigos. Dados excluídos do aceite de 200/400, mesmo com FPS alto. O produtor sintético passou a substituir somente essa onda na preparação; Process e scheduler avançam uma vez no tick compartilhado. Piloto seguinte atingiu 400, mas esgotou 3.323 derrotas pela reposição artificial: também excluído. Não foi aplicado teto de cura/XP nem continuada uma captura com exceção como se fosse válida; o orçamento diagnóstico foi ampliado e a rodada repetida.

A primeira exportação 30 FPS R2 observava pendências antes do último Advance, deixando uma cauda não observada de até 24 registros no metadado. Finish agora observa as recompensas finais antes de exportar; **30 FPS R3** repetido nas duas plataformas, com identidade materializados + pendentes = total e nenhum erro. As capturas válidas são benchmark60, run60, stress60-r2 e stress30-r3. As alterações posteriores a run60 afetaram apenas o ramo de stress e a leitura após o término; regras e avanço da sessão longa permaneceram os mesmos. Build GUID por captura é preservado nos JSONs.

## Carga ampliada — picos 200/400 com efeitos, drops e atração

| Plataforma / alvo FPS / fase | Amostras | Inimigos ativos min–máx | FPS | p95 / p99 ms | Main Thread / scheduler p95 ms | GPU p95 ms | Unity máx. MiB |
|---|---:|---:|---:|---:|---:|---:|---:|
| Windows / 60 / 200 | 1780 | 176–200 | 60.00 | 16.667 / 16.668 | 16.666 / 0.681 | indisponível | 318.89 |
| Windows / 60 / 400 | 1776 | 295–400 | 60.00 | 16.667 / 16.684 | 16.666 / 1.441 | indisponível | 319.19 |
| S23 / 60 / 200 | 1782 | 165–200 | 60.00 | 16.676 / 16.686 | 18.447 / 2.862 | 7.723 | 337.89 |
| S23 / 60 / 400 | 1776 | 295–400 | 60.00 | 16.675 / 16.715 | 18.694 / 4.174 | 9.629 | 338.71 |
| Windows / 30 / 200 | 888 | 165–200 | 30.00 | 33.334 / 33.337 | 33.335 / 0.684 | indisponível | 316.49 |
| Windows / 30 / 400 | 888 | 255–400 | 30.00 | 33.334 / 33.336 | 33.334 / 1.416 | indisponível | 317.14 |
| S23 / 30 / 200 | 888 | 163–200 | 30.00 | 33.355 / 33.759 | 34.005 / 3.335 | 7.535 | 335.18 |
| S23 / 30 / 400 | 888 | 246–400 | 30.00 | 33.357 / 33.738 | 33.913 / 4.729 | 9.559 | 335.83 |

Fases indicam o alvo/pico, não população constante: mortes e reposição reduzem o número entre frames e a transição para 400 permanece na janela. Somente o benchmark original mantém 200/400 constantes. Nestas quatro capturas: zero perdas de foco, sobrescritas e exceções; nenhum frame acima de 40 ms nas janelas. Atração observada por amostragem em todas.

| Captura | Derrotas | Registros | Materializados / pendentes finais | Espera máxima materializada / pendente mais antiga (s) | Frames positivos de GC nas duas fases / bytes totais |
|---|---:|---:|---:|---:|---:|
| windows-stress60-r2 | 3321 | 3658 | 419 / 3239 | 4.549 / 58.264 | 1362 / 904736 |
| android-stress60-r2 | 3199 | 3522 | 429 / 3093 | 6.763 / 58.261 | 1320 / 874998 |
| windows-stress30-r3 | 3274 | 3603 | 416 / 3187 | 8.200 / 58.149 | 1063 / 424552 |
| android-stress30-r3 | 3148 | 3464 | 433 / 3031 | 9.999 / 57.950 | 1051 / 424196 |

O ledger diagnóstico de 32.768 não esgotou. O pool de views permaneceu em 272 e alcançou 270 nas janelas, com pendências preservadas. GC não foi zero: os números incluem o produtor sintético, mortes, feedback e escolhas automáticas, não somente o loop estável de movimentação. Não atribuir todo GC a um subsistema sem captura de stacks.

## Rastreabilidade das evidências

Evidências locais ignoradas em `TestResults/T049` e `Logs/Polimento002/T049`; CSV principal, JSON e anexos por prefixo. Manifesto local `capture-manifest.json` inclui SHA-256 de todos os anexos. Artefatos brutos e binários não são versionados.

| Captura | Build GUID | SHA-256 CSV principal |
|---|---|---|
| windows-benchmark60 | 176a8ebfc27e47a0b83b42782290616c | 7927cba9a647394a3cbe11fa6f60adf2662d8f4b2ea8bcddae23e4ddef1bf7a4 |
| windows-run60 | 176a8ebfc27e47a0b83b42782290616c | 8799aa64f5a85c5b67487205f6cc0a84cdbef16da2123a1ffe2fee2d9980f0d1 |
| android-benchmark60 | 137bea571a104aa0a42ac6c7fe540319 | e500e1f486b83fb7a6ed8eda4e2883b91cf8c824170cdb9101a60e34cb667a94 |
| android-run60 | 137bea571a104aa0a42ac6c7fe540319 | c8418f8f184c32e8166aeb69d7a1bbafe0b261f9a6ddb4ca6dab5cbe5ea6f4fd |
| windows-stress60-r2 | e35fc40bdd944a87bf67763c8b5945c4 | 904b8ff0e30db87d8f2b07d8e968dd3586c414c223fa5b810a4a5e86712cc846 |
| android-stress60-r2 | ca4f13dcbb6940aea84dd4b89cc6b1b0 | 4a4d67dbfd19f3536f29886ce0ab4428a171823fd1bdddfbd31948068eb865ac |
| windows-stress30-r3 | aa64aadae28645bc9f5ede7e4e6d00dd | 8e2e5c48ab4536de7d70d06b702ef6e855e0a26475c51b891b59058c4def17a4 |
| android-stress30-r3 | a72036ac1f7743ee90208e63b80043f3 | 1b509540213b9727b67afaebc32c759b56a7a4700f29c94c377db00b43f6920a |

Builds finais de diagnóstico: `Builds/T049/ValidationProject/Builds/T049Windows/QuackQuackProfile002.exe` e `Builds/T049/ValidationProject/Builds/T049Android/QuackQuackProfile002.apk`, marcadores `T049_PROFILE_BUILD_OK`, saídas 0. Cada execução válida teve marcador de conclusão e ausência de Exception/FATAL EXCEPTION no log da sessão.
