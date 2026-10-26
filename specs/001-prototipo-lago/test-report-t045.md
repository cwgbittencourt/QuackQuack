# Validação de T045

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T045 concluída. UpgradeFeedbackController compõe o anel existente do
Quack com estrela dourada/losango roxo, legenda EVOLUÇÃO!/RECOMPENSA RARA e dois
tons temporários distintos. Um wrapper IPoolLifecycle liga conclusão/reset ao
PoolService. O bridge aquece quatro instâncias e dirige animação/áudio centralmente,
sem criar objetos ou AudioClip a cada ativação. Geometria usa pontos pré-calculados.

UpgradeSystem agora publica UpgradeConfirmation depois de uma escolha aceita e
da oferta fechada. O tipo de evolução vem da receita validada; comandos inválidos,
repetidos, melhorias comuns e pulo não produzem feedback de evolução. O bridge
publica o sinal EvolutionObtained e consome somente os eventos de evolução/raro.
O consumidor não reaplica regras nem concede recursos.

ExperiencePickup recebeu InitializeRare/SpawnRare: uma correlação nova por lease,
XP concedido uma vez e devolução ao pool antes do evento RareReward. Reset limpa
raridade, correlação e referências, impedindo herança numa aquisição comum.
O pickup roxo na arena, em (-9, 2), concede 40 XP. Três comuns e a rara totalizam
70 XP; limiares editáveis 10–70 permitem sete subidas. Há cinco pickups ativos ao
iniciar: três XP comuns, um raro e a recompensa de Fôlego original. O teste de
arena foi atualizado para distinguir essa composição.

O bridge deduplica por tipo/correlação, inclusive em replay de evento. A memória
de deduplicação é limitada a 1024 eventos; a fila de notificações durante cartas,
a quatro. Saturação descarta cosméticos, preservando XP/build. Ao sair das cartas,
evolução é priorizada e a fila toca sequencialmente para não sobrepor legendas.
Pausa congela animações/áudio; término e desativação limpam efeitos/fila e removem
assinaturas. Reativação não duplica consumidores. Pool descartado impede aquisições.

UpgradeFeedbackSetup gerou o prefab de efeito, variante de pickup raro, Evolution.wav
e RareReward.wav, e conectou a cena Lake. UpgradeSelectionSetup reconecta esse
feedback ao reconstruir a composição T044. Arquivos serializados e .meta foram
gerados pelo Unity; o GUID fb86267c7de2e40479ccd6bec2e07d8a de Lake foi preservado
e seu .meta comparado antes da cópia. Não houve edição manual de YAML.

Sete testes PlayMode novos verificam evolução aceita/replay sem repetir build,
coleta rara e retorno anterior à notificação, fila durante as cartas, pausa e
limpeza de label/áudio/geometria, saturação sem criação extra nem concessão de XP,
reenable sem assinaturas duplicadas, ausência de falso sinal em melhoria/pulo,
reutilização rara/comum e prioridade sequencial sem sobrepor legendas.
Um teste EditMode adicional verifica que a notificação de confirmação encontra
a oferta já fechada e não pode confirmar novamente por reentrada.

| Suíte final | Aprovados | Falhas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 209 | 0 | 0 | 0 |
| PlayMode | 77 | 0 | 0 | 0 |

Sem erros de compilação, exceções de gameplay ou falhas de regressão. Execução
batchmode/nographics/runTests em Temp/T033Validation. XMLs regeneráveis:
TestResults/t045-editmode.xml e TestResults/t045-playmode.xml, com logs em Logs/.
Geração: executeMethod QuackQuack.Editor.UpgradeFeedbackSetup.Build.

Logs/t045-preview.png foi renderizada com gráficos habilitados e inspecionada em
1280x720 por UpgradeFeedbackSetup.Preview. A comparação preparada no Editor mostra
os dois sinais lado a lado com legendas separadas; o fluxo real da fila é sequencial.
Não é captura de playtest. Os testes verificam clipes atribuídos e amostras válidas,
mas não houve avaliação auditiva humana. Prévia, logs e XMLs ficam fora do Git.

Os limites do pool/fila e o descarte de cosméticos foram verificados funcionalmente;
não houve perfil de CPU/GPU, GC ou memória em hardware alvo. Espaços finais dos
campos vazios serializados permanecem como emitidos pelo Unity; scripts/documentação
passam na verificação de whitespace.

Esta tarefa não aprova balanceamento, agregação de todos os modificadores de
passivas, US2 completa, partida de 15 minutos ou builds/plataformas. Esses aceites
continuam dependentes de integração, convergência e validação próprias. T046 em
diante continuam pendentes.
