# Validação de T042

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T042 concluída. Fonte dos seis nomes: página 6 de
documentos/CEMG-Games_Quack-quack_Survive_Ideias_Funcionais.pdf, consultada por extração
de texto com pypdf. O escopo de três evoluções continua o da especificação ativa;
não foram acrescentadas as demais evoluções sugeridas no documento original.

WeaponRunner compõe padrões de ataque e movimentos de WeaponProjectile:
Bumerangue retorna à origem móvel; Ovos deixam armadilhas explosivas; Quack frontal
atinge em cone sem consumir Fôlego; Aura de energia pulsa ao redor da origem;
Foguetes perseguem e explodem no contato; Bolhas atravessam alvos em linha reta.
São comportamentos iniciais verificáveis; balanceamento/conteúdo autoral fica na T043.

WeaponDefinition recebeu comportamento, velocidade/duração do projétil, raio de
colisão, velocidade angular, cone/dispersão e sprite opcional. Valores são editáveis
e validados contra números inválidos e overflow de distância. Armas de projéteis
exigem quantidade positiva; armas de área podem usar zero. Os cinco níveis e a
afinidade autoral permanecem intactos, com os parâmetros efetivos capturados no tiro.

O runner consulta a build somente quando muda seu histórico, mantém até seis
slots e atualiza centralmente os projéteis, sem Update por objeto. O aquecimento
do PoolService é o único local de criação; disparos adquirem/devolvem instâncias.
Saturação reduz o número lançado e limita inclusive uma quantidade autoral muito
grande ao espaço disponível, sem objetos adicionais nem loop por disparos descartados.
Reset limpa referências, sprite, tempo e acertos, permitindo reutilização.

Colisões varridas impedem atravessar obstáculos; áreas usam linha de visão. Alvos
hostis marcados como projéteis não recebem dano dessas armas. Dano/derrota emitem
eventos únicos de mudanças aceitas, atribuídos à definição da arma. O runner não
devolve inimigos, deixando recompensa/ciclo de vida para seus sistemas. Pausa e
seleção congelam recargas/voos; término ou desativação devolve todos os projéteis.
Encerrar a partida em callback interrompe acertos de área e ataques restantes.

Buffers de consulta têm 128 contatos e cada projétil memoriza até 512 IDs de alvos.
Esses limites são explícitos: alvos além dos buffers podem não ser processados
naquela consulta. Não foi medido GC/frame time nem validada a escala de 400 inimigos.

19 testes PlayMode novos cobrem os seis padrões, bloqueio por obstáculos para
todos, retorno do bumerangue sem acerto repetido, saturação/expiração/reset,
pausa/seleção/término, cone versus aura, afinidade sem mutar asset, derrota única,
exclusão de projéteis hostis, desativação e encerramento durante callback.
Seis casos EditMode adicionais validam os novos parâmetros e comportamento;
o teste existente de arma sem projétil foi especializado para Aura de energia.

Primeira execução dos 18 cenários iniciais: 17 passaram e um falhou porque a
fixture fotografava o asset antes de selecionar o comportamento. Essa comparação
foi corrigida; não se tratava de falha da regra de dano. Um caso adicional foi
incluído para encerramento durante callback de combate.

| Suíte final | Aprovados | Falhas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 197 | 0 | 0 | 0 |
| PlayMode | 62 | 0 | 0 | 0 |

Execução batchmode/nographics/runTests em Temp/T033Validation. XMLs regeneráveis:
TestResults/t042-red.xml (primeira execução), TestResults/t042-editmode.xml e
TestResults/t042-playmode.xml, com logs correspondentes em Logs/. Sem erros de
compilação ou falhas de regressão. Evidências regeneráveis ficam fora do Git.
O Unity gerou .meta de Gameplay/Weapons, scripts e teste, com GUIDs preservados.
Campos vazios de .meta mantêm espaços emitidos pelo Unity; scripts/documentação
passam na verificação de whitespace.

Nenhuma cena/prefab foi editada e não foram criados os seis assets autorais nem
suas passivas/evoluções, pertencentes à T043. Os testes compõem os componentes
reais em GameObjects isolados; não houve avaliação visual/auditiva ou playtest.
Bootstrap normal continua a arena US1, sem as seis armas equipadas. Cartas e
integração visual ficam nas tarefas seguintes. Não há aceite de US2 completa,
builds, plataformas ou desempenho. T043 em diante continuam pendentes.
