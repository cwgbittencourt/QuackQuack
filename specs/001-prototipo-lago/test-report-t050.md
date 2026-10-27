# Validação de T050

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementada EnemyDefinition como ScriptableObject com dados editáveis e getters
sem setters públicos, mais 31 casos EditMode em EnemyDefinitionTests.

| Inimigo previsto | Papel e dados preparados |
|---|---|
| Formiga | Swarm, movimento e dano de contato |
| Aranha | WebCaster, alcance/intervalo, duração e multiplicador de lentidão |
| Vespa | Charger, velocidade/duração da investida e dano de ataque |
| Besouro | FrontalArmored, arco frontal e redução de dano |
| Caramujo Aquático | SlowingTrail, duração e multiplicador da trilha lenta |

Vida/velocidade positivas finitas, dano não negativo, resistências normalizadas,
recompensas não negativas e limites 0 < pool inicial <= máximo são validados.
Prefab exige IPoolLifecycle na raiz; papéis com ataques especiais exigem sinais
visual/sonoro e antecedência positiva dentro do intervalo de ataque. Lentidão,
investida e armadura validam seus próprios limites e números não finitos.

Família de pool é Enemy; PoolKey deriva do ID e TelemetryId usa o mesmo ID.
IsValidCatalog rejeita lista ausente/vazia, entradas nulas/inválidas e IDs
duplicados. Consultas não alteram os dados. O contrato está documentado em
contracts/gameplay-contracts.md.

Esta tarefa entrega definição/configuração. Não registra pools, não instancia
inimigos e não emite contadores de telemetria. Estado/recompensa única, spawn e
comportamentos continuam nas T053–T055; assets do elenco e catálogo fechado
continuam na T056; instrumentação de desempenho é da T072. Nenhuma cena mudou.

| Suíte | Aprovados | Falhas esperadas anteriores | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode | 240 | 52 | 0 | 2 |

Todos os 31 casos novos passaram. As falhas são exclusivamente as já conhecidas:
12 EncounterScheduleTests (T046), 18 RunResultTests (T047), 11 RelicLoadoutTests e
11 SaveServiceTests (T048), por tipos ainda ausentes. Não houve regressão dos 209
casos anteriormente aprovados. A saída 2 reflete essas fases vermelhas pendentes.

Execução batchmode/nographics em Temp/T033Validation. A primeira tentativa
encontrou a cópia temporária sem os pacotes do projeto e parou antes dos testes;
após sincronizar Assets, Packages e ProjectSettings, os 292 casos executaram.
Unity gerou os .meta da pasta, definição e testes, copiados preservando GUIDs.

Evidências regeneráveis fora do Git: TestResults/t050-editmode.xml e
Logs/t050-editmode.log. PlayMode não foi repetido: somente definição de conteúdo
sem consumidores em runtime e testes EditMode mudaram. Último PlayMode (T049):
77 aprovados e nove falhas esperadas pela ausência do chefe.

Não há aceite de comportamentos táticos, pooling em combate, sinais percebidos,
balanceamento, plataforma ou desempenho com base nestes testes. T051 em diante
permanecem pendentes.
