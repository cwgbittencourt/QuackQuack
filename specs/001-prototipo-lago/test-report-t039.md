# Validação de T039

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T039 concluída. BuildState implementa o estado C# por partida, sem
dependência de cena, ScriptableObjects ou apresentação. Mantém até seis armas e
seis passivas independentes, níveis, duas rerrolagens, um banimento, evoluções e
histórico ordenado das alterações de inventário aceitas.

IDs vazios/em branco, duplicatas e aquisição sem slot são rejeitados sem mutação.
Armas iniciam no nível um e melhoram até cinco; passivas usam o máximo autoral
fornecido pelo chamador. Melhorias não consomem novos slots. Recursos nunca ficam
negativos. Wrappers somente de leitura protegem inventários, evoluções e histórico.

Evolução exige base no nível cinco, passiva presente e resultado não possuído;
substitui a arma mesmo com os seis slots cheios e preserva passivas/outras armas.
A forma evoluída retém nível cinco e é final. O estado impede readquirir a base
já evoluída ou evoluir novamente o resultado. O histórico registra a substituição.
Rejeições não alteram inventários, recursos ou histórico. A validação de IDs do
catálogo e receitas permanece no chamador, conforme o contrato das T033/T038.

Os 23 testes T033 foram migrados de reflexão para chamadas tipadas, preservando
os cenários originais. Quatro casos adicionais verificam proteção das coleções,
histórico de alterações aceitas, forma evoluída final e níveis autorais de passivas
com os seis slots ocupados.

| Suíte | Aprovados | Falhas esperadas | Código de saída |
|---|---:|---:|---:|
| EditMode final | 157 | 20 da T034 | 2 |

Todos os 27 casos BuildState passaram; os 23 que estavam vermelhos desde T033
agora exercitam as asserções comportamentais. As únicas falhas finais são da
ausência de UpgradeSystem (T040). Não houve erro de compilação nem nova falha de
regressão. A fase vermelha anterior está registrada em T033 e nas suítes até T038.

Execução em Temp/T033Validation, batchmode/nographics/runTests, testPlatform
EditMode. Evidências regeneráveis fora do Git: TestResults/t039-editmode.xml e
Logs/t039-editmode.log. O Unity gerou os .meta de Core/Progression e BuildState;
GUIDs foram preservados ao copiar. Campos vazios do .meta da pasta preservam os
espaços gerados pelo Unity; scripts e documentação passam na verificação de whitespace.

PlayMode não foi repetido: nenhum controlador ou composição de cena foi alterado.
A última execução T035 permanece com 34 aprovados e seis falhas esperadas. Ofertas,
XP e composição dos efeitos de conteúdo continuam pendentes nas tarefas seguintes.
Não há aceite de US2 jogável, builds, plataformas ou desempenho. T040 em diante
continuam pendentes.
