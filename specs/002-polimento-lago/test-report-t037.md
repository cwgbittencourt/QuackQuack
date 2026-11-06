# T037 — barras individuais de inimigos especiais

Data: 06/11/2026. Base: `8f3dec9`. Escopo somente T037, FR-012.

## Implementação

SpecialEnemySnapshot é uma projeção readonly por valor no Core: instância, lease, encontro, nome, categoria, âncora mundial e fração de vida. Rejeita valores inválidos/não finitos; fração usa o máximo efetivo, inclusive escala de encontro, sem expor referências a atores. EnemyState só projeta a lease viva esperada. FrogKingController agora possui lease monotônica por aquisição e a limpa em release/reset; só projeta após inicializar o encontro e enquanto vivo/ativo.

SpecialEnemyRegistry usa array fixo, rejeita identidade duplicada e overflow, e é reconstruído sem novas coleções. RunDirector dimensiona sua capacidade pela soma dos máximos dos pools de minibosses mais uma vaga de chefe: três no catálogo atual. Lê somente atores vivos do SpawnDirector, classifica os dois minibosses pelos IDs autorados e inclui o chefe ativo do pool. Inimigos comuns ficam de fora. O refresh ocorre também em pausa, no LateUpdate; desativação do diretor e resultado terminal limpam o registro. Falha de capacidade/identidade é explícita, sem supressão silenciosa.

SpecialEnemyBarsView recebe registro, câmera mundial e container uGUI de área segura por Bind. Pré-cria as barras conforme a capacidade na composição e reutiliza seus objetos. Cada barra apresenta nome/categoria e vida sem número, com preenchimento por âncoras (sem depender de sprite). Vida e posição são atualizadas; texto é reconstruído ao mudar identidade/lease/nome/categoria. Elementos não capturam raycasts.

A projeção exige âncora à frente da câmera, no viewport e dentro do container. Barras próximas recebem faixas verticais acima de suas próprias âncoras, sem sobreposição dos painéis. Linhas individuais ligam a posição ajustada à âncora e são desenhadas atrás de todos os painéis. Não há painel agregado nem barra de alvo fora da tela deslocada sobre outro alvo. Se não houver espaço para o painel inteiro, ele é ocultado e contabilizado em HiddenForSpace, em vez de cortar/sobrepor ou inventar outra posição. A matriz visual ampliada e o aceite desses limites continuam T039/T040.

Unbind/desativação ocultam as views; destruição libera os objetos pertencentes ao componente, mesmo quando criados sob container separado. Não há Instantiate/Destroy durante atualização estável. Nenhuma regra de dano, encontro, recompensa, entrada, cura ou Sabão foi alterada.

O registro está integrado ao diretor de produção. A criação/ligação da view nas cenas e geradores permanece T038; esta entrega não declara barras já presentes na build normal nem altera cenas/prefabs serializados.

## Testes e revisão

SpecialEnemyBarsTests não depende de UnityEditor nem é excluída dos players. Inclui projeção de vida máxima escalada/lease antiga/morte/reuso; registro limitado/duplicação/default; reset visual de nome/categoria/vida por lease; três âncoras coincidentes; movimento de câmera/fora da tela/atrás da câmera; insuficiência de espaço; reuso estável/disable/unbind; alocação do registro; reaquisição real do pool de chefe; valores inválidos; captura de apresentação; e integração na LakeRun real com dano, pausa, desativação, morte, reuso, ambos os minibosses, chefe e vitória.

| Execução | Resultado |
|---|---|
| Focused.xml | 9/10 aprovados: fixture de espaço insuficiente incorreta |
| FocusedR2.xml | 12/12 aprovados, zero ignorados, código 0 |
| EditMode completo final — Final-EditMode.xml | 710/710 aprovados, zero falhas/ignorados, código 0 |
| PlayMode completo final — Final-PlayMode.xml | 578/578 aprovados, zero falhas/ignorados, código 0; 16 casos da nova classe |

A fixture inicial reservava somente 60 unidades de altura centradas na âncora e esperava uma barra cujo topo ficava a 52 unidades acima dela. O resultado correto era zero. A fixture foi corrigida para 120 unidades (limite superior 60), permitindo uma barra e ocultando a segunda. Não se alterou a câmera/viewport para mascarar a falha.

A captura sintética inicial revelou linhas atravessando textos dos painéis inferiores. O tether foi movido para trás dos painéis e o teste exige essa ordem de desenho. A captura final foi inspecionada: três nomes legíveis, painéis sem sobreposição, proporções de vida distintas e linhas atrás dos textos/painéis. O cenário de captura usa três especiais coincidentes e vidas 25%, 60% e 100%, não uma partida nativa. A cópia local `overlapping-specials-final.png` preserva a captura final para consulta sem cache de prévia.

## Reprodução e limites

Unity 6000.4.3f1, cópia isolada ignorada em `Builds/Polimento002Validation`, sem helper privado do laboratório T032 em Assets. Metas dos três scripts novos geradas via Unity e preservadas na raiz. XMLs em `TestResults/Polimento002/T037`, logs/captura `overlapping-specials.png` em `Logs/Polimento002/T037`; artefatos regeneráveis não versionados.

O teste de alocação mede somente mil reconstruções aquecidas do registro no Editor, não FPS/desempenho global. Os testes são portáveis, mas não houve execução nativa Windows/Android nesta task. Integração no Editor e captura sintética não encerram aceite jogável, físico, artístico, desempenho ou matriz completa. T038–T040 e validações transversais permanecem pendentes.
