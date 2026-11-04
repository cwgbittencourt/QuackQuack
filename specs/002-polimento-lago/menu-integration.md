# Integração dos menus e perfis — T013

**Data**: 04/11/2026. **Base**: c7f3727 (T012). Escopo FR-014/015 e integração com FR-013.
Somente T013; aceite físico por modelo, T014 e T075 anterior continuam separados.

## Uso na partida

Abra RunBootstrap para a partida atual ou Bootstrap para o treino. Pause com o atalho
configurado (padrão Menu/Start ou Esc). O menu de pausa permite navegar por
CONTINUAR, RELÍQUIAS, CONTROLES e ABANDONAR quando disponível. Confirme CONTROLES
para editar os três atalhos/família ou restaurar padrões. Os botões percorrem opções
válidas sem exigir Select. VOLTAR ou Cancel fecha a configuração preservando a pausa
anterior. A tela usa o store versionado separado do progresso implementado na T012.

Relíquias alterna pelo comando comum RelicsPressed, inclusive com R no teclado e
Select/View no perfil padrão de controle. Abrir durante simulação adquire a pausa;
fechar por VOLTAR, Cancel ou atalho só retoma uma pausa adquirida pelo próprio menu.
Uma pausa anterior continua pausada. Resultado/seleção obrigatória rejeitam o atalho.

## Implementação

PlayerController publica RelicsRequested a partir da mesma leitura de PlayerCommand;
LakeRelics assina/remove a assinatura sem criar segundo leitor de entrada. Pausa mantém
sua prioridade quando os dois comandos chegam juntos. A configuração bloqueia os
comandos de gameplay/pausa/relíquias enquanto aberta, conservando navegação de UI.
O bloqueio é removido ao fechar/terminar, limpando comandos e restaurando o contexto.

RelicLoadoutView captura/restaura foco, adquire foco dentro do modal e recompõe o grafo
após mudanças no loadout. Relíquias bloqueadas/limite de três não entram na navegação.
UpgradeSelectionView recompõe o grafo após rerrolar/banir, exclui slots vazios e recursos
esgotados e restaura o foco anterior ao resolver. Cancel em BANIR volta a ESCOLHER da
mesma carta; nos demais controles mantém a oferta obrigatória aberta. Só as ações
válidas ESCOLHER/PULAR/banimento final seguem as regras existentes de resolução.
Não há confirmação ou pulo implícito via Cancel.

MenuNavigation cria listas explícitas cíclicas: cima/esquerda percorrem a ação anterior,
baixo/direita a próxima. Os grafos não escapam do modal e o foco inválido é reparado.
MenuCancelRelay encaminha o Cancel enviado pelo InputSystemUIInputModule ao botão
selecionado para o dono do menu. Cor selecionada distinta e contorno amarelo de quatro
unidades tornam o foco visível; o contorno acompanha Select/Deselect/Disable.

ControlSettingsMenu integra os botões de pausa existentes na LakeRun e acrescenta
CONTROLES. No treino sem RunDirector usa um único PAUSAR/CONTINUAR próprio, ocultando
o botão móvel duplicado. Sua pausa própria preserva o retorno ao estado Boss/Playing;
quando aberto a partir da pausa mantém a pausa ao fechar. O grafo de pausa leva a
Relíquias e Controles sem Select; perfis e alternativas usam controles ativos fornecidos
à view. A interface oferece apenas atalhos não vazios: não remove o último acesso.
Perfis externos com atalho vazio continuam recebendo os padrões seguros no consumidor
sem capacidade de menu declarada (ForConsumer), como na T012.

O botão sul permanece Submit na UI. Mesmo após remapear Quack, Pausa/Relíquias não
podem ocupar sul, evitando atalho e confirmação simultâneos. O leste continua Cancel.
A validação do perfil também rejeita essas combinações ao carregar um arquivo.
Família automática/genérica tem rótulo localizado; identificação e nomes reais dos
controles mantêm o comportamento da T012.

## Assets e geração

PolishUiSetup.Build gera RelicLoadout.prefab, UpgradeSelection.prefab e o novo
ControlSettings.prefab e integra a configuração em Lake.unity e LakeRun.unity.
RelicSetup.BuildView/UpgradeSelectionSetup.BuildView ficam reutilizáveis e seus botões
já recebem relay e contorno; não se regeneraram catálogos ou balanceamento.
IntegratedUiSetup também reconhece e configura o novo modal com SafeAreaController e
ReferencePanelFit. GUIDs dos assets existentes e todos os .meta foram preservados.
Cenas e prefabs foram gerados pelo Unity 6000.4.3f1 na cópia isolada da feature 002.
As duas cenas recebem somente uma nova instância do prefab (98 linhas por cena);
nenhuma geometria da arena, cronograma ou definição de combate foi modificada.

## Validação e limites

A primeira execução focada aprovou os 33 casos existentes de RelicLoadoutFlowTests e
UpgradeSelectionTests: as dez regressões T009 foram corrigidas sem retirar asserções.
O foco com os oito novos ControlSettingsFlowTests aprovou 41/41.

A primeira suíte completa teve 388/389: o teste antigo de entrada usava um botão
artificial fora do novo grafo de pausa. Ele foi atualizado para usar o botão real de
Relíquias, mantendo as asserções de movimento, Quack, Submit e ausência de vazamento.
Foi acrescentado um teste EditMode para reservar Submit mesmo após remapear Quack.
A asserção compartilhada de foco também verifica o contorno ativo quando presente.
UpgradeSelectionTests tornou-se portável: só as opções de roteamento do Editor ficam
sob UNITY_EDITOR, sem remover os casos ou pular as verificações no player.

Os oito novos casos de fluxo cobrem: acesso a Controles sem Select/Cancel mantendo
pausa, pausa própria e retorno a Boss, edição por Submit/persistência/atalho novo,
reparo de foco após perda/troca de dispositivo e quatro proporções (16:9, 18:9,
19,5:9 e 4:3), cada uma com recorte dos dois lados. Os testes usam entradas sintéticas.
A captura de revisão visual é regenerável e não integra o commit; não aprova arte ou
uso físico. Testes de disposição verificam os cantos de todos os widgets do perfil;
não substituem o aceite de legibilidade no aparelho.

Resultados finais (nenhuma falha ou teste ignorado):

| Execução | Total | Aprovados | Falhas | Ignorados |
|---|---:|---:|---:|---:|
| EditMode completo | 491 | 491 | 0 | 0 |
| PlayMode completo | 389 | 389 | 0 | 0 |
| Windows x64 IL2CPP — entrada/menus/UI | 123 | 123 | 0 | 0 |
| Android ARM64 IL2CPP — entrada/menus/UI no S23 Ultra | 121 | 121 | 0 | 0 |

Todos encerraram com exit 0. Os players incluem RelicLoadoutFlowTests,
UpgradeSelectionTests, ControlSettingsFlowTests, ControllerAvailabilityTests,
UnifiedPlayerInputTests, RelicsInputTests, GamepadInputAdapterTests,
ControlProfileRuntimeTests e IntegratedUiLayoutTests. A diferença de dois casos no
Android corresponde aos layouts HID DualSense condicionais em RelicsInputTests;
nenhum caso foi ignorado para obter aprovação.

O validador de conteúdo aprovou 65 assets, zero erros e dois avisos de tipos de
renderização sem validador de domínio (LakeRenderer2D/LakeURP), sem alterações neles.
Marcador T073_CONTENT_VALIDATION_OK. Evidências: final-EditMode.xml,
final-PlayMode.xml, windows-runtime.xml, android-runtime.xml e ValidateForCI.log.

O APK de testes usa com.cemggames.quackquack.validation002.t013tests, nome QuackQuack
T013 Tests. Preserva o aplicativo normal T007. Não foi gerada nova build normal.
As quinze regressões T008 e dez T009 estão aprovadas. Isso não encerra a matriz
física Xbox/PlayStation/genérico, o aceite jogável/artístico ou o profiling; T014,
validações posteriores e T075 anterior mantêm suas pendências.

O whitespace de código/documentos foi conferido com git diff --check. Nos YAML
serializados foi preservado o formato gerado pelo Unity (espaços após campos vazios),
sem edição manual para normalizar esses arquivos.

Logs/XML/builds em diretórios
ignorados Logs/Polimento002/T013, TestResults/Polimento002/T013 e Builds/T013 da cópia.
Nenhum serial de aparelho, save do usuário, log, captura ou build integra o commit.

## Integridade das fontes e assets

SHA-256 idênticos na raiz e na cópia de validação:

- Editor/IntegratedUiSetup.cs: 6D3A018658AAB02F99C9032DCF80AAB9E22A1B6CC0E0E0C8165D2DE54598FC36
- Editor/RelicSetup.cs: C2B38E1DEECDB33799230975BA8D9F90A1EFE2222F4663A5784D6234E5BCCAFE
- Editor/UpgradeSelectionSetup.cs: 9582B919DDDD5973B368F357A1F227174144C5CBEC48A87296E139CD303855C5
- Editor/PolishUiSetup.cs: 3E0F0116A8EC5E4ED1C3DC073A4702E51B4568F97BC9C106A5DA07F12F519EDB
- Gameplay/Player/PlayerController.cs: FB4404DDB64FF15B79635905B64A18ABA5CF97A1FEAD3DC7A1319D63FE137751
- Gameplay/Upgrades/LakeRelics.cs: EC4C4E364DEC68975B18296866BCE20A49E0379201546B16681B0AFF152255B9
- Gameplay/Upgrades/ControlSettingsMenu.cs: B2C4E0E98121210C510FF6C841C4A0610C84871E1E4E2CBEDD789C84008690EB
- Platform/Input/ControlProfile.cs: 3F72B27F64C0E299B6C0DA0C906DA5CF347597F94021ACE219997F3F337F76FA
- Presentation/UI/ControlBindingsView.cs: 3A7F87F78F6DE49A968EE826376D1DEF64B06EAF733366212D4B75BDB5D883CE
- Presentation/UI/RelicLoadoutView.cs: B24CFB2D3737BEC0B75F5B3691BC20C31E09AD7ECD2B4B3FE72366EE8D82B649
- Presentation/UI/UpgradeSelectionView.cs: 9D9F9E979A8C9C4D78F9802BE21EC9AE1FA6909F6262D725BC08BB1FF3C152F6
- Presentation/UI/MenuCancelRelay.cs: 614A57D7117AB7B0090E701315591F067305D548CC309BF5ECCB961A690EC1ED
- Presentation/UI/MenuNavigation.cs: 99BFB11D9770AB16D7DA6B2F183FA426AD0C65FDE0C53E6CC5669EF0A9A5CF21
- Tests/EditMode/ControlProfileTests.cs: 905FEDDCE9FCA6D3B166A8BE97F72691E4E7F3AB9009D1F8EA838C84BBFF7AFE
- Tests/PlayMode/QuackQuack.PlayModeTests.asmdef: FB38ABEBA7BDBCA71893FC98EC4E5D37EE50957D3EECAF45DEB76A473B077076
- Tests/PlayMode/RelicLoadoutFlowTests.cs: 2392644CA0DF528A748D8FE26CFFCFB1A6068CCC3B6BC7736616AF1279885A75
- Tests/PlayMode/UnifiedPlayerInputTests.cs: DE1D0090E46B7203D6005C7B78E65B357153B0423B160E37738931C8E61C1E42
- Tests/PlayMode/UpgradeSelectionTests.cs: 650EA0365E544D47623622BB77EBDC41B1E1279D5D557D81901BDA589F565D68
- Tests/PlayMode/ControlSettingsFlowTests.cs: 37714B620DE669AAAE98D4C840EDA04AB16C987E7704BD8ADECA0D4C9C83E0F9
- Scenes/Lake.unity: 7EEDBCCB81D74E66C1C3C0CB1AA5E158BDF0651FAFB53F458A1B361840161C1B
- Scenes/LakeRun.unity: 0947854E385D2B285D098CFC4A68840AF2F49B1C7881B14DF4E9C78D8FC8741D
- Prefabs/Prototype/RelicLoadout.prefab: 6428EBA1250FDC00B1F578B8E063540B8F42BB19079294C1074E4AC5CE763451
- Prefabs/Prototype/UpgradeSelection.prefab: 389214A013FEF65FE1A8C25EFA27F6FB2FFA4D086F2E96CDDBC2895BE6D15C25
- Prefabs/Prototype/ControlSettings.prefab: DA5E5E457DEE9F378C77F71D4186074097074E88649474B07B7BFF442EB551CD
