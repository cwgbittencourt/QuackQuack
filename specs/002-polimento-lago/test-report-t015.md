# T015 — Contrato de atributos efetivos

Data: 05/11/2026. Base: `4fb5527`. Escopo: testes EditMode de FR-008,
sem implementar T016–T018 ou alterar gameplay/conteúdo.

O usuário solicitou T015 após adiar o teste 8BitDo/S23. Esse aceite físico
continua pendente em T014; não é dependência técnica do cálculo puro de atributos.
O avanço explícito para T015 não encerra T014 nem a T075 da feature 001.

## Cobertura

`EffectiveAttributesTests` acrescenta 55 casos:

- 36 combinações: seis passivas autoradas nos níveis 0–5; verifica atributo
  afetado e preservação dos demais. O catálogo tem duas passivas de dano,
  uma de ataque, capacidade, recuperação e raio; não inventa uma sétima passiva.
- Bolhas nos níveis 1/5: HitRadius ×1,08/×1,40 mesmo com AreaRadius zero,
  preservando dano, aquisição e velocidade do projétil.
- Sabão não afeta cinco outras armas nem três evoluções.
- Cem resoluções repetidas, troca de terreno e remoção de bônus, sem acúmulo.
  Dados de velocidade/recuperação do Pato são lidos dos assets em terra/água.
- Relíquias: soma interna preservada, filtro de terreno, composição após
  passivas, área aplicada uma vez, ataque, movimento, recuperação e alcance;
  desequipar não deixa bônus antigo.
- Três evoluções usando BuildState real: substituir base pelo resultado no
  nível 5, mantendo a passiva adquirida.
- Clone de Sabão com incremento 12% e teto 25%: a resolução deve consumir o
  valor autorado, sem fixar 8%/40% no código. Clone destruído no finally.
- TearDown compara serialização dos assets consumidos antes/depois.

## Fronteira proposta para T016

Core não referencia Runtime/Content. Para manter essa direção e compilar a
etapa vermelha sem stub produtivo, o teste usa reflexão apenas no adaptador
`Resolve`. Não há Ignore, teste explicit ou fallback que simule a regra.

Contrato proposto: tipo `QuackQuack.Core.Combat.EffectiveAttributes`, método
público estático `Resolve`, recebendo nesta ordem:

1. Nove floats base: Damage, AttackInterval, MovementSpeed, BreathCapacity,
   BreathRecovery, Range, AreaRadius, HitRadius, ProjectileSpeed.
2. Bool de elegibilidade de Bolhas.
3. Seis bônus autorados (float[]) na ordem de PassiveAttribute: Damage,
   AttackSpeed, MovementSpeed, BreathCapacity, BreathRecovery, AreaRadius.
4. RelicLoadout e TerrainType atuais.

Resultado expõe propriedades float com os nove nomes acima. O adaptador
converte definições/níveis em bônus usando GetModifier e resolve afinidade
autoral antes da chamada. Não implementa o cálculo esperado. Valores
esperados são independentes e verificam os números aprovados.

Essa assinatura é uma fronteira de teste proposta, não uma API de produto
já existente. T016 pode substituí-la por DTOs puros tipados, ajustando somente
o adaptador e preservando cenários/assertivas. Integração de BuildState e
invalidação de caches no jogo pertencem à T017; colisão/visual à T018/T022.

## Execução e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`, EditMode Win64.
Comparação dos arquivos de Assets/QuackQuack com a raiz: apenas LakeURP.asset
difere por prefiltragem regenerável já conhecida; não foi copiado de volta.
O .meta do teste foi gerado pelo Unity na cópia e preservado na raiz.

Primeira rodada: 546 descobertos, 491 aprovados, 55 falhas, zero ignorados,
exit 2. Todas as falhas têm a mensagem “T016 pending: pure EffectiveAttributes
resolver is missing.” Não houve falha de compilação nem regressão nos 491
casos existentes. Rodada final, após incluir dados reais de terreno do Pato: mesmos 546 casos, 491 aprovados, 55 falhas esperadas, zero ignorados, exit 2. Conferida descoberta dos 55 casos da classe e ausência de qualquer falha fora dela.

Artefatos locais regeneráveis: `TestResults/Polimento002/T015/EditMode.xml`,
`EditMode-final.xml`; logs correspondentes em `Logs/Polimento002/T015`.
Não entram no commit. Testes deliberadamente vermelhos documentam a entrega
T015; não aprovam a regra futura, gameplay, plataformas ou desempenho.
