# Validação de T031

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T031 concluída. GameplayHud agora está montado em Canvas uGUI na cena Lake:
vida e Fôlego numéricos com barras sólidas, terreno, estado do Quack, tempo e mensagens.
Os componentes são informativos e não interceptam raycasts. A apresentação recebe um
HudSnapshot por função de leitura ligada pelo LakeArenaController; não reaplica regras.
O vínculo é removido ao destruir a arena. A atualização ocorre em LateUpdate, após a
inicialização e as alterações de gameplay do frame.

HudSnapshot passou a transportar QuackReady e QuackState, calculados pelo controlador.
Assim Fôlego cheio não implica prontidão durante cooldown ou pausa. O painel mostra
pronto normal/aprimorado, recarregando, sem Fôlego ou indisponível. A barra cheia fica
dourada e a parcial ciano. As rejeições passaram a distinguir recarga e falta de Fôlego;
mensagens de pausa e fim do teste prevalecem no snapshot. O relógio usa tempo de partida.

GameplayHud limita visualmente as barras e trata denominador zero. Os textos numéricos
são reconstruídos quando os inteiros exibidos mudam; o relógio, quando muda o segundo.
Isso não substitui medição de alocações/performance no hardware alvo.

GameplayHudSetup adiciona/reconstrói apenas a raiz gerada do HUD pelo Unity e salva a
cena existente. LakeArenaSetup reconecta esse HUD ao reconstruir a arena. As placas
estáticas de comandos e terra que ficariam sob os painéis foram removidas pelo Editor.
GUID de Lake preservado: fb86267c7de2e40479ccd6bec2e07d8a. Novos .meta gerados pelo Unity.
As referências uGUI necessárias foram adicionadas aos assemblies Editor e PlayModeTests.

Dois testes GameplayHudTests carregam a cena real e verificam vida, Fôlego, terreno,
prontidão, dano refletido na barra, rejeições distintas, pausa e morte. Um novo teste
em PlayerControllerTests confirma Fôlego cheio durante cooldown sem QuackReady.
Etapa vermelha: dois testes falharam pela ausência do HUD na cena, código de saída 2.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 28 | 0 | 0 |

Execução na cópia temporária Temp/T022Validation, com batchmode, nographics, runTests e
testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t031-red.xml, TestResults/t031-editmode.xml e TestResults/t031-playmode.xml.
Logs correspondentes: Logs/t031-red.log, Logs/t031-editmode.log e Logs/t031-playmode.log.
A cena foi gerada com executeMethod QuackQuack.Editor.GameplayHudSetup.Build.

A prévia Logs/t031-preview.png foi renderizada em 1280×720 com gráficos habilitados e
inspecionada: painéis, textos e barras legíveis, sem cobrir o Pato ou corredor de risco.
A escala da captura foi corrigida vinculando a câmera ao RenderTexture antes do layout.
É uma prévia de apresentação com snapshot de exemplo; a ligação runtime é verificada
pelos testes da cena, não inferida da imagem. Captura, logs, XMLs e cópia temporária
são regeneráveis e ficam fora do Git.

O HUD usa dimensões em pixels como base de integração. Safe Area, Canvas Scaler e a
matriz de proporções/dispositivos permanecem em T068–T069 e testes correspondentes.
Não houve aceite de experiência de gameplay, builds ou desempenho. T032 permanece
pendente; VFX e áudio não foram implementados. A alteração preexistente de ícones em
ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
