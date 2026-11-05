# T020 — Regressões do bumerangue por passagem e lease

Data: 05/11/2026. Base: `f3e0516`. FR-009, contrato C3.

## Entrega de testes

Substituído BoomerangTravelsOutAndBackWithoutHittingSameTargetTwice em
WeaponRunnerTests. O contrato antigo proibia o segundo acerto total; o contrato
atual permite um acerto por passagem Outbound/Inbound de cada emissão, para
cada lease de alvo. Nenhum script produtivo foi alterado; correção é T021.

Onze casos verificam:

1. Passos de 0,125 s: um dano na ida e um na volta, mesmo sobrepondo o alvo
   por vários passos. Impede corrigir limpando histórico a cada frame.
2. Um passo de 0,75 s atravessa o retorno e preserva os dois acertos.
3. Um passo de 2 s atravessa toda a duração de 1 s, preserva ida/volta e expira
   o projétil uma única vez.
4. Alvo no ponto de retorno recebe um acerto em cada lado da mudança de passagem,
   inclusive sobreposição inicial na volta, sem duplicatas posteriores.
5. Alvo entra somente durante a volta e ainda recebe seu primeiro acerto.
6. Colliders de raiz/filho deduplicados por alvo em cada passagem.
7. O mesmo projétil pooled é reutilizado; não herda histórico da emissão anterior.
8. Reuso do mesmo inimigo durante a ida com lease novo permite novo dano.
9. O mesmo reuso durante a volta permite novo dano.
10. Pausa no retorno congela idade/posição/dano e preserva o acerto da volta.
11. Parede inserida após a ida bloqueia o retorno antes do alvo.

Os casos de lease usam PoolService e PooledEnemy reais, capacidade 1, confirmando
mesmo componente/GameObject e LeaseId maior após Despawn/Spawn. QuackTarget é
ligado a State.Health/TakeDamage do ator capturando o lease da ativação. Não há
contador falso de vidas nem alteração de lease por reflexão. O scheduler tático
é dispensado nesta fixture para isolar passagem/deduplicação. Dados de inimigo
são ScriptableObjects sintéticos válidos, criados/destruídos apenas no teste.

Os demais testes de WeaponRunnerTests foram preservados; o arquivo .meta existente
não mudou. Nenhum Ignore, Explicit, stub produtivo ou asserção enfraquecida para
obter aprovação. O arquivo permanece compilável nos players (sem UnityEditor).

## Evidência

Rodada focada inicial, antes de acrescentar o caso de passo maior que a duração:
27 casos, 20 aprovados, 7 falhas, zero ignorados, exit 2. As falhas demonstram:

- Histórico único por projétil impede o acerto da volta: saúde 17 em vez de 14
  para ida/volta em passos pequenos, passo cruzando retorno, alvo no retorno,
  múltiplos colliders e pausa.
- InstanceID sozinho retém imunidade de uma vida anterior: saúde 20 em vez de
  17 após reutilizar o inimigo com novo lease, tanto na ida quanto na volta.

Já passaram os casos de entrada só na volta, reset do projétil e parede no
retorno, além dos 17 casos preexistentes preservados. Essas verificações devem
continuar passando quando T021 corrigir os novos casos vermelhos.

Suíte PlayMode completa final: **446 descobertos, 438 aprovados, 8 falhas esperadas, zero ignorados, exit 2**. WeaponRunnerTests tem 28 casos (17 preservados + 11 novos). As oito falhas pertencem exclusivamente aos casos novos: seis de retorno/passagem, incluindo passo além da duração, e dois de reuso de lease. Conferidos nomes e classe de todas as falhas; nenhuma regressão adicional ou falha de compilação. `git diff --check` aprovado. Comparação de Assets/QuackQuack com a cópia mostrou apenas a diferença regenerável conhecida de LakeURP.asset, não copiada de volta. Sem extensions.yml ou hooks de implementação.

## Ambiente e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`. Logs/XML locais em
Logs/Polimento002/T020 e TestResults/Polimento002/T020 não entram no commit.
Esta task entrega testes deliberadamente vermelhos; não aprova gameplay,
hardware, desempenho ou a regra ainda ausente. EditMode não é repetido para
uma alteração apenas de testes PlayMode; 579/579 da T019 são evidência histórica.
Players IL2CPP não executados nesta task.

T021 não implementada. T014/8BitDo no S23 e T075 anteriores continuam abertas.
