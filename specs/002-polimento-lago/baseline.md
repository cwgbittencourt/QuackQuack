# Baseline T001 — Polimento do Lago

Coleta iniciada em 03/11/2026 (America/Sao_Paulo), antes de alterações de gameplay da feature 002. Referência de comparação, não aceite global de gameplay/plataforma/desempenho. **T075 da feature 001 permanece aberta.**

## Fonte e isolamento

- Commit medido: `820229d2335d1265ee96d89015d776c8a7614a56` (planejamento); código/arte de produto herdados de `79dc7b7`.
- Workspace inicial limpo. Cópia nova: `Temp/Polimento002Validation`, sem .git, Library ou cache copiado.
- Assets: 633 arquivos; Packages: 2; ProjectSettings: 24. Contagens e SHA-256 comparados individualmente, iguais ao original antes da primeira importação.
- Nenhuma mudança de gameplay, catálogo ou serializado da cópia será aplicada ao projeto fonte. Helper de build Editor e cena de profiling existem apenas em Temp; configurações de teste ficam na cópia.
- Após importação/build, Unity atualizou somente três assets originais dentro da cópia: DefaultVolumeProfile.asset (defaults de volumes), UniversalRenderPipelineGlobalSettings.asset (lista de runtime settings) e QuackQuack/Settings/LakeURP.asset (prefiltros de shaders). Alterações nativas de importação/build foram inspecionadas e ficam isoladas; não são fixes de produto. Código, catálogos e cenas originais da cópia permanecem iguais por hash. ProjectSettings.asset da cópia contém identidade/IL2CPP/frame timings de validação.
- Evidências regeneráveis em `Logs/Polimento002/T001` e `TestResults/Polimento002/T001`, ambas ignoradas. Não versionar builds/APKs/logs/XML/CSV/screenshots/caches nem identificadores de dispositivo.

## Versões

| Componente | Versão |
|---|---|
| Unity | 6000.4.3f1 (39d1a88d4dd1) |
| URP | 17.4.0, renderer 2D |
| Input System | 1.19.0 |
| uGUI | 2.0.0 |
| Unity Test Framework | 1.6.0 |
| Memory Profiler | 1.1.9 |
| Players de profiling | Development IL2CPP renderizado, alvo 60, sem headless/nographics |

## Conteúdo de referência

Catálogos `PrototypeBuildCatalog.asset` e `LakeEncounterCatalog.asset` em Assets/QuackQuack/Content/Catalogs; dez relíquias e conteúdo de encontros existentes. Nenhuma substituição por espécies da imagem.

- Armas: Bumerangue, Ovos, Quack frontal, Aura de energia, Foguetes, Bolhas.
- Passivas: Pena aerodinâmica, Ninho reforçado, Pulmão poderoso, Amuleto do lago, Óculos de aviador, Sabão mágico.
- Evoluções: Tornado de penas, Incubadora de combate, Quack supersônico.
- Comuns: Formiga, Aranha, Vespa, Besouro, Caramujo Aquático; minibosses Libélula Alfa e Sapo-Cururu; chefe Sapo-Rei; Pato e bioma Lago.
- Modificações MEL ainda ausentes: contenção sob horda precisa diagnóstico; aura com oclusão; passivas sem aplicação efetiva; bumerangue deduplica toda a vida; XP só por contato e sujeito a supressão de pool; sem cura/atração; HUD anterior. Testes verdes refletem os contratos antigos, não atendimento à feature 002.

## Testes executados nesta rodada

| Verificação | Resultado | Evidência local |
|---|---|---|
| EditMode | 465/465, zero falhas/ignorados | TestResults/Polimento002/T001/EditMode.xml |
| PlayMode | 258/258, zero falhas/ignorados | TestResults/Polimento002/T001/PlayMode.xml |
| Conteúdo | 63 ScriptableObjects, zero erros; dois avisos UnsupportedType dos assets URP | Logs/Polimento002/T001/content.log, T073_CONTENT_VALIDATION_OK |

A importação inicial apresentou mensagens transitórias de compilação GUID em pacotes, recuperadas pelo processamento do Editor sem editar dependências. EditMode/PlayMode concluíram com exit 0 e XML Passed; isso não foi contado como falha de gameplay corrigida.

Testes de player não foram reexecutados nesta task: foram executadas suítes completas do Editor e builds/capturas IL2CPP de profiling nas duas plataformas. Essas capturas não equivalem a suítes de player ou aceite de entrada física. Os 34/34 históricos por player da T075 não são contados como execução nova.

## Equipamentos e condições

| Equipamento | Condição observada |
|---|---|
| Windows | Windows 11 Pro 10.0.26300, build 26300; Ryzen 7 5800X3D; RTX 4070 Ti, driver 32.0.16.1742; 67018248 KiB de RAM visível (~63,91 GiB) |
| Android conectado | S23 Ultra SM-S918B, Android 17/API 37, Adreno 740/Vulkan, 11044 MB RAM; USB autorizado, bateria 100%, status 5 (carregado); 36,1°C ao iniciar captura, 37,5°C após; medição preliminar de inventário 36,3°C |
| Outros Android | Intermediário/de entrada indisponíveis nesta coleta; sem extrapolação a partir do S23 |
| Entrada física | Teclado/controle/toque não reavaliados nesta task; confirmações anteriores são históricas, não nova evidência |

O ADB inicialmente estava bloqueado pelo sandbox; foi repetido com permissão. Nenhum Android estava conectado inicialmente; usuário conectou S23 para a medição atual. Serial não registrado neste documento.

## Método de profiling

Reutilizar `PerformanceBenchmark`, `PerformanceTelemetry` e `PerformanceProfileCapture` existentes, sem alteração de regras. Seed 71, fixed step 0,02s; warmup 250 ticks (5s), fase 200 de 1500 ticks (30s), fase 400 de 1500 ticks (30s). Pools: 512 projéteis, 512 pickups, ao menos 128 VFX, além de áreas dos inimigos; há tentativas intencionais de exaustão.

Player de benchmark tem alvo neutro cinemático com collider desabilitado e não concede XP. Não reproduz contenção física, partida completa, atração/cura futuras ou interação humana. Usar o mesmo cenário antes/depois como referência e acrescentar as cargas novas na T049, sem alegar comparação completa apenas com este cenário.

Helper isolado `Baseline002Build` segue `PerformanceProfileBuild`: habilita frame timings, usa IL2CPP/Development, adiciona captura à cena de benchmark e salva cena exclusiva. Nome `QuackQuack Baseline 002`, pacote `com.cemggames.quackquack.baseline002`, sem substituir jogo/perfil anterior. Saídas locais: Builds/Baseline002Windows/QuackQuackBaseline.exe e Builds/Baseline002Android/QuackQuackBaseline.apk dentro da cópia. Não usar helper no repo principal.

Análise: recortar 0,2s das bordas de cada fase, FPS =1000/média(frame_ms), percentis nearest rank, reportar GC/memória/counters indisponíveis e perdas de foco sem apagar amostras seletivamente. Exportar somente ao final. Conferir contagem, ausência de sobrescrita, população e saturação reais; GPU zero significa indisponível, não custo zero.

## Capturas atuais

| Plataforma | Inimigos | Amostras analisadas | FPS médio | Frame p95 ms | Frame p99 ms | Scheduler p95 ms | Unity máxima MiB | GC.Alloc máximo/frame |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Windows | 200 | 1777 | 60,00 | 16,667 | 16,676 | 0,937 | 245,95 | 0 bytes |
| Windows | 400 | 1777 | 59,99 | 16,667 | 16,670 | 2,803 | 246,95 | 0 bytes |
| S23 Ultra | 200 | 1777 | 59,99 | 16,680 | 16,780 | 3,707 | 239,19 | 0 bytes |
| S23 Ultra | 400 | 1738 | 58,68 | 16,689 | 33,337 | 5,329 | 239,45 | 0 bytes |

Windows: Direct3D12, Development IL2CPP, 1280×720; 3899 amostras totais, zero sobrescritas e zero perdas de foco reportadas. Build GUID `115aa13630a3412bb1fe93d13a6d745b`. Executado como processo auxiliar com Start-Process -WindowStyle Hidden, sem -batchmode/-nographics no player; não foi teste de interação em primeiro plano. GPU FrameTimingManager retornou zero e probe GPU Frame Time não forneceu amostras positivas: **GPU indisponível**, sem inferência de custo zero. MainThread p95 16,661 ms inclui espera para alvo 60 e não é custo puro de gameplay.

População mínima conferida nas fases: 200/400 inimigos, 512 projéteis, 512 pickups, 144/147 VFX (campos dos inimigos elevam acima de 128). Contadores de telemetria disponíveis (mask 15); zero frames com GC.Alloc positivo nos trechos analisados. Captura inteira retém aquecimento/arranque, que não foi misturado à fase estável.

Android: Development ARM64 IL2CPP, Vulkan, viewport 3088×1440, alvo 60; 3856 amostras totais, zero sobrescritas e zero perdas de foco. Build GUID `66431fefa2854a719a38edababc99698`. Captura iniciou 20:35:04 e completou 20:36:10 em 03/11/2026. Timings GPU válidos: p95 de 8,408 ms (200) e 11,886 ms (400); MainThread p95 19,006/21,022 ms, incluindo trabalho/espera do player Development. Contadores mask 15, mínimos reais dos pools iguais ao Windows e zero frames com GC.Alloc positivo nos trechos analisados.

**Observação a preservar**: S23 com 400 ficou em 58,68 FPS e p99 33,337 ms, portanto esta coleta não demonstra 60 FPS estáveis. Não houve otimização, exclusão seletiva de frames lentos ou ajuste de carga para melhorar o resultado. Investigar/comparar nas tasks de desempenho posteriores; T001 registra o ponto de partida, não corrige regressão nem declara aprovação da meta.

As resoluções Windows/Android diferem; comparar futuros resultados dentro de cada plataforma nas mesmas condições e protocolo, não diretamente o custo entre GPUs. Frametimes/FPS limitados a 60 são baseline deste workload, não demonstração de todos os frames abaixo de 16,6667 ms ou aceite de partida/latência/sessão térmica. Sem orçamento absoluto de memória, valores Development não constituem aprovação de RAM.

## Evidências de build/captura

| Artefato local | Identidade/resultado |
|---|---|
| Logs/Polimento002/T001/windows-build.log | BASELINE002_PROFILE_BUILD_OK StandaloneWindows64, exit 0 |
| Logs/Polimento002/T001/android-build.log | BASELINE002_PROFILE_BUILD_OK Android, exit 0 |
| Logs/Polimento002/T001/windows-player.log | ticks 0/250/1750/3250, T074_PROFILE_COMPLETE, exit 0 |
| TestResults/Polimento002/T001/windows-60.csv | 3899 linhas; JSON, gpu.csv, markers.csv e summary.json no mesmo prefixo |
| TestResults/Polimento002/T001/android-60.csv | 3856 linhas; JSON, gpu.csv, markers.csv e summary.json copiados da captura completa do pacote baseline002 |
| Windows executável | 667648 bytes; SHA-256 353A7873D63307B5A983790FED548B52F40C4C3EB750D06D60E3182F7CE06C9F; requer pasta inteira da build |
| Android APK | 45295281 bytes; SHA-256 D0252810EC5BF8E1B52341A6634BBCE482029EC2BA7E92B381D1C00A97DA3632; manifesto userLandscape |

## Limites e estado da T001

Baseline novo coletado nos dois equipamentos disponíveis, com versões/catálogos/testes e limitações registrados. Nenhum diagnóstico/correção MEL foi iniciado; T002 e seguintes permanecem pendentes.

Continuam sem novo aceite: entrada física por família, gameplay de 15 minutos e feedback de participantes, contenção da arena sob pressão, aparelhos intermediário/de entrada, opção 30 FPS e sessão térmica prolongada. GPU Windows indisponível nesta captura. A existência de XMLs verdes não valida as melhorias futuras. A T075 antiga mantém seu checkbox aberto e suas evidências históricas separadas.

## Rastreabilidade por SHA-256

| Arquivo | SHA-256 na fonte |
|---|---|
| Packages/manifest.json | 5C07CC33BB93334ED832624562A5A06E40D97E25B98DC5D6250234A0C57309D4 |
| Packages/packages-lock.json | 1D2DF2DEAD6B62D26F70CBEE314C4D068F7F7CFA486A22899827FA2E7D3FDB52 |
| Scenes/PerformanceBenchmark.unity | ECF985A804CC7CCB74D9B64FA256A50155AFE63B1CEE0623262AEED63312A0AA |
| Systems/Diagnostics/PerformanceBenchmark.cs | A6F0E48DA7914660CAC01B79D003BAB56C239C15A593AB546B1998304177076E |
| Systems/Diagnostics/PerformanceTelemetry.cs | 676A730273419F8363CCA35A731D04166195CF93668BCCF621903EBF3304AF50 |
| Systems/Diagnostics/PerformanceProfileCapture.cs | 410E9B1024A37D033D7EB35C1EC71BCBB69771453C7A5C1F37B171953AA841CB |
| Content/Catalogs/PrototypeBuildCatalog.asset | 61A310E153CBE689E516D0030285B9067D28F758194608CDC6AA7F7DF8959B09 |
| Content/Catalogs/LakeEncounterCatalog.asset | B0FE5BD53AEF74C2EBF4F31D216D60E0BE1BA8CD4E1BF9FCB324885DE2AFD396 |
| Content/Relics/PrototypeRelicCatalog.asset | FF33924FFCDDFE36DA6B57CE563E774D94A91C3EF404C630986A38111452B3C4 |

| Captura local | SHA-256 |
|---|---|
| windows-60.csv | AEB0F3A0F7DACCA9BFD76CFD392F506952BDE9379AA880C763DFCC74810F0F6F |
| windows-60.csv.json | 9FC89E3A2689F9E2E7C19234ECAB7C36CF68D177468B52783C96E6AAD68498D4 |
| android-60.csv | AB1B024478AE003A5A09ECCB972982644EE4EE13E109B2939A7A48FAC62CB0FA |
| android-60.csv.json | B9936D4ABB56DB8B101A97A063F13B22A83769A3CC2B9E68CE292BB552391BC4 |

Os hashes de captura identificam evidências desta rodada, não valores que devam coincidir em nova medição. Android 400 apresentou 39/1738 frames acima de 25 ms; Windows e Android 200 apresentaram zero nessa faixa. Temperaturas acima são do sensor de bateria, não leitura de CPU/GPU. As três regravações URP da cópia foram mantidas isoladas; para comparações futuras repetir o mesmo processo de importação/build.

## Reprodução da coleta atual

1. Checkout do commit medido e cópia completa nova conforme quickstart; comparar arquivos/hashes antes de importar e preservar configurações originais fora da cópia.
2. Executar EditMode, PlayMode e ContentValidationWindow.ValidateForCI como no quickstart. Não aplicar as tasks futuras para tentar tornar o baseline mais favorável.
3. Na cópia, preparar helper Editor equivalente ao PerformanceProfileBuild existente, com produto/pacote/cena/caminhos exclusivos descritos acima; chamar Baseline002Build.Windows com -buildTarget Win64 e Baseline002Build.Android com -buildTarget Android. Ambos usam BuildOptions.Development e ConfigureTarget do pipeline comum. Helpers locais desta rodada: Temp/Baseline002Build.cs.txt e Temp/Polimento002Validation/Assets/QuackQuack/Editor/Baseline002Build.cs.
4. Fechar Editor/compilação antes da captura Windows. Executável: `Temp/Polimento002Validation/Builds/Baseline002Windows/QuackQuackBaseline.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -profileTargetFps 60 -profileOutput <repo>/TestResults/Polimento002/T001/windows-60.csv -logFile <repo>/Logs/Polimento002/T001/windows-player.log`. Não usar -batchmode/-nographics para medir GPU/renderização.
5. Instalar somente APK do pacote baseline002 no Android autorizado, abrir launcher e manter desbloqueado; exporta em cache externo T074-profile.csv e arquivos .json/.gpu.csv/.markers.csv. Copiar via ADB pull para TestResults/Polimento002/T001/android-60.csv e sufixos correspondentes. Não alterar resolução, alvo ou quality silenciosamente entre comparações.
6. No CSV, 200 inimigos: ticks 260–1740 inclusive, warmup=0; 400: ticks 1760–3240 inclusive, warmup=0. Calcular como no método; checar mínimos reais dos pools. CSV de probes nativos pode ter nomes duplicados ou pontuação: usar ordem de NativeMarkers/NativeMarkerUnits no JSON para interpretar colunas e distinguir homônimos, sem deslocar valores. O CSV principal de telemetria possui cabeçalhos únicos. Helper de análise local: Temp/AnalyzeBaseline002.ps1.

Arquivos locais regeneráveis não precisam permanecer disponíveis em outro checkout; o commit, hashes, método, configurações e resultados resumidos permitem repetir a captura. As referências históricas da T074 usam outro pacote e não são sobrescritas.

Caminhos sem prefixo Packages acima são relativos a Assets/QuackQuack. Reprodução de testes e critérios futuros em [quickstart.md](quickstart.md); coleta anterior, apenas histórica, em [T074](../001-prototipo-lago/test-report-t074.md).
