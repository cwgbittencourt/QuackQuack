# Diagnóstico de pressão da arena — T002/T003

Data: 03/11/2026. Preparação T002 sobre `48da689` ([baseline T001](baseline.md)); reprodução T003 sobre `5b9f374`. A T002 preparou observação e fixture; a execução T003 abaixo reproduziu saída na LakeRun real e isolou o mecanismo físico. **Nenhuma correção foi aplicada; T004 em diante permanecem pendentes.** Requisitos FR-016/018; [contrato C1](contracts/gameplay-contracts.md).

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

## Escopo de reprodução preparado na T002

Compor cenário reproduzível na LakeRun real: seed, comandos, composição/leases, pressão em borda/canto/obstáculo, timestep/carga e contexto da arena do chefe. Capturar antes/depois dos passos relevantes, verificar buffers sem truncamento e registrar frame de expulsão/retomada. Só após reprodução afirmar causa e elaborar regressões T004. Nenhuma dessas execuções ou correções foi antecipada na T002; T075 da feature 001 continua aberta.

## Execução T003 — reprodução controlada na LakeRun

Relato complementar do usuário nesta rodada: Pato parado desde o início; inimigos o pressionaram para a esquerda duas vezes, até sair da cena; Quack ainda empurrava inimigos, mas movimento parecia indisponível. O relato orienta o cenário; não equivale a captura instrumentada da sessão original.

Fixture adicionada no mesmo ArenaContainmentTests.cs: classe ArenaLakeRunReproductionTests, categoria ArenaPressureReproduction. Carrega **RunBootstrap → LakeRun reais**, aguarda RunDirector.Ready, usa Pato/colliders/prefabs/formiga/pools/SpawnDirector/EnemyUpdateScheduler reais. Não altera cenas, definições, movimento ou scripts de runtime.

### Condições reproduzíveis

- Seed real do bootstrap: **42**; passo 0,02s; velocityIterations=8, positionIterations=3, sem alterar parâmetros do solver. Pato dinâmico, CircleCollider2D raio 0,42, detecção contínua autorada; inimigos cinemáticos, raio 0,4. Contatos inimigo/inimigo e XP continuam suprimidos como no produto; contato inimigo/Pato preservado no caso principal.
- Timeline/relógio automático do diretor e ataques automáticos de armas congelados para isolar pressão, sem substituir PlayerController. O teste injeta comando Move=zero e executa o FixedUpdate real do jogador, Advance do scheduler e Physics2D.Simulate em ordem explícita. Não altera a velocidade por força/teleporte durante a pressão.
- O seed/requisição de spawn é real, mas a **formação inicial é construída**, não uma reprodução natural das ondas: 64 formigas solicitadas pelo SpawnDirector em lotes, depois posicionadas em grade 8×8 a um lado do jogador, espaçamento 0,45 no eixo X e 0,30 no Y. Primeiro centro a 0,9 unidades em X e ±0,15 em Y, distância ~0,912 maior que soma dos raios 0,82: não começa com inimigos sobrepostos ao Pato. Sobreposição entre inimigos é permitida pela política de contatos do produto.
- Horda à esquerda do Pato em (11,8;0), junto da margem leste; variante do relato mantém **spawn real (-6;-2)** e coloca horda à direita. Não usar zero inimigos de fato com colliders invisíveis: o controle sem inimigos despawna todos.
- Dano inicial de 1 e invulnerabilidade longa mantêm o alvo vivo para não confundir morte com expulsão; propriedades físicas e dano/velocidade dos inimigos não são editados. Isso isola deslocamento, não é teste de balanceamento/sobrevivência.
- 600 passos (12s), com captura pré/pós por passo, 1200 frames no ring de capacidade 1400, 128 pares por frame, 64 atores mais quatro margens. Após pressão, despawn da horda e 120 passos (2,4s) de comando em direção à origem, usando controlador real. Quack é solicitado depois da tentativa de retorno; aceitação é registrada, sem inferir dano/empurrão sobre alvos já removidos.
- Fixture usa SimulationMode2D.Script somente durante a tentativa, restaura modo/passos ao terminar, descarrega cena e usa save temporário próprio. Antes de destruir serviços, abandona a partida para evitar dependências já destruídas. Nenhum save pessoal é usado.

Geometria observada/autorada da LakeRun: margem leste x=[12,25;12,75], oeste x=[-12,75;-12,25], norte y=[6,75;7,25], sul y=[-7,25;-6,75]. Margens horizontais abrangem x=[-12,8;12,8], verticais y=[-7;7]; há sobreposição sólida nos cantos, não uma abertura entre bancos. Rochas interiores permanecem presentes; esta task não cobre a matriz de pressão contra rochas ou a arena do chefe.

### Resultado e controles

Primeira saída completa é definida aqui como **bounds do collider inteiramente além da face externa de alguma margem**. Contato residual de skin pode permanecer mesmo nessa condição; isso não significa ausência absoluta de todo overlap do motor. Penetração inicial usa bounds além da face interna por mais de 0,02; não confundir essa condição parcial com saída completa.

| Cenário | Inicial do Pato | Primeira saída (tick zero-based / tempo após staging) | Posição após 12s | Após remover horda e comandar retorno por 2,4s |
|---|---|---|---|---|
| grid64-a / grid64-b | (11,8;0) | 162 / 3,26s, pela margem sul | (11,82516;-7,674999) | (5,863132;-7,674999), não reentrou |
| spawn64-right-a / spawn64-right-b | (-6;-2) | 392 / 7,86s, margem norte após pressão junto da oeste | (-11,825007;7,674999) | (-5,863071;7,674999), não reentrou |
| grid64-no-player-contacts | (11,8;0) | Não ocorreu em 12s | (11,8;0) | (1,640010;0), dentro |
| grid64-frozen | (11,8;0) | Não ocorreu em 12s | (11,8;0) | (1,640010;0), dentro |
| no-enemies | (11,8;0) | Não ocorreu em 12s | (11,8;0) | (1,640010;0), dentro |

Controle sem contato: adiciona exclusão da camada do Pato **somente nas instâncias da fixture**, mantendo scheduler/geometria iguais; release do pool restaura máscara. É contrafactual de diagnóstico, não proposta aprovada de correção. Controle congelado mantém colliders e horda inicial, mas não executa Advance do scheduler. No cenário sem inimigos, a lista de inimigos ativos é zero. Os controles mostram necessidade de movimento da horda + contato físico no caso construído; não são prova de que remover colisão seria solução aceitável.

As duas repetições de cada cenário principal produziram frames.csv byte a byte iguais, inclusive tempos/posições/contagens; leases/IDs dos atores são únicos por ciclo e não precisam coincidir entre execuções. Máximo observado de contatos: 5 no caso junto da leste e 7 no spawn. Leitura de **todos** os 1200 frames por caso: zero flags de buffer cheio, zero sobrescritas. Não houve exclusão seletiva de frames.

### Evidência do passo de expulsão

| Cenário e fase | Tick | Posição Y | Velocidade Y |
|---|---:|---:|---:|
| grid64-a, pós-física | 160 | -6,947831 | 0 |
| grid64-a, pós-física | 161 | -6,983831 | 0 |
| grid64-a, pré-física | 162 | -6,983831 | 0 |
| grid64-a, pós-física | 162 | -7,674783 | 0 |
| spawn64-right-a, pós-física | 391 | 6,979000 | 0 |
| spawn64-right-a, pré-física | 392 | 6,979000 | 0 |
| spawn64-right-a, pós-física | 392 | 7,674744 | 0 |

No primeiro caso, a margem sul (collider runtime 39208 na captura controlada inicial) tem distância assinada ao Pato -0,663831 no pré-passo 162; após simulação, -0,005217, com normal geométrica mudando de -Y para +Y. O bounds superior do Pato após a expulsão é -7,254783, além da face externa -7,25. Impulsos normais registrados nesses contatos são zero; posição muda apesar de velocidade do Pato zerada. IDs runtime mudam entre carregamentos; identificar geometria pelos quatro últimos atores registrados, não fixar ID como regra.

O Pato permaneceu em RunPhase.Playing e ActivateQuack foi aceito após o retorno em todos os cenários. Ainda ocorreu deslocamento paralelo à margem externa, mas a componente de retorno foi barrada. Portanto a reprodução demonstra **saída e impossibilidade de reentrar pelo caminho tentado**, não congelamento de todos os eixos nem falha global do adaptador de entrada. A variante do spawn passou junto da margem esquerda e saiu por cima; a saída exatamente pela face esquerda relatada pelo usuário não foi capturada. O empurrão do Quack contra inimigos enquanto fora também não foi medido.

### Causa confirmada nesta reprodução versus hipóteses

**Confirmado:** o scheduler aproxima inimigos cinemáticos por atribuição imediata de Body.position, fazendo Shape.Cast somente contra camada 8 (terreno), sem varrer o Pato. Os contatos físicos com o jogador dinâmico permanecem ativos. As leituras pré-simulação mostram sobreposição geométrica com inimigos; a pressão desloca o jogador sem comando, aprofunda a penetração no banco e a resolução posicional o expulsa para a face externa. A mesma configuração não expulsa ao retirar contato jogador/inimigo ou movimento do scheduler. Nenhuma escrita de posição do jogador ocorre no trecho medido da fixture; só staging inicial, comando pelo controlador e simulação física.

**Confirmado:** não há falha de carregamento da LakeRun ou buraco autorado naquele canto; as margens sólidas estão presentes e se sobrepõem. Detecção contínua do jogador não impediu esta correção posicional. Uma vez fora, a própria parede sólida bloqueia a reentrada tentada, enquanto a partida e comandos continuam aceitos.

**Ainda hipótese/limite:** não foi instrumentada a sessão original do usuário; não se afirma identidade de todos os detalhes ou da face de saída. Não se isolou qual escolha interna/ordem de contatos do solver é suficiente em todas as plataformas, nem comparou outro método de movimento cinemático. Não atribuir o defeito ao número exato de iterações, apenas à camada física observada. Timestep longo, rochas, outras bordas/espécies/quantidades, frequência render/fixed real e arena do chefe ainda precisam das regressões T004 e validação T007.

### Alternativas para avaliar na T005, após regressões T004

1. Resolver deslocamento/pressão dos inimigos sem inserção cinemática arbitrária contra o Pato, avaliando sweep/contatos relevantes e integração em passo físico. Comparar preservação de pressão, densidade, navegação e custo; nenhum método foi implementado nesta task.
2. Definir contrato de área caminhável e proteção/recuperação excepcional baseada em posição válida como defesa adicional, sem teleporte que esconda interpenetração ou elimine a pressão.
3. Reforçar geometria/ordem de simulação somente com prova comparativa; engrossar parede, aumentar iterações ou habilitar CCD isoladamente não constitui solução demonstrada.
4. Rejeitar desativação global de contatos jogador/inimigo como atalho: o contrafactual remove a pressão que deve ser preservada.

### Evidências e reprodução da T003

- Fonte: mesmo ArenaContainmentTests.cs, classe ArenaLakeRunReproductionTests. O teste verifica composição, captura e ausência de truncamento; **passar não significa que a arena contém o Pato**. T004 adicionará as asserções normativas de contenção/retomada que devem falhar antes da correção.
- Cópia de validação: Temp/Polimento002Validation, Unity 6000.4.3f1 Windows Editor PlayMode. Nenhuma nova medição Android/player/performance foi feita aqui.
- Definir QUACK_ARENA_OUTPUT para diretório ignorado e executar -batchmode -projectPath <copia> -buildTarget Win64 -runTests -testPlatform PlayMode -testFilter ArenaLakeRunReproductionTests -testResults <xml> -logFile <log>. Sem variável, exporta no temporaryCachePath/ArenaPressureT003. Repetir preservando geometria, seed, formação, ordem e passo.
- Sete casos focados aprovados em TestResults/Polimento002/T003/user-scenario.xml, zero ignorados; CSVs/metadata/summary em TestResults/Polimento002/T003/user-scenario. Tentativas iniciais em attempt1/attempt2/controlled foram preservadas, inclusive o resultado negativo da formação concentrada (64 em faixa estreita não expeliu em 12s).
- Primeiro ensaio teve falha de teardown por ordem de descarte dos serviços; a fixture passou a abandonar antes de destruir o bootstrap, sem mudar RunDirector/WeaponRunner. Não esconder esse erro com LogAssert.Expect nem contá-lo como falha de contenção.
- Regressão PlayMode final: **275/275 aprovados, zero falhas/ignorados**, incluindo os 10 casos de instrumentação T002 e os sete cenários T003 (TestResults/Polimento002/T003/PlayMode.xml). A execução completa também exportou as repetições em TestResults/Polimento002/T003/regression. Não houve alteração de regras nem testes EditMode novos; os 465/465 EditMode da T002 são referência anterior, não nova execução desta rodada. Logs, capturas e resultados regeneráveis continuam ignorados.

| Frames CSV da coleta focada | SHA-256 |
|---|---|
| grid64-a / grid64-b | A80BDACA5BC50A413C215855DA12DD76E40A2A14E5BAD97C20FDA16495C8154B |
| spawn64-right-a / spawn64-right-b | 47D2AB039BA95890622FCCD84B4EE683BC7A95B3A778E317C7E187DBAA02709E |

Esses hashes identificam a evidência desta rodada; não são expectativa portátil de bits de física entre máquinas/versões. Não versionar CSV/XML/logs. T004–T007 e T075 antiga permanecem abertas.
