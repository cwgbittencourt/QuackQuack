# Validação de T029

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T029 concluída. TerrainZone representa volumes retangulares de água ou terra,
com dimensões e histerese em unidades locais, acompanhando posição, rotação e escala
do Transform. Para entrar no tipo do volume é necessário cruzar o limite interno;
para sair, o limite externo. A margem deve ser finita, não negativa e menor que a
metade da menor dimensão, mantendo uma área de entrada válida. Configure rejeita
geometria inválida antes de substituir os dados anteriores.

Contains ignora volumes desabilitados/inativos, geometria serializada inválida,
terreno autoral desconhecido, pontos não finitos e escala singular. Resolve retorna
um único terreno por ponto: maior prioridade vence; em empate entre tipos, terra
prevalece independentemente da ordem do array. Sem volume ativo aplicável, usa terra.
Isso permite uma ilha de terra dentro de um volume de água. As decisões de sobreposição
são explícitas e não dependem da ordem de callbacks de física.

PlayerController usa Resolve uma vez por passo e o mesmo resultado para PlayerState,
velocidade, recuperação de Fôlego e evento de troca. A velocidade é selecionada pelo
TerrainAffinity.Select a partir dos dados de terra/água. BreathSystem mantém a escolha
da taxa correspondente no Core, sem depender de componentes de cena.
O campo serializado water foi preservado por compatibilidade e aceita os dois tipos.

TerrainAffinity.Apply calcula o valor efetivo a partir de base e multiplicadores
autoriais sem alterar dados; ausência de afinidade usa multiplicadores um. Valores
negativos, não finitos, terreno desconhecido e resultado não finito são rejeitados.
Consumidores devem recomputar a partir da base autoral, nunca do resultado anterior.
A infraestrutura para estatísticas de equipamentos está validada; não foram criadas
armas ou passivas das tasks posteriores.

Sete casos PlayMode foram acrescentados aos testes existentes: volume desabilitado,
geometria inválida sem mutação, afinidade inválida, prioridade/empate e ordem dos volumes,
volume transformado, afinidade sem acúmulo e integração do controlador com velocidade,
Fôlego e feedback único na borda. O teste original continua verificando oscilação na
margem durante 100 amostras. A integração do controlador executa seus passos físicos
diretamente para comparar as taxas sem depender da quantidade de frames renderizados.

Etapa vermelha: 19 testes PlayMode, 16 aprovados e três falhas nas validações anteriores;
código de saída 2. Os quatro casos da nova resolução/integração foram adicionados depois.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 23 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em Temp/T022Validation,
reaproveitada das tasks anteriores, com batchmode, nographics, runTests e testPlatform
EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t029-red.xml, TestResults/t029-editmode.xml e TestResults/t029-playmode.xml.
Logs: Logs/t029-red.log, Logs/t029-editmode.log e Logs/t029-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena, asset serializado ou .meta foi alterado. A alteração preexistente dos
ícones em ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
Não houve aceite da arena jogável, builds ou desempenho. T030 em diante permanecem
pendentes; a montagem visual e configuração dos volumes no Lago pertencem à T030.
