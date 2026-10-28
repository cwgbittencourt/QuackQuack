# T066 — Joystick e botões móveis

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T066. Próxima task: T067.

## Entrega

MobileInputAdapter é MonoBehaviour/IPlayerInput e implementa SetMove, SetQuack,
SetPause e Read do contrato T062, com Source Touch. Usa a normalização comum de
PlayerCommand; conserva intensidade parcial e neutraliza vetores não finitos.
Bordas são consumidas uma vez; manutenção não repete e tap entre leituras é aceito.
Não contém regras de movimento, Fôlego, combate, cooldown ou pausa de partida.

MobileJoystick converte pointer down/drag/up em direção radial e deslocamento do
handle, limitado pela geometria de base/knob. Captura um dedo por vez e ignora
movimento/soltura de outros. MobileActionButton captura um dedo independente para
Quack ou Pause. Movimento e ambos os botões podem ser mantidos simultaneamente.
Soltura rearma; cancelamento e desativação descartam também bordas ainda não lidas.
Perda de foco/suspensão limpam o estado e a posse dos dedos. O adaptador ignora
callbacks enquanto desativado. A vida útil é controlada pelo GameObject.

MobileControlsSetup gera pelo Unity o prefab MobileControls: CanvasScaler de
referência 1920×1080, GraphicRaycaster, adapter e controles com referências
serializadas. Labels/knob não interceptam raycasts. O adapter garante EventSystem
com InputSystemUIInputModule quando necessário. O assembly Editor passa a referenciar
o assembly Platform existente para montar o prefab; nenhum pacote foi adicionado.

InputParityTests agora instancia os três adaptadores concretos e usa chamadas
tipadas, removendo o shim de reflexão para entrada. Nove casos novos de
MobileInputAdapterTests verificam callbacks, normalização, posse de ponteiros,
entradas simultâneas, cancelamento, desativação e foco/suspensão. Um caso de
MobileTouchscreenTests usa o prefab nativo, Touchscreen sintético e o módulo UI
real: dois dedos mantêm movimento e Quack, depois Pause, e as solturas neutralizam
o movimento sem repetir bordas. As fixtures preservam configurações/dispositivos
e EventSystem anterior, removendo somente seus próprios objetos/dispositivos.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução | Total | Aprovados | Falhas esperadas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| Paridade antes da implementação | 15 | 10 | 5 | 0 | 2 |
| Paridade + testes móveis focados | 25 | 25 | 0 | 0 | 0 |
| PlayMode completo | 227 | 214 | 13 | 0 | 2 |

As cinco falhas iniciais eram exclusivamente ausência de MobileInputAdapter/T066.
Os 15 casos de paridade passaram após implementação, incluindo checkpoints reais
do PlayerController e rejeição em Won/Lost/Abandoned/Results. Os dez novos casos
passaram também na suíte completa. Todos os 199 casos antes aprovados permaneceram
aprovados; cinco passaram de vermelho para verde e dez foram acrescentados.
As 13 falhas restantes são nove conhecidas da T068/T069 e quatro da T070.

O gerador MobileControlsSetup.Build terminou com exit code 0. Prefab e .meta foram
produzidos pelo Unity e copiados com GUIDs preservados, assim como as metas dos
seis scripts novos. Nenhuma cena, configuração de projeto ou regra Core foi alterada.
A captura Logs/t066-controls.png (1280×720) foi inspecionada: base/knob à esquerda,
QUACK à direita abaixo e PAUSAR acima, com labels visíveis, sem corte na fixture.
É uma captura de componentes, sem cena de gameplay ou aprovação de UX.

Comandos: autoria com -batchmode -nographics -quit -executeMethod
QuackQuack.Editor.MobileControlsSetup.Build; testes com -batchmode -runTests
-testPlatform PlayMode -testResults <arquivo> -logFile <arquivo>. A fase vermelha
usou -nographics; testes móveis e suíte completa mantiveram gráficos.
Evidências regeneráveis ignoradas: TestResults/t066-red.xml, t066-mobile.xml,
t066-playmode.xml, Logs/t066-*.log e Logs/t066-controls.png.

## Limites

O prefab está disponível para instanciar/revisar e o adapter pode ser injetado em
PlayerController.Initialize. As cenas continuam escolhendo teclado por padrão;
a seleção automática/mapa unificado pertence à T067. Safe Area e integração do HUD
continuam nas tarefas correspondentes. Não houve build Windows/Android, aparelho
real, playtest humano ou perfil de desempenho nesta tarefa. O teste Touchscreen
comprova roteamento sintético no Editor, sem substituir aceite de Android físico.
EditMode não foi repetido porque Core/regras não mudaram; última evidência T061:
456/456 aprovados. Não se reapresenta o test player T064 como validação da T066.
