# T021 — Passagens e histórico por lease do bumerangue

Data: 05/11/2026. Base: `3f37865`. FR-009, contrato C3.

## Correção

WeaponProjectile registra a passagem Outbound/Inbound e identifica cada emissão
com LeaseId local crescente, zerado na devolução ao pool. Begin inicia uma nova
emissão, limpa logicamente o histórico e volta para Outbound. BeginInbound faz
a transição uma única vez, limpando o contador de acertos da passagem anterior.
Não há limpeza por frame. ResetState também restaura passagem/lease/idade.

O histórico prealocado de 512 entradas passa a guardar pares InstanceID/lease do
alvo. WeaponRunner resolve o lease atual de PooledEnemy para bumerangues; alvos
sem esse ator usam InstanceID com lease zero. Assim, comuns/minibosses reutilizados
não herdam imunidade da vida anterior, sem introduzir dependência em Content/Core.
O histórico pertence à emissão corrente do projétil e à passagem corrente.

O passo que cruza metade da duração continua dividido na fronteira: processa
ida, muda a passagem e processa a volta. Direção e deslocamento de retorno usam
a passagem explícita. Passos que ultrapassam a duração respeitam o restante da
vida. Um avanço de tempo zero não muda a passagem nem causa dano do bumerangue.
Pausa, obstáculos, estado terminal e snapshot de atributos foram preservados.

Leases dos contatos são capturados num buffer fixo antes de aplicar dano.
Se um callback derrota/recicla o ator, contatos consultados para o lease antigo
não podem atingir sua nova vida. Uma consulta nova pode atingir essa nova vida
na mesma passagem, conforme o contrato. Não há array/objeto novo a cada hit.
Isso não é uma medição de GC ou desempenho da partida.

Outros comportamentos mantêm sua deduplicação anterior; a mudança de passagem e
a consulta de lease são específicas de Returning. Nenhum asset/cena/prefab ou
arquivo .meta existente foi modificado. Sem mudanças em balanceamento ou arte.

## Evidência e regressões

Baseline T020: PlayMode com 438 aprovados e oito falhas esperadas; relatório
`test-report-t020.md`. Após a primeira correção, os 28 testes de WeaponRunnerTests
passaram, incluindo todas as oito falhas anteriores, exit 0.

Acrescentados três casos:

- Tempo zero na fronteira não inicia a volta; passos seguintes não limpam o
  histórico da volta novamente.
- O mesmo projétil reutilizado após completar Inbound recebe lease novo e
  começa Outbound, sem idade/histórico da emissão anterior.
- Callback de derrota reutiliza imediatamente o mesmo inimigo, com múltiplos
  colliders no cast antigo: preservar saúde da nova vida até uma consulta nova.

Na primeira rodada com esses três casos, 30/31 passaram; o caso de tempo zero
afirmava dano após somente 0,125 s de retorno (posição x=3, alvo em x=2).
Corrigida a fixture para avançar 0,25 s antes da asserção (x=2), preservando os
checks de tempo zero, fase e não duplicação. A proteção reentrante já passou
nessa rodada. Não foi enfraquecida a quantidade de dano esperada.

Suítes completas finais: **579/579 EditMode e 449/449 PlayMode aprovados**, zero falhas ou ignorados, exit 0 em ambas. Conferidos os 31 casos de WeaponRunnerTests (28 da T020 + três novos) e a aprovação das oito falhas anteriores. `git diff --check` aprovado. Comparação de Assets/QuackQuack mostrou somente a prefiltragem regenerável conhecida de LakeURP.asset na cópia, não copiada de volta. Sem extensions.yml ou hooks de implementação.

## Ambiente e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`. Logs/XML locais em
Logs/Polimento002/T021 e TestResults/Polimento002/T021 não versionados. Testes
novos permanecem portáveis, sem UnityEditor; players IL2CPP não executados nesta
task. Aceite jogável de armas/passivas é T023; nenhuma aprovação de hardware,
arte ou desempenho é inferida dos testes.

T022 e posteriores não implementadas; T014/8BitDo-S23 e T075 anteriores continuam abertas.
