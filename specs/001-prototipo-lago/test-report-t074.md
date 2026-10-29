# T074 — comparação, correção e novo profiling

Execução em 28/10/2026, Unity 6000.4.3f1, players Development IL2CPP renderizados.
T074 concluída no hardware abaixo. T075 e o aceite completo SC-006 permanecem pendentes.

## Método e equipamento

Benchmark T071, seed 71, passo 0,02 s: aquecimento de 5 s e duas fases de 30 s
com 200 e 400 inimigos. Pools saturados: 512 projéteis, 512 pickups e pelo menos
128 efeitos. Análise recorta 0,2 s de cada borda das fases; FPS médio é o inverso
da duração média do frame, percentis usam nearest rank. Exportação somente ao final.
Nenhum CSV sobrescreveu o buffer de 8192 amostras.

Windows 11, Ryzen 7 5800X3D, RTX 4070 Ti, 65447 MB RAM, Direct3D12,
janela 1280×720. Android real Samsung S23 Ultra SM-S918B, Android 17/API 37,
Adreno 740, Vulkan, 11044 MB RAM, viewport 3088×1440, conectado por USB.
As resoluções diferem: comparar melhorias dentro de cada plataforma.
APK exclusivo `com.cemggames.quackquack.profilet074`, sem substituir o jogo ou seus saves.

## Diagnóstico e correção

Capturas a 120 FPS expuseram criação excessiva de contatos 2D. No Windows,
Physics2D.FindNewContacts chegou a p95 de 51,54 ms e Simulate a 56,50 ms com
400 inimigos, enquanto GPU Frame Time teve p95 de 4,97 ms.

SpawnDirector agora exclui contatos inimigo/inimigo (layer 9) e inimigo/XP
(layer 10) por lease. Preserva colisões com jogador (0), terreno (8), consultas
de combate e a máscara autoral, restaurada ao devolver ao pool. ExperiencePickup
atribui layer 10 no Awake. Não houve mudança global na matriz de colisões.
Um experimento global foi descartado: Android ficou em 69,13 FPS/p95 25,01 ms,
sem ganho sobre a exclusão local. Não foram usados DOTS/ECS/Jobs/Burst.

| Plataforma, 400 inimigos, alvo 120 | FPS médio | Frame p95 ms | Frame p99 ms |
|---|---:|---:|---:|
| Windows diagnóstico antes | 41,66 | 67,10 | 317,84 |
| Windows depois | 95,33 | 14,20 | 14,73 |
| S23 antes | 36,22 | 66,69 | 316,75 |
| S23 depois | 69,72 | 25,01 | 33,34 |

O resultado Android a 120 ainda não sustentava cadência de 60. Repetimos com
alvo nominal 60; GameBootstrap passou a configurar 60 explicitamente e oferece
`limitTo30Fps` no Inspector. O benchmark configura seu próprio alvo, sem Bootstrap.

## Capturas finais, alvo 60

| Plataforma | Inimigos | FPS médio | Frame p95 ms | Frame p99 ms | GPU p95 ms | Memória Unity máxima MiB |
|---|---:|---:|---:|---:|---:|---:|
| Windows | 200 | 60,00 | 16,668 | 16,673 | 0,99 | 272,91 |
| Windows | 400 | 59,86 | 16,668 | 17,095 | 3,03 | 275,35 |
| S23 | 200 | 59,99 | 16,673 | 16,755 | 8,50 | 237,87 |
| S23 | 400 | 59,93 | 16,673 | 16,760 | 11,24 | 239,37 |

GC.Alloc: zero nas duas fases finais das duas plataformas. FindNewContacts p95
com 400: Windows 10,20 ms; S23 9,38 ms. Scheduler p95 S23: 5,54 ms.
Windows usa contador nativo GPU Frame Time; FrameTimingManager retornou GPU zero
nessa plataforma, tratado como indisponível. Android usa timings GPU válidos.
Memória inclui Development e arrays dos probes (dezenas de MB); não há orçamento
quantitativo de memória definido para declarar aprovação absoluta desse indicador.

Cadência nominal de aproximadamente 60 FPS observada neste cenário. Não significa
todos os frames abaixo de 16,6667 ms. Windows registrou duas perdas de foco na
execução final (`runInBackground=true`), sem remover amostras; repetir em ambiente
controlado para aceite formal. S23 final não registrou perda de foco.
S23 é flagship: esta coleta não comprova aparelhos intermediários/de entrada,
sessão térmica de 15 minutos, percepção de input ou a matriz completa T075.

## Rastreabilidade e validação

| Captura local em TestResults | Build GUID | Amostras |
|---|---|---:|
| t074-windows-diagnostic.csv | db236acf09584328af50e55186495d11 | 5462 |
| t074-windows-after.csv | 6784583c62804923b80e65f70c9d4ef5 | 7058 |
| t074-android-before.csv | 651c7e2643fb4c9b966bac214bfcb4ab | 4743 |
| t074-android-after.csv | 5e1da0dc777b45a39b75598f2912ba71 | 5999 |
| t074-windows-60.csv | 413c856ffa834f5097d924a2f0bcd764 | 3895 |
| t074-android-60.csv | af3742965f3e4d1db17a976a97751efc | 3891 |

254/254 PlayMode aprovados (`t074-playmode-verified.xml`); 4/4 testes no player
Windows IL2CPP (`t074-windows-smoke.xml`), incluindo limites 60/30 e smoke de
plataforma. O teste de filtro foi observado falhando antes da correção. Testes
cobrem restauração de máscara e consultas de combate; desempenho é sustentado
pelas capturas reais acima. Logs, builds e CSVs regeneráveis permanecem ignorados.

Reprodução e ferramentas em [quickstart](quickstart.md#profiling-t074).
