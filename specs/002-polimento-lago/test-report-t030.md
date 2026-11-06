# T030 — emissão e encerramento de recompensas

Data: 05/11/2026. Base: 55e64d1. Escopo limitado à T030.

## Regra aprovada

O usuário aprovou “Remover o XP do chefe (recomendado)”. Chefe final não gera XP nem cura, mantendo moedas, bônus de vitória e registros uma vez. Removidos BossExperience, autoria padrão e validação desse campo. LakeRunSettings.asset foi reserializado pelo Unity 6000.4.3f1; GUID preservado. Relatórios anteriores permanecem históricos.

## Implementação

RunDirector resolve origem, XP e moedas das derrotas comuns/minibosses pelo catálogo, sem confiar em quantidades da notificação. O ledger rejeita FinalBoss sem registrar obrigação nem consumir RNG. Capacidade atual: 3.323 derrotas elegíveis, 6.646 registros; emissão aquecida permanece sem alocação no trecho medido.

No término, RewardLedger.Close cancela registros não coletados e esvazia pendências antes da devolução das views. Claimed/Discarded são preservados. Close é idempotente e impede materialização, coleta e reenfileiramento posteriores. O resumo registra totais e XP cancelado sem concedê-lo. Fechamento durante callback de coleta preserva a conclusão aceita ou cancela a rejeitada, sem duplicação.

A derrota real do chefe é validada por identidade, vida e fase, e registrada uma vez antes de Won. Evento falso enquanto vivo não recompensa. Se um callback pausa durante a derrota, a retomada conclui a vitória sem republicar morte nem conceder XP/ofertas.

## Validação

Executada em Builds/Polimento002Validation, cópia isolada ignorada. Assets, Packages e ProjectSettings próprios; Editor principal preservado. A primeira execução ficou presa no acesso à licença; foi encerrado somente o processo de validação e refeito com acesso à licença local.

- EditMode: **686/686**, zero falhas/ignorados.
- PlayMode completo: **499/499**, zero falhas/ignorados.
- Reteste focado final RunDirectorFlowTests: **35/35**, zero falhas/ignorados, incluindo asserções adicionais de moedas/bônus.
- RefreshRewardSettings: código 0 e T030_REWARD_SETTINGS_OK.

Casos incluem fechamento/reentrada, ausência de pickups do chefe com payload zero/positivo, capacidade integral, vitória com pool de XP saturado, derrota/abandono sem coleta automática, notificação duplicada, eventos falsos e pausa na morte do chefe. A rodada anterior ao ajuste da regra do chefe não representa o resultado final desta entrega.

XMLs/logs regeneráveis ficam em TestResults/Polimento002/T030 e Logs/Polimento002/T030, fora do commit. Não foram repetidas builds nativas nem validações físicas: esta task verifica regras e integração no Editor; aceite jogável/plataforma/desempenho continua nas tasks específicas. T031 não executada; T014 continua pendente conforme decisão anterior.
