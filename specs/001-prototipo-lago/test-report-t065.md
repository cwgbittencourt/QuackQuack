# T065 — Adaptador de controle

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T065. Próxima task: T066.

## Entrega

GamepadInputAdapter implementa IPlayerInput/IDisposable, sem regras de gameplay.
Três ações do Input System convertem stick esquerdo, botão Sul e Start em Move,
QuackPressed e PausePressed. Source é Gamepad; PlayerCommand limita a magnitude.
A deadzone vem do processamento padrão do stick no Input System, preservando a
intensidade analógica e sem duplicar números ou regras de velocidade no adaptador.

Bordas performed são acumuladas até Read e consumidas uma vez; manter pressionado
não repete, e soltura permite nova ativação. Um tap entre leituras não é perdido.
Remoção/desconexão/desativação/reset de gamepad limpa bordas pendentes, enquanto o
Input System reseta o valor dos controles. O adaptador aceita um gamepad adicionado
depois. A limpeza de bordas considera qualquer gamepad; não há arbitragem multiplayer.

Dispose é idempotente, desregistra callbacks globais e das ações e libera recursos.
Read depois de Dispose lança ObjectDisposedException. Não há MonoBehaviour com
Update adicional, persistência ou dependência de estado de gameplay neste adaptador.

InputParityTests passa a usar o tipo concreto para controle. A reflexão fica apenas
para o MobileInputAdapter pendente da T066. GamepadInputAdapterTests acrescenta nove
casos: três de deadzone/intensidade parcial/saturação, isolamento do teclado,
remoção/reconexão com bordas ainda não lidas, tap entre leituras, resets soft/hard e
Dispose idempotente com rejeição de novas leituras. As fixtures removem somente seus
dispositivos sintéticos e restauram configuração de foco/deadzone/dispositivos atuais.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução | Total | Aprovados | Falhas esperadas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| Paridade antes da implementação | 15 | 5 | 10 | 0 | 2 |
| Paridade + primeiros seis casos do adaptador | 21 | 16 | 5 | 0 | 2 |
| PlayMode completo, com os nove casos finais | 217 | 199 | 18 | 0 | 2 |

A fase vermelha confirmou cinco ausências de GamepadInputAdapter/T065 e cinco de
toque/T066. Após a implementação, os cinco casos de controle passaram: comandos,
comparação de checkpoints do PlayerController real com teclado e rejeição em
Won/Lost/Abandoned/Results. A suíte final também aprovou os nove novos casos.

As 18 falhas restantes são exclusivamente cinco de T066, nove de T068/T069 e quatro
de T070, já conhecidas. Todos os 185 casos antes aprovados na T064 continuaram passando.
Não houve regressão. As asserções de controle exercitam Input System sintético e
componentes reais com passos controlados; não simulam hardware físico nem um
playtest humano. Não foi alterada nenhuma regra de terreno, Fôlego, pausa ou Quack.

O Unity gerou os .meta dos dois scripts novos, copiados com seus GUIDs preservados.
Nenhuma cena, prefab, configuração serializada ou dependência foi alterada.
EditMode não foi repetido: Core e suas regras não mudaram; a última evidência
permanece T061, 456/456. O test player Windows 2/2 da T064 não foi reexecutado e
não é apresentado como evidência de plataforma desta implementação.

Comandos: -batchmode -runTests -testPlatform PlayMode -testResults <arquivo>
-logFile <arquivo>. Rodadas focadas usaram -nographics e filtro InputParityTests
ou InputParityTests;GamepadInputAdapterTests. A suíte completa manteve gráficos.
Evidências regeneráveis ignoradas: TestResults/t065-red.xml, t065-input.xml,
t065-playmode.xml e Logs/t065-*.log.

## Limites

O adaptador é injetável em PlayerController.Initialize e foi testado por esse
caminho; quem o cria é responsável pelo Dispose. A cena mantém teclado por padrão.
A seleção unificada e o mapa de ações pertencem à T067. Não houve aceite de controle
físico na cena publicada, Android, Safe Area, build, gameplay ou desempenho.
Toque e tarefas posteriores não foram implementados.
