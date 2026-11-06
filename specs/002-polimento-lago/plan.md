# Implementation Plan: Polimento e confiabilidade do Lago

**Branch Git**: `main` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)
**Input**: specs/002-polimento-lago/spec.md, derivada de Melhorias.md e imagem de decisões.
**Status**: Planejamento concluído após aprovação de D1–D8 em 03/11/2026. Execução somente mediante task/intervalo solicitado.

## Summary

Corrigir expulsão do Pato, completar controle/menus, aplicar passivas e ajustar aura/bumerangue; depois integrar cura/atração, HUD simplificado e linguagem cartoon no elenco existente. Preservar regras compartilhadas, Safe Area, pooling e desempenho. A causa da expulsão ainda é hipótese até reprodução instrumentada; [research.md](research.md) distingue constatações e propostas.

setup-plan.ps1 -Json executado para esta feature. O resolvedor reporta BRANCH 002-polimento-lago a partir da feature; git branch --show-current confirma main. Diretório e branch real são independentes. Nenhuma branch nova foi criada.

## Technical Context

**Language/Version**: C# / Unity 6000.4.3f1 fixada.
**Primary Dependencies**: URP 2D 17.4.0, Input System 1.19.0, uGUI 2.0.0, Physics2D e pools atuais.
**Storage**: ScriptableObjects autorais; save existente preservado; dicas somente por partida, perfis de controle em arquivo local versionado separado.
**Testing**: Unity Test Framework 1.6.0, EditMode/PlayMode, players IL2CPP e validação física/jogável separada.
**Target Platform**: Windows x64 / Android ARM64, paisagem dos dois lados.
**Project Type**: jogo 2D com regras compartilhadas.
**Performance Goals**: 60 FPS Windows/intermediário com opção 30; entrada Android 30 estáveis; 200/400 inimigos com efeitos/coleta novos.
**Constraints**: quatro proporções, Safe Area, pooling, sem alocações recorrentes evitáveis; assets/cenas via Unity e .meta preservados; elenco fechado.
**Scale/Scope**: MEL-001–016; seis armas/seis passivas/três evoluções, cinco comuns/dois minibosses/chefe e dez relíquias existentes.
**Product decisions**: D1–D8 resolvidas em [decisions.md](decisions.md). Cura comum 10% de chance/15% de vida, miniboss 50% garantido, XP sempre; coleta XP/cura a 3 vezes raio físico e velocidade 1,25 vezes velocidade efetiva na água, persistente após captura. Dicas 10s de simulação; barras sobre cada especial; todas as famílias; Sabão apenas Bolhas (+16%/nível, teto 80% no raio; revisão D7 em 05/11/2026). Cartoon/ordem de assets aprovados.

## Constitution Check

| Gate | Antes da pesquisa | Após design |
|---|---|---|
| I Jogabilidade primeiro | PASS: confiabilidade, clareza, Quack e builds | PASS: C1/C3 e aceite por história |
| II Regras/dados | PASS: parâmetros editáveis e cálculo efetivo único, UI consumidora | PASS: data-model e C3–C6, sem bônus acumulado ou curva duplicada |
| III Adaptadores | PASS: regras comuns, foco/contexto e matriz de telas/entradas | PASS: C2, perfis/fallback e testes físicos previstos |
| IV Desempenho | PASS: pools/atualização central, profiling; sem DOTS/ECS/Jobs/Burst | PASS de design: ledger+views e dimensionamento; runtime depende profiling futuro |
| V Escopo | PASS: catálogo preservado, imagem não autoriza espécies | PASS: manifesto por ID, nenhuma espécie/bioma/monetização adicionados |
| Fluxo de planejamento | Decisões abertas na primeira revisão | PASS: D1–D8 aprovadas, design e tasks rastreados; implementação ainda não iniciada |

Sem exceções constitucionais propostas. T075 anterior continua aberta independentemente desta feature.

## Project Structure

### Documentation (this feature)

```text
specs/002-polimento-lago/
  spec.md
  plan.md
  research.md
  decisions.md
  data-model.md
  contracts/gameplay-contracts.md
  quickstart.md
  tasks.md
  checklists/requirements.md
  references/   # decisões anexadas e duas referências candidatas da conversa
```

Phase 0 e Phase 1 concluídas: [modelo](data-model.md), [contratos](contracts/gameplay-contracts.md), [guia de validação](quickstart.md) e [tasks](tasks.md). Verificação de consistência executada após geração de tasks; resultado resumido no checklist e na entrega, sem alegar testes de gameplay.

### Source Code (repository root)

```text
Assets/QuackQuack/
  Core/Combat, Core/Progression, Core/Run, Core/Shared
  Content/Weapons, Content/Passives, Content/Enemies
  Gameplay/Player, Gameplay/Enemies, Gameplay/Weapons, Gameplay/Pickups
  Gameplay/Bosses, Gameplay/Upgrades
  Platform/Input
  Presentation/UI
  Systems/Run, Systems/Upgrades, Systems/Spawning
  Editor
  Prefabs/Prototype, Scenes
  Tests/EditMode, Tests/PlayMode
```

**Structure Decision**: estender camadas atuais, sem reescrita geral. Core para regras, Content para dados, Gameplay para física e Presentation para visual. Arquivos novos propostos, não ainda existentes:

- Core/Combat/EffectiveAttributes.cs, Core/Progression/RewardLedger.cs, Core/Progression/AttractionState.cs, Core/Shared/ProgressSnapshot.cs.
- Content/Pickups/PickupPolicyDefinition.cs; Content/Encounters/LakeRunSettings.cs integra a política de pickups.
- Gameplay/Player/ArenaConfinement.cs somente se diagnóstico sustentar proteção geométrica; Gameplay/Pickups/HealingPickup.cs; Systems/Pickups/PickupAttractionSystem.cs.
- Platform/Input/ControlProfile.cs, ControlProfileStore.cs; Presentation/UI/ControlBindingsView.cs, SpecialEnemyBarsView.cs, ContextualHintsView.cs e UpgradeAreaPreview.cs.
- Presentation/Weapons/WeaponVisuals.cs e Presentation/Terrain/LakeWaterAnimation.cs; Editor/PolishUiSetup.cs e PolishArtSetup.cs.
- Art/LakeCartoon/ com fontes/exportações rastreáveis; specs/002-polimento-lago/art-manifest.md e relatórios futuros por história.

Os caminhos acima são relativos a Assets/QuackQuack exceto os iniciados por specs/. Criar .meta via Unity; não criar estrutura nova inteira sem necessidade da task.

## Fronteiras do design

- RewardLedger é a verdade de recompensas; views são pooled e podem aguardar slot sem perda de valor/posição/ID. RNG de cura ocorre uma vez por derrota, separado da geração de ofertas. Dimensionamento completo e stress de saturação são obrigatórios; SuppressedPickups não atende o novo contrato.
- Chefe final sem XP nem cura; mantém moedas, vitória e registros uma vez. Despawn não gera drop; fim cancela pickups pendentes sem coleta automática.
- EffectiveAttributes resolve base/passivas/relíquias/terreno sem mutar definições e preserva ordem de relíquias. Player, arma e visual consomem a mesma resolução; só Bolhas recebe Sabão nesta entrega.
- Bumerangue usa fases/leases, aura ignora oclusão somente para seu dano, UI lê snapshots, hotplug preserva contexto/pausa e perfis ficam separados do save de progressão.
- Correção de arena só é selecionada após T003/T004: método físico exato é resultado do diagnóstico técnico, não decisão de produto aberta. T005 registra alternativas/evidência antes de integrar proteção.

## Sequência proposta

Macroetapas detalhadas em tasks.md, sem autorização para implementar. Execução futura uma task por vez ou intervalo solicitado.

| Etapa | Requisitos e integração existente | Dependência | Evidência |
|---|---|---|---|
| 1 Diagnóstico crítico | MEL-016; PlayerController, EnemyUpdateScheduler, PooledEnemy, LakeArenaSetup/LakeRunSetup | Reproduzir antes de escolher correção | Falha demonstrada, causa registrada e regressão borda/canto/obstáculo; pressão preservada |
| 2 Controle/menus | MEL-013–015; UnifiedPlayerInput, ActionPlayerInput, inputactions, RelicLoadoutView, UpgradeSelectionView, LakeRelics | US1 aceita; C2 aprovado | Hotplug/foco/contexto; controle físico Windows/Android e toque restaurado |
| 3 Combate/passivas | MEL-007–009; WeaponRunner, WeaponProjectile, UpgradeSystem, PlayerController | C3 aprovado; estabilizar regras antes do visual | Aura sem oclusão própria; seis passivas verificadas; acerto por trecho/lease |
| 4 Cura/coleta | MEL-010–012; PlayerState, RunDirector.Reward, EnemyReward, ExperiencePickup/pools | Atributos efetivos US3; C4/C5 | Cura limitada/única, garantia miniboss, atração/obstáculo/pausa/reuso |
| 5 HUD/contexto | MEL-003–005 e barras 012; ExperienceState, HudSnapshot, GameplayHud/Setup, IntegratedUiSetup | Snapshots e estados reais das etapas 3/4 | XP/nível, duas barras, dicas temporárias, especiais sem referências obsoletas |
| 6 Arte integrada | MEL-001/002/006 e VFX 007/008; catálogo, prefabs e geradores Editor | Regras estabilizadas, amostra artística | Manifesto e cena aprovados; elenco e colisão preservados |
| 7 Aceite | Critérios 14/15, FR-017/018 | Etapas anteriores | Regressão, profiling e nova rodada de gameplay, com limitações declaradas |

Estudos de arte podem antecipar a integração, mas assets/VFX finais dependem do alcance real. Não transformar a screenshot em cenário único nem copiar espécies fora do catálogo.

## Validação prevista

- Regras: cura, probabilidade/limites, progresso, seis passivas/níveis/tetos, recomputação sem acúmulo, deduplicação por trecho e concessão por lease.
- Física: fixture com horda em cada borda/canto/obstáculo, posições/contactos/penetração; comparar antes/depois sem eliminar pressão. Testes sintéticos não encerram aceite jogável.
- Integração: aura com/sem parede, demais oclusões preservadas, pool saturado, pausa/morte/fim/reuso e múltiplos níveis/especiais.
- UI: grafos de foco, botões bloqueados, rerrolagem, limite de três relíquias, propriedade da pausa, Cancel e hotplug em cada contexto.
- Tela: 16:9, 18:9, 19,5:9 e 4:3, Safe Areas dos dois lados, controle/toque, estados de recursos e encontros simultâneos.
- Produto: Windows x64 e Android ARM64 IL2CPP; teclado/controle/toque físicos; avaliar critérios 1–15 e SC, registrar commit, aparelho/modelo e limitações.
- Desempenho: baseline reproduzível antes/depois, 200/400 inimigos com pickups/VFX/água; CPU/GPU/memória/GC, alvos 60/30 nos aparelhos exigidos. S23 sozinho não aprova intermediário/entrada.

## Riscos e mitigação

- Remover contatos pode ocultar expulsão eliminando pressão: verificar ambos os resultados.
- Passivas antes não aplicadas alteram balanceamento real: comparar cada nível, combinações, terreno/evolução e relíquias.
- Teste antigo do bumerangue exige regra oposta: substituir explicitamente mantendo regressão dos outros projéteis.
- Cura garantida e teto/pool podem conflitar: reservar capacidade/política explícita, não perder miniboss silenciosamente.
- Decoração pode encobrir perigo: hierarquia visual e área efetiva verificadas em gameplay/Android real.
- Parâmetros foram aprovados explicitamente; não confundir cura comum de 10% com cura garantida em toda morte. Revisão de dados futuros deve registrar qualquer mudança além desses valores.

## Complexity Tracking

Sem violações propostas e sem novo framework/serviço. Nenhuma alteração de código ou asset Unity nesta etapa.

## Entrega e próximo passo

Planejamento apto a execução incremental; nenhuma implementação iniciada. Primeiro incremento sugerido: T001 de tasks.md desta feature, seguido do diagnóstico US1. IDs T001–T053 pertencem à feature 002 e não alteram as tasks da 001. Cada task valida, atualiza checkbox e recebe commit lógico antes de parar para revisão/publicação. Aceite físico/artístico/performance incompleto deve permanecer pendente, sem marcar a história inteira pronta.


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](decisions.md) e o relatório T030.
