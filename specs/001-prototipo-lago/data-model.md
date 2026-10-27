# Data Model: Prototipo Jogavel do Lago

## Principios do modelo

- Definicoes autorais sao imutaveis durante a partida e referenciadas por identificadores estaveis.
- Estado runtime e criado por partida e nunca gravado de volta nos assets de conteudo.
- Referencias entre definicoes sao validadas no Editor antes de uma build.
- Valores de porcentagem usam intervalo normalizado; tempos usam segundos; distancias usam unidades
  de mundo.

## Entidades de conteudo

### CharacterDefinition

Campos: `id`, nome, atributos base, perfil de movimento em agua/terra, definicao do Quack, arma
inicial, apresentacao. Relaciona-se a uma `QuackDefinition` e a uma `WeaponDefinition`.

Validacao: identificador unico; vida e velocidades positivas; Pato e a unica definicao selecionavel
no prototipo.

### QuackDefinition

Campos: custo de Folego, dano, alcance, forca de empurrao, duracao de interrupcao, regra de reflexao,
multiplicadores de carga cheia e efeitos adicionais.

Validacao: custo entre zero e capacidade maxima; projeteis reflefiveis devem declarar essa propriedade;
efeitos nao podem aplicar colisao mais de uma vez por onda/alvo.

### WeaponDefinition

Campos: `id`, nome, nivel maximo, modo de alvo, cadencia, dano, projeteis/area, efeitos por nivel,
passiva compativel e evolucao resultante.

Inclui `terrainAffinity`, um conjunto opcional de modificadores para agua e terra aplicado sem alterar
a definicao durante a partida.

Validacao: exatamente cinco niveis; seis armas ativas no catalogo do prototipo; evolucao somente se
ambas as referencias existirem.

### PassiveDefinition

Campos: `id`, nome, atributo afetado, modificador por nivel, limite, arma compativel.

Validacao: seis passivas ativas; modificadores dentro de limites de balanceamento definidos.

### EvolutionDefinition

Campos: `id`, arma base, passiva exigida, arma resultante, feedback de desbloqueio.

Validacao: exatamente tres no prototipo; arma base exige nivel cinco; combinacao unica.

### EnemyDefinition

Campos: `id`, funcao tatica, vida, velocidade, contato/ataques, resistencias, recompensas, prefab,
pool inicial/maximo, sinais de perigo.

Validacao: cinco tipos comuns ativos; todo ataque perigoso possui antecedencia visual e sonora;
nenhum inimigo exige habilidade exclusiva inexistente.

### EncounterDefinition

Campos: `id`, janela de tempo, composicao, taxa/limite de spawn, arena, condicoes de conclusao e
modificadores.

Validacao: janelas nao deixam a partida sem progressao; dois minibosses antecedem o minuto 15;
Sapo-Rei e o encontro final.

### RelicDefinition

Campos: `id`, nome, descricao, condicao de desbloqueio, efeitos e raridade.

Validacao: dez reliquias cadastradas; no maximo tres equipadas; efeitos nao criam dependencia de
monetizacao.

### PrototypeEnemyRoster

Formiga: pressao por quantidade. Aranha: teias e reducao de mobilidade. Vespa: investida telegrafada.
Besouro: armadura frontal e flanqueamento. Caramujo Aquatico: trilha de lentidao e controle de rota.
Libelula Alfa: miniboss do minuto 5 com rasantes e corredores de vento. Sapo-Cururu: miniboss do
minuto 10 com saltos curtos e pocoes de area. Sapo-Rei: chefe do minuto 15.

## Entidades runtime

### RunState

Campos: `runId`, seed, fase (`Preparing`, `Playing`, `Paused`, `ChoosingUpgrade`, `Boss`, `Won`,
`Lost`, `Abandoned`), tempo decorrido, vida, Folego, XP, nivel, build, reliquias, estatisticas e
encontro atual.

Transicoes:

```text
Preparing -> Playing
Playing <-> Paused
Playing -> ChoosingUpgrade -> Playing
Playing -> Boss -> Won
Playing/Boss -> Paused/ChoosingUpgrade -> fase de origem
Playing/Boss -> Lost
Playing/Paused/ChoosingUpgrade/Boss -> Abandoned
Won/Lost/Abandoned -> Results
```

Regras: estados terminais sao idempotentes; pausa congela simulacao; selecao congela combate, mas nao
duplica eventos; minuto 15 solicita o chefe exatamente uma vez. T060 guarda a fase
de origem em ResumePhase; TryResume não troca Boss por Playing após um modal.

### PlayerState

Campos: posicao, terreno atual, vida, invulnerabilidade, Folego, estado do Quack, modificadores,
inventario e contadores de recursos.

Regras: Folego fica entre zero e capacidade; troca de terreno usa histerese/volume unico para impedir
oscilacao; dano respeita janela de invulnerabilidade.

### BuildState

Campos: seis slots de armas, seis slots de passivas, niveis, evolucoes, rerrolagens restantes,
banimento restante e historico de escolhas.

Regras: sem duplicatas invalidas; duas rerrolagens e um banimento por partida; evolucao consome ou
substitui a arma base sem exceder slots.

### RelicLoadout

Campos: IDs desbloqueados, IDs equipados e efeitos ativos.

Regras: exatamente dez definicoes disponiveis no catalogo; zero a tres reliquias equipadas; somente
IDs desbloqueados e conhecidos podem ser equipados; equipar/desequipar recompõe efeitos sem acumulo
duplicado.

### EnemyState

Campos: instancia, definicao, vida, posicao, estado de comportamento, efeitos, alvo e estado do pool.

Regras: uma instancia so pode estar ativa ou no pool; retorno limpa efeitos, eventos e referencias;
morte concede recompensa uma vez.

### RunResult

Campos: resultado, duracao, chefe alcancado, inimigos derrotados, dano por arma, maior sequencia,
snapshot da build, sinergias, moedas, itens, recordes e progresso de desafios.

Regras: produzido uma unica vez a partir do estado terminal; abandono aplica recompensa parcial e
nunca registra vitoria.

### SaveData

Campos: `schemaVersion`, configuracoes, recordes, moedas, reliquias desbloqueadas/equipadas, desafios
e ultima migracao.

Regras: tres reliquias equipadas no maximo; IDs desconhecidos sao ignorados com diagnostico; arquivo
invalido usa backup ou valores padrao sem impedir o jogo.

### RunProgressProfile e RunProgressReceipt (T061)

O schema 2 implementado armazena relíquias e Progress: moedas, partidas, vitórias,
recordes por ID, desafios por ID e IDs de partidas aplicadas. Configurações e última
migração da proposta acima continuam fora desta implementação. Perfil é imutável;
recibo contém perfil, deltas de recordes/desafios, moedas adicionadas e AlreadyApplied.
RunResult continua sendo snapshot terminal separado do perfil acumulado.

Uma ID única por partida garante aplicação única, mesmo com seed repetida. Carteira,
recordes, desafios e recibo são gravados atomicamente. Contagem de recibos coincide
com partidas; vitórias não excedem partidas. Recordes só aumentam; desafios não
regredem e respeitam suas metas. Regras usam derrotas, sobrevivência, vitórias ou
evoluções, cumulativas ou por melhor partida. Versões 0/1 migram com perfil vazio.
