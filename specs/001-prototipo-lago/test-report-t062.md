# T062 — Testes de paridade de entrada

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T062. Próxima task: T063. T065/T066 não foram implementadas.

## Entrega

InputParityTests contém 15 casos PlayMode. Cada uma das três fontes tem cinco
casos: sequência de comandos, checkpoints do controlador e rejeição de comandos
em Won, Lost e Abandoned (também depois da transição para Results).

Os cenários verificam repouso, direções, diagonal normalizada, metadado Source,
bordas de Quack/pausa consumidas uma vez, manutenção entre updates, soltura e nova
ativação. A integração compara cada checkpoint com o caminho real de teclado,
usando PlayerController, RunState, TerrainZone, CharacterDefinition, QuackDefinition
e GameplayEventBus reais: velocidade do Rigidbody2D, terreno, Fôlego, vida, fase,
relógio e quantidade de Quacks aceitos. Há asserções independentes para velocidades
autorais, consumo inicial, segundo Quack depois do cooldown, prioridade da pausa,
não repetição de pausa/Quack mantidos e movimento parado após retomar.

KeyboardState e GamepadState são enfileirados no Input System. O contrato de controle
usa leftStick, South e Start. O contrato móvel usa callbacks SetMove(Vector2),
SetQuack(bool) e SetPause(bool), com estado pressionado/solto. Os tipos de produção
são localizados por reflexão; tipo ausente falha explicitamente. Não há adaptador
falso, teste ignorado ou implementação de regra substituindo a produção. O shim
aceita construtor público sem argumentos ou componente Unity e deve dar lugar a
chamadas tipadas nas implementações T065/T066.

SetUp/TearDown restauram configuração de foco e os dispositivos atuais; removem
somente teclado/gamepad sintéticos próprios. Objetos/SOs são destruídos por fixture.
Não há reset global do Input System, leitura de save pessoal ou alteração de assets.
O .meta do novo teste foi gerado pelo Unity e copiado com seu GUID preservado.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução | Total | Aprovados | Falhas esperadas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| InputParityTests focado inicial | 15 | 5 | 10 | 0 | 2 |
| PlayMode completo final | 189 | 179 | 10 | 0 | 2 |

As dez falhas finais têm exclusivamente a causa de tipo de produção ausente:
cinco apontam T065/GamepadInputAdapter e cinco T066/MobileInputAdapter. As asserções
comportamentais de controle/toque ainda precisam ser exercitadas após essas entregas.
Todos os 174 casos PlayMode anteriores passaram, além dos cinco novos de teclado.

Após a rodada focada, a fixture foi isolada espacialmente em (10000,10000), longe
de entidades de cenas anteriores, e ganhou Quack/pausa simultâneos com borda nova.
A suíte completa acima executou essa versão final, confirmando os cinco casos de
teclado e as mesmas dez falhas esperadas. Não houve mudança de código de produção.
EditMode não foi repetido; a última evidência é T061, 456/456 aprovados.

Comandos Unity: -batchmode -runTests -testPlatform PlayMode -testResults <arquivo>
-logFile <arquivo>; a rodada focada acrescentou -nographics -testFilter
QuackQuack.Tests.PlayMode.InputParityTests. A completa manteve gráficos.
Evidências regeneráveis ignoradas: TestResults/t062-parity.xml,
TestResults/t062-playmode.xml, Logs/t062-parity.log e Logs/t062-playmode.log.

## Limites

O componente recebe ApplyCommand e um FixedUpdate/Advance controlados, sem esperar
frames reais. A posição é definida em checkpoints de terreno iguais; não se mede
deslocamento físico ou colisão. Toque não usa Touchscreen nem simula arraste,
hit testing ou múltiplos dedos. Não se declara paridade ponta a ponta, comportamento
de deadzone analógica, hardware real, build Windows/Android, Safe Area, gameplay ou
desempenho aprovados. Nenhuma dependência, cena ou asset de produção foi alterado.
