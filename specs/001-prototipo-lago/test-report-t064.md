# T064 — Smoke tests de build e inicialização

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T064. Próxima task: T065. A pipeline de produto T070 não foi implementada.

## Entrega

PlatformBuildSmokeTests.cs contém seis casos PlayMode. Quatro casos, somente no
Editor, exigem a futura PrototypeBuildPipeline real por reflexão: CreateOptions
para Windows/Android, cenas compartilhadas com RunBootstrap primeiro e LakeRun,
alvo/caminho de saída corretos, sem duplicatas/fixtures de teste; ConfigureTarget
para IL2CPP e somente ARM64 no Android. Os testes partem de Mono/ARMv7 para evitar
aprovar um método vazio por herdar configurações já corretas. Restauram os valores
anteriores em finally. Como o tipo T070 ainda falta, essa lógica posterior continua
pendente de execução. Não há stub, Ignore ou troca automática do alvo pelos testes.

Dois casos de runtime, categoria PlatformRuntimeSmoke, também compilam nos players.
Verificam versão Unity, disponibilidade de RunBootstrap/LakeRun e inicialização real
da partida. Exigem diretor pronto em até 30 segundos, bootstrap único, eventos/pool
válidos, Playing, Pato vivo, HUD, câmera, catálogos, spawn/scheduler e relógio avançando
em até dois segundos. No player, exigem WindowsPlayer/Android, 64-bit e ENABLE_IL2CPP.

Cada caso usa diretório exclusivo sob Application.temporaryCachePath e injeta o
SavePathProvider antes de carregar cenas. A limpeza mantém esse provider até o unload,
restaura o anterior e verifica caminho absoluto, prefixo do cache e nome próprio antes
da remoção recursiva. Não lê nem altera o save pessoal. O caso que somente verifica
cenas não descarrega a cena do runner. Nenhum código de produção ou asset foi alterado.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução | Total | Aprovados | Falhas esperadas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| Smoke tests focados no Editor, cleanup corrigido | 6 | 2 | 4 | 0 | 2 |
| PlayMode completo final | 208 | 185 | 23 | 0 | 2 |
| Test player Windows x64 IL2CPP | 2 | 2 | 0 | 0 | 0 |

As quatro falhas novas têm exclusivamente a causa T070/PrototypeBuildPipeline
ausente. As outras 19 são as dez da T062 e nove da T063, sem regressão nos 183 casos
antes aprovados. O Editor aprovou os dois novos testes de runtime.

O Unity Test Framework compilou e iniciou PlayerWithTests.exe. O log registra
Build Finished, Result: Success; o XML do player contém os dois casos aprovados e
T064_RUNTIME_SMOKE_OK platform=WindowsPlayer unity=6000.4.3f1. A asserção de 64-bit
e a guarda ENABLE_IL2CPP passaram. O arquivo usa UnityEditor apenas sob compilação
condicional; o player não depende das classes Editor de configuração.

O artefato é um test player Development com cena inicial do Test Runner, incluindo
as cenas de gameplay. Não é uma build de release nem comprova o contrato de ordem
da entrada da pipeline T070. Artefatos estão em Temp/T033Validation/Builds/T064Windows/
PlayerWithTests e permanecem ignorados, assim como caches, logs e XML.

A primeira rodada focada precisou ser encerrada: o cleanup do caso sem load de
produção descarregava a cena do Test Runner. A fixture foi corrigida para descarregar
somente o que carregou. A primeira tentativa de player expôs Is.AnyOf indisponível
no NUnit empacotado, em ramo exclusivo de player; a asserção foi corrigida para
EqualTo(...).Or.EqualTo(...) e a segunda build passou. A revisão final acrescentou
a pré-condição Mono/ARMv7 nos casos Editor, sem alterar o runtime já aprovado.

O .meta foi gerado pelo Unity e os hashes entre geração e fonte foram comparados,
permanecendo idênticos. EditMode não foi repetido; a última evidência é T061, 456/456.

Comandos: -batchmode -runTests -testPlatform PlayMode para a suíte Editor. Para o
player: -testPlatform StandaloneWindows64 -testCategory PlatformRuntimeSmoke
-playerHeartbeatTimeout 120 -buildPlayerPath <pasta> -testResults <arquivo>
-logFile <arquivo>. Sem -quit e com gráficos. Resultados finais ignorados:
TestResults/t064-smoke-confirm.xml, t064-playmode-final.xml,
t064-windows-player-confirm.xml e Logs/t064-*.log.

## Limites

Os módulos Windows/Android estão presentes e o Visual Studio inclui ferramentas
C++ x64. Não foi gerada ou executada build Android nesta tarefa; não há aceite em
aparelho Android. Os testes estão preparados para execução nele pelo Test Framework.
O sucesso Windows cobre inicialização do test player, sem aprovar build de produto,
controles, Safe Area, gameplay, FPS/GC ou desempenho. Esses aceites permanecem nas
tarefas correspondentes, incluindo T070/T075. Nenhuma dependência foi acrescentada.
