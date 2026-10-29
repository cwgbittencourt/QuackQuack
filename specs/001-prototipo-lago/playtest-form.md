# T076 — formulário de playtest do Lago

Modelo em branco, preparado em 29/10/2026. **Nenhum participante foi avaliado e
nenhum SC foi aprovado pela criação deste formulário.** Copiar as fichas por
participante, sessão, partida, escolha e morte. Critérios: [spec.md](spec.md).
Estado técnico: [validation-report.md](validation-report.md).

## Preparação do moderador

Usar participantes representativos do público-alvo, identificados por códigos
(P01, P02…), sem nome ou contato nas fichas. Definir a amostra antes da coleta;
a especificação não fixa seu tamanho. Resultados de grupo pequeno são descritivos.
Guardar respostas e capturas em local de acesso restrito; só incluir no repositório
um resumo anonimizado revisado. Pedir autorização antes de gravar voz ou imagem.

| Plano da rodada, preencher antes da coleta | Valor |
|---|---|
| ID da rodada / responsável / data e fuso | |
| Perfil de público / recrutamento / participantes planejados | |
| Versão do protocolo / plataforma e entrada de cada grupo | |
| Build de software e conteúdo congelada para a rodada | |
| Perfil de save inicial, relíquias disponíveis/equipadas | |
| Seeds das dez partidas e ordem por participante | |
| Intervalos, duração das sessões e distribuição das dez partidas | |
| Convenção de configuração/combinação SC-003 (abaixo) confirmada | |
| Defeitos conhecidos, critérios de interrupção e responsável pela revisão | |

Na rodada inicial da T075, VAL-UI-01/02/03 impediam acesso a relíquias e encobriam
ações/relógio no Android. Consultar a retomada no relatório para a correção e a
identidade da build revalidada. Uma coleta na build antiga é exploratória e deve registrar o impedimento;
não aprovar experiência integral ignorando esses defeitos. Preparar o formulário
não exige encerrar a T075, mas o aceite depende de sua revalidação.

Não usar cheats, invulnerabilidade, relógio acelerado, XP concedido ou builds
forçadas na amostra de experiência. Ensaios técnicos assistidos ficam separados.
Não trocar seed, balanceamento, save inicial ou versão silenciosamente entre
tentativas. Mudanças abrem outra rodada; não juntar resultados como se fossem iguais.
As dez partidas podem ocupar várias sessões para reduzir fadiga.

## Roteiro sem indução

1. Na primeira sessão, explicar apenas: “Jogue como faria normalmente. Você pode
   parar quando quiser.” Confirmar que o dispositivo funciona, sem ensinar terreno,
   Quack, Fôlego ou combinações. Registrar familiaridade e contato prévio com o jogo.
2. Cronometrar os primeiros **120 segundos reais** desde o início do controle.
   Menus e pausas escolhidos pelo participante contam. Observar sem dicas. Se morrer
   antes, registrar e permitir continuação sem ensinar; não reiniciar o cronômetro.
3. Aos 120 s, registrar SC-001 com as perguntas neutras abaixo e demonstração.
   Não corrigir as respostas durante essa medição. Anotar qualquer ajuda anterior.
4. No primeiro término natural, observar o restante da **primeira sessão natural**,
   até a pessoa decidir encerrá-la, se inicia outra partida sem convite. Não sugerir
   “jogar novamente” nem anunciar ainda o bloco de dez partidas. Não impor uma
   janela de dois minutos à SC-005: esse prazo pertence somente à SC-001.
   Se iniciou uma segunda partida antes dos 120 s iniciais, preservar a evidência.
5. Depois da observação espontânea, entrevistar sobre a experiência da primeira partida
   (SC-002 e mortes). O clique de reinício precisa efetivamente iniciar uma partida;
   intenção verbal ou reinício solicitado não conta para SC-005.
6. Só então apresentar, em sessão de continuação, o bloco dirigido: dez partidas válidas por participante para
   SC-003, incluindo as partidas naturais já observadas quando usam as condições
   planejadas. Solicitar jogar normalmente, sem impor armas ou ensinar receitas.
   Cada morte gera ficha SC-004, antes de mostrar replay ou explicar a causa.

Se houve dica, registrar texto, instante e motivo. Não refazer a “primeira vez” com
a mesma pessoa para apagar a contaminação. Falhas técnicas e desistências ficam
visíveis no total recrutado; não selecionar apenas participantes que foram bem.

## Ficha de participante e sessão

| Campo | Registro |
|---|---|
| Rodada / participante / sessão / moderador | |
| Data, início/fim e fuso | |
| Experiência com jogos semelhantes e com a entrada usada | |
| Já conhecia este jogo, recebeu dicas ou viu outra sessão? | |
| Windows/Android, versão do SO, aparelho ou CPU/GPU/RAM | |
| Teclado / modelo do controle / toque; resolução e orientação | |
| Build ID, commit completo, Unity, Development ou produto | |
| Identidade do artefato: SHA-256 do APK ou GameAssembly.dll | |
| Application.buildGUID, se disponível; origem do dado | |
| Perfil inicial de save/relíquias; ID da cópia isolada | |
| Autorização de gravação e referências locais das evidências | |
| Interrupções técnicas, ajuda e alteração de condições | |

“Build ID” acima identifica o executável; “configuração” abaixo identifica as armas,
passivas e evoluções da partida. Não confundir os dois. GUID não exibido pelo jogo
deve ser marcado “não disponível”, sem inventá-lo. O hash identifica o artefato.
Não abrir nem sobrescrever saves pessoais para preparar a sessão.

## SC-001 — compreensão aos dois minutos

Perguntar, sem nomear a resposta esperada: “Mostre como você se movimenta”,
“O que muda entre essas partes do cenário?” e “O que essa ação faz e quando você
a usaria?”, apontando a ação Quack sem explicar seu efeito. Registrar a fala literal.

| Aspecto | Fala / comportamento observado aos 120 s | Sim / Não / Sem dado |
|---|---|---|
| Movimento: executa deslocamento intencional | | |
| Terreno: identifica água/terra e uma diferença real de mobilidade ou recuperação | | |
| Quack: aciona e explica um uso para controlar inimigos ou projéteis | | |

Tempo real: ____; tempo no jogo: ____; mortes/reinícios anteriores: ____.
Ajuda verbal, demonstração ou dica recebida antes dessa avaliação: ____.
Resultado individual: ____; justificativa/evidência: ____.
Sucesso requer **os três aspectos**, sem explicação verbal prévia do moderador.
Tutorial/texto do próprio jogo pode ser usado, devendo ser registrado.

## SC-002 — decisão sobre Fôlego

Após a janela SC-005: “Como decidiu quando usar suas ações? Conte um momento em
que escolheu agir ou esperar e por quê.” Só depois da resposta livre, perguntar
“Houve um momento em que decidiu usar ou guardar Fôlego? Qual?”

- Resposta literal: ____.
- Partida, instante e situação citados: ____.
- Escolha descrita (usar/guardar), alternativa considerada e motivo: ____.
- Relatou decisão consciente: Sim / Não / Sem dado: ____.
- Evidência do relato e eventual divergência da observação: ____.

O indicador mede o relato do participante. Um disparo observado, por si só,
não comprova decisão consciente. “Sim” sem exemplo fica sem evidência suficiente.

## SC-003 — dez partidas e configurações

Convenção operacional desta ficha: uma **escolha de configuração por partida**,
capturada no momento em que chega ao segundo miniboss, ou no término se não chegou.
São dez escolhas por participante. A assinatura canônica ordena IDs de armas,
passivas e receitas evoluídas; recupera a arma base de uma evolução para comparar
o conjunto. Só nível ou relíquia diferente não cria nova combinação. Registrar
níveis e relíquias separadamente. Esta é a unidade usada para “60% das escolhas”;
não interpretar essa taxa como frequência de cliques em cartas. Se a rodada adotar
outra unidade para esse termo, definir o denominador e a regra **antes** da coleta,
preservar os dados brutos e submeter a interpretação ao responsável pelo aceite.

“Chegou ao segundo miniboss” exige evidência do encontro **Sapo-Cururu**, solicitado
no minuto 10, com jogador vivo; relógio 10:00 sozinho não comprova o encontro.
Usar a configuração nesse checkpoint para as partidas que chegaram. Não usar uma
evolução obtida depois dele para contar diversidade nesse ponto.

| Partida | ID da tentativa / build ID / seed | Resultado e duração | Sapo-Cururu: sim/não + instante/evidência | Assinatura no checkpoint ou término | Níveis / relíquias / escolhas (refs.) |
|---|---|---|---|---|---|
| 01 | | | | | |
| 02 | | | | | |
| 03 | | | | | |
| 04 | | | | | |
| 05 | | | | | |
| 06 | | | | | |
| 07 | | | | | |
| 08 | | | | | |
| 09 | | | | | |
| 10 | | | | | |

Morte ou abandono voluntário são resultados válidos, mesmo sem chegar ao miniboss.
Crash, controle bloqueado ou intervenção técnica invalidam a tentativa para a série;
guardar seu registro, motivo e vínculo da repetição. Não substituir derrotas normais
por resultados melhores. Dez tentativas válidas são necessárias para concluir SC-003.

Copiar este registro para **cada oferta**, incluindo rerrolagem e pulo:

| Tentativa / instante / nível / offerId, se disponível | IDs/níveis oferecidos | Escolhida / banida / pulada / rerrolada | Build antes → depois / motivo espontâneo / evidência |
|---|---|---|---|
| | | | |

| Consolidação por participante | Valor |
|---|---|
| Dez partidas válidas completas? | |
| Assinaturas distintas que chegaram ao segundo miniboss (listar) | |
| Para cada assinatura, quantidade entre as dez escolhas (incluindo insucessos) | |
| Maior quantidade / 10 × 100 (dominância) | |
| Pelo menos três assinaturas chegaram e dominância ≤60%? | |

Avaliar por participante; não compensar um participante com o resultado de outro.
Comparar também as escolhas brutas para reconhecer receitas recorrentes em conjuntos
diferentes; a assinatura de conjunto não demonstra sozinha ausência de dominância
de uma receita compartilhada. Registrar essa limitação na conclusão do responsável.

## SC-004 — explicação de cada morte

Antes de replay ou explicação, perguntar: “O que aconteceu para a partida terminar?
Você percebeu algum sinal antes? Descreva qual e quando.” Não sugerir inimigo,
cor, som ou ataque na pergunta. Copiar a ficha para **cada morte observada**.

| Campo | Registro |
|---|---|
| Participante / sessão / tentativa / build ID / seed | |
| Instante da morte e referência de captura/observação | |
| Explicação literal do jogador | |
| Perigo e sinal anterior que ele identificou | |
| Evidência observada do sinal antes do dano; antecedência, se disponível | |
| Causa compatível com o relato? Sim / Não / Incerto | |
| Jogador explicou a morte por perigo previamente sinalizado? Sim / Não / Sem dado | |
| Ajuda, problema técnico ou discordância entre avaliadores | |

Sucesso exige relato de causa compatível **e** sinal anterior identificado pelo
jogador e sustentado pela observação. Sinal visto só pelo moderador não basta.
Mortes sem explicação ou sem evidência permanecem no denominador de mortes observadas.
Vitória e abandono não são mortes. Sem mortes observadas, SC-004 fica sem dados.

## SC-005 — repetição espontânea

| Primeira oportunidade natural de reinício, uma por participante | Registro |
|---|---|
| Tentativa, resultado e horário de término | |
| Horário de início/fim da observação no restante da primeira sessão natural | |
| Nova partida realmente iniciada? Horário / nova tentativa / seed | |
| Houve convite, instrução, recompensa externa ou reinício técnico? Qual? | |
| Ação/fala espontânea literal e referência de evidência | |
| Resultado: Sim / Não / Sem oportunidade válida | |

“Não” significa encerrar voluntariamente a sessão sem iniciar outra partida. Se
o moderador encerra a sessão antes de uma decisão, registrar observação incompleta.
Ausência do botão, crash ou reinício por orientação são impedimento/contaminação,
sem converter em reinício espontâneo. As partidas dirigidas restantes não contam
como novas oportunidades SC-005. Registrar quantas pessoas ficaram sem oportunidade.

## Consolidação da rodada

Preencher numerador, denominador, percentual **sem arredondar para aprovar**, faltantes,
exclusões justificadas e fontes. Manter resultados separados por build/plataforma/entrada;
agrupar somente condições comparáveis e preservar os subtotais. Definir antes da coleta
se a primeira experiência será analisada por entrada ou como coorte única.

| Critério | Numerador / denominador | Meta | Resultado / evidências |
|---|---|---|---|
| SC-001 | Participantes com os três aspectos sem ajuda / participantes avaliados na primeira experiência | ≥80% | |
| SC-002 | Participantes que relataram decisão consciente com exemplo / participantes entrevistados | ≥70% | |
| SC-003 | Por participante: ≥3 configurações no segundo miniboss; maior frequência entre dez escolhas | ≥3 e ≤60% | |
| SC-004 | Mortes explicadas com sinal prévio sustentado / todas as mortes observadas | ≥80% | |
| SC-005 | Participantes que iniciaram espontaneamente a segunda partida / participantes com oportunidade válida na primeira sessão | ≥60% | |

- Recrutados: ____; participaram: ____; concluíram: ____; motivos de saída: ____.
- Para cada SC: avaliáveis: ____; sem dado/contaminados: ____; motivos: ____.
- Participantes com dez partidas incompletas e quantidade restante: ____.
- Falhas técnicas e possíveis vieses de aprendizagem, fadiga, seed ou ofertas: ____.
- Divergências entre relato e observação, receitas dominantes e interpretação SC-003: ____.
- Evidências e revisão do responsável (nome de função/código, data): ____.
- Decisão de cada SC: Aprovado / Reprovado / Inconclusivo, com justificativa: ____.
- Ações corretivas e rodada de repetição planejada: ____.

Denominador zero, campos essenciais ausentes ou série incompleta não aprovam o SC.
Resultados com exclusões/contaminação devem trazer cobertura e análise do impacto;
não declarar aprovação geral só porque uma fração selecionada atingiu a meta.
O aceite técnico SC-006 a SC-008 continua na T075; esta ficha cobre SC-001 a SC-005.
