# Validacao de T018 a T021

Data: 2026-10-25. Editor: Unity 6000.4.3f1 (39d1a88d4dd1), Windows.

## Escopo

Somente os testes T018, T019, T020 e T021 foram revisados e concluidos neste incremento.
Os arquivos de gameplay iniciados no incremento anterior foram usados como dependencias existentes;
as tarefas T022 em diante continuam pendentes. Este relatorio nao representa aceite do MVP jogavel,
de uma build Windows/Android ou da matriz final de T075.

| Task | Fixture | Casos | Resultado |
|---|---|---:|---|
| T018 | BreathStateTests | 8 | Aprovado |
| T019 | QuackRulesTests | 3 | Aprovado |
| T020 | TerrainAndKnockbackTests | 2 | Aprovado |
| T021 | KeyboardPlayerInputTests | 2 | Aprovado |
| Regressao da fundacao | RunStateTests, PlayerCommandTests, PoolServiceTests, BootstrapTests | 11 | Aprovado |
| Total | EditMode + PlayMode | 26 | 0 falhas |

## Cobertura

- T018: limites de Folego, rejeicao de custos invalidos, recuperacao em agua/terra,
  ativacao parcial que conclui ao zerar, rejeicao da ativacao seguinte e janela de invulnerabilidade.
- T019: dano normal/aprimorado, critico, empurrao, interrupcao, reflexao de projetil leve,
  direcao/velocidade refletida e deduplicacao por onda/alvo, inclusive colisao.
- T020: histerese na margem, afinidade por terreno e valor neutro, empurrao bloqueado pela fisica,
  afastamento posterior do obstaculo e concessao unica da recompensa. A montagem da rota de risco
  na arena permanece em T030; aqui a recompensa verifica o contrato de concessao unica.
- T021: WASD e setas geram o mesmo comando diagonal normalizado; Espaco produz uma unica borda
  por acionamento e o mesmo consumo de Folego de um comando canonico; soltar para o movimento;
  novo acionamento e borda unica de pausa.

## Execucao e evidencias

Os testes foram executados pelo Unity Test Runner em batchmode, com os argumentos abaixo
acrescentados ao executavel instalado do Unity:

```text
-batchmode -nographics -projectPath E:\CemgGames\QuackQuack -runTests -testPlatform EditMode -testResults E:\CemgGames\QuackQuack\TestResults\t018-t021-editmode.xml -logFile E:\CemgGames\QuackQuack\Logs\t018-t021-editmode.log
-batchmode -nographics -projectPath E:\CemgGames\QuackQuack -runTests -testPlatform PlayMode -testResults E:\CemgGames\QuackQuack\TestResults\t018-t021-playmode.xml -logFile E:\CemgGames\QuackQuack\Logs\t018-t021-playmode.log
```

EditMode: 17 aprovados. PlayMode: 9 aprovados. Ambos encerraram com codigo 0.
XML e logs sao evidencias locais regeneraveis, excluidas do Git pelas regras do projeto.

A etapa vermelha anterior esta registrada em `Logs/mvp-red.log`: compilacao falhou pela ausencia
das regras verificadas antes da implementacao iniciada no incremento anterior.

Na primeira execucao PlayMode deste incremento, os dois casos de teclado falharam com movimento
zero por falta de foco da Game View no batchmode. A fixture passou a encaminhar entrada sintetica
ao jogo durante o teste e restaura as configuracoes e remove o dispositivo em `finally`.
O codigo de entrada do jogo nao foi alterado neste incremento.

Ha avisos de API obsoleta (`GetInstanceID`) nos arquivos de gameplay previamente iniciados.
Eles nao impediram a compilacao e permanecem para revisao nas tarefas de implementacao correspondentes.
