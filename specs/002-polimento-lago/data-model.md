# Modelo de dados — Polimento do Lago

Design de 03/11/2026; nomes novos são contratos propostos, não APIs já implementadas. Dados autorais em ScriptableObjects; regra pura não depende de sprites/controles.

## 1. PickupPolicyDefinition (novo Content/Pickups)

| Campo | Regra |
|---|---|
| CommonHealChance | finito em [0,1], autorado 0,10; sorteio independente por derrota comum |
| CommonHealFraction | finito em (0,1], autorado 0,15 sobre MaxHealth no momento da coleta |
| MiniBossHealFraction | finito em (0,1], autorado 0,50; emissão garantida por derrota de miniboss |
| CaptureBodyRadiusMultiplier | finito positivo, autorado 3; raio centro-centro = 3 × raio físico mundial do Pato |
| AttractionWaterSpeedMultiplier | finito >1, autorado 1,25 × velocidade efetiva na água |
| Presentation/ledger capacities | inteiros positivos dimensionados para carga/partida suportadas; não são limites de chance nem autorizam descartar recompensa |

Não há cooldown de cura comum nem teto de gameplay. Probabilidade 10% não é promessa de um sucesso em cada dez eventos. Fonte de RNG determinística dedicada a drops, separada de ofertas/spawn. Fronteira do sorteio: valor em [0,1) menor que 0,10 aceita; 0,10 rejeita.

## 2. RewardId e RewardLedger (novos Core/Progression)

RewardId = identidade de partida + lease da origem + espécie de recompensa + ordinal. Nunca somente GetInstanceID. Common/MiniBoss/FinalBoss são origens resolvidas pelo catálogo/contrato de encontro, não por nome de GameObject.

Registro contém ID, origem, posição mundial de derrota, tipo XP/HealCommon/HealMiniBoss, XP inteiro positivo ou fração de cura, estado, alvo atual e ligação opcional à view pooled. Moedas/estatísticas preservam seus contratos; não repetir ao reprocessar drop.

Transições: Due → WaitingForView → Grounded → Attracted → Claimed → Released. A ausência de slot visual retém Due/WaitingForView. Cada transição de claim aceita uma vez; recycle limpa alvo/velocidade/claim/lease. RNG ocorre só na derrota, não ao tentar materializar novamente. Ledger e views são prealocados/poolados; nenhuma perda silenciosa ou concessão remota para resolver saturação.

XP de cada derrota deve ser positivo em dados aprovados; despawn/expiração não é derrota. Chefe final usa origem própria e valor editável positivo, concedido uma vez antes do resultado terminal; zero cura automática. Autoria da quantidade de XP de chefe é revisão de dados em T026, sem número escondido no código.

**Nota de implementação T025 (05/11/2026):** `RewardLedger` puro recebe partida, RNG dedicado, chance/frações e capacidade explícita de derrotas; pré-aloca até duas recompensas por derrota. Capacidade insuficiente falha antes de RNG/mutação e deve ser dimensionada na integração T027. Identidade implementada inclui origem e ordinal zero por tipo. Estados iniciais Due/WaitingForView/Grounded/Claimed e claims positivos são lógicos; views, Attracted/Released e teardown permanecem nas próximas tasks. Ver `test-report-t025.md`.

## 3. AttractionState (novo Core/Progression)

Campos: RewardId, posição atual, estado Grounded/Following/AwaitingMissingHealth, alvo, velocidade derivada. Captura usa distância entre centro do Pato e centro do pickup <= 3 × raio físico do Pato, inclusive igualdade. Base atual: 0,42 → 1,26. Escala física autorada uniforme positiva; mudanças invalidam cache do raio. Sprite/animação não alteram captura.

Após Following, sair do raio não cancela. Movimento contínuo até contato de coleta, velocidade = 1,25 × velocidade efetiva do jogador na água, incluindo modificadores vigentes e sem penalidade transitória de terreno terra. Atualizar referência quando modificadores mudam. Observar obstáculos não bloqueia movimento do pickup; isso não muda colisão do Pato/inimigos.

Pause/ChoosingUpgrade: congelar posição/claim. Dead/terminal: cancelar following e limpar no teardown; não curar morto. Cura com vida cheia não inicia captura; se ficar cheia após captura, acompanha sem claim até haver vida faltante. Fim de partida não concede automaticamente curas/XP de chão; exceção explicitamente aprovada é XP devido do chefe final antes de encerrar.

## 4. PlayerState.Heal e EffectiveAttributes

Heal aceita fração finita positiva e jogador vivo: applied = min(MaxHealth - Health, MaxHealth × fraction). Zero aplicado não consome cura. Nunca exceder MaxHealth nem ressuscitar. MaxHealth é lido na coleta, não na emissão.

EffectiveAttributes agrupa atributos base, níveis de passivas, relíquias e terreno sem mutar definições. Recomputar do estado base após mudança, não reaplicar cumulativamente. Manter ordem/composição de relíquias existentes e testar cada passiva autorada. Speed/Capacity/Recovery/Damage/AttackInterval e raio de Bolhas são positivos/finitos; limites de passiva seguem dados.

BubbleHitRadius = authored HitRadius × (1 + Sabão.GetModifier(level)), combinado com modificadores relevantes de área existentes sem duplicar. Sabão só atua em Bolhas; 0 níveis retorna base, 1 nível 1,16 × base, 5 níveis 1,80 × base. Diâmetro visual = 2 × raio efetivo em unidades mundiais. Aquisição, dano e velocidade não mudam por Sabão. AreaRadius zero de Bolhas não pode anular o bônus.

## 5. ProjectilePassage

Campos: projectile lease, fase Outbound/Inbound, alvos atingidos por identidade de alvo+lease na fase. Troca de fase exatamente uma vez; passo grande é segmentado na fronteira; um alvo com vários colliders conta uma vez. Reset ocorre no spawn/release e na mudança de passagem, nunca a cada frame. Reuso de alvo pooled é identidade nova.

## 6. UI snapshots

- ProgressSnapshot: Level >=1, XP acumulado, limite anterior/próximo e fração normalizada [0,1] produzidos por ExperienceState. Múltiplos níveis processados antes da leitura. Após último limiar, mostrar nível atual e barra cheia, sem dividir por zero ou criar nova curva.
- ResourceSnapshot: Health01, Breath01, Ready, Empowered/cooldown e ícones/estados. Vida/Fôlego sem números/textos de terreno; progresso/nível e avisos de pausa/fim continuam em áreas próprias.
- SpecialEnemySnapshot: encounter/lease, posição âncora, Health01 e categoria. Registro aceita ciclo EnemyState e FrogKingController; remove ao morrer/desativar/reusar. View segue acima do alvo e não representa alvo fora da tela em outra posição.
- HintWindow: elapsedSimulationSeconds em [0,10], família/bindings; inicia a cada partida, pausa suspende, troca entrada não reseta, após 10 não reaparece.
- UpgradePreview: raio atual/previsto de Bolhas, nível de Sabão e grandeza descrita como raio, sem exagerar 16% de raio como 16% de área.

## 7. InputAvailability e ControlProfile

Campos: dispositivos conectados/habilitados, controle disponível, última fonte ativa, família e bindings, foco anterior, contexto Gameplay/Paused/Relics/ChoosingUpgrade/Results. Disponibilidade e última atividade são conceitos distintos. Smartphone com controle disponível oculta touch; perda/desabilitação restaura touch, limpa comandos presos e preserva contexto.

Bindings: Gameplay Move/Quack/Pause/Relics e UI Navigate/Submit/Cancel. Xbox Menu/View, PS Options/Share/Create quando expostos, genérico Start/Select; sul confirma/leste retorna. Perfil editável permite fallback sem Select, com opção focalizável no menu. Nenhum binding pode apagar último acesso à pausa/relíquias sem alternativa. Salvar perfil em arquivo local versionado separado do save de progresso, sem serializar dispositivo físico ou credenciais.

## 8. ArtManifest (documental)

Cada asset: ID canônico, função, referência, origem/autoria/licença, arquivo-fonte/exportado, dimensão/escala/pivô, estados/animações, parâmetros de importação, camada/ordem, contraste, prioridade e evidência de aprovação. Ordem de lote aprovada em decisions.md. Cenário não pode redefinir colisão por decoração, nem acrescentar espécies. Fontes e assets finais duráveis no projeto; arquivos temporários/logs fora dos commits.
