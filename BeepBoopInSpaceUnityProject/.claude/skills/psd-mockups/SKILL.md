---
name: psd-mockups
description: Lire une maquette Photoshop (.psd) de Beep Boop dans l'espace et s'en servir pour intégrer un écran au plus près — arbre des calques, rendu de chaque configuration (groupes affichés tour à tour), positions, couleurs, polices et tailles en px, effets, extraction d'un calque en texture, sprites livrés en double taille, puis allers-retours entre les captures du jeu et la maquette (superposition, écart moyen, boîte réelle des textes). À utiliser dès que l'utilisateur fournit un .psd ou parle de mock-up, de maquette, de « coller au design », de « réduire l'écart », de reproduire une image de référence, ou quand un écran doit suivre la charte du menu principal.
---

# Maquettes Photoshop

But : intégrer un écran d'après une maquette PSD sans Photoshop, mesurer au lieu de deviner, et
vérifier soi-même l'écart par des captures. Validé sur `RawAssets/UI/Menus/UI_CharacterSelection.psd`
(sélection des personnages, commit « Character selection screen from the mockup »).

```bash
M=".claude/skills/psd-mockups/scripts/mockup.py"   # depuis BeepBoopInSpaceUnityProject/
python $M setup          # une fois : environnement Python dans Library/claude-psd-venv (internet, ~1 min)
```

`setup` installe `psd-tools[composite]`, Pillow, numpy, scipy et fonttools depuis PyPI ; les autres
commandes se relancent seules dans cet environnement. À refaire si `Library/` a été supprimé.

| Commande | Rôle |
| --- | --- |
| `tree <psd> [--path GLOB] [--depth N]` | Arbre des calques : type, `HIDDEN`, taille@position, opacité, opacité de fond, mode de fusion, `clipped`, texte (police, taille en px, couleur), fond et contour des formes, effets |
| `info <psd> GLOB...` | Détail des calques visés, effets compris (couleur, taille, opacité, mode) |
| `render <psd> [--exclusive GLOB] [--show GLOB] [--hide GLOB]` | `merged.png` = le composite enregistré par Photoshop (la seule image exacte), puis un rendu par calque visé par `--exclusive`, affiché seul parmi eux (avec ses parents). 1 à 2 min par rendu sur un gros PSD : en arrière-plan |
| `layer <psd> GLOB [--white-mask]` | Pixels d'un calque, à leur place sur la toile ; `--white-mask` : blanc, alpha = luminance × alpha (motif d'un calque en densité de couleur/superposition, à teinter dans le jeu) |
| `sheet <out.png> <png>...` | Planche de sprites sur damier, avec taille et boîte opaque de chacun |
| `crop`, `pixel`, `inkbox` | Recadrer ; couleur de pixels ; boîte des pixels clairs (ou `--dark`) d'une zone = où est vraiment un texte |
| `compare <maquette> <capture> <out> [--box ...] [--scale ...]` | Maquette, capture et leur mélange à 50 % empilés, écart moyen |

Chemin d'un calque : noms depuis la racine joints par `/` (`CHARACTER SELECTION/3 PLAYERS/PLAYERS SETUP/J1`).
Dans un GLOB (sensible à la casse), `*` et `?` restent dans un nom, `**` traverse les groupes :
`"**/J1/*"` = les enfants directs de chaque J1 (ses états). Les images vont dans
`Logs/mockups/<nom du psd>/` (ignoré par git).

## Méthode

1. **Lire la structure** : `tree --depth 2`, puis `tree --path` sur un groupe. Repérer les
   **configurations** (groupes frères dont un seul est visible : « 2 PLAYERS », « 3 PLAYERS »...) et
   les **états** (« READY PLAYER », « NOT READY PLAYER », « NEW PLAYER »). Un groupe masqué a une boîte
   vide : seul un rendu dit à quoi il ressemble.
2. **Voir** : `render --exclusive "<parent>/* PLAYERS"` puis lire chaque PNG. `merged.png` est la
   référence ; psd-tools ne rend ni biseaux ni lueurs, et ses dégradés diffèrent un peu.
   `--exclusive` ne touche qu'aux calques visés (et montre leurs parents) : viser **tous** les états
   frères (`J4/*`, pas `J4/READY PLAYER` seul, que l'état visible par défaut recouvrirait), et pour
   une configuration masquée, cacher celle qui était visible à l'enregistrement, sinon elles se
   superposent (`--exclusive "**/4 PLAYERS/PLAYERS SETUP/J4/*" --hide "CHARACTER SELECTION/3 PLAYERS"`).
3. **Mesurer** : `info` sur chaque élément (fond, contour 3 px centré, opacité de fond, effets),
   textes en px de la toile (taille Photoshop × échelle du calque, déjà calculée). Un texte coloré
   donne sa couleur en `#RRGGBB`.
4. **Sprites livrés** (`RawAssets/UI/Menus/<écran>/`) : `sheet` dessus. Ils sont en **double taille**
   (un cercle de 440 px de la maquette = sprite de 875 px) et souvent **blancs, à teinter**. Placer un
   élément : position de la maquette − marge transparente du sprite / 2 (boîte opaque de `sheet`).
5. **Polices** : celles du projet sont dans `RawAssets/UI/Fonts` (BeepBoop.otf, Boogaloo-Regular.ttf,
   Magnetar dans `RawAssets/Fonts`). Une police nommée par le PSD peut manquer (BeepBoop-Bold) :
   `-unity-font-style: bold` + `letter-spacing`, réglés en comparant les boîtes d'encre. Vérifier
   les glyphes accentués avec fonttools si le texte en a.
6. **Intégrer** (skill `ui-toolkit-screens`), capturer (`unity_cli.py screenshots`), puis **comparer**
   zone par zone : `compare` pour l'œil, `inkbox` sur la maquette et sur la capture pour les écarts de
   texte au pixel (un même texte mesure la même boîte : seul le décalage compte), `pixel` pour les
   couleurs. Corriger, recapturer, jusqu'à ne plus voir que des écarts voulus.
7. Dire à l'utilisateur ce que la maquette ne tranche pas (configurations incohérentes entre elles,
   textes en anglais, police manquante) et le choix fait, plutôt que deviner en silence.

## Reproduire un effet de Photoshop dans UI Toolkit

| Photoshop | UI Toolkit |
| --- | --- |
| Forme pleine, contour centré | sprite fourni teinté (`-unity-background-image-tint-color`), ou `background-color` + `border` |
| Opacité de fond 20 % | couleur en `rgba(..., 0.2)` (la teinte d'un sprite accepte l'alpha et se transitionne) |
| Calque écrêté (`clipped`) sur un rond | parent `border-radius` + `overflow: hidden` : UI Toolkit découpe aux coins arrondis |
| Calque en **densité de couleur** (filigrane) | `layer --white-mask` → texture blanche à teinter de `fond / (1 − s)` (s ≈ opacité de fond du calque) ; recalée sur la toile en code si elle doit raccorder avec le fond (`CharacterWidget.AlignFiligrees`) |
| Biseau léger sur un texte | `text-shadow: 0 2px 1px rgba(0, 0, 0, 0.45)` |
| Lueur externe / interne | non reproduites (aucun équivalent simple) : les signaler |
| Dégradé de fond | utiliser le sprite de fond livré (`SPR_Background`, identique au rendu de la maquette) |

## Pièges vécus

- **Fond d'une forme** : psd-tools le range dans le bloc `vscg` (`VECTOR_STROKE_CONTENT_DATA`) et non
  dans `SoCo` ; `info` lit les deux.
- **Sortie console en cp1252** sous Windows : les noms de calques (♦, é) font planter un `print` ;
  le script repasse la sortie en UTF-8.
- `psd.composite()` exige `psd-tools[composite]` (aggdraw pour les formes vectorielles, scipy pour les
  dégradés).
- Le PSD fait 58 Mo et n'est pas versionné : ne pas le commiter sans accord.
- **La maquette n'est pas toujours cohérente** : la grille des robots n'apparaissait que dans la
  configuration à 4, un joueur « J4 » y était à la fois à rejoindre et en train de choisir, le contenu
  est décalé de 5 px vers la droite. Reproduire l'intention, signaler l'écart.
- **Textures livrées en mode Multiple sans sprite** : pas de Sprite à référencer avant un import par
  Unity ; les utiliser comme `Texture2D` (`url()` dans le USS, champ `Texture2D` en C#). Un import
  headless leur ajoute ensuite un sprite `_0` dans le `.meta` (comme l'éditeur).
- **Atlas en tight packing** : les dossiers de sprites livrés ont chacun un atlas serré, avec
  rotation ; un sprite de l'atlas dessiné par UI Toolkit avec `background-size` peut afficher ses
  voisins dans ses zones transparentes. Passer ces images en `Texture2D` depuis le C#, et importer
  une texture extraite du PSD (filigrane) en type Default pour qu'elle reste hors de l'atlas.
- **Icônes avec du vide sous le robot** (Aero, Turbyo) : la maquette les cale par le bas visible ;
  un décalage par personnage (`CharacterDataAsset.SelectionIconOffset`) plutôt que retoucher le PNG.
- Charte qui en découle pour le reste du menu : `ui-toolkit-screens/references/main-menu-style.md`.
