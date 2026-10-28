# T067 — Mapa de ações e entrada unificados

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T067. Próxima task: T068.

## Entrega

QuackQuackInput.inputactions define Gameplay/Move, Quack e Pause com grupos/esquemas
Keyboard, Gamepad e Touch. Bindings vêm desse asset: WASD/setas, Espaço/Esc,
stick esquerdo/Sul/Start e controles virtuais do joystick/botões móveis. Os três
adaptadores usam ActionPlayerInput, com clone privado e máscara de grupo, sem
habilitar ou alterar o asset autoral. O mapa preserva os contratos de intensidade,
bordas consumidas uma vez, cancelamento e descarte das entregas anteriores.

MobileCommandDevice recebe vetores e botões da UI por InputState.Change. Cada
MobileInputAdapter possui seu próprio dispositivo, ligado apenas ao clone Touch.
Não é um Gamepad, evitando ativar a entrada física com valores dos controles móveis.
O prefab uGUI continua recebendo Touchscreen pelo módulo UI já usado na T066.

PlayerInputConfiguration, em Resources/PlayerInput.asset, referencia mapa e prefab.
UnifiedPlayerInput torna-se a entrada padrão do PlayerController. A atividade mais
recente escolhe a fonte, sem somar vetores ou ressuscitar um dispositivo antigo
mantido. O prefab móvel é instanciado quando há Touchscreen ou plataforma móvel.
Entradas injetadas por chamadores continuam sendo propriedade desses chamadores.

PlayerController traduz fases em flags de contexto: gameplay em Playing/Boss,
somente Pause em Paused, nenhuma ação de gameplay em seleção/terminal/Results.
Ao reabilitar, movimento precisa neutralizar e botões precisam de nova ativação;
não há Quack/movimento vazando dos menus. Perda de foco limpa entradas antigas e respeita BackgroundBehavior; IgnoreFocus
permite novos comandos em background. Suspensão do app tem flag independente e
sempre bloqueia entrada até retomar; desativação também bloqueia as ações. A composição drena as bordas das fontes não selecionadas.

A composição preserva/restaura sendNavigationEvents, habilitando navegação/Submit/
Cancel apenas nos menus; cliques/toque permanecem disponíveis. Assim, o botão Sul
não ativa UI selecionada junto com Quack. Um caso isolado adicional encontrou
interferência na movimentação ao deixar um botão selecionado; a versão final
valida movimento, ausência de Submit no combate e Submit funcionando na pausa.

O EventSystem criado pela composição tem vida separada do prefab ocultável, para
que esconder joystick/botões não impeça usar cartas/resultados. EventSystem já
existente é reutilizado e não é destruído pela composição.

UnifiedInputSetup gera JSON pelo InputActionAsset.ToJson e configuração SO pelo
Unity. Não recria o arquivo de ações existente, preservando bindings e IDs. O novo
Resources e as metas foram gerados no Editor; nenhuma cena foi editada ou regravada.
Editor recebe referência direta ao pacote Input System já instalado, sem pacote novo.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução | Total | Aprovados | Falhas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| Contrato antes da implementação | 1 | 0 | 1 esperada | 0 | 2 |
| Primeira rodada de entradas | 45 | 41 | 4 de fixture | 0 | 2 |
| PlayMode completo final | 235 | 222 | 13 esperadas | 0 | 2 |
| Test player Windows x64 IL2CPP | 9 | 9 | 0 | 0 | 0 |

Oito casos novos: contrato de ações/grupos; fonte mais recente e clone autoral
inalterado; retorno de menus com direção/botões mantidos; pausa/suspensão; toque
isolado do gamepad físico; gamepad movendo/usando Quack/pausando o Pato real na cena;
criação dos controles móveis e ocultação em seleção mantendo EventSystem ativo;
política de foco/background e suspensão independentes. O caso de gamepad também
seleciona um Button real: Sul não deve submeter UI no gameplay e deve submeter
normalmente na pausa.
Todos os 15 casos de paridade e fixtures anteriores de teclado/gamepad/toque passaram.
Os 214 casos antes aprovados continuaram passando; as 13 falhas restantes são nove
conhecidas da T068/T069 e quatro da T070. Nenhuma regressão de regra foi observada.

Os quatro problemas da primeira rodada eram callbacks móveis testados sem política
de foco para dispositivos sintéticos: agora que passam pelo Input System, a fixture
usa/restaura IgnoreFocus e AlwaysGoesToGameView, como as demais fixtures. As
asserções foram preservadas. A autoria também exigiu ajustar a chamada de composite
binding para a API instalada (WithGroup após AddCompositeBinding).

A build Windows final registrou Build Finished, Result: Success, seguida de nove
casos aprovados e exit code 0: sete UnifiedInputRuntime e dois PlatformRuntimeSmoke.
Isso verifica empacotamento de Resources/mapa/prefab e funcionamento do dispositivo
virtual em IL2CPP, incluindo integração das cenas. É um test player Development
com cena do Test Runner, não release nem validação da pipeline de produto T070.

O gerador terminou com exit code 0. Assets/metas nativos foram copiados preservando
GUIDs; scripts existentes mantiveram suas metas. Não houve acesso ao save pessoal:
as integrações novas usam o treino de sessão e os smokes T064 mantêm save próprio.
Não foram geradas novas capturas nesta tarefa. EditMode não foi repetido porque
Core/regras não mudaram; última evidência T061, 456/456.

Comandos: -batchmode -runTests -testPlatform PlayMode -testResults <arquivo>
-logFile <arquivo>. Autoria: -batchmode -nographics -quit -executeMethod
QuackQuack.Editor.UnifiedInputSetup.Build. Test player: -testPlatform
StandaloneWindows64 -testCategory UnifiedInputRuntime;PlatformRuntimeSmoke
-playerHeartbeatTimeout 120 -buildPlayerPath <pasta>. Testes finais mantiveram gráficos.
Evidências ignoradas: TestResults/t067-*.xml, Logs/t067-*.log e
Temp/T033Validation/Builds/T067WindowsFinal/PlayerWithTests.

## Limites

Não houve build/aparelho Android, controle físico, playtest humano, revisão de
Safe Area ou medição de desempenho. A integração móvel nas cenas está disponível,
mas layout responsivo/Safe Area e aceite físico permanecem nas tarefas correspondentes.
Nenhuma tarefa posterior foi implementada. A camada Platform não conhece RunPhase
ou regras de gameplay; recebe flags de contexto e emite PlayerCommand comum.
