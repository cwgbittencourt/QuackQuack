# T016 — Resolução pura de atributos efetivos

Data: 05/11/2026. Base: `7d1e138`. FR-008, contrato C3.

## Entrega

`Core/Combat/EffectiveAttributes.cs` implementa uma projeção imutável (readonly
struct) usando a assinatura numérica proposta na T015. Não depende de Content,
UnityEditor ou apresentação e não guarda referências aos argumentos.

O chamador fornece bases com afinidade de terreno já aplicada e seis bônus
obtidos de `PassiveDefinition.GetModifier(level)`, que continua sendo a fonte
dos níveis/tetos autorados. A resolução não duplica 8%/40% nem aumenta o teto
individual; aceita a soma de passivas do mesmo atributo (duas de dano no catálogo).
Nenhum asset foi alterado. Extração do estado da build e integração são T017.

Composição: base × (1 + bônus passivo) × (1 + soma das relíquias elegíveis).
Intervalo de ataque divide pelos dois fatores. Usa GetBonus existente para
preservar soma interna, filtro de terreno e limite de equipadas das relíquias.
Cada chamada começa nas bases, sem cache nem aplicação cumulativa.

Sabão só altera HitRadius quando a entrada indica Bolhas. Não altera dano,
alcance de aquisição, velocidade ou AreaRadius zero de Bolhas. Relíquia de área
compõe uma vez com o raio de Bolhas; nas demais armas mantém a política existente
de AreaRadius, sem aumentar HitRadius por Sabão. Preview/colisão/visual ainda
dependem de T018/T022.

Entradas negativas/não finitas, bases obrigatórias zero, terreno desconhecido
e vetor de bônus ausente ou com tamanho incorreto são rejeitados. Operações usam
intermediário double e rejeitam overflow/underflow que produza valor inválido
em float. Área e recuperação autorais zero continuam válidas, coerentes com
WeaponDefinition e QuackDefinition; as demais bases devem permanecer positivas.
Não há alocação explícita no caminho válido da resolução; isso não é aceite de
GC/desempenho da partida.

## Testes

Os 55 cenários da T015 foram preservados; o adaptador agora chama diretamente
o método produtivo, removendo a reflexão necessária quando a classe não existia.
As falhas vermelhas anteriores estão no relatório T015.

Mais 27 casos cobrem bases/bônus inválidos, zeros permitidos/proibidos, formato,
terreno, overflow e underflow, identidade com bônus zero, combinação de passivas,
100 recomputações, preservação dos argumentos e Version de relíquias, estabilidade
do resultado antigo após alterar entradas, e política de área para outras armas.

Unity 6000.4.3f1, EditMode Win64, cópia isolada da feature 002:

| Verificação | Resultado |
|-------------|-----------|
| Suíte completa EditMode | 573/573 aprovados |
| EffectiveAttributesTests | 82/82 aprovados (55 anteriores + 27 novos) |
| Falhas / ignorados | 0 / 0 |
| Exit code do Unity | 0 |

Arquivos de Assets/QuackQuack comparados com a cópia: somente LakeURP.asset
difere por prefiltragem regenerável já conhecida e não foi copiado de volta.
O Unity gerou o .meta do novo arquivo na cópia, preservado na raiz.
`git diff --check` aprovado. Sem extensions.yml ou hooks de implementação.

XML local: `TestResults/Polimento002/T016/EditMode.xml`; log em
`Logs/Polimento002/T016/EditMode.log`. Artefatos regeneráveis não versionados.

## Limites

Sem mudanças em WeaponRunner, PlayerController, UpgradeSystem, cenas ou prefabs.
Não foi necessário executar PlayMode ou players para uma regra ainda não ligada
ao gameplay. O resultado aprova a regra pura, sem declarar aceite jogável,
físico, artístico ou desempenho. T017 e posteriores continuam pendentes.
T014 segue aberta pelo teste 8BitDo/S23 adiado pelo usuário; T075 não muda.
