# T070 — Pipeline de build compartilhado

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T070. Próxima task: T071.

## Entrega

PrototypeBuildPipeline expõe CreateOptions(BuildTarget, string), ConfigureTarget,
Build(BuildTarget, string), BuildWindows e BuildAndroid. Os dois alvos compartilham
exatamente RunBootstrap e LakeRun, nessa ordem. As cenas de treino e o Test Runner
não são empacotados no produto. Cada chamada retorna uma lista independente e não
altera EditorBuildSettings ou o alvo ativo. Saídas exigem extensão .exe ou .apk.

ConfigureTarget aplica IL2CPP e somente ARM64 no Android. Build exige alvo ativo
correto, módulo disponível e cenas existentes, cria a pasta de saída e compila com
BuildOptions.None. Android seleciona APK. Falha ou cancelamento da compilação lança
BuildFailedException, sem indicar sucesso em CI. Não há instalação ou autoexecução.

Menu QuackQuack > Build prototype e entradas -executeMethod usam caminhos ignorados
Builds/Windows/QuackQuack.exe e Builds/Android/QuackQuack.apk. A seleção de plataforma
é feita antes pelo Editor ou -buildTarget, evitando troca implícita durante testes.
Configurações aplicadas por ConfigureTarget/Build permanecem no projeto usado.
A validação ocorreu exclusivamente em Temp/T033Validation; nenhum ProjectSettings,
EditorBuildSettings ou cena do projeto principal foi alterado. O .meta do novo
script foi gerado pelo Unity na cópia e preservado no projeto.

## Testes

Os quatro contratos que falhavam na T069 agora passam. A suíte completa passou
antes de ampliar as asserções de configuração. Depois foram executados os nove
casos finais do contrato, sem repetir testes de gameplay que não foram alterados.

| Execução | Total | Aprovados | Falhas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| PlayMode completo | 237 | 237 | 0 | 0 | 0 |
| Contratos finais do pipeline | 9 | 9 | 0 | 0 | 0 |

As asserções adicionais verificam BuildOptions.None, exclusão das cenas de treino,
listas independentes entre chamadas e preservação do alvo/lista global de cenas.
Cinco casos novos rejeitam caminhos vazios/inválidos e alvos WebGL/macOS fora do
escopo. Os testes de ConfigureTarget partem de Mono e ARMv7 e restauram os valores
anteriores em finally. Nenhum stub ou Ignore foi introduzido.

Artefatos ignorados: TestResults/t070-playmode.xml, TestResults/t070-build-config.xml,
Logs/t070-playmode.log e Logs/t070-build-config.log. Nenhuma mudança em Core;
EditMode não foi repetido.

## Compilação de produto

Windows x64 IL2CPP: sucesso, código 0, Build Finished Result: Success e
PROTOTYPE_BUILD_OK target=StandaloneWindows64. Executável de 667648 bytes e
GameAssembly.dll de 47190016 bytes em Temp/T033Validation/Builds/Windows.
Essa é a build de produto com duas cenas, sem a entrada do Test Runner. Não foi
iniciada, para manter separado o aceite de execução e evitar usar o save pessoal.


Android ARM64 IL2CPP: sucesso, código 0, Build Finished Result: Success e
PROTOTYPE_BUILD_OK target=Android. APK de 35249928 bytes em
Temp/T033Validation/Builds/Android/QuackQuack.apk. A inspeção do ZIP confirmou
bibliotecas somente em lib/arm64-v8a, incluindo libil2cpp.so e libunity.so;
não há ARMv7/x86. A compilação utilizou SDK/NDK/OpenJDK/Gradle do módulo instalado.
Logs de compilação ignorados: Logs/t070-windows-build.log e
Logs/t070-android-build.log. Nenhum artefato binário é versionado.

## Reprodução e limites

Comandos executados com Unity.exe 6000.4.3f1:

~~~text
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t070-playmode.xml -logFile <repo>/Logs/t070-playmode.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testFilter PlatformBuildConfigurationSmokeTests -testResults <repo>/TestResults/t070-build-config.xml -logFile <repo>/Logs/t070-build-config.log
-batchmode -quit -buildTarget Win64 -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildWindows -logFile <repo>/Logs/t070-windows-build.log
-batchmode -quit -buildTarget Android -projectPath <copia> -executeMethod QuackQuack.Editor.PrototypeBuildPipeline.BuildAndroid -logFile <repo>/Logs/t070-android-build.log
~~~

Não houve instalação, publicação ou teste em aparelho Android. Compilação e
contratos do Editor não substituem execução, gameplay, legibilidade ou desempenho
nos alvos. A matriz completa e o aceite em dispositivos continuam na T075.
Os test players de smoke de tarefas anteriores têm relatórios próprios e não são
contabilizados como execução da build de produto desta task.

Checklist requirements.md: 16/16, sem alterações. .specify/extensions.yml ausente
antes e depois, sem hooks. Constituição preservada; T071 e posteriores pendentes.
