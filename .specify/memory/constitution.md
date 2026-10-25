<!--
Sync Impact Report
- Version change: 1.0.0 -> 2.0.0
- Modified principles: none
- Modified constraints: Unity 6.3 LTS -> Unity 6000.4.3f1 fixada para o prototipo
- Added sections: none
- Removed sections: none
- Follow-up TODOs: none
-->
# Quack-quack Survive Constitution

## Core Principles

### I. Jogabilidade Primeiro
Cada incremento MUST preservar e tornar testaveis os tres pilares do produto: controle ativo por
Quack, alternancia tatica entre agua e terra e formacao de builds variadas. Uma funcionalidade que
nao fortalece a validacao desses pilares MUST permanecer fora do primeiro prototipo. Feedback
visual e sonoro MUST tornar dano, perigo, alcance do Quack e estado do Folego compreensiveis sem
exigir leitura durante o combate.

### II. Regras Deterministicas e Dados Editaveis
Regras de combate, atributos, efeitos, progressao e estado da partida MUST ser independentes da
apresentacao e testaveis em C# sempre que o Unity permitir. Numeros de balanceamento e definicoes de
personagens, armas, inimigos, passivas e reliquias MUST residir em dados editaveis, nunca duplicados
em controladores. Habilidades MUST usar composicao em vez de uma hierarquia extensa de subclasses.

### III. Multiplataforma por Adaptadores
Windows x64 e Android ARM64 MUST compartilhar a mesma base de regras e conteudo. Teclado, controle e
toque MUST ser convertidos nos mesmos comandos de movimento, Quack e pausa. Somente entrada,
vibracao, salvamento e servicos de plataforma MAY variar por dispositivo. A interface MUST operar
em paisagem, respeitar Safe Area e permanecer legivel em 16:9, 18:9, 19,5:9 e 4:3.

### IV. Desempenho Medido
Pooling MUST ser usado para inimigos, projeteis, recompensas e efeitos recorrentes. O jogo MUST
evitar criacao e destruicao continuas e alocacoes recorrentes no loop de combate. Toda ampliacao de
escala MUST ser sustentada por perfil de CPU, GPU e memoria em Windows e aparelho Android real.
DOTS, ECS, Jobs ou Burst MUST NOT ser introduzidos antes de o Profiler demonstrar que GameObjects,
pooling e sistemas centralizados nao atingem as metas aprovadas.

### V. Escopo do Prototipo Protegido
O primeiro prototipo MUST conter apenas o necessario para validar uma partida de 15 minutos no Lago:
Pato, agua e terra, Quack e Folego, seis armas, seis passivas, tres evolucoes, cinco inimigos, dois
minibosses, Sapo-Rei, dez reliquias e tela final. Loja, anuncios, compras, multiplos biomas e demais
personagens MUST permanecer fora do escopo. Novos conteudos somente podem entrar depois de validar
movimento, decisao de uso do Quack, variedade de builds, justica das derrotas e desejo de repetir.

## Restricoes Tecnicas e de Produto

- A engine aprovada e Unity 6000.4.3f1, fixada para todo o prototipo, com C#, projeto 2D e URP 2D.
- Qualquer mudanca da versao fixada MUST passar por emenda constitucional, reanalise dos artefatos,
  abertura controlada no novo Editor e execucao completa dos testes EditMode/PlayMode e builds.
- Unity Input System, Canvas uGUI, Canvas Scaler e Safe Area sao obrigatorios.
- ScriptableObjects MUST representar conteudo editavel e balanceamento.
- A meta e 60 FPS no Windows e em celular intermediario, com opcao de 30 FPS; celular de entrada
  MUST manter 30 FPS estaveis.
- O pico inicial suportado MUST ser de 200 a 400 inimigos simultaneos.
- Monetizacao MUST NOT fazer parte do prototipo e, no produto, MUST NOT vender poder permanente.
- Arquivos Unity MUST ser organizados por funcao, e cenas, prefabs e dados MUST permanecer
  versionaveis e rastreaveis.

## Fluxo de Desenvolvimento e Qualidade

Cada feature MUST seguir Specify, Plan, Tasks, Implement e Converge. A especificacao MUST declarar
cenarios verificaveis antes do planejamento tecnico. O plano MUST demonstrar conformidade com esta
constituicao antes e depois do design. Tarefas de gameplay MUST incluir verificacao da regra pura,
integracao Unity quando aplicavel e validacao jogavel. Marcos somente sao aprovados quando sua
entrega verificavel funciona com os dispositivos de entrada previstos e sem regressao mensuravel nas
metas de desempenho. Mudancas de escopo ou arquitetura MUST registrar justificativa e alternativas.

## Governance

Esta constituicao prevalece sobre convencoes informais e planos de feature. Emendas exigem alteracao
documentada, justificativa, impacto nos artefatos ativos e aprovacao do responsavel pelo produto.
Versoes seguem SemVer: MAJOR para remocao ou redefinicao incompativel de principios, MINOR para novos
principios ou ampliacoes materiais e PATCH para esclarecimentos sem mudanca de obrigacao. Toda
revisao de especificacao, plano ou implementacao MUST verificar conformidade; desvios MUST ser
explicitamente justificados no plano e acompanhados de estrategia de simplificacao ou reversao.

**Version**: 2.0.0 | **Ratified**: 2026-10-24 | **Last Amended**: 2026-10-24
