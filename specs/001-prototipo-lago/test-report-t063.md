# T063 — Testes de UI responsiva e Safe Area

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T063. Próxima task: T064. T068/T069 não foram implementadas.

## Entrega

ResponsiveUiTests contém 13 casos PlayMode, carregando Bootstrap/Lake reais.
A matriz é paisagem: 1920×1080 (16:9), 2160×1080 (18:9), 2340×1080 (19,5:9)
e 1440×1080 (4:3). Câmera própria e RenderTexture definem o viewport sem alterar
a resolução do computador ou os arquivos das cenas. O treino é pausado e o HUD
recebe snapshot controlado, com vida/Fôlego parciais, água, estado do Quack,
mensagem e relógio. Não há I/O de save pessoal nesta fixture de treino.

Quatro casos verificam os oito bindings essenciais ativos, todos os cantos dentro
do viewport sem recortes e altura dos textos dentro de suas caixas. Quatro exigem
CanvasScaler autoral, ScaleWithScreenSize e referência 1920×1080; depois verificam
a hierarquia segura e a contenção em recorte assimétrico. A fixture não acrescenta
um scaler para mascarar a ausência da T069.

Outros quatro casos exigem SafeAreaController real para normalizar anchors,
zerar offsets, não acumular margens e restaurar o viewport inteiro. A sequência
dinâmica exige atualização em frames normais após mudar 16:9 para 19,5:9, espelhar
o recorte, alterar somente a Safe Area com resolução constante e retirar margens.
As margens sintéticas são 72 px à esquerda, 24 à direita, 32 abaixo e 48 acima.
Não representam dados de um aparelho específico.

O contrato T068 usa SetViewportProvider(Func<Vector2>, Func<Rect>) como fonte
injetável para o componente real; o jogo usará dados reais por padrão. A atualização
acontece em LateUpdate, também testada aguardando frames. Reflexão permite compilar
antes da T068 sem componente falso ou teste ignorado; substituir por chamadas
tipadas quando a implementação existir. O contrato está em gameplay-contracts.md.

## Validação observada

Execução em Temp/T033Validation, cópia isolada de Assets/Packages/ProjectSettings.

| Execução final | Total | Aprovados | Falhas esperadas | Ignorados | Exit code |
|---|---:|---:|---:|---:|---:|
| ResponsiveUiTests focado | 13 | 4 | 9 | 0 | 2 |
| PlayMode completo | 202 | 183 | 19 | 0 | 2 |

As nove falhas da T063 são exclusivamente quatro ausências de CanvasScaler na
composição autoral (T069) e cinco ausências de SafeAreaController (T068). As
asserções posteriores a essas dependências ainda precisam ser exercitadas após
as implementações. Os quatro baselines sem recortes passaram nas quatro proporções.
As outras dez falhas são as mesmas ausências de GamepadInputAdapter e
MobileInputAdapter da T062. Todos os 179 casos antes aprovados continuaram passando.

Duas rodadas intermediárias detectaram medições prematuras na fixture: mudar o
render target não atualiza imediatamente a projeção e os anchors dos filhos do
Canvas. A versão final fixa o aspecto da câmera, aguarda dois frames e confirma
que os cantos do Canvas projetam exatamente o viewport antes de medir o HUD.
Assim, o falso extravasamento inicial em 4:3 não foi atribuído ao layout autoral.
A rodada focada final e a suíte completa usam a fixture corrigida.

O .meta foi gerado pelo Unity e copiado com seu GUID preservado. Teardown libera
render target, câmera e objetos próprios, destrói o bootstrap e descarrega a cena
de treino. Nenhuma cena, prefab, conteúdo ou código de produção foi alterado.
EditMode não foi repetido: a última evidência permanece T061, 456/456 aprovados.

Comandos: -batchmode -runTests -testPlatform PlayMode -testResults <arquivo>
-logFile <arquivo>; filtro focado: QuackQuack.Tests.PlayMode.ResponsiveUiTests.
Todas as rodadas mantiveram gráficos. Evidências regeneráveis ignoradas:
TestResults/t063-responsive.xml, t063-responsive-confirm.xml,
t063-responsive-final.xml, t063-playmode.xml e Logs/t063-*.log.

## Limites

São testes geométricos do HUD e altura de textos, não aceite visual humano de
todas as telas/modais, Safe Area em Android real, toque, builds ou desempenho.
Não houve geração/inspeção de capturas nesta tarefa. A troca do lado do recorte
representa mudança em paisagem; retrato não faz parte da matriz do produto.
Aceite integral de interface e legibilidade permanece na T075.
