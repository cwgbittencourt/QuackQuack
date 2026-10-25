# Validação de T024

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T024 concluída. QuackDefinition mantém balanceamento em campos serializados
privados, expostos para leitura. IsValid rejeita valores não finitos em todos os campos
numéricos e retorna diagnóstico sem alterar o conteúdo:

- Capacidade, alcance, cooldown e velocidade de reflexão positivos.
- Custo positivo, menor ou igual à capacidade, compatível com BreathSystem.
- Dano, empurrão, interrupção e recuperação em terra não negativos.
- Recuperação em água maior que em terra; multiplicador de carga cheia maior que um.
- ID não vazio, pela validação herdada de ContentDefinition.

Foi acrescentado reflectionSpeed editável (padrão 7) e CanReflect, que usa a regra
compartilhada do Core: somente projéteis leves e explicitamente refletíveis.
A integração do controlador com esses membros permanece nas tasks posteriores;
PlayerController não foi alterado. Não foram criados assets autorais nem cenas.

QuackDefinitionTests tem 30 casos: valores não finitos, limites inválidos, zeros
permitidos, custo igual à capacidade, ID ausente, quatro combinações de flags de
reflexão e uso dos dados no BreathSystem/QuackSystem. O teste de consumo e acerto
aprimorado verifica dano, empurrão, interrupção, crítico, deduplicação por alvo e
preservação do conteúdo serializado. O .meta do novo script foi gerado pelo Unity.

Etapa vermelha: 57 testes EditMode, 47 aprovados e dez falhas nas validações numéricas
anteriores; código de saída 2. Evidências locais em TestResults/t024-red.xml e
Logs/t024-red.log. Os casos da nova API de reflexão foram acrescentados após essa etapa.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 63 | 0 | 0 |
| PlayMode | 9 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em
Temp/T022Validation, reaproveitada das tasks anteriores, com batchmode, nographics,
runTests e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t024-editmode.xml e TestResults/t024-playmode.xml.
Logs: Logs/t024-editmode.log e Logs/t024-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

A alteração preexistente dos slots vazios de ícones em
ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit da T024.
Nenhum aceite de gameplay, build de plataforma ou desempenho é declarado.
T025 em diante permanecem pendentes.
