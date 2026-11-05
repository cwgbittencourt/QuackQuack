# T029 — Atração compartilhada de XP e cura

Data: 05/11/2026. Base: `d3c2eda`. Escopo: atração centralizada, integração ao RunDirector e testes portáveis. T030 não iniciada.

## Revisão explícita do usuário

Durante a implementação o usuário rejeitou a regra anterior de cura capturada seguindo o Pato com vida cheia. A regra vigente é: **cura já capturada é descartada quando encontra vida cheia durante simulação**, sem aplicar cura, sem claim positivo e sem voltar ao chão. Cura ainda não capturada permanece no chão com vida cheia. Pausa/seleção suspendem o processamento; eventual descarte ocorre na retomada. Especificação, contrato C5, data-model, tasks, quickstart e decisions foram atualizados; o histórico anterior não constitui a regra atual.

`RewardState.Discarded` retém a prova do descarte deliberado, distinta de Claimed. Reset de uma view descartada não a devolve à fila e replay de identidade continua bloqueado pelo ledger. XP não participa desse descarte.

## Implementação

- `Core/Progression/AttractionState`: estado por lease (Grounded/Following/Discarded), captura inclusiva, perseguição persistente, movimento sem overshoot, pausa e cancelamento. Não depende de física/controles/cenas; usa somente posições e dados numéricos.
- `Systems/Pickups/PickupAttractionSystem`: um driver da partida percorre arrays já alocados de XP/cura; as views não recebem Update/corrotina. O RunDirector materializa antes e avança a atração somente durante simulação.
- `IPickupAttractionView` compartilha collider/transform, lease, estado, vínculo de alvo e coleta. Reset limpa o estado novo; lease diferente nunca herda captura. Cancelamento limpa o alvo e o estado de following sem conceder recompensa.

Raio de captura é o raio físico atual do CircleCollider2D do Pato × escala mundial uniforme positiva × multiplicador autorado (3). A geometria é relida a cada Advance; mudanças de raio/escala produzem efeito imediato e decoração não altera captura. Centro usa o offset físico transformado. Base autorada é aproximadamente 1,26 unidades. A comparação quadrática usa double dos dois lados, sem epsilon: a rodada inicial revelou que arredondamento assimétrico float no Mono podia rejeitar a igualdade exata.

Velocidade é a velocidade efetiva na água resolvida pelo UpgradeSystem/relíquias, incluindo lentidão vigente, × multiplicador autorado 1,25. Não usa penalidade por estar na terra nem duplica a resolução de passivas. Base 7,5; mudanças de loadout/lentidão são relidas. Uma vez capturado, afastar-se do raio não cancela a atração.

O movimento do pickup não faz cast contra paredes. Coleta somente após chegar à distância de contato dos dois círculos físicos; chamar o consumidor no contato reutiliza o mesmo guard/ledger/UpgradeSystem dos triggers. Não há concessão remota ao entrar no raio de atração. Pausa/seleção congelam movimento/coleta; morte/resultado cancelam e não concedem itens de chão. A física do Pato/inimigos não foi alterada.

O driver processa as views pertencentes à partida RunDirector. A arena de treino e o laboratório privado não foram ampliados para registrar pickups no driver. Liquidação final do XP do chefe e destino terminal completo das pendências continuam T030.

## Testes portáveis

`PickupAttractionTests` não usa UnityEditor, assets do projeto ou cenas autorais: cria definições/serviços reais e é compilável no player. Casos cobrem:

1. Dentro/igual/fora do raio físico, sem conceder XP no raio de captura.
2. Atualização de escala/raio do corpo e independência da decoração.
3. Perseguição após afastamento, passo grande e coleta única somente no contato.
4. Velocidade de água estando em terra, relíquia por terreno, remoção e lentidão.
5. Cura comum e de miniboss: vida cheia no chão não captura; vida cheia após captura descarta, limpa alvo e não reenfileira/nem cura depois.
6. Pausa e ChoosingUpgrade congelam; morte/Abandoned cancelam sem concessão.
7. Pickup atravessa parede sólida e o corpo do Pato continua bloqueado no teste físico.
8. Reuso pooled limpa captura/alvo e uma nova lease não herda following.
9. Mil Advances aquecidos sem alocação gerenciada no trecho medido.

A fixture inicial precisou do namespace PlayerCommand e das dez relíquias exigidas pelo contrato do catálogo. A falha de igualdade levou à correção numérica acima, mantendo o caso externo rejeitado. Depois dessas correções, a rodada focada anterior à duplicação do caso de cura por origem passou 14/14. A suíte final contém 15 casos de atração.

## Execução e limites

Unity 6000.4.3f1. As rodadas focadas usaram Temp/Polimento002Validation. A pasta deixou de existir quando o Editor principal foi aberto; a validação final foi reiniciada numa cópia isolada e ignorada em **Builds/Polimento002Validation**, com Assets/Packages/ProjectSettings atuais. Não foram alteradas configurações do projeto aberto para executar os test players.

| Verificação final | Resultado |
|---|---|
| EditMode | 685/685 aprovados, zero falhas/ignorados, código 0 |
| PlayMode completo | 492/492 aprovados, zero falhas/ignorados, código 0 |
| Test player Windows IL2CPP (15 casos) | 15/15 aprovados, zero falhas/ignorados, código 0 |
| Test player Android ARM64 IL2CPP (15 casos), SM-S918B/S23 Ultra | 15/15 aprovados, zero falhas/ignorados, código 0 |

XMLs locais em TestResults/Polimento002/T029: final-EditMode.xml, final-PlayMode.xml, native-Win64.xml e native-Android.xml. Logs em Logs/Polimento002/T029, ignorados pelo Git. Pacote do test player Android: com.cemggames.quackquack.validation002.t029tests, separado do jogo normal. Os scripts da entrega conferem por hash com a cópia validada. Helpers e binários de teste somente na cópia isolada, não versionados. Metas dos scripts/pasta novos gerados pelo Unity e preservados.

Esses testes não aprovam gameplay/frequência de drops, revisão artística, desempenho global CPU/GPU ou demais aparelhos. O aceite jogável continua T031/T032; T014 segue pendente. Os números de alocação limitam-se ao trecho central aquecido medido pelo teste.
