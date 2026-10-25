# Research: Prototipo Jogavel do Lago

## Unity e renderizacao 2D

**Decision**: Usar a versao instalada Unity 6000.4.3f1 em um projeto 2D com URP e 2D Renderer,
mantendo uma unica base para Windows x64 e Android ARM64 e fixando essa versao durante o prototipo.

**Rationale**: A versao instalada oferece os recursos necessarios e evita a instalacao paralela de
outro Editor. Fixar exatamente 6000.4.3f1 limita variacao entre ambientes; qualquer migracao exige
emenda constitucional e regressao completa. O renderer 2D atende o prototipo sem uma segunda pilha.

**Alternatives considered**: Instalar Unity 6.3 LTS (maior estabilidade, mas custo operacional agora);
Built-in Render Pipeline (menos alinhado); projeto 3D com sprites (complexidade desnecessaria);
engines distintas por plataforma (viola paridade).

## Conteudo dirigido por dados

**Decision**: Definir personagens, armas, passivas, evolucoes, inimigos, encontros e reliquias como
ScriptableObjects imutaveis em runtime; manter estado mutavel da partida em objetos de dominio.

**Rationale**: ScriptableObjects centralizam dados compartilhados e evitam copias por prefab, mas nao
devem ser usados como mecanismo de salvamento no build. Isso separa autoria/balanceamento de estado e
reduz duplicacao. Referencia: [Unity Manual - ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html).

**Alternatives considered**: Constantes em scripts (dificulta balanceamento); JSON para todo o
conteudo (perde fluxo de autoria e referencias do editor); estado runtime dentro dos assets (risco de
contaminacao entre sessoes e editor).

## Entrada multiplataforma

**Decision**: Um asset de acoes expoe Move, Quack e Pause. Adaptadores de teclado, controle e toque
produzem o mesmo `PlayerCommand`; a simulacao nunca consulta dispositivos diretamente.

**Rationale**: Mantem regras iguais e permite testes de entrada em estado conhecido. O Input System
oferece fixture propria para simular dispositivos e acoes em testes. Referencia:
[InputTestFixture](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/api/UnityEngine.InputSystem.InputTestFixture.html).

**Alternatives considered**: Consultar teclas/toques em `PlayerController` (acopla plataforma);
controladores separados com regras proprias (gera divergencia).

## Pooling e atualizacao de hordas

**Decision**: Usar `UnityEngine.Pool.ObjectPool<T>` por tipo de inimigo, projetil, pickup e VFX, com
um `PoolService` para ciclo de vida e um `SpawnDirector` para ritmo. Processos de alta frequencia sao
agrupados e distribuidos entre frames quando nao alteram a resposta imediata.

**Rationale**: O pool nativo reduz criacao/destruicao repetitiva e expoe contadores uteis para
instrumentacao. Referencia: [Unity ObjectPool API](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html).

**Alternatives considered**: `Instantiate/Destroy` continuo (picos de CPU/GC); ECS/DOTS desde o inicio
(proibido sem evidencia do Profiler); pool generico sem politicas por tipo (capacidade e reset pouco
controlaveis).

## Persistencia local

**Decision**: Serializar um `SaveData` pequeno em JSON, com `schemaVersion`, escrita atomica em
arquivo temporario seguida de substituicao e valores padrao em falha de leitura. ScriptableObjects
mantem apenas definicoes autorais.

**Rationale**: O prototipo precisa guardar configuracoes, recordes, desbloqueios e reliquias sem
backend. Versionamento permite migracao, e a escrita atomica reduz corrupcao.

**Alternatives considered**: PlayerPrefs para todo o estado (pouco estruturado); banco embutido
(escopo excessivo); nuvem (fora do prototipo).

## Elenco e encontros do prototipo

**Decision**: Usar Formiga, Aranha, Vespa, Besouro e Caramujo Aquatico como inimigos comuns;
Libelula Alfa como miniboss do minuto 5; Sapo-Cururu como miniboss do minuto 10; e Sapo-Rei no minuto
15.

**Rationale**: O elenco cobre quantidade, restricao de movimento, esquiva, flanqueamento e controle
de rota. Os minibosses combinam mobilidade aerea e controle de area sem duplicar a progressao em
fases do Sapo-Rei.

**Alternatives considered**: Cinco inimigos exclusivamente aquaticos (menor variedade tatica);
Girino como inimigo comum (sobrepoe a invocacao do chefe); minibosses sem identidade propria (nao
testam build e posicionamento de forma verificavel).

## Afinidade de terreno e reliquias

**Decision**: Equipamentos declaram modificadores opcionais por agua/terra, resolvidos por um servico
de afinidade. Reliquias usam catalogo autoral, estado de desbloqueio e `RelicLoadout` limitado a tres,
com efeitos compostos pela mesma infraestrutura de modificadores.

**Rationale**: Isso torna verificavel a eficiencia de equipamentos por terreno e impede que reliquias
sejam apenas dados persistidos sem impacto jogavel.

**Alternatives considered**: Condicionais em cada arma (duplica regras); efeitos de reliquia dentro da
UI (acopla apresentacao); equipar quantidade ilimitada (viola a especificacao).

## Testes e profiling

**Decision**: Testar regras em EditMode, integracoes de GameObjects e cenas em PlayMode, e manter um
cenario de benchmark deterministico com 200 e 400 inimigos. Validar CPU, GPU, GC e memoria em aparelho
Android real e Windows.

**Rationale**: Testes rapidos protegem regras; PlayMode cobre integracao; somente medicao no hardware
alvo confirma os orcamentos. Unity documenta que requisitos minimos nao garantem desempenho de um
projeto especifico. Referencia: [Unity 6 system requirements](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html).

**Alternatives considered**: Apenas testes manuais (regressoes lentas); apenas Editor (nao representa
IL2CPP, termica ou GPU movel); benchmark sintetico fora da cena real (baixa fidelidade).

## Conclusao

Todas as decisoes tecnicas necessarias ao plano foram resolvidas. Nao ha marcadores de esclarecimento
pendentes.
