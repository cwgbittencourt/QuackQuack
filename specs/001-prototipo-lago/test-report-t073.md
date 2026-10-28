# T073 — Validador de ScriptableObjects no Editor

Data: 2026-10-28. Unity 6000.4.3f1, revisão 39d1a88d4dd1, Windows.
Escopo: somente T073. Próxima task: T074.

## Entrega

ContentValidationWindow oferece o menu QuackQuack > Validate content. A janela
mostra totais, erros/avisos, códigos, caminhos e propriedades, permite atualizar
o relatório e selecionar/pingar os assets envolvidos. Não há correção automática,
SetDirty, ApplyModifiedProperties ou SaveAssets no validador.

ValidateProject varre Assets/QuackQuack por padrão, carregando ScriptableObjects
principais e subassets de .asset/.inputactions. Ordena os resultados e não abre
cenas. Escopos inválidos/vazios, assets ilegíveis e entradas nulas são erros.
Validate aceita conjuntos explícitos, deduplicando referências repetidas ao mesmo
objeto antes de verificar unicidade.

As regras cobrem:

- IDs ContentDefinition únicos entre tipos, por comparação ordinal; cada asset
  duplicado recebe diagnóstico, sem confundir referências repetidas com novos IDs.
- IDs de passivas, armas resultantes e inimigos resolvidos com destino único e tipo
  correto; vínculos arma/passiva recíprocos e combinações evolutivas repetidas.
- Exceção explícita do ID sapo-rei no encontro FrogKing, cujo controlador é externo
  ao catálogo de EnemyDefinition e fornecido pela configuração de partida.
- Limites, cardinalidades e referências obrigatórias pelos IsValid existentes dos
  dados/catálogos/settings; desafios pelos CreateRules existentes.
- Configuração comum de entrada, mapa Gameplay/Move/Quack/Pause e subassets
  InputActionReference que precisam resolver asset e ação.
- Propriedades serializadas com NaN/Infinity ou referência quebrada, inclusive
  opcional; referências opcionais simplesmente vazias não viram erro genérico.

Exceções de validação são reportadas por asset e não impedem checar os demais.
Tipos sem regra de domínio registrada recebem UnsupportedType como aviso e passam
pela verificação estrutural. ValidateForCI registra o relatório, lança
BuildFailedException em erros e emite T073_CONTENT_VALIDATION_OK no sucesso.
Avisos não bloqueiam. A entrada é explícita, sem hook novo no pipeline T070.

## Varredura dos assets reais

Execução em lote final: **63 ScriptableObjects, zero erros, dois avisos**, código 0.
T073_CONTENT_VALIDATION_OK assets=63 warnings=2.

Os avisos correspondem a Assets/QuackQuack/Settings/LakeRenderer2D.asset
(Renderer2DData) e LakeURP.asset (UniversalRenderPipelineAsset). São tipos nativos
do pacote sem IsValid de domínio registrado neste projeto. Recebem a checagem
estrutural de referências quebradas e valores finitos; seus limites internos não
são certificados pela ferramenta. Nenhum asset de conteúdo precisou de alteração.

## Testes

| Execução | Total | Aprovados | Falhas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|
| Contrato vermelho antes da implementação | 1 | 0 | 1 esperada | 0 | 2 |
| EditMode completo final | 465 | 465 | 0 | 0 | 0 |
| Varredura CI final | 63 assets | 0 erros | — | — | 0 |

Os nove ContentValidationTests cobrem projeto real válido/ordenação estável,
leitura sem alterar bytes/dirty state do asset de controle, duplicatas entre tipos,
deduplicação de objetos, limites e NaN, referência obrigatória ausente, IDs
inexistentes/de tipo errado, reciprocidade, receitas repetidas, escopos inválidos,
limites de catálogo e IDs internos repetidos de desafios.

Uma referência opcional quebrada é produzida criando texture/sprite/weapon pelo
AssetDatabase e apagando o sprite: o validador identifica projectileSprite mesmo
sem a referência ser obrigatória. O caso de lote persiste um Quack inválido em
fixture exclusiva e confirma o log de erro e BuildFailedException. Nenhum YAML
foi editado manualmente para fabricar cenários inválidos.

A primeira execução completa passou 464/465: a fixture de vínculos que pretendia
ser válida deixava a arma sem resultado de evolução, infringindo a regra existente
de exigir passiva e resultado juntos. A fixture foi completada com um resultado
válido, preservando o validador/regra. A execução final também verifica o caminho
absoluto de limpeza das fixtures dentro de Assets/QuackQuack/Tests, com prefixo
T073Fixtures e GUID próprio. Não restaram diretórios de fixture na cópia isolada.

O asmdef EditModeTests passou a referenciar QuackQuack.Editor para testar a API
real diretamente. As duas assemblies continuam limitadas ao Editor; não houve
alteração de código/assemblies de runtime. Os .meta dos dois scripts novos foram
gerados pelo Unity e preservados no projeto principal.

Artefatos regeneráveis ignorados:

- TestResults/t073-red.xml e Logs/t073-red.log: fase vermelha.
- TestResults/t073-editmode-verified.xml e Logs/t073-editmode-verified.log: 465/465.
- Logs/t073-content-validation-final.log: varredura final de 63 assets, código 0.
- TestResults/t073-editmode.xml registra a fixture inicialmente incompleta;
  t073-editmode-final.xml registra 465/465 antes da asserção adicional de limpeza.

Comandos Unity executados na cópia isolada Temp/T033Validation:

~~~text
-batchmode -nographics -projectPath <copia> -runTests -testPlatform EditMode -testResults <repo>/TestResults/t073-editmode-verified.xml -logFile <repo>/Logs/t073-editmode-verified.log
-batchmode -nographics -quit -projectPath <copia> -executeMethod QuackQuack.Editor.ContentValidationWindow.ValidateForCI -logFile <repo>/Logs/t073-content-validation-final.log
~~~

## Limites

Validação de dados no Editor não substitui integração de cenas/prefabs completos,
leitura visual, gameplay, builds ou profiling. PlayMode e players não foram
reexecutados: a alteração é restrita às ferramentas/testes do Editor. IDs globais
são os de ContentDefinition; desafios internos seguem a regra do seu catálogo.
O universo de resolução por ID é o conjunto/escopo validado, não todos os pacotes
ou arquivos fora de Assets/QuackQuack.

Checklist requirements.md 16/16, sem alterações. .specify/extensions.yml ausente
antes/depois, sem hooks. Constituição preservada; nenhum dado autoral, cena,
ProjectSettings ou regra de gameplay alterado. T074 e posteriores permanecem pendentes.
