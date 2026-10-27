# Validação de T051

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementadas EncounterDefinition e EncounterSchedule. Os 12 testes da T046 foram
migrados de reflexão para chamadas tipadas e aprovados. Adicionados 20 casos de
definição, continuidade da timeline e validação do cronograma.

EncounterDefinition contém janelas, composição por IDs/quantidades, intervalo e
limite de spawn, arena lake, multiplicadores e condição de conclusão. Os marcos
validam Libélula Alfa aos 300 segundos, Sapo-Cururu aos 600 e Sapo-Rei aos 900,
cada um com seu ID de alvo obrigatório e quantidade um.

IsValidTimeline exige os três marcos únicos e cobertura por ondas de zero até
900 segundos sem lacunas; sobreposições são permitidas. IDs de encontro são
únicos e referências de inimigos devem constar da coleção conhecida recebida.
CreateSchedule valida e ordena a timeline sem alterar assets; em tempos iguais,
onda precede marco. A composição e as quantidades têm snapshots somente de leitura.

O Core copia IDs/tempos, valida entradas e emite cada encontro vencido uma vez.
Avanços grandes preservam todos os pedidos em ordem. Preparação, pausa, escolha,
Boss e estados terminais não emitem; pausa/escolha preservam pedidos pendentes
para retomada. Nova sessão possui cursor independente. Consultas vazias não
alocam lista; pedidos retornam snapshot somente de leitura.

Testes adicionais cobrem lacunas iniciais/internas/finais, ondas sobrepostas,
marcos ausentes/duplicados/deslocados/com alvo errado, dados não finitos, composição
inválida, referências desconhecidas, imutabilidade e construção inválida.

| Suíte | Aprovados | Falhas esperadas anteriores | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode | 272 | 40 | 0 | 2 |

Os 240 casos anteriormente aprovados, os 12 da T046 e os 20 novos passaram.
Falhas restantes exclusivamente: 18 RunResultTests (T047), 11 RelicLoadoutTests
e 11 SaveServiceTests (T048), por tipos ainda ausentes. A saída 2 reflete essas
fases vermelhas. A execução inicial passou com 19 novos casos; após acrescentar
validação de alvo obrigatório, a execução final aprovou os 20.

Execução batchmode/nographics em Temp/T033Validation. Unity gerou .meta da pasta,
definição, cronograma e novos testes, copiados preservando GUIDs. Evidências
regeneráveis fora do Git: TestResults/t051-editmode.xml e Logs/t051-editmode.log.

Nenhum asset de encontro/catalogo ou cena foi criado/alterado. Catálogo autoral
permanece na T056; spawn e integração da partida nas T054/T060. Este incremento
valida dados e pedidos, sem comprovar encontros jogáveis ou pressão/balanceamento.
PlayMode não foi repetido por não haver consumidor em cena destes novos tipos;
último resultado T049: 77 aprovados e nove falhas esperadas pela ausência do chefe.
Nenhum aceite de gameplay, plataforma ou desempenho foi realizado. T052 em diante
permanecem pendentes.
