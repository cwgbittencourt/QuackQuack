# Validação de T025

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T025 concluída. A implementação preexistente de BreathSystem foi revisada e
atende às regras: reserva inicialmente cheia; consumo válido limitado a zero; ativação
parcial aceita enquanto houver reserva; rejeição da próxima ativação com reserva vazia;
estado aprimorado calculado antes do consumo; recuperação por segundos de simulação e
terreno, limitada à capacidade. Água recupera mais rápido que terra. Entradas inválidas
não alteram estado. O sistema não consulta relógio, dispositivo, cena ou apresentação.

Não foi necessária mudança comportamental. Foram documentados os contratos de consumo
e recuperação no código e acrescentados seis casos em BreathStateTests:

- Consumo cheio aprimorado, consumo parcial normal e retorno ao aprimorado após recuperação.
- Recuperação proporcional ao tempo em terra e água, incluindo saturação com tempo extremo.
- Tempo negativo/não finito e terreno inválido preservam reserva parcial.
- Tempo zero e recuperação terrestre zero não alteram reserva.
- Duas execuções dos mesmos 200 passos produzem os mesmos resultados a cada passo.

Os testes existentes continuam cobrindo capacidades e custos inválidos, ativação parcial
até zero, rejeição com reserva vazia e ciclos repetidos de consumo/recuperação.
Não há nova etapa vermelha: os testes adicionais passaram sobre a lógica já existente;
nenhuma falha artificial foi introduzida. A etapa vermelha original de T018 está descrita
no relatório T018–T021. Repetibilidade aqui significa mesmas entradas e sequência no mesmo
ambiente; não é evidência de equivalência bit a bit entre plataformas distintas.

| Suíte | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 69 | 0 | 0 |
| PlayMode | 9 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em
Temp/T022Validation, reaproveitada das tasks anteriores, com batchmode, nographics,
runTests e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t025-editmode.xml e TestResults/t025-playmode.xml.
Logs: Logs/t025-editmode.log e Logs/t025-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena, asset serializado ou .meta foi alterado. A alteração preexistente de
ícones em ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
Não houve aceite de gameplay, builds ou desempenho. T026 em diante permanecem pendentes.
