# T069 — HUD responsivo nas cenas do Lago

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T069. Próxima task: T070.

## Entrega

Lake.unity e LakeRun.unity usam CanvasScaler ScaleWithScreenSize, referência
1920×1080, MatchWidthOrHeight pela altura. Os painéis existentes de vida/Fôlego,
relógio e mensagens foram movidos para HUD Safe Area, com RectTransform esticado
e SafeAreaController T068. Seus pontos de ancoragem continuam respectivamente
superior esquerdo, superior direito e centro inferior, agora relativos à área segura.

ResponsiveHudSetup.Build configura as duas cenas existentes pela API do Editor,
sem reconstruir o gameplay. GameplayHudSetup.Attach reutiliza Configure para que
reconstruções futuras preservem o layout. LakeRunSetup já parte da cena Lake.
GUIDs das cenas e referências do GameplayHud foram preservados. O .meta do novo
script foi gerado pelo Unity. O salvamento nativo também explicitou trainingPickups=1
em Lake, igual ao valor padrão já usado em runtime; LakeRun mantém seu valor falso.
Nenhum YAML foi editado manualmente. Nenhuma regra de simulação foi alterada.

## Validação

O contrato vermelho anterior tinha quatro falhas de HUD na T068 pela ausência de
CanvasScaler. Todos esses casos agora passam com o HUD autoral, sem layout substituto
criado pela fixture. Testes medem a projeção real do Canvas e os limites dos oito
elementos essenciais, incluindo altura necessária aos textos.

| Execução | Total | Aprovados | Falhas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| PlayMode completo | 237 | 233 | 4 esperadas | 0 | 2 |
| UI + fluxo de produção, após ampliar asserções/capturas | 16 | 16 | 0 | 0 | 0 |
| ResponsiveUiTests, incluídos acima | 15 | 15 | 0 | 0 | — |

As quatro falhas restantes são exclusivamente PlatformBuildConfigurationSmokeTests,
por PrototypeBuildPipeline ainda ausente (T070). O caso existente de RunSceneFlowTests
agora verifica também o CanvasScaler e os três painéis sob SafeAreaController em
LakeRun. A fixture conserva o isolamento de save por diretório temporário próprio.

As quatro capturas geradas pelos casos com recorte foram inspecionadas: vida/Fôlego,
terreno, Quack, relógio e mensagens estão dentro dos limites e os painéis não se
sobrepõem em 1920×1080, 2160×1080, 2340×1080 e 1440×1080. Recorte sintético: esquerda
72, direita 24, inferior 32 e superior 48 pixels. As capturas usam câmera própria
para isolar o HUD, sobre fundo uniforme; não representam composição de gameplay.
O controlador também mantém os casos de mudança de resolução/recorte, remoção de
margens, geometria indisponível e reativação aprovados.

Artefatos regeneráveis ignorados:

- Logs/t069-authoring.log: salvamento nativo, código 0, T069_RESPONSIVE_HUD_SAVED.
- TestResults/t069-playmode.xml e Logs/t069-playmode.log: suíte completa.
- TestResults/t069-ui-final.xml e Logs/t069-ui-final.log: verificação final focada.
- Logs/t069-hud-1920x1080.png, t069-hud-2160x1080.png,
  t069-hud-2340x1080.png e t069-hud-1440x1080.png: capturas.

Comandos Unity executados na cópia isolada Temp/T033Validation:

~~~text
-batchmode -quit -projectPath <copia> -executeMethod QuackQuack.Editor.ResponsiveHudSetup.Build -logFile <repo>/Logs/t069-authoring.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t069-playmode.xml -logFile <repo>/Logs/t069-playmode.log
-batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testFilter ResponsiveUiTests;RunSceneFlowTests -testResults <repo>/TestResults/t069-ui-final.xml -logFile <repo>/Logs/t069-ui-final.log
~~~

EditMode não repetido: Core não foi alterado. Nenhuma build Windows/Android ou teste
em aparelho foi executado nesta task. A validação cobre o HUD; cartas, loadout,
resultados, controles móveis e aceite integral de legibilidade/aparelhos continuam
na matriz T075. Não há aceite de desempenho ou gameplay com base nesta execução.

Checklist requirements.md: 16/16, sem alteração. .specify/extensions.yml ausente
antes e depois; nenhum hook a executar. Constituição preservada. T070 e tarefas
posteriores permanecem pendentes.
