# Perfis de controle — T012

Estado atual: integração T013 concluída e regressões de menus corrigidas. Veja [menu-integration.md](menu-integration.md). O conteúdo abaixo preserva a entrega e os resultados históricos da task indicada no título.

**Data**: 04/11/2026. **Base**: 9afeae9 (T011). Escopo FR-015, somente T012.

## Comportamento implementado

ControlProfile versão 1 mantém família Automatic/Xbox/PlayStation/Generic e caminhos
portáveis Gamepad para Quack, Pausa e Relíquias. Padrões preservados: sul, Start e Select.
O jogador pode atribuir Relíquias a ombro, gatilho ou outro botão permitido, sem depender
de Select. Caminhos de dispositivo físico, teclado, controles desconhecidos, Cancel/leste
e duplicidade entre os três comandos são rejeitados. Movimento, navegação UI,
confirmar/sul e retornar/leste continuam nos bindings autorados existentes.

Automatic identifica famílias pela herança de layout do Input System; não usa nome de
fabricante ou suposição por posição. Xbox usa A/B/X/Y, Menu/Start e View/Back;
PlayStation usa Cross/Circle/Square/Triangle, Options e Share/Select ou Create quando o
layout DualSense é conhecido. Perfil explícito permite escolher a família quando o layout
normalizado não a identifica. Genérico usa displayName real do controle ou nome do caminho
quando não há dispositivo. São rótulos textuais; não foi criado atlas de ícones/arte.

ControlProfileStore grava control-profile-v1.json em Application.persistentDataPath,
separado do save de progressão. Somente versão, família e três caminhos são serializados;
não há ID, serial, credencial ou referência a aparelho. Leitura valida campos, versão,
família, tamanho e bindings. Ausência usa padrões; corrupção/versão incompatível tenta
backup válido e depois padrões, sem sobrescrever o arquivo na leitura. Escrita usa
arquivo temporário no mesmo diretório, Flush(true), File.Replace e backup da versão
anterior válida. Um arquivo principal corrompido não substitui o backup válido.
Falha de escrita não publica evento nem modifica o perfil ativo.

ActionPlayerInput aplica overrides somente aos bindings Gamepad de uma cópia runtime do
asset autorado. GamepadInputAdapter recebe opcionalmente um store para composição/teste;
produção usa o store compartilhado. Salvar publica a mudança aos consumidores ativos:
limpa bordas de Quack/Pause/Relics e exige movimento neutro, preservando contexto e ações
permitidas. Dispose remove a assinatura. Não há leitura de arquivo ou aplicação de
perfil por frame; teclado, touch e asset original são preservados.

ControlBindingsView oferece botões de ciclo para cada atalho/família e restauração dos
padrões. Usa listeners removíveis, rótulos dos bindings atuais, dicas dos comandos fixos
e mensagem de resultado da gravação. As opções de atalho não exigem pressionar Select.
A troca do controle atual atualiza rótulos. Remover atalho exige alternativa ativa e
interagível fornecida pela composição; a persistência também verifica essa capacidade.
Um consumidor sem acesso de menu declarado usa padrões seguros para um perfil com
atalho vazio, impedindo perder Pausa/Relíquias antes da integração.

## Fronteira com T013

Nenhuma cena ou prefab foi alterado. A T013 deve gerar e integrar ControlBindingsView,
seus botões/status/dicas e alternativas focalizáveis em PolishUiSetup; conectar ações
reais de pausa/relíquias e declarar as capacidades ao consumir um perfil sem atalho.
ForConsumer(pauseAlternative, relicsAlternative) aceita esses acessos somente quando
realmente disponíveis. O componente não assume a propriedade da pausa nem implementa
o toggle/grafos dos menus. Navegação visual, foco global e Cancel permanecem na T013.
Até essa integração não há nova tela de configuração disponível na partida normal.
Não foram implementadas T013/T014 ou encerrado o aceite físico da US2/T075 anterior.

## Validação

Unity 6000.4.3f1, cópia isolada Temp/Polimento002Validation. .meta dos cinco scripts novos
gerados na importação Unity e copiados de volta sem alterar os GUIDs existentes.
Presentation passa a referenciar Platform e Unity.InputSystem para consumir os perfis;
não há dependência inversa ou regra de gameplay na view.

| Execução | Total | Aprovados | Falhas | Ignorados |
|---|---:|---:|---:|---:|
| EditMode completo | 490 | 490 | 0 | 0 |
| PlayMode completo, final | 381 | 371 | 10 | 0 |
| Windows x64 IL2CPP — entrada, final | 78 | 78 | 0 | 0 |
| Android ARM64 IL2CPP — entrada, S23 Ultra | 76 | 76 | 0 | 0 |

ControlProfileTests: 23 casos novos de padrões, quatro famílias, atribuição sem Select,
roundtrip separado do progresso, corrupção/esquema, backup, tamanho, snapshots,
conflitos/caminhos inválidos e falha de escrita sem publicação. Os oito casos novos de
ControlProfileRuntimeTests passaram no Editor, Windows e Android: identificação por três
layouts, labels explícitos/genéricos, view sem alternativa, aplicação de Relíquias sem
Select, borda única, limpeza de borda antiga, Pausa/Quack remapeados e asset preservado.
Também verificam no player substituição atômica/backup/roundtrip separado do progresso
e retenção do store ao desativar/reativar a view.
Os 70 testes anteriores de entrada também passaram no player Windows; no Android são
68, pois dois casos RelicsInputTests de layout HID DualSense são condicionais à plataforma.
Contagens finais: ControllerAvailabilityTests 30, UnifiedPlayerInputTests 7,
GamepadInputAdapterTests 9, RelicsInputTests 24 Windows/22 Android e
ControlProfileRuntimeTests 8 em ambos os players.

As dez falhas PlayMode foram comparadas por fullname ao resultado T011 e coincidem,
sem novas falhas. Permanecem as regressões da T009 previstas para T013. Exit 0 em
EditMode/Windows/Android; exit 2 no PlayMode completo por essas dez regressões conhecidas.
A primeira compilação da fixture usou um helper de estado inexistente; corrigido para
GamepadState.WithButton. Na primeira execução houve falhas da fixture por roteamento
sem foco e representação de gatilho como bit; corrigidos para política do runner e
valor analógico. Nenhuma asserção de comportamento foi retirada para obter aprovação.

Android usou o pacote de testes separado
com.cemggames.quackquack.validation002.t012tests (QuackQuack T012 Tests), preservando
o app normal T007. Nenhuma nova build normal foi gerada. Evidências finais em EditMode.xml,
PlayMode-final.xml, windows-final.xml e android-runtime.xml.

Logs/XML/builds ficam em Logs/Polimento002/T012, TestResults/Polimento002/T012 e
Builds/T012 da cópia, todos ignorados pelo Git. Esses testes usam dispositivos
sintéticos; não aprovam modelo físico, glifos em hardware, gameplay ou desempenho.

## Integridade das fontes validadas

SHA-256 idênticos entre a raiz e a cópia isolada:

- Platform/Input/ControlProfile.cs: 8B7F261CAC256A77C60FBE8DAEB6A224B20A7AF1E1EB3156A8CCDC069C8D386A
- Platform/Input/ControlProfileStore.cs: 29B935935326CB5DCF7430C538522F2D8D9AD7A5D0D006B812EE817975216630
- Platform/Input/ActionPlayerInput.cs: 690A9C9151A4A71E13A2AED034248BDD2085D17F3E425C390429D0B856A62363
- Platform/Input/GamepadInputAdapter.cs: DBB5124D1E2CB6468ADF946847E645D3453576B45B8F3BBDD2225A40B9220441
- Presentation/QuackQuack.Presentation.asmdef: 7FE272A53DD62D2E2546A0AB986B436C9AE9258A5A5286FFCC941E90B36285F5
- Presentation/UI/ControlBindingsView.cs: 6AE2EF9803EFAED8F9DF89C4786FF1ACF1C09AAC5611CBF3F4CD1BDCD1213003
- Tests/EditMode/ControlProfileTests.cs: F3985869328F2F0128ECAD69D5D2FB0E074A63863FB038D6B6F21A245DB0617C
- Tests/PlayMode/ControlProfileRuntimeTests.cs: F1A2115297B930CC8AAE4DC02533C8D22F1F5A4311691DF72809F2A8A6D79332
