# T038 — composição autorada do HUD

Data: 06/11/2026. Base: `9936b6a`. Escopo somente T038.

## Implementação

GameplayHudSetup migra o HUD existente em vez de destruir/recriar sua raiz. Preserva painéis, barras e referências externas; remove objetos legados de números de Vida/Fôlego, terreno e controles permanentes, deixando seus antigos campos de compatibilidade nulos nas cenas. Autora ícones ao lado das duas barras, estado textual do Quack, relógio, mensagens separadas, XP/nível inferior central, label de dicas temporárias e overlay de especiais. Referências de ícones/XP são serializadas, sem depender do fallback criado pela T035 para fixtures/cenas antigas.

GameplayHudComposition compõe serviços e apresentação por referências serializadas. Aguarda jogador e diretor prontos, liga HUD ao snapshot real, dicas ao mesmo RunState e à fonte de entrada do jogador, e barras ao registro de especiais do diretor/câmera/área segura. A arena de treino usa registro vazio. Disable/unbind e reativação preservam o relógio da partida; barras já pré-aquecidas são reutilizadas. Presentation continua sem referências ao assembly de gameplay.

PlayerController expõe somente a fonte para apresentação, sem consumir novos comandos: no Android respeita disponibilidade de controle versus toque; no Windows mantém a última fonte e cai para teclado se o controle foi perdido. Entrada injetada conserva a fonte do último comando aplicado. Regras, bindings e disponibilidade permanecem nas implementações existentes.

PolishUiSetup.BuildGameplayComposition é a entrada de geração. Atualiza a prévia em lugar, conserva o perfil existente e chama IntegratedUiSetup para configurar os cinco prefabs e ambas as cenas. Os prefabs de cartas/Relíquias/resultados/controles já tinham prévia, efeitos ativos, perfil e ajuste modal válidos: sua passagem pelo gerador não alterou seus bytes. Apenas Lake e LakeRun tiveram mudanças de assets. A geração antiga de menus também reaplica a composição; LakeRunSetup foi conectado à migração para não herdar uma composição sem diretor ao reconstruir a partida a partir da arena.

Os IDs serializados de GameplayHud, PlayerController e LakeBuildProgression foram comparados entre as cenas originais e geradas e permaneceram iguais nas duas cenas. O ID de RunDirector em LakeRun também foi preservado. GUIDs/.meta existentes permanecem; os novos scripts receberam metas do Unity. Não houve edição textual de cenas/prefabs.

## Validação

Quatro testes EditMode novos verificam bindings nas duas cenas, remoção de objetos legados, referências de prévia/efeitos/perfil/modais e geração repetida com bytes/GUIDs idênticos dos sete artefatos. Três casos PlayMode portáveis novos carregam Bootstrap/RunBootstrap e verificam composição pronta, recursos/dicas reais, pausa/expiração/rebind sem reinício ou duplicação, além da ligação da barra do chefe à vida real e limpeza terminal.

O teste de layout existente passou a localizar o painel de recursos pelo ícone autorado, pois a antiga label numérica foi removida. Não amplia a matriz da T039, não altera viewport/câmera/insets e não enfraquece as verificações existentes.

| Execução | Resultado |
|---|---|
| Geração inicial/final | Códigos 0, marcadores T038 presentes |
| Composition.xml | 4/4 aprovados, zero ignorados |
| Focused.xml (composição/HUD/especiais/layout existente) | 69/69 aprovados, zero falhas/ignorados |
| EditMode completo final — Final-EditMode.xml | 714/714 aprovados, zero falhas/ignorados, código 0 |
| PlayMode completo final — Final-PlayMode.xml | 581/581 aprovados, zero falhas/ignorados, código 0 |
| Conteúdo / windows-build.log | 66 assets válidos, dois avisos URP preexistentes, marcador T073_CONTENT_VALIDATION_OK |
| Build Windows x64 IL2CPP | Código 0 e PROTOTYPE_BUILD_OK; iniciada e inspecionada |

Na build nativa isolada “QuackQuack T038”, a inspeção via skill computer-use confirmou ícones ao lado das barras, estado do Quack, XP/nível e dicas de teclado em duas linhas no primeiro segundo. Aos 11 segundos as dicas já não apareciam. Pausar pelo botão exibiu CONTINUAR, acesso a CONTROLES e PAUSADO na região de mensagens; XP parcial permaneceu separado. Capturas locais `windows-start.png`, `windows-after-window.png` e `windows-paused.png` em Logs/Polimento002/T038. A janela foi deixada pausada; o Player.log não apresentou NullReferenceException, MissingReferenceException, ArgumentException ou InvalidOperationException no trecho executado. Essa inspeção não cobre controle/toque, Android, especiais nativos ou a matriz completa da T040.

## Reprodução e limites

Unity 6000.4.3f1, cópia isolada ignorada `Builds/Polimento002Validation`, sem helper de laboratório T032 em Assets. Entrada: `QuackQuack.Editor.PolishUiSetup.BuildGameplayComposition`. Logs em `Logs/Polimento002/T038`; XMLs em `TestResults/Polimento002/T038`. Somente cenas/metas e código de produto/testes especificados são copiados de volta; caches, builds, configurações privadas, logs e capturas ficam fora do Git. O diff de código/documentação não tem erros de whitespace; espaços após chaves YAML vazias m_Name/m_Text foram emitidos pelo serializador Unity e preservados sem edição textual dos assets.

T038 integra os componentes nas cenas. A ampliação da matriz, ambos os recortes/estados/especiais e aceite visual/jogável completo continuam T039/T040. Não há aceite físico Android, desempenho global ou direção artística por inferência dos testes de integração. Os ajustes de chance de cura e bônus do Sabão permanecem separados, sem alteração nesta task.
