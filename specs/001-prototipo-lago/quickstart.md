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
  desta sessão; salvamento entre sessões continua na T059.
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
- Este catálogo prepara a composição da partida; o acionamento de ondas na arena
  e a integração com chefe/finalização permanecem nas T057/T060. O Bootstrap
  continua abrindo a arena de treino existente.

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
O botão de dano serve para testar fases; armas/Quack e o pedido aos 900 segundos
ainda dependem da composição da T060. A arena Bootstrap/Lake continua sendo treino.
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

A T058 entrega o modelo e acumulador em Core. Ainda não há tela de resultado
na arena; coleta dos eventos reais fica na T060, apresentação na T061 e save na T059.
O produtor deve atribuir o último dano/derrota/recompensa antes da transição terminal,
pois fatos posteriores são rejeitados. Consultar o resultado não credita carteira
nem grava progresso.

## Evidencias a conservar

- Relatorio do Test Runner para EditMode e PlayMode.
- Capturas das quatro proporcoes e da Safe Area em Android.
- Perfis de CPU, GPU, memoria e GC nos picos de 200 e 400 inimigos.
- Identificador da build, aparelho, configuracao, seed e resultado de cada playtest.
- Planilha ou formulario consolidando compreensao, justica percebida e repeticao espontanea.
