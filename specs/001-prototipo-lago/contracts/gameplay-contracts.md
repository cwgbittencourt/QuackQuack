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

## WeaponDefinition (T036)

ScriptableObject em Content/Weapons com getters sem setters públicos. Define modo
de alvo e exatamente cinco WeaponLevel (dano, intervalo entre ataques em segundos,
alcance, quantidade de projéteis e raio de área). GetLevel recebe nível de um a
cinco e retorna cópia por valor; nenhum array mutável é exposto. Dano, intervalo
e alcance são finitos e positivos; área e quantidade não são negativas e existe
pelo menos um projétil ou uma área positiva em cada nível. Dados iniciais de um
asset recém-criado exigem autoria antes de serem considerados válidos.

CompatiblePassiveId e EvolutionWeaponId são IDs estáveis opcionais, exigidos em
par; resultado igual à própria arma é inválido. IsValid valida a definição local.
IsValidCatalog recebe armas e o catálogo tipado de PassiveDefinition (T037),
valida suas definições, IDs únicos e a existência de cada referência de evolução.
Toda passiva referencia uma arma existente; vínculos de evolução arma/passiva
devem ser recíprocos. A combinação evolutiva única e exigência de nível cinco na
regra de evolução são de T038/T039; seis armas, seis passivas e três combinações autorais
ficam em T043. Este validador não declara esses próximos requisitos concluídos.

Afinidade habilitada fornece multiplicadores positivos de dano e intervalo por
terra/água. GetLevel(level, terrain) usa TerrainAffinity a partir do valor autoral,
sem acumular modificadores nem alterar o asset. Sem afinidade, retorna os valores
base. A validação rejeita NaN, infinitos e produtos que excedam float ou zerem
por underflow. Os comportamentos de combate dessas armas são de T042.

## PassiveDefinition (T037)

ScriptableObject em Content/Passives com atributo afetado, ModifierPerLevel,
ModifierLimit, MaxLevel e CompatibleWeaponId editáveis no Inspector e somente
getters públicos. Atributos iniciais: dano, velocidade de ataque/movimento,
capacidade/recuperação de Fôlego e raio de área. São identificadores de dados;
a composição dos efeitos no combate pertence às tarefas de integração.

Os modificadores são bônus percentuais normalizados: incremento e teto finitos
no intervalo (0, 1], incremento não superior ao teto e nível máximo positivo.
O limite é autoral por passiva; o valor um representa 100% de bônus. GetModifier
aceita zero (não adquirida) até MaxLevel e retorna min(nível * incremento, teto),
sem acumular chamadas ou modificar o asset. Índices inválidos geram exceção;
definições inválidas não podem produzir modificadores. O nível máximo não está
fixado em cinco para passivas: esse requisito da especificação se aplica às armas.

CompatibleWeaponId é obrigatório e resolvido em WeaponDefinition.IsValidCatalog.
Esse catálogo rejeita passivas/armas ausentes, IDs duplicados, referências que não
existem e vínculos contraditórios entre os dois lados. Uma arma sem evolução pode
ser referenciada por uma passiva; esta definição sozinha não cria uma evolução.
As seis passivas autorais e as três combinações ficam em T043/T038.

## EvolutionDefinition (T038)

ScriptableObject com referências tipadas BaseWeapon, RequiredPassive e ResultWeapon,
sem setters públicos. IsValid exige todas as definições válidas, resultado com ID
diferente da base e concordância dos IDs de compatibilidade e resultado declarados
na arma/passiva. RequiredWeaponLevel é fixo em cinco, sem configuração divergente.

CanEvolve(ownedWeaponId, weaponLevel, ownedPassiveIds) é consulta pura: só aceita
a arma base no nível exatamente cinco, com a passiva exigida presente por ID ordinal
e uma receita válida. Dados ausentes, nível fora da faixa ou outra arma/passiva
retornam false. Não substitui inventário nem emite eventos; aplicação pertence à
BuildState/UpgradeSystem nas T039/T040.

IsValidCatalog valida primeiro o catálogo tipado de armas/passivas e depois as
receitas, exigindo IDs de evolução e pares (arma base, passiva) únicos. Referências
devem apontar para os próprios assets registrados, não cópias com o mesmo ID.
Catálogos ausentes/vazios e definições nulas/inválidas são rejeitados. O validador
aceita subconjuntos não vazios para autoria/testes; o catálogo final com exatamente
três combinações e seu feedback são entregas de T043/T045.

## BuildState (T033/T039)

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

As operações retornam booleano de aceitação. Os testes da T033 agora usam chamadas
tipadas à implementação da T039. Inventários, Evolutions (base para resultado) e
History (BuildChange somente de leitura) usam wrappers que impedem mutação externa.
O histórico registra somente aquisições, melhorias e evoluções aceitas, na ordem.

A forma evoluída mantém o nível cinco no mesmo slot e é final: não recebe melhorias
nem nova evolução. A base já evoluída não pode ser adquirida novamente na partida.
TryUpgradePassive(id, maxLevel) recebe o máximo da definição validada pelo chamador,
sem fixar cinco para passivas, e funciona mesmo com slots cheios. O Core não resolve
assets ou catálogos; IDs conhecidos, limites autorais e receitas são responsabilidade
da integração com conteúdo. Cada nova BuildState possui recursos e histórico próprios.

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

### Fronteira de testes T034 (implementação na T040)

`QuackQuack.Systems.Upgrades.UpgradeSystem(int seed)` representa uma sessão por
partida. `GenerateOffer(string offerId, string[] eligibleIds)` recebe os IDs já
filtrados pelas regras de conteúdo/build e retorna um snapshot com `OfferId`,
`Choices` (`IReadOnlyList<string>`) e `IsClosed`. Remove IDs vazios/em branco e
duplicados, seleciona até três IDs e usa todos quando restam um ou dois. Nenhum
elegível retorna null e permite continuar a partida. O filtro de compatibilidade,
nível e capacidade de slots será integrado com as definições/BuildState na T040;
estes testes verificam a fronteira posterior a esse filtro e a exclusão de banidos.

`CurrentOffer`, `ConfirmedChoices` (histórico somente de leitura), `RerollsRemaining`
e `BanishRemaining` permitem observar o resultado sem componentes de cena.
Os contadores devem compartilhar o estado da partida quando integrados à BuildState,
sem consumir duas vezes o mesmo recurso. Mesmo seed e requisições produzem as
mesmas escolhas; o array recebido não é modificado.

`TryConfirm(offerId, choiceId)`, `TryReroll(offerId)`,
`TryBanish(offerId, choiceId)` e `TrySkip(offerId)` retornam true somente na primeira
aplicação aceita. Repetição, ID desconhecido/obsoleto, escolha fora da oferta ou
recurso esgotado retornam false e preservam o estado. Confirmar registra uma única
escolha e fecha a oferta; pular fecha sem escolher ou gastar recursos. Repetir
GenerateOffer com o mesmo ID não substitui escolhas nem reabre uma oferta encerrada.

Rerrolar consome uma das duas rerrolagens e publica uma nova oferta com ID diferente,
invalidando o anterior. Banir consome o único banimento, exclui permanentemente o ID
da sessão e publica nova oferta com ID diferente; não consome rerrolagem. Se banir
a última opção, a seleção encerra sem publicar oferta vazia aberta. Repetir uma
ação usando o ID anterior não afeta a substituta. Rerrolar pode repetir opções da
oferta anterior quando continuam elegíveis; não se exige mudança artificial.

T034 usa reflexão somente no adaptador dos testes, até existir o tipo real na T040.
As falhas por ausência são a fase vermelha; substituir por chamadas tipadas na T040.

### Integração de XP e seleção (testes T035)

Para os testes de integração, UpgradeSystem expõe
`InitializeProgression(RunState run, int[] cumulativeThresholds, string[] eligibleIds)`.
Os limiares são dados injetados, não constantes de balanceamento; nos testes,
10/20/30 XP totais permitem observar os primeiros níveis sem uma partida longa.
`Experience` informa XP total coletado e `Level` começa em um. Cada limiar cruzado
gera uma escolha pendente. A seleção usa o mesmo CurrentOffer e os comandos da T034.

`QuackQuack.Gameplay.Pickups.ExperiencePickup` é um Component com Collider2D trigger
e IPoolLifecycle. `Initialize(PoolService pools, UpgradeSystem upgrades, int amount)`
configura cada aquisição; sobreposição física com o PlayerController concede XP
uma vez e devolve a instância ao pool. ResetState limpa dados e trava de coleta
para permitir reutilização sem repetir a recompensa anterior. Nenhum teste cria
um pickup por coleta: a capacidade de um é pré-aquecida pelo PoolService.

Ao cruzar limiar, a sessão entra em ChoosingUpgrade. O controlador real, os alvos,
o relógio e a recuperação de Fôlego permanecem congelados; Quack é rejeitado.
Confirmação válida registra uma escolha e retorna a Playing uma vez; callbacks
repetidos/obsoletos não repetem histórico ou transição. Escolha inválida mantém
a seleção aberta; pulo fecha sem conceder escolha ou consumir recursos.

Se uma coleta cruzar vários limiares, cada escolha usa um offerId distinto e o
combate continua congelado entre elas. A última resolução retoma a partida uma vez.
Após abandono, callbacks de confirmação/pulo não concedem escolha nem retomam jogo.
Os testes contam as transições reais de RunState. Integração de cartas visuais e
feedback é das T044–T045; a fixture da T035 chama a mesma fronteira de comandos.

As classes reais de XP/progressão ficam nas T040–T041. T035 usa reflexão para
manter a compilação antes dessas classes, com falha explícita no SetUp enquanto
ausentes; as interações físicas e as asserções seguintes não passam nessa fase.
Na implementação, trocar o adaptador por chamadas tipadas e ligar a composição
normal da partida aos mesmos serviços exercitados na fixture.

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
