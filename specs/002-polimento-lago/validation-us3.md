# T023 — Comparação de armas e passivas (US3)

**Data:** 05/11/2026. **Estado:** em validação; T023 permanece aberta. Unity 6000.4.3f1, cópia isolada em `Temp/Polimento002Validation`. Não representa aceite global de plataforma, desempenho ou partida de 15 minutos.

## Revisão aprovada durante o teste

O usuário aprovou Sabão somente em Bolhas, base 0,20, +16% de raio por nível até +80%. N0/N1/N5 têm raio 0,20/0,232/0,36 e diâmetro 0,40/0,464/0,72. Colisão e visual compartilham o atributo efetivo; dano, velocidade e aquisição permanecem independentes. Esta revisão substitui os 8%/40% dos relatórios históricos T015–T022; ver `decisions.md`.

O usuário relatou ausência de animação perceptível na arma Quack frontal. Foi acrescentado um contorno de setor nominal com alcance e ângulo autorados, duração visual de 0,25 s e opacidade dirigida pelo relógio de simulação. Pausa congela o efeito; desativação o esconde. O contorno não representa recorte por obstáculos nem modifica dano/oclusão; arte definitiva permanece fora desta task.

## Modificadores conferidos

| Passiva | Atributo | N1 / N5 | Comparação sem relíquias, arma N1 |
|---|---|---|---|
| Pena aerodinâmica | Velocidade de ataque | +8% / +40% | Bumerangue: intervalo 1,8 → 1,666667 → 1,285714 s; dano 3 |
| Ninho reforçado | Dano | +8% / +40% | Ovos em terra: 6,25 → 6,75 → 8,75; intervalo 3 s |
| Pulmão poderoso | Capacidade de Fôlego | +8% / +40% | 100 → 108 → 140; não aumenta dano do cone |
| Amuleto do lago | Recuperação de Fôlego | +8% / +40% | Terra: 12 → 12,96 → 16,8/s; água: 24 → 25,92 → 33,6/s |
| Óculos aviador | Dano | +8% / +40% | Foguetes: 7 → 7,56 → 9,8; intervalo 2,8 s |
| Sabão Mágico | Raio de acerto de Bolhas | +16% / +80% | 0,20 → 0,232 → 0,36; não altera outras armas |

Cada passiva tem cinco níveis. Os dois bônus de dano somam entre si: ambos N5 resultam em +80%, antes das relíquias. O intervalo divide pelos fatores de velocidade de ataque; bônus não é redução percentual direta do intervalo. Não há passiva autorada de movimento neste catálogo.

| Relíquia | Efeito autorado | Condição |
|---|---|---|
| Pena de combate | Dano +5% | Ambos terrenos |
| Apito veloz | Velocidade de ataque +5% | Ambos |
| Anel de ondas | Área +5% | Ambos; compõe o raio de Bolhas |
| Pena leve | Movimento +5% | Ambos |
| Botas do lago | Movimento +5% | Água |
| Garras de junco | Movimento +5% | Terra |
| Concha de fôlego | Recuperação +5% | Ambos |
| Lente de orvalho | Alcance +5% | Ambos |
| Pedra da margem | Dano +5% | Terra |
| Pérola do lago | Dano +5% | Água |

Composição: base autorada por terreno × (1 + soma de passivas pertinentes) × (1 + soma de relíquias pertinentes). Movimento base terra/água é 4/6; Pena leve com Botas e Garras dá 4,4/6,6. Concha dá recuperação 12,6/25,2 sem Amuleto. Sabão N5 com Anel dá raio 0,378. Ovos e Incubadora têm dano ×1,25 em terra; Bolhas ×1,25 na água. As demais afinidades de dano e todas as afinidades autoradas de intervalo são neutras.

As três receitas preservam a passiva: Bumerangue → Tornado de penas (Pena), Ovos → Incubadora de combate (Ninho), Quack frontal → Quack supersônico (Pulmão). A passiva continua afetando o atributo pertinente após a evolução.

Bases autoradas sem passivas/relíquias; dano em terra (água entre parênteses quando diferente). Evoluções N5 abaixo são referência numérica sem o bônus da passiva, não uma build obtível sem cumprir a receita.

| Arma | Dano N1 → N5 | Intervalo N1 → N5 (s) | Alcance N1 → N5 | Área radial N1 → N5 |
|---|---|---|---|---|
| Bumerangue | 3 → 5,4 | 1,8 → 1,363636 | 6 → 6,6 | 0 |
| Ovos | 6,25 → 11,25 (5 → 9) | 3 → 2,272727 | 4 → 4,6 | 1,3 → 1,82 |
| Quack frontal | 3 → 5,4 | 1,6 → 1,212121 | 4 → 4,6 | 1 → 1,4 |
| Aura | 2 → 3,6 | 1,4 → 1,060606 | 3 → 3,6 | 2 → 2,8 |
| Foguetes | 7 → 12,6 | 2,8 → 2,121212 | 8 → 8,6 | 1,5 → 2,1 |
| Bolhas | 2 → 3,6 (2,5 → 4,5) | 1,2 → 0,909091 | 6 → 6,6 | 0; usa HitRadius 0,20 |
| Tornado de penas N5 | 14,4 | 0,909091 | 8,6 | 0 |
| Incubadora de combate N5 | 27 (21,6) | 1,515152 | 5,6 | 3,08 |
| Quack supersônico N5 | 18 | 0,681818 | 7,6 | 2,8 |

Velocidades de projétil: Bumerangue 7, Tornado 8, Foguetes 5, Bolhas 3. O campo autorado de Ovos/Incubadora e ataques de área é 1; ataques instantâneos não passam a viajar por esse número. HitRadius base é 0,20 em todas as definições, mas ataques de área/cone seguem sua geometria própria.

## Evidência automatizada

| Execução | Resultado confirmado | Limite |
|---|---|---|
| EditMode, revisão Sabão/efeito frontal | 584/584 aprovados, zero falhas ou ignorados | Regras e contratos do Editor |
| PlayMode, revisão Sabão/efeito frontal | 460/460 aprovados, zero falhas ou ignorados | Integração no Editor |
| Matriz numérica final | 2.100 projeções finitas/válidas | Diagnóstico, não 2.100 testes de gameplay |
| Android nativo, versão anterior do balanceamento | 95/95 aprovados na repetição R2 | Não comprova revisão final de Sabão/efeito frontal |
| Android nativo IL2CPP no S23, revisão R3 | 97/97 aprovados, zero falhas/ignorados, código de saída 0 | Integração executada no aparelho; aceite visual separado |

XMLs locais: `TestResults/Polimento002/T023/final-EditMode.xml`, `final-PlayMode.xml`, `android-tests-r2.xml`. Matriz: `Logs/Polimento002/T023/modifiers.csv`; versão anterior preservada em `modifiers-before.csv`. SHA-256 da matriz final: `FAD5D656CAFE0E8C38DF919DF41954EF8E654182A315E77461406909183BB82E`. Artefatos regeneráveis não são versionados.

## Builds da revisão R3

Builds normais IL2CPP, sem o script privado de laboratório nos Assets durante a compilação, com cenas do pipeline de produção e BuildOptions.None:

| Plataforma | Artefato local na cópia isolada | SHA-256 |
|---|---|---|
| Windows | `Builds/T023R3/Windows/GameAssembly.dll` | `0F410DA1374131DA4A77090A98BCA0BC848F4D3DCF59CA56C571B6737BD7B78C` |
| Android | `Builds/T023R3/Android/QuackQuackT023.apk` | `83E920E45B5232C82CA41C15FAA69F1F443F7E9ADD8F7DDDBC91636CEC257ED6` |
| Laboratório Windows | `Builds/T023LabR3/Windows/GameAssembly.dll` | `F5B9FD9B140060314B7CC16CD79CF64407E067F865BA74EA54D3E88B2C32D103` |
| Laboratório Android | `Builds/T023LabR3/Android/QuackQuackT023LabR3.apk` | `BD04CF631A50795A417A623E0341A64CBE857CBE73DAC320E609D1D8E3DE0146` |

Pacote normal Android: `com.cemggames.quackquack.validation002.t023`; laboratório: `com.cemggames.quackquack.validation002.t023lab`. As quatro builds R3 terminaram com código zero. A instalação R3 normal no S23 (SM-S918B) foi concluída sem limpar dados. Inicialização Windows observada com Pato, ondas, ataques e HUD. Solicitada conferência de movimento, Quack, pausa e escolha/progressão, o usuário respondeu “parece tudo normal”; não informou separadamente cada etapa. Comparações controladas R3 ainda precisam dos respectivos resultados.

A primeira execução Android apresentou uma falha de sincronização do teste de movimento: a confirmação de melhoria limpa comandos, e o teste amostrava FixedUpdate antes do próximo Update. A fixture agora verifica velocidade zero ao fechar e espera uma atualização antes da física; não houve alteração do controlador de movimento. A repetição nativa passou. Os dois novos testes do feedback frontal falharam pela ausência do efeito antes da implementação e passaram na suíte final; cobrem ângulos 60°/120°, alcance, dano preservado, pausa, término, repetição e desativação.

## Comparação jogável recebida

Laboratório usa cenas, catálogo, resolução, física, projéteis, cartas e runner reais, com ondas/cronômetro de partida desativados, alvos sintéticos e botões diagnósticos. Uma emissão usa instrumentação para impedir novas emissões após a primeira. Cada repetição recria o caso; não serve para provar ausência de recarga gratuita no fluxo normal ou desempenho. Pacote/save do laboratório separado do jogo normal.

| Caso Windows, laboratório anterior à revisão | Relato do usuário |
|---|---|
| Bumerangue N1, uma emissão, Pato parado | Um hit em A e B na ida e outro na volta; retorno visível |
| Pena / Ninho / Óculos em N0/N1/N5 | Números e ataques funcionam corretamente |
| Bolhas e prévia com Sabão anterior | Funcionam; solicitou dobrar somente o bônus, posteriormente esclarecido em 16%/80% |
| Aura com parede, limite e pausa/retomada | Funciona corretamente |
| Pulmão e Amuleto, capacidade/recuperação | Passivas funcionam; ataque frontal sem animação visível |

## Pendências para encerramento

- Retestar visual/colisão/prévia de Sabão na revisão 16%/80% e contorno do ataque frontal no Windows.
- Confirmar as três evoluções e composição com relíquias/terrenos em gameplay.
- Detalhar o fluxo normal de escolha/progressão no Windows caso não tenha sido coberto pelo retorno geral “parece tudo normal”.
- Repetir os cenários físicos no S23; testes nativos finais Android já passaram. Laboratório R3 instalado e launcher iniciado por ADB; a aparência e a interação aguardam confirmação do usuário.
- Registrar os retornos físicos R3, mantendo T023 aberta até o aceite necessário. Configuração Windows para player de testes preparada com código zero; isso não constitui execução de testes nativos Windows.

T014 permanece com a validação 8BitDo/S23 adiada por solicitação do usuário. T024 não foi iniciada.
