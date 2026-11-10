# Desempenho — T049

Data: 10/11/2026. **Continuação Windows/S23 concluída:** aumento de memória investigado, referência provisória aprovada e latência por saturação corrigida/retestada. **T049 permanece aberta somente por Android intermediário/de entrada**, indisponíveis e explicitamente mantidos pendentes pelo usuário. Windows/S23 não autorizam extrapolação às plataformas ausentes. Os resultados antigos abaixo são histórico, não o estado final da correção.

## Continuação T049 — memória e apresentação

Retomada sobre `ec7df46`, mantendo pendentes Android intermediário/de entrada por decisão do usuário. Foi aprovada explicitamente uma referência **provisória de 400 MiB em Total Used Memory do Unity**, incluindo instrumentação, exclusivamente Windows/S23 e preservando qualidade da arte. Não constitui orçamento do processo inteiro, aceite de RSS/GPU total nem aprovação dos dispositivos ausentes.

### Decomposição reproduzida

Comparação nova entre o commit T001 `820229d2335d1265ee96d89015d776c8a7614a56` e o runtime da retomada, em cópias isoladas. Mesmo benchmark seed 71, passo 0,02, warmup 5 s, fases 200/400 de 30 s e Development IL2CPP renderizado. `PerformanceMemoryCensus` exporta objetos/tipos e contadores nativos **depois** da janela medida; suas enumerações e formatação não pertencem às amostras de desempenho. Valores de objetos, alocador, heap e driver têm domínios diferentes: não somá-los como uma reconciliação exata.

| Evidência | Windows T001 → atual | S23 T001 → atual |
|---|---:|---:|
| Total Used Memory no último frame medido, MiB | 275,30 → 309,53 | 238,84 → 315,86 |
| Aumento Texture2D no censo, MiB | 30,63 | 30,76 |
| Aumento heap gerenciado usado, MiB | 4,35 | 4,16 |
| Contador do driver gráfico, MiB | não coletado nesta comparação | 102,88 → 170,37 |

Os cinco maiores atlas usam RGBA32, não são legíveis pela CPU e têm cerca de 6,3–6,5 MB cada: armas, ameaças, pickups, minibosses e amostra. Também aumentaram SpriteRenderers (~1,14 MiB), transforms, GameObjects e componentes visuais. O driver no S23 cresceu 67,49 MiB; o censo de texturas explica uma parcela conhecida, mas não permite atribuir todo o restante a um asset específico nem demonstrar vazamento. Os valores absolutos desta comparação nova incluem instrumentação/sessão diferentes da tabela histórica; não misturar suas bases. Nenhuma compressão, resolução ou pixel da arte foi alterado.

Evidências válidas em `Logs/Polimento002/T049/Continuation`: `windows-baseline-memory`, `windows-current-memory`, `android-baseline-memory-valid` e `android-current-memory-valid`, CSV/JSON/censos por prefixo. GUIDs respectivos: `f5c4fb78db984a2bac71bbc04727a92b`, `0906e5ad2dee4cbbaab65bbdb343dd87`, `9e907bf37ee8440c8cfd0b57957565fb`, `e4e008800dcf4f30b0dd6a52b9fdd4db`. S23: bateria 87→88%, sensor 29,8→31,5°C, USB; não é leitura interna de CPU/GPU.

### Correção da apresentação saturada

O ledger preservava drops, mas 256 views XP/16 curas deixavam obrigações invisíveis por minutos. O excedente agora recebe apresentação pré-aquecida em lotes de até 1.024 sprites por mesh, com índices UInt16. Cada RewardId mantém posição e AttractionState próprios; não se agregam valores nem se concede à distância. O pool físico e a reserva de miniboss permanecem; chance, velocidade e raio não mudaram. Meshes usam os sprites/UVs/tint originais, shader URP 2D próprio e quads para sprites não empacotados ou retangulares. A capacidade cobre o ledger autoral inteiro; o stress usa ledger diagnóstico maior, incluindo seu custo na medição.

Regressão vermelha: dois casos reproduziram WaitingForView após saturação. Reexecução final focada: **100/100**, suíte completa **725/725 EditMode e 630/630 PlayMode**, zero falhas/ignorados. Inclui 6.646 obrigações do pior caso autoral, conservação por identidade entre objetos e lotes, captura/contato únicos, pausa, descarte de cura capturada com vida cheia, encerramento, geometria/UVs válidos e zero alocação gerenciada no trecho estável do teste. Isso não substitui os retestes de player.

Pilotos visuais Windows/S23 ocultaram todos os SpriteRenderers individuais: XP e curas excedentes continuaram visíveis nas meshes, confirmando a renderização real. O helper existe somente na cópia de diagnóstico e marca `VisualOnly=true`; esses pilotos são excluídos das medições de aceite. Capturas finais de 65/900 s concluídas.

**Limite da auditoria de logs:** o S23 registra duas `java.lang.AssertionError: Failed to close dex file in finalizer` no daemon Java. A mesma mensagem existe nos logs anteriores a esta correção (`android-benchmark60.log`, `android-run60.log` e stress anteriores); não é uma regressão nova demonstrada pelo lote de pickups. As capturas terminam normalmente, mas não devem ser descritas como livres de toda exceção do processo. As afirmações históricas de ausência de exceções abaixo se limitam a Unity/C# e encerramentos fatais; não equivalem a ausência dessa mensagem do runtime Android. Sua causa não foi atribuída ao gameplay.

### Reteste final de saturação — 65 segundos

Mesma carga sintética de reposição, seis armas/passivas N5, drops e atração ativos. Fases analisadas 5,2–34,8 s e 35,2–64,8 s; alvo/pico 200/400, não população constante. Sem compilação concorrente no Windows. `VisualOnly=false`, zero perda de foco e zero sobrescrita nas quatro capturas.

| Plataforma / alvo / fase | FPS | Frame p95 / p99 ms | Main Thread / scheduler p95 ms | GPU p95 ms | Unity máxima na fase MiB |
|---|---:|---:|---:|---:|---:|
| Windows / 60 / 200 | 59,95 | 16,668 / 16,677 | 16,667 / 0,677 | 0,196 | 354,42 |
| Windows / 60 / 400 | 59,97 | 16,669 / 16,773 | 16,669 / 1,576 | 0,366 | 355,84 |
| Windows / 30 / 200 | 30,00 | 33,340 / 33,345 | 33,342 / 0,697 | 0,257 | 351,31 |
| Windows / 30 / 400 | 30,00 | 33,340 / 33,353 | 33,341 / 1,505 | 0,339 | 352,59 |
| S23 / 60 / 200 | 59,96 | 16,673 / 16,778 | 17,222 / 3,165 | 9,826 | 351,53 |
| S23 / 60 / 400 | 60,00 | 16,673 / 16,727 | 18,490 / 4,205 | 14,423 | 352,85 |
| S23 / 30 / 200 | 30,00 | 33,352 / 33,652 | 34,023 / 3,037 | 9,689 | 349,50 |
| S23 / 30 / 400 | 30,00 | 33,355 / 34,098 | 34,057 / 4,583 | 17,457 | 350,16 |

| Captura final | Derrotas / registros | Materializados / pendentes | Maior espera observada ms | Máximo em lotes | GC bytes nas duas fases |
|---|---:|---:|---:|---:|---:|
| windows-stress60-final | 3.287 / 3.617 | 3.617 / 0 | 24,520 | 2.244 | 1.194.066 |
| windows-stress30-final | 3.238 / 3.565 | 3.565 / 0 | 33,710 | 2.205 | 497.380 |
| android-stress60-final | 3.278 / 3.607 | 3.607 / 0 | 16,850 | 2.172 | 1.255.940 |
| android-stress30-final | 3.132 / 3.448 | 3.448 / 0 | 34,247 | 2.062 | 919.604 |

Zero pendência máxima e final, zero espera aberta nas quatro. Antes, o stress deixava ~3.000 registros esperando 58 s; agora todas as obrigações foram apresentadas, preservando identidade e coleta local. Os máximos de espera correspondem à resolução de observação por frame, não a medição óptica da tela. Trajetórias, mortes e escolhas automáticas diferem; não tratar as execuções como replay idêntico. O diagnóstico resolve uma oferta confirmando a primeira carta quando PULAR já não está disponível, evitando estagnar a simulação com mais XP coletável.

Não houve frames acima de 40 ms nas fases; uma amostra acima de 25 ms em cada captura a 60 FPS. GC permanece não zero e inclui produtor sintético, ofertas e mortes. Os contadores de pickups do pool continuam limitados a 272; o anexo `.run.csv` acrescenta `batched` para que isso não seja confundido com o total visível. Nesta rodada o Windows retornou timings GPU positivos; a indisponibilidade da rodada histórica continua registrada e não foi reinterpretada como zero.

### Sessões finais de 900 segundos

Windows completou 900,015 s de simulação / 900,310 s reais, 53.941 frames sem sobrescrita, uma perda de foco. Foram 623 derrotas e 686 registros, todos materializados, 416 simultaneamente em lote, nenhuma pendência máxima/final e zero XP coletado. Memória máxima analisada 330,43 MiB; por terço 329,69 / 330,24 / 330,43 MiB. p95 por terço 16,669 / 16,670 / 16,669 ms, sem piora progressiva demonstrada; isso não prova ausência absoluta de vazamento. GC: 212.132 bytes em 1.453 frames positivos.

Houve 12 frames acima de 25 ms e sete acima de 40 ms na janela Windows. Eles se concentram em 99–101 s, quando `batched=0`, só 51–52 recompensas e nenhuma pendência. A maior espera observada, 202,711 ms, coincide com o frame 5.993 de 202,711 ms, ainda sem lotes ativos: é resolução/atraso do frame na observação, não uma fila de recompensas esperando slot. O maior frame foi 225,973 ms; não houve GC nos dois frames citados. Não há evidência para atribuir causalidade à perda de foco ou a outro processo. O resultado não recebe a descrição de ausência de travadas; os demais terços não tiveram frames acima de 25 ms.

S23 completou 900,010 s de simulação / 900,552 s reais, 53.991 frames sem sobrescrita/perda de foco. Foram 425 derrotas / 473 recompensas, todas materializadas, 202 em lote; nenhuma pendência máxima/final, zero XP coletado, espera máxima observada 17,075 ms. Na rodada antiga com as mesmas 425 derrotas, 202 registros ainda aguardavam apresentação ao terminar: esse atraso foi eliminado. FPS 59,99; frame p95/p99 16,673/16,828 ms, Main Thread/scheduler p95 17,183/3,806 ms, GPU p95 7,597 ms. Nenhum frame acima de 25 ms na janela analisada. Memória por terço 325,30 / 325,82 / 326,80 MiB, p95 16,673 / 16,673 / 16,673 ms; GC 202.124 bytes em 1.419 frames positivos. Temperatura de bateria observada durante a sessão 31,5→32,8°C, carga 99→100%, USB, status final 5; não prova ausência de throttling.

Windows na janela completa analisada: FPS 59,94; p95/p99 16,669/16,677 ms; Main Thread/scheduler p95 16,676/0,754 ms, GPU p95 0,212 ms. As duas sessões mantiveram todos os registros apresentados sem conceder XP remoto. A capacidade autoral permanece 3.323 derrotas; o teste automatizado cobre seu pior caso de 6.646 obrigações, pois a exposição real não esgotou esse teto.

| Captura | Total Used Memory máximo em **todas** as amostras, MiB |
|---|---:|
| Windows stress60 / stress30 / run60 | 355,84 / 352,59 / 330,43 |
| S23 stress60 / stress30 / run60 | 367,77 / 367,74 / 343,76 |

As seis capturas satisfazem a referência provisória de 400 MiB, inclusive aquecimento e buffers diagnósticos. Os picos de todas as amostras diferem dos máximos das fases estabilizadas nas tabelas. Não incluem a enumeração/formatação do censo pós-medição. Nenhuma pendência, sobrescrita, visual-only ou divergência materializados=registros nas seis; GUIDs conferidos contra as builds finais. Nenhuma exceção Unity/C# ou encerramento fatal observado; as mensagens Java preexistentes são a limitação explicitada acima. O perfil não equivale a aceite físico de gameplay em dispositivos ausentes.

### Rastreabilidade da continuação e tentativas descartadas

Manifesto local completo `Logs/Polimento002/T049/Continuation/capture-manifest-final.json` contém SHA-256/tamanho/metadados de cada anexo das dez capturas válidas. Capturas finais Windows usam GUID `12d3377bab8d43acb6ad1dec0ffa7b15`; S23 `745a376d296f4fa580bb50890b1f2b74`.

| CSV da continuação | SHA-256 |
|---|---|
| windows-baseline-memory.csv | ae3aeca9040d963b5ccc1c412f18df94a5d0fead678dc73676de30b6b246ee1f |
| windows-current-memory.csv | 1632f38a9320331a6396ce88fd2f6512c11fabf0b60e1294f2cb2f5424512fdd |
| android-baseline-memory-valid.csv | 52e30e9f73474eee0904d0c13f6e939f250f495081655fc9f255d28a32fe4879 |
| android-current-memory-valid.csv | 5926263ae773db15ba581f6b95a577728547fbb7f6fc01fa23c34a939617b063 |
| windows-stress60-final.csv | c10415672df7e20b282104453807ad024cbd11985057a080a599d80d3346893f |
| windows-stress30-final.csv | c8a74d6e96a6a0255eaa46ba829cb31203b6a667d00e02b882e0004166ae2c14 |
| windows-run60-final.csv | 50fe0596f840eae2f2761d3ca6738824a0ac98be29507c031d99f7d54ddac316 |
| android-stress60-final.csv | 055e669d3bb9697c8d061f446a8f4f68a0460222a3ad995c465c07d0a97b2b8f |
| android-stress30-final.csv | 9f08c01b86965c6ad9c3064b5c74bb5a867144858fa14b4670ed8d9939ce8e51 |
| android-run60-final.csv | 00a46c6c341cbc18ecea4162285ee1bc97a202c4bd5bcd7efee14c4a2ec39cf8 |

Builds finais em `Builds/T049/ContinuationProject/Builds/T049Windows/QuackQuackProfile002.exe` e `Builds/T049/ContinuationProject/Builds/T049Android/QuackQuackProfile002.apk`, SHA-256 `353a7873d63307b5a983790fed548b52f40c4c3eb750d06d60e3182f7ce06c9f` e `a362f1fd616dbae7b2ba00102ab9ee40c367921f24f165821da36e19f47ba987`. Mesmos hashes dos cinco arquivos de runtime/shader entre checkout e cópia de build; helpers visuais privados não retornam ao produto. Brutos/binários continuam ignorados.

Tentativas excluídas: compilação Android simultânea interrompida pelo daemon Gradle compartilhado (repetida sequencialmente); leitura do cache interno errado retornando texto de arquivo ausente (repetida com `adb pull` do cache externo e validação JSON/cabeçalho); aparelho em repouso antes do desbloqueio; primeiro comando final Android sem `-buildTarget Android` (corrigido e compilado com sucesso); instalação interrompida pelo encerramento de outro Editor/ADB (reinstalação repetida depois de todos saírem). Nenhuma dessas tentativas entrou nas tabelas finais. Asserções antigas que esperavam fila natural foram atualizadas para apresentação imediata; cenários explícitos de adaptador indisponível continuam testando preservação de pendências.

## Fonte e método histórico

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
