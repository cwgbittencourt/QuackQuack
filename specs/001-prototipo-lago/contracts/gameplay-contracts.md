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

## BuildState (contrato de testes T033)

Estado C# por partida em `QuackQuack.Core.Progression.BuildState`, criado vazio por
construtor sem argumentos. `Weapons` e `Passives` expõem mapas somente de leitura
de ID para nível, com até seis entradas independentes por família. Aquisição começa
no nível um. `TryAddWeapon(id)` e `TryAddPassive(id)` retornam falso para IDs vazios,
em branco, duplicados ou sem slot, preservando o estado. `TryUpgradeWeapon(id)`
melhora uma arma existente até cinco, inclusive com os slots cheios.

`RerollsRemaining` começa em dois; `BanishRemaining`, em um. `TryUseReroll()` e
`TryUseBanish()` consomem somente o próprio recurso e retornam falso ao esgotar.
A idempotência por oferta pertence ao UpgradeSystem (T034/T040).

`TryEvolveWeapon(baseId, requiredPassiveId, resultId)` recebe uma combinação já
validada pelo catálogo: exige a arma base no nível cinco e a passiva indicada
presente. Substitui a base pelo resultado no mesmo slot, preserva as outras armas
e todas as passivas. Rejeita base ausente, passiva ausente, nível insuficiente,
resultado vazio/em branco ou já possuído, sem mutação parcial. Compatibilidade
autoral das combinações pertence a T038/T043, não é criada por estes testes.

As operações retornam booleano de aceitação. T033 usa um adaptador de reflexão
somente nos testes para manter a compilação enquanto a classe da T039 não existe.
A suíte deve falhar explicitamente nessa ausência; não há stub, Ignore ou aceite
de progressão. Na T039, preferir chamadas tipadas ao implementar este contrato.

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
