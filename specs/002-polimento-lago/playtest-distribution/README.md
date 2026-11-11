# Distribuição remota — T051, 11/11/2026

Pacotes preparados a pedido do usuário, para que P02–P05 possam jogar em seus
próprios dispositivos. Preparar/distribuir não constitui participação nem aceite.
O [relatório](../playtest-report.md) preserva a coleta parcial de P01.

## Arquivos entregues

Pasta local: `C:/Users/lucas/Desktop/QuackQuack_T051_Para_Enviar`.

| Enviar ao participante da plataforma | Bytes | SHA-256 |
|---|---:|---|
| QuackQuack_T051_Windows_20261111.zip | 54.075.967 | fb7a234bd8c627f512f0fdaa5bf88b912ca63bf29219b61ef91f0f387718cfb7 |
| QuackQuack_T051_Android_20261111.zip | 47.777.663 | 3e1962abfb8a71e839e941c934ac1325bfdacd95a334d1462f5cba2267f755cd |

Cada ZIP contém `LEIA_PRIMEIRO.txt`, `FICHA_APOS_JOGAR.txt`, `VERSAO.txt`,
`MANIFESTO-SHA256.txt` e licença do Input System. Windows inclui `Jogo/` com EXE,
DLLs, D3D12 e pasta de dados; Android inclui `QuackQuack-T051-Android.apk`.
Não enviar só o EXE. A ficha deve ser aberta após a primeira experiência livre;
as perguntas de comparação ficam depois do bloco de identificação/relato.

Fora dos ZIPs ficam `PARA_ORGANIZADOR_NAO_ENVIAR.txt`, `CHECKSUMS-SHA256.txt` e
`delivery-manifest.json`. O guia do organizador contém critérios e respostas
esperadas: não enviá-lo antes do reconhecimento. Os documentos de participantes
não incluem os resultados de P01 nem o relatório interno. Não há envio automático
das fichas; o usuário distribui os pacotes e recebe as respostas pelo canal combinado.

Templates versionados nesta pasta: instruções Windows/Android, ficha e guia do
organizador. Binários, arquivos compactados, saves e logs continuam fora do Git.

## Identidade das builds

- Runtime/conteúdo congelados em `d378efd89ad09755e0f942479e3b106d51fd6e14`,
  Unity 6000.4.3f1, seed 42. Produto normal IL2CPP, `BuildOptions.None`, cenas
  RunBootstrap/LakeRun, sem laboratório, relógio acelerado ou build forçada.
- Windows é o artefato usado por P01: GameAssembly SHA-256
  `01c9951f7b2244b3547308e603663d0e490ef7d1af17b13febcad77597be1532`;
  EXE `18d5d1aea4095fbf1a99fb556003fb53c0d7600e147698664779e600d7350cbb`.
- APK SHA-256 `7290af2541ab757a5c946b23f78d308ceaf14d0d5c13d907f9e406b39f8b50c7`;
  pacote `com.cemggames.quackquack.playtest002`, rótulo `QuackQuack Playtest 002`,
  versão 1.0/code 1, ARM64 (`arm64-v8a`), API mínima 25, target API 36.
  Assinatura de teste Android Debug, esquema v2 verificado pelo `apksigner`;
  nenhuma chave privada foi incluída. Isso não é publicação na loja.
- Cópia isolada `Builds/T051/ValidationProject`: 456 arquivos de Core/Content/
  Gameplay/Platform/Presentation/Systems/Prefabs/Scenes comparados por hash com o
  checkout, sem diferenças. O helper Editor altera somente identidade do produto
  e pacote na cópia e chama o pipeline de produção existente.
- Produto de playtest tem progresso separado do produto habitual. Não foram
  incluídos saves de P01/pessoais; instalação anterior deve ser informada antes
  de uma sessão comparável. Não presumir perfil vazio numa atualização.

## Verificação e limites

Build Android: `Build Finished, Result: Success`, `PROTOTYPE_BUILD_OK target=Android`
e saída 0 em `Logs/Polimento002/T051/android-distribution-build.log`.
`aapt dump badging` confirmou pacote/rótulo/arquitetura/APIs; `apksigner verify`
retornou sucesso. Nenhum novo teste físico Android foi realizado nesta entrega:
instalação/jogabilidade nos aparelhos remotos ainda deve ser registrada.

O empacotamento excluiu a pasta de backup `ButDontShipItWithYourGame`, símbolos,
fontes, keystores, logs e saves. CRC de ambos os ZIPs verificado; conteúdo extraído
logicamente do arquivo comparado por SHA-256 com cada arquivo de origem, inclusive
instruções/ficha. Cópias na Área de Trabalho conferidas por hash contra os ZIPs
gerados. Não foram executados novos testes de regras: não há mudança de runtime.

Evidências locais ignoradas: `Logs/Polimento002/T051/package-delivery.py`,
`android-apk-badging.txt`, `android-apk-signature.txt`, `desktop-delivery.json` e
`Builds/T051/Distribution/delivery-manifest.json`. A execução desta entrega não
adicionou participantes, não completou a cobertura das armas/evoluções e não
encerrou T051, os Android ausentes da T049 ou 8BitDo/S23 da T050.
