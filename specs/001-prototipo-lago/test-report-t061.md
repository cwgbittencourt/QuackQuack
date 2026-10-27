# T061 — Tela final e progresso persistido

Data: 2026-10-27. Unity 6000.4.3f1, revisão 39d1a88d4dd1.
Escopo: somente T061. Próxima task: T062.

## Entrega

ResultsView apresenta vitória, derrota e abandono, tempo/chefe/seed, derrotas,
sequência, dano por origem, build/níveis, evoluções, sinergias, relíquias,
recursos/histórico de escolhas, recompensas/desconto/bônus, desbloqueios,
carteira, recordes e desafios. Três colunas roláveis mantêm detalhes acessíveis.
ResultsBridge conecta o resultado imutável e a transição para Results, com retry
após falha e JOGAR NOVAMENTE. O reinício espera a destruição do bootstrap antigo
antes de carregar RunBootstrap; a nova partida recebe outra ID.

RunProgression aplica o resultado uma vez por RunId, independente da seed. Moedas
usam a recompensa terminal existente, sem recalculá-la; abandono não registra
vitória. Recordes usam máximos e tempo inteiro; desafios são cumulativos ou melhor
partida. Perfil/recibo copiam as coleções. Metas autorais provisórias: 100 inimigos
acumulados, 900 segundos em uma partida e uma vitória contra o Sapo-Rei. São dados
editáveis, sem recompensa monetária adicional ou aceite de balanceamento.

Schema 2 grava carteira, recordes, desafios, recibos e relíquias atomicamente.
Migração 0/1 preserva relíquias com perfil vazio. Perfil inválido recupera backup
coerente. Save(loadout) preserva o progresso. Repetir CommitResult não regrava nem
rotaciona backup; falha de escrita permite retry sem crédito duplicado. A UI mostra
saldo projetado e pede nova tentativa quando pendente. Sair com erro não resolvido
não garante persistência do resultado. Não há histórico integral em disco.

## Autoria e validação

ResultsSetup.Build gerou ResultsScreen.prefab, ResultsProgression.asset e ligação
em LakeRun pelo Unity, sem edição manual de YAML. LakeRunSetup reconecta a tela
quando os assets existem. O hash do .meta de LakeRun foi comparado antes da cópia
e preservado; arquivos novos receberam .meta do Editor.

Execução na cópia isolada Temp/T033Validation. Logs/XML/capturas são ignorados.

| Etapa | Total | Aprovados | Falhas | Exit code |
|---|---:|---:|---:|---:|
| Contrato antes da implementação | 1 | 0 | 1 esperada | 2 |
| EditMode completo | 456 | 456 | 0 | 0 |
| PlayMode completo | 174 | 174 | 0 | 0 |

Quinze novos casos EditMode cobrem contrato, três finais, repetição, seeds iguais
com IDs distintas, máximos/metas, saturação, imutabilidade, validação de perfil,
segundos inteiros, migração, backup, falha/retry e preservação do perfil ao salvar
relíquias. Dez novos casos PlayMode cobrem campos dos três finais, textos longos,
rolagem e callbacks sem duplicação; abrem RunBootstrap para vitória, morte,
abandono, falha de gravação/retry e reinício preservando saldo. O caso acelerado
do chefe desativa scheduler/armas e avança marcos de modo controlado.

As fixtures usam diretórios temporários exclusivos e SavePathProvider injetado;
a limpeza valida caminho absoluto/prefixo. Nenhum save pessoal foi lido ou alterado.
Capturas Logs/t061-won.png, t061-lost.png, t061-abandoned.png e t061-details.png
foram inspecionadas a 1280×720. Mostram fixture de UI com build máxima autoral e
colunas roladas, sem corte impeditivo dos campos longos; não são playtest humano.

Comandos: -executeMethod QuackQuack.Editor.ResultsSetup.Build com -batchmode
-nographics -quit; testes com -batchmode -runTests -testPlatform EditMode|PlayMode
-testResults <arquivo> -logFile <arquivo>. EditMode usou -nographics; PlayMode
manteve gráficos. Arquivos: TestResults/t061-red.xml, t061-editmode.xml,
t061-playmode.xml e Logs/t061-*.log.

## Limites

Sem playtest humano completo, builds Windows/Android, aparelhos reais, matriz de
proporções/Safe Area ou perfil de desempenho. Não se aprova gameplay, justiça,
balanceamento, controles móveis ou FPS/GC por estes testes. T062 em diante não
foram implementadas. Nenhuma dependência nova foi introduzida.
