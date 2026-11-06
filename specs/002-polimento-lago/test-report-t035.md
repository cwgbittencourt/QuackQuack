# T035 — HUD de recursos e progresso

Data: 06/11/2026. Base: `da12604`. Escopo somente T035.

## Implementação

PlayerController.Snapshot transporta o progresso real de UpgradeSystem e deixa vazios terreno e mensagem ociosa. Pausa, vitória, derrota, abandono e avisos transitórios continuam separados do progresso. O aviso de pausa deixa de sugerir uma tecla específica. Nenhuma regra de combate, entrada, recuperação ou balanceamento foi alterada.

GameplayHud usa Health01/Breath01 do snapshot, sem duplicar normalização ou curva de XP. As barras deixam de apresentar números; os ícones de coração e ondas têm silhuetas distintas, independentes de fonte. Prontidão, aprimoramento e recarga permanecem explícitos em texto, além da cor. XP e nível ficam no centro inferior, dentro da hierarquia de área segura. O rodapé legado de controles e os textos de terreno e recursos são limpos.

ResourceIconGraphic gera geometria uGUI e exige CanvasRenderer. A inspeção inicial das capturas encontrou a ausência desse componente, que não era detectada pelo teste da geometria isolada. A dependência foi corrigida e o teste passou a exigir o renderer dos dois ícones.

Para preservar referências serializadas existentes, o HUD compõe os novos elementos uma única vez quando faltam bindings. Referências já autoradas têm precedência. Não há criação de objetos nas renderizações seguintes. Labels legados permanecem como referências vazias; cenas, prefabs e geradores não foram regravados. A composição autorada definitiva continua na T038.

## Validação

Cinco novos casos PlayMode cobrem silhuetas monocromáticas, estados do Quack, ausência de instruções/terreno permanentes, vitória/abandono separados do nível e composição estável após cem renderizações. Os dois casos vermelhos de T035 escritos na T033 passaram. A matriz responsiva existente verifica os elementos efetivamente visíveis (ícones, barras, mensagens, relógio, XP e nível), preservando viewport, câmera e insets.

- EditMode completo: **710/710 aprovados**, zero ignorados, código 0 (`Final-EditMode.xml`).
- PlayMode completo antes do ajuste de CanvasRenderer: **558 casos, 553 aprovados e cinco falhas esperadas da T036**, zero ignorados, código 2 (`Final-PlayMode.xml`).
- Reteste completo após o ajuste: **558 casos, 553 aprovados e as mesmas cinco falhas esperadas da T036**, zero ignorados, código 2 (`FinalR2-PlayMode.xml`). Nenhuma regressão adicional.

As cinco falhas são exclusivamente pela ausência de ContextualHintsView: início/fim exato da janela, suspensão em pausa/escolha, troca de entrada/binding, ausência de repetição e supressão no encerramento. Nenhum teste foi ignorado ou relaxado para esconder essas pendências.

## Reprodução e limites

Unity 6000.4.3f1, cópia isolada em `Builds/Polimento002Validation`, sem helper privado do laboratório T032 em Assets. Logs/XMLs em `Logs/Polimento002/T035` e `TestResults/Polimento002/T035`. Capturas sintéticas da matriz em `Logs/t069-hud-{largura}x1080.png`, não versionadas. A matriz cobre 1920, 2160, 2340 e 1440 pixels de largura, com recorte assimétrico injetado; usa vida 75%, fôlego 65%, nível 3 e XP parcial.

As capturas finais de 1920×1080 e 1440×1080 foram inspecionadas: coração e ondas aparecem junto às barras, XP/nível ficam no centro inferior, aviso e relógio permanecem separados, sem cortes ou sobreposição entre esses elementos. As demais proporções são cobertas pelas asserções da matriz. O fundo vazio é próprio da câmera de teste; não representa uma captura de partida nativa.

As capturas/testes de integração não constituem aceite jogável Windows/Android, de desempenho ou de direção artística. T036, T038, T039 e T040 permanecem pendentes. Os ajustes de chance de cura e bônus de Sabão discutidos na T032 continuam separados: nenhuma mudança de balanceamento nesta entrega.
