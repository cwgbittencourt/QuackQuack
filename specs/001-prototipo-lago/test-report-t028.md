# Validação de T028

Data: 2026-10-25. Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

Somente T028 concluída. PlayerController aceita IPlayerInput, RunState e GameplayEventBus
por Initialize, validando os dados do personagem e Quack antes de criar PlayerState.
Na inicialização normal da cena, Start conecta os serviços do Bootstrap e cria o
adaptador de teclado padrão. Adaptadores injetados pertencem ao chamador; o teclado
criado pelo controlador é liberado no encerramento. A inicialização é única por jogador.

Update converte a leitura em ApplyCommand; FixedUpdate aplica a velocidade ao Rigidbody2D,
sem gravar diretamente a posição. O corpo não recebe gravidade, mantém rotação fixa e
usa detecção contínua de colisão. A velocidade vem dos dados por terreno. Ausência de
volumes não impede o movimento em terra; a implementação dos volumes permanece na T029.

Suspensão da simulação limpa movimento e Quack pendentes e zera velocidade. O comando
de pausa não executa simultaneamente movimento/Quack; a retomada exige novos comandos.
Estados terminais, jogador morto e componente desabilitado rejeitam Quack. Desabilitar
zera velocidade; reabilitar descarta bordas acumuladas pelo adaptador. O cooldown usa
segundos de simulação física e não expira durante a pausa; Fôlego também não recupera
fora da simulação.

O controlador integra a T026: aplica os efeitos aceitos e solicita a emissão única pelo
QuackSystem, encaminhando Resolved ao event bus. As publicações duplicadas anteriores
foram removidas. Colliders do mesmo alvo compartilham sua identidade, inclusive por
GetComponentInParent. Reflexão exige a regra da QuackDefinition e usa ReflectionSpeed
autoral da T024. Interrupção só estende prazos existentes quando o efeito é positivo.

Foram acrescentados cinco testes PlayMode com GameObjects e física reais, comandos
canônicos injetados e definições temporárias:

- Movimento por comando com origem Touch, colisão com parede e parada ao soltar.
- Pausa/retomada e estado terminal sem consumir Fôlego ou executar comandos pendentes.
- Reflexão na velocidade autoral, dois colliders sem duplicação e cooldown congelado na pausa.
- Dano, crítico e interrupção únicos em alvo com dois colliders.
- Desabilitação para o corpo e descarta Quack acumulado antes da reativação.

Etapa vermelha: 14 testes PlayMode, 11 aprovados e três falhas na ausência do contrato
Initialize, código de saída 2. Os dois últimos casos foram acrescentados após essa etapa.
O .meta do novo script de testes foi gerado pelo Unity na cópia de validação.

| Suíte final | Aprovados | Falhas | Código de saída |
|---|---:|---:|---:|
| EditMode | 75 | 0 | 0 |
| PlayMode | 16 | 0 | 0 |

Execução em cópia temporária de Assets, Packages e ProjectSettings em Temp/T022Validation,
reaproveitada das tasks anteriores, com batchmode, nographics, runTests e testPlatform
EditMode/PlayMode, com acesso à licença local fora do sandbox.
XMLs: TestResults/t028-red.xml, TestResults/t028-editmode.xml e TestResults/t028-playmode.xml.
Logs: Logs/t028-red.log, Logs/t028-editmode.log e Logs/t028-playmode.log.
Resultados, logs e cópia temporária são regeneráveis e ignorados pelo Git.

Nenhuma cena ou asset de conteúdo serializado foi alterado. A alteração preexistente
de ícones em ProjectSettings/ProjectSettings.asset foi preservada e fica fora do commit.
Os testes demonstram integração automatizada, não aceite da arena jogável, build de
plataforma ou desempenho. O scheduler de inimigos e seus prazos de interrupção ainda
dependem da integração posterior; o MvpArena iniciado usa Time.time para esses prazos
e não foi alterado aqui. A pausa validada nesta task cobre o controlador e seu cooldown.
T029 em diante permanecem pendentes, incluindo montagem da arena em T030.
