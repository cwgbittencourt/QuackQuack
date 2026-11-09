# T046 — cenário e superfície da água

Referência de estilo: `direcao-cartoon-candidata.png`, direção cartoon aprovada pelo usuário. A composição não redefine a arena ou seu elenco. Produção pelo ImageGen integrado; imagem final copiada para `Assets/QuackQuack/Art/LakeCartoon/Environment/lake-plants.png`. Sem edição raster por scripts. Recortes/importação e geometrias auxiliares produzidos pelo Unity.

## Prompt de geração

> Create a production game environment sprite atlas using the attached illustration ONLY as an artistic style reference. ONE square transparent 3x2 atlas, six equally sized cells with 15% transparent margin and no overlaps. Exact order top row: (1) small cattail/reed cluster with green blades and two brown seed heads, (2) low grass tuft with tiny white daisies, (3) two green lily pads with one tiny cream water flower. Bottom row: (1) tiny low grass-only tuft, (2) pale cyan curved WATER RIPPLE, just two short thin crescent strokes on transparent background, no objects, (3) low flat moss/grass patch with a few leaf brush marks, no tall vegetation. Match reference hand painted 2D cartoon lake style: warm olive greens, honey highlights, uneven dark brown outlines on plants, clean simple readable silhouettes. Top down three-quarter game view. These are NON-BLOCKING small decorations, no rocks, logs, borders, terrain squares, scene, HUD, creatures, words or symbols. Water ripple low contrast and delicate, plants matte without glows. All areas outside each subject genuinely transparent. No checkerboard baked in. Crisp high quality sprite cutouts.

## Revisão aplicada

> Edit this sprite atlas preserving the exact six subjects and grid positions. Remove ALL outer glow, haze, shadows and colored fog around every subject: outside the hard crisp silhouettes must be pure transparent alpha zero, including around grass, lily pads and reeds. Matte sprite art only. Replace bottom middle cyan attack-like arcs with TWO SHORT FLAT HORIZONTAL WATER RIPPLE MARKS, thin shallow smile curves, wider than tall, quiet pale teal, no glow, not vertical crescents or weapon slashes. Maintain 3 columns and 2 rows, evenly spaced. No background. No new objects or text. Preserve other plants, colors, outlines and illustration style.

A revisão troca os arcos verticais por marcas horizontais de superfície para não se confundir com Quack frontal. A prévia RGB pode mostrar cor em regiões transparentes; o canal alpha é preservado, inclusive alpha zero no exterior. Fonte anterior descartada, não atribuída ao projeto.

## Geometria e pedras

Faixas de areia são meshes simples geradas deterministicamente: contorno externo ondulado, sob o footprint original da água e da ilha. Água e terra conservam transforms e TerrainZone. Pedras reutilizam sprite/material do lote T043, distribuídas por segmentos dentro das caixas de colisão existentes; o footprint sólido inteiro continua visível na base. Plantas/nenúfares são decoração baixa e sem colliders. Não há licença externa nova para as imagens geradas; a autoria/origem é geração assistida desta task. O manifesto registra importação, ordem de desenho e limites de aceite.
