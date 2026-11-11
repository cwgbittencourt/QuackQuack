# T051 — nova rodada de gameplay do Lago

Rodada `002-T051-R1`, protocolo v1, aberta em 11/11/2026 (America/Sao_Paulo).
**Coleta parcial, sem aceite global:** mínimo cinco participantes; P01 avaliado,
P02–P05 ainda não recrutados. Nenhuma resposta anterior conta como nova sessão.
O usuário autorizou começar por ele e reunir os demais depois.

## Condições e fontes

- Runtime/conteúdo congelados em `d378efd`; Unity 6000.4.3f1. Alteração de código,
  arte, balanceamento ou save inicial abre nova revisão de rodada e identifica retestes.
- Build normal IL2CPP das cenas RunBootstrap/LakeRun, seed autoral 42, sem relógio
  acelerado, invulnerabilidade, XP concedido ou build forçada. Não usar o perfil
  automático da T049 nem laboratórios T023/T050 como experiência natural.
- Empacotamento isolado: produto `QuackQuack Playtest 002`; Android, quando preparado,
  pacote `com.cemggames.quackquack.playtest002`. Nenhum save habitual é sobrescrito.
  Build Windows concluída pelo pipeline de produção (`PROTOTYPE_BUILD_OK`, saída 0).
  Artefato em `Builds/T051/ValidationProject/Builds/Windows/QuackQuack.exe`;
  GameAssembly SHA-256 `01c9951f7b2244b3547308e603663d0e490ef7d1af17b13febcad77597be1532`,
  EXE SHA-256 `18d5d1aea4095fbf1a99fb556003fb53c0d7600e147698664779e600d7350cbb`.
  Commit completo `d378efd89ad09755e0f942479e3b106d51fd6e14`; GUID não exposto
  pela build normal, não inferido. Produto não Development, Windows x64 IL2CPP.
  O log da importação inicial contém CS0246/GUID em pacotes Editor; após nova
  compilação no mesmo processo, o relatório final registra `Build Finished, Result:
  Success`, `PROTOTYPE_BUILD_OK` e saída 0. Não descrever o log inteiro como livre
  de erros. Nenhuma alteração desses pacotes ou supressão de mensagens foi feita;
  o player resultante iniciou e seu log de sessão não apresentou erro/exceção observado.
- P01 escolheu Windows com controle Xbox; modelo exato/conexão não reconfirmados. Sua familiaridade
  com o projeto é conhecida e impede descrevê-lo como primeira exposição ingênua.
- Fonte dos critérios 1–15 conferida em `E:/T.I/Projetos/Quack-Quack/Melhorias.md`
  (SHA-256 `3805d6285f773e5b0892e0f66f658967757428ca902c0444cf2581734cc2e04c`).
  O arquivo externo não é copiado ao repositório. Valem as revisões aprovadas em
  [spec.md](spec.md) e [decisions.md](decisions.md), inclusive cura 10%/15%, miniboss
  50%, chefe sem XP/cura, Sabão 16%/nível até 80%, dicas 10 s e HUD sem tarja.
- Base de método: [formulário T076](../001-prototipo-lago/playtest-form.md).
  Seus SC-001–005 pertencem à feature 001; os SC-001–009 deste relatório são os
  da feature 002. Não intercambiar metas, numeração ou aprovação.
- T049: Windows/S23 medidos; Android intermediário/de entrada pendentes. T050:
  8BitDo/S23 pendente. Avanço a T051 solicitado explicitamente pelo usuário;
  nenhuma dessas pendências é encerrada pela rodada de gameplay.

## Protocolo sem indução

1. Atribuir P01–P05 sem nomes/contatos no relatório. Registrar experiência prévia,
   plataforma, entrada/modelo, resolução, orientação, save/relíquias e identidade
   do artefato. Não mostrar o gabarito abaixo aos participantes antes de responderem.
2. Instrução inicial: “Jogue como faria normalmente. Você pode parar quando quiser.”
   Dicas do próprio jogo são permitidas e registradas; não explicar ícones, terrenos,
   alvos, armas ou receitas. Guardar falas literais e ajuda recebida com instante.
3. Observar os primeiros 120 s reais, inclusive menus/pausas, seguindo a ficha T076.
   Essa janela mede compreensão inicial, não o limite da sessão natural.
4. No primeiro término natural, observar se reinicia espontaneamente, sem convidar.
   Depois perguntar sobre decisões de Fôlego, clareza/justiça de mortes e vontade
   de repetir. Para P01, classificar a familiaridade anterior e a limitação de inferência.
5. Antes de explicar funções, perguntar de forma neutra sobre os elementos vistos:
   “O que você reconhece aqui? O que espera que aconteça ao chegar perto ou atravessar?”
   Registrar separadamente avatar, recompensas, bloqueios, decoração e ameaças.
   Elemento que não apareceu fica **não apresentado**, nunca aprovado por suposição.
6. Após o bloco natural e identificação, fazer comparações dirigidas de armas,
   passivas, terreno, menus e pressão. Solicitar descrição antes de dar a explicação;
   registrar ordem, assistência e condições. Demonstrações forçadas, se necessárias,
   ficam em sessão assistida separada e não provam descoberta, balanceamento ou
   repetição espontânea. Nenhum laboratório é tratado como experiência natural.
7. Registrar todas as tentativas e falhas, inclusive mortes, desistências e sessões
   interrompidas. Crash/comando bloqueado exige interrupção técnica e tentativa
   vinculada de reteste; derrota normal não é descartada nem substituída por vitória.
8. Falha de reconhecimento/clareza abre defeito e reteste identificado; não apagar a
   primeira resposta nem aprovar pela média. Se a correção mudar o estímulo, a pessoa
   já exposta pode retestar clareza, mas não recuperar uma primeira exposição perdida.
9. Para métricas herdadas da feature 001, usar integralmente a ficha T076, inclusive
   dez partidas por participante para seu SC-003; uma sessão curta não as aprova.
   A T051 não encerra a T075 antiga por inferência.

Gravação de voz/imagem somente após autorização; por padrão usar respostas escritas
e observação. Evidências brutas e saves de pesquisa ficam locais/restritos, fora do
Git; versionar apenas resumo anonimizado. Não enviar convites a terceiros sem pedido.

## Participantes e sessões

| Código | Familiaridade | Plataforma/entrada | Sessão nova | Identificação sem explicação | Estado |
|---|---|---|---|---|---|
| P01 | Conhece o jogo e revisões anteriores | Windows/Xbox; conexão/modelo exato não reconfirmados | S01 encerrada voluntariamente | Identificou os cinco grupos apresentados | Identificação consolidada; cobertura complementar pendente |
| P02 | A confirmar | A confirmar | Não iniciada | Não coletada | Recrutamento pendente |
| P03 | A confirmar | A confirmar | Não iniciada | Não coletada | Recrutamento pendente |
| P04 | A confirmar | A confirmar | Não iniciada | Não coletada | Recrutamento pendente |
| P05 | A confirmar | A confirmar | Não iniciada | Não coletada | Recrutamento pendente |

Copiar a [ficha individual](playtest-session-template.md) por participante/sessão.
Resultados via chat podem ser evidência de relato; distinguir de observação direta
ou inspeção de logs. Resposta genérica não comprova elementos que não foram apresentados.

### P01 / S01 — resultado parcial

Iniciada a build em 11/11/2026 às 19:32:56 −03:00. Janela `QuackQuack Playtest 002`
conferida via computer-use: partida presente, HUD, avatar e cenário, relógio 00:10.
Essa inspeção só confirma inicialização, não reconhecimento pelo participante.
Argumentos iniciais de janela 1280×720, paisagem 16:9; a janela foi ampliada durante
a sessão. Resolução efetiva posterior não foi lida do runtime; não aprova outras telas.
Nenhuma ação de gameplay foi injetada pelo moderador. Relato de duração, modelo,
elementos vistos, decisões, morte/sinais e eventual reinício foi solicitado.
Os primeiros 120 s não foram cronometrados por observação contínua; não aprovar
compreensão inicial da feature 001 por esse registro. Sem gravação de voz/imagem
do participante; resumo escrito da conversa é a fonte de relato prevista.

Primeiro retorno de P01: **“o jogo funciona corretamente”**. Registrado como relato
geral favorável de funcionamento, sem aprovar identificação, arma específica,
decisão de Fôlego, conclusão natural, reinício espontâneo ou matriz completa.
Solicitado complemento de modelo/término e descrição livre do que reconheceu.
Inspeção pontual posterior, sem inputs: relógio 03:13 e nível 10, recursos e combate
visíveis. Observação do moderador não substitui as respostas de reconhecimento.

Complemento de P01: **“joguei no controle de xbox, eu fechei a partida, não comecei
outra”** e **“tudo fácil de perceber”**. Encerramento voluntário informado; processo
deixou de existir e log registra shutdown, sem exceção/erro observado no recorte
examinado. Não foi relatada morte; duração final e uso de ABANDONAR versus fechar a
janela não foram discriminados. Não iniciou segunda partida, conforme autorrelato;
isso não é falha funcional nem uma medida de toda a coorte.

Facilidade subjetiva favorável, sem ambiguidade relatada. Naquele ponto faltavam nomes/
funções por elemento para avaliar identificação objetiva: a resposta genérica não
foi convertida em cinco identificações corretas. Solicitados lista curta e percepção de Bolhas/Sabão;
nenhuma explicação das funções foi dada antes desse complemento. Decisão de Fôlego
com exemplo, causa/sinal de morte e cobertura integral dos critérios não foram obtidos.

Após complemento estruturado sem fornecer nomes/funções esperados, P01 respondeu:
**“pato; xp, subir de nível, cura, recuperar; pedras; plantas; formiga, aranha.”**
Identificação correta dos elementos apresentados: avatar, dois tipos de recompensa,
bloqueio/decoração e ameaças comuns citadas. Nenhum especial foi identificado nesta
resposta; não estender a eles o resultado. A familiaridade prévia permanece explícita.

Na pergunta posterior sobre a comparação Bolhas/Sabão, selecionou **“Comparei e
percebi o aumento”**. Percepção de crescimento positiva; níveis, terreno, relíquias
no instante e configuração anterior/posterior não foram informados. Não comprova
porcentagens nem comparações N0/N1/N5 específicas, que permanecem lacunas de cobertura.

Após a leitura inicial negada, a inspeção do save dedicado foi executada com a
permissão de arquivo apropriada e cópia local em `P01-S01-save-after.json`, sem
publicar o JSON bruto. Schema 2, uma partida, zero vitórias, 92 moedas; dez relíquias
desbloqueadas e três equipadas no estado final (`pena-de-combate`, `pedra-da-margem`,
`apito-veloz`). SHA-256 `d0645aca0cb4ef637a1f4328520093260ae15ad8a8b9ba71d543e6ec142bde62`.
Isso confirma o estado posterior, não o estado inicial nem todos os passos de
navegação. Não houve snapshot anterior: não presumir loadout inicial. Congelar e
registrar esse perfil antes das próximas sessões comparáveis. Saves habituais não
foram lidos/alterados; somente o produto de playtest tem essa evidência dedicada.

Síntese P01: funcionamento e facilidade relatados favoravelmente; reconhecimento
dos itens apresentados e crescimento percebido de Bolhas positivos. Nenhuma falha
nova relatada, nenhuma correção de gameplay feita. Não há relato específico de
decisão de Fôlego, sinal de morte, cinco outras armas/três evoluções, especiais,
hotplug Android ou toda a matriz de tela/pressão. **Não avaliado** nesses casos.

Evidências locais ignoradas: `Logs/Polimento002/T051/windows-build.log`,
`P01-S01-artifact.json` e `P01-S01-player.log`. Nenhum novo teste de regra executado:
runtime inalterado desde T049, que registrou 725/725 EditMode e 630/630 PlayMode.
Esses números são contexto técnico identificado, não resultado de gameplay T051.

## Matriz dos critérios 1–15

Síntese rastreável do material externo, com revisões aprovadas; não são resultados.
Em cada linha registrar participante, sessão/tentativa, instante, falas, evidência,
defeito e reteste. Resultados específicos abaixo; os demais participantes ainda não
foram avaliados. A ficha parcial de P01 não aprova integralmente um critério composto.

| Critério | Verificação de gameplay | Referência | Limite de aceite |
|---|---|---|---|
| 1 | Reconhecer avatar, inimigos, recompensas, bloqueios/decoração; água sem ocultar perigo | FR-001/002; SC-007 | P01 identificou itens citados; água/especiais não avaliados; P02–P05 pendentes |
| 2 | XP/nível no centro inferior, inclusive após escolha/subida | FR-003; SC-005 | Confirmar evento real, não só presença da barra |
| 3 | Vida/Fôlego, prontidão/aprimoramento e desaparecimento das dicas | FR-004/005; SC-006 | Registrar estados efetivamente vistos e tempo de simulação |
| 4 | Distinguir cada arma pela forma/animação/trajetória e alcance | FR-006; SC-007 | Seis armas e três evoluções cobertas na rodada |
| 5 | Aura dentro/fora do alcance, com/sem obstáculo | FR-007; SC-003 | Parede continua bloqueando movimento |
| 6 | Efeitos das passivas e comparação Bolhas/Sabão antes/depois | FR-008; SC-003/007 | P01 percebeu crescimento; níveis/configuração não registrados; raio/porcentagens e demais passivas não aprovados pela percepção |
| 7 | Bumerangue na ida e volta, sem múltiplos acertos por trecho | FR-009; SC-003 | Percepção separada do contrato técnico T023 |
| 8 | Cura comum probabilística e limitada à falta de vida | FR-010; SC-004 | Amostra curta não prova chance de 10%; sem quota de dez mortes |
| 9 | Captura próxima, persistência, obstáculos e concessão única de XP | FR-011; SC-005 | Não confundir seguir após captura com capturar de longe |
| 10 | Barras especiais, remoção ao morrer e cura dos dois minibosses | FR-012; SC-004 | Chefe final sem XP/cura; registrar especiais não alcançados |
| 11 | Controle/toque no smartphone sem perda de ações | FR-013; SC-002 | Não avaliado por sessão Windows; 8BitDo/S23 segue pendente |
| 12 | Menus, foco, confirmar/voltar, atalho Relíquias e dono da pausa | FR-014/015; SC-002 | Registrar modelo e telas efetivamente operadas |
| 13 | Pressão em borda/canto/pedra e retomada com espaço | FR-016; SC-001 | Não extrapolar um canto para toda a matriz |
| 14 | HUD/menus/efeitos legíveis nas áreas/proporções previstas | FR-017; SC-006 | Uma resolução não aprova quatro proporções/dois recortes |
| 15 | Rodada nova + testes/profiling sem regressões aplicáveis | FR-018; SC-008/009 | Evidências técnicas separadas; hardware ausente permanece pendente |

Demais resultados P01: critério 2, observação pontual de nível 1→10 e função do XP
identificada, sem avaliação completa de transições; 3, recursos visíveis e relato
geral, sem discriminar prontidão/aprimoramento/dicas; 4, somente comparação relatada
de Bolhas, sem prova de diferenciação de todo o conjunto; 5/7 não avaliados; 8,
função da cura identificada sem medir chance/quantidade; 9, função do XP identificada
sem medir captura/obstáculos/concessão única; 10/11 não avaliados; 12, Xbox usado e
relato funcional, sem roteiro completo de menus/atalhos; 13 não discriminado por
local; 14, relato favorável na sessão Windows, sem matriz completa; 15 parcial,
uma sessão de participante e contexto técnico anterior, nunca aceite global.

## Cobertura de armas, passivas e evoluções

| Elemento | Participante/sessão/tentativa | Natural ou assistido | Identificou/compreendeu? Fala | Falha/reteste |
|---|---|---|---|---|
| Bumerangue / Pena aerodinâmica | — | — | Não coletado | — |
| Ovos / Ninho reforçado | — | — | Não coletado | — |
| Quack frontal / Pulmão poderoso | — | — | Não coletado | — |
| Aura de energia / Amuleto do lago | — | — | Não coletado | — |
| Foguetes / Óculos de aviador | — | — | Não coletado | — |
| Bolhas / Sabão mágico N0→N1 e N1→N5 | P01/S01; níveis não informados | Comparação relatada na partida normal | “Comparei e percebi o aumento” | Comparações exatas/contexto pendentes; nenhuma falha relatada |
| Tornado de penas | — | — | Não coletado | — |
| Incubadora de combate | — | — | Não coletado | — |
| Quack supersônico | — | — | Não coletado | — |

Sem impor receitas antes da primeira experiência. Se a distribuição natural não
mostrar algum item, manter lacuna explícita até sessão complementar; T023 não
preenche automaticamente cobertura desta rodada. Percepção de crescimento das
Bolhas deve incluir o relato e a condição comparada, não apenas os valores técnicos.

## Consolidação SC-001–009 da feature 002

| SC | Estado da nova rodada | Evidência necessária |
|---|---|---|
| 001 | Não coletado | Locais/pressão/retomada, sem extrapolar matriz incompleta |
| 002 | Não coletado | Plataformas/modelos/contextos operados e limitações |
| 003 | Parcial perceptivo: crescimento Bolhas relatado | Aura/Sabão/bumerangue e correspondência real/visual completos pendentes |
| 004 | Função da cura reconhecida; comportamento não avaliado | Cura/vida/consumo único; probabilidade apoiada pelos testes |
| 005 | Função do XP reconhecida e progressão vista pontualmente | Captura/obstáculos/concessões/transições completos pendentes |
| 006 | Relato favorável de P01 e inspeção pontual; sem matriz | Resoluções/recortes/telas/estados, cobertura efetiva |
| 007 | Parcial: P01 reconheceu itens apresentados e percebeu crescimento de Bolhas | Faltam P02–P05, demais armas/evoluções e contexto das comparações |
| 008 | Parcial técnico, fora da amostra de experiência | T049 Windows/S23; intermediário/entrada pendentes |
| 009 | Parcial: critérios 1–15 registrados com resultados/limitações | Rodada e cobertura incompletas; nenhuma aprovação global |

Recrutados para esta retomada: 1; sessões novas encerradas: 1; participantes com
identificação registrada: 1; rodadas com cobertura completa: 0. Mínimo adicional de
participantes: 4. Encerrar sessão não equivale a concluir cobertura da rodada.
Nenhum SC foi aprovado pela criação deste protocolo. Defeitos e retestes serão
registrados abaixo, sem converter ausência de ocorrência em sucesso.

## Ocorrências e decisão

Nenhum defeito novo relatado na sessão P01; nenhuma reprodução/correção/reteste novo
necessário a partir desse relato. Ausência de relato não demonstra cobertura completa.
Aprovações históricas T007/T014/T023/T032/T040/
T047/T050 são contexto e não respostas dos cinco participantes. A T051 permanece
aberta até executar e consolidar a rodada exigida.
