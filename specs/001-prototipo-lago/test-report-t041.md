# Validação de T041

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T041 concluída. ExperiencePickup implementa IPoolLifecycle e coleta física
por Collider2D trigger com PlayerController vivo/ativo. Initialize exige uma
instância adquirida do PoolService ativo da família Pickup. Recompensa aceita
concede XP uma vez e devolve a instância; ResetState limpa sessão, quantidade e
trava de coleta. OnTriggerStay permite a coleta após retomar uma pausa ainda
sobreposta, sem conceder XP durante a pausa/seleção ou em estados terminais.

RegisterPool cria objetos somente na fábrica de pré-aquecimento. Spawn usa Acquire,
retorna null ao saturar e devolve a instância em erro de configuração. A consulta
PoolService.IsActive verifica propriedade, estado ativo e família; um objeto fora
do pool não pode ser inicializado mesmo chamando OnAcquire manualmente.

ExperienceState no Core recebe uma cópia dos limiares cumulativos, positivos e
estritamente crescentes, sem valores de balanceamento no controlador. Mantém XP
total/nível e rejeita recompensas não positivas ou overflow sem mutar o estado.
UpgradeSystem.InitializeProgression valida a configuração antes de vinculá-la à
partida e impede reinicialização. TryCollectExperience avança os níveis durante
Playing e abre ofertas com IDs distintos, usando a fila de escolhas acumuladas.

A resolução abre a próxima escolha antes de retomar o combate. O último resultado
retoma Playing uma vez; callbacks obsoletos não repetem escolhas. Se não houver
opções elegíveis, os níveis pendentes são consumidos sem deixar a partida congelada.
As ofertas continuam usando os mesmos recursos e regras da T040, com catálogo ou
na fronteira de IDs pré-filtrados.

Os seis testes PlayMode T035 foram convertidos de reflexão para chamadas tipadas
e passaram com colisões reais, retomada, múltiplos níveis e abandono na cena Lake.
Três casos adicionais verificam pausa sobreposta, saturação/retorno após falha e
rejeição de objeto sem pool. Oito testes EditMode cobrem limiares, cópia dos dados,
contagem de níveis, recompensas inválidas, overflow, ofertas esgotadas e
inicialização atômica/limpeza por Dispose.

| Suíte final | Aprovados | Falhas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 191 | 0 | 0 | 0 |
| PlayMode | 43 | 0 | 0 | 0 |

Nenhum erro de compilação. As seis falhas esperadas de T035 foram resolvidas e
suas asserções comportamentais agora foram exercitadas. A fase vermelha original
está registrada em T035 e nas execuções até T040. Não houve falha de regressão.

Execução em Temp/T033Validation, batchmode/nographics/runTests, plataformas de
teste EditMode e PlayMode. XMLs: TestResults/t041-editmode.xml e
TestResults/t041-playmode.xml, com logs correspondentes em Logs/. Evidências
regeneráveis permanecem fora do Git. O Unity gerou os .meta de scripts/testes e
Gameplay/Pickups; GUIDs preservados ao copiar. Campos vazios do .meta da pasta
preservam os espaços gerados pelo Unity; scripts e documentação sem problemas
de whitespace.

Não foram alteradas cenas ou criados catálogos/prefabs autorais. A composição de
XP é exercitada pela fixture, que carrega a arena e registra o pickup real pelo
pool; o Bootstrap normal ainda mostra a arena de treino US1. Conteúdo de armas,
catálogo final e cartas continuam nas T042–T044. Isso não representa aceite de
US2 jogável completa, builds, plataformas, desempenho ou playtest humano.
T042 em diante continuam pendentes.
