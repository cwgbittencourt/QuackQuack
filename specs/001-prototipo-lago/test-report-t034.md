# Validação de T034

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T034 concluída: 20 casos EditMode em UpgradeOfferTests. UpgradeSystem
continua ausente, previsto para T040; não houve implementação de produção.

Os cenários preparam a verificação de ofertas com um, dois, três ou mais IDs
elegíveis, limite de três escolhas distintas, eliminação de duplicatas e IDs
inválidos, ausência de opções sem bloquear a próxima seleção e determinismo por
seed sem mutar a entrada. A entrada dessa fronteira já foi filtrada pelas regras
de conteúdo/build; os testes não validam compatibilidade autoral nem elegibilidade
por slots/nível, que dependem de T036–T040.

Confirmação, rerrolagem, banimento e pulo têm casos de repetição por offerId,
IDs desconhecidos/obsoletos, opções não ofertadas e preservação do estado após
rejeição. Confirmação registra uma escolha; pulo não gasta recursos. Rerrolagem
consome somente uma unidade por oferta e respeita o total de duas. Banimento
consome somente uma unidade, persiste nas ofertas seguintes e encerra a seleção
quando remove a última opção. Ofertas substitutas recebem um ID diferente.

O contrato de teste está em contracts/gameplay-contracts.md. O adaptador de
reflexão exige o tipo real no assembly de produção, sem stub nem Ignore; mantém
o projeto compilável antes da T040. Substituir por chamadas tipadas na T040 e
integrar os recursos à BuildState, evitando contadores independentes em produção.
ConfirmedChoices é o histórico observável da sessão; aplicação à build e eventos
de integração ainda dependem das tarefas posteriores.

| Suíte EditMode | Aprovados | Falhas | Ignorados |
|---|---:|---:|---:|
| Regressão anterior a T033 | 75 | 0 | 0 |
| T033: BuildStateTests | 0 | 23 esperadas | 0 |
| T034: UpgradeOfferTests | 0 | 20 esperadas | 0 |
| Total | 75 | 43 esperadas | 0 |

Código de saída 2. Todas as falhas da T034 vêm do SetUp: `T040 pending: production
UpgradeSystem is missing (T034 red phase).` As 23 da T033 continuam pela ausência
de BuildState. Não houve erro de compilação no log. Essa execução confirma a fase
vermelha e a descoberta dos 20 casos, não a aprovação das asserções comportamentais,
que só serão exercitadas após T040. Não representa aceite de US2.

Execução em Temp/T033Validation (cópia reutilizada sem Assets/_Recovery), batchmode,
nographics, runTests e testPlatform EditMode. Evidências regeneráveis fora do Git:
TestResults/t034-editmode.xml e Logs/t034-editmode.log. O Unity gerou o .meta do
novo script, copiado com seu GUID preservado. git diff --check sem ocorrências.

PlayMode não foi repetido: nenhuma alteração de produção, cenas ou assets de
gameplay. Não foram realizadas builds, avaliação jogável ou medição de desempenho.
Assets/_Recovery e seu .meta preexistentes permanecem fora do commit.
T035 em diante continuam pendentes.
