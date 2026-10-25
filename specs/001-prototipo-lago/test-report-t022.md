# Validação de T022

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T022 concluída. PlayerState preserva vida entre zero e o máximo, rejeita dano
durante a janela de invulnerabilidade e aceita novo dano no instante final da janela.
Dano letal limita a vida a zero; jogadores mortos rejeitam novos danos. Terreno inicia
em terra e aceita somente Land ou Water. A resolução de volumes e histerese permanece
na integração de terreno, fora desta task.

PlayerState e seu BreathSystem rejeitam valores não finitos para impedir corrupção
de vida e Fôlego. Custos inválidos não consomem reserva; definições inválidas falham na
construção. Dano inválido retorna false sem alterar vida ou prazo de invulnerabilidade.

Foram adicionados quatro testes em BreathStateTests para terreno exclusivo, dano letal,
preservação da janela após dano rejeitado e entradas numéricas inválidas. Os testes
existentes continuam verificando consumo, recuperação, ativação parcial e limites de Fôlego.

| Suíte | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 21 | 0 | 0 |
| PlayMode | 9 | 0 | 0 |

Como o projeto original estava aberto no Editor, as suítes foram executadas em uma
cópia temporária de Assets, Packages e ProjectSettings em Temp/T022Validation, usando
batchmode, nographics, runTests e testPlatform EditMode/PlayMode. O acesso à licença
exigiu execução fora do sandbox. XMLs estão em TestResults/t022-editmode.xml e
TestResults/t022-playmode.xml; logs em Logs/t022-validation.log e Logs/t022-playmode.log.
Esses resultados e a cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena, asset serializado ou arquivo .meta foi alterado. A alteração preexistente
em ProjectSettings/ProjectSettings.asset foi preservada. Não houve aceite de gameplay,
build de plataforma ou desempenho. T023 em diante permanecem pendentes.
