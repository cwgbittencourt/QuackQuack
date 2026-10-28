# T068 — Controlador de Safe Area

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T068. Próxima task: T069.

## Entrega

SafeAreaController em Presentation/UI requer RectTransform e normaliza os limites
Screen.safeArea pelo viewport Screen.width/height. Atualiza em OnEnable/LateUpdate
quando resolução ou recorte mudam, incluindo mudança de lado sem redimensionar.
Zera offsets, evita acúmulo e restaura âncoras zero/um quando os recortes desaparecem.

Fontes Func<Vector2>/Func<Rect> podem ser injetadas com SetViewportProvider para
testes determinísticos; argumentos null restauram individualmente as fontes Screen.
Geometria não finita, não positiva ou sem interseção útil mantém o último layout
válido até a recuperação. Limites parcialmente externos são cortados ao viewport.
O componente deve ficar em um filho direto de Canvas de tela inteira; conteúdo
protegido fica abaixo dele. Não suporta subviewports por câmera.

Nenhuma cena ou prefab foi alterado. A configuração de CanvasScaler e da hierarquia
do HUD autoral permanece na T069. O .meta do novo script foi gerado pelo Unity na
cópia isolada e preservado no projeto. Sem mudança de regras de gameplay/Core.

## Validação

A fase vermelha anterior está registrada na T063 e na suíte T067: cinco testes
falhavam pela ausência do controlador. Esses testes agora usam chamadas tipadas,
preservando as asserções de geometria. Dois casos adicionais verificam geometria
indisponível/recuperação, clipping, reativação e retorno às fontes reais Screen.

Execução PlayMode completa na cópia isolada Temp/T033Validation, com renderização:

| Resultado | Quantidade |
|---|---:|
| Total | 237 |
| Aprovados | 229 |
| Falhas esperadas | 8 |
| Ignorados | 0 |
| ResponsiveUiTests | 11 aprovados / 4 falhas esperadas |
| Casos do controlador T068 | 7 aprovados / 0 falhas |

As quatro proporções são 1920×1080, 2160×1080, 2340×1080 e 1440×1080.
Os quatro casos restantes de ResponsiveUiTests falham pela ausência do CanvasScaler
autoral (T069). As outras quatro falhas pertencem a PlatformBuildConfigurationSmokeTests,
por PrototypeBuildPipeline ausente (T070). Não houve falha inesperada.

Artefatos regeneráveis ignorados: TestResults/t068-playmode.xml e
Logs/t068-playmode.log. Unity encerrou com código 2 devido aos oito contratos
pendentes. Uma primeira tentativa encontrou referência residual ao helper de
reflexão removido; corrigida antes da execução completa registrada acima.

Comando executado com Unity.exe 6000.4.3f1:

~~~text
-batchmode -projectPath <repo>/Temp/T033Validation -runTests -testPlatform PlayMode -testResults <repo>/TestResults/t068-playmode.xml -logFile <repo>/Logs/t068-playmode.log
~~~

EditMode não repetido: a alteração não toca regras Core. Nenhuma build Windows ou
Android foi executada nesta task. Testes de âncoras no Editor não aprovam recortes
reais, legibilidade de todas as telas, gameplay ou desempenho em aparelhos.

Checklist requirements.md: 16/16, sem alterações. .specify/extensions.yml ausente
nas verificações anterior e posterior; nenhum hook a executar. Constituição
preservada; T069 e tarefas posteriores continuam pendentes.
