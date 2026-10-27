# T058 — Resultado e estatísticas da partida

Data: 2026-10-27. Unity 6000.4.3f1, revisão 39d1a88d4dd1.
Escopo: somente T058. T059 em diante permanecem pendentes.

## Implementação

RunResult é um resumo imutável em Core com seed, resultado, vitória, duração,
chefe alcançado/ID, derrotas totais e por inimigo, dano por arma, maior sequência,
build, sinergias, moedas ganhas/bônus/recebidas, itens, recordes e desafios.
RunBuildSnapshot copia níveis de armas/passivas, evoluções, histórico de escolhas,
recursos restantes e IDs de relíquias equipadas. Não retém assets ou estado mutável.

RunResultSession recebe fatos aceitos durante Playing/Boss. Dano acumulado deve
ser o efetivamente aplicado; BreakStreak é comandado pelo consumidor. O acumulador
não deduplica impactos/derrotas: essa responsabilidade fica nos produtores, que já
possuem contratos de correlação/lease. Synergies não aceita duplicatas; snapshots
de recordes/desafios repetidos não somam progresso novamente.

Ended congela o resultado automaticamente; FinalizeResult retorna null antes do
fim e a mesma instância depois, inclusive em Results. Fatos tardios são rejeitados,
e mudanças na build/loadout depois do encerramento não afetam o resumo.
Dispose remove assinaturas; depois de concluído, o resultado permanece consultável.

Recompensa é injetada: Won recebe moedas ganhas e bônus, Lost mantém as ganhas sem
bônus, Abandoned recebe floor(ganhas × fator), sem bônus. Fator válido está em [0,1].
Vitória satura a soma em int.MaxValue; entradas/somas inválidas nas estatísticas
são rejeitadas sem mutação. Não foi definido balanceamento de recompensa nesta task.
Itens e estatísticas aceitas continuam no resumo no abandono; não há crédito em
carteira, desbloqueio ou persistência por consultar o objeto.

## Validação observada

Execução em `Temp/T033Validation`, cópia isolada de Assets/Packages/ProjectSettings.
Unity gerou os três novos arquivos meta; os existentes foram preservados.
Não houve alteração em cenas, prefabs ou assets serializados.

| Etapa | Total | Aprovados | Falhas | Exit code |
|---|---:|---:|---:|---:|
| T047 antes da implementação | 18 | 0 | 18 esperadas | 2 |
| EditMode completo após T058 | 408 | 397 | 11 esperadas | 2 |
| PlayMode completo | 149 | 149 | 0 | 0 |

As 18 falhas iniciais eram exclusivamente pela ausência de RunResultSession.
RunResultTests foi migrado de reflexão para chamadas tipadas: seus 18 casos passaram.
Os 30 novos RunResultStatisticsTests passaram. As 11 falhas restantes pertencem
exatamente a SaveServiceTests, pela implementação de save ainda ausente (T059).

Cobertura nova: todos os campos, cópia de build evoluída/loadout, congelamento no
terminal antes da primeira consulta, coleções somente leitura, pausa/seleção,
sequência e quebra, chefe por fase sem inferência do relógio, valores padrão,
IDs/dano/progresso inválidos, overflow, progresso monotônico, sinergias únicas,
limites da política de abandono e descarte da sessão.

Comandos Unity: `-batchmode -runTests -testPlatform EditMode|PlayMode
-testResults <arquivo> -logFile <arquivo>`; EditMode com `-nographics`, PlayMode
com gráficos/áudio. Fase vermelha usou `-testFilter RunResultTests`.
Saídas ignoradas: `TestResults/t058-red-editmode.xml`, `t058-editmode.xml` e
`t058-playmode.xml`; logs em `Logs/t058-*.log`.

## Limites do aceite

Não houve playtest humano, builds ou medição de desempenho. A suíte PlayMode
verifica regressões das integrações existentes, não a coleta real do resultado:
esta task entrega modelo/acumulador testável sem conectar GameplayEventBus.

Na T060, fatos finais devem ser registrados antes de Won/Lost/Abandoned, incluindo
o golpe que encerra o chefe. O controlador da T057 publica parte dos eventos após
a transição: a integração deverá ajustar essa ordem/atribuição. Criar a sessão
antes de começar para capturar os encontros; criação tardia não reconstrói histórico.
Avaliação das regras de recordes/desafios e apresentação são composição posterior,
com os valores anterior/atual/meta fornecidos ao modelo. Save e tela final continuam
nas T059/T061; a arena permanece de treino.
