# Validação de T033

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T033 concluída: 23 casos EditMode em BuildStateTests. Não foi implementado
BuildState, previsto para T039, nem o fluxo de ofertas de T034/T040.

Cobertura preparada:

- Seis slots independentes por família, rejeição da sétima aquisição sem mutação,
  duplicatas e IDs inválidos; novas aquisições no nível um.
- Melhoria de arma já possuída com slots cheios até nível cinco e rejeição além
  do limite ou para arma ausente.
- Duas rerrolagens e um banimento, rejeição repetida após esgotar, independência
  dos recursos e isolamento entre partidas.
- Evolução em capacidade máxima substitui a base, preserva outras armas e passivas,
  e rejeita repetição. Nível insuficiente, falta de base/passiva e resultado inválido
  ou já possuído não alteram parcialmente o inventário nem consomem recursos.

O contrato foi registrado em contracts/gameplay-contracts.md. Um adaptador de
reflexão restrito aos testes permite compilar antes de existir a classe de produção.
O SetUp exige explicitamente o tipo real; não há implementação falsa nem testes
ignorados. Na T039, substituir o adaptador por chamadas tipadas. Validação das
combinações autorais e idempotência de ofertas ficam nas respectivas tarefas.

| Suíte | Aprovados | Falhas esperadas | Ignorados | Código de saída |
|---|---:|---:|---:|---:|
| EditMode | 75 | 23 | 0 | 2 |

Todas as 23 falhas apresentam a mesma causa: `T039 pending: production BuildState
is missing (T033 red phase).` Os testes existentes passaram. Essa execução confirma
a fase vermelha e a descoberta dos cenários; as asserções comportamentais ainda
precisam ser exercitadas após a implementação da T039. Não há aceite de US2.

Execução em Temp/T033Validation, cópia isolada sem Assets/_Recovery, com batchmode,
nographics, runTests e testPlatform EditMode. A importação inicial registrou erros
transitórios de GUID em pacotes, seguidos de compilação bem-sucedida e execução dos
98 casos; uma segunda execução com cache aquecido confirma o resultado.
O .meta do novo script foi gerado pelo Unity e copiado com seu GUID preservado.

Evidências regeneráveis, fora do Git: TestResults/t033-editmode.xml,
TestResults/t033-editmode-confirm.xml e respectivos logs em Logs/.
PlayMode não foi repetido: não houve alteração de produção, cena ou asset de gameplay.
Nenhuma build, avaliação de gameplay ou medição de desempenho foi realizada.
Assets/_Recovery e seu .meta preexistentes permanecem fora do commit.
T034 em diante continuam pendentes.
