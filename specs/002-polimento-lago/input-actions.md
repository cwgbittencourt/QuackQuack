# Ação comum de relíquias — T010

**Data**: 04/11/2026. **Base**: 572bfd1 (T009). Implementação da camada de entrada FR-015;
consumo do comando para abrir/fechar o menu e foco/Cancel de relíquias continuam na T013.

## Comando e bindings

PlayerCommand expõe RelicsPressed independente de PausePressed. O argumento opcional foi
acrescentado depois de Source para preservar o significado dos construtores posicionais
existentes; chamadas anteriores continuam com RelicsPressed=false. KeyboardInputAdapter e
GamepadInputAdapter recebem a nova ação por ActionPlayerInput. UnifiedPlayerInput encaminha
a borda da fonte selecionada; sua política de disponibilidade touch não mudou (T011).

| Entrada | Comando |
|---|---|
| Gamepad start | Pause; Menu em Xbox, Options em PlayStation, Start em genérico conforme layout |
| Gamepad select | Relics; View em Xbox, Share/Create em PlayStation, Select em genérico conforme layout |
| Teclado R | Relics |
| Teclado Esc | Pause, preservado |
| Sul / leste em UI | Submit / Cancel pelo InputSystemUIInputModule existente |
| Analógico / direcional em UI | Navegação pelo módulo de UI existente |

Os layouts do Input System 1.19.0 instalado normalizam os botões dos fabricantes para
start/select/buttonSouth/buttonEast. Não duplicar bindings por nome comercial para o mesmo
controle normalizado. Famílias/glifos e fallback para controle sem Select pertencem à
T012/T013; esta entrega não declara aceite de hardware físico. O botão touch de relíquias
já é uma ação da view; não foi inventado controle MobileCommand/relics ou novo prefab.

## Borda por contexto

- Gameplay: movimento, Quack, Pause e Relics disponíveis.
- Pausa/relíquias: movimento/Quack bloqueados; Pause e Relics disponíveis pelo contexto atual.
- Escolha obrigatória/resultados: comandos de gameplay, Pause e Relics descartados; UI
  continua recebendo Submit/Cancel/navegação no canal próprio.
- Uma pressão produz uma borda; segurar ou ler novamente não repete. Relics também atualiza
  a última atividade para seleção da fonte comum.
- Troca de contexto descarta bordas pendentes do contexto anterior. Suspensão/foco já
  aplicam essas transições; botões segurados durante contexto bloqueado não se repetem
  ao retomar, exigindo nova pressão. Remoção, Disabled e reset limpam Relics junto às
  demais bordas. Dispose remove callbacks.
- Clones privados habilitam o mapa Gameplay; não habilitam nem mutam o asset autoral.
  Bordas de consumidores diferentes são independentes.

## Geração e integração

UnifiedInputSetup.UpdateRelicsBindings (menu QuackQuack/Update Relics input (002 T010))
carrega o asset existente, acrescenta a ação Button e bindings ausentes e escreve ToJson
pela API do Unity. UnifiedInputSetup.Build chama a mesma atualização, inclusive quando
reaproveita um asset já existente. Importação e geração executadas em Unity 6000.4.3f1 na
cópia isolada Temp/Polimento002Validation; apenas o .inputactions gerado foi copiado à raiz.

IDs anteriores de mapa, ações e bindings foram comparados ao Git e preservados. .meta
permanece intacto. Segunda geração retornou exatamente o mesmo hash de arquivo (exit 0,
T010_RELICS_BINDINGS_SAVED, Logs/Polimento002/T010/idempotence.log).
O validador de conteúdo passa a exigir Relics, pois o adaptador agora depende dessa ação.
O contrato estrutural antigo exigia três ações para todos os grupos; foi atualizado para
exigir exatamente Move/Quack/Pause/Relics, mantendo os três grupos das ações de gameplay
e Keyboard/Gamepad para Relics. Não remover asserções de comportamento para fazê-lo passar.

## Testes adicionados

RelicsInputTests: 24 casos no Editor/Windows, sem UnityEditor/AssetDatabase. Quatro layouts
sintéticos: Gamepad, XInputController, DualShockGamepad e DualSenseGamepadHID. O HID é incluído
somente nas plataformas onde o pacote o registra; no Android são três layouts/22 casos,
sem marcar casos indisponíveis como Ignore. Essa execução Android não foi realizada aqui.

Cobertura: R/Select com uma borda, independência de Pause/Quack, fonte escolhida, cinco
contextos, bordas pendentes, botões segurados na escolha, perda/reset do controle, clones
independentes e roteamento real de Submit/Cancel/navegação no EventSystem sem gameplay.
Dois testes EditMode de PlayerCommand verificam compatibilidade posicional e independência
de Relics sem contornar validação do vetor.

A fixture usa eventos de estado, com escrita de controles no evento para suportar botões
bitfield. A navegação HID usa DualSenseHIDInputReport com eixos byte e hat codificado,
segundo a implementação instalada do pacote, conferindo também o valor normalizado
recebido antes de exigir navegação. Eventos UI são processados por frames reais.
Dispositivos/currents/políticas do runner são restaurados no teardown.

Tentativas iniciais corrigiram erros da fixture (InputState.Change não aceita bitfields;
vetor float não equivale ao payload HID; quebra de linha em diretiva condicional). Não
contar esses erros como defeito do produto. O ensaio focado final aprovou **31/31** casos
(24 novos + 7 UnifiedPlayerInputTests), zero ignorados, em focused-accepted.xml.
Uma suíte intermediária identificou somente o contrato estrutural antigo de três ações
além das 25 falhas esperadas; foi adaptado explicitamente ao novo protocolo de quatro ações.

Logs/XML regeneráveis em Logs/Polimento002/T010 e TestResults/Polimento002/T010, ignorados.
Resultados completos e limites finais abaixo.

## Resultado final

| Validação | Resultado |
|---|---|
| EditMode completo | **467/467 aprovados**, zero falhas/ignorados, exit 0 |
| PlayMode completo | **367 casos: 342 aprovados e 25 falhas pré-existentes**, zero ignorados, exit 2 |
| RelicsInputTests no player Windows x64 IL2CPP | **24/24 aprovados**, zero falhas/ignorados, exit 0 |
| Validador de conteúdo | T073_CONTENT_VALIDATION_OK, 65 assets, zero erros, dois warnings URP UnsupportedType já conhecidos |
| Repetição da geração | Mesmo hash de arquivo e IDs antigos preservados |

A lista das 25 falhas PlayMode foi comparada por fullname à T009 e coincide exatamente:
15 da disponibilidade T008 e 10 de menus T009. Nenhuma falha nova de comportamento T010.
Os 318 casos anteriormente verdes continuam aprovados, junto aos 24 novos casos runtime.
Os testes de oferta obrigatória continuam impedindo escolha/pulo implícito por Cancel.

XMLs EditMode.xml, PlayMode.xml e windows-runtime.xml em TestResults/Polimento002/T010;
logs correspondentes e content.log em Logs/Polimento002/T010. Test player separado sob
Temp/Polimento002Validation/Builds/T010/WindowsTests. Não houve nova build normal,
execução Android, teste físico de controle ou medição de desempenho nesta task.

Fontes da raiz/cópia testada com hashes SHA-256 idênticos:

| Arquivo | SHA-256 |
|---|---|
| PlayerCommand.cs | 6CD03B4289BE94B1C4F698FE7A291B73B73957EF433D3EC8D57CB4F6088B3413 |
| ActionPlayerInput.cs | CB71C88FA65C5DE90ADDDF656859A67B6F6CD789DAF4BC55E5075849A566FC8E |
| QuackQuackInput.inputactions | D585C9306F216A5C1BDBD37655A6B541D7C8C24B5FDB25E894BB9FF806A3EBAF |
| UnifiedPlayerInput.cs | A253FCD8A521CAAB1E383EDB5F1735520060441FCA36A380807E2980327F85EE |
| RelicsInputTests.cs | 2CA9A0F9971C4A1FDAC844606A53E2B805A7617C95559100A68B14A8EDB185C0 |

.meta do asset autoral preservado, hash
4ED618C5B77AC00595B7BF7E668FB054017F27E1471D38FE52103EA5C69B86FF.
.meta do novo teste gerado pelo Unity: GUID 3e8cd04f6f2747849b411e347131220c.
Nenhuma cena/prefab foi regravada.

Repetir a fixture com -runTests -testPlatform PlayMode -testFilter RelicsInputTests;
para IL2CPP, -buildTarget Win64 -testPlatform StandaloneWindows64 -testFilter RelicsInputTests
-playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T010/WindowsTests. Não usar -quit
em -runTests. Para regenerar somente bindings, usar -batchmode -quit -executeMethod
QuackQuack.Editor.UnifiedInputSetup.UpdateRelicsBindings na cópia.

**T010 concluída.** Não é aceite final do atalho na partida: PlayerController/LakeRelics
passarão a consumir RelicsPressed na integração T013. Os testes de abrir/fechar por Select
da T009 permanecem vermelhos até essa integração. Não antecipar T011–T014, perfis/glifos,
acesso sem Select ou validação física, nem alterar o estado da T075 antiga.
