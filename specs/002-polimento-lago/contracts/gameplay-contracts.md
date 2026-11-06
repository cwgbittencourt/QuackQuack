# Contratos observáveis — extensão do Lago

Base: [contratos da feature 001](../../001-prototipo-lago/contracts/gameplay-contracts.md). Estes contratos alteram somente os comportamentos explicitamente descritos; nomes novos serão implementados nas tasks desta feature.

## C1 — Área jogável e pressão (FR-016)

Entrada: comandos equivalentes, contatos e geometria autoral. Saída: jogador inteiramente na região caminhável, sem aprisionamento permanente quando há espaço. Pressão normal continua relevante. Registre reprodução/causa antes de selecionar correção. Não trocar prova por clamp visual, desabilitar contato global ou teleporte sem diagnóstico. Testar perímetro, cantos, obstáculos e arena do chefe com timestep/carga suportados.

## C2 — Comandos e foco (FR-013–015)

RelicsPressed é borda de ação comum, separada de PausePressed. É descartada em contexto terminal; toggle aceito uma vez. Abrir relíquias durante simulação adquire pausa; fechar só retoma se esse menu for dono. Abrir durante pausa não retoma ao fechar.

No smartphone, controle conectado e habilitado oculta joystick e QUACK touch independentemente da última fonte. Removed/Disconnected/Disabled restaura controles e limpa move/pressed sem reinício. Navegação de UI continua disponível em pausa/modal, sem vazar movimento/Quack.

Grafos de foco incluem cartas, detalhes, banir/rerrolar/pular, relíquias e voltar. Controles indisponíveis não capturam foco; após rerrolagem/limite de equipadas, foco permanece válido. Cancel fecha detalhe/retorna foco; seleção obrigatória não fecha com escolha/pulo implícito. PULAR válido ou escolha conclui oferta. Relíquias fecha por Cancel/atalho, restaurando foco/contexto. Results também mantém foco utilizável após hotplug.

Bindings/ícones seguem perfil real Xbox/PlayStation/genérico. Se Select não existir, perfil configurável e ação focalizável garantem acesso; não adivinhar glifos. Perfis locais versionados não alteram regras nem o save de progresso.

## C3 — Atributos, aura e bumerangue (FR-006–009)

ResolveEffective(base, passives, relics, terrain) é idempotente para mesmos argumentos e não muta assets. Todas as passivas autoradas têm efeito real verificável e apresentação correspondente.

Aura: pulso consulta inimigos no raio efetivo, inclui fronteira, deduplica múltiplos colliders e ignora obstrução para dano. Continua sem collider sólido; paredes seguem bloqueando movimento. Outros ataques mantêm suas políticas de oclusão. VFX translúcido animado representa raio real e pausa/desativa com regra.

Sabão Mágico: elegível somente Bolhas, raio = base × (1 + min(0,16 × nível, 0,80)), níveis 0–5 conforme dados atuais. Fonte numérica é PassiveDefinition, não constante duplicada no runner/view. HitRadius de colisão e diâmetro visual usam o mesmo valor efetivo; não alterar dano/aquisição/velocidade nem outras armas. Preview antes/depois usa a mesma resolução.

Bumerangue: cada lease de alvo recebe no máximo um acerto por passagem Outbound e Inbound do mesmo lease de projétil. Passo cruzando retorno preserva ambas as fases; reset não acontece por frame. Reuso de alvo não herda imunidade da vida anterior. Substituir teste antigo que proíbe segundo acerto total.

## C4 — Derrota, cura, XP e saturação (FR-010–012)

ProcessDefeat(origem, lease, dados, posição): valida uma vez por derrota real. Common → XP garantido + um sorteio independente r <0,10 para cura de 0,15 × MaxHealth; miniboss → XP + uma cura 0,50 garantidos, sem sorteio comum extra; chefe → sem XP nem cura; moedas, vitória e registros uma vez. Despawn não é derrota. Um em dez significa probabilidade, não quota. RNG é dedicado e reproduzível; repetir notificação/materialização não sorteia novamente.

Ledger registra obrigação antes de alugar view. Sem slot: manter posição/ID/valor e tentar materializar, sem descartar nem conceder à distância. Reservar/priorizar miniboss e medir capacidade. Teto técnico nunca reduz chance; incapacidade de suportar carga autoral bloqueia aceite. No teardown registrar destino de pendências; não carregar drops para nova partida.

Heal(fração): jogador vivo, valor aplicado min(falta, MaxHealth × fração). Claim somente após aplicação positiva; não desperdiçar cura cheia nem reanimar morto. Cada coleta aceita uma vez. XP usa valor positivo e TryCollectExperience/fluxo existente de níveis; não duplica cartas por replay.

## C5 — Atração compartilhada (FR-011)

Distância de captura centro-centro <=3 × raio físico escalado do Pato (base 1,26). Cura cheia não captura. Capturado → seguir a 1,25 × velocidade efetiva na água, base 7,5, mesmo que distância aumente. Sem oclusão por obstáculos durante atração; coleta final usa contato/limiar existente sem conceder remotamente. Pausa/seleção suspendem; morte/fim cancelam. Se cura já capturada fica sem vida faltante, é descartada sem aplicar cura nem reenfileirar a recompensa (revisão do usuário na T029, 05/11/2026). Reset pooled limpa todo estado e preserva concessão única.

## C6 — HUD e dicas (FR-003–005/012/017)

Snapshots são projeções de regras, sem recalcular curva/área na view. Vida/Fôlego: duas barras com ícones e estados além da cor; remover números, textos de terreno e instruções permanentes dos produtores, geradores e view. XP/nível inferior central; último limiar mostra barra cheia no nível atual.

Dicas ocupam apenas primeiros 10 segundos de simulação; pausa suspende, família muda símbolos sem reset, sem reapresentação posterior. Barra de especial individual acima da âncora de cada entidade, posição filtrada para não ocultar barra de outra, sem painel agregado. Registro por lease; fora da tela não deslocar barra sobre alvo diferente. Compatibilidade com UI Safe Area, modais de bloqueio inteiro e ambos os lados da paisagem.

## C7 — Arte e validação (FR-001/002/006/018)

Identidade reconhecível do elenco aprovado, contornos cartoon, distinção decoração/colisão. Manifesto de assets e cena-amostra aprovada antes de lote; referências duráveis não são assets finais. Animação de água não encobre água/terra/limites/perigos. Seis armas e três evoluções têm forma/animação/trajetória distintas além da cor, correspondentes ao alcance real.

Aceite separado: testes de regras, integração Unity, entrada física, revisão artística, gameplay e profiling. Quatro proporções, recortes dos dois lados; Windows/Android com teclado/controle/toque; alvos 200/400 inimigos e 60/30 FPS constitucionais. Nenhum teste sintético, screenshot ou S23 isolado aprova todos os dispositivos.


**Revisão T030 (05/11/2026):** chefe final sem XP nem cura; moedas, vitória e registros preservados uma vez. Pickups pendentes são cancelados no término, sem coleta automática. Esta decisão substitui referências anteriores a XP do chefe; ver [decisions.md](../decisions.md) e o relatório T030.
