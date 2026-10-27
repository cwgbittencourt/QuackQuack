# Validação de T048

Data: 2026-10-27. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T048 concluída: SaveServiceTests.cs contém 11 casos RelicLoadoutTests e
11 casos SaveServiceTests. RelicLoadout, RelicDefinition, UI e JsonSaveService
continuam pendentes nas T052/T059; nenhum código de produção foi adicionado.

Cobertura preparada de relíquias:

- Loadout vazio, IDs inválidos/desconhecidos rejeitados e bloqueio de equipamento
  conhecido ainda não desbloqueado.
- Desbloqueio/equipamento repetidos sem duplicação ou efeito adicional.
- Zero a três equipadas; quarta rejeitada preservando estado e efeitos.
- Recomposição de dano a partir da base, reaplicação sem acúmulo, remoção parcial
  preservando os demais efeitos e remoção total restaurando a base.
- Reequipamento, liberação de slot, cópia dos dados e independência de loadouts.

Cobertura preparada de recuperação:

- Ausência de save retorna padrões utilizáveis com versão atual e listas vazias.
- JSON vazio/malformado/null, objeto sem schema e versão negativa, com backup
  inválido, retornam padrões e diagnóstico.
- Principal inválido recupera backup válido; principal válido tem precedência.
- IDs desconhecidos/duplicados, equipamento bloqueado e excesso de três são
  filtrados com diagnóstico, preservando ordem dos primeiros elegíveis.
- Coleções null são recuperadas e Load repetido não duplica entradas.

As fronteiras propostas estão em contracts/gameplay-contracts.md. Dez IDs e
bônus percentuais de dano são fixtures; autoria das dez relíquias e demais efeitos
continuam na T052. Migração, escrita atômica e integração de progresso são da T059.
Testes de arquivo usam caminho temporário exclusivo com GUID e limpeza restrita
ao diretório validado; não acessam save pessoal. Nesta fase, o SetUp falha antes
de criar esses arquivos, pela ausência dos tipos reais.

Reflexão somente nos testes mantém a compilação sem classes futuras, exigidas
explicitamente no SetUp. Não há implementação falsa ou testes ignorados. Migrar
para chamadas tipadas ao implementar T052/T059.

| Suíte | Aprovados | Falhas T046 | Falhas T047 | Falhas T048 | Ignorados | Saída Unity |
|---|---:|---:|---:|---:|---:|---:|
| EditMode | 209 | 12 | 18 | 22 | 0 | 2 |

As 22 novas falhas são exclusivamente `T052 pending: production RelicLoadout is
missing (T048 red phase).` e `T059 pending: production JsonSaveService is missing
(T048 red phase).` As 30 falhas anteriores permanecem esperadas. Nenhum caso
anteriormente aprovado regrediu. Esta execução confirma descoberta e fase vermelha;
as asserções comportamentais precisam ser exercitadas após implementar as classes.

Execução batchmode/nographics em Temp/T033Validation. A primeira tentativa apontou
uma asserção não disponível na versão NUnit do projeto; corrigida para asserção
compatível antes da execução final dos 261 casos. O Unity gerou o .meta do script,
copiado preservando o GUID. Evidências regeneráveis fora do Git:
TestResults/t048-editmode.xml e Logs/t048-editmode.log.

PlayMode não foi repetido: alteração restrita a testes EditMode e documentação.
Nenhum aceite de gameplay, plataforma, persistência em dispositivo ou desempenho
foi realizado. T049 em diante permanecem pendentes.
