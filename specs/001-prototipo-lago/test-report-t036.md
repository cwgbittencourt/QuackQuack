# Validação de T036

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T036 concluída. WeaponDefinition é um ScriptableObject com exatamente
cinco níveis, modo de alvo, vínculo por IDs estáveis à passiva e arma evoluída e
afinidade opcional de dano/intervalo por terreno. WeaponLevel é uma struct de
valores com getters; a definição não expõe arrays ou setters públicos.

IsValid rejeita dados locais inválidos, incluindo números não finitos, limites
dos cinco níveis, modo inexistente, ausência de projétil e área, vínculo evolutivo
incompleto e autoevolução. IsValidCatalog valida definições, IDs únicos e referências
existentes. Como PassiveDefinition ainda pertence a T037, a coleção de passivas
usa ContentDefinition e deve ser fornecida pelo chamador como catálogo de passivas.
O teste dessa fronteira usa apenas um ContentDefinition existente como fixture de
ID, sem afirmar que esse asset seja uma passiva real.

Afinidade é recalculada a partir do valor autoral usando TerrainAffinity (T029).
A validação exige multiplicadores e resultados finitos e positivos, inclusive
para evitar overflow/underflow. Ler níveis ou alternar terreno não modifica o asset.

23 testes EditMode novos cobrem cinco níveis, índices fora do intervalo, cópia
por valor, limites numéricos em todos os níveis, arma de área sem projéteis,
modo/ID inválidos, referências locais e em catálogo, afinidade e ausência de
mutação. As fixtures são criadas em memória e editadas por SerializedObject.

| Etapa | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| T036 vermelha: validação ainda mínima | 5 | 18 | 2 |
| EditMode final: regressão e T036 | 98 | 43 esperadas de T033/T034 | 2 |

Todos os 23 casos novos passaram na execução final, sem erro de compilação.
As falhas restantes são exatamente as 23 da ausência de BuildState (T039) e 20
da ausência de UpgradeSystem (T040); não houve nova falha de regressão.
As evidências são TestResults/t036-red.xml, TestResults/t036-editmode.xml e
respectivos logs em Logs/, regeneráveis e fora do Git.

Execução batchmode/nographics em Temp/T033Validation, testPlatform EditMode.
O Unity gerou os .meta da pasta, do script e do teste; os GUIDs foram preservados
na cópia ao projeto. A verificação do índice apontou somente três espaços finais
nos campos vazios do Weapons.meta gerado pelo Unity, preservados como serializados.
Scripts e documentação não apresentaram ocorrências de whitespace.

Não foram criados os catálogos ou comportamentos de armas. Combinações evolutivas
completas, passivas tipadas e conteúdo autoral continuam nas T037/T038/T043.
PlayMode não foi repetido: esta alteração só introduz dados e sua validação, sem
alterar a composição da cena ou controladores existentes. A última execução T035
permanece com 34 aprovados e seis falhas esperadas. Não há aceite de gameplay,
builds, plataformas ou desempenho. T037 em diante continuam pendentes.
