# Validação de T053

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementados EnemyState no Core e PooledEnemy como adaptador Unity, sem Update
individual. Não houve alteração de cena, catálogo autoral ou gameplay da arena.

Estado reutilizável por instância contém lease, ID de conteúdo, vida, posição,
alvo por ID, controle e recompensas injetadas. Ativação valida os dados e não
sobrescreve aquisição ativa. Dano exige lease atual vivo e valor finito positivo;
limita ao restante da vida e reserva uma única recompensa ao morrer. EnemyReward
captura XP, moedas, posição, ID e lease; a entrega é feita por consumidor injetado.

O adaptador registra o prefab em PoolService e pré-aquece a capacidade máxima da
definição. Spawn adquire somente do pool; saturação retorna null e configuração
inválida devolve a instância. A derrota captura seus dados, retorna ao pool e
depois notifica dano/derrota e recompensa. Reentrada pode readquirir o mesmo objeto,
mas callbacks com lease antigo não afetam a nova aquisição. Acertos têm correlações
próprias; recompensas usam a identidade da aquisição.

Reset limpa definição, alvo, serviços, delegate, estado Core, efeitos de controle,
posição/rotação e velocidades do Rigidbody2D. Preserva somente o maior lease visto
no Core para rejeitar callbacks antigos. Despawn e Dispose não concedem recompensa.
Pausa, seleção e estados terminais bloqueiam dano e novos spawns. O coordenador
futuro deverá devolver as aquisições restantes ao encerrar encontros.

| Execução | Aprovados | Falhas esperadas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode completo | 306 | 29 | 0 | 2 |
| PlayMode completo, antes dos dois casos adicionais | 90 | 9 | 0 | 2 |
| PooledEnemyTests final, incluindo casos adicionais | 12 | 0 | 0 | 0 |

Dez novos EditMode cobrem ativação, dano, reserva única, dados inválidos, reset,
controle e leases antigos. Doze novos PlayMode cobrem morte/recompensa única,
limpeza antes da notificação, reentrada/readquisição, saturação/reutilização,
estados suspensos/terminais, pool incorreto, inicialização inválida, descarte do
pool, erro em observador e correlações distintas. A reentrada também comprova que
a aquisição seguinte usa seu novo consumidor de recompensa.

As falhas anteriores permanecem exclusivamente 18 RunResultTests e 11
SaveServiceTests no EditMode e nove FrogKingTelegraphTests no PlayMode, por tipos
ainda ausentes. Nenhum caso anteriormente aprovado regrediu. Os dois últimos
testes foram adicionados após a suíte completa e validados junto dos dez da
fixture, sem alteração adicional de produção.

Execuções batchmode em Temp/T033Validation; EditMode com nographics, PlayMode
com suporte gráfico. Unity gerou os .meta dos scripts e da pasta Gameplay/Enemies,
copiados com GUIDs preservados. Evidências regeneráveis fora do Git:
TestResults/t053-editmode.xml, TestResults/t053-playmode.xml,
TestResults/t053-enemy-pool-final.xml e logs correspondentes em Logs/.

Esta tarefa valida o ciclo isolado. SpawnDirector, scheduler/IA, catálogo e ligação
de armas/Quack aos inimigos reais e consumo das recompensas na partida permanecem
nas tarefas seguintes. Não há aceite de hordas, gameplay, plataforma ou desempenho
com base nestes testes. T054 em diante permanecem pendentes.
