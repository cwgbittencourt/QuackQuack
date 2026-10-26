# Validação de T038

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T038 concluída. EvolutionDefinition é um ScriptableObject com referências
tipadas à arma base, passiva requerida e arma resultante. A API somente de leitura
fixa o requisito de nível em cinco e valida conteúdo/referências antes de permitir
uma consulta de elegibilidade.

IsValid exige referências não nulas, definições válidas e concordância com os IDs
de compatibilidade/resultado da arma e da passiva. Autoevolução é rejeitada por ID.
CanEvolve exige a arma base no nível exatamente cinco e a passiva correspondente
presente, sem alterar nenhum asset nem conceder/substituir itens na build.

IsValidCatalog reutiliza o validador tipado de armas/passivas, verifica IDs de
evolução únicos e pares arma/passiva únicos. Cada referência deve ser exatamente
o asset registrado no catálogo; uma cópia com o mesmo ID não é aceita. Entradas
nulas, catálogos ausentes/vazios e conteúdo inválido retornam motivo de rejeição.
Os catálogos recebidos de IEnumerable são materializados uma vez para permitir
validação e busca mesmo com uma fonte de enumeração única.

17 casos EditMode cobrem receita válida, ausência de mutação, nível cinco e níveis
inválidos, arma/passiva ausentes ou diferentes, referências nulas/inválidas,
autoevolução, vínculos contraditórios, IDs/combinações duplicados, referências não
registradas e entradas inválidas. Fixtures são criadas em memória e configuradas
por SerializedObject; nenhum asset/cena serializado foi editado manualmente.

| Etapa | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| T038 vermelha: validação mínima | 6 | 11 | 2 |
| EditMode final | 130 | 43 esperadas de T033/T034 | 2 |

Os 17 testes novos passaram. Não houve erro de compilação ou falha nova de
regressão; as restantes são exatamente as 23 da ausência de BuildState (T039)
e 20 da ausência de UpgradeSystem (T040).

Execução em Temp/T033Validation, batchmode/nographics/runTests, testPlatform
EditMode. Evidências regeneráveis fora do Git: TestResults/t038-red.xml e
TestResults/t038-editmode.xml, com logs correspondentes em Logs/. O Unity gerou
os .meta do script/teste, copiados com GUIDs preservados. git diff --check sem
ocorrências nos arquivos rastreados; a verificação do índice é feita no commit.

Não foram implementadas a aplicação na BuildState, ofertas, feedback ou as três
combinações autorais de T043. O validador aceita subconjuntos não vazios para
autoria/testes; a cardinalidade final de três pertence ao catálogo do protótipo.
PlayMode não foi repetido porque não houve alteração de controladores/cenas;
a última execução T035 permanece com 34 aprovados e seis falhas esperadas.
Não há aceite de gameplay, builds, plataformas ou desempenho. T039 em diante
continuam pendentes.
