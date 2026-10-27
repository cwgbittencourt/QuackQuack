# Validação de T049

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T049 concluída: nove casos PlayMode em FrogKingTelegraphTests. Nenhum
controlador, prefab, áudio autoral ou comportamento do Sapo-Rei foi implementado.
T057 continua pendente.

Cobertura preparada:

- Salto, onda, girinos e destruição de área segura mostram marcação com geometria,
  largura e opacidade positivas e AudioSource ativo antes da execução.
- DangerTelegraphed tem prioridade Danger, posição alvo, correlação positiva e
  duração que cobre a antecedência. AttackCommitted usa a mesma correlação.
- Janela de aviso sem evento de combate ou execução antecipada; um frame real
  entre preparação e avanço manual; execução única ao cruzar o prazo.
- Pedido concorrente não substitui pendente nem duplica aviso.
- Vitória, morte e abandono cancelam sinais/ataque pendente e rejeitam novo pedido.
- Desativação limpa sinais e reativação não executa ataque antigo.

Contrato proposto registrado em contracts/gameplay-contracts.md. A fixture cria
componentes Unity reais e um tom não silencioso somente para teste. Antecedência
de 0,5 segundo é injetada, sem fixar balanceamento. A entrada explícita de cada
padrão serve à composição determinística; fases e seleção automática são da T057.

Reflexão temporária exige o MonoBehaviour real no SetUp, mantendo compilação
antes da implementação sem criar substitutos. Migrar para chamadas tipadas na T057.

| Suíte | Aprovados | Falhas esperadas T049 | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|
| PlayMode | 77 | 9 | 0 | 2 |

Todas as nove falhas ocorrem no SetUp: `T057 pending: production FrogKingController
is missing (T049 red phase).` Os 77 casos existentes passaram. Esta execução
confirma descoberta e fase vermelha; as asserções de sinal/ordenação ainda precisam
ser exercitadas após implementar o chefe. Não há aceite de áudio, legibilidade,
dano, fases, pooling ou justiça do combate com base nesta execução.

Executado em Temp/T033Validation com batchmode, runTests e testPlatform PlayMode,
com suporte gráfico (sem nographics). Unity gerou o .meta do script, copiado com
GUID preservado. Evidências regeneráveis fora do Git:
TestResults/t049-playmode.xml e Logs/t049-playmode.log.

EditMode não foi repetido: não houve mudança de produção ou dos testes EditMode.
Último resultado T048: 209 aprovados e 52 falhas esperadas das T046–T048. Não houve
build, playtest humano ou medição em dispositivo. T050 em diante permanecem pendentes.
