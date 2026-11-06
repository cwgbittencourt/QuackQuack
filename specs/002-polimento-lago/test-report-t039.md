# T039 — matriz ampliada de layout integrado

Data: 06/11/2026. Base: `67f4a19`. Escopo somente T039, FR-017.

## Cobertura

IntegratedUiLayoutTests mantém os quatro casos de fluxo de HUD/Relíquias/cartas/resultados e acrescenta doze casos: três cenários novos multiplicados por quatro proporções. Cada caso verifica os dois recortes assimétricos (esquerda/direita), totalizando oito geometrias por cenário. Resoluções: 1920×1080, 2160×1080, 2340×1080 e 1440×1080. Insets: x=72 ou 24, y=32, largura=viewport−96, altura=viewport−80.

| Cenário | Verificações |
|---|---|
| Fluxo existente ampliado | Botões de partida, Relíquias, escolha obrigatória e resultados dentro da área segura; raycasts chegam aos botões; modais cobrem a área segura e bloqueiam os cantos contra interação no gameplay. Prévia de Bolhas garantida nas cartas, imagens/caption/efeitos dentro da área e próximo raio visual maior. |
| Recursos/progresso/dicas | Vazio/parcial/cheio com máximos efetivos 200/140 e preenchimentos 0/0,5/1; XP nos patamares 0/35/60 com níveis/fração da regra existente, último limiar cheio; XP centralizado em x na área segura e na metade inferior; sem labels legadas de recursos/terreno; estados textuais do Quack, dicas e avisos sem truncamento ou sobreposição com os demais painéis/toque. |
| Perfil e menus | Xbox/PlayStation/genérico, labels do perfil real e remapeamento de Relíquias para botão norte; foco dentro do modal, botões clicáveis, textos cabem, modal bloqueia o gameplay; fechar retorna ao contexto e dicas usam os rótulos remapeados. |
| Especiais/toque | Três snapshots próximos (dois minibosses e chefe), barras individuais com vida 25/60/100%, nomes legíveis, sem sobreposição entre si ou com HUD/toque e dentro da área segura; controle disponível oculta joystick/Quack; disable/remove restaura e enable oculta novamente; especial fora do lado correspondente do viewport não é deslocado sobre os outros. |

Dados sintéticos de recursos são passados como snapshots somente à view autorada; não representam jogador morto/cheio real. Para a prévia, uma oferta somente de apresentação conserva o ID da oferta real e usa 0,20→0,232 (Sabão vigente), garantindo cobertura sem depender do RNG. A ação PULAR resolve a oferta real depois da inspeção. As três barras recebem snapshots pelo binding público da view autorada, sem alegar que minibosses e chefe coexistiram na regra da partida. Essas regras/ciclos já possuem seus próprios testes de integração.

## Fixture e isolamento

A câmera de UI usa o RenderTexture na resolução exata escolhida. A projeção mundial dos especiais usa o mesmo tamanho de target e a proporção correspondente; isso corrige a fixture antes de exigir posições em pixels. Não se reduz viewport, muda ortografia para acomodar painéis nem altera o produto. Asserções conferem o pixelRect de ambas as câmeras e de todos os canvases ativos. SafeAreaController continua sendo o componente de produção com providers injetados.

Controles físicos presentes no processo do Editor são temporariamente desabilitados para tornar a disponibilidade inicial determinística e restaurados no teardown. Somente os dispositivos sintéticos criados pela fixture são removidos. A política de foco é restaurada. Perfis usam ControlProfileStore em diretório temporário exclusivo, sem salvar no perfil compartilhado do usuário. Save da partida também fica isolado. A limpeza verifica caminho absoluto e prefixo antes de remover seu diretório.

Não houve alteração em cenas, prefabs, geradores, código de gameplay/apresentação, parâmetros de balanceamento ou metas de desempenho. O `.meta` da classe existente foi preservado. Os testes continuam compiláveis para runtime, sem dependência de UnityEditor; a execução desta task foi no Editor.

## Resultados

| Execução | Resultado |
|---|---|
| Matrix.xml — matriz inicial | 16/16 aprovados, zero falhas/ignorados, código 0 |
| Final-EditMode.xml — regressão completa | 714/714 aprovados, zero falhas/ignorados, código 0 |
| Final-PlayMode.xml — regressão completa | 593/593 aprovados, zero falhas/ignorados, código 0 |
| MatrixFinal.xml — reteste após últimas asserções de prévia/XP/fora da tela | 16/16 aprovados, zero falhas/ignorados, código 0 |

As suítes completas passaram antes das últimas asserções adicionais da própria matriz. O reteste final cobre a classe inteira após essas adições; nenhum código de produção foi modificado entre as execuções.

## Evidências e limites

Unity 6000.4.3f1, cópia isolada ignorada em `Builds/Polimento002Validation`, sem helper de laboratório T032 em Assets. XMLs em `TestResults/Polimento002/T039` e logs em `Logs/Polimento002/T039`. A matriz emite 96 capturas regeneráveis por execução, separadas por proporção/estado/recorte; não versionadas.

Capturas de 4:3 com recursos cheios/toque/dicas, três especiais próximos e perfil PlayStation remapeado foram inspecionadas: conteúdo essencial legível, barras e painéis separados, modais ajustados à área segura. A captura final de cartas com prévia também foi inspecionada: círculos de raio atual/próximo, legenda, efeitos e ações permanecem legíveis sem corte. Isso é revisão sintética da fixture, não aceite global artístico/jogável.

T040 permanece pendente: entrada física, Android nas duas orientações, revisão visual/jogável completa e demais dispositivos não foram aprovados por inferência. T014/8BitDo Android segue pendente conforme decisão anterior. Nenhuma alegação de FPS, GC do produto ou desempenho em aparelhos é feita nesta entrega de testes.
