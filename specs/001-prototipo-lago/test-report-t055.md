# Validação de T055

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementados EnemyUpdateScheduler, EnemyAreaEffect pooled e EnemyBehaviorSettings
editável. PooledEnemy passou a fornecer Rigidbody2D/CircleCollider2D para movimento
varrido. PlayerController aplica/restaura o multiplicador de lentidão sem acumular
sobre velocidade base ou relíquias. Nenhuma cena ou asset autoral foi modificado.

| Papel | Comportamento implementado |
|---|---|
| Formiga / Swarm | Persegue, move com colisão e causa contato respeitando invulnerabilidade |
| Aranha / WebCaster | Mantém distância e cria teia lenta no ponto previamente anunciado |
| Vespa / Charger | Anuncia ponto alvo, trava direção e investe após antecedência |
| Besouro / FrontalArmored | Persegue e reduz dano frontal, permitindo dano por flanqueamento |
| Caramujo / SlowingTrail | Deixa área lenta na posição anunciada, com duração limitada |

Um FixedUpdate central dirige todas as aquisições do SpawnDirector. Decisões de
direção usam orçamento em rodízio; movimento, contato e prazos são processados
para todas as instâncias. PooledEnemy e EnemyAreaEffect não têm Update individual.
Buffers/arrays são preparados na inicialização, sem criar objetos em cada ataque.

Avisos têm LineRenderer/AudioSource reais e eventos DangerTelegraphed. Efeitos
recorrentes são pré-aquecidos pelo PoolService; sem capacidade para aviso, ataque
é suprimido. Teias/trilhas aplicam a maior lentidão, sem multiplicar sobreposições,
e expiração/desativação restaura a base. Campos já emitidos duram pelo tempo autoral.

Movimento usa varredura contra paredes da camada 8 e bloqueia em saturação do
buffer. Contato considera percurso e raio, evitando perder acertos de investida
rápida; obstáculos e invulnerabilidade bloqueiam dano indevido. Dano aceito tem
correlação própria. Armadura depende da direção do ataque; controles respeitam
resistências e cancelam avisos/investidas. Slots e efeitos usam lease para não
executar ataque antigo depois de retorno/readquisição.

Pausa/seleção congelam áudio e simulação; fim/desativação limpa efeitos e lentidão.
O diretor permanece pertencendo ao chamador. ApplyHit/ApplyControl são as fronteiras
para conectar armas/Quack na composição seguinte, sem alterar alvos legados nesta
tarefa. Catálogo, prefabs autorais, settings asset e montagem na arena ficam na T056.

| Execução | Aprovados | Falhas esperadas anteriores | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode completo | 323 | 29 | 0 | 2 |
| PlayMode completo final | 123 | 9 | 0 | 2 |

Sete novos EditMode validam números e orçamentos de EnemyBehaviorSettings.
Dezesseis novos PlayMode cobrem perseguição/steering, aviso visual/sonoro anterior
à investida, direção travada, áreas lentas/duração/sobreposição, armadura frontal
e flanqueamento, controle/resistência, paredes, pausa/seleção, saturação de VFX,
derrota/readquisição, fim/desativação, contato varrido/invulnerabilidade e reentrada
durante o aviso. Os 107 PlayMode anteriormente aprovados continuam passando.

As falhas restantes são exclusivamente 18 RunResultTests, 11 SaveServiceTests e
nove FrogKingTelegraphTests, por tipos ainda ausentes. A primeira suíte PlayMode
aprovou 13 novos cenários; os três casos adicionais e ajustes de contato foram
validados na fixture e novamente na suíte completa. Após os testes, somente a
mensagem diagnóstica de validação de intervalo recebeu ajuste de texto.

Execuções batchmode em Temp/T033Validation; EditMode com nographics e PlayMode
com suporte gráfico. .meta dos novos scripts/testes gerados pelo Unity e copiados
com GUIDs preservados. Evidências regeneráveis fora do Git:
TestResults/t055-editmode.xml, TestResults/t055-playmode.xml,
TestResults/t055-scheduler-final.xml e logs correspondentes em Logs/.

Os testes usam composição isolada com definições/settings temporários e o Pato
real. Não aprovam integração final da arena, legibilidade/percepção dos sinais,
balanceamento ou perfil com 200–400 inimigos. Não houve build ou medição em aparelho.
T056 em diante permanecem pendentes.
