# Validação de T023

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T023 concluída. CharacterDefinition mantém campos serializados privados e
propriedades de leitura para os dados autorais. IsValid exige ID canônico `pato`, vida
e velocidades finitas e positivas, água mais rápida que terra, invulnerabilidade finita
não negativa e referência de Quack presente. Falhas retornam diagnóstico sem alterar dados.

IsValidSelection valida o conjunto completo de opções: exatamente um Pato válido,
sem referência ausente ou ID duplicado. A unicidade pertence ao conjunto recebido,
não a um registro global de objetos carregados. O futuro consumidor da seleção deve
usar esse método; o validador global de assets permanece em T073. Não foram criados
assets de personagem, catálogo, tela de seleção ou implementação de Quack de T024.

CharacterDefinitionTests tem 12 casos EditMode cobrindo dados válidos, IDs vazios e
não canônicos, valores negativos/não finitos, zeros, vantagem de velocidade na água,
referência ausente e seleção vazia/nula/com duplicatas (inclusive assets distintos).
O .meta do novo script foi gerado pelo Unity na cópia de validação e preservado no projeto.

Etapa vermelha: 32 testes EditMode, 28 aprovados e quatro falhas nas validações
numéricas da implementação anterior; código de saída 2. Evidência local:
TestResults/t023-red.xml e Logs/t023-red.log.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 33 | 0 | 0 |
| PlayMode | 9 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em
Temp/T022Validation, reaproveitada da validação anterior, com batchmode, nographics,
runTests e testPlatform EditMode/PlayMode. A execução usou acesso à licença local
fora do sandbox. XMLs: TestResults/t023-editmode.xml e TestResults/t023-playmode.xml.
Logs: Logs/t023-editmode.log e Logs/t023-playmode.log. Cópia, logs e resultados
regeneráveis são ignorados pelo Git.

Nenhuma cena ou asset de conteúdo serializado foi alterado. A alteração preexistente
em ProjectSettings/ProjectSettings.asset foi preservada. Estes testes não representam
aceite de gameplay, builds ou desempenho. T024 em diante permanecem pendentes.
