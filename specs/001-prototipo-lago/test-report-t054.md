# Validação de T054

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Implementados SpawnRequest e SpawnDirector, com fila e arrays de aquisições de
capacidade fixa. PoolService ganhou consulta de registro por chave/tipo/família;
PooledEnemy ganhou via interna para definições já validadas, preservando a validação
das APIs públicas. Nenhuma cena ou asset de conteúdo foi alterado.

O pedido contém identidade monotônica, encontro, inimigo, quantidade, região,
distância mínima, prioridade e deadline de tick. Validação rejeita entradas
inválidas sem consumir ID. Fila cheia permite tentar novamente o ID rejeitado.

O diretor valida definições/pools na composição e processa uma vez por tick
simulado. Remove pedidos expirados antes das demais políticas; deadline é
inclusivo. Ordena por prioridade e ID. Orçamento limita trabalho/tentativas e
restante pendente continua sujeito a expiração, sem rajada de recuperação tardia.

Posições vêm de PRNG local por seed, dentro da região, fora do retângulo visível
expandido e respeitando distância ao alvo atual. Ausência de posição válida,
saturação do pool ou limite global reduz o lote e descarta seu restante. Outro
tipo de inimigo pode prosseguir quando somente um pool satura. Não há criação
fora do pool nem coleções novas durante Process.

Limite global conta toda a família Enemy; propriedade das aquisições usa objeto
e lease. Descarte/encerramento devolve somente as próprias ainda válidas, sem
recompensa. Reutilização externa de um mesmo objeto não autoriza devolvê-lo por
um registro antigo. Pausa/seleção preservam pedidos sem avançar processamento.

| Suíte | Aprovados | Falhas esperadas anteriores | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| EditMode completo | 316 | 29 | 0 | 2 |
| PlayMode completo | 107 | 9 | 0 | 2 |

Dez novos EditMode validam o contrato de pedido e entradas inválidas. Quinze novos
PlayMode exercitam pools reais e cobrem limite ativo/global, contagem de instâncias
constante na saturação, prioridade/orçamento/replay, fila cheia, prazo inclusivo,
expiração do restante adiado, múltiplas espécies, geometria/determinismo, região
inviável/distância, pausa/seleção, fim, serviços descartados e lease externo.

Todos os novos casos passaram, assim como os anteriormente aprovados. Falhas
restantes exclusivamente: 18 RunResultTests e 11 SaveServiceTests em EditMode e
nove FrogKingTelegraphTests em PlayMode, pelos tipos ainda ausentes.

Execuções batchmode em Temp/T033Validation; EditMode com nographics, PlayMode
com suporte gráfico. Unity gerou .meta dos scripts/testes e pasta Systems/Spawning,
copiados com GUIDs preservados. Evidências regeneráveis fora do Git:
TestResults/t054-editmode.xml, TestResults/t054-playmode.xml e logs em Logs/.

A área visível é fornecida pelo chamador em coordenadas de mundo; os testes
comprovam exclusão de posições com margem configurável, sem afirmar legibilidade
ou cobertura de silhuetas em câmera real. Produção de pedidos pelo cronograma,
conexão da câmera, scheduler, catálogo e composição da partida permanecem nas
T055/T056/T060. Não há aceite de hordas, plataforma ou desempenho. T055 em diante
permanecem pendentes.
