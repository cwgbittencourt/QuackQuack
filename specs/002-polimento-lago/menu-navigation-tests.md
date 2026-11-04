# Regressões de navegação e pausa dos menus — T009

**Data**: 04/11/2026. **Base**: c96d516 (T008).
**Escopo**: testes antes da implementação de FR-014/015, sem corrigir código de produção.
Arquivos: RelicLoadoutFlowTests.cs e UpgradeSelectionTests.cs, preservando os .meta.

## Cobertura adicionada

24 casos novos: **13 em relíquias e 11 em seleção de melhorias**. Preservados os nove
casos anteriores dessas duas classes. A cena Bootstrap/Lake, prefabs, dados, bridge,
LakeRelics, UpgradeSystem e EventSystem reais são usados, sem métodos de produto inventados
ou campos privados acessados por reflexão.

| Área | Casos novos | Exigência observável |
|---|---:|---|
| Foco inicial de relíquias | 1 | Seleção ativa/interativa dentro do painel, com sinal visual de foco |
| Cancel de relíquias | 2 | East fecha e restaura foco; retoma somente pausa adquirida pelo menu |
| Fechamento por botão | 2 | Devolve foco ao botão anterior, partindo de gameplay ou pausa |
| Propriedade da pausa | 2 | Repetir Show(true) não perde ownsPause; fechar não retoma pausa preexistente |
| Atalho de relíquias | 2 | Select/View abre/fecha uma vez por pressão e preserva propriedade da pausa |
| Contexto terminal | 1 | Atalho não abre painel nem tira partida do estado terminal |
| Limite de três relíquias | 1 | Foco se recupera quando o item selecionado fica indisponível |
| Confirmar relíquia | 1 | South alterna uma vez por pressão, sem Quack por trás do menu |
| Grafo de relíquias | 1 | Ações desbloqueadas e VOLTAR alcançáveis por direções, sem destino inválido |
| Oferta obrigatória e Cancel | 4 | East em escolher/banir/rerrolar/pular não escolhe, consome recurso ou pula implicitamente |
| Retorno da ação secundária | 1 | Cancel em Banir retorna à carta sem resolver a oferta |
| Grafo da seleção | 2 | Uma/três cartas, ações de recursos e PULAR alcançáveis, sem escapar do modal |
| Rerrolar pelo controle | 1 | Uma pressão consome uma unidade, troca oferta e mantém foco válido |
| Recursos esgotados | 1 | Grafo ignora rerrolagem/banimento indisponíveis e mantém foco |
| Confirmar melhoria | 1 | Aplica uma escolha, retorna à simulação sem Quack herdado do Submit |
| Retorno após escolha | 1 | Foco anterior disponível é restaurado ao sair da seleção |

A oferta obrigatória é criada por TryCollectExperience(10), no fluxo real de subida de
nível. Os quatro casos conferem OfferId ainda aberto, fase ChoosingUpgrade, histórico,
confirmações, recursos e contador OfferResolved intactos após Cancel. Esses testes
protegem a regra mesmo quando a navegação/Cancel forem implementados depois.

## Entrada, foco e isolamento

Helper compartilhado de teste MenuRegressionInput no arquivo RelicLoadoutFlowTests.cs.
Injeta GamepadState no Input System e deixa InputSystemUIInputModule despachar os eventos
por frames reais. South/East/Select não são substituídos por chamadas falsas a handlers
ou listeners. Duas frames de botão segurado verificam ação única por borda. As chamadas
diretas ao botão Close/Show/Toggle são usadas nos casos isolados de pausa/foco, sem
confundi-las com prova do atalho ou do Cancel físico.

Gamepad genérico sintético: não é aceite físico Xbox/PlayStation/genérico nem de glifos.
Teclados/gamepads preexistentes são desabilitados somente no InputSystem do runner e
restaurados em teardown, junto a currents e políticas de foco. Scene/bootstrap são
limpos mesmo quando as asserções falham. Relógio do bootstrap é externo nos casos novos
para evitar avanço acidental; o desbloqueio do Apito é provocado explicitamente aos 60 s.

O grafo é inspecionado por FindSelectableOnUp/Down/Left/Right: exige alcançar todas as
ações disponíveis, sem saltar para ações ocultas, desabilitadas ou fora do modal. Não
obriga uma técnica particular de implementação. Foco válido também exige feedback de
seleção configurado (cor/sprite/animação); não substitui revisão visual de contraste.

## Primeiro ensaio focado

33 descobertos: **23 aprovados, 10 falhas, zero ignorados**, exit 2, em
TestResults/Polimento002/T009/focused.xml. Todas as dez falhas são dos casos novos:

| Falha | Casos |
|---|---:|
| Cancel não fecha relíquias (gameplay/pausa preexistente) | 2 |
| Close deixa foco no controle oculto em vez de restaurar o anterior | 2 |
| Abrir relíquias mantém foco fora do modal | 1 |
| Atalho Select/View não abre relíquias | 2 |
| Limite de equipadas deixa foco nulo, sem recuperação | 1 |
| Cancel em Banir não retorna à carta | 1 |
| Resolver melhoria deixa foco nulo | 1 |

**Os quatro testes de Cancel sem pulo implícito passaram**, assim como propriedade da
pausa, grafos observados, ação única e ausência de Quack nos ensaios de Submit. Estes
casos verdes devem continuar passando; não implementar Cancel chamando PULAR.

Nenhum erro de compilação, setup ou teardown foi contado como falha de menu.
Logs/XML em Logs/Polimento002/T009 e TestResults/Polimento002/T009, ignorados.

## Suíte completa e conclusão

**343 descobertos, 318 aprovados, 25 falhas, zero ignorados**, Unity exit 2, em
TestResults/Polimento002/T009/PlayMode.xml. As falhas são **15 já documentadas na T008 +
10 novas da T009**. Todos os 304 casos que passavam na rodada anterior continuam passando,
incluindo os nove testes antigos destes menus. Nenhuma falha fora das classes previstas.

| Fixture | Total | Aprovados | Falhas |
|---|---:|---:|---:|
| ControllerAvailabilityTests (T008, inalterada) | 24 | 9 | 15 |
| RelicLoadoutFlowTests (3 antigos + 13 novos) | 16 | 8 | 8 |
| UpgradeSelectionTests (6 antigos + 11 novos) | 17 | 15 | 2 |

T009 entrega **24 regressões novas: 14 verdes e 10 vermelhas antes da implementação**.
Não foram usados Ignore, Explicit ou expectativas que transformem erro esperado em sucesso.

Fonte da raiz e da cópia isolada Temp/Polimento002Validation com hashes SHA-256 idênticos:

| Arquivo | SHA-256 |
|---|---|
| RelicLoadoutFlowTests.cs | FBE60B54D37289890E04F035983A027359CFA81B33D4A31E0216AAF5E498B496 |
| UpgradeSelectionTests.cs | A9E2787A29B23570C9CA77847D8733FF4FFB064107294441CF4A17A013F83357 |

Repetir em Unity 6000.4.3f1 com -batchmode -projectPath <copia> -buildTarget Win64
-runTests -testPlatform PlayMode -testFilter "RelicLoadoutFlowTests;UpgradeSelectionTests"
-testResults <xml> -logFile <log>, sem -quit. Conferir 33 casos para o filtro ou 343
para a suíte completa desta versão, além dos IDs das falhas novas/anteriores.

Esta rodada é de **Editor PlayMode**. O guard UNITY_EDITOR herdado de UpgradeSelectionTests
foi preservado; não alegar execução dessa fixture nos players. Não houve nova build,
teste Android, aceite físico de controle/toque, profiling ou suíte EditMode completa.

T009 concluída como task de testes anteriores à correção. T010 deve introduzir ações/
bindings comuns, T011 tratar disponibilidade T008 e T012/T013 completar perfis/menus.
Em T013, manter os testes positivos de propriedade da pausa e Cancel sem pulo implícito,
além de corrigir foco, retorno, atalho e Cancel de relíquias. T014 ainda precisa do aceite
físico. Nenhum código de produção, cena, asset, binding ou .meta foi alterado na T009.
