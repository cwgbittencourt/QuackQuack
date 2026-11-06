# T031 — integração de recompensas e ciclo de pickups

Data: 05/11/2026. Base: `0342d68`. Escopo: somente T031, sem alterações nas regras do produto.

## Composição e limites

`PickupRewardFlowTests.cs` usa os catálogos, definições e prefabs autorais de Pato, sete inimigos, armas, relíquias, pickups e chefe. Cada fixture cria uma partida isolada com `RunDirector`, `SpawnDirector`, `PooledEnemy`, `RewardLedger`, `PoolService`, `PickupAttractionSystem` e `UpgradeSystem` reais, além de persistência temporária própria. As primeiras emissões passam por spawn e dano letal reais; não são adicionadas diretamente ao ledger.

Pedidos de spawn explícitos com IDs altos impedem ondas automáticas adicionais durante os cenários controlados. Scheduler e armas ficam desabilitados para isolar recompensas. O adaptador do método privado `RunDirector.Reward` é usado somente para repetir notificações já emitidas por mortes reais; não substitui a primeira emissão. Coleta controlada usa a interface de contato da view ou o driver de atração de produção. Um caso separado aguarda contatos físicos reais de ambos os prefabs, sem chamada manual de coleta/atração.

A classe depende de `UnityEditor` para carregar conteúdo e criar cópias transitórias; fica protegida por `UNITY_EDITOR`. É evidência de integração no Editor, sem alegar execução nativa ou aceite físico Windows/Android. A suíte portátil de atração da T029 permanece disponível. T032 continua responsável pelo aceite jogável.

## Matriz de 34 casos

| Cobertura | Casos | Verificação |
|---|---:|---|
| Cinco comuns e dois minibosses | 7 | Morte real, posição/XP do catálogo, replay sem nova obrigação, coleta única, moedas e estatística únicas |
| Dano não letal, despawn e lease antiga de inimigo reutilizado | 1 | Nenhum drop antes da morte da lease vigente |
| Sequência de drops e isolamento de RNG | 1 | 24 mortes comuns com seed 31; replay, despawn, seis mortes de miniboss e geração/rerrolagem de ofertas não consomem sorteios comuns adicionais |
| Cura comum/miniboss com máximos 100/200 e falta pequena/grande | 6 | 15%/50% do máximo efetivo da instância, limitado à falta, claim único |
| Vida cheia antes da captura | 2 | Permanece no chão, sem seguir; depois de dano pode curar |
| Vida cheia após captura | 2 | Descarte definitivo, sem cura nem reenfileiramento |
| Jogador morto | 2 | Sem XP, cura ou reanimação; derrota cancela registros |
| Saturação e drenagem dos pools autorais | 1 | Preserva todos os IDs, posições, valores e pendências até coletar; reserva de cura de miniboss respeitada |
| Várias subidas por um pickup | 1 | 50 XP atravessam quatro limiares; quatro ofertas únicas, sem confirmação duplicada nem coleta durante a escolha |
| Pausa entre morte e entrega da notificação | 1 | Registros ficam Due sem views até retomar |
| Pausa, atração e devolução/rebinding de views | 2 | Congela posição/coleta; reuso limpa alvo e captura, preserva obrigação e usa nova lease |
| View coletada reutilizada por outra morte | 2 | Nova identidade/posição, sem vínculo ou atração herdados; nova coleta única |
| Abandono na escolha e dentro do callback de abertura | 2 | Rejeita confirmação antiga; conclui XP já em curso sem acesso indevido ao pool ou reenfileiramento |
| Derrota, abandono pausado e vitória real contra chefe | 3 | Cancela registros Grounded/Attracted/pendentes, mantém Claimed e não concede o chão; chefe sem pickups |
| Contatos físicos de XP/cura de miniboss | 1 | Unity entrega os triggers, concede uma vez e devolve ambas as views |

`MaxHealth` é somente leitura no estado atual do jogo. A variação 100/200 é autorada numa cópia transitória de `CharacterDefinition` antes de inicializar o jogador. Não foi criada mutação dinâmica de vida máxima nem modificado o asset canônico para satisfazer o teste.

## Conservação e rastreabilidade

Em cada checkpoint, a fixture compara IDs distintos, quantidade pendente com estados Due/WaitingForView e vínculo exclusivo entre cada registro Grounded/Attracted e uma view ativa. Registros coletados/cancelados/descartados não mantêm views. Os marcadores `T031_REWARD_FLOW` e `T031_RECEIPT` registram etapa, contagens, XP, saúde, partida, origem, source, lease, tipo, ordinal e posição. Para saturação, o log mostra amostras e a primeira pendência; as asserções percorrem todos os registros.

Rodada focada: **263 mortes comuns + dois minibosses = 265 derrotas**, com a chance autoral de 10%, sem forçar resultados do RNG. Foram **294 obrigações**: 265 de XP e 29 de cura (27 comuns e duas de miniboss). Pools originais de 256 XP/16 curas ficaram com **22 pendências**, XP concedido zero. A drenagem pelo driver real terminou com **294 Claimed**, **626 XP**, zero pendências/views de pickup, preservando a identidade e a posição de cada registro. Esses números documentam uma sequência determinística, sem alegar frequência estatística de gameplay ou medir desempenho.

## Execuções

Unity **6000.4.3f1**, cópia isolada ignorada `Builds/Polimento002Validation`, preservando o Editor principal. XMLs em `TestResults/Polimento002/T031`, logs em `Logs/Polimento002/T031`; nenhum desses resultados regeneráveis integra o commit.

| Rodada | Resultado |
|---|---|
| Focada inicial — `Flow.xml` | 30/30, zero falhas/ignorados |
| Focada ampliada — `FlowR2.xml` | 34/34, zero falhas/ignorados |
| PlayMode completo final — `PlayMode.xml` | **533/533**, incluindo **34/34** da nova classe, zero falhas/ignorados |

A rodada completa inclui as verificações adicionais explícitas de registros Grounded e Attracted antes de cada encerramento. O `.meta` do arquivo novo foi gerado pelo Unity e preservado. Não houve necessidade de corrigir código de produção nas rodadas focadas.

Não foram repetidas as suítes EditMode, builds nativas, testes com aparelhos/controles, profiling ou aceite visual/jogável. A presente entrega não encerra T032, T014 ou os gates globais de plataforma/desempenho.
