# Validação de T032

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T032 concluída. QuackFeedbackBridge conecta os eventos aceitos do combate a
um pool pré-aquecido de 32 PooledQuackFeedback, na família Vfx do PoolService. O prefab
contém QuackFeedbackController, LineRenderer e AudioSource. Ondas douradas usam o
alcance informado pelo evento; impactos são alaranjados, reflexões ciano, colisões
laranja e críticos recebem contorno branco em volta do sprite do alvo.

O contorno acompanha o alvo ativo e se desprende na última posição quando ele sai
da cena/pool, mantendo o breve feedback de morte. QuackTarget mantém um registro
de alvos ativos por ID para evitar buscas globais por evento; entradas são removidas
ao desativar e o registro é limpo na inicialização do runtime.

A apresentação lê eventos, não aplica dano, Fôlego ou regras de reflexão. Saturação
descarta o cosmético sem criar objetos fora do pool. A atualização dos efeitos ativos
é centralizada na ponte; as direções do círculo são calculadas uma vez. O callback
de devolução é preparado no aquecimento e reutilizado por cada instância.

QuackFeedbackController foi revisado: conclusão emitida uma vez; ResetEffect limpa
geometria, cor, áudio, callback e alvo seguido. Pausa suspende avanço visual e áudio;
retomada continua ambos. Estados terminais/desativação liberam os efeitos e removem
assinaturas. Reativação não duplica assinaturas. A API antiga de Play permanece
compatível com os scripts iniciados anteriormente, sem ativar o antigo MvpArena.

QuackFeedbackSetup gera pelo Unity o material do shader URP QuackRing, o prefab,
três tons WAV curtos (Quack, Impact, Reflection) e a ligação na cena Lake existente.
Os clipes são assets carregados pelo prefab/ponte, sem criar AudioClip no combate;
cada instância usa seu AudioSource com volume limitado e o limpa ao retornar.
O GUID fb86267c7de2e40479ccd6bec2e07d8a de Lake e os .meta existentes foram preservados.
Novos .meta e assets serializados foram gerados no Editor e copiados ao projeto.

QuackFeedbackTests acrescenta seis casos: conclusão única/reset; Quack real com
visual e clipe atribuído/retorno ao pool; contorno branco seguindo alvo sem alterar
vida; pausa e limpeza de áudio/geometria; saturação de 100 solicitações mantendo
32 instâncias e sem alterar vida/Fôlego; pulso ciano e clipe de reflexão importado
com amostras, sem consumir recursos. A regressão existente cobre dano, colisão,
reflexão e emissão única de eventos.

Etapa vermelha: dois testes falharam (conclusão repetida e ausência de feedback na
arena), código 2. Durante a suíte foi corrigida uma limpeza de cena vazia do teste,
que tentava criar uma cena com nome já existente.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 34 | 0 | 0 |

Execução na cópia temporária Temp/T022Validation, com batchmode, nographics, runTests
e testPlatform EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t032-red.xml, TestResults/t032-editmode.xml e TestResults/t032-playmode.xml.
Logs correspondentes: Logs/t032-red.log, Logs/t032-editmode.log e Logs/t032-playmode.log.
Geração: executeMethod QuackQuack.Editor.QuackFeedbackSetup.Build.

A prévia Logs/t032-preview.png foi renderizada com gráficos habilitados e inspecionada
em 1280×720: onda visível e contorno branco legível com o HUD. Ela usa exemplos de
efeitos e snapshot, não é captura de playtest humano. Os testes verificam integração
e configuração do áudio; não houve avaliação auditiva ou aceite perceptual dos tons.
Logs, XMLs, prévia e cópia temporária são regeneráveis e ficam fora do Git.

T022–T032 estão implementadas e verificadas dentro desses limites. Isso não aprova
SC-001–SC-005, experiência de gameplay, paridade de plataformas, builds ou desempenho;
esses aceites continuam dependentes de playtest e validações próprias. T033 em diante
permanecem pendentes. A alteração preexistente de ícones em
ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
