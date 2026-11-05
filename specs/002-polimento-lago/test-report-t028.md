# T028 — Views pooled de cura e ciclo dos pickups

Data: 05/11/2026. Base: `f112df2`. Escopo: view/contato/reset, integração do pool de cura e geração de prefabs. Não implementa deslocamento de atração da T029 nem a finalização de recompensas do chefe da T030.

## Entrega

`HealingPickup` exige uma obrigação de cura válida, jogador/partida compartilhados e aquisição do pool dono. O contato usa o jogador vinculado, verifica simulação e aplica a fração pelo ledger/PlayerState real. Vida cheia, pausa e morte preservam o pickup e não consomem claim; contato Stay pode coletar quando voltar a existir vida faltante. Não reanima nem aplica duas vezes.

XP e cura expõem ViewState, BoundRewardId, ViewLeaseId e AttractionTarget. A aquisição inicia Due; inicialização/materialização produz Grounded. TryMarkAttracted marca o ledger e vincula alvo somente quando permitido; não move o objeto nem testa o raio de captura — esses comportamentos são T029. Claim positivo é retido como Claimed no ledger e seguido de Released na view. Isso conserva a prova de concessão única mesmo depois que o objeto físico foi devolvido ao pool. WaitingForView pertence ao registro lógico sem view.

Release/reset devolve obrigação não coletada (Grounded ou Attracted) à fila, limpa identidade, alvo, consumidor, partida/jogador, dono do pool e flags. A lease ativa volta a zero; o contador privado de aquisições permanece monotônico para distinguir reuso do mesmo objeto. Prefab/arte/escala autorados não são destruídos nem reconstruídos. Não há Instantiate/Destroy, novo material ou Update por pickup no loop de coleta.

O callback de XP pode abrir modal ou liberar sua própria view. O ledger protege claim em andamento e, se o consumidor rejeitar após a liberação, reencaminha a obrigação sem perda. A view captura pool/lease antes do callback e só altera/libera a aquisição que iniciou a coleta, evitando acesso ao dono já limpo e interferência em uma nova aquisição. O caminho de treino/rare sem ledger permanece disponível.

## Pool real e dados

RunDirector instala o materializador de cura real no adaptador da T027. Pool compartilhado pré-aquecido de **16 views**, com **duas vagas reservadas para minibosses**, em campos editáveis de LakeRunSettings. Esses são valores técnicos iniciais, não um limite de chance nem aceite de desempenho. Comuns ocupam no máximo 14 vagas; curas excedentes continuam no ledger e minibosses têm prioridade. Liberação de slot permite nova materialização na simulação seguinte. Pausa impede materialização e coleta.

`HealingPrefab`, capacidade e reserva são validados na composição. Os dois campos de capacidade devem satisfazer capacidade > 0 e reserva em [0,capacidade]. XP continua usando o pool autorado de 256 views. No encerramento, o diretor devolve também as views de cura; o destino terminal completo das pendências permanece T030, sem concessão automática de itens de chão.

## Prefabs gerados pelo Unity

`PickupPolicySetup.BuildPickups` carrega/salva o prefab de XP existente, preservando GUID/componentes e configurando layer/trigger. Gera HealingPickup.prefab somente se ausente, com CircleCollider2D trigger na mesma layer de pickups, círculo verde e cruz branca feita de dois sprites Square existentes, material unlit existente. A cruz distingue cura de XP além da cor; arte temporária, sem representar aprovação da direção artística futura.

O gerador vincula HealingPrefab no LakeRunSettings; Configure também mantém a composição válida ao regerar o Lago. Duas execuções geraram os mesmos bytes de ambos prefabs, metas e settings. O .meta existente de XP foi preservado, e a nova meta de cura/script foi produzida pelo Unity. Não houve edição manual de YAML ou alteração de cenas.

## Testes e resultados

- EditMode: composição inválida de pool/reserva, referências/collider/visuais do prefab gerado; release dentro do consumidor de XP, tanto aceito quanto rejeitado; testes anteriores de cura, chance/ledger e capacidade autoral inteira continuam na suíte.
- PlayMode: cura comum 15% e miniboss 50% por contato, claim único, vida cheia/pausa/morte preservadas, coleta posterior por Stay, reuso Grounded/Attracted/Released com alvo limpo para XP/cura, reserva real para minibosses sem criar views além da capacidade e liberação de view dentro do callback de XP. Usa as regras/pools/UpgradeSystem reais; callbacks tardios de objeto inativo são invocados diretamente, pois Unity não entrega SendMessage a objetos inativos.
- O teste de prioridade do adaptador T027 desativa explicitamente o novo materializador real antes de usar sua probe de pool, preservando a finalidade daquele teste. A reserva de produção é verificada separadamente com HealingPickup real.

Unity 6000.4.3f1, cópia isolada Temp/Polimento002Validation, alvo Win64:

| Verificação | Resultado |
|---|---|
| Geração repetida | T028_PREFABS_IDEMPOTENT_OK; GUIDs/bytes estáveis |
| Validação de conteúdo | 66 assets, zero erros; dois avisos URP existentes |
| Primeira EditMode | 685/685 aprovados, zero ignorados |
| Primeira integração focada | 26/26 aprovados, zero ignorados |
| EditMode final | 685/685 aprovados, zero falhas/ignorados, código 0 |
| PlayMode final | 477/477 aprovados, zero falhas/ignorados, código 0 |

Artefatos locais em Logs/Polimento002/T028 e TestResults/Polimento002/T028, não versionados. Sem builds ou aceite físico nesta task; gameplay completo e frequência de drops serão validados em T031/T032. T014 permanece pendente e T029 não foi iniciada.
