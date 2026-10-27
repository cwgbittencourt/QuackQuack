# T060 — Orquestração da partida do Lago

Data: 2026-10-27. Unity 6000.4.3f1, revisão 39d1a88d4dd1.
Escopo: somente T060. T061 em diante permanecem pendentes.

## Entrega

RunBootstrap abre LakeRun e aguarda Pato, build e loadout. RunDirector passa a
avançar um único relógio; GameBootstrap cede esse controle. Bootstrap/Lake continuam
como treino, sem alterar o GUID ou conteúdo das duas cenas existentes.
RunBootstrap é a primeira cena da lista de build. Há controles PAUSAR/CONTINUAR
e ABANDONAR, HUD ligado ao Pato e câmera que acompanha o jogador dentro dos limites.

O diretor consome o catálogo T056: seis ondas, Libélula Alfa aos 300 s, Sapo-Cururu
aos 600 s e Sapo-Rei aos 900 s. A cadência e o limite de cada onda são autorais;
frame longo não gera todos os lotes atrasados. Minibosses possuem prioridade e
tentativas limitadas de reposição quando o pedido não consegue espaço. O pedido do
chefe acontece uma vez, limpa a horda anterior sem recompensa e inicia Boss.
O tempo continua no chefe; chegar aos 15 minutos não concede vitória automaticamente.

CombatTargetBinding conecta os sete inimigos e o chefe a armas/Quack, usando o
lease atual. Dano frontal e controle passam pelo scheduler. Eventos de dano aceito
são emitidos pelo alvo, evitando duplicação no bus. Fatores de vida/dano dos
encontros são aplicados sem modificar os assets; escala de vida só ocorre uma vez
por lease e reset a remove. O chefe respeita os mesmos fatores na vida/dano e fases.

EnemyReward registra derrota/moedas e cria XP exclusivamente no pool limitado;
saturação suprime o pickup e é contada. O resultado recebe dano por ID de arma,
Quack, derrotas, sequência, build, sinergias e itens desbloqueados. Dano e derrota
finais do chefe precedem Won, preservando o último golpe no snapshot T058.

RunState guarda ResumePhase e retoma Playing ou Boss após pausa/cartas/loadout.
Aviso e invocações do chefe congelam sem serem descartados por um modal.
Vitória, morte e abandono encerram uma vez, limpam entidades próprias e entregam
RunDirector.Result/ResultAvailable. A passagem para a tela Results fica na T061.
O HUD atual identifica VITÓRIA, DERROTA ou PARTIDA ABANDONADA.

O save T059 carrega desbloqueios/equipamento antes de iniciar a simulação.
RunBootstrap usa lake-relics.json em Application.persistentDataPath; grava estado
sujo em pausa/seleção/terminal, suspensão do app ou descarregamento. LastSave expõe
falha sem impedir o final. Não há gravação de arquivo a cada frame de combate.
O schema 1 continua restrito a relíquias; não credita carteira/recordes/desafios.

## Autoria Unity e conteúdo versionado

LakeRunSettings.asset contém catálogo, chefe, prefab de XP, regiões, orçamento de
pickups, prazo de pedidos e política provisória de recompensa (bônus 50, chefe 50,
abandono 0,5). Não são números de balanceamento aprovados. A composição aquece os
pools dos sete inimigos, XP e chefe, incluindo pools das invocações, antes de jogar.

LakeRunSetup.Build gera settings, RunBootstrap/LakeRun, controles e refúgios e
acrescenta bindings aos oito prefabs existentes pelo Unity. A cena de partida usa
o treino como base, remove o controlador/alvos/placas de treino e não cria seus
pickups de demonstração. A progressão tem 64 thresholds cumulativos autorais.
As oito metas dos prefabs foram comparadas por hash entre fonte e geração e
permaneceram idênticas. Cenas/prefabs/dados serializados foram produzidos pelo
Editor, sem editar YAML manualmente. EditorBuildSettings também veio do Unity.

## Validação observada

Execução na cópia isolada Temp/T033Validation, com Assets/Packages/ProjectSettings.
Logs, XML e capturas permanecem ignorados por Git.

| Etapa | Total | Aprovados | Falhas | Exit code |
|---|---:|---:|---:|---:|
| Contratos antes da implementação | 3 | 0 | 3 esperadas | 2 |
| EditMode completo | 441 | 441 | 0 | 0 |
| PlayMode completo | 164 | 164 | 0 | 0 |

Cinco novos casos EditMode cobrem existência do diretor, suspensão/retomada do
chefe por dois modais, escala de vida por lease e captura de metadados durante
seleção antes do abandono sem aceitar dano/derrotas nessa fase.

RunDirectorFlowTests contém 13 casos com serviços e assets reais: marcos 5/10/15,
pedido único do chefe, cap/cadência da onda, pausa sem catch-up, Quack e projétil
real atingindo inimigo bound, recompensa/XP, último golpe e derrota no resultado,
aviso congelado do chefe, carta retomando Boss, morte, abandono ativo/pausado e
falha de save sem bloquear encerramento, alinhamento físico/visual da horda e
coleta automática de XP via callbacks reais da física, sem duplicação.
O teste acelerado de cronograma desativa
scheduler/armas nesse caso; o combate é verificado em casos separados. Não simula
um playtest humano de 15 minutos ou um benchmark de hordas.

RunSceneFlowTests abre RunBootstrap, espera LakeRun pronta, confirma relógio único,
abre/equipa/fecha o loadout, verifica gravação e encerra por abandono. Usa diretório
temporário exclusivo injetado em SavePathProvider, assim como as fixtures de fluxo;
limpeza valida caminho absoluto e prefixo antes de remover. Nenhum save pessoal
foi lido ou alterado pelos testes.

Capturas Logs/t060-scene.png e Logs/t060-loadout.png foram inspecionadas. Mostram
Pato, terreno/refúgios, HUD, controles e loadout equipado. São capturas de componentes
no teste, não evidência de aceite de percepção/balanceamento. A captura de início
em 00:00 não pretende mostrar uma horda já em combate.

Na primeira rodada focada passaram 9/11: dois testes exigiram ajustar a preparação
de posição física e vida restante do alvo (Quack aprimorado causa 4, Formiga tem 8).
Na primeira suíte completa passou 161/162; a falha era a expectativa antiga do HUD
“FIM DO TESTE”, atualizada para DERROTA. A revisão física adicional encontrou um
atraso real de 0,45 unidade entre Rigidbody2D e Transform após um passo: a posição
física já mudava, enquanto apresentação/mira aguardavam o próximo passo da física.
O scheduler agora sincroniza posição/rotação no mesmo frame antes das consultas de
armas. Os dois novos casos cobrem essa integração e coleta de XP; a suíte completa
terminou com 164/164.

Comandos Unity: -executeMethod QuackQuack.Editor.LakeRunSetup.Build com
-batchmode -nographics -quit; testes com -batchmode -runTests -testPlatform
EditMode|PlayMode -testResults <arquivo> -logFile <arquivo>. EditMode usou
-nographics, PlayMode manteve gráficos/áudio. Saídas: TestResults/t060-red.xml,
t060-editmode.xml, t060-playmode.xml e Logs/t060-*.log.

## Limites do aceite

Não houve playtest humano completo, build Windows/Android, aparelho real ou perfil
de desempenho. Testes de componentes e cronograma não aprovam justiça dos padrões,
força da build, legibilidade nas quatro proporções, FPS/GC ou metas de 200/400 inimigos.
Esses aceites permanecem nas tarefas de validação/plataforma/desempenho.

T061 ainda precisa apresentar o resultado completo e compor o progresso persistido
de carteira/recordes/desafios com extensão versionada do save. Nesta entrega, o fim
é identificado no HUD e pelo snapshot; para reiniciar no Editor, sair de Play e
abrir RunBootstrap novamente. Bootstrap/Lake e FrogKingPractice continuam disponíveis
como fixtures independentes, além da nova partida integrada.
