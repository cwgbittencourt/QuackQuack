# Validação física de entrada e menus — T014

**Data**: 04/11/2026. **Código-base**: e016c39 (T013). **Estado**: em execução;
T014 permanece aberta até concluir a matriz obrigatória. T075 da feature 001 não muda.

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
| Genérico, marca/modelo ainda não informado | Receptor 2,4 GHz disponível | Usuário informa que não funciona no S23 |

Inventário Windows relatou Xbox Wireless Controller / Bluetooth / Status OK. Esse
inventário não substitui confirmação de entrada no player. Serial, endereço Bluetooth,
IP e identificadores de instância não são registrados neste relatório.

A combinação genérico/Android está **não coberta por indisponibilidade de hardware
compatível**; não é falha atribuída ao jogo sem reprodução. Não aprovar essa célula por
resultados Xbox/DualSense, simulação ou teste genérico no Windows.

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
