# T040 — revisão visual e jogável do HUD (US5)

Início: 06/11/2026. Base: `99b517f`. Retomada e conclusão: 09/11/2026, a partir de `23413ea`, por solicitação do usuário. Estado: **T040 concluída** após os aceites físicos de Xbox Series e DualSense no S23. A pausa anterior preservou os aceites já recebidos; não houve alteração de código nem repetição desses roteiros nesta retomada. Somente T040; T014 e validações globais continuam independentes.

## Ambiente e revisão solicitada

Unity 6000.4.3f1, builds IL2CPP Windows x64 / Android ARM64 em cópia isolada `Builds/Polimento002Validation`. Android conectado: Samsung Galaxy S23 Ultra SM-S918B, Android 17. Usuário confirmou disponibilidade de teclado/toque, Xbox Series e DualSense. 8BitDo no Android permanece adiado por decisão anterior.

Na partida Windows normal, o usuário confirmou “o jogo está funcionando como esperado” para movimento/Quack/pausa/coleta/escolha, mas solicitou remover a tarja de status abaixo de XP e os textos nela, inclusive em pausa. Depois pediu colocar XP/nível nesse espaço. Essa revisão de FR-004/C6 foi registrada em spec.md, contracts/gameplay-contracts.md, data-model.md e decisions.md.

R1 remove Messages/statusLabel das duas cenas e posiciona XP/nível em y=12 unidades de referência, mantendo centro horizontal e tamanho. Sinais de Quack permanecem junto aos recursos; pausa é indicada por CONTINUAR/controles e fim pelo modal de resultados. Metadados de status permanecem no modelo para compatibilidade, sem binding de rodapé nas cenas. Sem mudanças em dano, progressão, cura, Sabão ou outros valores de balanceamento.

## Preparação e limites dos laboratórios

Build normal e laboratório são separados. O laboratório privado, fora do Git, começa pausado, suspende spawns/IA e armas automáticas, avança o relógio apenas durante simulação e permite: estados de recursos; XP +10/+100; dois minibosses reais próximos; antecipação do encontro real do chefe; dano/derrota reais; reinício. Fôlego zero pode ser fixado pelo harness somente para observar o estado SEM FÔLEGO; CHEIO libera essa fixação. Não se infere recuperação/balanceamento dessa condição forçada.

Os botões do laboratório não são navegação do produto; ele se oculta em cartas/Relíquias/perfil/resultados. As views, snapshots, atores, leases, ações, modais e controles do jogo são os de produção. Laboratório não constitui partida normal de 15 minutos nem aceite de desempenho. Seu script fica fora de Assets antes de testes e builds normais; builds normais não contêm o harness. Configurações privadas e arquivos regeneráveis não retornam ao projeto.

## Evidências automatizadas da revisão R1

- Geração via PolishUiSetup.BuildGameplayComposition: aprovada.
- EditMode R1: 714/714 aprovados, zero falhas/ignorados.
- Primeiro PlayMode R1: 592 aprovados e uma falha de expectativa antiga em RunSceneFlowTests, que ainda exigia Messages. A asserção foi atualizada para exigir ausência da tarja e posição y=12 de XP, conforme pedido explícito do usuário; nenhuma regra/tolerância foi relaxada para esconder defeito.
- Reteste PlayMode completo R1-Final-PlayMode.xml: 593/593 aprovados, zero falhas/ignorados. EditMode e PlayMode terminaram com código 0.
- Matriz ampliada mantém quatro proporções, ambos os insets e casos de recursos/perfis/prévia/especiais/toque; os papéis visuais removidos foram substituídos por asserções explícitas de ausência do rodapé.

## Matriz de aceite

| Caso | Windows | S23 |
|---|---|---|
| Normal: movimento, Quack, pausa, XP e escolha | Usuário confirmou funcionamento esperado inicialmente e “funcionando corretamente” no reteste R1 | Usuário confirmou “Está funcionando corretamente” no roteiro normal R1 |
| XP inferior central, sem tarja/status redundantes | Aprovado pelo usuário no reteste R1 | Aprovado no roteiro R1, ambas as paisagens |
| Vida/fôlego, estados de Quack além da cor | Usuário: “funciona corretamente” no roteiro teclado/mouse | Usuário: “funciona corretamente” no comparativo CHEIO/PARCIAL/FÔLEGO 0 |
| Dicas: início, pausa, 10s, não repetição e fonte | Janela/pausa/não repetição e rótulos dos dois controles aprovados pelo usuário | Janela/pausa/não repetição e rótulos de Xbox/DualSense aprovados pelo usuário |
| XP parcial/múltiplas subidas/retomada | XP +100, escolhas e retomada aprovados pelo usuário | XP +100, escolhas e retomada aprovados pelo usuário |
| Dois minibosses: barras próximas/dano/pausa/morte/reuso | Roteiro completo aprovado pelo usuário | Roteiro completo aprovado pelo usuário |
| Chefe: barra/dano/remoção/resultados | Barra/dano/derrota/vitória aprovados pelo usuário | Barra/dano/derrota/vitória aprovados pelo usuário |
| Xbox Series: rótulos, HUD/menus e troca de fonte | Roteiro Xbox + DualSense aprovado pelo usuário, sem exceção informada | Bluetooth: usuário confirmou “Funciona corretamente” em 09/11, sem exceção informada |
| DualSense: rótulos, HUD/menus e troca de fonte | Roteiro Xbox + DualSense aprovado pelo usuário, sem exceção informada | Bluetooth: usuário confirmou “funcionando corretamente” em 09/11, sem exceção informada |
| Toque condicional e ambas as paisagens | Não aplicável como aparelho físico | Toque/duas paisagens aprovados anteriormente; restauração e ocultação após desconexão/reconexão aprovadas para ambos os controles em 09/11 |

## Artefatos e pendências

Logs/capturas locais em `Logs/Polimento002/T040`; XMLs em `TestResults/Polimento002/T040`. As quatro builds R1 (normais e laboratórios Windows/Android) concluíram com código 0/marcador PROTOTYPE_BUILD_OK. Builds iniciais sem R1 ficaram obsoletas após a revisão do usuário e não aprovam o layout final.

Foram inspecionadas oito capturas R1 da matriz, cobrindo cada proporção e lado do recorte: 1920×1080 recursos vazio/esquerda e parcial/direita; 2160×1080 perfil Xbox/esquerda e especiais+toque/direita; 2340×1080 cartas com prévia/esquerda e recursos cheios/direita; 1440×1080 especiais sem toque/esquerda e perfil PlayStation/direita. XP permanece no rodapé, sem Messages, textos e painéis essenciais legíveis sem sobreposição nessas amostras. Os 96 PNGs estão em `MatrixR1/`. Isso é evidência sintética complementar, não extrapolação para aparelhos físicos.

Windows R1 normal e laboratório foram abertos; imagem `r1-windows-normal.png` registra o layout em execução. S23 recebeu o normal R1 e o laboratório R1. A primeira captura `android-r1-normal.png` foi feita durante a transição de abertura e não é evidência do HUD; `android-r1-lab.png` já mostra o HUD real/controles de laboratório, iniciado pausado. O usuário confirmou o roteiro normal R1 no S23. Os aceites físicos da tabela foram recebidos separadamente das capturas/asseverações.

T040 concluída para os roteiros registrados de HUD, Windows e S23 com teclado/toque/Xbox/DualSense. Não encerra T075 da feature 001, T014/8BitDo Android, classes de desempenho Android intermediário/entrada, aceite artístico final ou rodada com cinco participantes. Os casos genéricos da matriz permanecem evidência sintética; não representam aceite físico do 8BitDo Android, adiado expressamente pelo usuário.

## Retomada concluída em 09/11/2026

Em 09/11/2026 o S23 SM-S918B/Android 17 foi reconhecido, o APK de laboratório foi conferido pelo mesmo SHA-256 R1 e o app instalado foi aberto no perfil principal do aparelho. O workspace estava limpo em `23413ea`; o harness continua fora de Assets. Checklist de requisitos: 11/11 aprovados. O roteiro pendente foi reapresentado separadamente para Xbox e DualSense, e ambos foram aprovados pelo usuário.

O roteiro físico executado solicitou Xbox Series e DualSense um por vez por Bluetooth, no laboratório R1, com reinício antes de cada controle. Cobriu dicas/rótulos antes de 00:10, movimento/Quack/pausa/Relíquias, menus de melhoria/CONTROLES e HUD legível; após 00:10 dicas não reaparecem. Desligar o controle na partida restaura joystick/Quack touch; reconectar os oculta novamente preservando contexto. Respostas recebidas, respectivamente: “Funciona corretamente” e “funcionando corretamente”, sem falhas ou etapas não cobertas informadas. Não houve novo build nem reexecução de EditMode/PlayMode em 09/11: os resultados anteriores permanecem aplicáveis porque nenhum código/asset foi alterado.

Builds já prontas em `Builds/Polimento002Validation/Builds/`: `T040Windows`, `T040Android`, `T040LabWindows` e `T040LabAndroid` (executável/APK QuackQuack). APKs instalados no S23: pacotes `com.cemggames.quackquack.validation002.t040` e `com.cemggames.quackquack.validation002.t040lab`, rótulos R1. O script privado do laboratório foi retirado de Assets ao terminar as builds e está guardado em `Logs/Polimento002/T040/Validation040Lab.cs`/`.held.cs`; não restaurá-lo para testes ou builds normais. Não confundir com os artefatos iniciais pré-R1.

R1 normal Android SHA-256: `5943CC18420AA2D362A9AD78080E8514A9C7D0F62BF62CC4C34A40F0303C02CB`; R1 laboratório Android: `9F32D33AAC736EC6738A1DF0E9B7E07D3122031277518DAB68EAE62B9BA3A193`. Windows usa o mesmo player stub nas builds; a identidade do código foi conferida por GameAssembly.dll: normal `B2932D5F82132DFDBE90FA03EB06A9BEB106BEB3AC3EA031A8E681DAC5EA0183`, laboratório `D945690D8541FFF0F531920C184DF77BC692E6B8092D8D6662BE0C06B0FDC703`. Os logs de aplicação conferidos não continham NullReferenceException, MissingReferenceException, ArgumentException ou InvalidOperationException; a leitura por processo do laboratório Android também não encontrou FATAL EXCEPTION. Isso não substitui o aceite físico restante.
