# Implementation Plan: Prototipo Jogavel do Lago

**Branch**: `001-prototipo-lago` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-prototipo-lago/spec.md`

## Summary

Construir um vertical slice de 15 minutos de Quack-quack Survive no bioma Lago, com o Pato,
movimentacao em agua e terra, Quack/Folego, progressao por cartas, seis armas, seis passivas, tres
evolucoes, cinco inimigos, dois minibosses, Sapo-Rei, dez reliquias e tela final. A solucao separa
regras C# testaveis de adaptadores Unity, usa ScriptableObjects para conteudo, Input System para
comandos comuns e pools centralizados para entidades de alta rotatividade.

## Technical Context

**Language/Version**: C# suportado pelo Unity 6000.4.3f1, fixado para todo o prototipo

**Primary Dependencies**: Unity 6000.4.3f1, Universal Render Pipeline com 2D Renderer, Unity Input
System, uGUI, Unity Test Framework, UnityEngine.Pool, Unity Profiler e Memory Profiler

**Storage**: ScriptableObjects para conteudo autoral; arquivo JSON versionado por schema para
preferencias, desbloqueios, reliquias, recordes e progresso local

**Testing**: NUnit via Unity Test Framework em EditMode; testes PlayMode para integracao de cena,
entrada, UI e ciclo de partida; testes manuais instrumentados em Windows e Android reais

**Target Platform**: Windows 10+ x64 e Android ARM64; arquitetura preparada para adaptador iOS futuro

**Project Type**: Jogo 2D multiplataforma em um unico projeto Unity

**Performance Goals**: 60 FPS no Windows e celular intermediario; 30 FPS estaveis em celular de
entrada; pico validado de 400 inimigos ativos

**Constraints**: Paisagem; UI 1920x1080 por referencia, Safe Area, 16:9/18:9/19,5:9/4:3; sem
alocacoes recorrentes no combate; mesma simulacao para teclado, controle e toque; operacao offline

**Scale/Scope**: Um bioma, um personagem, partida de 15 minutos, 6 armas, 6 passivas, 3 evolucoes,
5 inimigos, 2 minibosses, 1 chefe, 10 reliquias e 200-400 inimigos simultaneos

**Elenco fechado**: Formiga (pressao por quantidade), Aranha (teias), Vespa (investida), Besouro
(armadura frontal) e Caramujo Aquatico (trilha lenta). Libelula Alfa no minuto 5 testa esquiva e
reposicionamento; Sapo-Cururu no minuto 10 testa controle de area; Sapo-Rei encerra a partida.

## Constitution Check

*GATE inicial: aprovado. Reavaliado apos o design da Fase 1: aprovado.*

- **Jogabilidade Primeiro - PASS**: o plano entrega e mede Quack, agua/terra e builds antes de
  qualquer expansao de conteudo.
- **Regras Deterministicas e Dados Editaveis - PASS**: `Core` nao depende de componentes de cena;
  catalogos e balanceamento ficam em ScriptableObjects validados.
- **Multiplataforma por Adaptadores - PASS**: todos os dispositivos produzem `PlayerCommand`; somente
  adaptadores de entrada, vibracao e persistencia conhecem a plataforma.
- **Desempenho Medido - PASS**: pools, atualizacao centralizada, orcamentos e testes de profiling
  fazem parte dos marcos; inimigos, projeteis, recompensas e VFX usam pools explicitamente, e nenhuma
  tecnologia de alta complexidade e adicionada antecipadamente.
- **Escopo do Prototipo Protegido - PASS**: loja, anuncios, compras, outros biomas, personagens e
  modo infinito nao aparecem na estrutura nem nos contratos.
- **Fluxo e Qualidade - PASS**: cada marco termina com teste de regra, integracao e validacao jogavel.

## Project Structure

### Documentation (this feature)

```text
specs/001-prototipo-lago/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── gameplay-contracts.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # gerado posteriormente por $speckit-tasks
```

### Source Code (repository root)

```text
Assets/QuackQuack/
├── QuackQuack.Runtime.asmdef
├── Core/
│   ├── QuackQuack.Core.asmdef
│   ├── Combat/
│   ├── Progression/
│   ├── Run/
│   └── Shared/
├── Gameplay/
│   ├── Player/
│   ├── Enemies/
│   ├── Weapons/
│   ├── Pickups/
│   ├── Terrain/
│   └── Bosses/
├── Systems/
│   ├── Spawning/
│   ├── Combat/
│   ├── Upgrades/
│   ├── Pooling/
│   └── Save/
├── Content/
│   ├── Characters/
│   ├── Weapons/
│   ├── Passives/
│   ├── Enemies/
│   ├── Encounters/
│   └── Relics/
├── Presentation/
│   ├── QuackQuack.Presentation.asmdef
│   ├── UI/
│   ├── Audio/
│   ├── VFX/
│   ├── Animation/
│   └── Camera/
├── Platform/
│   ├── QuackQuack.Platform.asmdef
│   ├── Input/
│   ├── Haptics/
│   └── Persistence/
├── Scenes/
│   ├── Bootstrap.unity
│   ├── MainMenu.unity
│   └── Lake.unity
└── Tests/
    ├── EditMode/
    └── PlayMode/

Packages/
ProjectSettings/
```

**Structure Decision**: Um unico projeto Unity reduz divergencia entre Windows e Android. `Core`
contem regras sem dependencia de cena; `Gameplay` traduz essas regras para GameObjects; `Content`
centraliza ScriptableObjects; `Systems` possui orquestradores; `Presentation` reage a eventos; e
`Platform` encapsula variacoes de dispositivo.

## Delivery Milestones

1. **Fundamentos**: projeto 2D URP, cenas Bootstrap/Lake, camera, comandos comuns, movimento e smoke
   build em Windows/Android.
2. **Mecanica central**: terreno agua/terra, Folego, Quack, feedback e testes deterministas.
3. **Combate e escala**: dano, cinco inimigos, coleta, pools, SpawnDirector e perfil com 400 alvos.
4. **Builds**: experiencia, cartas, seis armas, seis passivas, tres evolucoes e limites de escolha.
5. **Partida completa**: dois minibosses, Sapo-Rei, 15 minutos, reliquias, persistencia e resultado.
6. **Validacao**: matriz de entrada/tela, builds Windows/Android, profiling e playtest dos criterios
   SC-001 a SC-008.

Pooling e ciclo de atualizacao sao gates do marco 3: inimigos, projeteis, pickups e VFX MUST obter e
devolver instancias pelo `PoolService`; hordas MUST ser atualizadas por scheduler centralizado, sem
um `Update` independente por inimigo. Relíquias sao gate do marco 5: desbloqueio, loadout de ate tres
itens e aplicacao de efeitos MUST funcionar antes da persistencia ser aprovada.

## Complexity Tracking

Nao ha violacoes da constituicao a justificar.
