# Quack-quack Survive

Protótipo de sobrevivência 2D da **CEMG Games**, desenvolvido em Unity. O jogador controla o Pato
no bioma Lago, alterna entre água e terra e usa o Quack com uma reserva de Fôlego para controlar
inimigos e projéteis. A progressão planejada inclui armas, passivas, evoluções e encontros em uma
partida de 15 minutos.

> Em desenvolvimento. Este repositório contém o projeto Unity e os artefatos do Spec Kit necessários
> para continuar o trabalho. Inclui a orquestração jogável da partida e uma arena de treino;
> tela final e progresso persistido; aceite de gameplay, plataformas e desempenho continuam pendentes.

## Imagens do jogo

Abra **RunBootstrap** para a partida do Lago ou **Bootstrap** para a arena de treino.
Arte e áudio são temporários. Capturas de playtest serão adicionadas com o aceite do MVP.

## Estado da entrega

| Etapa | Estado |
|---|---|
| T001–T017: configuração, contratos, estados, pooling e bootstrap | Concluída |
| T018–T021: testes iniciais de Fôlego, Quack, terreno e teclado | Concluída |
| T022: estado do jogador e invariantes | Concluída |
| T023: definição e validação do Pato | Concluída |
| T024: definição e validação do Quack | Concluída |
| T025: regras de consumo, recuperação e carga do Fôlego | Concluída |
| T026: resolução do Quack e deduplicação de eventos | Concluída |
| T027: adaptador de teclado para comandos comuns | Concluída |
| T028: controlador do Pato, física e comandos comuns | Concluída |
| T029: volumes de terreno, histerese e afinidades | Concluída |
| T030: arena curta do Lago | Concluída |
| T031: HUD de vida, Fôlego, terreno e estado do Quack | Concluída |
| T032: feedback visual e áudio do primeiro MVP | Concluída |
| T033: testes dos limites da build e dos recursos de escolha | Concluída (aprovados com T039) |
| T034: testes de ofertas e idempotência por offerId | Concluída (aprovados com T040) |
| T035: testes de coleta de XP e fluxo de seleção | Concluída (aprovados com T041) |
| T036: definição de arma, cinco níveis e afinidade por terreno | Concluída |
| T037: definição de passiva, bônus limitados e vínculo com arma | Concluída |
| T038: definição e validação das combinações evolutivas | Concluída |
| T039: estado da build, slots, recursos e evolução | Concluída |
| T040: ofertas, rerrolagem, banimento, pulo e confirmação | Concluída |
| T041: coleta pooled de XP e progressão de níveis | Concluída |
| T042: seis comportamentos de arma e projéteis pooled | Concluída |
| T043: catálogo de seis armas, seis passivas e três evoluções | Concluída |
| T044: seleção de cartas e progressão integrada à arena | Concluída |
| T045: feedback pooled de evolução e recompensa rara | Concluída |
| T046: testes do cronograma e pedido único do chefe | Concluída (aprovados com T051) |
| T047: testes de resultado único e abandono parcial | Concluída (aprovados com T058) |
| T048: testes de relíquias e recuperação de save | Concluída (aprovados com T052/T059) |
| T049: testes dos sinais visuais/sonoros do Sapo-Rei | Concluída (aprovados com T057) |
| T050: definição de inimigos, papéis táticos, pool e identidade de telemetria | Concluída |
| T051: definição e cronograma dos encontros sem lacunas | Concluída |
| T052: dez relíquias, desbloqueios, efeitos e tela de loadout | Concluída |
| T053: estado de inimigo, recompensa única e ciclo pooled | Concluída |
| T054: diretor de spawn com limites, exclusão e expiração | Concluída |
| T055: cinco comportamentos táticos com scheduler central | Concluída |
| T056: catálogo fechado do Lago, sete inimigos e cronograma autoral | Concluída |
| T057: fases do Sapo-Rei, ataques sinalizados e arena isolada | Concluída |
| T058: resultado imutável, estatísticas e recompensa parcial | Concluída |
| T059: save de relíquias versionado, gravação atômica e recuperação | Concluída |
| T060: ondas, encontros 5/10/15, combate, três finais e save integrado | Concluída |
| T061: tela final, carteira, recordes/desafios e nova partida | Concluída |
| T062: testes de paridade de teclado, controle e toque | Concluída (paridade sintética aprovada em T066) |
| T063: testes de HUD nas quatro proporções e Safe Area | Concluída (matriz aprovada em T069) |
| T064: smoke tests de build e inicialização Windows/Android | Concluída (contrato T070 em fase vermelha; test player Windows aprovado) |
| T065: adaptador de controle para comandos comuns | Concluída |
| T066: adaptador, joystick e botões móveis em prefab | Concluída |
| T067: mapa comum e seleção de entrada nas cenas | Concluída |
| T068: controlador de Safe Area com atualização automática | Concluída |
| T069: HUD responsivo com CanvasScaler e Safe Area nas duas cenas | Concluída |
| Multiplataforma e desempenho | Planejados |

A fonte de verdade do progresso é [tasks.md](specs/001-prototipo-lago/tasks.md).
Alguns scripts de gameplay de T022 em diante já foram iniciados e fazem parte deste snapshot para
preservar o trabalho e as dependências dos testes. A presença desses arquivos **não significa que
as respectivas tarefas estejam concluídas**: faltam revisão, integração, dados autorais e montagem
da arena. T022–T069 foram revisadas e concluídas; a próxima tarefa é **T070**.

Validação T069: **237 PlayMode: 233 aprovados, 4 falhas esperadas da T070, nenhum ignorado**.
Os 15 casos de UI responsiva passaram. O HUD de Lake e LakeRun agora usa CanvasScaler
1920×1080 e âncoras dentro da Safe Area, preservando suas referências.
Consulte o [relatório T069](specs/001-prototipo-lago/test-report-t069.md).

Validação T068: **237 PlayMode: 229 aprovados, 8 falhas esperadas, nenhum ignorado**.
Os sete casos do controlador de Safe Area passaram: quatro proporções, mudanças de
resolução/recorte, geometria indisponível e reativação. A integração do HUD autoral
é T069; pipeline de produto é T070. Consulte o [relatório T068](specs/001-prototipo-lago/test-report-t068.md).

Validação T067: **235 PlayMode: 222 aprovados, 13 falhas esperadas, nenhum ignorado**.
Um asset Gameplay/Move/Quack/Pause atende teclado, controle e toque. As cenas usam
seleção pela atividade mais recente, controles móveis quando há toque e bloqueio
de gameplay nos menus. **9/9 testes passaram em test player Windows x64 IL2CPP**.
Safe Area e pipeline de produto seguem pendentes. Consulte o [relatório T067](specs/001-prototipo-lago/test-report-t067.md).

Validação T066: **227 PlayMode: 214 aprovados, 13 falhas esperadas, nenhum ignorado**.
Os 15 casos de paridade passaram. Dez testes novos cobrem callbacks, posse dos dedos,
cancelamento e Touchscreen sintético via raycast no prefab MobileControls. A seleção automática chegou na T067; Safe Area e pipeline de produto continuam pendentes. Consulte o [relatório T066](specs/001-prototipo-lago/test-report-t066.md).

Validação T065: **217 PlayMode: 199 aprovados, 18 falhas esperadas, nenhum ignorado**.
Os cinco casos de paridade do controle e nove testes adicionais do adaptador passaram.
Toque, Safe Area e pipeline de produto seguem pendentes. A seleção unificada chegou na T067.
Consulte o [relatório T065](specs/001-prototipo-lago/test-report-t065.md).

Validação T064: **208 PlayMode: 185 aprovados, 23 falhas esperadas, nenhum ignorado**.
Os quatro novos casos de configuração aguardam a pipeline T070. Os dois casos de
runtime também passaram em um **test player Windows x64 IL2CPP** compilado pelo
Test Framework. Android não foi executado; a build de produto e o aceite de
plataforma continuam pendentes. Consulte o [relatório T064](specs/001-prototipo-lago/test-report-t064.md).

Validação T063: **202 PlayMode: 183 aprovados, 19 falhas esperadas, nenhum ignorado**.
Os quatro casos do HUD sem recortes passaram. Há nove falhas novas de T068/T069,
além das dez de T065/T066 já registradas na T062. Não houve regressão nos casos
antes aprovados. Consulte o [relatório T063](specs/001-prototipo-lago/test-report-t063.md).

Validação T062: **189 PlayMode: 179 aprovados, 10 falhas esperadas, nenhum ignorado**.
Os 174 testes anteriores passaram; os cinco novos casos de teclado passaram. Os dez
casos de controle/toque falham pela ausência dos adaptadores T065/T066. A T062 entrega
os testes, sem antecipar essas implementações ou aprovar paridade de dispositivos.
Consulte o [relatório T062](specs/001-prototipo-lago/test-report-t062.md).

Validação T061: **456/456 EditMode e 174/174 PlayMode aprovados, sem falhas**.
A tela final apresenta combate, build, recompensas e progresso em três colunas
roláveis. O schema 2 persiste carteira, recordes e desafios sem repetir crédito.
Consulte o [relatório T061](specs/001-prototipo-lago/test-report-t061.md).
Os testes de build, ofertas e fluxo de XP das T033–T035 passaram.
Consulte o [relatório T045](specs/001-prototipo-lago/test-report-t045.md).
Esses testes não substituem playtest, builds ou medição em aparelhos reais.
Bootstrap abre a arena de treino com Bumerangue, três pickups cianos de XP e um raro roxo.
A coleta abre cartas e pausa o combate; é possível escolher, rerrolar, banir ou
pular. Evolução exibe estrela dourada e recompensa rara, losango roxo, com legendas
e tons temporários. Esse Bootstrap continua sendo uma fixture de treino independente.
O botão RELÍQUIAS abre o loadout e pausa a arena. Até três bônus podem ser
equipados; tempo sobrevivido e XP desbloqueiam as demais opções nesta sessão.
Em **RunBootstrap**, as ondas usam os cinco inimigos, com minibosses aos 5/10 minutos
e Sapo-Rei aos 15. A câmera acompanha o Pato. PAUSAR/Esc permite continuar na mesma
fase; ABANDONAR encerra sem vitória. Relíquias são carregadas antes do combate e
salvas na pausa/seleção, ao encerrar ou sair, em `lake-relics.json` sob
`Application.persistentDataPath`. O treino Bootstrap mantém estado por sessão.
O schema 2 persiste também carteira e progresso. Confira o estado do save na tela
final; se falhar, use TENTAR NOVAMENTE antes de sair. JOGAR NOVAMENTE reinicia a partida.

## Ambiente necessário

| Componente | Uso |
|---|---|
| Git e acesso autorizado ao repositório privado | Clonar, criar branches e enviar alterações |
| Unity Hub e conta com licença Unity ativa | Instalar e abrir o Editor; cada desenvolvedor usa sua própria licença |
| **Unity 6000.4.3f1**, revisão `39d1a88d4dd1` | Versão fixada para o protótipo |
| IDE C# com suporte a Unity, por exemplo Visual Studio | Editar e depurar os scripts |
| PowerShell 7 (`pwsh`) | Executar os scripts do Spec Kit apresentados abaixo |
| Internet na primeira abertura | Restaurar os pacotes do Unity |

O fluxo documentado e validado usa **Windows x64**. Não atualizar o Editor nem os pacotes
automaticamente: uma mudança da versão fixada exige o processo definido na
[constituição](.specify/memory/constitution.md).

Para **gerar uma build Windows IL2CPP**, instalar pelo Hub o suporte Windows IL2CPP e, pelo
instalador do Visual Studio, as ferramentas C++ e o Windows SDK compatíveis com o Editor.
Isso não é necessário somente para executar os testes no Editor.

Para **gerar uma build Android**, adicionar à instalação do Editor pelo Hub:

- Android Build Support;
- Android SDK & NDK Tools;
- OpenJDK.

Usar as ferramentas Android fornecidas pelo Hub para essa versão. O alvo configurado é ARM64/IL2CPP.
Um aparelho Android real será necessário para o aceite de desempenho e interação móvel.
Na validação T064, os módulos Windows e Android estavam presentes na instalação.
O test player Windows IL2CPP foi validado; nenhuma build Android foi validada.

Não é necessário instalar URP, Input System, uGUI, Test Framework ou Memory Profiler manualmente.
As dependências estão declaradas em `Packages/manifest.json` e fixadas em `Packages/packages-lock.json`.
Não são necessários Docker, servidor ou banco de dados externo para o estado atual do protótipo.

## Clonar e abrir

```powershell
git clone https://github.com/cwgbittencourt/QuackQuack.git
cd QuackQuack
```

1. No Unity Hub, usar **Projects → Add → Add project from disk** e selecionar a pasta clonada.
2. Abrir com **6000.4.3f1** e aguardar a importação inicial e a compilação.
3. Abrir `Assets/QuackQuack/Scenes/Bootstrap.unity`.
4. Clicar em **Play** e selecionar a aba **Game**.

**Resultado atual esperado:** Bootstrap cria os serviços e carrega `Lake` com Pato amarelo à esquerda,
lago central com ilha, obstáculos cinza, dois alvos vermelhos e um corredor de risco à direita.
O emissor roxo dispara projéteis leves. A recompensa dourada no corredor recupera Fôlego uma vez.
Use WASD/setas para mover, Espaço para Quack e Esc para pausar/retomar. Ao morrer, saia de Play
e entre novamente por Bootstrap para reiniciar. O HUD mostra vida, Fôlego, terreno, prontidão,
rejeições do Quack e tempo da partida. O Quack cria uma onda dourada, impactos alaranjados,
pulso ciano na reflexão e contorno branco no crítico, acompanhados de tons temporários.

A arena, HUD e feedback permitem testar a mecânica central de US1. O aceite de gameplay,
as builds de plataforma e as metas de desempenho ainda dependem da validação correspondente.

O menu `QuackQuack → Configure prototype` reaplica configurações básicas quando necessário.
As configurações e os assets URP já estão versionados; não é preciso executar esse menu para abrir
um clone normal. Não recriar as cenas existentes: a integração deve preservar seus arquivos e `.meta`.

## Executar os testes

Com o projeto aberto, usar **Window → General → Test Runner** e executar **Run All** nas abas
**EditMode** e **PlayMode**. O Test Runner deve reconhecer os dois assemblies do projeto.

Também é possível executar pelo PowerShell, a partir da raiz do clone, com o Editor do projeto fechado:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe'
$projectRoot = (Get-Location).Path
New-Item -ItemType Directory -Force -Path TestResults, Logs | Out-Null

# Executar cada comando e aguardar sua conclusão antes do próximo.
& $unityEditor -batchmode -nographics -projectPath $projectRoot -runTests -testPlatform EditMode -testResults "$projectRoot\TestResults\editmode.xml" -logFile "$projectRoot\Logs\editmode.log"
& $unityEditor -batchmode -nographics -projectPath $projectRoot -runTests -testPlatform PlayMode -testResults "$projectRoot\TestResults\playmode.xml" -logFile "$projectRoot\Logs\playmode.log"
```

Em um terminal que não aguarda aplicativos gráficos, executar os comandos individualmente e
confirmar o encerramento do processo Unity antes do seguinte. Para automação, usar
`Start-Process -Wait -PassThru` e conferir `ExitCode`, além do resultado nos XML.

A licença precisa estar disponível para o usuário do Windows que executa o Unity. Se a execução
automatizada informar falta de licença, abrir primeiro o projeto pelo Hub na sessão licenciada.
Os testes de teclado configuram temporariamente o foco para entrada sintética e restauram o estado
ao terminar. Há avisos de API obsoleta `GetInstanceID` nos scripts de gameplay em andamento.

## Continuar com Spec Kit

O projeto foi inicializado com **Spec Kit 1.1.3**, integração Codex e scripts PowerShell.
As skills estão em `.agents/skills/`; os scripts, templates e a constituição estão em `.specify/`.
Não é necessário inicializar novamente o projeto ou regenerar o plano existente.
O editor/assistente de IA é opcional para desenvolver o jogo; para usar as skills, utilizar um
ambiente que reconheça `.agents/skills/`, com sua própria autenticação.

Leia nesta ordem:

1. [Constituição](.specify/memory/constitution.md).
2. [Especificação](specs/001-prototipo-lago/spec.md) e [plano](specs/001-prototipo-lago/plan.md).
3. [Modelo de dados](specs/001-prototipo-lago/data-model.md) e [contratos](specs/001-prototipo-lago/contracts/gameplay-contracts.md).
4. [Tarefas](specs/001-prototipo-lago/tasks.md), [validação](specs/001-prototipo-lago/quickstart.md) e relatório de testes.

O ponteiro `.specify/feature.json` é estado local e não acompanha o clone. Para selecionar a feature
existente, abra PowerShell 7 na raiz do repositório e execute:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = 'specs/001-prototipo-lago'
pwsh -NoProfile -File .specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
```

O script grava o ponteiro local e deve retornar a pasta absoluta da feature e os documentos disponíveis.

**Acordo de trabalho:** implementar uma task por vez, ou apenas o intervalo explicitamente solicitado;
executar a validação correspondente, atualizar seu checkbox e parar para revisão e publicação.
Não executar todo `tasks.md` de uma vez. Exemplo de solicitação ao assistente:

```text
$speckit-implement T022 somente. Valide a task, atualize tasks.md e pare.
```

Criar uma branch por incremento, por exemplo `task/T022-player-state`, e revisar o diff antes do
commit. Manter scripts em andamento que pertencem a outras tarefas identificados como pendentes.

## Estrutura e versionamento

| Caminho | Conteúdo |
|---|---|
| `Assets/QuackQuack/Core/` | Regras e estado testáveis |
| `Assets/QuackQuack/Gameplay/` | Integração com objetos, movimento e física |
| `Assets/QuackQuack/Content/` | Definições de conteúdo autoral |
| `Assets/QuackQuack/Systems/` | Eventos, pools e orquestração |
| `Assets/QuackQuack/Platform/` | Adaptadores de entrada e plataforma |
| `Assets/QuackQuack/Presentation/` | Interface e feedback |
| `Assets/QuackQuack/Scenes/` e `Settings/` | Cenas e URP 2D |
| `Assets/QuackQuack/Tests/` | Testes EditMode e PlayMode |
| `Packages/` e `ProjectSettings/` | Dependências e configurações reproduzíveis |
| `documentos/` | Documento funcional de origem |
| `specs/`, `.specify/` e `.agents/` | Planejamento, contratos e fluxo Spec Kit |

Versionar os assets **sempre com seus `.meta`**. Mover/renomear assets pelo Unity para preservar
referências. `Library`, `Temp`, `Logs`, `UserSettings`, arquivos de IDE e resultados regeneráveis
de testes são locais e estão excluídos pelo `.gitignore`; o Editor recria os caches.

O repositório é privado. O responsável precisa conceder acesso ao outro desenvolvedor no GitHub;
conhecer o endereço do clone não concede permissão. Licenças Unity, credenciais, instalações e
ferramentas de plataforma não são distribuídas pelo repositório.
