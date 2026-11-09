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
