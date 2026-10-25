# Validação de T026

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T026 concluída. QuackSystem mantém resolução por alvo e onda, com dano,
empurrão e interrupção multiplicados na carga cheia. Interrupção exige alvo
interrompível; projéteis não recebem dano, empurrão, interrupção ou crítico.
A elegibilidade de reflexão continua sendo a conjunção de leve e refletível.

BeginWave exige identificador positivo e crescente; repetir o identificador atual é
idempotente e não libera novos acertos. Ondas antigas são rejeitadas. Uma nova onda
limpa acertos, colisões e emissões anteriores. TryHit exige onda iniciada e balanceamento
finito e válido; rejeita também resultados multiplicados não finitos, antes de reservar
o alvo. TryCollision exige um acerto anterior em alvo não projetil e aceita uma vez.

O evento Resolved expõe CombatEvent. TryEmitHitEvents emite DamageApplied e, quando
aplicável, CriticalHit e EffectApplied (quantidade em segundos de interrupção); para
projétil, emite apenas ProjectileReflected. TryEmitCollisionEvent exige colisão aceita.
Cada grupo é emitido no máximo uma vez por alvo/onda, com tick, correlação, fonte,
alvo e posição. A reserva da emissão ocorre antes de notificar assinantes.

O chamador deve primeiro aplicar o acerto ou confirmar a reflexão elegível no estado
runtime, e só então chamar TryEmitHitEvents. Para colisão, deve confirmar a física,
aceitar TryCollision e solicitar a emissão. A conexão de Resolved com o event bus e
a retirada das publicações equivalentes do PlayerController ficam para sua integração
em T028; o controlador não foi alterado nesta task. Não usar simultaneamente os dois
caminhos de publicação para a mesma mudança.

Foram adicionados seis testes em QuackRulesTests. Etapa vermelha: 72 testes EditMode,
69 aprovados e três falhas (reinício da mesma onda, colisão sem acerto e dano NaN);
código de saída 2. Os testes adicionais da emissão verificam tipos, quantidades,
metadados, ausência de duplicação, nova onda e tratamento de projéteis.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 9 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em
Temp/T022Validation, reaproveitada das tasks anteriores, com batchmode, nographics,
runTests e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t026-red.xml, TestResults/t026-editmode.xml e TestResults/t026-playmode.xml.
Logs: Logs/t026-red.log, Logs/t026-editmode.log e Logs/t026-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena, asset serializado ou .meta foi alterado. A alteração preexistente dos
ícones em ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
Não houve aceite de gameplay, builds ou desempenho. T027 em diante permanecem pendentes.
