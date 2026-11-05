# T017 — Passivas conectadas ao gameplay

Data: 05/11/2026. Base: `2c235ef`. FR-008; depende da resolução pura T016.

## Implementação

UpgradeSystem extrai os níveis da BuildState e usa GetModifier da definição
autoral, somando por atributo num buffer de seis floats reutilizado. O histórico
da build invalida esse buffer após aquisição, nível ou evolução; mudanças de
terreno e relíquias são lidas na resolução seguinte, sem cache desses resultados.
IDs de passivas ausentes do catálogo são rejeitados, sem ignorar bônus silenciosamente.

WeaponRunner usa Damage/AttackInterval/Range/AreaRadius efetivos em cada novo
ataque. PlayerController usa velocidade, capacidade e recuperação efetivas em
cada passo de simulação, mantendo a lentidão inimiga como fator transitório
separado. A resolução sempre parte dos dados base, nunca do resultado anterior.
Sem nova alocação de arrays por frame. Isso não constitui medição de GC/FPS.

LakeBuildProgression conecta o mesmo UpgradeSystem ao jogador e às armas nas
cenas existentes; remove a referência do jogador e desabilita o runner antes
de descartar o serviço. Sem cena/prefab recriado ou referência serializada nova.
Os consumidores sem serviço de progressão mantêm o comportamento base/relíquias.

BreathSystem aceita capacidade efetiva e recuperação por taxa. Aumentar o máximo
preserva a reserva absoluta, sem conceder Fôlego gratuito; reduzir o máximo
limita a reserva ao novo teto. Recuperação mantém limite e tempo de simulação.
ResolvePlayer no Core projeta somente atributos do jogador, reutilizando a
composição e validação puras. Não há números de balanceamento novos.

Ataques/projéteis já emitidos preservam seus atributos de emissão e o intervalo
já agendado; a próxima emissão lê o estado atual e agenda o intervalo atualizado.
Sabão já é resolvido pelo serviço, mas o consumo do HitRadius por colisão/visual
permanece na T018; aura, bumerangue e prévia continuam nas tasks específicas.

## Evidência automatizada

Primeira rodada na cópia ainda com código T016: os três casos iniciais de
PassiveIntegrationTests falharam com “gameplay consumer must bind the same
UpgradeSystem”, zero aprovados/ignorados, exit 2. Depois da integração, quatro
casos focados passaram (incluindo o caso adicional de terreno/dano real), exit 0.

Casos portáveis de PassiveIntegrationTests usam assets sintéticos válidos,
ofertas confirmadas no UpgradeSystem, física real do PlayerController e danos
emitidos pelo WeaponRunner:

- Dano e velocidade de ataque após aquisição e upgrade, sem reaplicação cumulativa.
- Capacidade/recuperação na reserva viva, pausa congelada e ausência de recarga gratuita.
- Movimento após alteração da build, equipar/desequipar relíquia e remover progressão.
- Transição água/terra com recuperação e dano real, filtro de relíquia por terreno.

UpgradeSelectionTests acrescenta um caso na cena Bootstrap/Lake: ambos os
consumidores compartilham o serviço e confirmar Pulmão poderoso na carta aplica
1,08× de capacidade no próximo passo físico. Seis casos EditMode adicionais
verificam a atualização de capacidade/taxa, limites e entradas inválidas.

Suítes completas: **579/579 EditMode e 404/404 PlayMode aprovados**, zero falhas ou ignorados, exit 0 nas duas execuções. Conferida descoberta dos quatro casos portáveis e do caso novo na cena real. Os 82 casos de EffectiveAttributesTests continuam aprovados.

## Ambiente e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`. Comparação dos arquivos
de Assets/QuackQuack: somente LakeURP.asset difere pela prefiltragem regenerável
já conhecida, não copiada de volta. .meta do teste gerado pelo Unity na cópia.
XML/logs locais em TestResults/Polimento002/T017 e Logs/Polimento002/T017 não versionados.

Esta task valida integração automatizada. Sem novo aceite físico/jogável,
desempenho ou aprovação dos players IL2CPP. T018 e posteriores não implementadas;
T014 continua aberta pelo 8BitDo/S23 adiado e T075 anterior permanece aberta.
