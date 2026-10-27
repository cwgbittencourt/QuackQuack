# Validação de T052

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementados RelicDefinition, catálogo com dez assets, RelicLoadout com três slots,
RelicLoadoutView e composição LakeRelics na cena. Unity gerou assets, prefab, cena
e .meta; GUID da cena preservado. RelicSetup.Build permite regenerar a raiz isolada.

Todos os bônus iniciais são 5%, dados editáveis ainda sem aceite de balanceamento.

| Relíquia | Efeito | Desbloqueio nesta sessão |
|---|---|---|
| Botas do Lago | Movimento na água | Inicial |
| Garras de Junco | Movimento em terra | Inicial |
| Pena de Combate | Dano das armas | Inicial |
| Pedra da Margem | Dano das armas em terra | 15 s |
| Pérola do Lago | Dano das armas na água | 30 s |
| Apito Veloz | Velocidade de ataque | 45 s |
| Lente de Orvalho | Alcance das armas | 60 s |
| Anel de Ondas | Raio de área das armas | 10 XP |
| Concha de Fôlego | Recuperação de Fôlego | 20 XP |
| Pena Leve | Movimento | 30 XP |

Botas do Lago segue a sugestão da página 14 do documento funcional; os outros nove
itens são autoria inicial para efeitos suportados. Não são implementadas aqui as
sugestões de aliado, baú ou redução do requisito de evolução do documento amplo.
O requisito de arma N5 para evolução permanece conforme o escopo do protótipo.

Loadout copia dados e expõe coleções protegidas. Desbloqueio/equipamento repetidos,
IDs desconhecidos/bloqueados e quarta relíquia são rejeitados sem mutação.
Efeitos são compostos da base por atributo e terreno, sem acúmulo ou mudança do
asset; remoção retira somente seu efeito. PlayerController usa bônus de movimento
e recuperação; WeaponRunner compõe dano, cadência, alcance e raio por disparo.
Projéteis já emitidos preservam o snapshot do disparo.

O painel pausa ao abrir, mostra efeitos/condições/slots e permite equipar/remover.
Fechar retoma somente quando a abertura havia pausado; reabrir painel aberto não
perde esse controle. Cartas/fim ocultam o painel. Desbloqueios por tempo/XP não
avançam na pausa. Persistência entre sessões continua na T059.

| Execução | Aprovados | Falhas esperadas | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode completo | 296 | 29 | 0 | 2 |
| PlayMode completo | 80 | 9 | 0 | 2 |
| RelicLoadoutFlowTests após ajustes finais | 3 | 0 | 0 | 0 |

Os 11 casos RelicLoadoutTests da T048 agora usam chamadas tipadas e passaram;
13 novos casos cobrem catálogo, seis atributos, terreno, remoção, limiares e
limites de bônus. Os três novos PlayMode cobrem botões reais, pausa/retomada,
desbloqueios/slots/fim e velocidade do Rigidbody2D do Pato antes/durante/depois
de equipar Garras de Junco. Não houve regressão dos 77 PlayMode existentes.

Falhas EditMode restantes: 18 RunResultTests e 11 SaveServiceTests, pelos tipos
ainda ausentes. Falhas PlayMode: nove FrogKingTelegraphTests, pelo chefe ausente.
A execução inicial PlayMode revelou falta de limpeza do Bootstrap na nova fixture;
corrigido o teardown, a suíte completa terminou com somente as falhas conhecidas.

Prévia renderizada e inspecionada em 1280×720. Ajustados botão de abertura sobre
o título e rótulos dos itens bloqueados; regenerados os assets pelo Unity e
confirmados os três testes de fluxo após esse ajuste. A prévia usa estado preparado
no Editor e não grava esse estado na cena. Safe Area e outras proporções continuam
nas T068/T069. Não há aceite humano de gameplay ou balanceamento.

Execuções em Temp/T033Validation. Evidências regeneráveis fora do Git:
TestResults/t052-editmode.xml, TestResults/t052-playmode.xml,
TestResults/t052-relic-flow-final.xml, Logs/t052-preview.png e logs correspondentes.
Nenhuma build de plataforma ou medição de desempenho foi realizada.
T053 em diante permanecem pendentes.
