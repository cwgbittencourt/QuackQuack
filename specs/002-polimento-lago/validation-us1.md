# US1 — Validação de contenção, pressão e retomada (T007)

**Data**: 04/11/2026. **Estado**: T007 concluída para a regressão nativa e validação
manual dirigida descritas abaixo. **Base de código/cena**: ca27ed8 (T006), com T004/T005.
**Engine**: Unity 6000.4.3f1. Nenhuma alteração de gameplay nesta rodada.

## Resultado

| Verificação | Windows x64 IL2CPP | Android ARM64 IL2CPP |
|---|---|---|
| Regressão no player nativo | 35/35 aprovados | 35/35 aprovados |
| Falhas / ignorados | 0 / 0 | 0 / 0 |
| ArenaContainmentTests (instrumentação) | 10/10 | 10/10 |
| ArenaLakeRunReproductionTests | 25/25 | 25/25 |
| Subconjunto normativo T004 | 18/18, sem violação | 18/18, sem violação |
| Partida real com entrada física | Teclado, canto inferior esquerdo | Toque no S23, canto inferior esquerdo |
| Pressão, Quack e recuperação do movimento nesse ensaio | Confirmados pelo usuário | Avaliação positiva do usuário ao roteiro |

Os 17 cenários que falhavam antes da T005 passam nos dois players. Mesmos testes,
asserções e tolerâncias, sem Ignore/Explicit. Todos os 18 cenários normativos têm
firstInvalid=-1 e retomada aprovada. São cobertas quatro margens, quatro cantos, duas
pedras, passo longo 0,10 s, spawn original e quatro situações na fase Boss. Os outros
sete casos da segunda fixture são diagnósticos de captura/contrafactuais/repetição;
seu sucesso isolado não aprovaria contenção.

O caso de pressão mantém 48 passos com contato físico e deslocamento projetado de
aproximadamente 0,225 unidade em 1 s, nos dois players. No S23, 0,2254486; retomada
10,91999. Na T004, antes da correção, o mesmo ensaio deslocava 1,709099. O contato e
bloqueio permanecem, mas a pressão ficou menos intensa; não declarar equivalência de
balanceamento. Esses números são da simulação controlada, não métricas de FPS do produto.

## Confirmações na partida real

As builds normais usam RunBootstrap/LakeRun originais pelo PrototypeBuildPipeline, sem
invulnerabilidade, horda de teste, alteração de armas, câmera ou escala de tempo.

**Windows:** o executável desta rodada foi aberto; janela QuackQuack T007 observada com
computer-use, mostrando partida pausada. O usuário relatou: “O pato passou a ser empurrado
com menos intensidade, mesmo acumulando inimigos ele não chegou ultrapassar as margens.”
Confirmou depois Quack afastando inimigos, retorno do movimento pelo espaço liberado sem
prisão e pressão menor ainda perceptível: “Sim, Sim, com teclado, testei no canto esquerdo
inferior.” A observação visual não substitui esse relato nem a matriz física automatizada.

**Android:** Samsung SM-S918B (S23 Ultra), Android 17. APK normal instalado por ADB com
Success e atividade UnityPlayerGameActivity aberta com Status ok. Ao roteiro de toque
(pressão numa margem/canto e perto de pedra, Quack, joystick retomando e pressão ainda
perceptível), o usuário respondeu “Está ótimo” e esclareceu “testei no mesmo canto
esquerdo inferior”. Registrar avaliação positiva nesse local; não atribuir confirmação
manual de pedra, demais margens ou chefe a essa resposta.

## Preparação e identidade dos artefatos

Cópia isolada: Temp/Polimento002Validation. Helper Validation007Build somente nessa cópia
configura nome QuackQuack T007 e pacote com.cemggames.quackquack.validation002, separados
do jogo anterior e da baseline. Chama PrototypeBuildPipeline.Build com BuildOptions.None:
Windows x64 IL2CPP e Android somente ARM64 IL2CPP. Dois builds do produto com exit 0 e
marcador PROTOTYPE_BUILD_OK nos logs windows-build.log e android-build.log.

| Artefato do produto | SHA-256 |
|---|---|
| Builds/T007/Windows/QuackQuackT007.exe | 18D5D1AEA4095FBF1A99FB556003FB53C0D7600E147698664779E600D7350CBB |
| Builds/T007/Windows/GameAssembly.dll | CCD3E3FD86810990D64CA943FB8E445A60EF01F7720B41A70765530D8BF25D76 |
| Builds/T007/Android/QuackQuackT007.apk | BD258C63FD29FBF4E752980AD9A42080A41196C403131B5C8D7E53EECB4F0A89 |

Caminhos relativos à cópia isolada. GameAssembly identifica o código IL2CPP do produto
Windows; test players são builds separados, não o executável/APK usados no teste manual.
Saves de validação separados das versões anteriores. Nenhum serial, IP, cache, instalação,
binário, log ou XML entra no commit.

Comparação completa dos arquivos existentes de Assets/QuackQuack na raiz/cópia conferida
antes e depois: somente Settings/LakeURP.asset difere pelos prefilters de shader gerados
na importação/build. Fontes, cenas, prefabs, catálogos e seus .meta coincidem. Nenhuma
alteração de importação ou configuração da cópia foi levada ao projeto raiz.

- Manifest de pacotes idêntico: SHA-256
  5C07CC33BB93334ED832624562A5A06E40D97E25B98DC5D6250234A0C57309D4.
- LakeRun fonte/testada idêntica à T006: SHA-256
  04C2BC01279DC5459274737A79C3538DA44DD1B2F6A38676D8B03EA3DC9F3AF8.

## Reexecução e evidência local

Logs em Logs/Polimento002/T007; resultados em TestResults/Polimento002/T007.
XMLs windows-runtime.xml e android-runtime.xml: ambos com exit 0, 35 casos descobertos,
classes 10+25 conferidas e zero falhas/ignorados. Cada caso mantém seed, passo, posição,
primeira violação, contato e retomada no output. Logs/artefatos são regeneráveis e ignorados.

Runner na cópia, com backend IL2CPP configurado e sem -quit:

```text
Unity.exe -batchmode -projectPath <copia> -buildTarget Android -runTests
  -testPlatform Android -testFilter "ArenaContainmentTests;ArenaLakeRunReproductionTests"
  -playerHeartbeatTimeout 120 -buildPlayerPath <copia>/Builds/T007/AndroidTests
  -testResults <repo>/TestResults/Polimento002/T007/android-runtime.xml
  -logFile <repo>/Logs/Polimento002/T007/android-runtime.log
```

Para Windows: buildTarget Win64, testPlatform StandaloneWindows64, buildPlayerPath
Builds/T007/WindowsTests e nomes dos arquivos windows-runtime. Manter aparelho autorizado
para Android; nunca registrar serial. Fixtures controladas usam seed 42, Pato invulnerável
e simulação manual para isolar física, separados da partida normal com entrada física.

## Limites e validações posteriores

- A cobertura manual é dirigida ao canto inferior esquerdo, com teclado Windows e toque
  no S23. Outras margens, cantos, pedras e a fase do chefe foram cobertos pela regressão
  nativa, não por uma declaração fictícia de exploração manual integral.
- Não houve nova partida completa de 15 minutos, playtest com cinco participantes ou
  avaliação de todo o elenco/combate. A revisão transversal e a rodada ampla continuam
  nas tasks T048–T051; este resultado não encerra a T075 da feature 001.
- Controle físico não foi testado nesta rodada. Famílias Xbox/PlayStation/genérico e
  hotplug pertencem aos próximos aceites de entrada. S23 não representa Android
  intermediário/entrada.
- Nenhum profiling novo de FPS, p95, GPU ou GC do loop completo foi feito. Baseline T001
  anterior à correção não aprova desempenho atual; T049 continua necessária.
- Nenhuma suíte completa EditMode/PlayMode de Editor foi repetida aqui: 465/295 da T005
  e os 50 testes focados da T006 são históricos. Esta rodada acrescenta a execução nativa
  35+35 e as confirmações manuais acima.

Sem regressão encontrada nos critérios dirigidos de contenção/pressão/retomada; não foi
necessário modificar código, cenas ou dados. Continuidade somente mediante task solicitada.
