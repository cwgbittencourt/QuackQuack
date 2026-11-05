# T018 — Raio real e visual das Bolhas

Data: 05/11/2026. Base: `0876396`. FR-008, contrato C3.

## Entrega

WeaponRunner captura HitRadius efetivo calculado por UpgradeSystem na emissão e
o transmite ao WeaponProjectile. Sem progressão, usa o raio autoral; relíquia de
área mantém composição para Bolhas. O terreno é lido uma vez por emissão.
CircleCast consome o raio capturado do projétil em unidades mundiais, preservando
ordenação de contatos, bloqueio por obstáculos e deduplicação já existentes.

WeaponProjectile dimensiona o sprite de Bolhas com diâmetro mundial igual a
2 × HitRadius. Considera bounds do sprite/PPU e escala da hierarquia do pool;
não multiplica um sprite anterior de tamanho arbitrário. Com base 0,2, os
diâmetros são 0,4 / 0,432 / 0,56 nos níveis 0 / 1 / 5 de Sabão. AreaRadius zero
não anula o crescimento. O cálculo numérico de Sabão continua nos dados/T016.

O raio pertence à emissão: subir a passiva não redimensiona o projétil que já
está voando; a emissão seguinte usa o novo valor. Pausa preserva o estado.
Reset pooled limpa raio/escala/sprite junto aos demais campos, evitando herança
do tamanho ampliado. Outros comportamentos conservam seu raio e escala base.
O argumento opcional de Begin preserva o chamador existente do benchmark.

Sem mudanças em dano, velocidade, duração ou alcance de aquisição de Bolhas.
Sem criação recorrente de objetos, novo collider ou nova consulta por frame.
Cenas, prefabs, assets autorais e respectivos .meta existentes preservados.

## Testes e diagnóstico da fixture

BubbleAreaTests contém 19 casos portáveis, sem UnityEditor:

- Diâmetro mundial nos níveis 0/1/5 com sprite de bounds retangulares e PPU
  explícito, mantendo AreaRadius zero.
- Dentro/fora do alcance físico em cada nível; mesmo alvo fora da base e
  dentro da bolha ampliada.
- Dano, velocidade/deslocamento, duração e alcance de aquisição invariantes.
- Sabão não amplia projétil/visual do Bumerangue.
- Pausa, reset e reuso do mesmo objeto com retorno ao raio base.
- Relíquia de área composta uma vez e compensação de pai com escala 2×3.
- Parede impede dano atrás dela mesmo com raio ampliado.
- Upgrade altera a próxima emissão sem modificar a atual ou o asset.

Na primeira fixture, alvos de raio 0,01 próximos à tangência não eram detectados
nem por CircleCast independente usando o raio esperado. A medição está no log
probe; não foi tratada como defeito da regra de atributos. A fixture final usa
raio de alvo 0,1 e margem explícita de 0,03 para os casos dentro/fora. Um caso
adicional compara o mesmo alvo com/sem ampliação. Não exige tangência exata além
da tolerância da física. Ajustado também o passo do teste de nova emissão para
ultrapassar o cooldown, evitando igualdade sensível à representação float.

Após corrigir a fixture, a mesma versão final dos 19 testes foi executada antes
e depois da implementação, restaurando somente os dois scripts produtivos na
cópia isolada (sem alterar a raiz):

| Rodada | Total | Aprovados | Falhas | Ignorados | Exit |
|--------|-------|-----------|--------|-----------|------|
| Baseline final T017 | 19 | 11 | 8 | 0 | 2 |
| T018 focada final | 19 | 19 | 0 | 0 | 0 |

Falhas vermelhas finais: três tamanhos base/níveis, dimensão com relíquia/pai
escalado, retorno ao tamanho base no reuso, duas colisões ampliadas e dimensão
da emissão após upgrade. As evidências preliminares não substituem essa
comparação final reproduzível.

Suítes completas: **579/579 EditMode e 423/423 PlayMode aprovados**, zero falhas ou ignorados, exit 0 em ambas. Conferida a descoberta dos 19 casos de BubbleAreaTests na suíte completa. `git diff --check` aprovado.

## Ambiente e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`. XML/logs locais em
TestResults/Polimento002/T018 e Logs/Polimento002/T018 não versionados. O .meta do
teste novo é gerado pelo Unity na cópia e preservado na raiz. Comparação de Assets/QuackQuack com a cópia encontrou apenas a diferença regenerável conhecida em LakeURP.asset, não copiada de volta. Sem extensions.yml ou hooks de implementação.

Esta entrega aprova geometria/integração automatizada, sem declarar aceite
artístico, playtest, desempenho ou plataformas IL2CPP. Prévia na carta é T022;
aura é T019 e bumerangue permanece nas tasks próprias. T014 e T075 anteriores
continuam abertas; não foi solicitado novo teste físico do 8BitDo/S23.
