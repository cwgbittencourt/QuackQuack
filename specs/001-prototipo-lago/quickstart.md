# Quickstart de Validacao: Prototipo Jogavel do Lago

## Objetivo

Validar o vertical slice contra [spec.md](spec.md), os estados de [data-model.md](data-model.md) e os
contratos de [contracts/gameplay-contracts.md](contracts/gameplay-contracts.md).

## Pre-requisitos

- Unity Hub com Unity 6000.4.3f1, Windows Build Support e Android Build Support.
- Android SDK, NDK e OpenJDK instalados pelo Unity Hub.
- Um computador Windows x64, um celular Android intermediario e, quando disponivel, um de entrada.
- Projeto aberto pela pasta raiz `QuackQuack` depois que os arquivos Unity forem gerados na fase de
  implementacao.

## Validacao automatizada

1. No Unity Test Runner, executar todos os testes EditMode.
2. Confirmar que regras de Folego, Quack, terreno, dano, evolucao, ofertas e transicoes de partida
   passam sem dependencia de cena.
3. Executar todos os testes PlayMode.
4. Confirmar entrada equivalente, pools, spawns, UI/Safe Area, chefe e tela final.
5. Executar o cenario de benchmark com seed fixa para 200 e 400 inimigos e exportar os dados do
   Profiler e Memory Profiler.

Resultado esperado: zero falhas; zero alocacao recorrente atribuida a spawn, projetil, pickup ou VFX
durante a janela estavel; nenhuma instancia ativa simultaneamente no pool.

## Partida integrada disponível após T060

1. Abrir `Assets/QuackQuack/Scenes/RunBootstrap.unity` e entrar em Play. A cena abre
   LakeRun após inicializar os serviços. A lista de build já usa RunBootstrap como entrada.
2. Mover com WASD/setas, usar Quack com Espaço e pausar com Esc ou PAUSAR. A câmera
   acompanha o Pato. Inimigos entram fora da área visível, respeitando limites da onda.
3. Derrotar inimigos com armas/Quack e coletar XP ciano. Cartas congelam a simulação;
   confirmar/pular retoma a fase anterior. RELÍQUIAS abre o loadout e pausa.
4. Observar Libélula Alfa aos 5 minutos, Sapo-Cururu aos 10 e Sapo-Rei aos 15.
   O encontro final limpa a horda anterior. Os círculos verdes são refúgios contra
   ondas, destruídos na fase posterior. Marca e áudio antecedem os padrões do chefe.
5. Derrotar o chefe produz Won; vida zero produz Lost; ABANDONAR produz Abandoned.
   A tela T061 apresenta combate, build e recompensas/progresso em três colunas
   roláveis. Confira o estado do save; se falhar, use TENTAR NOVAMENTE antes de sair.
6. JOGAR NOVAMENTE inicia outra partida preservando o progresso salvo.
   Unlocks/equipamento carregam de `lake-relics.json` em Application.persistentDataPath.
   Alterações são salvas fora do combate; falha de save não bloqueia o encerramento.

O menu **QuackQuack > Build T060 complete lake run** é manutenção: atualiza bindings
nos prefabs existentes, gera settings e reconstrói LakeRun a partir do treino Lake,
preservando os GUIDs dos prefabs e mantendo Bootstrap/Lake. Usar após reconstruir
prefabs com os geradores T056/T057, que não possuem a composição T060. Os assets
estão versionados; não é necessário gerar nada para jogar um clone normal.

Testes: RunOrchestrationContractsTests (EditMode), RunDirectorFlowTests e
RunSceneFlowTests (PlayMode). Estes usam caminhos de save temporários próprios.
Capturas locais ignoradas: `Logs/t060-scene.png` e `Logs/t060-loadout.png`.
Ver [relatório T060](test-report-t060.md) para resultados e limites do aceite.

## Roteiro jogavel por marco

### Arena de treino disponível após T030

- Abrir Bootstrap e entrar em Play; usar WASD/setas, Espaço e Esc.
- O Pato inicia à esquerda. Aproximar-se do alvo vermelho e usar Quack para empurrá-lo
  contra a pedra; atravessar o lago central e verificar a diferença de mobilidade.
- Entrar no corredor à direita, usar Quack contra o alvo e os projéteis roxos, e coletar
  a recompensa dourada. A recompensa desaparece e concede recuperação de Fôlego uma vez.
- Pausar e retomar; projéteis, dano, recuperação e prazos de combate devem permanecer
  congelados na pausa. Após morte, sair de Play e iniciar novamente por Bootstrap.
- O HUD mostra vida/Fôlego e terreno no canto superior esquerdo, relógio à direita
  e mensagens na parte inferior. Dourado indica Fôlego cheio; a linha do Quack distingue
  pronto/aprimorado, recarregando e indisponível. Tentar Quack durante a recarga ou sem
  Fôlego deve mostrar o motivo. Pausa e fim do teste têm prioridade sobre mensagens temporárias.
- Quack aceito deve criar onda dourada e tom temporário. Acertos mostram impacto
  alaranjado; reflexão, pulso ciano; crítico, contorno branco que acompanha o alvo.
  Na pausa, animações e sons devem parar e depois continuar. Ao terminar o efeito,
  o objeto retorna ao pool; rejeições não devem gerar onda ou som de ativação.
  Este roteiro não representa aceite completo de US1 nem validação de plataforma.

Os assets e a cena estão versionados. O menu `QuackQuack → Build T030 lake arena`
é uma ferramenta de manutenção que reconstrói a raiz gerada da arena e atualiza os assets
temporários pelo Unity; não é necessário executá-lo para abrir um clone normal.
O menu `QuackQuack → Build T031 gameplay HUD` atualiza somente a composição do HUD
na cena existente. A ferramenta da arena também reconecta o HUD caso ele já exista.
O menu `QuackQuack → Build T032 Quack feedback` gera o prefab de efeito, os tons
temporários e a ligação dos eventos. Os assets estão versionados; não executar esses
menus em um clone normal para simplesmente jogar.

### Cartas e demonstração de progressão disponíveis após T044

- O Pato inicia com Bumerangue automático. Três pickups cianos de XP ficam em
  (-6, -4), (0, -2) e (5, -5). Cada um concede dez XP uma vez, usando o pool.
- Coletar um pickup abre até três cartas e congela o combate. Comparar tipo,
  efeito atual/próximo, sinergia e requisito de evolução antes de escolher.
- Clicar em Escolher confirma; Banir remove aquela opção da sessão. O rodapé
  informa as duas rerrolagens e o único banimento disponíveis. Pular não gasta
  esses recursos. O foco inicial permite confirmar com Enter; os botões também
  usam a navegação padrão do Input System UI.
- A resolução fecha o modal e retoma o combate; se houver níveis acumulados,
  apresenta a próxima oferta antes de retomar. Encerrar a partida oculta as cartas.
- Os três pickups comuns permitem três escolhas. Com a recompensa rara da T045,
  a demonstração totaliza até sete subidas de nível. Evoluções ainda exigem arma
  N5 e passiva compatível, e dependem das ofertas elegíveis. Este trecho não substitui
  a partida completa nem o aceite de efeitos/balanceamento de todas as passivas.

### Feedback de evolução e recompensa rara disponível após T045

- O pickup roxo em (-9, 2) concede 40 XP uma única vez e retorna ao pool.
- Após resolver as cartas, deve aparecer um losango roxo com RECOMPENSA RARA e
  um tom temporário. Uma evolução aceita mostra estrela dourada, EVOLUÇÃO! e outro
  tom. Melhorias comuns, pulo e comandos rejeitados não geram o sinal de evolução.
- Se várias notificações se acumularem durante as cartas, a evolução tem prioridade
  e os efeitos são reproduzidos um por vez. Pausar congela animação e áudio; encerrar
  a partida limpa efeitos ativos e pendentes.
- Repetir a coleta ou callbacks da mesma oferta não pode conceder XP/evolução de novo.
  Os testes também preparam uma build elegível para verificar as evoluções diretamente.

O menu `QuackQuack → Build T045 upgrade feedback` gera os prefabs, tons temporários
e ligação na cena existente. Assets já estão versionados; não executar para apenas
jogar um clone normal. A ferramenta T044 reconecta o feedback T045 quando ele já existe.

Os prefabs UpgradeSelection e ExperiencePickup e a ligação na cena estão
versionados. O menu `QuackQuack → Build T044 upgrade selection` reconstrói essa
composição pelo Editor, preservando a cena existente e suas demais raízes. Não
precisa ser executado para usar um clone normal. A prévia de manutenção usa
UpgradeSelectionSetup.Preview e não grava alterações de visualização na cena.

O catálogo de build está em `Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset`.
Contém seis armas básicas, seis passivas e três receitas; as três armas resultantes
ficam referenciadas pelas receitas e são excluídas das aquisições comuns. Os assets
já estão versionados; não é necessário executar geração para usar um clone.
O menu `QuackQuack → Build T043 prototype build catalog` é uma ferramenta de
manutenção que regrava o balanceamento inicial e reutiliza os assets existentes,
preservando GUIDs. Ele não modifica cenas nem altera a seleção de cartas.

### Relíquias disponíveis após T052

- Em Lake, clicar em RELÍQUIAS no topo: a arena pausa. Os dez itens mostram efeito,
  estado e condição de desbloqueio; até três podem ficar equipados.
- Botas do Lago, Garras de Junco e Pena de Combate começam disponíveis. Sobreviver
  15/30/45/60 segundos desbloqueia Pedra da Margem, Pérola do Lago, Apito Veloz e
  Lente de Orvalho. Coletar 10/20/30 XP libera Anel de Ondas, Concha de Fôlego e Pena Leve.
- Equipar aplica o bônus; REMOVER retira somente aquele bônus. Em três slots cheios,
  outras opções ficam indisponíveis até remover uma equipada. VOLTAR retoma se o
  próprio painel pausou o jogo; se já estava pausado, preserva a pausa.
- Equipar Garras de Junco e comparar movimento em terra; Botas do Lago funciona
  somente na água. Bônus de armas usam o terreno no disparo; projéteis já disparados
  preservam os atributos aceitos naquele disparo. Concha aumenta a recuperação de Fôlego.
- As dez definições e o catálogo estão em Content/Relics; todos os bônus iniciais
  são 5%. Esses números não representam balanceamento aprovado. Desbloqueios são
  desta sessão na arena atual; serviço de save disponível na T059, ligação automática na T060.
- O menu `QuackQuack → Build T052 relic loadout` é manutenção: gera assets e prefab
  pelo Unity, preservando GUIDs existentes e atualizando somente a raiz Relics T052
  na cena Lake. Não precisa executá-lo para jogar um clone normal.

### Catálogo de encontros disponível após T056

- Abrir `Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset` no Inspector.
  Verificar cinco comuns, Libélula Alfa, Sapo-Cururu, settings, seis ondas e três marcos.
- Prefabs temporários estão em `Prefabs/Prototype/Enemies`; as definições e as
  ondas estão em `Content/Enemies/Prototype` e `Content/Encounters/Prototype`.
- O menu `QuackQuack → Build T056 lake encounter catalog` é manutenção: regrava
  o catálogo e dados iniciais preservando GUIDs. Assets já estão versionados e
  não precisam ser gerados para abrir um clone normal.
- A prévia de manutenção `LakeEncounterCatalogSetup.Preview` rende uma galeria
  sem salvar cena. Os testes usam os assets reais para criar pools/diretor e
  verificar os dois minibosses com o scheduler compartilhado.
- A composição com ondas, chefe e encerramento está disponível em RunBootstrap
  desde T060. Bootstrap continua abrindo a arena de treino existente.

### Marco 1 - Fundamentos

- Abrir `Bootstrap`, iniciar `Lake` e mover o Pato com teclado, controle e toque.
- Alternar 16:9, 18:9, 19,5:9 e 4:3; verificar paisagem e Safe Area.
- Gerar e iniciar uma build Windows e uma Android.

### Marco 2 - Quack e terreno

- Comparar velocidade e recuperacao de Folego em agua e terra.
- Usar Quack vazio, parcial e cheio; validar rejeicao, efeito normal e efeito aprimorado.
- Empurrar, interromper e refletir alvos elegiveis; verificar feedback correspondente.

### Marco 3 - Combate e escala

- Enfrentar os cinco inimigos isoladamente e em composicoes.
- Confirmar Formiga, Aranha, Vespa, Besouro e Caramujo Aquatico e enfrentar Libelula Alfa no minuto 5
  e Sapo-Cururu no minuto 10.
- Elevar a densidade para 200 e 400 ativos; registrar FPS, frame time, GC e memoria.
- Verificar que inimigos, projeteis, pickups e VFX retornam limpos aos pools.
- Confirmar ausencia de `Update` individual por inimigo no perfil de hordas.

### Marco 4 - Builds

- Forcar subidas de nivel; validar tres cartas, duas rerrolagens, um banimento e pular.
- Maximizar cada arma e testar as tres combinacoes evolutivas.
- Confirmar que cartas explicam efeito, sinergia e requisito sem bloquear o combate apos a escolha.

### Marco 5 - Partida completa

- Jogar 15 minutos com seed registrada.
- Confirmar dois minibosses antes do chefe e todas as telemetrias do Sapo-Rei.
- Finalizar por vitoria, morte e abandono; comparar tela final e recompensas.
- Fechar e reabrir o jogo; confirmar recordes, desbloqueios e reliquias.
- Desbloquear, equipar zero a tres reliquias e confirmar aplicacao/remocao idempotente dos efeitos.

### Marco 6 - Aceite

- Executar a matriz completa de entrada, tela e plataforma.
- Conduzir playtest e calcular SC-001 a SC-008.
- Aprovar somente se os criterios tecnicos e de experiencia forem atendidos ou se desvios estiverem
  documentados com tarefa corretiva.

## Treino isolado do Sapo-Rei (T057)

Abra `Assets/QuackQuack/Scenes/FrogKingPractice.unity` no Unity 6000.4.3f1 e entre
em Play. Mova o Pato amarelo com WASD/setas. O círculo verde maior é o chefe;
os três círculos verdes escuros protegem das ondas. O salto marca a posição do
Pato antes de aterrissar, portanto sair da marca evita o dano.

Use **Aplicar 10% de dano ao chefe** para explorar as fases: saltos/ondas no
início, girinos a partir de 70% da vida e destruição de refúgios a partir de 35%.
Observe o círculo de aviso e o áudio antes de cada execução. Girinos perseguem
o Pato e somem ao contato ou por duração; ondas expandem a partir do ponto marcado.
Um refúgio destruído deixa de proteger. Vida zero do chefe mostra Won; vida zero
do Pato mostra Lost; o botão de abandono mostra Abandoned. Saia de Play para reiniciar.

Essa fixture é exclusiva do Editor e fica fora da lista de cenas da build.
O botão de dano serve para testar fases. A partida RunBootstrap integra armas/Quack
e o pedido aos 900 segundos desde T060. Bootstrap/Lake continua sendo treino.
Para regenerar os assets e a fixture, use **QuackQuack > Build T057 Frog King**.
O gerador preserva os GUIDs existentes; a cena de treino é reconstruída.

Evidência automatizada: [relatório T057](test-report-t057.md). A captura em
`Logs/t057-practice.png` mostra a geometria do salto na arena; não inclui o
overlay OnGUI. Realizar playtest humano antes de aprovar percepção e justiça.

## Resultado de domínio disponível após T058

No Test Runner, execute `RunResultTests` e `RunResultStatisticsTests` em EditMode:
os 48 casos verificam resultado único, os três finais, abandono parcial e os
campos completos. A sessão recebe fatos aceitos e captura a build no encerramento;
o resumo continua igual mesmo se a build/loadout original mudar depois.
Consulte o [relatório T058](test-report-t058.md) e o contrato de resultado para a API.

A T058 entrega o modelo e acumulador em Core. A T060 coleta os eventos reais
em RunBootstrap; a apresentação completa está disponível desde T061. O serviço T059
também está ligado ao ciclo da partida para relíquias.
O produtor deve atribuir o último dano/derrota/recompensa antes da transição terminal,
pois fatos posteriores são rejeitados. Consultar o resultado não credita carteira
nem grava progresso.

## Serviço de save disponível após T059

No Test Runner, executar `SaveServiceTests` e `JsonSaveWriteTests` em EditMode
(39 casos) e `RelicSaveRoundTripTests` em PlayMode. As fixtures usam diretórios
temporários exclusivos e removem somente seus próprios dados, com caminho validado.
Não leem nem alteram saves pessoais.

`JsonSaveService(caminho, idsConhecidos)` recebe os dez IDs do catálogo. `Load()`
retorna Data, Status e Diagnostic; Data contém versão, desbloqueios e equipamento.
Reaplicar os IDs usa Unlock/Equip, respeitando os três slots e a idempotência do
loadout. `Save(loadout)` grava um snapshot, preservando a ordem equipada.
`RestoreBackup()` promove explicitamente o backup validado; Load apenas o consulta.

O schema atual é 2; versões 0/1 migram relíquias com perfil de progresso vazio.
Versão 0 usa equipamento vazio se ausente. Versão futura no principal impede gravação/restauração por esta versão.
Sem principal/backup utilizável, Load retorna padrões vazios para o jogo continuar.
Falhas de escrita retornam Failed com diagnóstico, sem expor caminho ou conteúdo.
O serviço não faz I/O por frame nem determina sozinho o caminho pessoal.

O treino Bootstrap mantém relíquias por sessão. RunBootstrap define o caminho local
e os momentos de carregar/salvar desde T060; carteira, recordes e desafios estão no schema 2 desde T061. Ver [relatório T059](test-report-t059.md).

## Evidencias a conservar

- Relatorio do Test Runner para EditMode e PlayMode.
- Capturas das quatro proporcoes e da Safe Area em Android.
- Perfis de CPU, GPU, memoria e GC nos picos de 200 e 400 inimigos.
- Identificador da build, aparelho, configuracao, seed e resultado de cada playtest.
- Planilha ou formulario consolidando compreensao, justica percebida e repeticao espontanea.

## Tela final e progresso (T061)

O menu **QuackQuack > Build T061 results screen** gera os assets pelo Unity;
o gerador T060 reconecta a tela quando seu asset de progressão existe. Os assets
estão versionados; não é necessário regenerar para jogar um clone normal.
ResultsProgression.asset contém metas provisórias editáveis: 100 inimigos acumulados,
15 minutos em uma partida e uma vitória contra o Sapo-Rei. Não concedem moedas extras.

Execute ResultsContractTests, RunProgressionTests e ProgressSaveTests em EditMode;
ResultsViewTests e ResultsSceneFlowTests em PlayMode. Cobrem três finais, retry e
reinício preservando saldo. Capturas Logs/t061-*.png são fixtures de UI a 1280×720,
não aceite de gameplay, Safe Area ou plataformas. Ver [relatório T061](test-report-t061.md).

## Testes de paridade disponíveis após T062

No Test Runner PlayMode, filtrar InputParityTests. São 15 casos. Após T066, todos devem passar com os adaptadores reais das três entradas.
Esses casos não substituem o aceite de entrada física e interface da US4.

Os testes usam teclado/gamepad sintéticos e callbacks do futuro joystick/botões;
não exigem dispositivos físicos. Comparam comandos e checkpoints do PlayerController
real em passos controlados, incluindo pausa, terreno, Fôlego e os três finais.
O contrato de callbacks e os limites constam em contracts/gameplay-contracts.md.
Depois de T065/T066, repetir os 15 casos e a suíte completa. T063/T064 continuam
responsáveis por UI responsiva e smoke tests de plataforma.

## Testes de UI responsiva disponíveis após T063

No Test Runner PlayMode, filtrar ResponsiveUiTests. Os 15 casos usam o HUD autoral
em 1920×1080, 2160×1080, 2340×1080 e 1440×1080, com câmera/render target próprios.
Não alteram a resolução do desktop nem gravam cenas. A fixture espera o layout e
confirma a projeção do Canvas antes de medir os oito elementos essenciais.

Quatro casos verificam o viewport sem recortes. Sete verificam o controlador T068:
normalização nas quatro proporções, mudança de resolução/recorte, idempotência,
remoção de margens, geometria inválida, reativação e retorno às fontes Screen.
Quatro verificam o CanvasScaler autoral 1920×1080 da T069 e a
hierarquia segura com os oito elementos essenciais dentro dos recortes.
Nenhum teste é ignorado por faltar implementação.

Para usar SafeAreaController, coloque-o em um RectTransform filho direto de um
Canvas de tela inteira e mova o conteúdo protegido para baixo dele. O componente
lê Screen.safeArea e atualiza âncoras/offsets automaticamente. O HUD de Lake e LakeRun já está configurado desde a T069. SetViewportProvider permite fontes de teste; null restaura
Screen. Ver contrato e [relatório T068](test-report-t068.md).

Depois de T068/T069, repetir a matriz e a suíte completa. Geometria do HUD e altura
de texto não substituem inspeção humana de todas as telas, recortes Android reais,
Safe Area de aparelho ou aceite de legibilidade da T075.

## Smoke tests de build e inicialização (T064)

No Test Runner PlayMode, executar PlatformBuildConfigurationSmokeTests e
PlatformBuildSmokeTests. Desde T070, os nove casos de configuração validam o pipeline de produção. Os dois de runtime verificam as
cenas de produção e inicialização de RunBootstrap/LakeRun com save temporário.
Passar no Editor não comprova inicialização no Windows Player ou no Android.

Para compilar e executar somente a fixture de runtime em test players, usar a
mesma versão Unity e uma cópia isolada do projeto, com os módulos e toolchains do
alvo instalados. Os players devem usar IL2CPP; Android deve estar configurado ARM64.
Exemplos de argumentos para Unity.exe (aguardar o processo e conferir XML/ExitCode):

~~~text
-batchmode -projectPath <copia-isolada> -runTests -testPlatform StandaloneWindows64 -testCategory PlatformRuntimeSmoke -buildPlayerPath <pasta-de-build> -testResults <windows.xml> -logFile <windows.log>
-batchmode -projectPath <copia-isolada> -runTests -testPlatform Android -testCategory PlatformRuntimeSmoke -buildPlayerPath <pasta-de-build> -testResults <android.xml> -logFile <android.log>
~~~

No Android, o segundo comando requer aparelho autorizado e conectado e constrói,
instala e executa o test player nele. O Test Framework inclui sua própria cena de
entrada; a ordem de cenas da build de produto é verificada pelo contrato da T070.
Artefatos de build, XML e logs devem ficar nas pastas ignoradas. Não incluir -quit
antes do término dos testes. Não transformar ausência de módulo/aparelho ou falha
de build em teste aprovado. Ver [relatório T064](test-report-t064.md).

## Adaptador de controle disponível após T065

Execute InputParityTests e GamepadInputAdapterTests no Test Runner PlayMode.
O adaptador usa stick esquerdo, botão Sul para Quack e Start para pausa; aplica a
deadzone configurada no Input System, preserva intensidade parcial e consome bordas
uma vez. Os testes usam gamepad sintético, incluindo reset e remoção/reconexão.

GamepadInputAdapter implementa IPlayerInput e pode ser injetado em
PlayerController.Initialize; quem o cria deve descartá-lo com Dispose. Desde T067, as cenas usam UnifiedPlayerInput e selecionam a fonte mais recente.
O aceite com dispositivos físicos continua pendente. Ver [relatório T065](test-report-t065.md).

## Controles móveis disponíveis após T066

O prefab Assets/QuackQuack/Prefabs/Prototype/MobileControls.prefab contém joystick,
QUACK e PAUSAR, ligados ao MobileInputAdapter. Pode ser instanciado para revisar
os controles no Editor. Desde T067, a entrada padrão instancia o prefab quando
há Touchscreen ou plataforma móvel, em Bootstrap/Lake e RunBootstrap/LakeRun.
Injetar o adapter em PlayerController.Initialize permite consumir seus comandos.
O mapa unificado está disponível desde T067; Safe Area e integração responsiva
permanecem nas tarefas correspondentes.

Arrastar a base move o knob; soltura retorna ao centro. Cada controle captura seu
dedo, permitindo movimento e botões simultâneos. Cancelar, ocultar controles,
perder foco ou suspender descarta entradas pendentes. O menu **QuackQuack > Build
T066 mobile controls** é manutenção; os assets já estão versionados.

Execute InputParityTests, MobileInputAdapterTests e MobileTouchscreenTests em
PlayMode. O último usa Touchscreen sintético e raycasts reais no prefab e gera a
captura ignorada Logs/t066-controls.png. Os testes de eventos/Editor não representam
aceite em aparelho Android. Ver [relatório T066](test-report-t066.md).

## Entrada unificada disponível após T067

QuackQuackInput.inputactions define o mapa Gameplay e as ações Move, Quack e Pause,
com grupos/esquemas Keyboard, Gamepad e Touch. Teclado usa WASD/setas, Espaço e Esc;
controle usa stick esquerdo, Sul e Start; toque usa o prefab móvel. A configuração
Resources/PlayerInput.asset referencia esse asset e MobileControls.prefab. Não é
necessário executar geradores em um clone normal.

O Pato das cenas padrão usa UnifiedPlayerInput. A última atividade não neutra
escolhe uma fonte; vetores não são somados, e um dispositivo mais antigo mantido
não volta a mover sozinho quando a fonte recente é solta. Quack/Move são desativados
na pausa; Pause continua disponível para retomar. Durante cartas e resultados,
ações de gameplay são desativadas e controles móveis ocultos. Após menus/foco,
movimento precisa voltar ao neutro; botões precisam de nova ativação.

O EventSystem continua ativo com os controles móveis ocultos. A entrada UI do Input
System permanece independente das ações de gameplay. O menu **QuackQuack > Build
T067 unified input** cria os assets ausentes e reconecta a configuração, preservando
bindings/IDs do asset existente. Os adaptadores usam clones em runtime.

Execute UnifiedInputContractTests e UnifiedPlayerInputTests, além das fixtures de
paridade e adaptadores. A categoria UnifiedInputRuntime também pode ser usada nos
test players. Ver [relatório T067](test-report-t067.md) para evidências Windows e
limites; Android físico, Safe Area e aceite integral continuam pendentes.

Na composição T067, navegação/Submit/Cancel da UI ficam habilitados somente nos
menus, enquanto cliques e toque continuam disponíveis no gameplay. Assim, Sul
não confirma acidentalmente um botão UI ao usar Quack. Perda de foco limpa entradas
antigas e respeita a política de background do Input System; suspensão do app
continua bloqueando entrada mesmo se a política permitir input em background.

## HUD responsivo disponível após T069

Lake e LakeRun já contêm CanvasScaler com referência 1920×1080 e correspondência
pela altura. Vitals, relógio e mensagens ficam sob HUD Safe Area: o controlador
reposiciona seus limites automaticamente, sem alterar as regras do jogo.
O menu **QuackQuack > Build T069 responsive HUD** reaplica a configuração nas duas
cenas existentes. Reconstruir o HUD por GameplayHudSetup também mantém esse layout.

Execute ResponsiveUiTests para a matriz 16:9, 18:9, 19,5:9 e 4:3, com recortes
assimétricos. Os quatro casos autorais geram capturas ignoradas
Logs/t069-hud-<largura>x<altura>.png na cópia de validação. RunSceneFlowTests confere
a presença da mesma hierarquia no fluxo de produção, com save temporário isolado.
Consulte o [relatório T069](test-report-t069.md); o aceite integral de UI/aparelhos
continua na T075.

## Builds de produto disponíveis após T070

O menu **QuackQuack > Build prototype** contém Windows x64 e Android ARM64 APK.
Selecione o alvo correspondente no Editor antes de executar. Para automatizar,
use Unity 6000.4.3f1 em uma cópia isolada do projeto:

~~~text
-batchmode -quit -buildTarget Win64 -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildWindows -logFile <windows-build.log>
-batchmode -quit -buildTarget Android -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildAndroid -logFile <android-build.log>
~~~

As saídas relativas à cópia são Builds/Windows/QuackQuack.exe e
Builds/Android/QuackQuack.apk. Ambos empacotam somente RunBootstrap e LakeRun e usam
IL2CPP; Android usa apenas ARM64. ConfigureTarget não troca o alvo ativo. A build
Android gera APK, não App Bundle, e precisa de SDK/NDK/JDK e dependências Gradle.
O comando registra PROTOTYPE_BUILD_OK somente após BuildResult.Succeeded; confira
código de saída e log. Build/ConfigureTarget deixam as configurações aplicadas no
projeto; nenhum desses comandos altera a lista global de cenas do Editor.

O pipeline não instala nem inicia o jogo. Smoke tests de runtime usam as entradas
separadas do Test Framework descritas acima, com save isolado. As configurações
podem ser validadas por PlatformBuildConfigurationSmokeTests, sem compilar ou
instalar players. Ver [relatório T070](test-report-t070.md) para resultados observados.
