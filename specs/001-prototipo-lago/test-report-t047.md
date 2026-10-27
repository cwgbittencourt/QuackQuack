# Validação de T047

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T047 concluída: 18 casos EditMode em RunResultTests. Nenhuma implementação
de resultado, carteira, save ou orquestração de partida foi adicionada. T058/T060
continuam pendentes.

Cobertura preparada:

- Sem resultado em Preparing, Playing, Paused, ChoosingUpgrade e Boss.
- Won, Lost e Abandoned geram uma única instância, preservada em consultas
  repetidas e após Results, com desfecho e duração estáveis.
- Primeira consulta feita já em Results preserva o desfecho original.
- Abandono em cada fase permitida recebe moedas parciais, sem bônus de vitória;
  tentativa tardia de vencer é rejeitada e não muda o resultado.
- Vitória recebe moedas ganhas mais bônus; abandono sem ganhos não gera bônus.
- Política de recompensa parcial injetada, com arredondamento para baixo.
- Consulta antes do fim não impede resultado posterior; sessões de mesma seed
  mantêm resultados independentes.

Fronteira proposta RunResultSession/RunResult documentada em
contracts/gameplay-contracts.md. Moedas, bônus e fatores dos testes são fixtures,
sem fixar balanceamento de produção. Política de recompensa por derrota, demais
estatísticas e persistência não são declaradas implementadas por esta tarefa.

Reflexão restrita aos testes permite compilar sem o tipo de produção. O SetUp
exige explicitamente a classe real no Core; não há implementação substituta ou
casos ignorados. Migrar para chamadas tipadas na implementação T058/T060.

| Suíte | Aprovados | Falhas T046 | Falhas T047 | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| EditMode | 209 | 12 | 18 | 0 | 2 |

Todos os 18 novos casos falham no SetUp com `T058/T060 pending: production
RunResultSession is missing (T047 red phase).` As 12 falhas anteriores continuam
pela ausência de EncounterSchedule. Nenhum teste anteriormente aprovado regrediu.
Esta execução confirma descoberta/fase vermelha; as asserções comportamentais
precisam ser exercitadas depois da implementação. Não representa aceite de US3.

Execução batchmode/nographics em Temp/T033Validation. O Unity gerou o .meta do
script, copiado com GUID preservado. Evidências regeneráveis fora do Git:
TestResults/t047-editmode.xml e Logs/t047-editmode.log.

PlayMode não foi repetido: somente testes EditMode e documentação mudaram, sem
alterar produção ou cena. Nenhuma build, avaliação de gameplay ou medição de
desempenho foi realizada. T048 em diante permanecem pendentes.
