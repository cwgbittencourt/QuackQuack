# T026 — Política autoral de pickups e XP positivo

Data: 05/11/2026. Base: `29a923e`. Escopo: dados/validação/geração no Unity, sem iniciar integração de pools, pickups ou RunDirector da T027.

## Dados e integração de autoria

`PickupPolicyDefinition` deriva de ContentDefinition e valida chance finita em [0,1], frações em (0,1], multiplicador de captura positivo e velocidade de atração maior que um. Asset `Content/Pickups/LakePickupPolicy.asset`:

| Campo | Valor autorado |
|---|---|
| CommonHealChance | 0,10 |
| CommonHealFraction | 0,15 |
| MiniBossHealFraction | 0,50 |
| CaptureBodyRadiusMultiplier | 3 |
| AttractionWaterSpeedMultiplier | 1,25 |

`CreateLedger` projeta a política no Core sem mutar o asset. Na criação da partida, converte a chance float pelo texto round-trip com cultura invariante para a precisão double do RNG. Um cast direto faria 0,10 virar aproximadamente 0,10000000149 e aceitaria indevidamente um sorteio igual a 0,10. Não usa epsilon; também preserva probabilidades positivas muito pequenas. Conversão/alocação ocorre na criação do ledger, não por morte.

`LakeRunSettings` agora exige referência válida à política e `BossExperience > 0`. `EnemyDefinition` exige XP positivo; moedas podem continuar zero. O validador de conteúdo reconhece a política e reporta referência ausente/inválida, XP de inimigo não positivo e XP de chefe não positivo com asset/propriedade identificados.

### Revisão de dados do chefe

Não existia campo de XP do chefe; somente BossCoins/VictoryBonus, que não são XP. Foi autorado `BossExperience = 50` no asset de configurações. Escolha de autoria desta task: reutilizar o patamar de 50 XP já presente em ambos minibosses, sem criar um novo patamar de balanceamento. É um valor inicial editável, não um número deduzido de moedas nem uma nova aprovação de balanceamento pelo usuário. Inimigos comuns mantêm XP 2/3/4/5/6, minibosses 50/50. Nenhum controlador recebeu constante de XP; a emissão/liquidação do chefe permanece T030.

`PickupPolicySetup.Build` gera/carrega a política canônica, escreve os valores aprovados via SerializedObject e vincula as configurações existentes. Usa GUIDs existentes ao regerar, preserva BossExperience positivo já autorado e preenche 50 quando ausente/não positivo. Como nos geradores do protótipo, executar Build restaura os padrões da política canônica. `LakeRunSetup` também configura a referência ao gerar configurações novas, evitando que uma regeneração futura produza settings inválidos.

## Verificação

Unity 6000.4.3f1, cópia isolada `Temp/Polimento002Validation`, Win64. Assets/.meta gerados pelo Unity; nenhuma edição manual de YAML. Duas execuções de geração produziram os mesmos hashes da política, seu .meta e LakeRunSettings; GUIDs preservados. Somente os dois campos novos foram acrescentados ao asset existente de settings.

Validação completa: **66 assets, zero erros e dois avisos existentes** de tipos URP sem validador de domínio (LakeRenderer2D/LakeURP). A política nova tem validador registrado e não gera UnsupportedType. Marcadores locais `T026_GENERATION_IDEMPOTENT_OK`, `T026_PICKUP_POLICY_OK` e `T073_CONTENT_VALIDATION_OK` conferidos.

41 casos novos em `PickupPolicyDefinitionTests`: limites/não finitos dos cinco campos, chance 0/1/subnormal, fronteira double imediatamente abaixo/igual/acima de 0,10 após leitura do float autorado, valores e referências gerados, XP positivo do chefe/inimigo, política ausente/inválida no settings, frações editáveis transmitidas ao ledger e ausência de mutação do asset. O contrato legado que permitia XP zero foi atualizado para exigir XP positivo mantendo moedas zero válidas.

| Execução | Resultado |
|---|---|
| Primeira EditMode | 677 casos, 676 aprovados, uma falha no contrato legado de XP zero; zero ignorados |
| EditMode após revisão do contrato | 677/677 aprovados, zero falhas/ignorados, código 0 |
| PlayMode | 462/462 aprovados, zero falhas/ignorados, código 0 |

Comparação de todos os arquivos de Assets/QuackQuack entre raiz e cópia validada: somente LakeURP.asset difere pela prefiltragem regenerável já conhecida, sem copiar essa alteração. Scripts, assets e metas da entrega correspondem aos usados nos testes.

Resultados locais em `TestResults/Polimento002/T026`; logs em `Logs/Polimento002/T026`. Arquivos regeneráveis não versionados.

## Limites

Não conecta a política à emissão real, não remove SuppressedPickups, não implementa atração/cura visual nem concede XP de chefe no fim. Essas integrações e o dimensionamento de capacidade continuam em T027–T032. Não houve build, aceite físico ou profiling nesta task de dados. T014 continua pendente; T027 não foi iniciada.
