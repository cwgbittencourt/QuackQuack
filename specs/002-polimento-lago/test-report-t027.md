# T027 — Ledger, pools e preservação de pendências

Data: 05/11/2026. Base: `3518ab0`. Escopo: integrar obrigações no RunDirector e no pool de XP, sem criar a view de cura, atração ou liquidação final do chefe.

## Integração

RunDirector cria um ledger por partida, com identidade própria, mapa de origens derivado do catálogo e RNG dedicado de drops (seed da partida com salt independente de spawn/ofertas). Common/MiniBoss são resolvidos por IDs do catálogo; nomes de GameObject não participam. ProcessDefeat aceita a obrigação antes de qualquer tentativa de alugar view. Replay também não duplica moedas/estatísticas. A origem final do chefe e sua liquidação continuam em T030.

SuppressedPickups foi removido: pool cheio mantém ID, quantidade/fração e posição no ledger. Cada espécie de recompensa tem uma fila intrusiva pré-alocada; consultar/remover/reinserir pendência é O(1), sem varrer o histórico de derrotas. Falta de view não sorteia novamente, não reduz chance nem concede XP remotamente. Materialização retoma em Advance enquanto a partida simula. Pausa/seleção conservam as obrigações sem alugar views.

ExperiencePickup recebe somente a ligação necessária ao ledger nesta task: ID e consumidor de XP cacheado na inicialização. A coleta passa pelo UpgradeSystem real, preservando ofertas/subidas de nível; o ledger marca claim só após sucesso. Ao liberar uma view não coletada, o reset devolve sua obrigação à fila. Liberar uma view já coletada não a reemite. O caminho de treino/rare sem ledger continua usando o fluxo anterior. Lifecycle completo/atração e view de cura continuam T028/T029.

Cura de miniboss tem primeira tentativa sobre o adaptador HealingMaterializer; se não obtiver slot, cura comum não ocupa o pool compartilhado antes dela. XP usa sua própria fila/pool e não é bloqueado pela falta de view de cura. Enquanto T028 não instalar um materializador real, curas permanecem Due/WaitingForView. Não há view provisória concedendo cura ou substituindo seu pickup por XP.

Uma morte já emitida pelo PooledEnemy pode chegar ao sink depois de um callback abrir pausa/modal. Ela é registrada e aguarda retomada. RunResultSession recebeu RecordIssuedDefeat para preservar também moedas/estatísticas desse fato já aceito, sem autorizar novos ataques/coletas durante pausa. O método comum RecordDefeat preserva sua restrição anterior à simulação. Resultado já terminal continua imutável; teardown completo e integração terminal pertencem à T030.

## Dimensionamento da partida inteira

LakeRunSettings.CalculateRewardDefeatCapacity deriva o limite do catálogo efetivo, sem uma constante escondida de gameplay:

- Para cada onda: `(ceil((fim − início) / intervalo) + 1) × soma das quantidades`. O lote adicional torna o cálculo conservador nos extremos; o runtime não faz catch-up após atraso/pausa.
- Soma das seis ondas atuais: **3.259 derrotas** possíveis no limite conservador, supondo todo pedido materializado e toda criatura derrotada.
- Reserva adicional: `2 × QueueCapacity + 1 = 65`: cobre os pedidos pendentes de cada miniboss no momento de sua primeira entrega, mais o chefe. Após entrega, o runtime não reenfileira o respectivo miniboss; pedidos expirados não produzem atores.
- Capacidade calculada: **3.324 derrotas**, com armazenamento pré-alocado de até **6.648 registros** (duas recompensas por derrota). No cenário extremo com uma recompensa de XP do chefe e cura em todos os demais sorteios, são **6.647 obrigações**, sem nenhuma view/coleta.

O teste emite esse limite inteiro, mantém tudo pendente e verifica IDs/contagens/RNG. O trecho medido de emissão após aquecimento alocou **0 bytes** por GC.GetAllocatedBytesForCurrentThread no Editor. Listas/índices/records são alocados na inicialização; saturação visual não causa crescimento do ledger. A capacidade é recalculada a partir dos dados ao iniciar outra partida; composição fora do limite suportado falha antes do sorteio/mutação.

O pool físico de XP mantém os 256 itens autorados. Teste de integração esgota os 256, emite mais três, confere posição/ID/quantidade, libera uma view e confirma rematerialização do registro mais antigo no mesmo lugar. O cenário não substitui profiling CPU/GPU/memória nem aceita 60/30 FPS ou aparelhos adicionais.

## Testes

- EditMode: fila com remoção fora de ordem/reinserção, ausência de novo sorteio, consumidor real de XP com rejeição/reentrada, e capacidade autoral inteira com pior caso de cura/nenhuma view.
- PlayMode: saturação e retorno de slot, pausa/retomada, view real de XP coletando uma vez, replay sem duplicar estatísticas/moedas e preferência do miniboss em um PoolService real com um único slot de probe. A probe de teste não é uma view de cura de produção.
- Entrega atrasada em pausa: o ledger retém a obrigação, a retomada materializa, nenhum XP é concedido à distância e o resultado preserva moedas/derrota.
- Testes anteriores de Quack/projétil matando inimigo pooled e devolução dos atores continuam na suíte RunDirectorFlowTests.

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`, alvo Win64:

| Rodada | Resultado |
|---|---|
| Primeira EditMode | 680/680 aprovados, zero ignorados; marcador T027_REWARD_CAPACITY_OK |
| Primeira integração focada | 16/17 aprovados; falha exclusiva da fixture ao usar SendMessage numa view já inativa |
| PlayMode após corrigir a fixture | 466/466 aprovados, zero ignorados |
| EditMode final com entrega em pausa | 680/680 aprovados, zero falhas/ignorados, código 0 |
| PlayMode final com entrega em pausa | 467/467 aprovados, zero falhas/ignorados, código 0 |

A fixture corrigida despacha o primeiro trigger pela view ativa e invoca diretamente o callback tardio da view já liberada: Unity não entrega SendMessage a GameObject inativo. Não foi necessário mascarar logs ou alterar a coleta de produção para satisfazer esse teste.

XMLs locais em `TestResults/Polimento002/T027`; logs em `Logs/Polimento002/T027`, ignorados pelo Git. Novo .meta de RewardCapacityTests gerado pelo Unity; demais metas preservados. Sem alteração de cenas/assets serializados.

## Limites

T028 cria/vincula a view de cura e completa o ciclo de pickups; T029 implementa atração. T030 finaliza chefe e teardown; T031/T032 completam integração e aceite jogável. Esta entrega demonstra capacidade lógica para o catálogo atual e preservação sob saturação do pool real de XP no Editor. Não declara cura visível pronta, teardown completo, desempenho em dispositivo ou aceite físico. T014 permanece pendente; T028 não foi iniciada.
