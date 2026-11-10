# US6 / T047 — revisão da arte integrada

Início: 09/11/2026; retomada: 10/11/2026. Base de produção: `95f4c6b` (T046). Escopo exclusivo T047; T048–T053 e T075 da feature001 não são encerradas por este relatório.

## Estado

**T047 concluída em10/11/2026**, após validação técnica final e aceite manual R3. Aceite artístico e aceite jogável são registrados separadamente abaixo. A avaliação do usuário não equivale à rodada de cinco participantes da T051, a teste físico Android nem a profiling. O usuário confirmou disponibilidade para Windows; S23 não foi incluído nesta sessão.

Checklist de requisitos11/11, somente leitura. Hooks `.specify/extensions.yml` ausentes. Unity6000.4.3f1, mesmas versões de pacotes e geometria vigentes. Revisão em cópia isolada; nenhuma alteração de balanceamento autorizada.

## Identificação sem explicação prévia da função

Prancha gerada pelo Unity com sprites de produção, números1–14, sem nomes ou instruções de função. Fonte reproduzível: `Editor/ArtReviewSetup.cs`. Imagem e chave ficam em `Logs/Polimento002/T047`, fora do commit. O usuário já participou da escolha da arte e teve contato anterior com o jogo; registrar como participante familiarizado, não como amostra ingênua.

Resposta literal: “o 1 é o jogador, de 2 a 9 são inimigos, 10 é a cura e 11 é o xp, do 12 ao 14 são características do cenário”.

| Grupo | Resultado observado | Limite |
|---|---|---|
| Pato | Jogador identificado | Não mede movimento/Quack pela imagem. |
| Cinco comuns, dois minibosses e chefe | Todos agrupados como inimigos; Sapo-Rei reconhecido como diferenciado | Minibosses não comunicam ameaça especial: falha artística aberta. |
| C2 / X3 sem estrela | Cura e XP identificados corretamente; usuário os tornou definitivos na R2 | Não altera chance, valor, atração ou regras de coleta. |
| Rocha / juncos / grama | Rocha identificada como barreira, juncos/grama como decorativos no segundo pedido sem gabarito | Identificação aprovada para esta pessoa familiarizada. |

Complemento literal: “o item 12 seria uma barreira, 13 e 14 são decorativos, apenas o sapo rei tem aspecto diferenciado, apesar do sapo comum ser bem parecido com o sapo rei, os mini boss não tem nada que mostre que são mais ameaçadores.” A prancha normalizou o tamanho dos sprites: não demonstra a escala maior em gameplay. Mesmo controlando essa limitação, registrar deficiência de silhueta/hierarquia; não converter a resposta em aprovação dos minibosses. O “sapo comum” citado é a aparência percebida do Sapo-Cururu, que no catálogo já é miniboss, não uma espécie comum extra.

## Conferência técnica contra manifesto

As tabelas iniciais do manifesto descrevem o inventário antes dos lotes e perfis propostos. As seções T043–T046 registram a integração efetiva e prevalecem sobre frases históricas como “Circle compartilhado” ou “a produzir”.

| Conjunto | Estado atual e revisão | Evidência a reunir nesta execução |
|---|---|---|
| Nove atores | Silhuetas individuais; animação procedural de idle/movimento/nado/dano, pausa e reset por lease. Sem sheet de frames nem animação autoral de morte: término desativa view. | Identificação, leitura em combate e testes específicos. |
| Seis armas/três evoluções | Sprites e formas distintos, fontes T044. Bumerangue/tornado giram e retornam; ovos/incubadora mostram detonação; frontal/supersônico mostram setor; foguete orientado/explosão; aura translúcida; bolha iridescente. | Conferência de raios e estados em testes/capturas. |
| Quack ativo | Circular, raio autorado4; fullMultiplier não altera Range. Arcos reservados à arma frontal. | Comparação com feedback/OverlapCircle e sessão jogável. |
| Aura | Raio base N1=2 / N5=2,8 antes de relíquias; visual usa diâmetro2×raio efetivo, opacidade pulsa sem expandir borda. | Teste de hierarquia não unitária, parede e pausa. |
| Bolhas/Sabão | HitRadius base0,20; Sabão N0/N1/N5=0,20/0,232/0,36. Prévia e colisão consomem a mesma resolução; sem novo tuning. | Testes e confirmação visual do alcance. |
| Recompensas/VFX | C2/X3 definitivos por decisão do usuário; XP raro com satélite; feedback de evolução/raridade e impacto/reflexão/crítico preserva geometrias procedurais/pooling. | Estados de coleta, pausa/fim/reuso e distinção visual. |
| HUD/menus | Duas barras sem números; XP/nível inferior sem tarja; foco/pressed/disabled, perfis, cartas/prévia, Relíquias e resultados. Correção T046 reconstrói texto após escala/resolução mudar. | Quatro proporções, dois recortes, recursos/modais/entrada condicional. |
| Ambiente | Vegetação sem collider; pedras sobre footprint sólido; areia exterior ondulada, água/terra conservam fronteira física original. Marcas de água abaixo de atores/perigos, animadas por RunState. | Leitura junto à margem, colisão e pausa. |
| Refúgios | Três anéis translúcidos sobre áreas preexistentes; proteção somente contra ondas, destrutíveis pelo chefe. | Verificar legibilidade e não confundir com perigo ou bloqueio. |

## Aceite artístico do usuário

**Aprovado para o conjunto revisado**, após a correção das cartas e o reteste R3. O aceite resulta das respostas documentadas nesta task, não da aprovação histórica da amostra T042. C2/X3 foram tornados definitivos explicitamente; minibosses/Rei receberam aprovação específica e a interface corrigida foi aprovada na retomada.

Revisão R1: dois minibosses receberam nova silhueta e insígnia comum de elite. Geração `T047_MINIBOSSES_OK count=2 physics=unchanged`; atlas original preservado, prefabs/marcadores físicos inalterados. A nova prancha recebeu: “ficaram ótimos mas o sapo cururu parece mais ameaçador que o sapo rei agora”. Portanto, aprovação dos dois sprites com ressalva de hierarquia global, não aceite artístico final. R2 reforça o Sapo-Rei sem mudar ataques/colisão; aguarda comparação e reteste.

Resposta R2: “aprovo as modificações dos mini boss é do sapo rei, a cura e o xp podem ficar definitivos, a interface da escolha de armas não está adequado, parte das letras ficam sobre postas dificultando ver o início das letra”. Portanto: **minibosses/Rei aprovados; C2/X3 definitivos; cartas reprovadas até correção/reteste**. Não declarar aprovação global da interface.

R3: cartas receberam margem horizontal44, afastamento dos cantos e nova distribuição vertical. Primeira tentativa mostrou descrições parcialmente cortadas; corrigidos os blocos para preservar linhas de atributos, efeitos, sinergia e evolução. A matriz agora verifica `Text.preferredHeight` contra a altura disponível em cada campo. Essa última distribuição e a nova asserção passaram nos testes da retomada e receberam aceite manual. O laboratório de revisão também passa a ocultar seus botões em modais, para não encobrir a dica superior.

As barras de especiais agora usam o topo máximo entre collider e sprite visível, com referência visual em cache. Nova regressão confirma que a crista pode ultrapassar a hitbox sem mudar collider nem cobrir a arte. No fechamento da R2, o log revelou notificação de resultado após destruição do painel; `ResultsView.Show` passa a ignorar somente essa apresentação já destruída, mantendo validação dos dados e regras de resultado. Uma regressão de teardown foi acrescentada.

## Aceite jogável do usuário

**Aprovado para o roteiro coberto no Windows, antes da revisão dos minibosses.** Janela “QuackQuack T047”, IL2CPP, 1280×720, cenas/regras da base95f4c6b. A cópia de revisão apenas isola o save e inicia pausada; auxiliares não entram no produto. Executável local `Builds/T047/Windows/QuackQuack T047.exe`, SHA-256 `18D5D1AEA4095FBF1A99FB556003FB53C0D7600E147698664779E600D7350CBB`. Inicialização confirmada por `T047_REVIEW_READY`, sem exceções no log inspecionado. Build código0 / `PROTOTYPE_BUILD_OK`.

Roteiro: margem/pedras com aproximação de inimigos, reconhecimento de jogador/ameaças/cura/XP/passagem/bloqueio, leitura sob água/efeitos, Quack, pausa/retomada, Relíquias e uma escolha de melhoria. Resposta literal: “funciona corretamente”. Nenhuma falha/caso não testado foi informado; não inferir encontros de5/10/15 minutos, todas as armas/evoluções ou hardware Android. A build para revisão não substitui o gate de players/testes transversais da T048.

## Evidências técnicas desta execução

Auditoria Unity: `T073_CONTENT_VALIDATION_OK assets=67 warnings=2` (avisos existentes), `T047_ART_AUDIT_OK weapons=9 levels=45 textureBindings=valid`. Exportação `weapon-geometry.tsv` registra Range/AreaRadius/HitRadius/ConeAngle e sprite/material dos45 níveis em terra; dados lidos, não alterados.

Primeiro lote PlayMode:84/84 aprovados, zero falhas/ignorados, cobrindo CombatArt, CartoonActorVisual, BubbleArea, AuraOcclusion, PassiveIntegration, QuackFeedback, UpgradeFeedback, LakeWaterAnimation, IntegratedUiLayout e CartoonUiTheme. Segundo lote:69/69 aprovados, zero falhas/ignorados, cobrindo sinais/encontro do Sapo-Rei, scheduler/estados pooled de inimigos e barras especiais. XMLs `art-playmode.xml` e `threat-playmode.xml`; PNGs da matriz em `Logs/Polimento002/T047/Matrix`. Esses resultados precedem a correção artística dos minibosses, que exige reteste específico.

Após a R2:725/725 EditMode (`editmode-final.xml`, SHA-256 `E15B82DCC1E54FBE43FF1E21A3DCF903B7E3E4EB5711C4236DE02BA2965FDFD3`) e154/154 PlayMode focados (`playmode-final.xml`) aprovados, zero falhas/ignorados. Incluem a regressão da âncora e os sprites revisados. Geração `T047_THREATS_OK count=3 physics=unchanged`; recortes/material e física dos três prefabs validados. Estes resultados precedem as mudanças finais das cartas.

Primeira correção de cartas/teardown:22/22 PlayMode (`cards-r3.xml`), zero falhas/ignorados. A inspeção da imagem4:3 revelou descrição de sinergia cortada; por isso esses22 testes **não aprovam a distribuição final**. Última geração `cards-generate-final.log` concluída; última matriz e EditMode ainda pendentes. As falhas iniciais de compilação do laboratório isolado e do nome do cache visual foram corrigidas antes dos resultados aprovados; não foram puladas.

## Checkpoint da pausa em09/11/2026 (histórico)

- Usuário pediu: “os tokens irão acabar e isso irá interromper os testes. Pare antes do teste real, deixe para depois que renovar os tokens.” Nenhuma nova sessão foi aberta após o pedido.
- Build R3 IL2CPP concluída, código0 / `PROTOTYPE_BUILD_OK`: `C:/Users/lucas/source/repos/QuackQuack/Builds/T047/R3/QuackQuack T047 R3.exe`. **Ainda não iniciada nem aceita pelo usuário.**
- Cópia de validação local: `Temp/T046Validation`, com Library já importada e código sincronizado. O harness runtime foi retirado de Assets após o build para não interferir em testes.
- Harnesses/fontes auxiliares locais: `Logs/Polimento002/T047/T047ReviewR3Harness.cs` e `T047Build.cs`. Não versionar esses auxiliares nem logs/XML/builds. A R3 tem controlesF5/F6/F7 (encontros) eF8 (+100XP para cartas), disponíveis com partida pausada; ocultos em Relíquias/Controles/escolha/resultados. Nenhum desses atalhos entra no produto.
- Próximo passo técnico: executar EditMode e PlayMode focado final, incluindo IntegratedUiLayoutTests, ResultsViewTests, CartoonActorVisualTests, testes de armas/raios/ameaças e água; gerar nova pasta de capturas e conferir todas as linhas das cartas. Se algum teste anterior for considerado interrompido, refazê-lo; os XMLs aprovados acima são de execuções concluídas.
- Depois, somente quando o usuário retomar, abrir a R3. Retestar cartas viaF8, todos os textos/ações/prévia; depoisF5/F6/F7 para ameaças/barras/avisos. O pedido anterior de reabrir o teste R2 foi substituído por esta pausa; não contar como resposta positiva.
- Registrar aprovação artística da interface e aceite jogável dos encontros revisados; atualizar hashes finais/manifesto e fechar T047 somente com resultado suficiente. T048 não foi iniciada.

## Retomada em10/11/2026

A antiga cópia `Temp/T046Validation` havia sido removida. A tentativa inicial terminou antes de executar testes por ausência do diretório. Cópia limpa recriada em `Builds/T047/ValidationProject`, fora de Temp, sem reaproveitar Library. Pacotes/settings do projeto preservados. O harness diagnóstico não foi incluído nessa cópia, para não interferir nos testes.

Resultados finais, zero falhas/ignorados:

| Execução | Resultado | XML / SHA-256 |
|---|---|---|
| EditMode completo |725/725| `resumed-editmode.xml` / `80378B4A3603C3B6BC70E51534CF690761A512ED2894D73CAF270FDDAAA1B319` |
| PlayMode focado de arte/UI/ameaças |160/160| `resumed-playmode.xml` / `DFAEC0BE7013389E3D715F350D33226EA1046E8166426C980FE183C3C173B096` |

O lote PlayMode reúne os grupos de arte/raios/passivas/feedback/água/interface/ameaças e ResultsViewTests; inclui regressões de crista acima da hitbox, notificação terminal após destruição do painel e altura preferida de todos os campos das cartas. Não é declaração de execução completa de PlayMode ou de test players: isso pertence à T048.

As96 capturas finais em `Logs/Polimento002/T047/ResumedMatrix` cobrem1920×1080,2160×1080,2340×1080 e1440×1080, ambos os recortes. Cartas4:3 e16:9 inspecionadas após o último ajuste: início das letras fora da decoração e descrições de sinergia/evolução completas. Nenhuma asserção de encaixe/raio/pausa foi removida. Código/serializados modificados do projeto conferidos contra a cópia testada, sem divergência.

A R3 preservada foi aberta somente após esses resultados. `resumed-r3-player.log` confirmou `T047_REVIEW_R3_READY`; F8 confirmado por `T047_REVIEW_CARDS`. Caminho: `Builds/T047/R3/QuackQuack T047 R3.exe`; hash de `GameAssembly.dll`: `BAD3259CAF714D379810347BD38B2E7F65FC26A29629F56762F1C41D045A7D4B` (o launcher .exe é compartilhado entre builds e sozinho não distingue revisões).

## Aceites finais R3 — 10/11/2026

| Aceite | Roteiro apresentado | Resposta literal |
|---|---|---|
| Artístico/interface | F8: títulos, atributos, sinergia, evolução, prévia do Sabão; linhas fora da moldura, ações escolher/banir/rerrolar/pular e aprovação da interface/conjunto atual | “Ficou bom” |
| Jogável/ameaças | F5/F6/F7: minibosses especiais, Rei superior, barras acima das cristas/coroa e avisos de ataque legíveis | “funciona corretamente” |

Nenhuma falha ou etapa não testada foi informada nessas respostas. Aceite manual do usuário familiarizado no Windows, com encontros antecipados por diagnóstico; não é evidência de uma partida normal de15 minutos. O log confirma as cartas e seeks300/600; não contém seek900 na inspeção final. Portanto a etapa do chefe tem confirmação manual do roteiro, não comprovação automática de acionamento do atalho. Sem exceções na sessão inspecionada. Não inferir teste de todas as armas em combate, de todos os controles, Android ou desempenho.

## Pendências e critério de fechamento

- Revisão técnica de estados/assets/raios e quatro proporções concluída, com limites de cobertura registrados.
- Identificação inicial sem gabarito registrada; falhas de hierarquia corrigidas e aprovadas em reteste assistido, sem alegar nova avaliação ingênua.
- Aceites artístico e jogável R3 registrados separadamente; correções de cartas, barras e teardown verificadas.
- Continuam pendentes as suítes/test players transversais T048, profiling T049, matriz física T050 e rodada com cinco participantes T051.

T047 concluída no escopo desta revisão. T048 não iniciada; a T075 da feature001 mantém seu estado independente.
