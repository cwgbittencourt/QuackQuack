# Validação de T027

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T027 concluída. KeyboardInputAdapter usa Input System para produzir
PlayerCommand com movimento, bordas de Quack/pausa e Source explicitamente Keyboard.
WASD e setas são aliases no mesmo composite 2DVector: combinações mistas formam
diagonal normalizada e direções opostas se cancelam. Espaço e Escape geram uma borda
por acionamento; Read consome as bordas pendentes. Dispose libera as InputActions.

A revisão confirmou que o adaptador não consulta estado da partida, Fôlego, terreno,
dano ou cooldown, nem aplica alterações de gameplay. O descarte de comandos em estados
terminais e o roteamento entre UI e gameplay ficam no consumidor; PlayerController
e cenas não foram alterados. O mapa de ações compartilhado permanece em T067.

KeyboardPlayerInputTests foi ampliado de dois para quatro casos com teclado sintético:
W+D, seta cima+direita, W+seta direita e seta cima+D. Cada caso verifica diagonal,
origem, equivalência ao comando canônico, soltura, novo acionamento, manutenção das
teclas sem repetição das bordas e cancelamento de direções opostas entre os aliases.
As configurações temporárias de foco e o dispositivo são restaurados ao terminar.

Etapa vermelha: 11 testes PlayMode, sete aprovados e quatro falhas, código de saída 2.
Os casos originais falharam no cancelamento de teclas opostas e os mistos na direção
diagonal, antes de unificar os composites.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 11 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em
Temp/T022Validation, reaproveitada das tasks anteriores, com batchmode, nographics,
runTests e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t027-red.xml, TestResults/t027-editmode.xml e TestResults/t027-playmode.xml.
Logs: Logs/t027-red.log, Logs/t027-editmode.log e Logs/t027-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena, asset serializado ou .meta foi alterado. A alteração preexistente dos
ícones em ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
Não houve aceite de gameplay, builds ou desempenho. T028 em diante permanecem pendentes.
