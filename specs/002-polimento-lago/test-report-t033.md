# T033 — contratos de progresso, recursos e dicas

Data: 06/11/2026. Base: `970dd4d`. Escopo restrito à entrega de testes da T033; T034–T036 não implementadas.

## Cobertura

`ExperienceStateTests.cs` preserva os oito casos anteriores e adiciona 12:

- XP 0/5/10/25/30/45/60/99 numa curva cumulativa irregular 10/30/60. Confere nível, XP, limiar anterior/próximo e fração do intervalo, com barra cheia no último limiar e após excedê-lo.
- Curva vazia já completa, sem divisão por zero; inteiro máximo, rejeições sem mutação e cópia da autoria.
- Snapshot por valor que não muda após novas recompensas. `UpgradeSystem` projeta todas as subidas antes de resolver ofertas obrigatórias, sem mudar a fração ao confirmar cartas.

`GameplayHudTests.cs` acrescenta 20 casos e atualiza um contrato legado incompatível com FR-004:

- Sete combinações de recursos: vazio, parcial, cheio, excesso, negativo, máximo zero e máximos efetivos 200/140. Projeção normalizada e renderização real das duas imagens são verificadas separadamente; renderizar não altera os dados da instância.
- Caminho real PlayerController → UpgradeSystem → HudSnapshot → GameplayHud: 35 XP atravessam dois limiares, mostra nível 3/fração 1/6; ao alcançar 60, mostra nível 4/barra cheia. Não há nova curva calculada pela fixture de UI.
- Dicas ausentes em Preparing, presentes no início/9,999 s e ausentes exatamente aos 10 s de simulação.
- Pausa e escolha obrigatória não consomem janela; visibilidade durante o modal não é imposta, mas o estado restante deve reaparecer ao retomar e terminar no mesmo limiar.
- Teclado, Xbox, PlayStation, genérico, remapeamento de Relíquias e toque atualizam instruções sem estender o prazo. Reativar/rebind da mesma partida não reapresenta dicas expiradas; uma partida nova recebe sua própria janela. Derrota, abandono, vitória e resultados não mantêm dicas de início.

O antigo teste exigia números 100/100 e texto de terreno no HUD. Agora exige ausência desses conteúdos visíveis, mantendo a verificação de barras distintas, Fôlego cheio, prontidão do jogador e dano real 100→75 refletido em 0,75 de vida. Labels legados podem ser removidos ou ocultados; os testes não exigem preservá-los como elementos visíveis. O teste anterior de mensagens de recarga, Fôlego insuficiente, pausa e derrota permanece.

## Fronteira testável para as próximas tasks

Não foram criados snapshots, views ou implementações substitutas para deixar testes verdes. Reflexão verifica membros reais e falha explicitamente quando ausentes, permitindo compilar/descobrir a fase vermelha antes da implementação:

| Fronteira da fixture | Contrato exercitado |
|---|---|
| `ExperienceState.Progress`, `UpgradeSystem.Progress` | `QuackQuack.Core.Shared.ProgressSnapshot` por valor, com Level, Experience, PreviousThreshold, NextThreshold e Fraction |
| `HudSnapshot.Progress`, `Health01`, `Breath01` | Projeções da regra, com recursos limitados a [0,1] e capacidade efetiva |
| Bindings de GameplayHud | `experienceFill`, `levelLabel`, além das referências existentes de recursos |
| `ContextualHintsView` | Text serializado `label`; `Bind(RunState, Func<InputSource>, Func<ControlProfile>)`; IsVisible e texto efetivamente oculto/limpo ao expirar |

Esses nomes são a fronteira dos testes para T034–T036, não novos números de balanceamento. Eventuais adaptações mecânicas de assinatura devem preservar todos os cenários. Limiares no teto são iguais ao último valor (ou zero na curva vazia), com Fraction=1; não se inventa um próximo nível.

## Execuções e diagnóstico

Unity 6000.4.3f1, cópia isolada ignorada `Builds/Polimento002Validation`, alvo Editor Windows. O helper privado do laboratório T032 foi retirado de Assets da cópia antes dos testes, evitando interferir nos cenários LakeRun. Arquivos `.meta` existentes dos dois scripts preservados; nenhum asset/cena foi regenerado na raiz.

As duas primeiras tentativas PlayMode ficaram sem XML. A limpeza antiga sempre descarregava a cena ativa, mesmo quando os novos casos não haviam carregado a arena. A fixture passou a limpar cenas somente quando ela própria chamou LoadArena; objetos próprios continuam sendo destruídos em todos os casos. A repetição então concluiu, sem alterar código do produto. Tentativas interrompidas não são contabilizadas como testes aprovados/reprovados.

| Execução | Total | Aprovados | Falhas esperadas | Ignorados |
|---|---:|---:|---:|---:|
| ExperienceStateTests focada — Experience.xml | 20 | 8 | 12 | 0 |
| GameplayHudTests focada após corrigir teardown — HudR3.xml | 20 | 7 | 13 | 0 |
| EditMode completa — Final-EditMode.xml | 698 | 686 | 12 | 0 |
| PlayMode completa — Final-PlayMode.xml | 551 | 538 | 13 | 0 |
| GameplayHudTests final, incluindo máximos 200/140 — HudFinal.xml | 22 | 8 | 14 | 0 |

As suítes completas rodaram antes de acrescentar duas instâncias de TestCase para os máximos 200/140 (uma para projeção e outra para renderização). A classe final de 22 casos foi reexecutada depois dessa alteração de dados. Não houve alteração de lógica/fixture ou produção após as suítes completas. A instância de renderização adicional passou; a de projeção falhou pelo mesmo membro Health01 ausente. Todos os processos que concluíram retornaram código 2 por falhas de asserção esperadas, sem erro de compilação.

As 12 falhas EditMode são 11 leituras de ExperienceState.Progress e uma de UpgradeSystem.Progress. As 14 falhas finais PlayMode são sete projeções Health01 ausentes, uma HudSnapshot.Progress ausente, cinco pela ausência de ContextualHintsView e uma pelo HUD ainda mostrar VIDA 100/100. Nenhuma falha fora das duas classes da T033 nas suítes completas.

XMLs ficam em `TestResults/Polimento002/T033`, logs em `Logs/Polimento002/T033`, fora do commit. Falhas esperadas devem permanecer visíveis, sem Ignore/Explicit/condição que as esconda. As verificações posteriores ao primeiro membro ausente só poderão passar pela execução completa quando a implementação correspondente existir; esta entrega não alega que o comportamento novo já funciona.

T033 é concluída como task de testes antes da implementação. As falhas de projeção são tratadas em T034, HUD em T035 e dicas em T036; composição/geradores, layout e aceite jogável permanecem em T038–T040. Não houve build/test player, aceite físico/visual ou medição de desempenho. Pedidos de balanceamento de cura/Sabão e T014 permanecem independentes e não foram executados.
