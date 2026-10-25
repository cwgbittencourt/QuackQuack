# Validação de T030

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T030 concluída. A cena Lake existente foi aberta e atualizada pelo Unity,
preservando sua câmera e o GUID fb86267c7de2e40479ccd6bec2e07d8a. Não foi recriada.
LakeArenaSetup produz uma raiz de conteúdo de treino, dados Pato/PatoQuack,
sprites geométricos, materiais unlit e prefabs para alvo, projétil e recompensa.
Todos os assets serializados e novos .meta foram gerados pelo Editor na cópia de
validação e trazidos ao projeto. Nenhum YAML foi escrito manualmente.

A arena tem Pato controlável, terra, lago com ilha, oito obstáculos/limites sólidos,
dois alvos vermelhos, emissor de projéteis leves e recompensa dourada em um corredor
guardado. As placas são instruções estáticas no mundo, sem implementar o HUD de T031.
O jogador inicia em terra à esquerda, longe de contato imediato com os alvos.

LakeArenaController compõe o cenário de treino, com aquisição e devolução de inimigos,
projéteis e recompensa exclusivamente pelo PoolService. As fábricas de prefabs são
pré-aquecidas na inicialização. Atualização dos alvos e projéteis é centralizada.
Alvos são guardas estacionários, sofrem Quack e causam dano por proximidade quando não
interrompidos. Projéteis deixam o pool, respeitam obstáculos/limites e retornam ao pool.
A recompensa concede recuperação de Fôlego e retorna ao pool uma única vez.

Timers de disparo e interrupção usam o tempo da partida. PlayerController passou a
gravar o prazo de interrupção nesse mesmo relógio, para a arena não perder o efeito
durante pausa. O teste de integração existente foi ajustado para esse contrato.
O antigo MvpArena iniciado permanece sem uso na cena; HUD e VFX/áudio não foram ativados.
As tasks T031–T032 continuam pendentes.

LakeArenaTests contém dois testes da cena real, carregada via Bootstrap: composição
e serviços/pools; Quack no alvo, água, pausa e coleta única da recompensa. A regressão
de Bootstrap foi ajustada para descarregar Lake ao terminar e não deixar objetos da
arena nos testes seguintes. Os testes anteriores continuam cobrindo movimento, colisão,
reflexão, terreno, Fôlego e eventos únicos.

Etapa vermelha: um teste falhou pela ausência do Pato na cena original, código 2.
Durante a integração foram corrigidas referências de prefab perdidas ao abrir a cena
no gerador; o teste de coleta passou a aguardar o passo de física após teletransporte.
Os materiais receberam referências explícitas às texturas e as placas foram reduzidas
após inspeção da prévia renderizada.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 25 | 0 | 0 |

Execução na cópia temporária Temp/T022Validation, com batchmode, nographics, runTests
e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t030-red.xml, TestResults/t030-editmode.xml e TestResults/t030-playmode.xml.
Logs correspondentes em Logs/t030-red.log, Logs/t030-editmode.log e Logs/t030-playmode.log.
A geração usa executeMethod QuackQuack.Editor.LakeArenaSetup.Build. A prévia usa
BuildAndPreview com gráficos habilitados e SubmitRenderRequest da URP.

Logs/t030-preview.png foi inspecionado visualmente em 1280×720: Pato, terreno, alvos,
corredor, recompensa e instruções estão visíveis. É uma renderização da cena com
exemplos dos prefabs, não uma captura de playtest humano. A prévia, logs, XMLs e cópia
temporária são regeneráveis e ficam fora do Git. Unity mantém espaços finais em
alguns campos vazios dos assets serializados; os fontes C# e documentos foram verificados.

A alteração preexistente de ícones em ProjectSettings/ProjectSettings.asset permanece
fora do commit. Esta entrega valida montagem e integração automatizada; não declara
aceite de gameplay de US1, builds Windows/Android, experiência de usuário ou desempenho.
T031 em diante permanecem pendentes.
