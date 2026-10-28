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

## LakeEncounterCatalog (T056)

Asset Content/Catalogs/LakeEncounterCatalog.asset com cinco inimigos comuns
canônicos, dois minibosses, seis ondas, dois encontros de miniboss e o encontro
final. CommonEnemies contém Formiga/Swarm, Aranha/WebCaster, Vespa/Charger,
Besouro/FrontalArmored e Caramujo Aquático/SlowingTrail. MiniBosses contém
Libélula Alfa/Charger e Sapo-Cururu/WebCaster, com parâmetros próprios e pools
de uma instância. São composições das estratégias existentes, sem fases extras.

IsValid exige IDs únicos, elenco e papéis fechados, PooledEnemy nos prefabs e
EnemyAreaEffect nos sinais especiais. Ondas usam somente espécies comuns,
particionam [0,900) sem lacunas/sobreposições, contêm as cinco espécies ao longo
da progressão e têm intervalo não crescente/limite ativo não decrescente.
MaxActive de cada onda não excede ActiveLimit global. Encontros de Libélula Alfa,
Sapo-Cururu e Sapo-Rei respeitam os marcos validados na T051.

GetAllEnemies retorna cópia das sete definições. GetAllEncounters inclui ondas,
minibosses e pedido final; getters de coleções expõem snapshots protegidos.
CreateSchedule usa os IDs de inimigo do catálogo mais sapo-rei como alvo externo
reservado para o controlador T057/T060. Não existe definição/prefab fictício do
chefe no elenco comum. RegisterPools pré-aquece as sete famílias específicas;
CreateDirector compõe SpawnDirector com os orçamentos editáveis e pools registrados.

Dados iniciais: limite global 200, fila 32, orçamento 8 por tick, 16 tentativas
por posição e margem de exclusão 1,5. Pools máximos 128/64/48/32/32 para comuns,
um por miniboss; total 306 instâncias pré-aquecidas. Settings táticos usam steering
64 e oito efeitos por tipo. Estes números são autoria inicial, sem perfil aceito.

LakeEncounterCatalogSetup.Build gera assets, prefabs, material e tons temporários
pelo Editor, preservando GUIDs existentes. Preview rende galeria sem salvar cena.
O catálogo está pronto para composição; produção periódica de lotes, limites por
onda, câmera, armas/Quack, recompensas e ciclo final serão ligados pela orquestração
da partida. A T056 não inicia hordas automaticamente na arena de treino.

## EnemyDefinition (T050)

ScriptableObject em Content/Enemies, derivado de ContentDefinition, com dados
serializados editáveis e getters sem setters públicos. EnemyRole declara Swarm
(Formiga), WebCaster (Aranha), Charger (Vespa), FrontalArmored (Besouro) e
SlowingTrail (Caramujo Aquático). A seleção do comportamento pelo scheduler e os
assets autorais do elenco ficam nas T055/T056.

Health/MoveSpeed são positivos finitos; ContactDamage não negativo. Resistências
a empurrão/interrupção usam [0,1]; recompensas de XP/moedas são inteiros não negativos.
Dados específicos: intervalo/alcance/dano de ataque, duração e multiplicador de
lentidão, velocidade/duração de investida e arco/multiplicador de armadura frontal.
Aranha/Caramujo exigem lentidão em (0,1) e duração positiva; Vespa exige investida
mais rápida que seu movimento; Besouro exige redução em [0,1) e arco em (0,180],
preservando flanqueamento. Todos os campos usados por cada papel são validados.

Prefab deve ter componente IPoolLifecycle na raiz. PoolInitialSize/PoolMaxSize
exigem 0 < inicial <= máximo. PoolFamily é Enemy, PoolKey deriva de `enemy/` + Id,
e TelemetryId reutiliza Id estável, evitando identidades divergentes. Isso fornece
configuração para pool/telemetria, sem registrar pools, emitir contadores ou
instanciar inimigos. PoolService atual pré-aquece capacidade fixa; o consumidor
futuro deve respeitar o máximo sem criar fora do pool. A instrumentação é da T072.

WebCaster, Charger e SlowingTrail declaram HasTelegraphedAttack e exigem referência
visual, AudioClip não vazio e TelegraphSeconds positivo não maior que AttackInterval.
Swarm/FrontalArmored usam contato/defesa, sem exigir sinal de ataque especial.
Referências válidas não comprovam antecipação/legibilidade/audibilidade em jogo.

IsValidCatalog valida subconjuntos não vazios, elementos existentes/válidos e IDs
únicos, garantindo também unicidade de chaves de pool/telemetria. Exigir exatamente
cinco tipos comuns e montar prefabs/catalogar encontros pertence à T056. A definição
não altera assets em consultas nem implementa estado, recompensa única, spawn ou IA;
essas entregas continuam nas T053–T055.

## PrototypeBuildCatalog (T043)

Asset em Content/Catalogs/PrototypeBuildCatalog.asset com seis armas básicas,
seis passivas e exatamente três receitas. GetAllWeapons inclui também as três
formas resultantes, totalizando nove definições de arma para o runner e o sistema
de ofertas. Weapons contém somente as seis básicas; getters expõem coleções de
leitura e consultas de composição são feitas na inicialização.

IsValid exige contagens exatas, os seis comportamentos distintos, IDs únicos,
passivas ligadas às armas básicas, resultados distintos e receitas com referências
canônicas consistentes. CreateUpgradeSystem recebe seed/BuildState e compõe a
sessão usando o catálogo validado. O sistema exclui resultados de aquisições comuns.

Combinações autorais: Bumerangue + Pena aerodinâmica → Tornado de penas;
Ovos + Ninho reforçado → Incubadora de combate; Quack frontal + Pulmão poderoso →
Quack supersônico. Aura de energia, Foguetes e Bolhas têm Amuleto do lago, Óculos
de aviador e Sabão mágico como passivas compatíveis, sem receitas extras no protótipo.
Ovos recebem bônus de dano em terra e Bolhas na água. Cinco níveis de cada arma
e modificadores de cada passiva residem nos assets editáveis, sem mudar durante a
partida. Valores são iniciais e não representam balanceamento aceito por playtest.

## WeaponRunner (T042)

As seis armas da página 6 do documento funcional são Bumerangue, Ovos, Quack
frontal, Aura de energia, Foguetes e Bolhas. WeaponDefinition declara WeaponBehavior,
velocidade, duração, raio de colisão, velocidade angular, ângulo de cone/dispersão
e sprite opcional. Números de combate continuam nos cinco níveis e na afinidade
autoral; padrões com projéteis exigem quantidade positiva em cada nível.

WeaponRunner recebe RunState, BuildState, PoolService, GameplayEventBus, origem,
consulta de terreno e catálogo. Até seis slots são recompostos quando muda o
histórico da build. Padrões de ataque são estratégias compostas; o mesmo estado
pooled de projétil recebe movimento e dados por aquisição. Não há Update por projétil.

- Bumerangue: voo de ida e retorno à origem móvel, com um acerto por alvo/viagem.
- Ovos: armadilhas estacionárias que explodem em área ao detectar alvo.
- Quack frontal: cone automático, independente do Quack manual/Fôlego.
- Aura de energia: pulso radial periódico ao redor da origem.
- Foguetes: movimento guiado com explosão no contato com alvo.
- Bolhas: movimento reto com perfuração e um acerto por alvo/bolha.

RegisterProjectilePool cria instâncias somente na fábrica de pré-aquecimento;
ataques usam Acquire/Release e descartam disparos que excedam a capacidade.
Raios/cones e projéteis respeitam obstáculos da camada 8 e atingem QuackTarget
vivo não marcado como projétil na camada 9. Consultas de física usam buffers de
128 contatos; a memória de acertos por projétil tem limite de 512 alvos. Esses
orçamentos limitam processamento, sem afirmar aceite de hordas/performance.

Pausa e ChoosingUpgrade congelam movimento, duração e recargas. Estados terminais,
desativação e destruição do runner devolvem os projéteis. ResetState limpa dados,
alvo, sprite, idade e memória de acertos. Dano e derrota emitem CombatEvent com
fonte igual ao ID de instância da definição da arma e correlação por acerto.
Consumidores não reaplicam dano; os alvos são devolvidos pelo seu próprio ciclo
de vida, não pelo runner. Afinidade é capturada por disparo sem alterar o asset.
O catálogo visual e a composição na arena permanecem na T043.

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

### Fronteira de ofertas T034/T040

`QuackQuack.Systems.Upgrades.UpgradeSystem(int seed)` representa uma sessão por
partida. `GenerateOffer(string offerId, string[] eligibleIds)` recebe os IDs já
filtrados pelas regras de conteúdo/build e retorna um snapshot com `OfferId`,
`Choices` (`IReadOnlyList<string>`) e `IsClosed`. Remove IDs vazios/em branco e
duplicados, seleciona até três IDs e usa todos quando restam um ou dois. Nenhum
elegível retorna null e permite continuar a partida. Esse construtor é a fronteira
de IDs pré-filtrados: resolve ofertas e histórico sem aplicar conteúdo à build.
Para aplicar aquisições, melhorias e evoluções, usar o construtor com seed,
BuildState e catálogos tipados de armas/passivas/evoluções. Ele valida os catálogos
e IDs únicos entre categorias; GenerateOffer(offerId) considera o catálogo inteiro,
e a sobrecarga com IDs permite restringir os candidatos de uma recompensa.

Com catálogo, a sessão filtra slots cheios, níveis máximos, bases já evoluídas,
formas evoluídas como aquisições diretas e receitas sem seus requisitos. Confirmar
revalida a elegibilidade e aplica a operação à BuildState antes de fechar a oferta.

`CurrentOffer`, `ConfirmedChoices` (histórico somente de leitura), `RerollsRemaining`
e `BanishRemaining` permitem observar o resultado sem componentes de cena.
Os contadores compartilham a BuildState da sessão, sem consumir duas vezes o mesmo
recurso. Mesmo seed e requisições produzem as
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

T034 agora usa chamadas tipadas. OfferChanged publica novos snapshots somente de
leitura; OfferResolved sinaliza o encerramento aceito uma vez. BindRun vincula a
sessão à RunState antes da primeira oferta: entra em ChoosingUpgrade e retoma
Playing na resolução, se não houver outra oferta pendente. Partida terminal ou
pausada rejeita ações; Dispose bloqueia novos comandos e limpa assinaturas.
O encadeamento de ofertas por XP é implementado na T041; a composição normal com catálogo e
cartas permanece nas tarefas de conteúdo/UI.

### Integração de XP e seleção (T035/T041)

UpgradeSystem expõe
`InitializeProgression(RunState run, int[] cumulativeThresholds, string[] eligibleIds)`.
Os limiares são dados injetados, não constantes de balanceamento; nos testes,
10/20/30 XP totais permitem observar os primeiros níveis sem uma partida longa.
`Experience` informa XP total coletado e `Level` começa em um. Cada limiar cruzado
gera uma escolha pendente. A seleção usa o mesmo CurrentOffer e os comandos da T034.
ExperienceState, no Core, copia os limiares e exige valores positivos estritamente
crescentes; XP não positivo ou que exceda int.MaxValue é rejeitado sem mutação.
Não há constantes de curva de XP no controlador. TryCollectExperience recebe
somente recompensas durante Playing e preserva o estado na pausa/seleção/fim.
PendingLevels informa quantas escolhas ainda aguardam abertura.

`QuackQuack.Gameplay.Pickups.ExperiencePickup` é um Component com Collider2D trigger
e IPoolLifecycle. `Initialize(PoolService pools, UpgradeSystem upgrades, int amount)`
configura cada aquisição; sobreposição física com o PlayerController concede XP
uma vez e devolve a instância ao pool. ResetState limpa dados e trava de coleta
para permitir reutilização sem repetir a recompensa anterior. Nenhum teste cria
um pickup por coleta: a capacidade de um é pré-aquecida pelo PoolService.
Initialize exige que o objeto pertença ao pool ativo da família Pickup, verificado
por PoolService.IsActive. RegisterPool cria objetos somente na fábrica de aquecimento;
Spawn adquire/configura/posiciona uma instância e retorna null ao saturar, sem criar
objetos adicionais. Falha de configuração devolve a instância ao pool. Pausa mantém
o pickup disponível; OnTriggerStay permite coletá-lo ao retomar ainda sobreposto.

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

Os testes T035 agora usam chamadas tipadas e exercitam a interação física com o
pickup real, o pool e a arena. Limiares sem opções elegíveis são resolvidos sem
bloquear o combate. A composição normal da partida com catálogo e cartas ainda
será ligada aos mesmos serviços nas tarefas de conteúdo/UI.

## UpgradeSelectionView (T044)

Core.Shared fornece UpgradeSelectionSnapshot e UpgradeCardSnapshot imutáveis,
com tipo, nome, efeito do nível atual/próximo, sinergia, requisito, offerId e
recursos. UpgradeSelectionBridge traduz catálogo/build/oferta em apresentação
e reutiliza o snapshot enquanto não muda o estado. Números vêm das definições
autorais; a view não calcula elegibilidade nem reaplica efeitos de gameplay.

UpgradeSelectionView recebe fonte de snapshot e comandos de confirmar, banir,
rerrolar e pular. Encaminha o ID da oferta exibida; callbacks obsoletos são
rejeitados pelo sistema. Protege contra dois comandos no mesmo frame, oculta
slots sem carta, desabilita recursos esgotados e remove o modal fora da seleção.
Reativar o bridge não multiplica listeners. O modal captura raycasts e recebe
foco inicial no primeiro botão; entrada de UI usa InputSystemUIInputModule.

LakeBuildProgression compõe catálogo, build inicial com Bumerangue, armas, XP
e view em Lake. A cena serializa limiares de demonstração 10/20/30 e três pickups
de dez XP; criação/aquisição usa os pools compartilhados. PlayerController
descarta bordas de entrada ao sair da seleção. WeaponRunner ignora pools já
descartados, permitindo destruir Bootstrap antes da cena sem novas aquisições.
Safe Area e a matriz de proporções continuam nas tarefas de responsividade.

## UpgradeFeedback (T045)

UpgradeSystem.ChoiceConfirmed publica UpgradeConfirmation somente após aplicar
e fechar uma confirmação aceita, com IDs da oferta/escolha e IsEvolution obtido
do catálogo validado. Reentrada, confirmação repetida, pulo e rejeições não
repetem a notificação. O bridge traduz evolução em PresentationEvent.EvolutionObtained.

ExperiencePickup.InitializeRare/SpawnRare configura uma recompensa pooled com
correlação própria por aquisição. XP é concedido pelas regras existentes, o objeto
retorna ao pool e somente depois é emitido RareReward com a posição aceita.
ResetState limpa a classificação rara; reutilização comum não herda o evento.
Notificações de apresentação nunca concedem XP ou alteram a build.

UpgradeFeedbackController compõe anel, estrela/losango, legenda e áudio temporário.
PooledUpgradeFeedback liga seu término ao PoolService. UpgradeFeedbackBridge
pré-aquece quatro efeitos, deduplica correlações por tipo e limita a memória a
1024 notificações na sessão. Saturação descarta o cosmético sem afetar regras.
Notificações recebidas durante ChoosingUpgrade ficam numa fila limitada a quatro;
ao retomar, evolução tem prioridade e a fila toca um efeito por vez. Pausa congela
tempo/áudio; estados terminais e desativação devolvem efeitos e limpam a fila.

A demonstração de Lake tem um pickup raro roxo de 40 XP em (-9, 2), além dos três
comuns. Limiares cumulativos 10–70 permitem até sete escolhas. O efeito dourado
EVOLUÇÃO! é distinto do losango roxo RECOMPENSA RARA e dos sinais do Quack manual.
Clipes, perfis e prefabs são assets gerados pelo Unity; não há criação recorrente
de objetos ou AudioClip durante as ativações.

## Cronograma de encontros (T046/T051)

Fronteira implementada no Core: `QuackQuack.Core.Run.EncounterSchedule`, com construtor
`EncounterSchedule(RunState run, string[] encounterIds, double[] scheduledSeconds)`.
Recebe e copia IDs e tempos autorais correspondentes, em ordem cronológica. O
catálogo do Lago deverá fornecer Libélula Alfa aos 300 segundos, Sapo-Cururu aos
600 e Sapo-Rei aos 900; esses valores nos testes são fixtures, não balanceamento
embutido em um controlador. Validação da timeline pertence à T051; assets do catálogo à T056.

`CollectDue()` retorna `IReadOnlyList<string>` com os pedidos ainda não emitidos
cujo tempo seja menor ou igual a RunState.ElapsedSeconds, na ordem do cronograma.
Uma consulta que cruza vários marcos retorna todos, sem perder encontros; cada ID
é solicitado uma vez por sessão. Consultas repetidas e a entrada em Boss não
repetem o Sapo-Rei. Nova partida possui seu próprio cursor.

Preparing, Paused, ChoosingUpgrade e estados terminais não emitem pedidos.
Pausa/seleção preservam pedidos vencidos até retomar Playing; tempo suspenso não
conta. O cronograma não cria inimigos, não transiciona fases nem concede vitória:
essas responsabilidades ficam nos consumidores das T054/T060. Os testes T046
agora usam chamadas tipadas. O construtor copia IDs/tempos e rejeita dados nulos,
vazios, tamanhos divergentes, IDs vazios/duplicados e tempos não finitos, negativos
ou fora de ordem. Tempos iguais são permitidos, preservando a ordem recebida.
Consultas sem pedidos retornam coleção vazia compartilhada; pedidos retornam
snapshot somente de leitura e não podem ser reemitidos pela mutação do consumidor.

## EncounterDefinition (T051)

ScriptableObject com EncounterKind Wave/AlphaDragonfly/CururuToad/FrogKing,
StartSeconds/EndSeconds, composição por IDs/quantidades, SpawnInterval/MaxActive,
ArenaId e multiplicadores positivos de vida/dano. Getters de composição retornam
cópias somente de leitura; nenhuma consulta altera assets. A arena aceita é lake.
Waves têm janela [início,fim) positiva entre zero e 900 e conclusão por tempo;
marcos têm instante único e conclusão por derrota do alvo. Libélula Alfa exige
300 segundos e libelula-alfa; Sapo-Cururu, 600 e sapo-cururu; Sapo-Rei, 900 e
sapo-rei. Cada marco pede um único alvo com quantidade um.

IsValid rejeita números não finitos/fora da faixa, composição vazia, tamanhos
divergentes, IDs duplicados/vazios, quantidades não positivas ou lote maior que
MaxActive. IsValidTimeline recebe definições e IDs de inimigos conhecidos; exige
IDs de encontro únicos, referências conhecidas, os três marcos sem duplicação e
união das janelas de ondas cobrindo [0,900) sem lacunas. Sobreposição é permitida.
Os IDs conhecidos devem vir do catálogo validado na composição futura.

CreateSchedule valida a timeline e cria EncounterSchedule com cópias, ordenando
pedidos por início, Wave antes de marco no mesmo instante e ID ordinal para
desempate de ondas. O pedido inicia o encontro; não repete cada spawn da janela.
SpawnDirector/RunDirector deverão resolver os IDs e executar composição/conclusão
nas T054/T060. A T051 não cria assets do catálogo ou integra encontros na cena.

## SpawnRequest

Produzido pelo `SpawnDirector` e consumido pelo `PoolService`.

Campos: encontro, definicao do inimigo, quantidade, regiao permitida, distancia minima do jogador,
prioridade e tick limite.

Invariantes: nunca surge dentro da area de exclusao visivel; respeita limite ativo; uma requisicao
expirada e descartada; falha de capacidade reduz densidade antes de criar fora do pool.

### SpawnDirector (T054)

Core.Run.SpawnRequest é snapshot por valor com RequestId, EncounterId, EnemyId,
Quantity, Region (Rect de mundo), MinimumDistance, Priority e DeadlineTick.
Validação rejeita IDs vazios, quantidade/ID de pedido não positivos, regiões
degeneradas/não finitas, distância negativa/não finita, prioridade inválida e
prazo negativo. O consumidor de encontros futuro produz esses pedidos a partir
dos dados autorais; o diretor os executa usando PoolService.

Systems.Spawning.SpawnDirector recebe RunState, pools registrados, barramento,
alvo, definições, limite ativo global, capacidade da fila, orçamento por tick,
tentativas por posição, margem de exclusão, seed e consumidor de recompensa.
Valida definições e registro tipado dos pools na composição. A via interna
SpawnPrepared evita repetir validação de ScriptableObjects no loop de spawn;
as APIs públicas do PooledEnemy continuam validando entrada externa.

TryEnqueue exige RequestId estritamente maior que o último aceito, evitando
replay com memória limitada; fila cheia ou pedido inválido não consome o ID.
Process(tick, visibleArea) usa tick de simulação monotônico e retângulo atual
da câmera. Cada tick só é processado uma vez. Antes da prioridade, remove todos
os pedidos com tick > DeadlineTick; o prazo é inclusivo. Prioridade maior é
processada primeiro, com RequestId crescente como desempate.

Posições usam PRNG local por seed, dentro da região permitida, fora do retângulo
visível expandido pela margem (inclusive bordas), e respeitam a distância ao
alvo na hora da execução. Tentativas e trabalho por tick são limitados. Sem
espaço, pool disponível ou capacidade global, descarta o restante do lote,
contabilizando ReducedCount. Orçamento de tick esgotado preserva o restante até
próximo tick/prazo, sem acumular spawns expirados.

O limite global inclui todos os inimigos ativos na família Enemy, inclusive de
outros consumidores. As próprias aquisições são rastreadas por objeto e lease;
TryGetActive permite leitura agrupada sem criar snapshots. Process em estado
terminal limpa fila e devolve somente aquisições próprias ainda válidas. Pausa
e seleção preservam fila sem processar prazos. Dispose é idempotente, preserva
o pool compartilhado e não devolve leases externos que reutilizaram um objeto.
SpawnedCount/ExpiredCount/ReducedCount expõem resultados das políticas.

Não há Instantiate/Destroy no diretor; pré-aquecimento permanece na composição.
Arrays são alocados na inicialização e Process não cria coleções. Integração de
câmera real, produção de pedidos por cronograma, scheduler e catálogo/cena continuam
nas T055/T056/T060. Estes testes não aprovam desempenho ou exclusão visual em aparelho.

## EnemyState e PooledEnemy (T053)

Core.Combat.EnemyState é reutilizável por instância: TryActivate recebe leaseId
monotônico, ID da definição, vida, recompensas, posição e ID do alvo. Rejeita
ativação sobre sessão ativa, IDs vazios, lease antigo, números inválidos e recompensas
negativas. TryDamage exige lease atual e dano finito positivo, limita o dano à vida
restante e reserva EnemyReward uma única vez antes de qualquer observador externo.
O snapshot contém lease, ID de conteúdo, XP, moedas e posição aceitos.

TrySetControl/TrySetPosition exigem aquisição atual viva. Reset(leaseId) limpa vida,
referências por ID, alvo, posição, controle e trava da recompensa; preserva somente
o maior lease já visto para rejeitar callbacks antigos após reutilização. O Core
não conhece GameObjects, serviços, carteiras ou eventos de apresentação.

Gameplay.Enemies.PooledEnemy implementa IPoolLifecycle sem Update. RegisterPool
pré-aquece PoolMaxSize pelo PoolService a partir de prefab com PooledEnemy na raiz.
Spawn usa Acquire e retorna null na saturação ou fora de simulação; erro na
configuração devolve a instância. Initialize exige aquisição ativa na família Enemy
do dono informado, definição válida e serviços de partida/eventos/recompensa.

TakeDamage recebe o lease esperado e rejeita pausa/seleção/fim. Em derrota captura
dados/delegates, devolve a instância pelo pool e então publica DamageApplied e
EntityDefeated, chamando o consumidor de recompensa uma vez. Cada acerto tem
correlação de combate própria; cada aquisição tem lease próprio. Reentrada pode
readquirir o objeto sem permitir que o callback antigo danifique a nova aquisição.
O consumidor de recompensa é chamado em finally mesmo se observador de combate
lançar exceção; a notificação que falhou não é repetida automaticamente.

Despawn(leaseId) retorna sem recompensa. ResetState limpa referências da definição,
alvo, run, barramento/delegate, estado Core, transformação e velocidades físicas.
Dispose do pool executa a mesma limpeza sem conceder recompensa. O scheduler
futuro deve devolver aquisições ao encerrar/desativar encontros. Esta tarefa entrega
o ciclo isolado; spawn de hordas, IA, integração de dano dos alvos legados e
consumo da recompensa no gameplay continuam nas T054–T056/T060.

## Atualização tática centralizada (T055)

Gameplay.Enemies.EnemyUpdateScheduler tem um FixedUpdate para todas as aquisições
do SpawnDirector. Initialize recebe serviços, PlayerController, definições e
EnemyBehaviorSettings validados. Advance permite dirigir simulação manualmente;
AutomaticTick=false evita avanço duplicado em testes. Slots acompanham objeto e
lease; reutilização limpa o estado de decisão/ataque anterior. SteeringBudget
distribui decisões de direção em rodízio, enquanto movimento, prazos e contato
são processados para todas as aquisições ativas a cada avanço.

Swarm persegue; WebCaster mantém distância e deixa teia no ponto anunciado;
Charger trava alvo no aviso e investe naquela direção; FrontalArmored reduz dano
dentro do arco frontal, permitindo flanqueamento; SlowingTrail deixa área lenta
na posição anunciada. Aranha/Vespa/Caramujo preparam avisos por antecedência autoral,
com evento DangerTelegraphed e EnemyAreaEffect pooled com LineRenderer/AudioSource.
Sem capacidade para sinalizar, ataque é suprimido. Teia/trilha usam duração e
multiplicador autorais, aplicando a maior lentidão sem multiplicar sobreposições.

EnemyBehaviorSettings guarda orçamento de steering, capacidade de efeitos por
tipo, alcance de contato do jogador, raio de campo, distância preferida, margem
de colisão, decaimento do empurrão e cores. Intervalo de ataque de todos os papéis
deve ser positivo, inclusive para contato. Movimento varre CircleCollider2D contra
paredes na camada 8; buffer saturado bloqueia movimento. Contato varre o percurso,
inclui raio do inimigo e respeita obstáculos e invulnerabilidade do PlayerState.
Cada dano aceito tem correlação própria; vida zero solicita Lost.

ApplyHit recebe lease e origem do ataque para armadura frontal; ApplyControl
compõe resistências, interrompe preparação/investida e aplica empurrão com colisão.
Ambos exigem aquisição gerenciada atual e simulação ativa. São fronteiras para
conectar as armas/Quack na composição seguinte, sem alterar alvos legados aqui.
EnemySlowMultiplier é aplicado pelo PlayerController à velocidade base/relicada;
remoção/expiração dos campos restaura o valor um.

EnemyAreaEffect não tem Update: o scheduler avança idade, faz consultas espaciais
e devolve efeitos exclusivamente pelo pool. Avisos são cancelados com derrota,
lease antigo ou interrupção; campos já aceitos persistem pela duração autoral.
Pausa/seleção congelam tempos e áudio; fim/desativação limpam efeitos e lentidão.
O diretor permanece pertencendo ao chamador, que devolve suas aquisições na saída.
O catálogo e montagem de cena são da T056; não há aceite de hordas/performance aqui.

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

## Fronteiras de teste T048 (implementação T052/T059)

`Core.Progression.RelicLoadout(string[] knownIds, float[] damageBonuses)` recebe
cópias de dez IDs e bônus percentuais correspondentes de uma fixture. É uma
fronteira inicial para testar composição de dano; a autoria e demais tipos de
efeito são definidos na T052. Unlock, Equip e Unequip recebem ID e
retornam bool de aceitação; repetição retorna false, sem mutação. UnlockedIds e
EquippedIds expõem coleções somente de leitura. Equip exige conhecido/desbloqueado
e no máximo três. ApplyEffects(float baseDamage) recompõe o valor da base com a
soma dos bônus equipados, sem acumular chamadas. Remover equipamento retira somente
seu efeito e libera slot; remover todos restaura a base. Bônus de 10% são fixtures.

`Systems.Save.JsonSaveService(string path, string[] knownRelicIds)` usa caminho
injetado e backup `path + ".bak"`. CurrentSchemaVersion é propriedade estática;
Load() retorna Data, Status e Diagnostic. Data expõe SchemaVersion,
UnlockedRelicIds e EquippedRelicIds. JSON usa schemaVersion, unlockedRelicIds e
equippedRelicIds. Um save válido principal tem precedência; principal inválido
recupera backup válido (Recovered); ambos inválidos ou ausentes usam padrões
utilizáveis (Defaulted). Dados estruturais inválidos incluem JSON malformado,
null, objeto sem schema e schema negativo. Padrões têm versão atual e loadout vazio.

IDs desconhecidos e duplicados são filtrados com diagnóstico (Recovered);
equipamento é filtrado por desbloqueados/conhecidos, preservando os primeiros
três distintos na ordem salva. Coleções null tornam-se vazias por recuperação ou
padrões. Repetir Load não multiplica entradas. O JSON mínimo da fixture testa
somente os campos de relíquias; os demais campos do SaveData planejado ainda
exigem extensão versionada do schema inicial.
Escrita atômica e migração foram implementadas na T059. A composição com a partida
e o crédito de carteira permanecem nas tarefas de integração.

SaveServiceTests.cs contém fixtures separadas RelicLoadoutTests/SaveServiceTests.
As duas fixtures usam chamadas tipadas desde T052/T059, sem serviço substituto.
Arquivos são criados em diretório temporário exclusivo
com GUID, removido somente após validar seu caminho. Migrar para chamadas tipadas
nas respectivas implementações. Nenhum save pessoal é usado.

### Implementação de relíquias T052

RelicDefinition declara ID, nome, descrição, raridade, atributo, terreno, bônus e
condição/limiar de desbloqueio. Bônus do protótipo são positivos até 10%; os dez
assets iniciais usam 5%. PrototypeRelicCatalog exige exatamente dez IDs únicos e
dados válidos, criando loadouts independentes. O catálogo usa Botas do Lago como
referência do documento funcional e nove itens de autoria inicial para efeitos
suportados no protótipo; não declara as sugestões mais amplas do PDF implementadas.

RelicLoadout aceita também RelicModifier tipado por atributo/terreno. Copia os dados,
expõe coleções protegidas e Version somente para mudanças aceitas. GetBonus soma
até três equipadas aplicáveis; ApplyEffects recompõe da base, sem acumular consultas
e sem alterar assets. Os atributos são Damage, MovementSpeed, AttackSpeed, Range,
Area e BreathRecovery. Terreno pode ser Any, Land ou Water. Construtores rejeitam
quantidade diferente de dez, IDs inválidos/duplicados e bônus inválidos.

LakeRelics desbloqueia por tempo simulado ou XP da sessão, conecta o loadout ao
PlayerController e WeaponRunner e traduz comandos da RelicLoadoutView. Movimento
e recuperação usam terreno atual; dano/alcance/raio/cadência são compostos no
disparo. Ataques já disparados preservam seu snapshot. Não há gravação em assets.

RELÍQUIAS abre o painel durante Playing/Paused; pausar por abertura e retomar por
fechamento preserva a pausa anterior. Equipar/remover exige painel visível em Paused.
Reabrir painel aberto não perde propriedade da pausa. Seleção/fim oculta painel e
botão; callbacks rejeitados não alteram estado. A view renderiza dez opções,
condições, slots e comandos; rebind remove listeners antigos. Persistência dos
desbloqueios/loadout entre sessões permanece na T059.

## RunLifecycle

Comandos: `StartRun`, `PauseRun`, `ResumeRun`, `RequestUpgrade`, `StartBoss`, `WinRun`, `LoseRun`,
`AbandonRun`, `ShowResults`.

Eventos: `RunStarted`, `RunPaused`, `UpgradeStarted`, `BossStarted`, `RunEnded`, `ResultsReady`.

Invariantes: transicoes seguem [data-model.md](../data-model.md); comandos invalidos nao alteram
estado; `RunEnded` e `ResultsReady` ocorrem no maximo uma vez.

## Resultado único (T047; implementado na T058)

Fronteira no Core: `RunResultSession(RunState run, int earnedCoins,
int victoryBonus, double abandonmentFactor)`, em QuackQuack.Core.Run. Uma sessão
por partida recebe valores de recompensa já calculados/injetados; os números
101 moedas, bônus 50 e fatores 0,5/0,25 dos testes são fixtures, não balanceamento
aprovado. A sobrecarga com BuildState e RelicLoadout opcionais captura a build
no encerramento. A coleta dos fatos reais da partida pertence à T060.

`FinalizeResult()` retorna null enquanto não há Outcome terminal. RunResultSession
assina Ended para congelar um único RunResult assim que a partida encerra;
FinalizeResult retorna a mesma instância em toda chamada posterior,
inclusive em Results. Consulta antecipada não consome a possibilidade de fechar.
Outcome preserva Won/Lost/Abandoned; IsVictory somente é true para Won.
DurationSeconds captura o tempo da partida encerrada. Coins no abandono é
floor(earnedCoins * abandonmentFactor), sem bônus de vitória; Won recebe moedas
ganhas e bônus. Lost preserva as moedas ganhas, sem bônus; a soma na vitória
satura em int.MaxValue. O fator deve ser finito no intervalo [0,1], e moedas/bônus
iniciais devem ser não negativos. Entradas inválidas falham no construtor.

Abandono vindo de Playing, Paused, ChoosingUpgrade ou Boss jamais muda para
vitória por callback tardio. Sessões, mesmo com seed igual, não compartilham
resultado. A fronteira não grava saves ou credita carteira: entrega o resultado
estável para o consumidor; idempotência de persistência fica na integração futura.
RunResultTests usa chamadas tipadas desde a T058. A sessão implementa IDisposable:
descarte antes do fim cancela coleta/finalização; depois do fim preserva o resultado.
As assinaturas são removidas automaticamente quando o resultado é congelado.

### Estatísticas e snapshot (T058)

RunResult contém Seed, Outcome, IsVictory, DurationSeconds, BossReached/BossId,
EnemiesDefeated, DefeatsByEnemy, DamageByWeapon, LongestStreak, Build, Synergies,
EarnedCoins, VictoryBonus, Coins, Items, Records e Challenges. Todos os mapas,
listas e seus valores são imutáveis e independentes dos objetos de gameplay.
Mapas e sinergias possuem ordem ordinal determinística. Sem dados fornecidos,
as coleções são vazias e as estatísticas são zero.

RunBuildSnapshot copia armas/passivas com níveis, mapa de evoluções, histórico,
recursos restantes e IDs das relíquias equipadas. Uma mudança posterior na build
ou no loadout não altera o resultado, mesmo antes da primeira consulta da UI.

RecordDefeat incrementa contagem por ID e sequência; BreakStreak encerra a sequência
atual preservando o máximo. A composição determina quando interromper (por exemplo,
ao aceitar dano no Pato); não há uma janela temporal implícita neste modelo.
RecordWeaponDamage acumula dano efetivamente aplicado por ID de arma/origem.
RecordBossReached recebe o ID do encontro atingido; BossReached também é marcado
pela entrada em Boss. O relógio sozinho não deduz um chefe. Criar a sessão antes
do início para capturar o histórico; criação tardia não reconstrói encontros passados.

AddCoins e RecordItem somam recompensas já aceitas. IDs em branco, valores inválidos
e somas com overflow são rejeitados sem mutação. RecordSynergy não aceita duplicatas.
Os IDs são fornecidos pela composição com catálogo validado; Core não consulta assets.
Fatos de combate somente aceitam Playing/Boss; pausa, seleção, terminal e
descarte não aceitam dano/derrotas/moedas. Desde T060, itens e sinergias podem ser
capturados durante pausa/seleção para incluir a escolha/desbloqueio antes de um
abandono. Terminal e descarte continuam recusando toda mutação.
Os produtores devem deduplicar seus eventos:
o acumulador recebe cada fato aceito uma vez, sem armazenar um histórico de impactos.

SetRecord recebe valor anterior e valor desta partida; IsNewRecord compara ambos.
SetChallengeProgress recebe progresso anterior, atual e meta, expondo Gained e
IsCompleted. Progresso deve ser não negativo, monotônico e limitado pela meta;
baseline e meta não podem mudar na sessão. A avaliação de regras de recordes/desafios
e o crédito persistido permanecem nas tarefas de integração/UI, sem thresholds
duplicados neste modelo. Repetir o mesmo snapshot de progresso não soma de novo.

Todos os fatos finais devem chegar antes da transição terminal. A T060 atribui
o último dano/derrota/recompensa antes de Won/Lost; eventos publicados depois do
encerramento são rejeitados. RunDirector conecta o GameplayEventBus ao acumulador
com IDs de armas conhecidos e ajusta essa ordem no controlador do chefe.
O fator de abandono modifica somente Coins; itens e estatísticas aceitas permanecem
no resumo. O modelo não concede itens, moedas ou progresso persistido por consulta.

## SaveService

Operacoes: `Load`, `Save`, `CreateDefault`, `Migrate` e `RestoreBackup`.

Resultados usam `Success`, `Recovered`, `Defaulted` ou `Failed` e incluem diagnostico sem dados
sensiveis. Uma falha de persistencia nao bloqueia uma partida; salvamento ocorre fora do loop critico;
substituicao do arquivo principal e atomica.

### Persistência de relíquias (T059)

JsonSaveService recebe caminho de arquivo e exatamente dez IDs conhecidos únicos,
copiados na construção. Não escolhe um caminho pessoal nem faz I/O automaticamente
no ciclo de gameplay. SaveData é um snapshot somente leitura da versão e das listas
de IDs. O schema 1 desta tarefa contém schemaVersion, unlockedRelicIds e
equippedRelicIds. T061 amplia para schema 2 com progress. Configurações continuam
fora deste serviço. Save(loadout) preserva o perfil, sem creditar RunResult.

CreateDefault retorna versão atual, desbloqueios e equipamento vazios, sem I/O.
Migrate aceita schema 0 (mesmas listas; equipamento pode estar ausente), 1 e 2.
Versões 0/1 recebem perfil vazio; versão 2 preserva o perfil validado.
A versão 0 é a política de compatibilidade definida para esta implementação, não
evidência de um save anteriormente publicado. O método filtra desconhecidos,
duplicatas e equipamento bloqueado, preservando os primeiros três IDs distintos
na ordem salva. Versão negativa/futura é recusada. IDs não são extraídos de assets
no serviço: o proprietário fornece os IDs do catálogo validado.

Load lê principal antes do backup; ambos inválidos/ausentes retornam Defaulted.
Migração/normalização e backup retornam Recovered. Load é somente leitura, inclusive
na recuperação: não regrava principal ou backup. Objetos truncados, tipos inválidos,
schema duplicado e framing incompleto não viram progresso utilizável. Leitura usa
o leitor JSON da biblioteca .NET com verificação prévia de fechamento da estrutura;
gravação usa JsonUtility, sem dependência externa. Entradas têm limite de 1 MiB
e profundidade 32; arquivos grandes demais/ilegíveis não são sobrescritos por Save.

Save(SaveData) e Save(RelicLoadout) normalizam cópias, serializam UTF-8 para um
temporário exclusivo no mesmo diretório e chamam Flush(true). Primeiro save usa
File.Move; substituições usam File.Replace. Somente um principal anterior legível
e válido é rotacionado para .bak. Principal corrompido não substitui backup bom;
backup de versão futura é preservado. Principal de versão futura tem gravação
recusada, mesmo se Load conseguiu recuperar um backup antigo.

RestoreBackup valida e promove explicitamente o backup pelo mesmo caminho atômico,
preservando o próprio backup; também recusa substituir principal de versão futura.
Não há fallback que delete o principal antes de mover. Se o filesystem não suporta
a operação ou falha, retorna Failed com dados utilizáveis e diagnóstico sem paths,
conteúdo do arquivo ou detalhes de exceção. Cada operação só limpa seu temporário;
temporários de execução interrompida são ignorados, não promovidos automaticamente.

O serviço serializa operações de uma instância com lock; o chamador deve manter
um único proprietário por caminho. I/O é síncrono e deve ocorrer fora do combate.
Na partida T060, RunDirector define o caminho local, carrega desbloqueios/equipamento
antes de iniciar a simulação e salva em pontos de ciclo de vida fora do combate. Reaplicar
os IDs em RelicLoadout usa Unlock/Equip idempotentes, sem acumular bônus.

## Sinais do Sapo-Rei (T049; implementação T057)

Fronteira proposta para exercitar a composição real em PlayMode:
Gameplay.Bosses.FrogKingController é MonoBehaviour com AutomaticTick configurável,
Advance(float seconds) e TryBeginAttack(string attackId, Vector2 target).
Initialize(RunState, GameplayEventBus, LineRenderer, AudioSource, AudioClip,
float leadSeconds) recebe as dependências do sinal e antecedência autoral.
O teste usa 0,5 segundo e tom sintetizado como fixtures, sem definir balanceamento
ou áudio final. Com AutomaticTick=false, somente Advance dirige a simulação.

Os IDs de padrões jump, wave, tadpoles e destroy-safe-area exercitam o mesmo
caminho de preparação/execução usado pelo chefe. A entrada explícita permite
isolar cada padrão sem esperar o seletor automático de fases; elegibilidade e
composição das fases reais continuam na T057. Antes de executar, cada padrão
publica DangerTelegraphed com prioridade Danger, correlação positiva, posição
alvo e duração que cobre a antecedência. A marcação possui geometria não nula,
largura/opacidade positivas e renderer ativo; AudioSource reproduz o clip de
aviso sem mute e com volume positivo. Ambos precedem o commit do ataque.

AttackCommitted (Action<string, long>) informa padrão e a mesma correlação do
aviso uma única vez após a antecedência. Nenhum evento de combate é permitido
durante a janela de aviso. Pedido concorrente não substitui o ataque pendente
nem duplica aviso. Won/Lost/Abandoned ou desativação cancelam o pendente e param
renderer/áudio; reativar não executa ataque antigo.

FrogKingTelegraphTests cria componentes Unity reais em uma fixture isolada;
chamadas tipadas usam o controlador de produção desde a T057, sem implementação
substituta. Verificações de componente e
ordenação não aprovam legibilidade em tela, percepção sonora, dano real, fases,
pooling ou justiça de gameplay; esses aceites exigem integração e playtest.

## Controlador do Sapo-Rei (T057)

FrogKingSettings contém vida, limiares de fase, antecedência, recuperação, dano,
raios, velocidades, durações, quantidade de girinos e capacidade. O asset inicial
usa 1000 de vida; Summoning começa em 70% e Destruction em 35%. Os valores são
balanceamento provisório, editável no Inspector. A fase não regride.

InitializeEncounter recebe RunState, eventos, PoolService, PlayerController,
settings, LineRenderer, AudioSource e os FrogKingSafeArea da arena. Somente Boss
permite ataques/dano. O seletor alterna jump/wave; adiciona tadpoles na segunda
fase e destroy-safe-area na terceira. A entrada explícita também respeita essa
elegibilidade na composição completa. A sobrecarga de sinais da T049 continua
isolada, sem efeitos de gameplay ou seleção automática.

O salto fixa a posição no início do aviso, desloca o chefe para essa posição e
aplica dano de aterrissagem no raio configurado. A onda expande um anel, com
interseção varrida entre o raio anterior e o próximo. Girinos perseguem o Pato,
aplicam contato uma vez e retornam ao pool, ou expiram pela duração. As áreas
seguras protegem de ondas: a destruição fixa um refúgio no aviso, depois desativa
seu collider e visual. Não representa a Safe Area da interface ou troca de terreno.
Refúgios já destruídos não podem ser escolhidos novamente.

Ondas pertencem à família Projectile e girinos à Enemy, com pools separados
pré-aquecidos e um limite ativo compartilhado. Saturação suprime excedentes;
nenhuma instanciação ocorre no tick. FrogKingHazard não tem Update: o controlador
avança os efeitos em conjunto. O próprio chefe implementa IPoolLifecycle; reset
remove assinaturas, referências, ataques e efeitos. Reutilização com os mesmos
pools reaproveita a capacidade registrada. Advance não avança o relógio de RunState;
o proprietário da partida o avança uma vez por passo.

Dano do Pato respeita a invulnerabilidade de PlayerState. Vida zero encerra como
Lost; vida zero do chefe encerra como Won, cancela sinais/efeitos e emite derrota
uma vez. Estado terminal, desativação ou pool descartado cancelam trabalho pendente.
Não há recuperação de ataque antigo ao reativar. A T060 integra armas/Quack e a
entrada aos 900 segundos. Pausa/seleção congelam aviso, som e efeitos, preservando
o ataque pendente; ao retomar Boss, a antecedência restante continua. Os fatores
de vida/dano do encontro são aplicados sem alterar FrogKingSettings.

FrogKingPractice.unity é uma fixture jogável apenas no Editor, fora das cenas da
build. WASD/setas movem o Pato; um botão aplica 10% de dano ao chefe para explorar
as fases. Esse botão é um instrumento de treino, não combate por arma. O teste
carrega a cena, verifica a composição e captura uma imagem; não constitui aceite
humano de percepção, justiça ou desempenho.

## RunDirector e composição da partida (T060)

RunBootstrap abre LakeRun, com GameBootstrap aguardando Preparing até progressão,
Pato e loadout ficarem prontos. RunDirector assume ExternalClock; GameBootstrap
não avança um segundo relógio. Bootstrap/Lake continuam como fixtures de treino.
RunBootstrap é a primeira cena da lista de build; o gerador LakeRunSetup mantém
as cenas de treino e preserva seus GUIDs.

Initialize recebe serviços, Pato, UpgradeSystem, WeaponRunner, catálogo da build,
loadout, LakeRunSettings, refúgios e JsonSaveService. A inicialização valida o
conteúdo e aquece os pools de inimigos, XP, chefe e invocações antes da simulação.
O chefe é devolvido após aquecer seus pools e adquirido aos 900 segundos.
Advance(seconds, visibleArea) dirige relógio, timeline, spawn, scheduler, armas
e chefe. Os demais runners não avançam automaticamente esse mesmo trabalho.

Cada início de onda e marco da timeline emite EncounterStarted uma vez. Ondas
repetem sua composição na cadência autoral e ajustam SpawnDirector.ActiveLimit
ao MaxActive da onda, dentro da capacidade global. Um frame longo não produz
todos os lotes atrasados. Minibosses têm prioridade Essential e retry limitado
se não houver espaço; o pool de cada miniboss contém uma única instância.
Aos 900 segundos, BossRequests passa a 1, ondas/pendências/inimigos anteriores
são limpos sem recompensa de despawn e o chefe começa. O relógio continua durante
o combate final; completar 900 segundos sozinho não concede vitória.

Combate usa CombatTargetBinding/QuackTarget nos sete inimigos e chefe. Delegates
estáveis resolvem o lease atual e encaminham arma/Quack ao scheduler ou chefe;
os produtores de dano aceito publicam os eventos, evitando dano duplicado no bus.
Controle por Quack respeita resistências; dano frontal do Besouro usa o scheduler.
Fatores de vida/dano do encontro são aplicados uma vez por lease; reset os remove.
As composições de ondas/receitas/IDs são copiadas na inicialização, sem consultar
getters que criam snapshots dentro do loop de spawn.

EnemyReward registra derrota/moedas uma vez e cria XP no pool limitado; saturação
suprime o pickup e incrementa SuppressedPickups. Armas são atribuídas por ID de
conteúdo e Quack pela origem do Pato. O golpe e a derrota finais do Sapo-Rei são
publicados antes de Won, para entrarem no snapshot da T058. Vida zero do Pato
encerra Lost; TryAbandon aceita estados ativos/modais e nunca gera vitória.

RunState.ResumePhase guarda Playing ou Boss ao entrar em Paused/ChoosingUpgrade;
TryResume volta à origem. Escape/botão de pausa, loadout e cartas usam esse caminho.
Um modal do chefe não reinicia ondas nem pede outro chefe. Controles e invocações
congelam durante os modais. Ao terminar, o diretor limpa entidades próprias,
congela Result e emite ResultAvailable/ResultsAvailable uma vez, após sair da
pilha de dano. Não avança automaticamente a Results: a tela completa fica na T061.

Na cena, SavePathProvider resolve lake-relics.json em Application.persistentDataPath;
testes substituem esse provider por um diretório exclusivo. O save carrega antes
de iniciar o combate. Unlocks/equipamento são gravados quando sujos em pausa,
seleção ou terminal, ao suspender o app e ao descarregar a sessão. Falhas ficam
em LastSave com diagnóstico e não impedem resultado/encerramento. Persist não
credita moedas/recordes/desafios; o crédito pertence a CommitProgress na T061.

RunDirectorFlowTests usa serviços/assets reais com clock manual para verificar
limites e finais. RunSceneFlowTests abre as cenas, verifica relógio único, HUD,
controles/loadout e persistência. Essas evidências não aprovam balanceamento,
percepção dos sinais ou FPS em hardware; a matriz de plataforma/playtest permanece.

## PresentationEvent

Eventos minimos: terreno alterado, Folego pronto/consumido, perigo telegrafado, boss apresentado,
evolucao obtida, recompensa rara e resultado disponivel.

Cada evento declara prioridade e duracao maxima. Alertas de perigo e estado de Folego tem precedencia
sobre efeitos cosmeticos; Safe Area e proporcao nao podem ocultar informacao essencial.

## ResultsView e progresso persistido (T061)

ResultsBridge recebe ResultAvailable e mantém RunResult imutável. CommitProgress
usa regras copiadas de ResultsProgressionDefinition. RunState passa a Results
preservando Outcome. Três colunas roláveis apresentam tempo/chefe, derrotas/dano
por origem, sequência, armas/passivas/níveis, evoluções, sinergias, relíquias,
recursos/histórico de escolhas, moedas/bônus/desconto/saldo, desbloqueios,
recordes anteriores/atuais e progresso de desafios com nomes autorais.

RunId é único por partida, independente da seed. RunProgression.Apply devolve perfil
e recibo imutáveis; repetir uma ID não credita novamente. Recordes usam máximos de
tempo (segundos inteiros), derrotas, sequência e dano. Desafios acumulam ou usam a
melhor partida, limitados à meta; somente vitória aumenta Victories. Moedas usam
o valor final já calculado no resultado, com saturação em int.MaxValue.

Schema 2 acrescenta progress: coins, runs, victories, records [{id,value}],
challenges [{id,value}] e appliedRunIds. Contadores não negativos, recordes finitos,
IDs únicos e quantidade de recibos igual a runs são invariantes. JSON mínimo sem
progress representa perfil vazio. Perfil inválido recupera o backup inteiro.
Limites de leitura permanecem; escrita também recusa arquivos acima de 1 MiB.

CommitResult recarrega o perfil e grava resultado aplicado, recibo e relíquias na
mesma substituição atômica. ID já aplicada retorna sem escrita/rotação de backup.
Save(loadout) preserva o perfil. IDs aplicadas não são descartadas. Falha permite
retry sobre o estado autoritativo sem crédito duplo. A tela distingue Progresso salvo
de falha; saldo projetado aparece como Saldo após salvar enquanto pendente.

JOGAR NOVAMENTE remove o bootstrap anterior, espera um frame e carrega RunBootstrap
com nova ID e progresso salvo. Se o jogador sair com falha de armazenamento não
resolvida, o resultado pendente não tem persistência garantida. Não há histórico
integral de partidas em disco. Configurações não foram acrescentadas ao save.

## Contrato de paridade de entrada (T062, fase vermelha)

InputParityTests possui 15 casos PlayMode que executam adaptadores reais. Teclado
usa KeyboardState (WASD, Space, Escape); controle usa GamepadState (leftStick,
South para Quack, Start para pausa). Eventos passam por QueueStateEvent e Update.
SetUp/TearDown preservam configuração de foco e dispositivos atuais; só os dispositivos
sintéticos próprios são removidos, sem reset global do Input System.

T065 deve fornecer GamepadInputAdapter e T066 MobileInputAdapter no namespace
QuackQuack.Platform.Input, ambos IPlayerInput. A fixture instancia o tipo real por
construtor público sem argumentos ou AddComponent para componente Unity. A ausência
é falha explícita, nunca Ignore ou substituto de teste. O shim de reflexão deve ser
substituído por chamadas tipadas quando as implementações existirem.

O contrato de callbacks do toque é SetMove(Vector2), SetQuack(bool) e SetPause(bool).
SetMove recebe o vetor normalizado do joystick; bool representa botão pressionado
ou solto. Read consome bordas uma vez, preservando movimento enquanto mantido.
Repetir true entre updates não cria nova borda; soltar e pressionar novamente cria.
São pontos de entrada dos controles móveis futuros, não simulação de um Touchscreen
nem validação de arraste, múltiplos dedos ou hit testing de UI. T066 deve ligar os
controles de toque reais a esses callbacks; o mapa unificado permanece na T067.

A sequência compara repouso, direções, diagonal, Quack, manutenção, soltura, nova
ativação e pausa. Checkpoints do PlayerController real com seed 62 são comparados
com o caminho real de teclado: velocidade no Rigidbody2D, terreno, Fôlego, vida,
fase, relógio e número de eventos QuackActivated. Também há oráculos explícitos
para velocidades autorais, consumo inicial, dois Quacks aceitos após cooldown,
prioridade da pausa e parada. Cada fonte é testada após Won/Lost/Abandoned e Results.

O passo do componente é controlado (ApplyCommand, FixedUpdate, Advance), sem esperar
frames reais. A fixture posiciona o Pato em pontos iguais de terra/água longe de
outras entidades. Não mede deslocamento/colisão, frame pacing, hardware, Safe Area
ou equivalência ponta a ponta na cena publicada. Falhas por adaptadores ausentes
confirmam apenas a descoberta dos casos: paridade dos três dispositivos continua
pendente de T065/T066 e validação correspondente.

## Contrato de UI responsiva (T063, fase vermelha)

ResponsiveUiTests exercita o HUD real de Bootstrap/Lake nas resoluções paisagem
1920×1080 (16:9), 2160×1080 (18:9), 2340×1080 (19,5:9) e 1440×1080 (4:3).
A fixture pausa o treino, desliga o binding e apresenta um snapshot com vida,
Fôlego parcial, água, indisponibilidade do Quack, mensagem e tempo. Usa câmera
própria e RenderTexture; não altera Screen.SetResolution ou arquivos de cena.
Valida tamanho e projeção do Canvas antes de medir seus elementos em pixels.

Os oito elementos essenciais do HUD são os bindings healthLabel, breathLabel,
terrainLabel, quackLabel, statusLabel, timeLabel, healthFill e breathFill. Cada um
deve estar ativo, com os quatro cantos dentro dos limites seguros (tolerância de
0,5 pixel). Textos não podem ficar vazios ou exceder verticalmente sua caixa.
O caso sem recortes serve de baseline do layout autoral. A composição responsiva
exige CanvasScaler no Canvas real, ScaleWithScreenSize e referência 1920×1080;
a fixture não acrescenta scaler para ocultar ausência da T069.

T068 deve fornecer QuackQuack.Presentation.UI.SafeAreaController, MonoBehaviour no
RectTransform que delimita o conteúdo seguro, filho do Canvas. O controlador tem
SetViewportProvider(Func<Vector2>, Func<Rect>) para injetar tamanho e área segura;
no jogo, o padrão usa os dados reais da tela. LateUpdate atualiza o retângulo
quando tamanho ou Safe Area mudarem. O teste chama esse mesmo caminho e também
aguarda frames para comprovar atualização automática. O shim por reflexão localiza
o tipo real; ausência falha explicitamente e deve dar lugar a chamadas tipadas
na implementação T068. Todos os elementos essenciais devem pertencer à hierarquia
segura do controlador presente no HUD autoral (integração T068/T069).

A matriz usa margens assimétricas de 72 px à esquerda, 24 à direita, 32 abaixo e
48 acima, sem atribuí-las a um aparelho real. O retângulo normaliza os limites
por tamanho total em anchorMin/anchorMax, com offsets zerados. Reaplicar não acumula
margens; voltar ao viewport inteiro restaura anchors zero/um. A sequência dinâmica
muda 16:9 para 19,5:9, troca o lado do recorte e altera só a Safe Area mantendo a
resolução, antes de remover os recortes. Essas mudanças ocorrem pela fonte injetada,
sem cálculo substituto de layout na fixture.

Esta cobertura geométrica do HUD não representa aceite de legibilidade humana,
matriz de todas as telas/modais, toque físico, recorte Android real ou desempenho.
A rotação em paisagem é representada por mudança de lado do recorte; retrato não
faz parte do produto. Aceite integral da interface permanece na validação T075.

## Smoke de build e inicialização (T064)

PlatformBuildSmokeTests é uma fixture PlayMode também compilada nos test players.
A categoria PlatformRuntimeSmoke verifica versão Unity fixada, disponibilidade de
RunBootstrap/LakeRun e carregamento explícito da entrada de produção. Aguarda até
30 segundos reais pela prontidão do RunDirector, exige um bootstrap único, serviços
de eventos/pool, simulação Playing, Pato vivo, HUD, câmera, catálogos e diretores.
O relógio deve avançar em até dois segundos. No player, exige WindowsPlayer ou
Android, processo 64-bit e compilação ENABLE_IL2CPP. Editor não substitui essa prova.

Cada caso injeta save.json em diretório exclusivo sob Application.temporaryCachePath.
O provider anterior é restaurado após unload, e a limpeza valida caminho absoluto,
prefixo do cache e nome próprio antes de remover a pasta. O caso que somente
consulta cenas não descarrega a cena do Test Runner. Não há acesso ao save pessoal.

PlatformBuildConfigurationSmokeTests é compilada somente no Editor. Usa reflexão
para exigir o tipo real QuackQuack.Editor.PrototypeBuildPipeline (T070), sem stub,
Ignore ou troca implícita do alvo ativo. CreateOptions(BuildTarget, string) deve
retornar BuildPlayerOptions para StandaloneWindows64 ou Android, respeitar o caminho
de saída, incluir RunBootstrap primeiro e LakeRun, não repetir cenas nem incluir
fixtures Practice/Test, e compartilhar a mesma lista de gameplay entre os alvos.
ConfigureTarget(BuildTarget) deve selecionar IL2CPP; Android usa somente ARM64.
O teste restaura backend e arquiteturas anteriores em finally. ConfigureTarget
não deve iniciar build, instalar pacotes ou trocar o alvo ativo durante o teste.

A fixture de runtime pode ser selecionada por -testCategory PlatformRuntimeSmoke
com -testPlatform StandaloneWindows64 ou Android. O Unity Test Framework constrói
e executa seu test player; esses artefatos incluem a cena inicial do Test Runner.
A lista/entrada da build de produto é verificada separadamente pelo contrato T070.
Isso não é aceite de release, gameplay, controles, Safe Area ou desempenho.
