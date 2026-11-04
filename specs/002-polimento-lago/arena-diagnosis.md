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
## T004 — Regressões normativas antes da correção

Adicionada `WholeBodyStaysWalkableAndMovementResumesAfterPressure` na fixture
`ArenaLakeRunReproductionTests`, no mesmo `ArenaContainmentTests.cs`. Categoria
`ArenaContainmentRegression`: 18 casos descobertos, sem Ignore/Explicit ou falha esperada
convertida em sucesso. T003 continua diagnóstica; as novas asserções exigem o contrato C1.

- Usa RunBootstrap/LakeRun, seed 42, componentes, colliders e scheduler de produção.
  Estágio inicial controlado, Pato vivo/invulnerável, armas automáticas desligadas para
  isolar deslocamento. Não altera máscaras, tipos de corpo, paredes ou parâmetros do solver.
- Horda de 64 Formigas, formação 8×8 a partir de 0,9 unidade atrás do Pato; comando zero.
  Verifica quatro bordas, quatro cantos, Training rock, South rock e spawn original.
- Janela de pressão de 12 segundos, passo 0,02 s; repete leste e sudoeste com passo longo
  de 0,10 s. Este último é stress explícito, não declaração de desempenho/plataforma suportada.
- Em cada passo pós-física verifica bounds do corpo inteiro contra faces internas das
  quatro margens e ColliderDistance contra ambas as pedras. Tolerância 0,02 unidade
  para skin do solver, muito menor que espessura da margem; não tolera atravessar obstáculos.
  Acumula a primeira violação e continua até avaliar também a retomada.
- Após retirar a horda, aplica comando por 2,4 segundos em direção ao interior; nas
  pedras, afasta-se da face testada. Exige deslocamento projetado >0,5 unidade **e**
  corpo novamente caminhável. Deslizar paralelamente à parede por fora não satisfaz retorno.
- Caso `pressure`: Pato parado em área livre durante 1 segundo. Exige contatos físicos
  com inimigos e deslocamento pela pressão >0,05 unidade. Confere também que nenhuma
  máscara de exclusão dos inimigos removeu o jogador. Impede aceitar remoção global
  de contato ou tornar o jogador totalmente imóvel como correção de contenção.
- Quatro casos `boss-*`: transição real BeginBoss, fase Boss e instância do Sapo-Rei
  verificadas, ataques automáticos ativos e comando contra leste/oeste/norte/sul (sul em
  0,10 s). A transição de produção remove a horda e desliga o scheduler; não cria horda
  artificial na fase do chefe. Antes de medir retomada, desativa o corpo sólido do chefe
  para liberar espaço. Não cobre todos os padrões/fases do chefe ou contato em Android.

Execução na cópia isolada `Temp/Polimento002Validation`, Unity 6000.4.3f1, Windows Editor.
Para repetir somente esta matriz, usar `-runTests -testPlatform PlayMode
-testFilter WholeBodyStaysWalkableAndMovementResumesAfterPressure`; para executar também
T002/T003, filtrar `ArenaContainmentTests;ArenaLakeRunReproductionTests`. Não usar `-quit`.
Cada caso escreve seed, timestep, primeira violação, contatos, deslocamento, posição antes/
depois da liberação e projeção de retorno no output do XML. Resultados/logs seguem ignorados.

Primeira tentativa encontrou `Assert.Multiple` ausente no NUnit embarcado; substituído
por coleta explícita de violações e uma asserção final, preservando todas as condições.
Primeira suíte compilada: 293 casos, 276 aprovados/17 falhas/zero ignorados. Revisão da
fixture removeu também o chefe antes da retomada e direcionou retorno das bordas ao
interior; os resultados finais abaixo correspondem à fonte revisada.

### Resultado final T004

Suíte completa: **293 descobertos, 276 aprovados, 17 falhas, zero ignorados**;
`TestResults/Polimento002/T004/PlayMode-final.xml`, exit code Unity 2. Todas as 17 falhas
pertencem à nova matriz e são violações explícitas de contenção/retorno; nenhum erro de
setup/teardown ou teste antigo falhou. Os **275 testes anteriores passaram**.

| Grupo | Resultado observado antes da correção |
|---|---|
| Quatro bordas | Primeira violação tick 4; nenhum retorna à região caminhável após liberar horda |
| Quatro cantos | Primeira violação tick 7; retorno projetado ~0,30, abaixo de 0,5 e ainda fora |
| Duas pedras | Primeira violação tick 3; Training rock retorna a espaço válido depois, mas a violação durante pressão continua falhando |
| Leste/sudoeste em 0,10 s | Primeira violação tick 0/1; continuam fora após liberação |
| Spawn original | Primeira violação tick 263; termina fora e não reentra |
| Boss leste/oeste/norte/sul longo | Violação tick 51/51/51/10; **retomada passa após retirar corpo do chefe**. Falha é penetração transitória, não prova de expulsão completa nesses quatro casos |
| Pressão em espaço livre | Passa: 48 passos com contato; deslocamento projetado 1,709; retomada 11,400 |

Fonte testada e raiz com SHA-256 idêntico:
`CCF2D48F91AB4AB9CB0462EFF92EAB3F5F755EB267B2A3F0A9739752174579E7`.
Nenhuma correção de produção nesta task. O estado vermelho é intencional e não pode ser
tratado como aceite de gameplay: T005 deve fazer esta matriz passar preservando o teste
de pressão; T006/T007 continuam responsáveis por geometria e validação jogável/plataformas.
Não houve nova suíte EditMode, player IL2CPP, Android ou profiling nesta rodada.
## T005 — Correção de movimento e proteção do corpo físico (04/11/2026)

A solução mantém o Pato dinâmico, os inimigos cinemáticos e os contatos físicos autorados.
Não engrossa margens, altera solver/CCD, modifica máscaras de contato ou fixa coordenadas
visuais de arena. Não adiciona componente, asset ou cálculo geométrico puro novo: usa as
consultas Physics2D sobre os colliders existentes, no PlayerController e scheduler atuais.

### Escolha e alternativas

1. **Limitar a origem da sobreposição:** EnemyUpdateScheduler inclui a camada do jogador
   no Shape.Cast que já fazia contra terreno. Ao encontrar o Rigidbody2D do Pato, permite
   apenas a folga física `Physics2D.defaultContactOffset` além do primeiro contato, em vez
   de atravessar livremente o corpo no passo. A folga preserva contato e pressão do solver;
   terreno mantém a CollisionSkin autorada anterior. Após o avanço, ColliderDistance confere
   a profundidade real e retira somente o excesso além da folga física; o cast tem tolerância
   própria e não é uma medida exata da penetração. Afastamento radial não é bloqueado,
   incluindo Quack iniciado com as formas ligeiramente sobrepostas. O corpo do Pato é
   obtido uma vez na composição, não por inimigo/frame. Buffer cheio continua fechando
   o movimento. Atualização imediata de Body/Transform/EnemyState é preservada.
2. **Conter o deslocamento do solver:** PlayerController guarda a posição no início do
   FixedUpdate. Nos callbacks de colisão, varre o trajeto dessa posição até a posição
   resolvida pela física usando o círculo físico escalado e seu offset, contra sólidos da
   camada 8 e sem triggers. Usa a PhysicsScene2D do próprio jogador. Se houver obstáculo,
   limita o deslocamento ao primeiro impacto, com margem numérica de 0,001; buffer de 32
   cheio retorna ao início do passo. Isso impede atravessar margem/pedra por despenetração,
   incluindo o pouso do chefe (que não passa pelo scheduler comum). Não escolhe um ponto
   arbitrário de spawn nem faz clamp por nome de parede/coordenada de câmera.
3. **Pressão mantida em espaço livre:** nenhuma correção quando a varredura está livre.
   Consultas usam array reutilizável; múltiplos callbacks na mesma posição resolvida não
   repetem a consulta. ProjectSettings já usa ReuseCollisionCallbacks=1; não mudou aqui.
   Não há alocação explícita de coleção/array ou logging recorrente no caminho novo.

**Alternativa comparada e rejeitada:** trocar atribuição imediata por MovePosition, junto
à proteção do Pato, aprovou os 18 casos T004 (`attempt1.xml`), mas a suíte completa teve
289/293 aprovados e quatro regressões: SwarmPursuesAndSteeringIsBudgetedWithoutPerEnemyUpdate,
ChargerWarnsWithRealVisualAndAudioBeforeLockedDirectionCharge,
SweptContactDamagesPlayerOnceWithInvulnerabilityAcrossEnemies e
CentralStepActuallyMovesEnemyBodiesAndPresentationTogether. As consultas de combate atuais
precisam observar deslocamento no mesmo passo; adiar para Simulate quebrou esse contrato.
Não enfraquecer esses testes nem redesenhar a ordem de todo o combate nesta correção.
A alternativa final com atualização imediata + cast do Pato passou nos 35 testes focados
(`attempt2.xml`), antes de adicionar os dois casos de regressão específicos abaixo.

**Alternativas não adotadas:** remover colisão jogador/inimigo elimina pressão; somente
CCD/mais iterações/parede espessa não resolve a origem demonstrada; clamp retangular
ignora as pedras e duplica geometria. Um componente ArenaConfinement com limites autorais
separados não foi necessário; cenas e .meta permanecem intactos, para revisão na T006.

### Testes e limites

A matriz normativa T004 permanece **sem alteração**, inclusive tolerâncias, verificações
de retorno e teste de pressão (SHA-256 do arquivo
CCF2D48F91AB4AB9CB0462EFF92EAB3F5F755EB267B2A3F0A9739752174579E7).
EnemyUpdateSchedulerTests recebe mais dois casos: passo longo limita profundidade de
contato sem retirar máscara física; Quack consegue separar inimigo começando sobreposto
na folga de contato. Nenhum teste foi removido ou convertido em Ignore.

Durante preparação final, a assinatura de CircleCast em PhysicsScene2D foi ajustada após
erro de compilação, consultando o XML da API instalado junto à versão fixada. A rodada
final abaixo usa essa correção. Logs/XML/CSVs permanecem ignorados em Logs/Polimento002/T005
e TestResults/Polimento002/T005; cópia isolada Temp/Polimento002Validation.

Esta task não aprova gameplay físico ou desempenho: a sensação da pressão e deslize,
Windows/Android reais, hardware intermediário/entrada e profiling continuam para T007 e
as tasks transversais. A proteção assume início de passo válido e o collider circular
exigido pelo PlayerController; não é recuperação de spawn inválido/teleporte externo para
fora da arena. Alterações dinâmicas de geometria/escala não fazem parte deste incremento.

Uma suíte intermediária com os novos testes teve 294/295 aprovados: o teste de passo longo
mediu profundidade 0,025 para folga física 0,01, apesar do limite baseado no cast. A correção
passou a verificar ColliderDistance após avançar e retirar o excesso usando a separação
normal/signed distance da própria engine. O XML da API local documenta a orientação da
normal; não se duplicou interseção de círculo/retângulo em regra pura. O teste mantém
seu limite de folga +0,001 e não foi flexibilizado.

### Resultado final da correção

- **295/295 PlayMode aprovados**, zero falhas/ignorados, Unity exit 0;
  `TestResults/Polimento002/T005/PlayMode-accepted.xml`.
- Todos os 18 cenários T004 têm `firstInvalid=-1`, retorno a espaço caminhável e
  deslocamento de retomada >0,5. Os 17 casos antes vermelhos agora passam, sem alterar
  o arquivo da matriz. As duas regressões novas e todos os testes antigos passam.
- Pressão em espaço livre: 48 passos com contato e 0,2254483 unidade de deslocamento
  projetado em 1 s; retomada 10,91999 unidades. Antes, no mesmo ensaio T004, eram
  1,709099 unidades sob pressão. **O contato/empurrão permanece, mas a intensidade efetiva
  caiu com a limitação de sobreposição.** Não se declara equivalência de sensação ou
  balanceamento por esse teste: T007 deve avaliar se o bloqueio/pressão continuam relevantes.

| Grupo | Contenção e retomada na suíte completa |
|---|---|
| Quatro bordas | Sem violação; retomada projetada 10,17–12,29 |
| Quatro cantos | Sem violação; retomada 5,44–9,61 |
| Duas pedras | Sem violação; retomada 6,11 / 11,93 |
| Passo longo 0,10 s | Sem violação; retomada 10,01 / 9,61 |
| Spawn original | Sem violação; retomada 11,48 |
| Quatro casos do chefe | Sem violação; retomada 10,16–12,40 após liberar espaço |

Hashes SHA-256 das fontes na raiz e na cópia de validação são idênticos:

| Fonte | SHA-256 |
|---|---|
| EnemyUpdateScheduler.cs | E9F891C6E06A2319ACE6E53C2834364E5E3E3C5B8BF3245FC3047166F01E1D2B |
| PlayerController.cs | B085BA05FE767A93030771EB285C2179EA9ACBBC4E664D78D5FD465AFD897DF6 |
| EnemyUpdateSchedulerTests.cs | B3394BE25F44876B2ECA86E7346A6D17C44BA0FE66DC356E49225F98413A4839 |

Não houve nova build de produto/player IL2CPP, Android ou medição de FPS/GC nesta rodada.
T006/T007 e T075 anterior continuam pendentes. As cenas e todos os .meta foram preservados.

Suíte EditMode completa também aprovada: **465/465**, zero falhas/ignorados,
`TestResults/Polimento002/T005/EditMode-accepted.xml`, Unity exit 0. Não foi necessário
ArenaBoundsTests: não há novo algoritmo geométrico puro; a proteção usa Physics2D real,
validada na matriz de integração acima.
## T006 — Integração da cena e dos geradores (04/11/2026)

A geometria existente foi mantida: a matriz T004 já passava na T005, incluindo pedras,
cantos e chefe, sem engrossar/relocalizar margens. A integração necessária é explicitar
na cena a configuração física que PlayerController.Awake aplica em runtime.

- LakeArenaSetup.ConfigurePlayerPhysics configura corpo dinâmico, gravidade zero,
  rotação travada e detecção contínua, exigindo collider circular sólido/habilitado.
  O gerador da arena de treino e LakeRunSetup.Build usam a mesma configuração.
- LakeRunSetup.UpdateArenaPhysics atualiza a LakeRun existente e salva com
  EditorSceneManager. Menu: `QuackQuack/Update arena physics (002 T006)`.
  Evita reconstruir HUD/prefabs a partir de uma versão anterior da cena de treino.
- Gerado pelo Unity 6000.4.3f1 na cópia isolada Temp/Polimento002Validation, com
  `-batchmode -quit -buildTarget Win64 -executeMethod
  QuackQuack.Editor.LakeRunSetup.UpdateArenaPhysics`. Marcador
  `T006_ARENA_PHYSICS_SAVED` e exit 0 em Logs/Polimento002/T006/generate.log.
- Somente o LakeRun.unity gerado foi copiado de volta, com .meta original intacto.
  Diff da cena limitado a GravityScale 1→0, CollisionDetection 0→1 e Constraints 0→4
  no Rigidbody2D do Pato. BodyType já era Dynamic. Nenhum ID/referência, posição,
  tamanho de collider, layer, máscara, UI, câmera, água/terra, pedra, refúgio ou
  composição da fase do chefe mudou. Lake.unity não foi regravada nesta entrega;
  seu gerador já passa a aplicar a configuração compartilhada nas próximas gerações.
- Repetida a geração no Unity: exit 0, mesmo SHA-256 antes/depois. A atualização é
  idempotente. Log em Logs/Polimento002/T006/idempotence.log.

A configuração de runtime da T005 permanece igual; não é uma segunda correção de física
nem novo aceite de gameplay. Não houve edição textual de asset serializado. Logs/XML/
capturas e alterações de importação/configuração da cópia continuam fora do commit.

Validação da cena gerada: **50/50 PlayMode aprovados, zero falhas/ignorados**, Unity exit 0,
em TestResults/Polimento002/T006/PlayMode.xml:
ArenaContainmentTests 10, ArenaLakeRunReproductionTests 25 (incluindo os 18 normativos
T004), LakeArenaTests 2 e RunDirectorFlowTests 13. Cobre instrumentação, contenção,
pressão/retomada, treino e integração do encontro do chefe na cena atualizada.
Não houve nova suíte completa EditMode/PlayMode nem build/player/Android/profiling nesta
entrega de configuração; 465/295 completos da T005 são evidência anterior.

LakeRun.unity gerada/testada/raiz, SHA-256:
`04C2BC01279DC5459274737A79C3538DA44DD1B2F6A38676D8B03EA3DC9F3AF8`.
.meta original preservado, SHA-256:
`FFC6A9F42A419C811F78BB6A73BA7C5C10A0865A1A8C7D837FEB84C34DBBE455`.

T006 encerrada. A T007 permanece aberta para gameplay Windows/Android, comparação da
pressão e retomada com entrada física; T075 antiga não muda de estado.
