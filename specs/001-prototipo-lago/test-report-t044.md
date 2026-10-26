# Validação de T044

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T044 concluída. UpgradeSelectionView apresenta até três cartas com tipo
(nova arma/passiva, melhoria ou evolução), nome, números do nível atual/próximo,
sinergia, requisito de evolução e recursos restantes. Escolher, banir, rerrolar e
pular enviam comandos ao UpgradeSystem com o offerId exibido. A view não decide
elegibilidade, gasta recursos ou aplica regras; as regras da partida continuam
em UpgradeSystem/BuildState.

Snapshots imutáveis no Core separam a apresentação dos assets e sistemas. O bridge
deriva descrições do catálogo validado e recompõe o snapshot somente quando muda
oferta, fase, build, nível ou recursos. A view reutiliza seus três slots, oculta
os ausentes, desabilita recursos esgotados, fecha fora da seleção e impede dois
comandos no mesmo frame. Bind/rebind não acumula listeners. O primeiro botão
recebe foco e a entrada de UI usa EventSystem/InputSystemUIInputModule.

A composição LakeBuildProgression foi adicionada à cena por ferramenta Editor.
Bootstrap agora oferece Bumerangue inicial, pool de projéteis com capacidade 32,
três pickups cianos de dez XP e cartas ligadas à build real. Limiares 10/20/30
ficam serializados na cena, não no controlador. O pequeno percurso permite três
seleções; a carta de evolução é testada com build preparada. Recompensas de XP de
inimigos e a partida completa não foram antecipadas.

UpgradeSelectionSetup gera os prefabs UpgradeSelection e ExperiencePickup e
atualiza somente a raiz Build progression T044 de Lake. O GUID da cena
fb86267c7de2e40479ccd6bec2e07d8a foi preservado, com comparação do .meta antes da
cópia. Não houve edição manual dos assets/cena serializados. O Canvas do modal
tem referência 1920x1080; isso não conclui Safe Area nem responsividade do HUD.

Seis testes PlayMode novos cobrem coleta física de XP e clique único, metadados
dos três tipos de carta, foco, rerrolagem/banimento/pulo, callbacks obsoletos,
rebind sem duplicação, uma única carta, evolução sem novo slot, submit de teclado
e ocultação ao abandonar. O teste de teclado injeta eventos no módulo real e usa
o mesmo roteamento para Game View adotado pelos testes existentes em batchmode,
restaurando as configurações e removendo o dispositivo ao terminar.

A primeira regressão teve cinco falhas: contagem antiga de pickups, dois testes
de feedback manual contaminados por impactos de armas automáticas, submit
sintético sem roteamento apropriado e aquisição de projétil após descarte do
pool. As fixtures US1 agora distinguem os três pickups de XP e isolam feedback
manual do Quack. O runner foi corrigido para encerrar atualizações quando o pool
está descartado; não se removeu a verificação de ciclo de vida. A entrada sintética
foi corrigida no teste e a configuração redundante de ações UI foi removida.
PlayerController também descarta bordas de comando na retomada da seleção.

| Suíte final | Aprovados | Falhas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 208 | 0 | 0 | 0 |
| PlayMode | 70 | 0 | 0 | 0 |

Execução batchmode/nographics/runTests em Temp/T033Validation. Geração pelo método
QuackQuack.Editor.UpgradeSelectionSetup.Build. TestResults/t044-editmode.xml,
TestResults/t044-playmode.xml e TestResults/t044-keyboard.xml registram as
execuções finais; logs correspondentes em Logs/. Sem erros de compilação ou
exceções de gameplay na execução final. Evidências regeneráveis ficam fora do Git.

Prévia Logs/t044-preview.png renderizada e inspecionada em 1280x720, com gráficos
habilitados por UpgradeSelectionSetup.Preview. Os três cartões, campos e botões
ficam visíveis/legíveis, sem corte de texto observado. A prévia usa oferta preparada
no Editor (nível um); não é captura de playtest humano. Não houve validação
auditiva, Safe Area ou outras proporções/aparelhos.

Scripts, prefabs e cena gerados pelo Unity foram copiados com seus .meta. Espaços
de campos vazios serializados permanecem como emitidos pelo Unity; scripts,
documentação e asmdef passam na verificação de whitespace. A plataforma de
entrada passou a referenciar uGUI para compor o módulo de UI, sem ciclo entre
Presentation e Runtime.

A tela consulta os bônus autorais das passivas; isso não constitui aceite da
aplicação/agregação de todos os modificadores no combate, balanceamento ou US2
completa. Feedback de evolução/recompensa rara permanece na T045. Não houve build
de plataforma, medição de desempenho ou teste humano. T045 em diante continuam
pendentes.
