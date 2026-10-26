# Validação de T035

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T035 concluída: seis testes PlayMode em UpgradeFlowTests, preparando a
integração de XP, seleção e retorno ao combate. Nenhuma classe de produção,
cena ou asset de gameplay foi alterado. UpgradeSystem e ExperiencePickup estão
ausentes e continuam previstos para T040 e T041.

Os cenários usam os componentes reais quando disponíveis: carregam Bootstrap/Lake,
obtêm o PlayerController e RunState da arena e criam uma sessão de progressão com
limiares de XP injetados. Um PoolService pré-aquece um único ExperiencePickup real;
a coleta deve ocorrer por sobreposição física de Collider2D, não por simular o
resultado nem chamar diretamente um callback de trigger.

Cobertura preparada:

- XP abaixo do limiar concedido uma vez, retorno ao pool e reutilização da mesma
  instância sem repetir a recompensa anterior.
- ChoosingUpgrade congela posição do jogador e dos alvos, vida, recuperação de
  Fôlego e relógio durante frames reais; Quack é rejeitado.
- Confirmação registra uma escolha e retoma Playing uma única vez; repetição de
  callbacks de confirmar/pular não duplica histórico nem transições.
- Escolha inválida mantém combate congelado; pulo válido retoma uma vez, sem
  escolher carta ou gastar rerrolagem/banimento.
- Uma coleta que cruza dois limiares produz ofertas distintas e mantém o combate
  congelado entre escolhas, retornando somente depois da última resolução.
- Abandono durante a seleção impede que callbacks atrasados concedam escolha ou
  retomem a partida.

O contrato está em contracts/gameplay-contracts.md. Reflexão está restrita à
fronteira dos testes para compilar antes das classes de produção; não há stubs
nem Ignore. Na T040/T041, trocar por chamadas tipadas e ligar a composição normal
aos mesmos serviços. A fixture limpa assinaturas, sessão, pool e cena e reinicia
os contadores entre casos. As contagens observam RunState.Changed e o histórico
ConfirmedChoices; não constituem validação de eventos de evolução/VFX da T045.

| Suíte PlayMode | Aprovados | Falhas esperadas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| Regressão e T035 | 34 | 6 | 0 | 2 |

As seis falhas vêm do UnitySetUp: `T040 pending: UpgradeSystem is missing (T035 red
phase).` O próximo pré-requisito testado é ExperiencePickup. Não houve erro de
compilação no log. A execução confirma descoberta e fase vermelha dos cenários;
as interações físicas e as asserções comportamentais ainda não foram exercitadas
e devem ser validadas nas T040–T041. Não há aceite de progressão nem de US2.

Execução em Temp/T033Validation (cópia isolada reutilizada), batchmode, nographics,
runTests e testPlatform PlayMode. Evidências regeneráveis fora do Git:
TestResults/t035-playmode.xml e Logs/t035-playmode.log. O Unity gerou o .meta do
script, preservado ao copiar ao projeto. git diff --check sem ocorrências.

EditMode não foi repetido, pois nenhuma regra ou arquivo dessa suíte mudou; a
última execução T034 permanece com 75 aprovados e 43 falhas esperadas (T033/T034).
Não foram realizadas builds, avaliação jogável humana ou medição de desempenho.
T036 em diante continuam pendentes.
