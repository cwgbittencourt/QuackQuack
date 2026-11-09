# Manifesto artístico do Lago — T041

Inventário em 09/11/2026, base `665bbf1`, Unity 6000.4.3f1. Entrega documental de FR-001/002/006 e contrato C7. A direção cartoon está aprovada em D8; **nenhum asset final ou cena-amostra está aprovado por este inventário**. T042 produz a amostra e pede aceite antes dos lotes T043–T046. Não há alteração de arte, colisão, balanceamento ou importação nesta task.

## Referências, origem e licença

- R1: [partida cartoon](references/direcao-cartoon-candidata.png), imagem gerada na conversa e confirmada pelo usuário. Contorno escuro arredondado, volumes simples, expressões fortes, preenchimentos quentes, água azul-esverdeada e painéis claros com borda escura. Referência de linguagem, não sprites recortáveis ou autorização de espécies.
- R2: [corações e penas](references/hud-coracoes-penas-referencia.png), estudo gerado na conversa. Origem estilística dos ícones; o HUD vigente usa duas barras contínuas sem números. Não copiar contagem de corações/penas, números, posições ou tartarugas da ilustração R1.
- R3: [decisões anexadas](references/decisoes-anexadas.jpeg), documento fornecido pelo usuário; não é referência de arte.
- A0: recursos procedurais existentes, produzidos pelos geradores C# deste repositório, sem pacote artístico externo identificado na inspeção. Origem interna não implica licença pública: não há declaração de licença individual nesses arquivos. Preservar proveniência do projeto; não atribuir CC0 por inferência.
- A1: arte futura original, ainda não produzida. Para cada entrega registrar autor/ferramenta, data, referência, arquivo-fonte, exportação, condições de uso e eventual licença de terceiro. Se usar geração por IA, registrar essa origem; se usar fonte externa, guardar prova da licença antes de integrar. Estado atual de autoria/licença final: pendente de produção.
- F0: `LegacyRuntime.ttf`, fonte interna do Unity obtida por `Resources.GetBuiltinResource<Font>` nos geradores. Não há arquivo de fonte próprio exportado no projeto. Qualquer substituição deve registrar licença e cobertura de acentos portugueses; não redesenhar glifos de marcas de controles sem fonte autorizada.

## Convenções de leitura e produção

Os IDs sem prefixo abaixo são IDs reais de conteúdo, não nomes artísticos novos. IDs com `art.`, `fx.`, `ui.`, `env.` ou `audio.` são chaves **documentais**, sem introduzir ContentDefinition ou espécie. Evoluções têm ID da receita e ID da arma resultante distintos. Nenhuma skin altera esses vínculos.

Caminhos abreviados: `C/` = `Assets/QuackQuack/Content/`; `P/` = `Assets/QuackQuack/Prefabs/Prototype/`; `E/` = `Assets/QuackQuack/Editor/`; `V/` = `Assets/QuackQuack/Presentation/`; `S/` = `Assets/QuackQuack/Scenes/`. Todos os caminhos são relativos à raiz do repositório. Finais propostos sob `Assets/QuackQuack/Art/LakeCartoon/`; fontes editáveis em `Sources/` dentro de cada categoria, exportações PNG/sheets ao lado. Esses caminhos ainda não existem e não são evidência de entrega.

Prioridades respeitam a ordem aprovada: P0 amostra T042; P1 personagens/ameaças/obstáculos T043; P2 armas/efeitos/recompensas T044; P3 HUD/menus/controles T045; P4 cenário/água T046. P0 reutiliza representantes dos lotes, não autoriza produzi-los todos. Em todas as tabelas de produção, referência R1, origem final A1 e aprovação pendente são herdadas, salvo indicação explícita.

### Perfis propostos — confirmar escala na amostra

Dimensões são alvos de exportação por quadro, não dimensões já existentes nem novos raios físicos. Transparência com margem, sem texto rasterizado. Pivôs normalizados. FPS/quadros abaixo são propostas de produção a avaliar na T042, não contratos de gameplay.

| Perfil | Dimensão / pivô / importação proposta | Estados, camada e contraste |
|---|---|---|
| CH | Pato/comum 256×256; especiais 512×512; centro físico (0,5/0,5), alinhado ao corpo atual | Idle, mover em terra/água, preparar/atacar, dano e derrota; ciclos curtos de 4–8 quadros quando necessários. Silhueta e expressão distintas em cinza. Manter orientação/direção de ataque real e corpo físico visível. |
| OB | Pedras/bloqueios 256×256 ou módulos 512×256; pivô na âncora do collider | Estático; contorno fechado e base sólida. Arte se ajusta ao collider existente, não gera Physics Shape novo. |
| CO | Projétil 128×128; área/efeito 256×256; centro (0,5/0,5) | Rotação/voo/pulso/detonação conforme arma real; borda perceptível e interior translúcido. Tamanho em unidades deriva do raio efetivo, não do tamanho do PNG. |
| UI | Ícones 128×128; painéis 512×256 com borda 9-slice a autorar; centro (0,5/0,5) | Estados normal/foco/pressionado/desabilitado; texto vetorial uGUI separado. Contorno e símbolo distinguem estados além da cor. |
| EN | Solo/água 512×512 repetível; vegetação 256×256, base (0,5/0); centro para tiles | Decoração de baixo contraste. Água com ciclo suave proposto de 4–8 quadros ou material equivalente, sem mudar máscara de terreno. |

Para CH/OB/CO/EN: Sprite 2D, sRGB, alpha transparency, Bilinear, Clamp (Repeat somente tiles), sem mipmaps nem Read/Write; PPU inicial 128, escala visual calibrada para preservar medidas mundiais atuais. Sheets em Multiple com pivôs iguais por quadro; imagens únicas em Single. UI: PPU 100 e bordas explícitas, mipmaps off. Propor Windows RGBA32 para inspeção e Android ASTC 6×6 como ponto inicial, sujeitos a suporte, bordas legíveis, memória e medição; não aplicar override nesta task. Atlas por categoria somente após medir, sem textura gigante única. Sem collider automático a partir do sprite.

Preservar ordenação funcional atual: terreno -10…-6, bloqueios 1, Pato 5…8, inimigos 10/indicadores 12, cura 12/13, ataques/áreas 14 e avisos do chefe 20; valores são de componentes distintos, não uma nova camada global. Confirmar objetos serializados e Canvas antes de integrar. VFX não podem cobrir barras/telegráficos; aura fica atrás do corpo (atualmente -1). HUD e modais continuam nas hierarquias/Canvas atuais, Safe Area e foco preservados.

## Elenco e bloqueios — P1 / Characters e Objects

Pato e inimigos atuais são composições de Circle/Square, tints e linhas; não possuem sheets/Animator autorados. Fonte atual: geradores `E/LakeArenaSetup.cs`, `E/LakeEncounterCatalogSetup.cs`, `E/FrogKingSetup.cs`; dados/prefabs abaixo são vínculos de integração. Perfil CH, exceto bloqueios OB. Nome-base da exportação final = ID da linha, sob Characters/ ou Objects/.

| ID canônico | Função / vínculo atual | Identidade e estados específicos a produzir |
|---|---|---|
| pato | Jogador; `C/Characters/Pato.asset`, composição em Lake/LakeRun | Bico, asa e pés legíveis; remar/nadar versus caminhar, reação de dano e vocalização Quack. Não escalar raiz/collider de raio 0,42 para acomodar desenho. |
| formiga | Comum/enxame; `C/Enemies/Prototype/formiga.asset`, `P/Enemies/formiga.prefab` | Corpo segmentado, antenas e pernas; caminhada curta e contato, leitura mesmo em horda. |
| aranha | Comum/teia; mesmo padrão de caminho para aranha | Oito pernas e postura de lançar teia, sem parecer formiga recolorida. |
| vespa | Comum/investida; padrão vespa | Asas, cintura e direção do ferrão; antecipação e arrancada distinguíveis. |
| besouro | Comum/blindagem frontal; padrão besouro | Carapaça e frente protegida legíveis na direção real; não desenhar armadura em todo o corpo sugerindo imunidade total. |
| caramujo-aquatico | Comum/rastro lento; padrão caramujo-aquatico | Concha espiral, corpo baixo e rastro visual separado; deslocamento deslizante. |
| libelula-alfa | Miniboss/investida; padrão libelula-alfa | Quatro asas, abdome longo, maior presença sem virar outra espécie; antecipação/mergulho/retorno. Barra individual externa. |
| sapo-cururu | Miniboss/área; padrão sapo-cururu | Corpo largo, pele e bochechas; preparação e emissão da área. Barra individual externa. |
| sapo-rei | Chefe; `C/Enemies/Prototype/FrogKingSettings.asset`, `P/Enemies/sapo-rei.prefab` | Sapo com insígnia régia proposta; idle, preparação, ataques e transição das fases reais. Manter separação das zonas seguras/perigosas e barra própria. Sem XP/cura ao morrer. |
| art.obstacle.rocks | Training rock / South rock em Lake/LakeRun; `E/LakeArenaSetup.cs` | Rocha irregular cartoon ajustada aos dois retângulos físicos existentes; não esconder saída livre perto das quinas. |
| art.obstacle.banks | North/South/West/East bank e Risk corridor north/south; mesmo gerador/cenas | Margem bloqueante com borda contínua distinguível da margem decorativa. Não mover limites nem corredores. |

Não criar tartarugas ou substituir comuns por sapos da referência. O chefe é identificado por `LakeEncounterCatalog.FinalBossId` = sapo-rei, não por EnemyDefinition adicional: sete EnemyDefinition + um chefe, oito ameaças no total.

## Armas e evoluções — P2 / Combat

Dados atuais em `C/Weapons/Prototype/<ID>.asset`; os nove WeaponDefinition usam Circle como sprite compartilhado. Integração: `Gameplay/Weapons/WeaponRunner.cs`, `WeaponProjectile.cs` e `V/Weapons/WeaponVisuals.cs`. Perfil CO para mundo e UI para ícone (exportação separada `<ID>-icon.png` em UI/). Cada linha exige ícone/cartas e visual de ataque; nenhum incremento de nível autoriza ampliar dano/alcance por decoração.

| ID de arma | Função e forma proposta | Animação / alcance e contraste |
|---|---|---|
| bumerangue | Pena curva/bumerangue reconhecível | Giro, ida e volta visíveis; dois trechos reais, sem rastro sugerindo dano fora do projétil. |
| ovos | Ovo com casca e sombra curta | Colocação e detonação; anel de raio real já existe, manter congelamento em pausa. |
| quack-frontal | Ondas/setor saindo do bico | Pulso frontal acompanha ângulo/alcance reais; distinguir do Quack ativo. |
| aura-energia | Halo contínuo translúcido | Borda fixa no raio real, interior pulsante sem fingir mudança de alcance; não ocultar parede/atores. |
| foguetes | Foguete com ponta e aletas | Voo orientado e explosão distinta de Ovos; limites reais do impacto. |
| bolhas | Bolha com contorno, brilho e transparência | Crescer com HitRadius efetivo, inclusive Sabão N0/N1/N5; visual e colisão coincidem. Vigente +16%/nível até +80%, sem aplicar proposta futura de tuning. |
| tornado-penas | Evolução: espiral de penas | Vários bumerangues distinguíveis, giro/retorno preservados; não sugerir área contínua inexistente. |
| incubadora-combate | Evolução: ovos/incubadora reforçados | Detonação maior de raio real, já sinalizada pelo anel; alcance não fica restrito ao tamanho do ovo. |
| quack-supersonico | Evolução: frente sônica em camadas | Setor real mais forte com desenho distinto, sem confundir com ataque circular. |

Receitas preservadas: `evolucao-bumerangue` → `tornado-penas`; `evolucao-ovos` → `incubadora-combate`; `evolucao-quack-frontal` → `quack-supersonico`. Arquivos correspondentes em C/Weapons/Prototype; são três receitas, não três ataques extras.

## Efeitos, recompensas e fixtures — P2 / Combat

Linhas seguintes são chaves documentais; fonte atual A0. Perfil CO; exportações futuras com nome da chave sem prefixo. Reuso/pooling, duração e pausa seguem os componentes atuais, sem Instantiate/Destroy recorrentes.

| ID documental | Vínculo atual / função | Estados e leitura necessários |
|---|---|---|
| fx.quack-active | `C/Characters/PatoQuack.asset`, `P/QuackFeedback.prefab`, `V/VFX/` | Onda ativa, acerto, reflexão e crítico; símbolos/contornos diferentes, geometria real preservada. |
| fx.enemy-warning | `P/Enemies/EnemyWarning.prefab`, EnemyAreaEffect | Preparação/área ativa/expiração; teia, investida, rastro e áreas dos minibosses distinguíveis, aviso nunca confundido com pickup. |
| fx.boss-hazard | `P/Enemies/FrogKingHazard.prefab`, `E/FrogKingSetup.cs` | Sinalização das fases reais/área perigosa; contorno por forma/ritmo e zona segura visualmente separada. |
| fx.boss-safe | Safe refuge em LakeRun e BossArena | Refúgio seguro sem parecer cura; preservar colliders/posições existentes. |
| fx.evolution | `P/UpgradeFeedback.prefab`, `E/UpgradeFeedbackSetup.cs` | Estrela/acento/legenda temporários, não escondem escolha seguinte. |
| art.pickup.xp | `P/ExperiencePickup.prefab` | XP no chão, atraído e coletado; gema proposta distinta da cura em silhueta, sem número obrigatório no sprite. |
| art.pickup.xp-rare | `P/RareExperiencePickup.prefab` | Recompensa rara/fixture: losango destacado e brilho, distinguível do XP comum além de roxo. Não introduzir drop novo. |
| art.pickup.heal | `P/HealingPickup.prefab`, `E/PickupPolicySetup.cs` | Cura comum/miniboss compartilham view; símbolo de cura legível, chão/atração/coleta/descarte. Não prometer segundo visual por valor sem integração explícita. |
| art.fixture.target | `P/Target.prefab`, arena Lake | Alvo de treino; preservar função distinta de espécie do catálogo. |
| art.fixture.projectile | `P/LightProjectile.prefab`, emissor em Lake | Projétil refletível de treino e emissor; manter leitura de perigo/direção. |
| art.fixture.breath | `P/BreathReward.prefab` | Recompensa de Fôlego da rota de risco; não confundir com cura/XP. |

## Passivas e relíquias — P3 / UI

Ícones finais individuais, perfil UI, origem A1/R1. Hoje a apresentação é textual/genérica, não há dez sprites de relíquias nem seis sprites de passivas próprios. Estados: disponível/selecionado/bloqueado/equipado/banido onde aplicável; rótulos continuam como texto localizado. Exportação `<ID>-icon.png`; fonte editável correspondente. Não fundir IDs de pena, amuleto e itens parecidos.

| ID canônico | Dados atuais | Símbolo proposto / distinção |
|---|---|---|
| pena-aerodinamica | `C/Passives/Prototype/pena-aerodinamica.asset` | Pena curva com linhas de cadência. |
| ninho-reforcado | mesmo diretório, ninho-reforcado.asset | Ninho trançado envolvendo ovo. |
| pulmao-poderoso | pulmao-poderoso.asset | Pulmões estilizados, reserva de Fôlego. |
| amuleto-lago | amuleto-lago.asset | Pingente com gota, recuperação. |
| oculos-aviador | oculos-aviador.asset | Óculos com duas lentes e tira. |
| sabao-magico | sabao-magico.asset | Barra de sabão e bolhas; prévia antes/depois ligada ao raio real. |
| apito-veloz | `C/Relics/Prototype/apito-veloz.asset` | Apito com furo e cordão. |
| botas-do-lago | mesmo diretório, botas-do-lago.asset | Par de botas de água. |
| anel-de-ondas | anel-de-ondas.asset | Anel com ondas gravadas. |
| concha-de-folego | concha-de-folego.asset | Concha estriada, distinta do caramujo inimigo. |
| garras-de-junco | garras-de-junco.asset | Feixe de junco em forma de garra. |
| lente-de-orvalho | lente-de-orvalho.asset | Lente única com gota, distinta de Óculos. |
| pena-de-combate | pena-de-combate.asset | Pena rígida com insígnia, distinta da passiva. |
| pena-leve | pena-leve.asset | Pena pequena flutuante, distinta da pena de combate. |
| pedra-da-margem | pedra-da-margem.asset | Pedra polida com marca de margem, ícone distinto de bloqueio no mundo. |
| perola-do-lago | perola-do-lago.asset | Pérola com brilho dentro de concha aberta. |

## HUD, menus e entrada — P3 / UI

Cada chave define família reutilizável, não uma textura por botão. Fonte atual A0/F0: Images/Text uGUI procedurais, geometrias e linhas. Hierarquias reais em Lake/LakeRun e prefabs listados. Perfil UI; R1/R2 para estilo, layout aprovado em T040 prevalece. Animação apenas de transição/foco, sem atrasar confirmação ou alterar contexto.

| ID documental | Vínculo atual | Exportações/estados e restrições |
|---|---|---|
| ui.resources | `V/UI/GameplayHud.cs`, `E/GameplayHudSetup.cs` | Moldura, preenchimento, ícone vida e ícone Fôlego; só duas barras sem números. Cheio/parcial/vazio, sinais de Quack além da cor. |
| ui.progress | Experience progress em Lake/LakeRun | Barra XP e nível central no rodapé y=12 de referência; último limiar cheio. Não restaurar tarja/status removidos na T040. |
| ui.run-actions | Run controls, relógio, `E/LakeRunSetup.cs` | Botões Relíquias, PAUSAR/CONTINUAR, Abandonar e Controles; relógio legível. Um único toggle de pausa. |
| ui.hints | `V/UI/ContextualHintsView.cs` | Fundo discreto e símbolos por entrada; dez segundos de simulação, sem repetição. |
| ui.special-bars | `V/UI/SpecialEnemyBarsView.cs` | Moldura/preenchimento/nome/tether por especial; não agrupar nem ocultar atores próximos. |
| ui.upgrade | `P/UpgradeSelection.prefab`, `E/UpgradeSelectionSetup.cs` | Carta/borda/banir/rerrolar/pular, ícones de seis armas/seis passivas/três evoluções; prévia de Sabão permanece funcional e focalizável. |
| ui.relics | `P/RelicLoadout.prefab`, `E/RelicSetup.cs` | Painel, slot vazio/equipado/bloqueado, dez ícones acima; limite de três e propriedade da pausa preservados. |
| ui.results | `P/ResultsScreen.prefab` | Painéis/scroll/foco e ações salvar/jogar novamente; estados vitória/derrota/abandono, salvar indisponível conforme estado real. |
| ui.controls | `P/ControlSettings.prefab`, `V/UI/ControlBindingsView.cs` | Perfis/remapeamento/restaurar; destacar item focalizado, estado inválido e retorno. |
| ui.input-symbols | `Platform/Input/ControlProfile.cs` e ControlBindingsView | Famílias Xbox/PlayStation/genérico e teclado: confirmar/retornar/pausar/Relíquias/navegar, conforme bindings reais. Genérico sem marca; ícone nunca é única explicação do binding. |
| ui.touch | `P/MobileControls.prefab` | Base/haste do joystick e botão Quack, pronto/indisponível/pressionado; ocultação por disponibilidade de controle e Safe Area preservadas. |
| ui.focus | Vistas de menus acima | Moldura contrastante/foco/pressionado/desabilitado compartilhados, com espessura/forma além de cor. Testar quatro proporções e recortes dos dois lados. |

## Ambiente — P4 / Environment

Fonte atual A0: Square/Circle tintados e objetos Ground/Shore/Water/Island/Water ripple em Lake e LakeRun, `E/LakeArenaSetup.cs`; refúgios/arena em LakeRun/BossArena. Perfil EN, exportações por chave, R1. Vegetação futura é somente decoração, sem espécie de inimigo nova.

| ID documental | Função / fonte e exportação propostas | Animação e contraste |
|---|---|---|
| env.ground | Solo/ilha, tiles ground/island | Estático, textura discreta; não parecer água ou barreira. |
| env.shore | Margem decorativa/areia, módulos shore | Curvas orgânicas sem mudar TerrainZone nem sugerir passagem por bloqueio. |
| env.water | Superfície water e material correspondente | Ondulação suave, alpha controlado, limite água/terra constante e ameaças legíveis. Implementação LakeWaterAnimation é T046. |
| env.ripples | Water ripple e detalhe de superfície | Movimento de baixo contraste; não imitar raio de dano/atração ou aviso de chefe. |
| env.vegetation | Juncos/folhas/flores decorativos | Base ancorada, oscilação leve opcional; não encobrir Pato/pickups/cantos nem criar colisão nova. |
| env.props | Pedras decorativas e troncos propostos | Estáticos; linguagem mais suave que OB, sem contorno fechado de barreira. Validar identificação sem explicação. |

## Recursos físicos atuais compartilhados

Esta lista cobre os arquivos visuais/sonoros próprios encontrados em `Assets/QuackQuack/Art/Prototype/` e o shader de feedback; não é catálogo de imagens finais. Scripts/cenas/prefabs são fontes de composição indicadas acima; definições de ondas, políticas e settings são dados, não assets visuais adicionais. Recursos de pacotes Unity não são copiados para a arte autoral.

- `Circle.png` e `Square.png`: 64×64 gerados por LakeArenaSetup, Sprite Single, PPU 64, pivô central, Bilinear, Clamp, mipmaps off, sRGB/alpha, textura máxima 2048; overrides de compressão de plataforma não habilitados. São bases compartilhadas: **não substituir seus bytes por um personagem**, pois afeta todos os consumidores.
- `Unlit.mat` e `CircleUnlit.mat`: material URP Sprite-Unlit para bases quadrada/circular. `EnemyLine.mat` e `QuackRing.mat`: material de linhas/feedback, shader `V/VFX/QuackRing.shader` (QuackQuack/FeedbackRing). Sem dimensão/pivô de sprite aplicável; preservar blend/transparência e instâncias compartilhadas até integração medida.
- Áudio procedural mono PCM 22050 Hz, gerado pelos setups de feedback/encontros: `Quack.wav`, `Impact.wav`, `Reflection.wav`, `Evolution.wav`, `RareReward.wav`, `EnemyWeb.wav`, `EnemyCharge.wav`, `EnemyTrail.wav`, `AlphaDive.wav`, `CururuArea.wav`. Chaves documentais `audio.<nome do arquivo sem extensão>`; origem A0; dimensão/pivô visual não aplicáveis. Permanecem temporários, sem substituição sonora autorizada pela T041. Preservar sincronização dos eventos e não depender apenas de som para avisar perigo.
- Fonte F0 e shader acima são dependências compartilhadas, não assets finais de personagem. Não foram encontrados sheets `.anim`/`.controller` autorais nesta inspeção; animações atuais usam código/linhas/transforms. Não declarar que o projeto já possui ciclos de locomoção cartoon.

## Gate da amostra e manutenção do manifesto

T042 deve compor uma cena-amostra com Pato, uma ameaça **do catálogo** (proposta: Formiga), um bloqueio/rocha, efeito Quack, XP/cura e HUD no layout atual. Mostrar terra/água como contexto; a produção completa do cenário continua P4. Registrar aqui a imagem da amostra, fonte/exportação/GUID/importação efetivos, responsável/origem/licença e resposta explícita do usuário. A proposta de Formiga ainda não é aprovação de desenho.

Antes de cada lote, substituir perfis propostos por medidas reais por asset, preencher origem/licença, estados entregues e evidência de aceite. Preservar `.meta` e referências existentes; gerar serializados pelo Unity/PolishArtSetup. Separar visual filho de raiz física; não ajustar hitbox/raio para fazer arte caber. Armas, pickups, VFX e personagens pooled devem resetar animação/tint/escala na reutilização e congelar conforme o relógio de simulação.

Revisão final T047: reconhecer jogador, ameaça, cura, XP e bloqueio sem explicação; verificar seis armas/três evoluções além da cor; comparar silhuetas em cinza, água/terra, horda 200/400 e sobreposição com especiais; testar quatro proporções, dois recortes, foco e touch. Aceite artístico, gameplay e desempenho são registros separados, nenhum deles inferido deste documento.

## Verificação da T041

Inspeção estática de Content, prefabs, cenas, geradores, importadores e referência R1; reconciliação dos IDs com os assets versionados: um Pato, cinco comuns, dois minibosses, um chefe, seis armas, três resultados/três receitas, seis passivas e dez relíquias. Conferir também fixtures, recursos compartilhados, HUD e ambiente acima. Checklist de requisitos 11/11, sem alterar seus marcadores. Nenhuma execução Unity/build/teste de gameplay é necessária para esta entrega documental; sem alegação de aceite visual final ou desempenho. T042–T047 permanecem pendentes.

## Amostra T042 — 09/11/2026

Estado: cena-amostra implementada, **aceite artístico do usuário pendente**. Base `2847642`; somente T042. Não inicia lotes T043–T046 nem substitui prefabs/cenas da partida. Cena estática de exposição, sem IA, colliders, coleta, animação ou botões funcionais; o HUD é uma composição representativa da linguagem visual, não uma instância do fluxo de gameplay. Valores das barras/nível/relógio são exemplos visuais.

Cena: `Assets/QuackQuack/Art/LakeCartoon/Sample/LakeCartoonSample.unity`. Gerador: `Assets/QuackQuack/Editor/PolishArtSetup.cs`, menu **QuackQuack → Art → Build T042 cartoon sample**. Abrir a cena no Unity permite inspecionar os elementos; regeneração abre somente essa cena, portanto salvar qualquer outra cena aberta antes de usar o menu. Não é adicionada ao pipeline de build do produto.

### Entrega e proveniência

Fonte/exportação: `Assets/QuackQuack/Art/LakeCartoon/Sources/sample-atlas.png`, RGBA 1536×1024 com seis células 512×512, alpha preservado. SHA-256 `E19F869C43EEF25B359E1B96912A0F5EB29FE742741CE285D1CD7BD1CBB99F54`. Geração assistida por IA com ferramenta integrada image_gen em 09/11/2026, referência R1; prompts e restrições em [art-source-prompts.md](art-source-prompts.md). Fonte achatada disponível, sem arquivo em camadas. Não há licença pública/CC0 atribuída; nenhuma biblioteca externa de sprites integrada.

Importação efetiva da amostra: Sprite Multiple, seis slices centrais, PPU 128, Bilinear, Clamp, mipmaps/ReadWrite off, alpha transparency, sem compressão, máximo 2048. `.meta`, recortes e cena produzidos pelo Unity 6000.4.3f1 na cópia completa isolada `Temp/T042Validation`, sem copiar Library. Esses parâmetros pertencem à amostra; Android ASTC e perfis finais continuam propostas da T041.

| ID do manifesto | Slice / tamanho do quadro / pivô | Estado e tamanho autorados na amostra | Ordenação |
|---|---|---|---|
| pato | pato / 512×512 / 0,5;0,5 | Pose única vocalizando; quadro 1,5 unidades de altura (inclui margem transparente) | 5 |
| formiga | formiga / 512×512 / 0,5;0,5 | Pose única; cabeça/tórax/abdome separados, seis pernas e antenas; quadro 1,25 | 10 |
| art.obstacle.rocks | rock / 512×512 / 0,5;0,5 | Rocha única; quadro 2,1; exposição sem collider | 1 |
| art.pickup.xp | xp / 512×512 / 0,5;0,5 | Gema ciana, quadro 0,48; sem coleta | 12 |
| art.pickup.heal | heal / 512×512 / 0,5;0,5 | Folha verde com símbolo de cura, quadro 0,6; sem coleta | 12 |
| fx.quack-active | quack / 512×512 / 0,5;0,5 | Arcos internos, quadro 2,4; anel separado de raio 4 obtido de PatoQuack.asset, indica alcance circular nominal | 14 / anel 13 |
| ui.resources / ui.progress / ui.run-actions | uGUI + ResourceIconGraphic + LegacyRuntime.ttf | Duas barras sem números, coração/ondas distintos; XP/nível central a 12 unidades do rodapé, Relíquias/relógio/Pausar. Sem tarja de status; rótulo de exposição fora do HUD de produto | Canvas ScreenSpaceCamera |

Solo/água são blocos de contexto com cores e linhas discretas, não cenário final da T046. A amostra mostra diferenças por silhueta: Pato creme/bico, formiga segmentada, rocha, gema e folha com símbolo. Os arcos são acento gráfico: **não transformam o Quack ativo circular em ataque frontal**. A integração animada, reset pooled, skin de todos os estados e validação de alcance no combate continuam nas tasks de lote.

### Verificação executada e limites

Checklist de requisitos 11/11. Compilação e geração pelo Unity concluídas com marcador `T042_ART_SAMPLE_OK sprites=6 range=4`. A importação inicial teve erros transitórios de assemblies de pacotes e falha Burst JIT ao carregar DLL (erro Windows 4551); o gerador ainda executou. Reexecução com `--burst-disable-compilation` apenas no processo de validação confirmou geração sem esses erros, sem alterar pacotes/PlayerSettings. Logs em `Logs/Polimento002/T042/`; não versionados.

Quatro capturas de câmera, `sample-16x9.png` (1920×1080), `sample-18x9.png` (2160×1080), `sample-19_5x9.png` (2340×1080) e `sample-4x3.png` (1440×1080), inspecionadas. Correção R1 amplia somente o bloco de terra do contexto para não terminar na borda ultralarga. Isso é inspeção da amostra estática, sem aceite de Safe Area física, gameplay, animação, Android ou desempenho; suítes de gameplay não foram reexecutadas porque produção não foi alterada. Capturas regeneráveis permanecem locais no diretório de logs.

Aceite solicitado: reconhecer Pato/formiga/rocha/XP/cura e aprovar traço, paleta, proporções e linguagem das barras para orientar os próximos lotes. Registrar aqui a resposta explícita antes de marcar T042 concluída. Não inferir aprovação pela solicitação de implementação.

### Revisão solicitada pelo usuário — R2

Em 09/11/2026, o usuário apontou que os arcos não são compatíveis com o Quack ativo, mas servem ao Quack frontal, e pediu outras possibilidades de cura em vez da folha com cruz e de XP em vez do diamante. A amostra **não recebeu aceite final**. O trecho/tabela anterior descreve a R1, substituída neste ponto pela R2: os arcos deixam a cena do Quack ativo, o slice passa a chamar `quack-frontal`, e o Quack ativo permanece representado pela onda circular de raio nominal autorado. O desenho frontal fica como candidato da arma para futura integração, não como alteração de sua regra.

Novos pickups serão escolhidos em prancha comparativa: C1 coração, C2 poção com coração, C3 maçã, C4 bandagem com coração; X1 pena dourada, X2 estrela, X3 esfera/gota lisa, X4 ficha com pegada de pato. Não aplicar nenhuma alternativa ao gameplay nem interpretar a geração como escolha. A folha e o diamante da cena ficam provisórios até a seleção; não são aprovados pelo usuário. T042 continua aberta e T043 não iniciada.

Prancha durável: [opções C1–C4 e X1–X4](references/t042-pickup-options-r2.png), gerada com image_gen e inspecionada. Não contém folha com cruz nem gema facetada; C3 tem somente a pequena folha da maçã. A cena R2 foi regenerada no Unity com código 0, seis slices e raio 4, sem exceções/erros C# no log `Logs/Polimento002/T042/review-r2.log`; captura 16:9 inspecionada sem os arcos frontais. Mudança limitada à exposição, nenhum efeito de produção alterado.

### Seleção provisória do usuário — R3

Em 09/11/2026: “provisoriamente utilize o c2 para cura e o x3 para o xp, se possível apenas retire a estrela amarela dentro da esfera azul.” Aplicado à cena-amostra: cura é a poção rosa com coração C2; XP é a esfera azul lisa X3, sem estrela amarela ou outro símbolo interno. Escolha provisória não equivale ao aceite final de toda a T042.

Novo atlas `Assets/QuackQuack/Art/LakeCartoon/Sources/pickups-c2-x3.png`, PNG RGBA 1774×887, dois quadros 887×887 com pivô central, slices `heal` e `xp`. SHA-256 `63910AA78A852BBBB3070FDEC6FF7310FE1268B735B6FB4BA82A11AFACA1CC8E`. Origem/prompts em [art-source-prompts.md](art-source-prompts.md). Alpha da geração preservado; contorno e reflexos brancos mantidos, amarelo removido da esfera. Importação segue os parâmetros da amostra (Multiple, PPU 128, Bilinear, Clamp, sem mipmaps/ReadWrite/compressão). Tamanhos mundiais da exposição continuam 0,6 para cura e 0,48 para XP, incluindo margem do quadro; nenhuma mudança na regra ou raio de coleta.

O gerador agora usa esses dois slices no lugar dos pickups do atlas R1; este permanece como fonte histórica compartilhada dos demais elementos, sem substituir seus bytes. Metas e cena regeneradas no Unity, log `Logs/Polimento002/T042/review-r3.log`: código 0, `T042_ART_SAMPLE_OK sprites=6 range=4`, sem erros C#/exceções. Quatro capturas regeneradas, 16:9 inspecionada com poção e esfera sem estrela. Sem alteração dos prefabs da partida ou aceite de gameplay/desempenho. T042 permanece em revisão artística.

### Aceite da T042 e autorização do lote

Em 09/11/2026, ao confirmar o gate antes da T043, o usuário respondeu **“Aprovar e iniciar T043”** à aprovação da direção atual: Quack circular, arcos para o frontal e C2/X3 sem estrela provisórios. T042 concluída; estados anteriores de revisão acima são históricos. Essa aprovação permite o lote de personagens/ameaças/obstáculos da T043, sem tornar definitivas as escolhas de pickups nem aprovar por inferência gameplay, plataformas, animações finais ou desempenho.

## Lote T043 — personagens, ameaças e obstáculos

09/11/2026, base `e5c5baf` + revisão `d8003bf` já incorporada; direção T042 aprovada explicitamente antes do lote. Gerador parcial `Editor/PolishArtCharacters.cs`, integrado a `PolishArtSetup`, menu **QuackQuack → Art → Integrate T043 characters and obstacles**. Salvar cenas abertas antes de usar: o comando abre/salva Lake, LakeRun e FrogKingPractice, terminando na galeria `Art/LakeCartoon/Sample/LakeCartoonCast.unity`. Não adiciona cenas ao pipeline de produto.

### Fontes, dimensões e importação efetivas

Pato e formiga reutilizam `Art/LakeCartoon/Sources/sample-atlas.png`, quadros 512×512, origem/prompts T042. Demais sete ameaças e duas rochas: `Art/LakeCartoon/Characters/lake-threats-atlas.png`, PNG RGBA 1254×1254, grade 3×3, quadros 418×418. SHA-256 `CD3AE886C99B4AE68356E1AC088008D452AA87F910A9560369E668862A499BF7`. Arte assistida por IA via image_gen integrado, referência da amostra aceita; sem pacote de arte externo e sem licença pública/CC0 atribuída. Fonte disponível achatada, sem camadas. [Prompts, revisões e proveniência](art-t043-prompts.md). Rascunhos com anatomia/halos/contaminação entre células foram descartados.

Sprite Multiple, PPU 128, pivô central, Bilinear/Clamp, alpha transparency, sem mipmaps/ReadWrite/compressão, máximo 2048. Recortes e `.meta` pelo Unity 6000.4.3f1. Materiais Sprite-Unlit separados por atlas (`Characters/sample-atlas.mat` e `Characters/lake-threats-atlas.mat`) impedem associação de uma textura a recortes de outra. Não sobrescrever Circle/Square compartilhados. Overrides Android/atlas packing continuam para revisão medida, não foram aplicados.

| ID | Slice / vínculo autorado | Altura de quadro na integração / ordem | Identidade |
|---|---|---|---|
| pato | pato; filho Cartoon actor nas cenas Lake/LakeRun/FrogKingPractice | 1,5 / 5 | Pato creme, bico e pés laranja, asa/olhos expressivos |
| formiga | formiga; prefab de mesmo ID em Prefabs/Prototype/Enemies | 1,15 / 10 | Segmentos e antenas; sprite espelhado para alinhar à direção +X usada pelo scheduler |
| aranha | aranha; mesmo padrão de prefab | 1,15 / 10 | Oito pernas, dois volumes corporais, sem antenas |
| vespa | vespa | 1,15 / 10 | Listras, asas e ferrão |
| besouro | besouro | 1,15 / 10 | Carapaça/frente protegida, indicador frontal original preservado |
| caramujo-aquatico | caramujo-aquatico | 1,15 / 10 | Concha espiral e corpo deslizante |
| libelula-alfa | libelula-alfa | 1,7 / 10 | Quatro asas e abdome comprido, maior que os comuns |
| sapo-cururu | sapo-cururu | 1,7 / 10 | Pele verrugosa e bochechas infladas, sem coroa |
| sapo-rei | sapo-rei; prefab existente do chefe | 2,5 / 10 | Sapo régio com coroa, distinto do Cururu |
| art.obstacle.rocks | rock-wide; Objects/rock-wide.prefab | Encaixe na dimensão do collider existente / 1 | Rocha baixa/larga; treino e pedra sul |
| art.obstacle.banks | rock-tall; Objects/rock-tall.prefab | Encaixe no collider existente / 1 | Acento rochoso nos bloqueios de margem/corredor |

Altura inclui a margem transparente do quadro, não significa novo diâmetro físico. `Objects/` contém os dois prefabs visuais sem collider próprio; os slices estão no atlas compartilhado de Characters. O gerador aplica os visuais nos bloqueios existentes sem substituir seus BoxCollider2D; mantém uma base cinza cobrindo toda a pegada física para não criar colisão invisível nas margens transparentes da rocha. Margens orgânicas/vegetação/água continuam T046.

### Estados e animações entregues

`Gameplay/Player/CartoonActorVisual.cs` fica somente no filho visual, sem Rigidbody/Collider. Pose raster única por personagem, com animação procedural suave de escala: respiração idle, movimento terrestre e variante de nado do Pato; inversão horizontal do Pato pelo deslocamento. As ameaças acompanham a rotação real da raiz produzida pelo scheduler; preserva o sentido +X e o escudo frontal do Besouro. Dano usa a queda da vida real para tint temporário de 0,14 s. Não há novas regras de dano.

O relógio visual só avança quando o ator informa simulação ativa, por propriedades readonly que leem os RunState atuais. Pausa/escolha/fim congelam escala/tint. Desativação/reuso limpa fase, flash, direção e escala; leases novos de inimigo também reiniciam a amostragem. Sem criação/destruição de objetos, consultas de cena, arrays ou LINQ no loop visual. Há um LateUpdate por ator: seu custo em 200/400 e Android ainda precisa de medição T049, sem promessa de FPS nesta task.

Preparação/ataque/fases do chefe mantêm os telegráficos e linhas originais; o sprite usa a mesma pose com movimento/reação. Derrota de comum/miniboss segue a remoção pelo pool, sem clipe de morte novo; chefe segue o resultado original. Nenhum sheet de caminhada/ataque foi produzido, nem é declarado como entregue. Rochas são estáticas. Armas, pickups C2/X3 e HUD de produção não são alterados neste lote.

### Integração e validação

Prefabs existentes dos oito inimigos atualizados em lugar, preservando GUIDs, componentes de combate e os LineRenderer de direção/telegráfico. Renderers geométricos dos corpos antigos ficam desabilitados; filho Cartoon actor recebe a arte. Player integrado às três cenas reais, incluindo FrogKingPractice (o nome anterior BossArena no inventário era descritivo, não um arquivo existente). O gerador compara assinaturas de transforms físicos, Collider2D e Rigidbody2D antes/depois de cada mudança e falha se houver diferença. Conteúdo/balanceamento/cronograma permanecem nos assets anteriores.

Validação em cópia completa isolada `Temp/T043Validation`, sem copiar Library: 714/714 EditMode e 596/596 PlayMode, zero falhas/ignorados, incluindo três casos novos de animação sem deslocar física, pausa/retomada, flash de dano e reset/desativação/reuso. XMLs locais em `TestResults/Polimento002/T043`, logs em `Logs/Polimento002/T043`. As suítes precedem somente a limpeza final de pixels/recortes e a manutenção da base visual dos obstáculos; esses ajustes não alteraram o código runtime testado. A primeira geração interrompeu por caminho de cena incorreto; corrigido para FrogKingPractice antes da entrega. Erros transitórios de assemblies dos pacotes na importação inicial não se repetiram nas suítes.

Galeria e captura editor da LakeRun são evidências estáticas, não playtest nativo. Identidade artística e leitura de alcance em combate serão revisadas em T047; builds, aparelhos, profiling e rodada de participantes seguem T048–T051. Não extrapolar os testes para aceite físico/Android ou desempenho. Nenhuma task T044+ executada.

Geração final aprovada com código 0: `T043_CHARACTERS_OK actors=9 threats=8 geometry=unchanged` e `T073_CONTENT_VALIDATION_OK assets=66 warnings=2`, log `Logs/Polimento002/T043/final-generate.log`. Galeria final `sample-cast.png` e render editor `sample-lake-scene.png` nesse diretório inspecionados: sprites corretos por atlas, sem halos/quadrados contaminados ou invasão entre células; Pato visível na composição real. O render da LakeRun não inclui HUD ScreenSpaceOverlay e não representa uma partida em execução. T043 concluída como entrega de lote/integracão; o aceite global continua separado.

## Lote T044 — combate e recompensas

09/11/2026, base `643dbb9`. Somente T044. Gerador `Editor/PolishArtCombat.cs`, parcial de PolishArtSetup, menu **QuackQuack → Art → Integrate T044 combat and pickups**. Executar após geradores de conteúdo base; estes produzem a representação provisória anterior. O comando atualiza somente sprite/material das nove WeaponDefinition e apresentação dos quatro prefabs de recompensa; abre a galeria `Art/LakeCartoon/Sample/LakeCartoonCombat.unity`, sem adicioná-la ao pipeline de produto. Salvar qualquer cena aberta antes de usar o menu.

### Origem e escolha da bolha

[Prompts/fontes](art-t044-prompts.md): geração assistida por IA com image_gen integrado, referência da amostra aceita, PNGs achatados sem camadas. Sem pacote externo ou licença pública/CC0 atribuída. Durante a implementação, o usuário escolheu a bolha iridescente do rascunho, preservado em [references/t044-bubble-preferred.png](references/t044-bubble-preferred.png). A fonte final mantém aro ciano com trechos perolados rosa/lavanda e reflexos brancos, distinta do XP sólido. Não usar a alternativa plana como bolha final. O centro transparente deixa ver os atores/terreno; irregularidades finas do reflexo permanecem na textura, não alteram o raio de colisão.

Arquivos sob `Assets/QuackQuack/Art/LakeCartoon/Combat/`:

| Fonte PNG | Dimensão / SHA-256 | Uso efetivo |
|---|---|---|
| weapons-atlas.png | 1254×1254; `53F9B395B3D29CDC5710097F9EBB5F7C1F980379A90B899045B05003DDC0AAF1` | Sete armas/evoluções. Células antigas de aura/bolha não são atribuídas, substituídas pelas fontes abaixo. |
| circles-atlas.png | 1774×887; `D65291BBAE231C6B4872E60B6787986533843229343085889B7024B85132A3D8` | Somente aura. A bolha plana desse estudo não é atribuída. |
| bubble-iridescent.png | 1254×1254; `6116FFC709D195E6C9BD95C35D73C42515368FB84E42CD11054B7C8112699EA4` | Bolha escolhida pelo usuário, fonte independente. |
| pickups-atlas.png | 1774×887; mesmos bytes/SHA `63910AA78A852BBBB3070FDEC6FF7310FE1268B735B6FB4BA82A11AFACA1CC8E` da fonte C2/X3 | Cura/XP provisórios; GUID/recortes próprios, sem modificar a amostra T042. |

Importação: Sprite Multiple, PPU 128, pivô central, alpha transparency, Bilinear/Clamp, sem mipmaps/ReadWrite/compressão, máximo 2048. O gerador calcula retângulos dos pixels visíveis dentro das células, preservando o PNG original; círculos recebem retângulo quadrado. Importação única por atlas evita que uma reimportação de células inteiras anule o recorte. Quatro materiais Sprite-Unlit, um por textura, nome-base do respectivo PNG. Não usar um material de atlas para sprites de outra textura. `.meta` e serializados produzidos pelo Unity 6000.4.3f1.

| ID | Recorte final / forma | Estado e animação entregues |
|---|---|---|
| bumerangue | 345×326; pena curva dourada | Giro por idade de simulação, ida/volta e histórico por passagem preservados. |
| ovos | 283×339; casca creme salpicada | Armadilha estática; detonação com círculo laranja no raio real, sem expansão enganosa. |
| quack-frontal | 303×362; três arcos cianos | Decoração dentro do setor real; borda geométrica mantém alcance/ângulo exatos. |
| aura-energia | 802×802; anel recortado de interior ondulado | Escala = diâmetro do raio efetivo; opacidade pulsa sem mudar borda, segue jogador, permanece translúcida. |
| foguetes | 409×268; corpo com ponta/aletas/exaustão | Orientado pela direção homing; explosão com contorno serrilhado dentro do raio real. |
| bolhas | 985×985; aro iridescente | Diâmetro visual = 2×HitRadius efetivo, compensando sprite/hierarquia, inclusive Sabão N0/N1/N5. Centro transparente. |
| tornado-penas | 316×355; espiral de penas | Projéteis de ida/volta com giro; sem introduzir dano contínuo de aura. |
| incubadora-combate | 331×349; ovo em suporte mecânico | Armadilha distinta; detonação indica o raio efetivo maior da evolução. |
| quack-supersonico | 317×372; arcos serrilhados | Pulso frontal mais intenso por forma, mantendo setor real e duração da arma. |
| art.pickup.heal | 526×705; poção rosa C2 | Prefab HealingPickup; representação estática no chão/atração, removida pelo ciclo lógico existente. |
| art.pickup.xp | 538×552; esfera azul X3 sem estrela | Prefab ExperiencePickup; estados lógicos/coleta/atração inalterados. |
| art.pickup.xp-rare | Esfera X3 + esfera satélite por composição | Prefab RareExperiencePickup: diferença de silhueta, não apenas cor; satélite sem collider/recompensa próprios. |
| art.fixture.breath | Pena dourada do bumerangue | Prefab BreathReward de treino, estática; distinta de poção/esfera, sem alterar recompensa. |

Sprites do mundo na ordem 14 para projéteis; aura continua -1; arte frontal 13 atrás da borda 14; recompensas 12. Nomes dos arquivos de dados, seis armas, três receitas e três resultados evolutivos preservados. C2/X3 continuam provisórios; esta integração não os declara definitivos.

### Materiais, raio e ciclo de vida

WeaponDefinition ganhou apenas referência opcional `projectileMaterial`, sem parâmetro de dano/balanceamento novo. Fixtures legadas sem material continuam com fallback. Comparação das nove definições antes/depois, excluindo sprite/material, confirmou texto de dados idêntico. Não alterar Sabão para a proposta futura de tuning; vigente 16%/nível, teto 80%.

WeaponProjectile usa o material autorado, tamanho de combate e orientação/giro pelo relógio do runner. Reset devolve sprite/material/cor/escala/rotação iniciais, sem criar material por emissão. A bolha mantém diâmetro mundial nos dois eixos; outros projéteis usam silhueta proporcional dentro do tamanho nominal, sem alterar CircleCast, velocidade, alvo, duração, dano ou histórico de acertos.

WeaponVisuals mantém views/arrays limitados criados no Initialize. Arcos decorativos são encaixados dentro do setor, sem parecer uma onda radial do Quack ativo. A borda do setor permanece geométrica e exata. A explosão de foguete, antes sem apresentação de área, usa a mesma resolução de raio já usada pelo dano, com recortes para dentro do contorno; ovos preservam o círculo. Aura não muda raio durante pulso. Advance só é chamado pelo relógio de simulação do runner; pausa congela e HideAll/fim desativa também a arte frontal. Sem Instantiate/Destroy, leitura de arquivos ou carregamento de textura/material no loop de combate.

Quack ativo conserva o anel radial aceito; impacto/reflexão/crítico e feedback pooled de evolução/raridade conservam suas geometrias vetoriais já distintas (anel, contorno/estrela/acento) e ciclo de áudio. Não substituir Quack ativo pelos arcos. Os prefabs de recompensa conservam transforms físicos, colliders, scripts, GUIDs e valores: apenas renderers antigos desabilitados e filhos visuais sem física adicionados. O gerador compara assinaturas físicas antes/depois e falha em diferença.

### Evidências e limites

Geração final `Logs/Polimento002/T044/generate-trim.log`, código 0: `T044_COMBAT_OK weapons=9 pickups=4 geometry=unchanged` e `T073_CONTENT_VALIDATION_OK assets=66 warnings=2` (avisos existentes). Galeria `sample-combat.png` desse diretório na cópia isolada inspecionada com a bolha iridescente e recortes finais; não é captura de gameplay. Logs/XML/capturas regeneráveis ficam fora do commit.

EditMode inicial: 713 aprovados/uma falha na asserção de três renderers da cruz provisória. Asserção atualizada para uma poção visível, sprite e material corretos; invariantes de chance/cura/collider/capacidade preservadas. Reteste completo 714/714. Primeiro PlayMode: 599/600, por exigir escala unitária no bumerangue apesar do novo tamanho autorado. Teste ajustado para conferir o raio/tamanho não ampliados por Sabão em N0/N1/N5, preservando alvo fora do alcance. Quatro testes novos verificam arte frontal dentro do setor/tempo de simulação, raio/material de aura em hierarquia não unitária, explosão distinta sem ultrapassar raio, reset/material/diâmetro de Bolhas.

Reteste final PlayMode: **602/602 aprovados**, zero falhas/ignorados, incluindo os três níveis do caso de não ampliação do bumerangue por Sabão. Artefatos locais em `TestResults/Polimento002/T044`: `editmode-final.xml` SHA-256 `443075622A7DA7749BAB147EEC0BC8FA27598A2DDD21A8490FE1301F550D8516`; `playmode-final.xml` SHA-256 `3246F4F432770181A93CADEA00E0730F168A36355713361F441465E3A074445A`. EditMode completo precedeu somente a finalização dos recortes/bolha; PlayMode final usou os assets finais. A importação inicial teve erros transitórios de assemblies de pacotes; a geração final e os retestes concluíram sem esses erros. `--burst-disable-compilation` foi usado apenas nos processos de validação, sem alterar configurações/pacotes do produto.

T044 concluída como entrega do lote integrado. Nenhum aceite físico Windows/Android, desempenho 200/400, identificação por participantes ou aceite artístico global é inferido; seguem T047–T051. Skin do HUD/menus e cenário/água continuam T045/T046.
