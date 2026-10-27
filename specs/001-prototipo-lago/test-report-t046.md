# Validação de T046

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T046 concluída: 12 casos EditMode em EncounterScheduleTests. Nenhum
cronograma, catálogo de encontros, spawn ou orquestração de partida foi implementado.
Essas implementações continuam nas T051/T054/T056/T060.

Cobertura preparada:

- Libélula Alfa aos 300 segundos e Sapo-Cururu aos 600, antes do Sapo-Rei aos
  900; consultas imediatamente antes e exatamente nos limites.
- Avanço grande preserva todos os encontros cruzados em ordem cronológica.
- Pedidos únicos por sessão, inclusive após consultas repetidas e entrada em Boss.
- Preparing, pausa e seleção não emitem pedidos; tempo suspenso não avança e
  pedido vencido aguarda retomada.
- Morte, abandono, vitória e Results não emitem encontros pendentes.
- Nova partida independente; IDs/tempos injetados são copiados e respeitados.

Contrato proposto registrado em contracts/gameplay-contracts.md. Reflexão somente
nos testes permite compilar antes da implementação; o SetUp exige explicitamente
EncounterSchedule real no Core. Não existe implementação falsa ou teste ignorado.
Substituir a reflexão por chamadas tipadas ao implementar a fronteira.

| Suíte | Aprovados | Falhas esperadas | Ignorados | Código de saída Unity |
|---|---:|---:|---:|---:|
| EditMode | 209 | 12 | 0 | 2 |

As 12 falhas ocorrem no SetUp: `T051/T060 pending: production EncounterSchedule
is missing (T046 red phase).` Os testes existentes passaram. Esta execução confirma
descoberta e fase vermelha; as asserções comportamentais ainda precisam ser
exercitadas após implementar o cronograma. Não representa aceite de US3.

Execução batchmode/nographics em Temp/T033Validation. A primeira tentativa parou
na compilação porque a cópia temporária estava sem os pacotes do projeto; após
sincronizar Assets, Packages e ProjectSettings, compilação e execução concluíram.
O Unity gerou o .meta do novo script, copiado preservando seu GUID.

Evidências regeneráveis fora do Git: TestResults/t046-editmode.xml e
Logs/t046-editmode.log. PlayMode não foi repetido: alteração restrita a testes
EditMode e documentação, sem mudança de produção ou cena. Nenhum aceite de
gameplay, plataforma ou desempenho foi realizado. T047 em diante permanecem pendentes.
