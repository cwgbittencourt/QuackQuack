# T019 — Aura sem oclusão própria e alcance visível

Data: 05/11/2026. Base: `d30c6e2`. FR-007; contrato C3.

## Implementação

WeaponRunner ignora Linecast somente no dano de EnergyAura. Demais áreas,
cones e projéteis conservam as políticas existentes de bloqueio. OverlapCircle
usa o raio efetivo (máximo entre AreaRadius efetivo e HitRadius autoral), com
a deduplicação existente por QuackTarget para múltiplos colliders. Não altera
colliders sólidos, camadas de colisão ou movimento do jogador.

WeaponVisuals, no assembly Presentation, recebe apenas posição, raio, sprite e
tempo de simulação. Prealoca uma view por slot do runner (seis) na inicialização;
não cria objetos/materiais a cada pulso e não possui Update próprio. Reutiliza o
sprite circular existente da arma e não acrescenta collider. É apresentação
temporária, sem antecipar a produção artística da feature.

O disco translúcido tem diâmetro mundial 2 × raio real, compensando bounds do
sprite e escala da hierarquia. Anima somente a opacidade, entre 0,10 e 0,18,
mantendo a fronteira estável. Fica acima do terreno e abaixo dos personagens
na composição atual. O mesmo raio é usado na consulta de dano e no visual.

O runner resolve atributos também entre pulsos para atualizar o alcance quando
nível/relíquias mudam; a frequência de dano permanece governada pelo cooldown.
Pausa congela animação/dano, mantendo o alcance visível. Substituição da arma,
runner/view desabilitado ou fim da partida ocultam o efeito. A mudança de estado
terminal oculta imediatamente via RunState.Changed, mesmo quando o diretor
deixa de chamar Advance; a assinatura é removida em OnDestroy.

Sem mudanças em cenas/prefabs ou dados de balanceamento. O teste legado que
exigia parede bloquear todos os seis comportamentos foi renomeado para os
outros padrões, removendo apenas o caso contraditório de EnergyAura. Sua nova
regra está explicitamente coberta por AuraOcclusionTests; bumerangue não mudou.

## Testes

Baseline com os 12 casos iniciais: 9 aprovados, 3 falhas, zero ignorados, exit 2.
Falhas confirmadas: aura bloqueada por parede e ausência das duas verificações
de apresentação. Após a correção inicial, 30/30 casos focados passaram (12 da
aura + 18 de WeaponRunnerTests), exit 0.

Versão final acrescenta dois casos de ciclo/escala e exige ocultação terminal
sem chamar Advance. Os 14 casos portáveis de AuraOcclusionTests cobrem:

- Aura atravessa parede para dano; cinco outros comportamentos continuam bloqueados.
- Parede mantém collider sólido e resposta a raycast após o pulso.
- Alvos dentro, com centro na fronteira e fora do raio; consulta continua por
  sobreposição de colliders, conforme o sistema existente.
- Vários colliders do mesmo alvo recebem um dano por pulso; pausa não causa dano.
- Diâmetro real, ausência de collider no efeito, seguimento da posição do dono,
  animação apenas da opacidade, congelamento e ocultação imediata no fim.
- Nível/relíquia atualizam diâmetro entre pulsos e dano no próximo pulso.
- Substituir aura por outra arma oculta a view sem aumentar o número de objetos.
- Compensação de escala 2×3 e desabilitação explícita da view.

Suítes completas: **579/579 EditMode e 436/436 PlayMode aprovados**, zero falhas ou ignorados, exit 0 em ambas. Conferida descoberta dos 14 casos finais de AuraOcclusionTests, além dos 18 casos de WeaponRunnerTests. `git diff --check` aprovado. Sem extensions.yml ou hooks de implementação.

## Ambiente e limites

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`. Comparação de
Assets/QuackQuack: somente LakeURP.asset difere por prefiltragem regenerável já
conhecida e não foi copiado de volta. .meta da pasta/scripts novos gerados pelo
Unity e preservados na raiz. XML/logs em TestResults/Polimento002/T019 e
Logs/Polimento002/T019 são artefatos locais não versionados.

Testes verificam geometria, estado e integração, sem declarar aceite artístico,
playtest, hardware ou desempenho. A consulta mantém os buffers existentes do
runner; esta rodada não demonstra capacidade de dano em hordas saturadas nem
metas de FPS/GC. Não foram executados players IL2CPP nesta task.
T020 em diante não implementadas; T014/8BitDo-S23 e T075 anteriores seguem abertas.
