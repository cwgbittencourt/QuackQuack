# T024 — Contratos de cura e recompensas

Data: 05/11/2026. Base: `6cb2705`. Escopo: etapa vermelha de testes EditMode de FR-010–012. Não implementa T025–T032, não altera gameplay, conteúdo ou balanceamento.

## Cobertura

`RewardPolicyTests`: 22 casos. Cura comum/miniboss proporcional à vida máxima, limitada à falta, incluindo máximos 100/200; vida cheia e morte preservadas; frações zero, negativas, acima de um, NaN e infinitas sem aplicação. Sorteio comum testa 0, imediatamente abaixo de 0,10, igualdade, imediatamente acima e próximo de um. XP acompanha a derrota com ou sem cura. Dez mortes podem produzir dez curas ou nenhuma, sem quota/cooldown. Miniboss garante fração 0,50 sem sorteio comum; chefe tem XP e não herda cura.

`RewardLedgerTests`: 17 casos. Replay de derrota comum/miniboss/chefe não duplica nem sorteia novamente. Replay de sorteio sem cura não oferece segunda chance nem sobrescreve XP/posição. Identidade distingue partida, origem, lease e espécie de recompensa. Despawn não recompensa nem consome a futura identidade de derrota; XP não positivo rejeita o registro inteiro antes de consumir RNG/identidade. Saturação visual mantém ID, posição, XP/fração e obrigação por vinte tentativas; materializa uma vez quando há slot. Quarenta derrotas comuns pendentes não reduzem chance e não impedem cura garantida de miniboss. Cura não é coletada remotamente ou com vida cheia/morta; claim somente após aplicação positiva e uma vez. XP usa `ExperienceState` real e não duplica nível/quantidade. Fontes de RNG injetadas permitem reproduzir a sequência de drops independentemente de um RNG separado de ofertas.

Os números 9/25 de XP usados nas fixtures são dados de teste, não autoria de recompensas do jogo ou do chefe. Os máximos diferentes testam a proporção sobre `MaxHealth` da instância; não adicionam mutação de MaxHealth, atualmente somente leitura.

## Fronteira proposta para T025/T027

Os tipos/métodos novos ainda não existem. Um adaptador de reflexão restrito aos testes mantém a compilação sem stubs produtivos, Ignore ou implementação simulada. As assertivas verificam os resultados reais quando a API for implementada. A assinatura pode ser substituída nas próximas tasks, ajustando o adaptador e preservando os cenários:

- `PlayerState.Heal(float fraction) -> float applied`, inválido/sem aplicação retorna zero, sem reanimar ou alterar MaxHealth.
- `Core.Progression.RewardLedger(long runId, Func<double> nextDropRoll, double commonChance, float commonFraction, float miniBossFraction)`; fixtures usam 0,10 / 0,15 / 0,50.
- `ProcessDefeat(int sourceId, long lease, RewardOrigin origin, int experience, float x, float y, bool isDefeat) -> bool`, origens `Common/MiniBoss/FinalBoss`.
- `Entries`: registros observáveis com `Id`, `Kind`, `Experience`, `Fraction`, `X`, `Y`, `State`; tipos `Experience/HealCommon/HealMiniBoss`. ID tem igualdade por valor incluindo partida/origem/lease/tipo; a API concreta pode usar DTOs puros em lugar desses parâmetros.
- `TryMaterialize(id, bool hasViewSlot) -> bool`, indisponibilidade muda para `WaitingForView`, disponibilidade para `Grounded`; não sorteia, descarta, concede à distância ou materializa duas vezes. O booleano é a fronteira pura de disponibilidade, não um pool fake implementando recompensas.
- `TryCollectHeal(id, PlayerState) -> float applied`, `TryCollectExperience(id, ExperienceState) -> bool`; claim positivo muda para `Claimed`. Integração das views/colisão pertence a T027–T031.

A fronteira de chance é testada com `double` em 0,10 exato na precisão usada pelo sorteio, sem tolerância que transforme igualdade em sucesso. Ao adaptar os dados float de T026, preservar o comportamento aprovado de igualdade rejeitada.

## Execução

Unity 6000.4.3f1, EditMode Win64 em `Temp/Polimento002Validation`; `.meta` dos dois testes gerados pelo Unity e copiados preservando GUIDs. Saídas locais ignoradas pelo Git em `Logs/Polimento002/T024` e `TestResults/Polimento002/T024`.

Primeira rodada: 622 casos, 584 aprovados, 38 falhas esperadas, zero ignorados, código 2. Treze falhas identificam `PlayerState.Heal(float)` ausente; 25 identificam `RewardLedger` ausente. Nenhum erro de compilação ou falha nos testes anteriores.

Rodada final: 623 casos, 584 aprovados, 39 falhas esperadas, zero ignorados, código 2. Treze falhas apontam Heal ausente e 26 o ledger ausente; todas param na verificação de API, sem erro de compilação ou regressão anterior. As assertivas posteriores só serão exercitadas quando T025/T027 implementarem a fronteira. XML: `TestResults/Polimento002/T024/final-EditMode.xml`; log: `Logs/Polimento002/T024/final-EditMode.log`.

## Limites e continuidade

T024 entrega testes vermelhos; não declara cura, emissão, saturação ou coleta prontas. T025 implementa Heal/emissão; T027 resolve o ledger/pool/pendências, sem perda; T026 fornece política/XP positivo editável. Capacidade real da partida inteira, prioridade de materialização do miniboss, desempenho, conexão com ofertas reais, atração, teardown e fluxo final do chefe ainda exigem implementação e validação nas tasks correspondentes. O teste com RNG separado comprova isolamento da fronteira pura, não a integração futura do RunDirector.

Não foram necessárias builds ou testes físicos nesta entrega exclusivamente de testes de regras. T014 continua pendente e T025 não foi iniciada.
