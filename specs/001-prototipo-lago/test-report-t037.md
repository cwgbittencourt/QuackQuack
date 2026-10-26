# Validação de T037

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T037 concluída. PassiveDefinition é um ScriptableObject com ID, atributo,
bônus por nível, teto editável, nível máximo e ID da arma compatível. Campos são
privados/serializados e a API pública oferece somente leitura e consulta pura.

Os bônus são percentuais normalizados positivos até um (100%). O incremento não
excede o teto autoral. GetModifier aceita nível zero a MaxLevel, calcula desde a
base e limita o resultado, sem alterar o asset. Níveis fora da faixa e dados
inválidos são rejeitados. A multiplicação usa double antes do teto para manter
o resultado limitado mesmo para níveis grandes. O máximo de níveis de passiva é
autoral, sem transferir a exigência de cinco níveis das armas às passivas.

WeaponDefinition.IsValidCatalog agora recebe IEnumerable<PassiveDefinition>.
Valida referências de passivas a armas existentes e reciprocidade quando a arma
declara uma passiva de evolução. A fixture genérica da T036 foi substituída por
uma PassiveDefinition real. Nenhuma implementação de EvolutionDefinition, build
ou efeitos de combate foi antecipada.

14 testes novos de passiva verificam bônus limitados, ausência de mutação, valores
não finitos e limites, IDs obrigatórios, atributo, nível máximo, índices e níveis
grandes. Um teste adicional do catálogo verifica arma desconhecida, vínculo
contraditório, vínculo válido e passiva ausente. Os testes existentes da arma
continuam passando com a coleção tipada.

| Etapa | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| T037 vermelha: validação mínima da passiva | 6 | 8 | 2 |
| EditMode final | 113 | 43 esperadas de T033/T034 | 2 |

Sem erros de compilação. As falhas finais continuam exatamente nas 23 ausências
de BuildState e 20 de UpgradeSystem; nenhuma falha nova de regressão.
XMLs regeneráveis: TestResults/t037-red.xml e TestResults/t037-editmode.xml,
com logs correspondentes em Logs/. Execução em Temp/T033Validation, batchmode,
nographics, runTests, testPlatform EditMode. Esses resultados ficam fora do Git.

O Unity gerou os .meta da pasta, script e testes, copiados com GUIDs preservados.
Campos vazios de .meta podem conter os espaços finais emitidos pelo Unity;
scripts e documentação passam na verificação de whitespace.

PlayMode não foi repetido: a alteração introduz e valida dados, sem modificar
controladores ou composição das cenas. A última execução T035 permanece com 34
aprovados e seis falhas esperadas. Não há catálogo autoral de seis passivas,
aplicação dos bônus durante a partida, aceite de gameplay, builds ou desempenho.
T038 em diante continuam pendentes.
