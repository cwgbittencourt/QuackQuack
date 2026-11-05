# T022 — Prévia de raio e efeitos ativos nas cartas

Data: 05/11/2026. Base: `821c0b9`. FR-006/008, contrato C3.

## Implementação

UpgradeSystem oferece uma projeção hipotética de atributos: copia os bônus
atuais, substitui somente o nível proposto da passiva usando GetModifier e chama
a mesma resolução efetiva usada na partida. Não adiciona itens, altera a build,
emite confirmação ou muda assets. Limites e valores continuam autorados.

UpgradeSelectionBridge entrega raios atual/previsto em UpgradeCardSnapshot.
Sabão é descrito como bônus no raio de Bolhas, sem prometer aumento igual na área.
A prévia considera nível da arma, passivas e relíquias atuais; a ausência de
Bolhas é indicada como “Ao adquirir Bolhas”, com projeção da arma N1. O cache
inclui terreno, referência e Version de relíquias, além da build/oferta existentes.

UpgradeAreaPreview representa os dois raios na mesma escala e mostra seus
valores numéricos. O desenho compara atual/próximo sem usar pixels como regra
de combate. As cartas exibem também os bônus agregados das passivas atuais,
incluindo as duas fontes de dano. Stats de armas usam atributos efetivos com
terreno/relíquias em vez de exibir bases sem modificadores.

O crescimento real de sprite/colisão implementado na T018 continua consumindo
o mesmo HitRadius; seus 19 testes passam nesta rodada. Não foi criada uma segunda
fórmula de Sabão na view. WeaponVisuals contém somente a conversão proporcional
para o diagrama da carta. Prefab atualizado conecta uma preview/effects por slot,
sem imagens capturando raycast nem mudanças no grafo de botões.

## Autoria pelo Unity

`UpgradeSelectionSetup.UpdateAreaPreview` carrega e salva o prefab existente,
adiciona as referências novas e reorganiza o espaço interno das três cartas.
BuildView também inclui essa configuração para futuras gerações. Não foi
executada a recriação legada de cenas/treino.

Gerador executado no Unity 6000.4.3f1, cópia isolada: exit 0. Os 200 objetos
serializados anteriores foram preservados (257 após adições; zero IDs removidos).
GUID do prefab/.meta e referências existentes mantidos. Segunda execução produziu
hash idêntico do prefab, comprovando idempotência. .meta do script novo gerado
pelo Unity e copiado junto ao arquivo. Cenas e dados de balanceamento não mudaram.

## Validação

Antes: UpgradeSelectionTests tinha 23 casos, 18 aprovados e cinco falhas novas,
zero ignorados, exit 2. Falhas: raios ausentes nos dois níveis testados, cenário
sem Bolhas, ausência de efeitos ativos e cache sem invalidação por relíquias.

Depois: rodada focada 46/46 aprovada (27 de seleção e 19 de Bolhas), exit 0.
Novos casos de seleção verificam:

- N0→N1 e N4→N5 de Sabão: 0,2→0,216 e 0,264→0,28 de raio; tamanho proporcional
  do diagrama, nenhuma alteração da build na leitura e confirmação produzindo
  o mesmo raio previsto.
- Projeção sem Bolhas explicitamente prospectiva, sem adquirir a arma.
- Alterar relíquia invalida o snapshot sem alterar oferta: 0,22→0,2376 com +10%
  de área da relíquia aplicada uma vez.
- Duas passivas de dano exibem soma ativa de +16%.
- Quatro proporções com três cartas e seis passivas N4: texto sem truncamento,
  preview dentro da carta e captura renderizada em RenderTexture correspondente
  à resolução testada. Nenhum viewport artificial diferente da captura.

Cinco novos casos EditMode verificam projeção nos níveis 0/4/5, teto, preservação
de dano/aquisição/velocidade e rejeição de níveis -1/6 sem mutar build/cache/assets.

Suítes completas: **584/584 EditMode e 458/458 PlayMode aprovados**, sem falhas ou
ignorados, exit 0 em ambas. Conferir XML local para descoberta por classe.

## Revisão visual e limites

Capturas verificadas em 1920×1080, 2160×1080, 2340×1080 e 1440×1080: títulos,
comparação atual/próximo, círculos, efeitos, sinergia, evolução e ações visíveis,
sem sobreposição/corte nessa composição densa. Foco visível dos botões preservado.
Isso é inspeção de renderização, não aceite artístico ou físico de dispositivos.

Artefatos regeneráveis: XML/logs em TestResults/Polimento002/T022 e
Logs/Polimento002/T022; PNGs em Temp/Polimento002Validation/Logs/Polimento002/T022
(nos players, Application.temporaryCachePath). Não versionados. Comparação de
Assets/QuackQuack com a cópia: somente LakeURP.asset difere pela prefiltragem
regenerável conhecida, não copiada de volta.

Checks de diff para C#/Markdown e prefab (preservando espaços nativos do YAML)
aprovados. Sem extensions.yml ou hooks de implementação. Players IL2CPP e aceite
jogável comparativo não executados nesta task; T023 permanece pendente, assim
como T014/8BitDo-S23 e T075 anteriores. Não há aprovação de performance nesta rodada.
