# T025 — Cura e regras puras de emissão

Data: 05/11/2026. Base: `aa38b98`. Escopo: `PlayerState.Heal` e núcleo puro de `RewardLedger`, sem conectar pickups/pools/RunDirector ou iniciar T026.

## Implementação

`Heal(fraction)` rejeita frações não finitas, fora de (0,1] e jogador morto. Aplica `min(falta, MaxHealth × fraction)` e retorna o ganho efetivamente representável. Não altera MaxHealth, Fôlego ou invulnerabilidade. Ganho arredondado a zero não consome recompensa.

O ledger recebe identidade da partida, função dedicada de RNG, chance/frações e capacidade explícita de derrotas. Registra XP positivo para Common/MiniBoss/FinalBoss; comum faz exatamente um sorteio em [0,1), aceita somente valor estritamente menor que a chance; miniboss garante uma cura e não sorteia cura comum; chefe não herda cura. Nenhum valor de balanceamento ou XP de chefe é inventado no Core. Invalidade de origem/identidade/XP/posição ou despawn não consome a identidade/RNG. RNG inválido lança erro antes de registrar parcialmente a derrota.

`RewardId` é um valor com partida, origem, lease, tipo e ordinal (zero para as emissões atuais de uma recompensa por tipo). O registro de XP também deduplica a derrota inteira: replay não altera posição/valor, não sorteia novamente e não cria recompensa, mesmo se o primeiro sorteio falhou ou a recompensa já foi coletada. Uma nova lease ou outra partida permanece distinta.

`RewardEntry` guarda origem, ID, posição, XP/fração e estado; `Entries` expõe coleção somente leitura. Registros, lista e índice são pré-alocados para até duas recompensas por derrota. A capacidade é obrigatória no construtor, não um teto de chance. Exaustão lança erro antes do sorteio/mutação, sem supressão silenciosa. A integração deverá dimensionar a partida inteira em T027; este comportamento não aprova capacidade de produção.

As transições puras Due/WaitingForView/Grounded/Claimed completam a fronteira de testes de T024. `TryMaterialize` somente reconhece disponibilidade informada pelo host: não cria, aluga, agenda ou vincula views. `TryCollectHeal` exige recompensa de cura materializada e só marca claim após ganho positivo. Vida cheia/morta mantém o registro coletável; repetição não aplica novamente. `TryCollectExperience` usa `ExperienceState.TryAdd`, preserva a obrigação se houver overflow e não duplica XP/níveis. Proximidade, pausa, atração, liberação e descarte final ainda não são governados pelo ledger nesta task.

## API de teste e cobertura

A fronteira provisória T024 foi adaptada apenas onde necessário: construtor fortemente tipado agora inclui `int defeatCapacity` obrigatório (512 nas fixtures, sem significar capacidade autoral); Heal é chamado diretamente. Cenários e assertivas anteriores foram preservados, sem Ignore ou fallback. Reflexão residual nos acessores do adaptador lê os objetos reais do Core; não simula regra.

Os 39 casos vermelhos T024 passaram. Treze casos adicionais verificam chance inválida, RNG inválido sem commit parcial, chance 0/1 e frações editáveis diferentes de 15%/50%, exaustão técnica antes do RNG, rejeição de espécie/ID estrangeiro e overflow de XP, invulnerabilidade preservada e ganho de cura arredondado a zero. Total dos testes de recompensa: 52.

## Validação

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`, alvo Win64:

| Suíte | Resultado |
|---|---|
| EditMode | 636/636 aprovados, zero falhas/ignorados, código 0 |
| PlayMode | 462/462 aprovados, zero falhas/ignorados, código 0 |

Resultados locais em `TestResults/Polimento002/T025/EditMode.xml` e `PlayMode.xml`; logs em `Logs/Polimento002/T025`. Não versionados. Novo `.meta` gerado pelo Unity e preservado na raiz. Assets serializados, cenas, políticas e builds não foram alterados.

## Continuidade e limites

- T026: dados autorais/política e XP positivo, incluindo chefe. Preservar a fronteira 0,10 rejeitada ao adaptar o campo de chance para a precisão do RNG, sem tolerância/epsilon que aumente chance.
- T027: integrar obrigações/pendências com pools e RunDirector, remover SuppressedPickups, priorizar miniboss, dimensionar e demonstrar capacidade para a partida inteira. O reconhecimento lógico de disponibilidade implementado aqui não substitui essa integração.
- T028–T030: views, atração, pausa/fim, lifecycle/reuso, liquidação final de XP do chefe e teardown. Nenhuma recompensa nova está ligada à partida neste incremento.
- T031/T032: integração e aceite jogável. Não houve build ou aceite físico/performance nesta task de regras; testes puros não aprovam hardware ou gameplay.

T014 permanece pendente por decisão anterior do usuário. T026 não foi iniciada.
