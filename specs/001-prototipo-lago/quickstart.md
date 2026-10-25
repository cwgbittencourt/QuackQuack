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
- Ondas visíveis, impacto, contorno crítico e áudio ainda dependem de T032.
  Este roteiro não representa aceite completo de US1 nem validação de plataforma.

Os assets e a cena estão versionados. O menu `QuackQuack → Build T030 lake arena`
é uma ferramenta de manutenção que reconstrói a raiz gerada da arena e atualiza os assets
temporários pelo Unity; não é necessário executá-lo para abrir um clone normal.
O menu `QuackQuack → Build T031 gameplay HUD` atualiza somente a composição do HUD
na cena existente. A ferramenta da arena também reconecta o HUD caso ele já exista.

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

## Evidencias a conservar

- Relatorio do Test Runner para EditMode e PlayMode.
- Capturas das quatro proporcoes e da Safe Area em Android.
- Perfis de CPU, GPU, memoria e GC nos picos de 200 e 400 inimigos.
- Identificador da build, aparelho, configuracao, seed e resultado de cada playtest.
- Planilha ou formulario consolidando compreensao, justica percebida e repeticao espontanea.
