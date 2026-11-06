# US4 — cura e coleta: validação jogável T032

Data: 05/11/2026. Base: `eb03364`. **Roteiro funcional Windows concluído, com achados de balanceamento. Android pendente; T032 permanece aberta.**

## Escopo e disponibilidade

Roteiro: chance independente de cura comum, XP garantido de comuns/minibosses, cura de miniboss, atração rápida/persistente, pausa e tratamento de vida cheia. Chefe final sem XP nem cura conforme revisão T030. Barras individuais de especiais pertencem à T037, fora desta entrega.

O usuário informou **“Somente Windows por enquanto”**. Android/S23 permanece pendente; os aceites anteriores de atração em test players não substituem este aceite jogável. T014/8BitDo também permanece independente e adiada.

## Builds e composição

Unity **6000.4.3f1**, Windows x64 **IL2CPP**, cenas RunBootstrap/LakeRun e pipeline de produção. Geração na cópia isolada ignorada `Builds/Polimento002Validation`; projeto/Editor principal preservados. Produto e armazenamento local identificados como validação, separados do jogo habitual.

| Artefato relativo à cópia isolada | SHA-256 de GameAssembly.dll |
|---|---|
| `Builds/T032/Windows/QuackQuackT032.exe` | `7762CF803F186D2E43DCFBFC4B0D4F7BD7455B66D49E2FBE549D1E15198B3F78` |
| `Builds/T032Lab/Windows/QuackQuackT032Lab.exe` | `E84A99B30DB1A6B9B84FD74A9F4750E567ADC7D1F1267917FFA710A077775512` |
| `Builds/T032R2/Windows/QuackQuackT032.exe` (final) | `42E5ECCFE438F3DE7CDC5D1CC317512AFF5643FC2C147F338E7453638C3F8228` |
| `Builds/T032LabR2/Windows/QuackQuackT032Lab.exe` (final) | `8DFE252AAAE34AE065F67F2D98E6653143D5CC5CB29A581B8E10BD0A4B970CC4` |
| `Builds/T032LabR3/Windows/QuackQuackT032Lab.exe` (reinício corrigido) | `1E4D98DC34A240F3F7036503B20C91F0474328CF0AC783AB8E88519E74C39D6E` |

Ambas as builds concluíram com código zero e marcador `PROTOTYPE_BUILD_OK`. A build normal foi gerada antes de adicionar o script privado do laboratório; preserva o fluxo normal de ondas, ataques, XP e melhorias. Inicialização observada na primeira escolha de melhoria, com nível 2.

**Auditoria da cópia:** após a rodada inicial, a comparação de todos os arquivos rastreados identificou no `UpgradeSystem.cs` da cópia um método terminal de XP antigo, sem chamadas no fluxo atual. O arquivo foi sincronizado com a raiz antes das builds R2 finais, ambas aprovadas com código zero. Todos os scripts, assemblies e arquivos de entrada rastreados conferiram por hash. As três outras diferenças eram dados de renderização gerados/repreenchidos pelo Unity (perfil de volume, lista de runtime settings e prefilter de shaders); não foram levadas ao repositório. As builds R1 ficam como histórico; aceites obtidos nelas são identificados separadamente.

O laboratório utiliza partida, catálogos, inimigos, dano letal, pools, ledger, pickups, física e driver de atração reais. Desativa ondas/scheduler/armas para isolar observações, permite dano/preenchimento de vida, emissão de mortes e teletransporte de diagnóstico. O relógio/atração usam o estado comum de pausa. O botão de resolver cartas confirma escolhas pela API existente. Não serve para avaliar dificuldade, balanceamento percebido da partida inteira ou desempenho.

Helpers, configurações de build e binários permanecem apenas na cópia ignorada. Fontes auxiliares locais em `Logs/Polimento002/T032/Validation032Lab.cs` e `Validation032Build.cs`; não são scripts do produto. Logs em `Logs/Polimento002/T032`.

## Frequência registrada

Amostra **nativa IL2CPP**, laboratório, seed **42**, 100 mortes reais de Formiga em posição distante do Pato, política autoral inalterada de 0,10:

| Medida | Observado |
|---|---:|
| Derrotas comuns | 100 |
| Obrigações de XP | 100 |
| Curas comuns | 8 |
| Cura de miniboss | 0 (nenhum miniboss nessa amostra) |
| Recompensas coletadas/descartadas | 0 / 0 |
| Pendências por falta de view | 0 |
| XP concedido à distância | 0 |

Marcador `T032_OBSERVATION stage=sample100` em `Sample100R2.log`, repetindo os mesmos resultados da R1 (`Sample100.log`). A execução foi pausada após a emissão e encerrada depois de registrar os contadores. **8/100 é a frequência dessa sequência**, sem alegar comprovação estatística da probabilidade, exigir quota ou ajustar a chance para produzir dez. As fronteiras e independência do RNG já são cobertas pelas T024–T031.

## Roteiro e aceites

| Caso | Procedimento | Resultado |
|---|---|---|
| W01 — partida normal | Movimento, Quack, XP próximo/afastamento, escolha e pausa/retomada | R1: usuário informou “A atração funciona corretamente sem nenhum problema extra”; não separou confirmação de cada comando/etapa. Relatou cura frequente demais e crescimento de Bolhas ainda pequeno |
| W02 — minibosses e cura proporcional | Laboratório: DANO 80%, LIBÉLULA/CURURU e aproximar. Conferir XP e ganho de até 50, limitado à falta | R2: usuário confirmou “funcionou corretamente com os 2” no roteiro 20→70 de vida/50 XP. Log também registra cura e XP de miniboss |
| W03 — comum e 15% | Observar amostra sem quota; aproximar de cura comum com falta de vida e conferir ganho até 15 | R3: usuário confirmou “funcionou corretamente”. Seed 42, nove mortes comuns, nove drops de XP e uma cura: 18 XP coletados, vida 20→35, sem pendências |
| W04 — vida cheia antes de capturar | Gerar cura com vida 100 e aproximar; deve ficar no chão. Aplicar dano e coletar | R2: usuário confirmou “funcionou corretamente” |
| W05 — perseguição e velocidade | Com falta de vida, CAPTURAR/AFASTAR e observar XP/cura vindo de fora do raio; também aproximar andando normalmente | R1: atração normal aprovada. R2: roteiro de perseguição dos dois minibosses aprovado |
| W06 — pausa e descarte | CAPTURAR/PAUSAR congela; retomar conclui. CAPTURAR/ENCHER descarta cura sem conceder nem voltar ao chão | R2: usuário confirmou “funciona corretamente” para o roteiro de ambos os casos |
| A01–A06 — S23/toque | Repetir W01–W06 com builds Android atuais | Pendente por decisão do usuário |

Testes de regras/integração anteriores: T030 686 EditMode/499 PlayMode; T031 533/533 PlayMode, incluindo 34 casos de recompensas. São evidências históricas complementares, sem representar novos testes desta rodada ou aprovação de gameplay.

## Achados de balanceamento e continuidade

O usuário considerou a taxa de cura alta e pediu torná-la mais difícil. A política vigente é 10% por morte comum; **novo percentual ainda não definido**. A observação de 8/100 na amostra não invalida esse relato de percepção, nem autoriza escolher silenciosamente uma chance diferente.

Também pediu dobrar novamente o crescimento do Sabão Mágico. O ajuste solicitado corresponde a **32% de raio por nível, teto de 160%**, somente em Bolhas, mantendo raio de acerto e visual iguais: base 0,20; N1 0,264; N5 0,52. Esses valores descrevem o próximo ajuste, **não foram aplicados** nesta task de validação. O balanceamento vigente continua 16%/80%, aprovado na T023.

Momento recomendado informado ao usuário: um incremento específico após este aceite de cura/coleta, antes do trabalho de HUD. Registrar a nova chance, atualizar os artefatos de balanceamento e retestar cura/Sabão nesse incremento. Nenhuma task posterior foi executada pela T032.

A primeira tentativa de abrir o laboratório foi interrompida por Esc. O usuário esclareceu que foi acidental e autorizou retomar. Na retomada, a build final foi reaberta; a ferramenta de inspeção encontrou a janela minimizada e não obteve captura após restauração. Roteiros manuais foram enviados ao usuário, sem converter ausência de captura em aprovação.

Durante os testes R2, `PlayableR2.log` revelou uma exceção no reinício do helper: ele destruía o bootstrap antes de encerrar a partida. A revisão **R3** chama `TryAbandon()` antes de destruir serviços e carregar RunBootstrap. Mudança restrita ao helper privado; scripts e dados autorais do produto permanecem iguais. Build R3 aprovada com código zero em `WindowsLabR3.log`. Solicitado reteste de reinício e cura comum isolada; os aceites R2 acima são relatos do usuário, sem ocultar a falha de instrumentação encontrada no log.

**Reteste R3 concluído:** o usuário respondeu “funcionou corretamente” ao roteiro de reinício e coleta de cura comum. `PlayableR3.log` registra reinício com contadores/XP zerados e vida 100, além da coleta com 18 XP e ganho de 15 de vida. Nenhuma exceção registrada nessa execução. Scripts, assemblies e ações rastreados conferiram novamente com a raiz após a geração.

O aceite funcional Windows não aprova o balanceamento percebido da chance atual de cura nem o crescimento atual de Bolhas: ambos receberam pedidos de revisão acima. Android não executado nesta rodada por indisponibilidade declarada; T032 não marcada como concluída. Não houve alteração de regras/assets do produto nem execução de tasks posteriores.
