# Validação física de entrada e menus — T014

**Data**: 04/11/2026. **Código-base inicial**: e016c39 (T013); correções R1/R2 descritas abaixo. **Estado**: validação parcial registrada;
T014 permanece aberta até concluir a matriz obrigatória. T075 da feature 001 não muda.

## Resumo atual da rodada

Xbox Series/Windows Bluetooth aprovado em C01–C09, após retestes físicos das correções
de ordem da pausa e acesso às ações de resultados. DualSense/Windows também aprovado;
8BitDo/Windows também aprovado. Xbox/S23 Bluetooth aprovado em C01–C10;
DualSense/S23 Bluetooth também aprovado em C01–C10.
8BitDo/S23 não reconhecido na tentativa OTG, causa ainda não isolada. Por decisão do usuário, essa combinação fica pendente por enquanto; manter Xbox e DualSense como validações aprovadas. T014 permanece aberta.

R2: 399/399 PlayMode completo; 63/63 em cada player IL2CPP Windows/Android. Builds
normais R2 geradas com sucesso, com duas correções localizadas e prefab de resultados
atualizado pelo Unity. Nenhum teste sintético substitui as células físicas pendentes.

## Preparação comprovada

Unity 6000.4.3f1, IL2CPP Windows x64 e Android ARM64. As builds normais foram geradas
por PrototypeBuildPipeline.Build/CreateOptions, com RunBootstrap e LakeRun, sem test
runner, input sintético, invulnerabilidade, aceleração de partida ou menu de depuração.
O helper de nome/pacote existe somente na cópia isolada Temp/Polimento002Validation.
Nenhum código, prefab, cena ou ProjectSettings do produto foi alterado para preparar
esta validação. Comparação de todos os arquivos em Assets/QuackQuack encontrou somente
a diferença regenerável de prefiltragem de shaders no LakeURP.asset da cópia, já
conhecida nas rodadas anteriores. Não foi copiada de volta.

As duas builds encerraram com exit 0/PROTOTYPE_BUILD_OK. Saídas locais ignoradas:

- Windows: Temp/Polimento002Validation/Builds/T014/Windows/QuackQuackT014.exe.
- Android: Temp/Polimento002Validation/Builds/T014/Android/QuackQuackT014.apk.
- Logs de build: Logs/Polimento002/T014/build-Win64.log e build-Android.log.

Nome QuackQuack T014; pacote Android com.cemggames.quackquack.validation002.t014,
separado do app normal T007 e dos runners T011–T013. APK instalado com sucesso no
S23 Ultra, modelo SM-S918B, Android 17. Não foi apagado o save ou perfil dos apps
anteriores. A janela Windows foi observada na escolha real de melhoria N2; isso prova
inicialização/abertura do modal, não aceite dos comandos físicos.

SHA-256 dos artefatos (o executável sozinho é o lançador; GameAssembly identifica o
código nativo, em conjunto com o commit e dados da build):

- QuackQuackT014.exe: 18D5D1AEA4095FBF1A99FB556003FB53C0D7600E147698664779E600D7350CBB
- GameAssembly.dll: CEE23A1069454FD522E553367ED54391419BD8EB93F7DEA15CA93FB97276FF58
- QuackQuackT014.apk: 7628E5200E2ECD9B22BA131FE9B489A40E9FEF7852A9BDC6BC02FFDAD39A596C

## Hardware e disponibilidade

Modelos/conexões informados pelo usuário nesta rodada:

| Controle | Windows | S23 Ultra |
|---|---|---|
| Xbox Series | Bluetooth disponível | Bluetooth disponível |
| PlayStation DualSense | Bluetooth disponível | Bluetooth disponível |
| 8BitDo, nome do sistema Ultimate C 2.4G (informado inicialmente como 2C) | Receptor 2,4 GHz/XInput aprovado | OTG/B + Home: tentativa não reconhecida, causa ainda não isolada |

Inventário Windows relatou Xbox Wireless Controller / Bluetooth / Status OK. Esse
inventário não substitui confirmação de entrada no player. Serial, endereço Bluetooth,
IP e identificadores de instância não são registrados neste relatório.

A combinação 8BitDo/Android permanece **não aprovada**. Há OTG disponível, mas a
tentativa não reconheceu o controle e a causa ainda não foi isolada; aguarda teste do
adaptador e estado do pareamento. Não atribuir falha ao jogo nem aprovar essa célula
por resultados Xbox/DualSense, simulação ou teste Windows.

## Critérios da rodada

- C01: mover, Quack, pausa/retomada, uma ação por pressionamento e comandos neutros.
- C02: atalho de Relíquias e navegação; fechar retoma somente a pausa de que é dono.
- C03: cartas/banir/rerrolar/pular alcançáveis com foco; Cancel não resolve escolha
  obrigatória e retorna de BANIR para sua carta; confirmar aplica uma vez, sem Quack.
- C04: CONTROLES acessível sem Select, família/rótulos reais, edição de atalho e
  persistência; restaurar padrões antes de trocar de modelo.
- C05–C09: desconectar com analógico/botão pressionado e reconectar em gameplay,
  pausa, Relíquias, escolha de melhoria e resultados; não repetir ação nem perder
  contexto/foco. Em resultados não retornar à partida por atalho de gameplay.
- C10 Android: controle disponível oculta touch; perda restaura movimento/Quack no
  contexto permitido, sem gesto antigo; menus de escolha/resultados continuam seguros.

A execução física e o relato do usuário serão registrados por combinação/contexto.
Pendência, não testado e falha são estados distintos; nenhuma confirmação ampla será
estendida a cenário não perguntado/testado.

## Evidência complementar anterior

T013: 491/491 EditMode, 389/389 PlayMode, 123/123 no Windows IL2CPP e 121/121 no Android
IL2CPP no S23; zero falhas/ignorados. Evidência histórica em menu-integration.md e
TestResults/Polimento002/T013. Entradas sintéticas: não encerram a matriz física T014.
Nenhuma repetição das suítes foi necessária só para gerar estas builds sem alterar
fontes. Não há novo profiling, aceite artístico ou playtest de 15 minutos nesta task.

## Situação registrada ao aguardar o primeiro relato físico

| Combinação | Estado do teste físico |
|---|---|
| Xbox Series / Windows Bluetooth | Build aberta; relatos C01–C03 solicitados, ainda sem confirmação do usuário |
| DualSense / Windows Bluetooth | Não executado nesta rodada |
| Genérico / Windows receptor 2,4 GHz | Não executado; marca/modelo ainda não informado |
| Xbox Series / S23 Bluetooth | APK instalado; teste físico ainda não executado |
| DualSense / S23 Bluetooth | APK instalado; teste físico ainda não executado |
| Genérico / S23 | Não coberto: usuário informa incompatibilidade/indisponibilidade |

A janela Windows também foi observada na pausa com CONTROLES selecionado. Não houve
injeção de teclas/cliques/comandos de controle pelo agente na partida. O computador
indicou interação do usuário; as capturas não identificam por si só o dispositivo ou
confirmam todos os critérios. Permanecem pendentes os relatos físicos solicitados,
C04 e hotplug C05–C09, restauração de toque C10 e as demais combinações disponíveis.

Próximo bloco: confirmar, no Xbox/Windows, navegação/Cancel/confirmar na melhoria,
movimento/Quack/Start/View e propriedade da pausa em Relíquias; depois testar perfil,
hotplug nos cinco contextos e resultados, antes de trocar de controle/plataforma.
As builds normais permanecem disponíveis nos caminhos acima; T014 não foi marcada
como concluída e nenhuma task posterior foi executada.

## Relato físico Xbox Series / Windows Bluetooth — build inicial

O usuário confirmou nesta rodada, sem mouse:

- Direcional e analógico alcançam todas as ações da melhoria, com contorno visível.
- B em BANIR retorna à carta sem encerrar a escolha; A escolhe uma única vez.
- Movimento, Quack/A, Start/Menu e Relíquias/View funcionam corretamente.
- Abrir/fechar Relíquias durante simulação retoma; abrir durante pausa conserva a pausa.

**Falha encontrada**: na pausa, cima/baixo do direcional e analógico percorrem uma ordem
contrária à disposição dos botões. Todas as opções são alcançáveis, mas a direção é
incoerente. O usuário não explicitou isoladamente ausência de Quack automático após
confirmar uma melhoria; esse detalhe permanece para reconfirmação.

Regressão adicionada antes da correção: PauseVerticalNavigationMatchesScreenOrder,
quatro casos (DpadDown/DpadUp/StickDown/StickUp), 0/4 aprovados, mesmas quatro falhas
na navegação real do InputSystemUIInputModule. O teste também confere a ordem visual
dos widgets; não é apenas uma inspeção dos links. XML pause-direction-before.xml,
exit 2. A causa é a lista [Continuar, Relíquias, Controles, Abandonar], sem relação
com a ordem vertical autorada. Correção localizada para [Relíquias, Continuar,
Abandonar, Controles]; não inverter eixos do dispositivo nem mudar os demais menus.
A próxima build normal usa Builds/T014R1, preservando a inicial e seus hashes acima.

### Reteste físico da correção — Xbox / Windows R1

O usuário respondeu "perfeito" ao reteste de cima/baixo do direcional e analógico
na sequência visual Relíquias → Continuar → Abandonar → Controles. A falha de
navegação da pausa está corrigida também no teste físico desta combinação.
Confirmou explicitamente: escolher uma carta retoma sem Quack automático.

Regressão após a correção: 52/52 PlayMode e 52/52 no Windows IL2CPP, zero falhas e
ignorados, exit 0. Inclui os quatro casos de direção (todos os botões, direcional e
analógico), mais fluxos de Relíquias, melhoria e entrada unificada. Logs/XML em
Logs/Polimento002/T014 e TestResults/Polimento002/T014: pause-direction-after e
windows-r1-tests. Suítes completas T013 permanecem evidência histórica; não foram
repetidas para esta troca localizada de ordem.

Build normal Windows R1 gerada com sucesso por PrototypeBuildPipeline, sem runner
ou alteração de regras. Caminho: Temp/Polimento002Validation/Builds/T014R1/Windows/
QuackQuackT014.exe. SHA-256 GameAssembly.dll:
15CE8EB59095AD10DCF25724C904A50CF73AF71FEC18614B4C8979BBC439C7F5.
A janela foi observada a partir desse executável, não da build inicial.

Xbox/Windows: C01/C02 e C03 confirmados no relato inicial e no reteste dirigido;
C04 (perfil/novo atalho) e C05–C09 (hotplug por contexto) estão solicitados e ainda
aguardam relato. As demais combinações e C10 Android seguem pendentes.

### Perfil, hotplug e achado em resultados — Xbox / Windows R1

O usuário confirmou o perfil automático e os rótulos esperados, a mudança do atalho de
Relíquias, seu uso no controle, retenção do valor ao reabrir a configuração e RESTAURAR
PADRÕES (C04). Relatou que desligar/ligar funcionou sem eventos inesperados. O relato
não enumerou os contextos individualmente; a discriminação C05–C09 ainda será confirmada,
sem exigir repetição do que ele já realizou.

**Falha nova em resultados**: controle alterna entre barras com esquerda/direita e
rola com cima/baixo, mas não chega aos botões. Usuário precisou do mouse. O botão
TENTAR SALVAR deve permanecer indisponível quando salvo; JOGAR NOVAMENTE precisa ser
alcançável. Causa confirmada no uGUI local: Scrollbar em navegação Automatic retorna
null nos links do próprio eixo e consome cima/baixo como rolagem; busca espacial não
criou uma saída horizontal até as ações. ResultsView também não preparava contornos.

Regressão results-before.xml: cinco novos casos, um aprovado e quatro falhas esperadas,
exit 2. Falharam alcance horizontal das ações com save pendente/concluído e retorno das
barras por Cancel no direcional/analógico. A regra de rolagem existente foi preservada.
A R2 cria links explícitos horizontais incluindo colunas e ações disponíveis, mantém
cima/baixo para rolar e usa Cancel para focar ações sem ativá-las. AutoHide exige atualizar
links após o layout; a primeira correção deixou um link transitório para barra escondida
(66/67); foi acrescentada atualização após o layout do Canvas, mantendo essa regressão.
Acrescentado também teste de retry por Submit que salva sem reiniciar implicitamente.
ResultsScreen.prefab é atualizado via ResultsSetup.UpdateNavigation no Unity, sem
recriar a cena ou alterar regras de save/recompensa.

A R1 passou também 52/52 no runner Android IL2CPP (exit 0, zero ignorados), em pacote de
testes separado com.cemggames.quackquack.validation002.t014tests. Seu APK normal foi
gerado com sucesso; o reteste físico Android aguarda a versão com resultados corrigidos.

### Confirmação de cobertura do hotplug Xbox / Windows

O usuário esclareceu: "Incluiu todas", em resposta à enumeração dos cinco contextos
(partida, pausa, Relíquias, melhoria, resultados) e à tentativa segurando analógico/
Quack. C05–C09 têm relato positivo de desligar/religar sem eventos inesperados na R1;
o acesso às ações de resultados segue em reteste específico na R2 devido à falha de
navegação identificada. Não solicitar repetição integral dos demais casos já aprovados.

### Validação automatizada final da R2

- PlayMode completo: 399/399, zero falhas/ignorados, exit 0 (r2-PlayMode.xml).
- Windows x64 IL2CPP: 63/63, zero falhas/ignorados, exit 0 (windows-r2-tests.xml).
- Android ARM64 IL2CPP no S23: 63/63, zero falhas/ignorados, exit 0 (android-r2-tests.xml).

Os 63 casos incluem ControlSettingsFlowTests (12), RelicLoadoutFlowTests (16),
UpgradeSelectionTests (17), UnifiedPlayerInputTests (7) e ResultsSceneFlowTests (11).
Os runners R2 usam QuackQuack T014 Tests e, no Android, pacote .t014tests, separados
da build normal de teste manual. Os seis novos casos de resultados incluem os dois
estados de save, navegação horizontal a partir de cada coluna, rolagem vertical por
direcional/analógico, Cancel, reparo de foco/hotplug, barras ocultas e retry real por
Submit sem reinício involuntário. Os quatro testes novos da pausa permanecem aprovados.

Build normal Windows R2 gerada com sucesso em Builds/T014R2/Windows. SHA-256 de
GameAssembly.dll: 5585342DD300829FF4638F3C113417D4291BCB5E66D100E3FB02DD2D1A15DB5C.
A primeira abertura foi encerrada antes do reteste; usuário indicou fechamento manual
e o log mostrou saída normal, sem exceção do jogo. R2 reaberta e observada na partida;
retorno físico de resultados/ações/reconexão solicitado. Não classificar o encerramento
como crash nem declarar esse reteste aprovado antes do relato.

### Reteste físico dos resultados — Xbox / Windows R2

O usuário confirmou que, a partir de uma barra, esquerda/direita alcança JOGAR NOVAMENTE
com contorno, cima/baixo continua rolando e B foca as ações. A inicia uma única partida;
TENTAR SALVAR permanece indisponível quando o progresso está salvo. Confirmou também
que desligar/religar nesta tela mantém o foco utilizável, sem ativar a ação sozinho.
O bloqueio de navegação de resultados está corrigido fisicamente nesta combinação.

**Xbox Series / Windows Bluetooth aprovado no roteiro C01–C09**: comando básico,
propriedade da pausa, melhoria sem Quack automático, perfil/atalho/restauração, hotplug
nos cinco contextos com analógico/Quack pressionado e retestes R1/R2 das duas falhas.
Não extrapolar este resultado para os outros modelos ou Android. Roteiro do DualSense
no Windows solicitado; testes desse modelo e do genérico ainda aguardam retorno.

### Artefatos R2 e integridade

APK normal R2 gerado com exit 0/PROTOTYPE_BUILD_OK em
Temp/Polimento002Validation/Builds/T014R2/Android/QuackQuackT014.apk. SHA-256:
5E844DEBFFD0054808756016D3249E57A45B2A89FB7AB6995F4FDE6ED31AF285.
Usa o mesmo pacote normal de validação .t014; o runner .t014tests é separado.
Nenhuma credencial, serial, log, resultado XML, cache, screenshot ou build é versionado.

Fontes/asset abaixo conferidos por SHA-256 entre raiz e cópia que produziu os testes
e as builds. .meta existentes preservados; apenas ResultsScreen.prefab precisou ser
atualizado pelo Unity na R2. Cenas e ProjectSettings do produto permanecem inalterados.
- Gameplay/Upgrades/ControlSettingsMenu.cs: 4E085CAD2F8F1570D7025AC8F22569BA9F5980D8B1124D22A59BB3B2EA20132D
- Presentation/UI/ResultsView.cs: 57633CDC6C5B7F37547F8619D6FD1EA189DB12E2B02225D770DF4F67496C5317
- Editor/ResultsSetup.cs: 9C383CF2F396903CA2575F5FEEE662A39D44F235E880EBB53BD7D8B09266A7B5
- Prefabs/Prototype/ResultsScreen.prefab: EF09A0D129631C7AC9A5C05F2AB7D4C6FFCF761D5DBA358A25CCF286B6B03DA6
- Tests/PlayMode/ControlSettingsFlowTests.cs: 4E76E6C13C1EDAD00CD5719DB9E1687261FC97CE42ED251A24C2E3CE19D2E4EE
- Tests/PlayMode/RelicLoadoutFlowTests.cs: 334A252E46B3BAE42E235B869D59BAAAD4DDB0DFD5111B1D7971A7020AE19170
- Tests/PlayMode/ResultsSceneFlowTests.cs: 95978486C829A2C991088D7F01997DA524A967083C3F826A8888DC329B96607D

### DualSense / Windows Bluetooth — R2

O usuário confirmou "Tudo funcionando corretamente" para o roteiro sem mouse de
movimento/Cross, Options, Create/Share, propriedade da pausa e navegação/confirmar/
retornar nas melhorias, pausa, Relíquias e resultados, com foco/rótulos adequados.
Confirmou também o segundo roteiro de troca/restauração de atalho e desligar/religar
nos cinco contextos, incluindo analógico/Quack pressionado, sem relatar exceções.
**DualSense/Windows aprovado no roteiro C01–C09 relatado.**
Genérico/Windows solicitado na sequência; genérico/Android continua não coberto.

APK R2 atualizado com sucesso no S23 via instalação -r, mantendo o pacote normal de
validação .t014. Não foi aberto para teste físico Android enquanto os controles estão
em uso no Windows. Etapa Android pronta; dados/apps anteriores preservados.

### Identificação e modo do terceiro controle no Windows

Usuário informou inicialmente "8BitDo Ultimate 2C 2.4G"; o Windows apresenta
"Ultimate C 2.4G Wireless Controller" e o usuário confirmou ausência de seletor
2.4G/Bluetooth. Registrar essa identificação do sistema e a divergência do nome
informado, sem inventar uma revisão de hardware. O procedimento do Ultimate C foi
consultado no manual/FAQ oficial:

- [Windows — Ultimate C 2.4G](https://manual.8bitdo.com/ultimate-c-2.4g-controller/ultimate-c-windows-2.4g.html).
- [FAQ — modos XInput/DInput e Android](https://support.8bitdo.com/faq/ultimate-c-2.4g-wireless-controller.html).

Antes do ajuste, consulta isolada do Unity registrou gamepads=0 e joysticks=0
(8bitdo-inventory.log, helper Editor somente na cópia, sem injeção de entrada).
Após ligar com X + Home, o usuário confirmou que passou a funcionar corretamente.
Reconhecimento/movimento foi restabelecido com o modo Windows indicado pelo fabricante;
nenhuma mudança de código ou firmware foi feita para esse achado. Os roteiros completos
C01–C09 foram retomados; não aprovar todos com base apenas no reconhecimento.

O manual também prevê Android com receptor USB em modo B + Home. Foi solicitada a
confirmação de adaptador USB-C/OTG antes de manter ou retirar a pendência Android.
Não foi confirmado Bluetooth nesse modelo nem inferida incompatibilidade definitiva
com S23 a partir da tentativa inicial. O resultado Windows será específico ao modo
XInput e a este controle, sem extrapolar para DirectInput/Joystick ou outros modelos.

### 8BitDo / Windows 2,4 GHz, modo XInput — R2

Após o ajuste de modo, o usuário confirmou "tudo funciona" para comandos, propriedade
da pausa, navegação/confirmar/retornar nos quatro menus e alteração/restauração do
atalho de Relíquias. Confirmou também o roteiro de retirar/recolocar o receptor nos
cinco contextos com analógico/Quack pressionado, sem exceções relatadas.
**Terceiro controle/Windows aprovado em C01–C09 no modo XInput recomendado.**

Usuário confirmou disponibilidade de adaptador USB-C/OTG. A combinação 8BitDo/Android
passa de não coberta a teste pendente pelo receptor, em B + Home conforme manual;
não é necessário adquirir hardware. As três combinações Windows estão aprovadas.

### Tentativa inicial 8BitDo / S23 por OTG

Com receptor no OTG e B + Home, usuário informou que não funcionou, acrescentando que
o adaptador pode estar com problema. O LED/estado de pareamento e o funcionamento do
adaptador ainda não foram isolados. Classificação: **não reconhecido na tentativa,
causa indeterminada**; não atribuir falha ao jogo nem aprovar Android desse modelo.
Solicitado teste do adaptador com periférico USB conhecido e confirmação de desligar
completamente antes de trocar o modo. Em paralelo, iniciado roteiro Xbox/S23 Bluetooth,
que não depende do OTG. A porta em modo host deixou o aparelho indisponível no ADB;
o app normal R2 já estava instalado e pode ser aberto manualmente.

### Atualização do diagnóstico OTG e início Xbox / S23

Usuário informou receptor e controle com LED aceso, porém sem mover o Pato e sem
conseguir interagir com o próprio Android por esse controle, ao contrário dos demais.
Isso não isola a causa: alimentação/LED não comprova tráfego USB, pareamento ou
compatibilidade do modo; ainda não há confirmação do OTG com periférico conhecido.
Não classificar como defeito do jogo nem declarar adaptador defeituoso sem essa prova.

Xbox Series por Bluetooth no S23: usuário confirmou funcionamento, ocultação dos
controles touch e identificação automática da família Xbox, sem ajuste manual.
O roteiro completo de menus/perfil/hotplug nos cinco contextos e restauração do toque
foi solicitado; não estender o aceite básico a esses cenários antes do retorno.
DualSense/S23 continua pendente.

### Xbox Series / S23 Bluetooth — R2

Após o aceite básico/família automática, o usuário confirmou "tudo funcionou" para
Quack, pausa, Relíquias/propriedade da pausa, navegação/confirmar/retornar nos quatro
menus, perfil/atalho/restauração e ausência de Quack automático na escolha.
Confirmou "tudo funcionou corretamente" para desligar/religar nos cinco contextos,
com analógico/Quack pressionado, preservação de foco/contexto e retorno do toque na
partida sem controle, ocultando novamente ao reconectar. Não relatou exceções.
**Xbox Series/S23 Bluetooth aprovado em C01–C10.**
DualSense/S23 solicitado na sequência. 8BitDo/S23 permanece não aprovado e sem causa
isolada na conexão OTG; não extrapolar o resultado Bluetooth para esse receptor.

### DualSense / S23 Bluetooth — R2

O usuário confirmou que o DualSense funcionou corretamente para o roteiro de
movimento/Cross, Options, Create, propriedade da pausa, menus de melhoria/pausa/
Relíquias/resultados e alteração/restauração de atalho, com foco/confirmar/retornar e
rótulos. Não explicitou se a família foi automática ou selecionada manualmente;
registrar funcionamento/rótulos aprovados, sem inventar esse detalhe de identificação.
Confirmou "tudo funcionou" para desligar/religar nos cinco contextos, incluindo
analógico/Quack pressionado, preservação de foco/contexto, retorno funcional do toque
na partida e ocultação novamente ao reconectar. Não relatou casos não testados.
**DualSense/S23 Bluetooth aprovado no roteiro C01–C10 relatado.**

Cinco combinações aprovadas: Xbox, DualSense e 8BitDo/XInput no Windows; Xbox e DualSense
por Bluetooth no S23. Única combinação restante: 8BitDo/S23 pelo receptor OTG, ainda
não reconhecido na tentativa e com causa não isolada. Solicitados teste do OTG com
mouse conhecido e novo pareamento do receptor no modo Android conforme FAQ oficial.
Isso não encerra a T014 nem a T075 anterior; não foi alterado código nesta atualização.

### Encerramento parcial solicitado pelo usuário

O usuário informou que não possui periférico USB conhecido para testar o OTG e pediu
para deixar o controle genérico pendente no S23. Reforçou que, por enquanto, devem ser
mantidas as validações com Xbox e DualSense. Não foi solicitado continuar o diagnóstico
ou repetir o pareamento; esses passos não foram confirmados como executados.

| Controle | Windows | S23 Ultra |
|----------|---------|-----------|
| Xbox Series | Aprovado C01–C09 | Aprovado C01–C10, Bluetooth |
| DualSense | Aprovado C01–C09 | Aprovado C01–C10, Bluetooth |
| 8BitDo (identificado como Ultimate C) | Aprovado C01–C09, XInput/receptor | Pendente por decisão do usuário; tentativa OTG sem resposta, causa não isolada |

Preservar a evidência já obtida no Windows para o 8BitDo, sem estender o aceite ao
Android. A T014 continua com checkbox aberto pela combinação restante. Não há mudança
de código ou novo aceite da T075 nesta atualização. Revisão documental e verificação
de whitespace são suficientes para este registro; os testes anteriores continuam
identificados acima e não foram repetidos para uma alteração apenas documental.