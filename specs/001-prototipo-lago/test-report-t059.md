# T059 — Save versionado de relíquias

Data: 2026-10-27. Unity 6000.4.3f1, revisão 39d1a88d4dd1.
Escopo: somente T059. T060 em diante permanecem pendentes.

## Implementação

JsonSaveService recebe caminho e dez IDs conhecidos, copiando o catálogo de IDs.
SaveData/SaveOperationResult expõem snapshots somente leitura, status e diagnóstico.
O schema 1 contém schemaVersion, unlockedRelicIds e equippedRelicIds. CreateDefault
não faz I/O e retorna listas vazias. Migrate normaliza versões 0/1, filtra IDs
desconhecidos/duplicados e mantém somente os primeiros três equipamentos distintos
desbloqueados. A versão 0 é uma política de compatibilidade definida nesta task,
sem alegação de que esse formato foi publicado anteriormente.

Load prefere principal, recupera backup ou entrega defaults. É somente leitura,
inclusive na recuperação. Tipos inválidos, schema duplicado, estrutura truncada e
JSON sem schema não viram progresso utilizável. Null/ausência de listas é recuperável.
O leitor JSON da biblioteca .NET recebe uma verificação prévia de fechamento da
estrutura, necessária porque aceitou EOF com objeto incompleto no teste inicial.
Não foram adicionadas dependências. Gravação usa JsonUtility e UTF-8.

Save escreve um temporário exclusivo no mesmo diretório, faz Flush(true) e usa
File.Move na primeira gravação ou File.Replace ao substituir o principal.
Somente principal anterior válido vira backup; principal corrompido não substitui
backup bom. RestoreBackup promove a cópia validada pelo mesmo caminho atômico,
preservando o backup. Não existe sequência que delete o principal antes de mover.
Falhas retornam Failed sem propagar exceções de armazenamento esperadas ou expor
caminhos/conteúdo. A limpeza só alcança o temporário criado pela operação.

Versão futura no principal recusa Save/RestoreBackup; backup futuro é preservado.
Arquivos ilegíveis ou maiores que 1 MiB também não são sobrescritos pelo serviço.
Limite de profundidade: 32. Temporários interrompidos são ignorados, não promovidos.
I/O é síncrono; a composição deve manter um proprietário por caminho e chamar o
serviço fora do loop crítico. Nenhum diretório pessoal é escolhido automaticamente.

## Validação observada

Execução em `Temp/T033Validation`, cópia isolada de Assets/Packages/ProjectSettings.
Unity gerou os novos arquivos meta; os existentes foram preservados. Cenas, prefabs,
assets serializados e dependências não foram alterados.

| Etapa | Total | Aprovados | Falhas | Exit code |
|---|---:|---:|---:|---:|
| T048 antes da implementação | 11 | 0 | 11 esperadas | 2 |
| EditMode completo após T059 | 436 | 436 | 0 | 0 |
| PlayMode completo | 150 | 150 | 0 | 0 |

SaveServiceTests foi migrado de reflexão para chamadas tipadas: os 11 casos passaram.
Os 28 novos JsonSaveWriteTests passaram. Cobrem criação de diretórios, reabertura
em outra instância, rotação de gerações, principal corrompido/backup preservado,
restauração, Load sem mutação, escrita bloqueada, destino inválido, limpeza de
temporário próprio, temporário interrompido ignorado, migração, versões futuras,
normalização/slots, snapshots imutáveis, Unicode/escapes, tipos/framing inválidos,
limites de entrada e argumentos inválidos.

O teste de falha mantém o principal aberto no Windows sem compartilhamento de
exclusão. File.Replace falha; principal e backup permanecem com o conteúdo anterior,
e o temporário da tentativa é removido. Nenhum save real foi usado.

RelicSaveRoundTripTests usa os IDs/loadout do catálogo real em PlayMode: salva,
abre outro serviço, restaura em outro loadout, reaplica três vezes, compara efeitos
por atributo/terreno e remove equipamentos, voltando aos valores base sem acumular.
Diretórios de todas as fixtures são exclusivos sob temp; a limpeza valida caminho
absoluto e prefixo de propriedade antes de remover recursivamente.

A primeira suíte teve 433 aprovados e uma falha: JSON truncado aceito pelo leitor.
Após acrescentar a verificação de estrutura e casos de vírgula final/schema com
zero inicial, a suíte completa passou com 436 casos.

Comandos Unity: `-batchmode -runTests -testPlatform EditMode|PlayMode
-testResults <arquivo> -logFile <arquivo>`. EditMode usou `-nographics`; PlayMode
manteve gráficos/áudio. Fase vermelha filtrou QuackQuack.Tests.EditMode.SaveServiceTests.
Saídas ignoradas: `TestResults/t059-red-editmode.xml`, `t059-editmode.xml`,
`t059-playmode.xml` e `Logs/t059-*.log`.

## Limites do aceite

Não houve build Windows/Android, teste em aparelho ou simulação de queda física
de energia. Atomicidade/Flush foram exercitados no filesystem Windows deste host;
suporte e durabilidade no Android precisam de validação em dispositivo. Quando a
substituição atômica não é suportada, o serviço retorna Failed, sem fallback destrutivo.

Esta task fornece o serviço e o round-trip de loadout, sem conectar LakeRelics ao
diretório pessoal. A T060 deve compor o caminho, carregamento e pontos de salvamento.
O schema inicial não armazena carteira/RunResult, configurações, recordes ou desafios;
esses campos exigirão ampliação versionada na integração. O resultado T058 não é
creditado por consultar ou salvar relíquias. A arena continua mantendo estado de
relíquias somente na sessão atual até essa composição.
