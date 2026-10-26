# Validação de T043

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T043 concluída. Criado PrototypeBuildCatalog.asset e 18 definições
persistidas, geradas/serializadas pelo Editor: seis armas básicas, seis passivas,
três receitas e três armas resultantes. Não houve edição manual de YAML ou cenas.

| Arma básica | Passiva | Evolução incluída |
|---|---|---|
| Bumerangue | Pena aerodinâmica | Tornado de penas |
| Ovos | Ninho reforçado | Incubadora de combate |
| Quack frontal | Pulmão poderoso | Quack supersônico |
| Aura de energia | Amuleto do lago | — |
| Foguetes | Óculos de aviador | — |
| Bolhas | Sabão mágico | — |

Nomes conforme página 6 do documento funcional, com somente as três evoluções
aprovadas pela especificação ativa. Cada arma tem cinco níveis, parâmetros de
movimento/colisão e sprite temporário compartilhado Circle. As passivas têm cinco
níveis de bônus normalizado, incremento de 8% e teto de 40%, com atributo e arma
compatível. Ovos recebem bônus de dano de 25% em terra e Bolhas de 25% na água.
Esses valores são dados iniciais editáveis, sem aceite de balanceamento.

O catálogo expõe as seis armas básicas separadamente e inclui as três formas
evoluídas em GetAllWeapons. Valida contagens exatas, cobertura dos seis padrões,
IDs/referências únicos e coerência com as definições de armas, passivas e receitas.
CreateUpgradeSystem compõe as ofertas com esses assets; formas evoluídas não são
ofertadas como aquisições comuns e receitas ficam bloqueadas até seus requisitos.

PrototypeBuildCatalogSetup.Build cria/atualiza os assets via AssetDatabase e
SerializedObject. O comando foi executado duas vezes na cópia temporária;
a comparação dos .meta registrados entre execuções encontrou zero alterações.
O catálogo foi validado em ambas. A ferramenta preserva GUIDs, mas reaplica os
valores iniciais de autoria ao ser executada; o clone normal usa os assets salvos.

11 casos EditMode novos verificam os assets persistidos, contagens/cobertura,
IDs/nomes, cinco níveis, referências canônicas, proteção das coleções, rejeição
de contagens e referências inválidas, oferta inicial, as três evoluções aplicadas
uma vez e afinidade de terreno. Dois casos PlayMode carregam os assets reais:
equipam as seis básicas ou as três formas evoluídas e comprovam dano atribuído
a cada definição usando WeaponRunner e PoolService com capacidade fixa, além
da devolução de todos os projéteis ao desativar.

| Suíte final | Aprovados | Falhas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 208 | 0 | 0 | 0 |
| PlayMode | 64 | 0 | 0 | 0 |

Sem erros de compilação ou falhas de regressão. Execução batchmode/nographics
em Temp/T033Validation. Geração/regeneração: executeMethod
QuackQuack.Editor.PrototypeBuildCatalogSetup.Build. Logs: Logs/t043-generate.log
e Logs/t043-regenerate.log. TestResults/t043-editmode.xml e
TestResults/t043-playmode.xml registram os testes; os logs correspondentes e
TestResults/t043-guid-snapshot.json são regeneráveis e ficam fora do Git.

Os assets e .meta gerados foram copiados preservando os GUIDs. Nenhum cache,
log ou resultado regenerável foi versionado. Os espaços de campos vazios em
.meta/strings serializadas são preservados conforme emitidos pelo Unity.

A validação usa componentes reais em fixtures isoladas, sem mudar a composição
de Bootstrap/Lake. O roteiro normal ainda mostra a arena US1; seleção de cartas
e feedback continuam nas T044–T045. Não houve avaliação visual/auditiva, playtest
de balanceamento, build de plataforma ou medição de desempenho. Não representa
aceite de US2 completa. T044 em diante continuam pendentes.
