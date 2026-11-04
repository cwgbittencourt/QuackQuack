# Disponibilidade de controle — T008/T011

Estado atual: T011 concluída; as 15 falhas de disponibilidade da T008 foram corrigidas. As seções anteriores à T011 preservam o histórico de regressões antes da correção.

**Data**: 04/11/2026. **Base**: 16978fa (T007).
**Escopo**: testes antes da implementação FR-013; nenhuma correção de runtime nesta task.
Arquivo: Assets/QuackQuack/Tests/PlayMode/ControllerAvailabilityTests.cs.

## Contrato exercitado

Disponibilidade de controle conectado/habilitado deve ocultar joystick e Quack touch
independentemente da última atividade. Ao perder o último controle, restaurar touch
respeitando o contexto, sem ressuscitar movimento/bordas antigas e sem reiniciar o consumidor.
Um segundo controle ainda habilitado mantém a supressão. UI de menus permanece ativa.

24 casos, categoria ControllerAvailabilityRuntime, sem Ignore/Explicit ou mecanismo que
converta falha esperada em sucesso. Testes observam comandos públicos, controles gráficos
do prefab real, eventos do Input System e estado do EventSystem. Não exigem campo/API de
disponibilidade que ainda não existe, nem acessam campos privados por reflexão.

## Composição e isolamento

- UnifiedPlayerInput e MobileControls reais, teclado/tela touch/gamepads sintéticos.
  Touchscreen sintético compõe mobile também no runner Windows sem fingir Application.
- Dispositivos físicos já presentes ficam temporariamente desabilitados **apenas no
  InputSystem do runner** e são restaurados com suas condições/currents originais.
  Não desabilita hardware no sistema operacional ou em outra instância do jogo.
- Dispose, destruição diferida dos clones/dispositivos e restauração de InputSettings
  executados em UnityTearDown mesmo quando as asserções falham. EventSystem da fixture
  é preservado no hotplug e destruído no cleanup.
- Verifica Added/Enabled/Disabled/Removed efetivamente emitidos antes das asserções de
  produto. DeviceRemoveEvent também testa remoção vinda da fila de runtime.
- **DeviceRemoveEvent de gamepad sintético emite Removed, não Disconnected**: conforme
  InputManager do pacote 1.19.0 instalado, Disconnected é adicional para dispositivo nativo
  com descrição. Não marcar esse caso como prova de desconexão física; esta segue para
  T014. Não adulterar flags nativas privadas ou chamar callbacks internos para inventar evento.

## Matriz

| Grupo | Casos | Exigência |
|---|---:|---|
| Sem controle / controle previamente desabilitado | 2 | Touch visível e comandos novos funcionam; dispositivo sintético mobile não conta como gamepad |
| Presente antes da composição, Added neutro, Enabled neutro e atividade de teclado | 4 | Ocultar ambos os controles touch sem exigir atividade física ou Source=Gamepad |
| Último controle Removed/Disabled/DeviceRemoveEvent | 3 | Nenhuma borda replay, novo Quack não reativa gesto touch antigo e novo movimento funciona |
| Gameplay/Paused/Relics/ChoosingUpgrade/Results × Removed/Disabled | 10 | Suprimir quando disponível, restaurar visibilidade anterior, contexto de comandos e EventSystem/navegação preservados |
| Dois controles × Removed/Disabled | 2 | Só restaurar touch após indisponibilidade do último habilitado |
| Readição neutra e Disabled→Enabled com bordas pendentes | 2 | Não repetir comandos antigos, aceitar novo gesto/pressão |
| Dispose seguido de hotplug | 1 | Não reviver mobile destruído nem gerar erro/evento tardio |

A matriz de contexto usa SetContext e verifica máscara de comandos/visibilidade/navegação.
Relics usa os mesmos flags de pausa; ChoosingUpgrade e Results bloqueiam gameplay/pausa.
**Não é teste de foco dos cinco menus reais**: integração/foco/atalhos pertencem a T009,
T010/T013 e validação física T014. Visibilidade após perda é comparada ao contexto anterior,
sem impor que joystick de gameplay apareça em resultados/modal.

## Validação em andamento

Cópia isolada Temp/Polimento002Validation, Unity 6000.4.3f1 Windows Editor PlayMode.
Primeiro ensaio descobriu 24 casos: 9 passaram/15 falharam. Um erro da fixture impedia
registrar Added, pois a notificação é síncrona antes de AddDevice retornar; observador
ajustado para coletar eventos Gamepad desde a criação. Essa falha inicial não é contada
como evidência de bug no produto. A rodada final abaixo deve usar o observador corrigido.

Logs/XML em Logs/Polimento002/T008 e TestResults/Polimento002/T008, ignorados.

## Resultado Editor após corrigir a fixture

Suíte completa: **319 descobertos, 304 aprovados, 15 falhas, zero ignorados**, exit 2,
em TestResults/Polimento002/T008/PlayMode.xml. Todos os **295 testes anteriores passaram**;
as 15 falhas pertencem exclusivamente a ControllerAvailabilityTests. Nessa fixture:
**24 casos, 9 aprovados e 15 vermelhos**.

| Falhas normativas | Quantidade | Evidência |
|---|---:|---|
| Disponibilidade neutra/inicial/Enabled e teclado mais recente | 4 | Joystick continua visível quando esperado oculto; a verificação também exige ocultar Quack |
| Gameplay/Paused/Relics × Removed/Disabled | 6 | Touch não oculta quando controle disponível, independentemente do contexto |
| Último controle indisponível nas três formas de remoção | 3 | Um novo Quack touch retorna Move=(-1,0), herdando o gesto antigo, quando deveria ser zero |
| Um dos dois controles ainda disponível | 2 | Touch continua visível quando deveria permanecer suprimido |

Os nove casos positivos verificam touch sem controle, controle desabilitado antes da
composição, contextos bloqueados ChoosingUpgrade/Results (quatro casos), reintrodução
neutra, descarte de bordas pendentes e Dispose seguido de hotplug. Nenhuma falha de
compilação/setup/teardown está sendo contada como defeito de disponibilidade na rodada final.

Não implementar a solução nesta task: separação disponibilidade/atividade e limpeza no
hotplug pertencem à T011, após T009/T010. Os testes antigos de toque em UnifiedPlayerInputTests
criam um gamepad em SetUp mesmo nos cenários de touch; T011 deve revisar a composição de
TouchUsesAuthoredActionsWithoutBecomingAPhysicalGamepad e
DefaultSceneCreatesTouchControlsAndHidesThemDuringSelection para manter o objetivo de
toque **sem** controle disponível, respeitando FR-013, em vez de enfraquecer esta matriz.

Fonte testada na raiz/cópia, SHA-256:
086188AC4DA3A6C462175796ED369CF9C06536FB236092D2C9C118345A377BBE.
.meta gerado pelo Unity na cópia isolada e preservado: GUID
0fddaf4ae2e56fb44b76c41c8e770735. Nenhuma cena/prefab/inputactions ou código de produção mudou.

## Portabilidade executada e conclusão

A mesma fixture compilou e rodou no **player Windows x64 IL2CPP**:
24 descobertos, 9 aprovados, **as mesmas 15 falhas**, zero ignorados, exit 2.
A lista de falhas foi comparada à suíte Editor e coincide, sem erros de setup/teardown.
XML TestResults/Polimento002/T008/windows-runtime.xml; log windows-runtime.log.
Não há dependência UnityEditor/AssetDatabase; o único bloco UNITY_EDITOR salva/restaura
uma opção de encaminhamento de entrada do runner Editor. Os testes/comandos/UI usam APIs
de runtime, permitindo executar a mesma classe no player Android posteriormente.

Repetir com -runTests -testPlatform PlayMode -testFilter ControllerAvailabilityTests para
Editor; para player, -buildTarget Win64 -testPlatform StandaloneWindows64
-playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T008/WindowsTests.
Não usar -quit em -runTests. Registrar XML/log em diretórios ignorados e conferir 24 casos.

**T008 concluída como entrega de regressões antes da implementação.** A suíte continua
vermelha intencionalmente: T011 deve corrigir FR-013 e fazer os casos passarem, preservando
os positivos. T009/T010 permanecem pendentes; esta task não autoriza avançar nelas.
Não houve nova execução Android, teste físico de hotplug, validação de foco real,
profiling ou suíte EditMode completa. T014 permanece responsável pelo aceite físico de
Xbox/PlayStation/genérico nos dispositivos/contextos reais; T075 antiga não muda de estado.
## T011 — Disponibilidade independente e restauração segura (04/11/2026)

A implementação adiciona ControllerAvailable em UnifiedPlayerInput como estado separado
de Source (histórico da última atividade). Considera qualquer Gamepad adicionado e
habilitado, inclusive sem atividade. Calcula na composição e atualiza nos eventos
Added/Removed/Disconnected/Reconnected/Enabled/Disabled; não faz LINQ/coleções novas ou
varredura dos controles a cada Read. Dispose remove a assinatura de eventos.

ApplyContext combina a disponibilidade com as permissões já existentes de gameplay,
pausa/modal e suspensão. Com controle disponível, desativa o root dos controles touch e
suas ações; OnDisable limpa joystick e botões antigos. Sem o último controle, reativa
somente o que o contexto atual permite. Source não é forçado a Gamepad ao conectar ou
Touch ao desconectar: uma ação nova escolhe a fonte. Pausa/resultado/seleção não mudam de
fase/dono pelo hotplug. EventSystem de menus continua separado e ativo.

ActionPlayerInput passa também a exigir movimento neutro após perda/reset de dispositivo,
além de limpar Quack/Pause/Relics pendentes. Isso impede retomar automaticamente um
analógico antigo de outro controle ainda conectado quando o controle ativo desaparece.
Uma pressão/gesto novo funciona sem recriar a partida ou o consumidor.

Os 24 casos originais T008 foram mantidos; a asserção de neutralidade foi fortalecida
para incluir RelicsPressed introduzido na T010. Seis casos adicionais verificam:
histórico Source separado da disponibilidade; perda do controle ativo entre dois
(Removed e Disabled); composição touch tardia; suspensão durante hotplug; zero bytes
gerenciados em 100 Read após aquecimento. Esse contador cobre a leitura estável, não
FPS/GC do loop completo do jogo.

UnifiedPlayerInputTests continua verificando os mesmos comportamentos, mas seus cenários
de toque removem o gamepad sintético criado pelo SetUp. Gamepads preexistentes ficam
isolados/desabilitados no InputSystem do runner e são restaurados no teardown. Assim,
toque é testado sem controle disponível, como já previsto no relatório T008; não se
relaxaram asserções para permitir touch com um controle disponível.

Primeiro foco com a implementação: 64/64 aprovados. Foco após os seis casos adicionais e
máscara também aplicada às ações touch: **70/70 aprovados**, zero falhas/ignorados, em
TestResults/Polimento002/T011/focused-final.xml. Inclui ControllerAvailabilityTests,
UnifiedPlayerInputTests, RelicsInputTests e GamepadInputAdapterTests.

Nenhuma cena, prefab, inputactions ou .meta foi alterado. Validação na cópia isolada
Temp/Polimento002Validation, Unity 6000.4.3f1. Evidências regeneráveis em
Logs/Polimento002/T011 e TestResults/Polimento002/T011, fora do commit.

### Validação final T011

| Execução | Total | Aprovados | Falhas | Ignorados |
|---|---:|---:|---:|---:|
| EditMode completo | 467 | 467 | 0 | 0 |
| PlayMode completo | 373 | 363 | 10 | 0 |
| Windows x64 IL2CPP — entrada | 70 | 70 | 0 | 0 |
| Android IL2CPP — entrada, S23 Ultra | 68 | 68 | 0 | 0 |

Os 30 casos ControllerAvailabilityTests passam nas três plataformas. Os 10 erros da
suíte PlayMode são exatamente as regressões de menus da T009, comparadas por nome;
continuam previstas para T013. Nenhuma falha nova. EditMode e players encerraram com
exit 0; PlayMode completo com exit 2 devido a essas dez regressões conhecidas.
Android contém 22 casos RelicsInputTests (Windows contém 24), pois o layout HID
DualSense usado por dois casos só é registrado nas plataformas correspondentes.
Os controles desses testes são simulados: não aprovam hotplug físico ou modelos reais.

Resultados: EditMode.xml, PlayMode.xml, windows-runtime.xml e android-runtime.xml em
TestResults/Polimento002/T011. Android usa pacote de testes separado
com.cemggames.quackquack.validation002.t011tests, preservando o app normal T007.
Não foi produzida nova build normal nem medido desempenho global nesta task.
Aceite físico Xbox/PlayStation/genérico e contextos permanece na T014; T075 antiga
permanece aberta. T012/T013 não foram implementadas.

SHA-256 das fontes testadas, idênticas entre raiz e cópia isolada:

- UnifiedPlayerInput.cs: A0943AB6B7466984BEBA71F3988827A03D2E0A1CBFF64154546F8B0B61E9E2A6
- ActionPlayerInput.cs: 81C3D9D429F031344E73CE0C6EF411ED06A73B42168B0112AA539C5E5EEEBF51
- ControllerAvailabilityTests.cs: BCEB2EB6BF882431BEBC9AC2760FF33021FC018CC9510C5C1488D793352537D3
- UnifiedPlayerInputTests.cs: D8AC9255B162B3131BB5AC78CB8EF7646BCC5377A6FA9FA1E0E5C1D8C517C969