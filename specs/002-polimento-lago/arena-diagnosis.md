# Diagnóstico de pressão da arena — preparação T002

Data: 03/11/2026. Base: `48da689` ([baseline T001](baseline.md)). Esta entrega prepara observação e fixture; **não reproduz a expulsão de produção, não confirma a causa física e não corrige movimento**. T003/T004 continuam pendentes. Requisitos FR-016/018; [contrato C1](contracts/gameplay-contracts.md).

## Ativação opt-in

Componente: Assets/QuackQuack/Systems/Diagnostics/ArenaPressureDiagnostics.cs, namespace QuackQuack.Systems.Diagnostics. Não foi adicionado a prefabs/cenas/geradores de produto nem ao benchmark. Awake desabilita o componente; habilitar pelo Inspector sem Begin não inicia captura. No jogo normal ele não existe e não faz consultas, logs, formatação ou alocação.

Em uma cópia de diagnóstico ou fixture, adicionar explicitamente a um GameObject e chamar:

```csharp
var observer = new GameObject("Arena pressure observer")
    .AddComponent<QuackQuack.Systems.Diagnostics.ArenaPressureDiagnostics>();
observer.Begin(seed, playerBody, playerCollider, observedColliders,
    capacity: 2048, maxPairs: 128, automatic: false);
// Após cada Simulate do PhysicsScene2D pertencente à fixture:
observer.Capture(simulationTick, simulationSeconds,
    QuackQuack.Systems.Diagnostics.ArenaCapturePhase.ExplicitAfterSimulation);
// Fora da janela medida:
observer.Stop();
observer.ExportCsv(System.IO.Path.Combine(Application.temporaryCachePath,
    "ArenaPressure", "capture-seed-" + seed));
```

seed é o seed real da partida/fixture, não um valor novo escolhido pelo observador. playerBody/playerCollider precisam pertencer ao mesmo corpo; observedColliders registra inimigos/obstáculos cujas posições serão amostradas. Registrar todos os colliders do pool, inclusive inativos, permite observar a troca de lease. O array fornecido é copiado para bindings; substituir elemento no array do chamador não muda a captura. Referências de objetos destruídos geram amostra inativa, não exceção.

Não trocar SimulationMode do jogo principal nem chamar Simulate extra na arena de produção para obter dados. O modo manual acima é para fixture que já controla sua própria simulação. Como alternativa de observação no jogo, Begin(..., automatic: true) usa FixedUpdate sem intervir na física.

## Ordem temporal e limites

- **FixedBeforeSimulation**: FixedUpdate com ordem 10000, antes da física daquele passo; contatos podem pertencer à última simulação concluída. Tick é contador local da captura, não RunDirector.Tick. Time.fixedTimeAsDouble é registrado. Não interpretar esse modo como pós-solver.
- **ExplicitAfterSimulation**: o chamador controla tick/tempo e deve amostrar depois de Simulate da fixture, como nos testes. A API não consegue provar a ordem de uma chamada externa; a fase é declaração do chamador.
- Cada frame guarda fixedDeltaTime observado; metadata também registra o valor no momento da exportação. Para Simulate com passo diferente do global, registrar o passo real no roteiro da fixture e usar tempo/tick fornecidos — não presumir que o valor global é o passo manual.
- Begin aloca buffers uma vez e reinicia seed/contadores/histórico. Capacidade 1–4096, pares 1–1024, até 1024 atores e no máximo 1048576 entradas por buffer indexado. Valores inválidos são rejeitados antes de iniciar.
- Ring preserva amostras mais recentes e conta Written/Overwritten. Capacity=2048 a passo 0,02 cobre aproximadamente 40,96s, não uma partida inteira. Exportar fatias adequadas sem ocultar sobrescrita.
- Contatos e overlaps usam buffers próprios de maxPairs por frame. BufferFull=true quando a capacidade é atingida: sinal conservador de possível truncamento, não prova de que todos os pares foram capturados. Aumentar limite e repetir quando necessário; nunca concluir ausência de pressão com buffer cheio.
- A lista de atores é fixa até novo Begin. Novos objetos fora dessa lista podem aparecer nas consultas de contato/overlap, mas não ganham histórico de posição automaticamente. Usar lista completa do pool ao compor a reprodução T003.
- Stop ou desabilitação suspendem; reabilitar sozinho não recomeça captura. Dados permanecem consultáveis/exportáveis. Consultas retornam structs por valor e índices inválidos retornam false.

## Dados coletados

| Arquivo | Conteúdo |
|---|---|
| .frames.csv | seed, sequência monotônica, tick/tempo/fase, passo global, posição/velocidade do Pato, bounds do collider, contagens e flags de buffers cheios |
| .actors.csv | índice registrado, collider ID, lease atual de PooledEnemy (zero quando ausente), atividade física, posição/velocidade |
| .contacts.csv | IDs dos dois colliders, leases atuais, ponto/normal, velocidade relativa, separação e impulso normal do contato retornado pelo motor |
| .overlaps.csv | collider/lease, validade, flag de sobreposição, distância assinada Collider2D.Distance e pontos/normal de separação |
| .metadata.txt | seed, capacidade, total escrito/sobrescrito, máximo de pares, fixedDeltaTime de exportação e versão do Unity |

Distância negativa válida sinaliza penetração geométrica atual; não confundir com contato reportado pelo solver nem com violação do limite autoral. A captura não contém avaliação automática de confinamento ou causa. Colliders trigger são excluídos; não há mudança em máscara/matriz de colisões. Leases são os atuais no instante da leitura; em modo pré-física, correlacionar contatos do último solver com cuidado ao reciclar objetos.

Arquivos usam cultura invariável (ponto decimal), exportação somente após Stop. Exportar enquanto captura lança exceção; não há escrita em disco/Debug.Log nos callbacks. Arquivos regeneráveis devem ficar em Logs/TestResults/cache, fora dos commits; não incluir serial, credenciais ou save pessoal.

## Fixture auxiliar T002

Assets/QuackQuack/Tests/PlayMode/ArenaContainmentTests.cs cria PhysicsScene2D local, corpo circular e parede. Controla passo 0,02 e espera o unload da cena no teardown, isolando testes de outras arenas. Não usa UnityEditor e pode compor futuras fixtures portáveis. Casos atuais verificam instrumentação, não contenção sob horda:

1. Componente dormente; Begin/Stop e retenção.
2. Contatos, penetração assinada e ausência de movimento provocado pela observação.
3. Ordem do ring, cópias por valor e sobrescrita.
4. Buffer cheio sinalizado.
5. Lease muda no reuso; array externo não altera bindings.
6. Desabilitação/reinício e reset de identidade.
7. Zero bytes gerenciados em chamadas dormentes e amostragem aquecida.
8. Exportação parada, seed/overflow e cultura pt-BR sem vírgula decimal.
9. Configurações/índices inválidos rejeitados.
10. Modo automático registra fase pré-física/passo e pode ser suspenso sem reiniciar a captura.

## Validação da implementação

Testes focados iniciais: 9/9 aprovados, zero ignorados (TestResults/Polimento002/T002/diagnostics-fixture.xml). O décimo caso do modo automático foi incluído na suíte final: todos os 10 casos da classe passaram. Primeiro ensaio tinha zero casos por caminho de cópia incorreto e foi descartado. A primeira execução real encontrou erro de ciclo de vida da fixture (scene unload assíncrono sem espera); corrigido com UnityTearDown, sem alterar regra de produto.

Regressão: **268/268 PlayMode** na versão final (TestResults/Polimento002/T002/PlayMode-final.xml), incluindo os 10 casos novos, e **465/465 EditMode** (TestResults/Polimento002/T002/EditMode.xml); zero falhas/ignorados. O caso de alocação confirmou zero bytes gerenciados em 100 chamadas dormentes e 100 amostras após aquecimento, usando GC.GetAllocatedBytesForCurrentThread fora das asserções. Isso não equivale a profiling ou aceite de desempenho de uma horda no aparelho.

Logs/XMLs locais em Logs/Polimento002/T002 e TestResults/Polimento002/T002, ignorados. .meta novos foram gerados pelo Unity na cópia isolada e preservados no projeto fonte. Nenhuma cena, prefab, máscara de colisão ou script de movimento foi alterado; não foram executados novos testes de player/Android ou benchmarks nesta task de instrumentação.

Reprodução: sincronizar somente os dois scripts e seus .meta na cópia isolada T001; Unity 6000.4.3f1, -batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform PlayMode -testFilter ArenaContainmentTests -testResults <arquivo.xml> -logFile <arquivo.log>. Não usar -quit com -runTests. Exigir total 10, Passed e zero ignorados; executar depois a regressão PlayMode completa sem filtro. Não aceitar execução com zero casos.

## Próxima etapa autorizável: T003

Compor cenário reproduzível na LakeRun real: seed, comandos, composição/leases, pressão em borda/canto/obstáculo, timestep/carga e contexto da arena do chefe. Capturar antes/depois dos passos relevantes, verificar buffers sem truncamento e registrar frame de expulsão/retomada. Só após reprodução afirmar causa e elaborar regressões T004. Nenhuma dessas execuções ou correções foi antecipada na T002; T075 da feature 001 continua aberta.
