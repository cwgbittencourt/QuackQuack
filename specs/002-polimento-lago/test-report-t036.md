# T036 — dicas por tempo de simulação

Data: 06/11/2026. Base: `058f967`. Escopo somente T036, FR-005.

## Implementação

ContextualHintsView lê RunState.ElapsedSeconds diretamente. Não cria contador próprio nem usa Time.time, deltaTime, relógio de parede ou tempo desde Bind/OnEnable. As instruções ficam visíveis somente durante simulação com tempo menor que 10 segundos e sem resultado terminal. Preparação, pausa, escolha de melhoria e encerramento limpam o texto, sem alterar a ativação do objeto da label. Pausa e escolha já suspendem o relógio no RunState existente.

Reativação, rebind da mesma partida, troca de fonte, mudança de perfil e troca de dispositivo não reiniciam a janela. Bind tardio aproveita apenas o tempo restante; saltar além de dez segundos encerra a apresentação. Uma nova RunState tem sua própria janela. Unbind/OnDisable/OnDestroy retiram o texto; destruir ou trocar o binding também remove a assinatura do perfil compartilhado.

O binding injetável recebe RunState e provedores de fonte/perfil, conforme os testes da T033. A sobrecarga de produção recebe RunState/fonte e acompanha ControlProfileStore.Shared, guardando sua cópia defensiva até o evento Changed. Assim, a leitura estável não copia o perfil a cada quadro. A view usa ControlProfile.Label com Gamepad.current para família automática, rótulos reais e remapeamentos, sem tabela paralela de botões. Texto só é reconstruído ao mudar fonte, dispositivo, família ou bindings. Depois da janela, os provedores deixam de ser consultados.

Teclado apresenta WASD/setas, Espaço, Esc e R, correspondentes ao inputactions atual; toque apresenta joystick e botões Quack/Pausar/Relíquias. A label não intercepta raycasts. Não há mudança em comandos, regras, balanceamento, save de progresso ou propriedade da pausa.

Esta task entrega o componente e seu contrato de binding. A composição autorada, posicionamento e ligação nas cenas/geradores são explicitamente T038; não foram antecipados. Não se declara que as dicas já estão presentes na build normal do produto. T037, T038, T039 e T040 continuam pendentes.

## Testes

As cinco falhas esperadas da T033/T035 passaram sem enfraquecer os contratos: fronteira exata 9,999→10, pausa/escolha, famílias/remapeamento/toque, reativação/rebind/nova partida e três resultados terminais.

Quatro casos adicionais verificam troca automática de dispositivo genérico para PlayStation em nove segundos; disable/re-enable e Unbind sem manipular ativação da label; binding tardio/salto de relógio/ausência de polling após expirar; binding de produção com mil atualizações aquecidas sem alocação gerenciada ou reconstrução de texto. O teste de pausa/escolha também exige texto efetivamente oculto nesses contextos.

| Execução | Resultado |
|---|---|
| Focada inicial — Focused.xml | 27/27 aprovados, zero ignorados, código 0 |
| Focada com cobertura adicional — FocusedR2.xml | 31/31 aprovados, zero ignorados, código 0 |
| EditMode completo — Final-EditMode.xml | 710/710 aprovados, zero falhas/ignorados, código 0 |
| PlayMode completo — Final-PlayMode.xml | 562/562 aprovados, zero falhas/ignorados, código 0 |

## Reprodução e limites

Unity 6000.4.3f1, cópia isolada ignorada `Builds/Polimento002Validation`, sem helper privado do laboratório T032 em Assets. `.meta` do novo componente gerado pelo Unity e preservado na raiz. Logs em `Logs/Polimento002/T036` e XMLs em `TestResults/Polimento002/T036`, não versionados. Nenhuma cena, prefab ou outro asset serializado foi alterado.

O teste de alocação mede somente o trecho de atualização aquecido no Editor, não desempenho global nem hardware. A troca de dispositivo é sintética. Não substitui aceite de entrada física, quatro proporções, arte, gameplay ou Android/Windows nativos. Esses aceites permanecem nas tasks correspondentes. Chance de cura e bônus de Sabão permanecem inalterados.
