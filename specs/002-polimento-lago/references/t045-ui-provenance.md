# T045 — fontes da interface ilustrada

Referência do usuário: `direcao-cartoon-candidata.png`, reapresentada em 09/11/2026 ao rejeitar a primeira skin geométrica. A revisão usa placas creme, contornos marrons, vegetação, coração, pena e controles dourados. Mantém duas barras contínuas sem valores numéricos, conforme decisão funcional vigente; não reproduz os números/pips da ilustração.

## Atlas

`Assets/QuackQuack/Art/LakeCartoon/UI/IllustratedHud.png`: gerado com ImageGen a partir da referência, sem edição por Python. Nove células, alpha, recorte por limites visíveis no Unity. Não contém textos; todos os rótulos são textos vivos do jogo.

Prompt utilizado:

> Create ONE SQUARE 3x3 UI sprite atlas matching the attached duck lake game HUD illustration VERY CLOSELY. Hand illustrated warm cream parchment, thick uneven dark chocolate outlines, honey-gold bevel shading, organic imperfect rounded edges, little lake reed/leaf flourishes. This must look illustrated like reference, NOT generic flat UI or smooth plain rounded rectangles. Nine equally sized square cells, each subject centered with 12% transparent margin, no overlap. Exact order row1: (1) wide BLANK cream relics nameplate with reeds/cattails at both ends like the top center reference, no lettering; (2) wide BLANK cream capsule frame for a resource row, organic double dark brown outline and warm gold lower edge, no hearts or numbers inside; (3) large glossy GOLD circular Quack button, thick brown outline, lighter gold inner rim and highlight, three dark brown sound arcs in its UPPER third, leave lower half blank for live game text. Row2: (1) joystick base, dark translucent teal circle with gold outer rim and four cream directional triangles like reference bottom left, empty center; (2) glossy cream/golden joystick knob with dark outline; (3) red HEART icon with strong brown outline and white shine, tiny green leaves behind it like reference. Row3: (1) CYAN FEATHER icon with dark outline and cream shine like reference breath icon; (2) small cream stopwatch with dark outline and dark hands, gold top knob; (3) large BLANK cream parchment menu/card panel with uneven dark outline and small green leaf/cattail accents confined to corners, generous clean blank interior. All pixels outside subjects genuinely transparent. No text, words, letters, numbers, labels, grids, scene, character, turtle, XP items or pickups. Clean production sprite alpha, no noise, no external glow. Keep decorative art within cells. Frames will be 9-sliced in Unity, so center and long edge middle areas must be clean and stretchable.

## Fonte

Lilita One, arquivo original sem modificações, copyright 2011 Juan Montoreano, nome reservado Lilita. Licença SIL Open Font License 1.1 incluída em `UI/Fonts/OFL.txt` e referenciada pelo tema para acompanhar o player.

- [Fonte oficial](https://github.com/google/fonts/tree/main/ofl/lilitaone)
- [Arquivo TTF original](https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf)
- [Licença original](https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt)

## Elementos auxiliares

`Fill.png` e `Round.png` são gerados deterministicamente pelo editor Unity: preenchimento de barras e comparação circular de raios. `Panel.png`/`Button.png` registram a base geométrica inicial, substituída pelo atlas nas placas/cartas/botões do tema final. Não são texturas de combate, cura ou XP. C2/X3 permanecem provisórios e o Quack ativo continua radial; arcos de ataque pertencem ao Quack frontal.
