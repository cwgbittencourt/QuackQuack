# Validação de T056

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Criado LakeEncounterCatalog.asset com classe, validação fechada e fábricas de
composição para cronograma, pools e SpawnDirector. Unity gerou sete definições,
sete prefabs de inimigo, prefab de aviso pooled, settings táticos, nove encontros,
material e cinco tons temporários. Nenhuma cena foi alterada.

| Inimigo | Papel | Vida | Velocidade | Pool máximo |
|---|---|---:|---:|---:|
| Formiga | Swarm | 8 | 1,8 | 128 |
| Aranha | WebCaster | 14 | 1,2 | 64 |
| Vespa | Charger | 12 | 2 | 48 |
| Besouro | FrontalArmored | 32 | 1 | 32 |
| Caramujo Aquático | SlowingTrail | 24 | 0,8 | 32 |
| Libélula Alfa | Charger | 180 | 2,8 | 1 |
| Sapo-Cururu | WebCaster | 300 | 1,1 | 1 |

Os minibosses compõem as estratégias existentes com atributos, antecedências,
recompensas e áudio próprios. Prefabs têm formas/cores temporárias e indicação
de direção; Besouro inclui arco frontal. Não há aceite de balanceamento ou arte.

| Onda | Janela | Composição introduzida | Intervalo | Limite ativo |
|---|---|---|---:|---:|
| 1 | 00:00–02:30 | Formiga | 3 s | 32 |
| 2 | 02:30–05:00 | + Aranha | 2,7 s | 56 |
| 3 | 05:00–07:30 | + Vespa | 2,4 s | 80 |
| 4 | 07:30–10:00 | + Besouro | 2,1 s | 104 |
| 5 | 10:00–12:30 | + Caramujo Aquático | 1,8 s | 128 |
| 6 | 12:30–15:00 | Cinco espécies | 1,5 s | 152 |

Libélula Alfa é solicitada aos 300 segundos, Sapo-Cururu aos 600 e Sapo-Rei aos
900. O alvo sapo-rei é reservado ao controlador futuro T057/T060; não foi criado
um prefab de chefe comum substituto. A timeline cobre os 15 minutos sem lacunas
ou sobreposições e usa IDs estáveis para cada encontro.

Validação exige elenco/papéis canônicos, IDs únicos, referências existentes,
componentes de pool e sinal, ondas somente com comuns, pressão não decrescente
por intervalo/limite e três marcos corretos. Coleções são protegidas e sessões
recebem cópias. Dados iniciais globais: 200 ativos, fila 32, oito tentativas de
trabalho por tick, 16 tentativas por posição e margem de exclusão 1,5. Oito efeitos
por tipo e steering 64 nos settings; pools somam 306 instâncias. Esses orçamentos
não equivalem a desempenho aprovado.

| Suíte | Aprovados | Falhas esperadas anteriores | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode completo | 339 | 29 | 0 | 2 |
| PlayMode completo | 125 | 9 | 0 | 2 |
| EditMode do catálogo com material final | 16 | 0 | 0 | 0 |
| PlayMode do catálogo com material final | 2 | 0 | 0 | 0 |

Dezesseis novos EditMode cobrem elenco, prefabs/sinais, cronograma, snapshots,
entradas ausentes/duplicadas, referências inválidas, lacunas, pressão e limites.
Dois novos PlayMode criam os pools reais: as sete definições são adquiridas sem
aumentar a contagem de instâncias, e ambos os minibosses usam os sinais reais
com o scheduler, incluindo a área lenta de Sapo-Cururu.

Todos os novos casos e os anteriormente aprovados passaram. Falhas restantes:
18 RunResultTests, 11 SaveServiceTests e nove FrogKingTelegraphTests, pelos tipos
ainda ausentes. Build do catálogo e regeneração terminaram com saída zero;
os 25 arquivos .meta auditados de inimigos/settings, encontros e prefabs mantiveram
seus hashes. GUIDs de novos assets foram gerados pelo Unity e preservados na cópia.

Prévia 1280×720 gerada e inspecionada; rótulos táticos em português. É uma galeria
de manutenção em cena temporária não salva, sem gameplay simulado. O material final
usa QuackQuack/FeedbackRing, preservando cores e transparência dos sinais; os 16
testes do catálogo e os dois de integração foram confirmados após esse ajuste.
Executado em Temp/T033Validation; evidências
regeneráveis fora do Git: TestResults/t056-editmode.xml,
TestResults/t056-playmode.xml, TestResults/t056-EditMode-final.xml,
TestResults/t056-PlayMode-final.xml, Logs/t056-preview.png e logs de build/testes/prévia.

O catálogo prepara os dados e a composição. Agendamento periódico de lotes,
aplicação de limites por onda, câmera, armas/Quack, recompensas e encerramento
serão conectados pela orquestração da partida na T060, com chefe da T057.
A arena de treino atual mantém seu fluxo existente. Não houve aceite completo
de US3, builds de plataforma, playtest humano ou medição de desempenho.
T057 em diante permanecem pendentes.
