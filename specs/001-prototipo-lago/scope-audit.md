# T078 — auditoria de escopo do protótipo

**Data:** 29/10/2026, America/Sao_Paulo. **Base auditada:** `e2166e3`.
**Resultado:** escopo estático conforme FR-020 e princípio V da constituição.
Não foi encontrada implementação de loja, anúncio, compra, bioma ou personagem
extra; nenhuma remoção de código, dependência, asset ou `.meta` foi necessária.
Isto não encerra a T075 nem concede aceite de gameplay, UI, plataforma ou desempenho.

## Critério e método

Fontes: [especificação](spec.md), [plano](plan.md), [modelo de dados](data-model.md),
[contratos](contracts/gameplay-contracts.md) e
[constituição](../../.specify/memory/constitution.md).
O protótipo permite somente Lago/Pato, seis armas básicas, seis passivas, três
evoluções, cinco inimigos comuns, dois minibosses, Sapo-Rei e dez relíquias.

Foram examinados Assets, Packages e ProjectSettings versionados: fontes C#,
assemblies, rótulos/referências serializados, catálogos, Resources, seis cenas,
manifesto, lockfile e pipeline de build. Foram resolvidos os GUIDs dos `.meta`
para identificar tipos/IDs e seguir referências a partir de RunBootstrap,
LakeRun e Resources, sem editar YAML. Não se inferiu conformidade apenas por
ausência de palavras em uma busca.

Buscas incluíram loja/shop/store, anúncios/ads/advertisement/rewarded/interstitial,
compra/purchase/IAP/billing/checkout/price, SDKs conhecidos, bioma/biome e
infinite/endless/infinito. Também foram examinados carregamentos de cenas,
Resources e possíveis caminhos de download/código externo no runtime autoral.
Documentação de origem e restrições sobre funcionalidades futuras não são
funcionalidades disponíveis no executável. Caches, logs, builds e cópias em Temp
não fazem parte do conteúdo de produto auditado.

## Conteúdo autoral encontrado

O inventário físico identificou 58 arquivos `.asset` em Assets; contagem de arquivos
não equivale à de ScriptableObjects/subassets examinados pelo validador Unity.

| Categoria | Quantidade e identidade | Evidência |
|---|---|---|
| Personagem | 1: `pato` | Content/Characters/Pato.asset; CharacterDefinition.IsValid rejeita outro ID e IsValidSelection exige exatamente um Pato |
| Bioma/arena | 1: Lago, ID `lake` nos nove encontros | Content/Encounters/Prototype; volumes de água/terra pertencem ao mesmo bioma |
| Armas básicas | 6: bumerangue, ovos, quack-frontal, aura-energia, foguetes, bolhas | PrototypeBuildCatalog.asset, lista weapons |
| Passivas | 6: pena-aerodinamica, ninho-reforcado, pulmao-poderoso, amuleto-lago, oculos-aviador, sabao-magico | Mesmo catálogo, lista passives |
| Evoluções | 3: evolucao-bumerangue, evolucao-ovos, evolucao-quack-frontal | Mesmo catálogo, lista evolutions e referências base/passiva/resultado |
| Armas resultantes | 3: tornado-penas, incubadora-combate, quack-supersonico | WeaponDefinition referenciadas pelas receitas; não são três aquisições básicas extras |
| Inimigos comuns | 5: formiga, aranha, vespa, besouro, caramujo-aquatico | LakeEncounterCatalog.asset, commonEnemies |
| Minibosses | 2: libelula-alfa aos 300 s e sapo-cururu aos 600 s | miniBosses e miniBossEncounters |
| Chefe | Sapo-Rei solicitado aos 900 s | encounter-sapo-rei.asset e LakeRunSettings.BossPrefab/BossSettings; controlador separado das sete EnemyDefinition |
| Relíquias | 10: botas-do-lago, garras-de-junco, pena-de-combate, pedra-da-margem, perola-do-lago, apito-veloz, lente-de-orvalho, anel-de-ondas, concha-de-folego, pena-leve | PrototypeRelicCatalog.asset; loadout limitado a três |

Todas as definições dos grupos personagem, armas, passivas, evoluções, inimigos e
relíquias acima são alcançáveis no grafo serializado das cenas de produto/Resources;
não foram encontrados assets autorais adicionais desses tipos fora dos catálogos.
Girinos do chefe, alvos neutros, projéteis, pickups e VFX são elementos de combate
ou fixtures, sem seleção de outro personagem jogável.

As seis ondas cobrem [0,900), seguidas pelo encontro final. RunDirector interrompe
ondas comuns ao entrar no chefe e encerra por vitória, morte ou abandono. O combate
com o chefe pode ultrapassar o minuto 15; isso não constitui um modo infinito.
ResultsBridge reinicia RunBootstrap após o resultado, com outra partida do Lago.

## Cenas e exposição no produto

| Cena | Papel | Pipeline de produto T070 |
|---|---|---|
| RunBootstrap | Inicialização da partida, seed 42, destino LakeRun | Incluída como entrada |
| LakeRun | Partida integrada do Lago | Incluída |
| Bootstrap | Inicialização do treino do Lago | Excluída |
| Lake | Arena curta de treino do mesmo Lago/Pato | Excluída |
| FrogKingPractice | Fixture de treino do mesmo chefe/Pato | Excluída |
| PerformanceBenchmark | Carga técnica determinística, seed 71 | Excluída; helper de profiling separado |

ProjectSettings/EditorBuildSettings mantém RunBootstrap, Bootstrap, Lake e LakeRun
habilitadas para trabalho/testes no Editor. PrototypeBuildPipeline.CreateOptions
define sua própria lista, somente RunBootstrap/LakeRun, para os dois alvos; não
depende da lista global. Treino/benchmark não são biomas extras e foram preservados.
Build manual fora desse pipeline exige conferir sua própria lista de cenas.

Os carregamentos no runtime autoral são o destino serializado de GameBootstrap
(LakeRun na entrada produtiva) e RunBootstrap no reinício. Resources contém a
configuração PlayerInput e seus vínculos de entrada/controles móveis, sem catálogo
adicional de personagens, loja ou biomas. Não foram encontrados carregamentos
remotos por UnityWebRequest/HttpClient/WebClient, AssetBundle/Addressables,
Application.OpenURL ou Assembly.Load nos diretórios de runtime examinados.

## Monetização, dependências e ocorrências descartadas

| Verificação | Resultado e interpretação |
|---|---|
| Manifesto/lockfile | 15 dependências diretas e 31 resolvidas; nenhum pacote de Ads, Purchasing, billing ou SDK de monetização |
| Plugins próprios | Nenhum DLL, AAR, JAR, SO, DEX, Java/Kotlin, manifesto XML ou Gradle encontrado em Assets/Packages; pacotes Unity resolvidos são tratados pelo lockfile |
| UnityConnectSettings | UnityAdsSettings.m_Enabled=0 e UnityPurchasingSettings.m_Enabled=0; não há inicialização de SDK desses serviços no código autoral |
| “App Store” em ProjectSettings | Nome de slot nativo de ícone iOS, com lista de texturas vazia; não é loja no jogo nem alvo adicional entregue pelo pipeline |
| “Windows Store Apps” em QualitySettings | Chave de qualidade de plataforma do Unity, não fluxo de venda |
| “banner”/androidEnableBanner | Campo nativo do PlayerSettings para imagem de banner Android; referência de imagem nula, sem componente/SDK de anúncio |
| Moedas, carteira, recompensas e relíquias | RunResult/RunProgression/JsonSaveService registram recompensa local e progresso; não foi encontrado fluxo de preço, compra, cobrança, gasto em loja ou venda de poder |
| Testes/geradores/documentos | IDs inválidos criados por fixtures, comandos de manutenção e menções a conteúdo futuro não disponibilizam esse conteúdo ao jogador |

Os campos nativos foram preservados, assim como os dados de progressão previstos
no escopo. Remover esses itens com base na palavra “store”, “banner” ou “moedas”
não removeria monetização e poderia alterar configuração legítima da engine/jogo.
Burst/Collections transitivos de pacotes Unity não são SDKs comerciais de gameplay;
esta tarefa não adicionou tecnologia de simulação ou dependência.

## Validação e limites

| Execução nesta tarefa | Resultado | Evidência local ignorada |
|---|---|---|
| Inventário de tipos/IDs e resolução de GUIDs | 1 personagem, 9 armas (6+3), 6 passivas, 3 evoluções, 7 inimigos, 10 relíquias; nove encontros com arenaId `lake` | TestResults/t078-static-inventory.json e leitura das referências |
| EditMode em cópia nova | 465/465 aprovados; zero falhas/ignorados | TestResults/t078-editmode-clean.xml; Logs/t078-editmode-clean.log |
| Validador Unity | 63 objetos, zero erros, dois avisos URP UnsupportedType | Logs/t078-content-validation-clean.log; T073_CONTENT_VALIDATION_OK |

A primeira tentativa em Temp/T033Validation falhou na compilação porque seu
manifesto local já não correspondia ao versionado e não tinha Input System.
Essa tentativa foi descartada como evidência de validação do projeto principal;
seu log permanece em Logs/t078-editmode.log. Foi criada Temp/T078Validation com
Assets, Packages e ProjectSettings completos do clone, sem caches ou `.git`.
Nenhum arquivo do projeto principal foi alterado para contornar a falha da cópia.

Os testes incluem rejeição de personagem extra/duplicado e limites dos catálogos;
o validador verifica IDs, vínculos e limites do conteúdo real. Os dois avisos são
os tipos nativos LakeRenderer2D/LakeURP sem validador de domínio. Não houve mudança
de gameplay/assets, nem nova build ou execução física nesta task. O alcance é
auditoria estática do snapshot e validação de domínio/conteúdo, não inspeção de
tráfego nem nova auditoria binária de um APK/EXE distribuído fora desse snapshot.

Reprodução: inventariar os tipos via GUIDs dos `.meta` ou pelo AssetDatabase,
examinar os catálogos e o pipeline indicados acima, executar EditMode e
ContentValidationWindow.ValidateForCI em cópia nova conforme
[quickstart](quickstart.md). Para busca inicial:

```powershell
rg -n -i '\b(shop|store|loja|ads|advertisement|admob|purchase|iap|billing|comprar|anuncio|anúncio|endless|infinite|infinito|biome|bioma)\b' Assets Packages ProjectSettings -g '!*.meta'
rg -n 'UnityEngine\.Purchasing|UnityEngine\.Advertisements|Unity\.Services|GoogleMobileAds|IronSource|AppLovin|StoreKit|BillingClient' Assets Packages ProjectSettings -g '!*.meta'
rg --files Assets Packages -g '*.dll' -g '*.aar' -g '*.jar' -g '*.so' -g '*.dex' -g '*.java' -g '*.kt' -g '*.xml' -g '*.gradle'
```

Ausência de correspondências em rg retorna código 1 e não é falha da auditoria;
qualquer ocorrência deve ser classificada no contexto, como as da tabela acima.
Checklist requirements.md mantido em 16/16. `.specify/extensions.yml` ausente,
sem hooks aplicáveis. **T078 concluída; T075 e seus bloqueios permanecem abertos.**
