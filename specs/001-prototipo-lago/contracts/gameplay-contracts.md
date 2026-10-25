# Gameplay Contracts: Prototipo Jogavel do Lago

Estes contratos definem fronteiras internas observaveis. Implementacoes Unity podem mudar sem alterar
os consumidores enquanto os contratos permanecerem estaveis.

## PlayerCommand

Produzido por teclado, controle ou toque e consumido pelo controlador do jogador uma vez por frame de
simulacao.

| Campo | Tipo | Regra |
|---|---|---|
| `move` | vetor 2D normalizado | magnitude de 0 a 1 |
| `quackPressed` | booleano de borda | verdadeiro somente no inicio da ativacao |
| `pausePressed` | booleano de borda | alterna pausa uma vez |
| `source` | enum diagnostico | nao pode alterar regras |

Invariantes: dispositivos distintos com valores equivalentes produzem o mesmo resultado; entrada e
descartada quando a partida esta terminal; comandos de UI nao vazam para movimento.

## CombatEvent

Eventos do Core para Gameplay e Presentation: `DamageApplied`, `CriticalHit`, `EntityDefeated`,
`QuackActivated`, `ProjectileReflected`, `EffectApplied` e `CollisionTriggered`.

Campos comuns: tick, fonte, alvo, posicao, quantidade, tipo e identificador de correlacao.

Invariantes: um evento representa uma mudanca ja aceita; consumidores nao reaplicam regra; efeitos
visuais/sonoros podem ser descartados sem mudar o estado; identificador impede dupla recompensa.

## UpgradeOffer

| Campo | Regra |
|---|---|
| `choices` | de uma a tres opcoes elegiveis, sem duplicatas |
| `rerollsRemaining` | inicia em dois e nunca fica negativo |
| `banishRemaining` | inicia em um e nunca fica negativo |
| `canSkip` | verdadeiro conforme regra da partida |
| `reason` | nivel, bau ou evolucao |

Cada opcao declara categoria, efeito atual/proximo, sinergia, requisito de evolucao e motivo de
elegibilidade. Confirmar, rerrolar, banir e pular sao operacoes idempotentes por `offerId`.

## SpawnRequest

Produzido pelo `SpawnDirector` e consumido pelo `PoolService`.

Campos: encontro, definicao do inimigo, quantidade, regiao permitida, distancia minima do jogador,
prioridade e tick limite.

Invariantes: nunca surge dentro da area de exclusao visivel; respeita limite ativo; uma requisicao
expirada e descartada; falha de capacidade reduz densidade antes de criar fora do pool.

## PoolLifecycle

Inimigos, projeteis, pickups/recompensas e VFX implementam `OnAcquire`, `OnRelease` e `ResetState`.
Nenhuma dessas familias usa criacao/destruicao recorrente durante combate. O scheduler central recebe
somente instancias ativas e distribui atualizacoes de hordas sem `Update` individual por inimigo.

## TerrainAffinity

Recebe terreno atual, definicao do equipamento e modificadores ativos; retorna o valor efetivo sem
alterar o asset autoral. A transicao e idempotente e um equipamento sem afinidade preserva seu valor
base.

## RelicLoadout

Operacoes: `Unlock`, `Equip`, `Unequip` e `ApplyEffects`. Somente IDs conhecidos e desbloqueados podem
ser equipados; o limite e tres; repetir uma operacao nao duplica efeitos; falhas retornam motivo
testavel e nao alteram o loadout.

## RunLifecycle

Comandos: `StartRun`, `PauseRun`, `ResumeRun`, `RequestUpgrade`, `StartBoss`, `WinRun`, `LoseRun`,
`AbandonRun`, `ShowResults`.

Eventos: `RunStarted`, `RunPaused`, `UpgradeStarted`, `BossStarted`, `RunEnded`, `ResultsReady`.

Invariantes: transicoes seguem [data-model.md](../data-model.md); comandos invalidos nao alteram
estado; `RunEnded` e `ResultsReady` ocorrem no maximo uma vez.

## SaveService

Operacoes: `Load`, `Save`, `CreateDefault`, `Migrate` e `RestoreBackup`.

Resultados usam `Success`, `Recovered`, `Defaulted` ou `Failed` e incluem diagnostico sem dados
sensiveis. Uma falha de persistencia nao bloqueia uma partida; salvamento ocorre fora do loop critico;
substituicao do arquivo principal e atomica.

## PresentationEvent

Eventos minimos: terreno alterado, Folego pronto/consumido, perigo telegrafado, boss apresentado,
evolucao obtida, recompensa rara e resultado disponivel.

Cada evento declara prioridade e duracao maxima. Alertas de perigo e estado de Folego tem precedencia
sobre efeitos cosmeticos; Safe Area e proporcao nao podem ocultar informacao essencial.
