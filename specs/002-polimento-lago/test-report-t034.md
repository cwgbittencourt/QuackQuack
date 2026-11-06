# T034 — projeção de progresso e recursos

Data: 06/11/2026. Base: `931e98c`. Escopo somente T034; implementação visual e dicas permanecem T035–T036.

## Implementação

`ExperienceState.Progress` produz `ProgressSnapshot` readonly por valor a partir da curva cumulativa já copiada/validada pelo estado. A leitura usa o nível já processado por TryAdd e os dois limiares correspondentes, em O(1), sem copiar arrays nem recalcular níveis. Fração = (XP − anterior)/(próximo − anterior), calculada em double antes da projeção float. No último limiar e após excedê-lo, anterior/próximo ficam iguais ao último valor e Fraction=1, sem divisão por zero ou limiar inventado. Curva inicializada vazia também está completa.

`ProgressSnapshot` expõe Level, Experience, PreviousThreshold, NextThreshold e Fraction. O construtor é interno ao Core, e o estado default permanece seguro: nível 1, XP/limiares/fração zero. Esse default representa progressão ainda não inicializada, diferente de uma curva conhecida e esgotada. Nenhuma referência à curva ou ao estado mutável sai no snapshot.

`UpgradeSystem.Progress` delega ao mesmo ExperienceState e retorna default antes da inicialização, preservando as propriedades anteriores Experience/Level e o fluxo de ofertas. A projeção reflete todas as subidas antes de resolver as escolhas e não concede XP adicional.

`HudSnapshot` transporta Progress num parâmetro opcional final do construtor, preservando chamadas existentes. Health01/Breath01 normalizam pelos máximos efetivamente recebidos, com clamp [0,1]. Leituras não finitas ou máximo não positivo retornam zero para não propagar NaN/Infinity à barra; valores brutos e demais dados permanecem preservados. A divisão em double também evita overflow intermediário com floats finitos extremos.

`PlayerController.Snapshot` e GameplayHud ainda não consomem/renderizam esse progresso: essa ligação é explicitamente T035. A T034 não remove labels nem introduz dicas, cenas, barras ou geradores novos.

## Testes e resultados

As 12 falhas de ExperienceStateTests da T033 ficam verdes sem alterar esses testes. Os sete casos de recursos normalizados de GameplayHudTests também passam pela implementação real. Foram adicionados 12 casos diretos em ProgressSnapshotTests: defaults/inicialização, transporte por valor e compatibilidade dos dados brutos, oito entradas inválidas de recursos, extremos finitos e leitura aquecida sem alocação gerenciada no trecho medido.

| Execução final | Resultado |
|---|---|
| EditMode — EditMode.xml | **710/710 aprovados**, zero falhas/ignorados, código 0 |
| PlayMode — PlayMode.xml | **553 casos: 546 aprovados e 7 falhas esperadas de T035/T036**, zero ignorados, código 2 |

As sete pendências PlayMode são: duas de T035 (labels numéricos ainda visíveis e PlayerController ainda enviando o Progress default em vez do progresso de UpgradeSystem) e cinco pela ausência de ContextualHintsView em T036. Não há falhas fora dessas pendências. Os testes T033 não foram enfraquecidos nem ignorados.

O teste de alocação mede somente mil leituras aquecidas de projeção de progresso/recursos no Editor, não desempenho global, CPU/GPU ou dispositivos. Nenhuma aprovação de gameplay/plataforma é derivada dele.

## Reprodução e limites

Unity 6000.4.3f1, cópia isolada ignorada em `Builds/Polimento002Validation`. O helper privado do laboratório T032 permanece fora de Assets durante testes. Logs em `Logs/Polimento002/T034` e XMLs em `TestResults/Polimento002/T034`, não versionados. Metas dos dois scripts novos geradas pelo Unity na cópia e preservadas na raiz. Nenhum asset serializado do jogo ou configuração do Editor principal foi alterado.

T034 entrega a regra/projeção, sem antecipar T035 (produtor do jogador/renderização), T036 (dicas), T038 (composição) ou T040 (aceite). Os achados de balanceamento da T032 continuam independentes e não foram implementados.
