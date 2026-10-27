# T057 — Fases do Sapo-Rei

Data: 2026-10-27. Unity 6000.4.3f1, revisão 39d1a88d4dd1.
Escopo: somente T057. T058 em diante permanecem pendentes.

## Implementação

FrogKingController implementa os quatro padrões com a mesma fronteira de aviso
e commit da T049. O alvo é fixado no aviso, há antecedência visual/sonora e
correlação única. Pedidos concorrentes não substituem o ataque em preparação.
O salto aterrissa na posição marcada; ondas fazem interseção varrida do anel;
girinos perseguem e aplicam contato; a terceira fase destrói refúgios designados.

FrogKingSettings.asset concentra os parâmetros provisórios: 1000 de vida,
fases em 70%/35%, aviso de 1 s e recuperação de 1,5 s. Salto: raio 2/dano 18.
Onda: velocidade 4, largura de contato 0,4, duração 3 s/dano 12.
Girinos: quatro por invocação, velocidade 2, raio 0,3, duração 10 s/dano 6.
O limite compartilhado é de 24 efeitos ativos. Cada família tem 24 instâncias
pré-aquecidas: ondas em Projectile, girinos em Enemy; excedentes são suprimidos.
O controlador atualiza os efeitos em conjunto, sem Update por girino/onda.

O chefe também implementa IPoolLifecycle. Reset limpa serviços, assinaturas,
estado, efeitos e ataques; reutilização reaproveita os pools registrados.
Vida zero encerra a partida como Won/Lost; terminal/desativação cancelam os
avisos e devolvem efeitos. Dano respeita a invulnerabilidade do Pato.

Unity gerou o asset de settings, os prefabs do chefe/efeitos, a cena de treino e
os arquivos meta. O gerador usa formas, material e áudio temporários já existentes.
Bootstrap/Lake não foram modificados. A fixture FrogKingPractice é exclusiva do
Editor e permite esquivar e aplicar dano por botão para explorar as três fases.

## Validação observada

Execução na cópia isolada `Temp/T033Validation`, com fontes/Packages/ProjectSettings
do projeto. Saídas regeneráveis permanecem ignoradas por Git.

| Suíte | Total | Aprovados | Falhas |
|---|---:|---:|---:|
| EditMode completo | 378 | 349 | 29 esperadas |
| PlayMode completo | 149 | 149 | 0 |

As 29 falhas EditMode são exatamente 18 RunResultTests (T058/T060) e 11
SaveServiceTests (T059), já existentes. Exit code 2 por esses testes em fase
vermelha; os dez novos FrogKingSettingsTests passaram.

PlayMode terminou com exit code 0. Os nove FrogKingTelegraphTests da T049 agora
usam chamadas tipadas ao controlador real e passaram. Os 14 novos casos de
FrogKingEncounterTests verificam elegibilidade/seleção de fases, alvo fixo,
esquiva, dano após aviso, invulnerabilidade, onda varrida/expiração, proteção e
destruição de refúgio, perseguição, limite/reuso do pool, ausência de dano em
passo zero, cancelamento terminal, descarte do pool e vitória única.

FrogKingPracticeTests carregou a cena pela API do Editor, confirmou serviços,
chefe ativo e marcação e capturou `Logs/t057-practice.png` em 1280×720.
A imagem foi inspecionada: Pato amarelo, chefe verde, três refúgios e marcação
laranja visíveis. A captura é da câmera, sem o overlay OnGUI.
A primeira execução completa teve uma falha por tentar carregar a fixture fora
da lista de build; corrigido com LoadSceneAsyncInPlayMode, a repetição passou.

Comandos Unity usados: `-executeMethod QuackQuack.Editor.FrogKingSetup.Build`
com `-batchmode -nographics -quit`; testes com `-batchmode -runTests
-testPlatform EditMode|PlayMode -testResults <arquivo> -logFile <arquivo>`.
EditMode usou `-nographics`; PlayMode manteve gráficos/áudio.
Resultados locais: `TestResults/t057-editmode.xml`, `TestResults/t057-playmode.xml`;
logs em `Logs/t057-*.log`.

## Limites do aceite

Não houve playtest humano, build Windows/Android ou perfil de desempenho.
Componentes e captura não comprovam percepção sonora, justiça de gameplay ou
metas de FPS/GC. A arena usa um botão de dano como instrumento de teste.
A ligação das armas/Quack ao chefe e aos novos inimigos, entrada aos 900 segundos,
pausa durante o encontro e encerramento integrado com resultados/save pertencem
à composição posterior da partida. Os girinos são invocações de contato com
expiração, sem recompensas; não ampliam o catálogo de cinco inimigos comuns.
