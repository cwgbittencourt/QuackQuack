# Quack-quack Survive

Protótipo de sobrevivência 2D da **CEMG Games**, desenvolvido em Unity. O jogador controla o Pato
no bioma Lago, alterna entre água e terra e usa o Quack com uma reserva de Fôlego para controlar
inimigos e projéteis. A progressão planejada inclui armas, passivas, evoluções e encontros em uma
partida de 15 minutos.

> Em desenvolvimento. Este repositório contém o projeto Unity e os artefatos do Spec Kit necessários
> para continuar o trabalho. Ainda não contém uma demonstração jogável completa.

## Imagens do jogo

As capturas da arena e do gameplay serão adicionadas quando a cena jogável estiver montada.
Não há imagens de gameplay disponíveis neste commit inicial.

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
| T028–T032: implementação e integração do primeiro MVP | Pendente de conclusão e aceite |
| Progressão, partida completa, multiplataforma e desempenho | Planejados |

A fonte de verdade do progresso é [tasks.md](specs/001-prototipo-lago/tasks.md).
Alguns scripts de gameplay de T022 em diante já foram iniciados e fazem parte deste snapshot para
preservar o trabalho e as dependências dos testes. A presença desses arquivos **não significa que
as respectivas tarefas estejam concluídas**: faltam revisão, integração, dados autorais e montagem
da arena. A próxima tarefa é **T028**.

Validação registrada: **75 testes EditMode e 11 PlayMode aprovados**, incluindo regressão da fundação.
Consulte o [relatório T027](specs/001-prototipo-lago/test-report-t027.md).
Esses testes não substituem playtest, builds ou medição em aparelhos reais.

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
O módulo Android ainda não estava instalado na máquina de origem durante esta entrega;
nenhuma build Android foi validada.

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

**Resultado atual esperado:** Bootstrap cria os serviços e carrega `Lake`. A cena Lake contém a
câmera e mostra seu fundo azul-escuro. Pato, terreno, inimigos e HUD ainda não estão montados nela.
Uma tela com apenas esse fundo é esperada neste commit, não uma falha de instalação.

Após a conclusão e validação de T030 haverá uma arena visual para teste; T031 acrescenta o HUD e
T032 integra efeitos e áudio. O primeiro MVP com feedback completo depende de T022–T032 concluídas.

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
