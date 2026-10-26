# Validação de T040

Data: 2026-10-26. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T040 concluída. UpgradeSystem implementa geração determinística de uma a
três escolhas distintas, rerrolagem, banimento persistente na sessão, pulo e
confirmação idempotentes por offerId. Snapshots de ofertas e histórico são somente
de leitura. Ofertas vazias, repetidas, encerradas ou obsoletas não reaplicam ações.

Os recursos vêm da mesma BuildState da sessão: duas rerrolagens e um banimento.
Rerrolar/banir publicam novos IDs e invalidam o anterior. Banir a última opção
encerra a seleção sem deixar oferta vazia aberta. Confirmar e pular fecham a oferta
antes de notificar consumidores. Uma oferta aberta não é substituída por outra
requisição externa. O gerador copia a entrada e usa PRNG inteiro por seed.

O construtor de IDs pré-filtrados preserva a fronteira da T034, resolvendo seleção
e histórico sem aplicar conteúdo desconhecido. O construtor com BuildState e
catálogos tipados valida conteúdo e filtra aquisições sem slot, níveis máximos,
armas evoluídas e receitas sem requisitos. Confirmar revalida e aplica a mudança
uma vez à build, inclusive a substituição por evolução com seis slots ocupados.
IDs de escolhas são únicos entre categorias. Formas evoluídas não são oferecidas
como aquisições comuns. O catálogo autoral do protótipo continua na T043.

BindRun integra ChoosingUpgrade/Playing, rejeita comandos em fases incompatíveis
e não retoma partidas terminais. OfferChanged e OfferResolved notificam estados
já aceitos; consumidores não reaplicam regras. O encadeamento por XP e sua composição
normal ficam na T041. Nenhuma coleta, curva de XP ou cena foi implementada aqui.

Os 20 testes T034 foram migrados de reflexão para chamadas tipadas. Seis novos
testes com ScriptableObjects reais cobrem aplicação única à build, filtragem por
slots/níveis, evolução, confirmação obsoleta após mudança externa, recursos
compartilhados, coleções protegidas, transições/notificação únicas e abandono.

| Suíte | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode final | 183 | 0 | 0 |
| PlayMode | 34 | 6 esperadas de T035 | 2 |

As 20 falhas esperadas da T034 foram resolvidas. As seis PlayMode agora falham
explicitamente no pré-requisito ExperiencePickup ausente (T041), depois de
confirmarem a existência de UpgradeSystem. As asserções de XP ainda não foram
exercitadas. Nenhum erro de compilação ou nova falha de regressão.

Execução batchmode/nographics/runTests em Temp/T033Validation, plataformas de teste
EditMode e PlayMode. Evidências regeneráveis fora do Git: TestResults/t040-editmode.xml,
TestResults/t040-playmode.xml e logs correspondentes em Logs/. A fase vermelha das
ofertas está registrada em T034 e nas suítes até T039.

O Unity gerou os .meta da pasta, script e teste, copiados com GUIDs preservados.
Campos vazios de .meta preservam os espaços gerados pelo Unity; scripts e
documentação passam na verificação de whitespace.
Não há aceite de US2 jogável, cartas visuais, builds, plataformas ou desempenho.
T041 em diante continuam pendentes.
