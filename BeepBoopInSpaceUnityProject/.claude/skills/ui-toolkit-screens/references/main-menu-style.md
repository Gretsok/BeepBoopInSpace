# Charte du menu principal

Tirée **uniquement** de la maquette `Assets/RawAssets/UI/Menus/UI_CharacterSelection.psd` (sélection
des personnages, trois configurations : 2, 3 et 4 cercles). Rien n'y est ajouté : ce que la maquette
ne montre pas est listé à la fin, à faire trancher. Cotes en pixels d'un écran 1920×1080, mesurées
avec le skill `psd-mockups` (`info`, `inkbox`, `pixel`). Planche visuelle :
[main-menu-style.png](main-menu-style.png).

## 1. Composition de l'écran

| Zone | Contenu dans la maquette | Cotes |
| --- | --- | --- |
| Haut, centre | Cartouche de titre | 918 px de large, centré (x 503 → 1421), collé en haut : il dépasse du bord (haut à −45), bas visible à 105 |
| Haut, coins | Pastilles d'action (gauche « Aléatoire », droite « Appeler ») | 279×68, à 29 px des bords gauche/droit, haut à 23 |
| Milieu | Contenu : cercles des joueurs, puis grille des robots | cercles de y 206 à 646 ; grille de y 727 à 990 |
| Bas, gauche | Légende « Retour » | texte à partir de x 82 |
| Bas, droite | Légende de l'action principale « Prêts ? » | texte jusqu'à x 1857 (63 px du bord) |

Les légendes du bas sont centrées sur y ≈ 1020 (disque du bouton de 997 à 1043). Le contenu est
centré horizontalement ; la maquette le décale de 5 à 9 px vers la droite selon la configuration
(centres à x 965-969), sans raison apparente.

## 2. Fond

- `RawAssets/UI/Menus/Communs/SPR_Background.png` (3840×2160, affiché en 1920×1080) : c'est exactement
  le fond de la maquette (écart moyen < 3/255).
- Il contient un **dégradé linéaire vertical** de `#031B26` (bas) à `#83D5FD` (haut) — arrêts à 38 % et
  74 % du dégradé, décalé vers le bas : l'écran est clair jusqu'à y ≈ 750 puis fonce jusqu'à
  `#0E2835` en bas — et un **filigrane** (motif de créatures) en densité de couleur à 10 %.
- Les formes sombres (cercles) laissent voir le même filigrane, plus marqué (densité de couleur à
  14 %, écrêté à la forme). Dans le jeu : `CharacterSelection/SPR_Filigree.png` (extrait de la
  maquette) teinté `#195A7C`, valeur calculée pour approcher la densité de couleur.

## 3. Couleurs

| Couleur | Rôle dans la maquette |
| --- | --- |
| `#031B26` (3, 27, 38) | Encre : fond du cartouche de titre et des pastilles ; aussi le bas du dégradé du fond |
| `#83D5FD` (131, 213, 253) | Haut du dégradé du fond |
| `#BDDBE2` (189, 219, 226) | Contour 3 px du cartouche et des pastilles ; disque des boutons de manette |
| `#BCDFED` (188, 223, 237) | Tous les textes clairs (titre, libellés) |
| `#182738` (24, 39, 56) | Lettre dans le disque d'un bouton de manette |
| `#164F6C` (22, 79, 108) | Cercles des joueurs (opaques) ; le même à 20 % d'opacité de fond : halo d'un cercle validé |
| `#FFFFFF` | « En cours » : anneau pointillé, pastille de joueur, contour de case de grille ; pastille « + » |
| `#000000` | Texte d'une pastille blanche |
| `#BDBDBD` (189, 189, 189) | Disque d'une case de grille sans robot ; silhouette grise `#A0A0A0` dessus |
| Couleur du personnage | « Validé » : contours festonné et intérieur, pastille, disque de la case. Pastilles de la maquette : Milo `#2994C8`, Dynamo `#61982C`, Aero `#C63D32`, Turbyo `#DD7E3B` ; robots qui ne sont pas dans le jeu : Zabbo `#5AA6B5`, Kubo `#5D8EC1`, Rowbo `#CC6B7E`, Qoko `#139CBE`, Wako `#5E962F`. Disque d'une case validée : Milo `#2994C8` avec un contour plus sombre `#1C678B` (× 0,68) ; Aero y est plus clair que sa pastille (`#EF6156`, relevé sur l'image) |

## 4. Typographie

| Usage | Police | Taille | Couleur |
| --- | --- | --- | --- |
| Titre d'écran | BeepBoop **Bold**, en capitales | 45 px (capitales de 35 px) | `#BCDFED` |
| Légendes du bas, invitation dans un cercle (« Retour », « Prêts ? », « Rejoindre ») | Boogaloo Regular | 40 px | `#BCDFED` |
| Libellé d'une pastille (« Aléatoire », « Appeler ») | Boogaloo Regular | 35 px | `#BCDFED` |
| Pastille de joueur « en cours » / « + » | Boogaloo Regular | 38 px | `#000000` sur blanc |
| Pastille de joueur « validé » | Boogaloo Regular | 32 px | `#FFFFFF` sur la couleur |
| Pastille dans la grille | Boogaloo Regular | 23 px | noir sur blanc / blanc sur couleur |
| Lettre d'un bouton de manette (X, Y, B, LB, RB) | Cooper Black | 26 px | `#182738` |

- Tous les textes (sauf les lettres des boutons) ont un **biseau** léger : profondeur 1, taille 0,
  angle 90°, altitude 30°, lumière blanche 52 %, ombre noire 100 % — un liseré sombre sous la lettre.
  Les disques de boutons et les pastilles l'ont aussi.
- Libellés en français, majuscule initiale, espace avant « ? » (« Prêts ? »). Le titre est en
  capitales, et en anglais dans la maquette (« CHARACTER SELECTION »).
- BeepBoop Bold et Cooper Black ne sont pas dans le projet (seulement `BeepBoop.otf` Regular et
  `Boogaloo-Regular.ttf`). Magnetar, la police actuelle des autres écrans, n'apparaît pas dans la maquette.

## 5. Composants

### Cartouche de titre
Rectangle `#031B26`, contour 3 px `#BDDBE2` centré, coins bas arrondis (~20 px), haut hors de l'écran.
Titre centré dedans (encre de x 564 à 1362, y 35 à 71). Sprites : `Communs/SPR_UpperBorderInt.png`
(fond) et `SPR_UpperBorderExt.png` (contour), 1870×242, affichés à 935×121.

### Pastille d'action (coins hauts)
279×68, rayon ~24 px, fond `#031B26`, contour 3 px `#BDDBE2`, lueur externe blanche (100 %, 10 px,
contraction 15 %, lumière vive) et lueur interne blanche (13 %, 29 px, éclaircir).
- Un bouton : disque à 31 px du bord gauche, puis le libellé (« X Aléatoire », 33 px entre disque et texte).
- Deux boutons autour du libellé : « LB Appeler RB », disques à 31 px des bords, 19 px de chaque côté du texte.
Sprites : `Communs/SPR_ButtonInterior.png` / `SPR_ButtonExterior.png` (560×137, affichés à 280×68,5).

### Bouton de manette
Disque de 43 px `#BDDBE2`, lettre Cooper Black 26 px `#182738`, biseau. Lettres Xbox dans la maquette.

### Légendes du bas (sans fond)
- Retour, en bas à gauche : **libellé puis bouton** (« Retour B », 19 px entre les deux).
- Action principale, en bas à droite : **bouton puis libellé** (« Y Prêts ? », 26 px).

### Cercle de joueur (440 px, rangée centrée, 38 px entre deux cercles)
| État | Composition |
| --- | --- |
| Invitation | Disque `#164F6C` + filigrane, anneau pointillé blanc de 383 px, pastille blanche « + » (62 px, centrée à 19 px du bord gauche et 143 px du haut du cercle), « Y Rejoindre » au centre (disque à 115 px du bord gauche, 25 px avant le texte) |
| En cours | Comme l'invitation, avec la pastille « J1 » à « J4 » (noire sur blanc) et l'image du robot à la place de « Rejoindre » |
| Validé | Halo : disque `#164F6C` à 20 % ; disque festonné `#164F6C` + filigrane (374 px) ; contour festonné (376 px) et anneau intérieur (326 px) dans la couleur du personnage ; pastille de 53 px dans la couleur, texte blanc, plus à l'intérieur (centre à 48 px du bord gauche, 154 px du haut) |

Image du robot : centrée, pied entre 45 et 115 px au-dessus du bas du cercle selon le robot.
Sprites : `CharacterSelection/SPR_CircleBackground.png`, `SPR_BorderIntPlayer<n>NoReady`,
`SPR_CircleBackgroundPlayerReady`, `SPR_BorderExtPlayerReady`, `SPR_BorderIntPlayer<n>Ready` /
`SPR_BordureIntJoueur<n>Ready`, `SPR_IconeNumberPlayer(No)Ready` (tous blancs, à teinter, double taille).

### Grille des robots
10 colonnes × 2 lignes, cases de 104 px, pas de 123,3 px en largeur et 151 px en hauteur, de x 357 à
1571, lignes à y 735 et 886. Icône 100×125 centrée, qui dépasse de 40 px au-dessus de la case, pieds
calés ~20 px au-dessus du bas de la case. Pastille de 37 px en bas à droite de la case.
| État de la case | Rendu |
| --- | --- |
| Visée par un joueur en cours | Anneau blanc (~8 px) autour du disque gris, pastille blanche |
| Prise par un joueur validé | Disque dans la couleur, contour 5,7 px plus sombre, pastille dans la couleur |
| Sans robot | Disque `#BDBDBD`, silhouette grise (`CharacterIcones/SPR_MiloIconeGrey.png`) |
Sprites : `SPR_BotSelectionCircleBackground`, `SPR_BotSelectionContourPlayerSelected`,
`SPR_BotSelectionPlayerNumber`, icônes `CharacterIcones/SPR_<Robot>Icone.png` (200×250).

## 6. Langage des états

La maquette code l'avancement de la même façon partout :
- **blanc** = en cours, pas encore validé (anneaux, pastilles, contour de case) ;
- **couleur + forme festonnée** = validé ;
- **gris + silhouette** = emplacement sans contenu ;
- **« + » + verbe d'action avec son bouton** = invitation (« Y Rejoindre »).

## 7. Ce que la maquette ne définit pas

À faire trancher par la graphiste avant de l'appliquer ailleurs ; ne pas l'inventer :

- Les **boutons sélectionnables** (accueil : Jouer / Crédits / Quitter, hub, cartes de niveau, lignes
  des paramètres) : la maquette n'a aucun élément focalisable, donc ni état normal, ni survol/focus,
  ni appuyé, ni désactivé.
- Les **sliders, listes défilantes, barres de défilement, textes longs** (crédits).
- Les **animations, transitions entre écrans et sons** (le motion design de la sélection des
  personnages est une proposition d'intégration, pas la maquette).
- Les **glyphes PlayStation et clavier** (seules les lettres Xbox sont dessinées).
- La **langue du titre** (anglais dans la maquette, français ailleurs dans le jeu), la police
  **BeepBoop Bold** et **Cooper Black** à fournir.
- La présence de la grille avec 2 ou 3 cercles (dessinée seulement dans la configuration à 4).
- Sprites livrés **sans maquette** : `Menus/Settings` (`SPR_Arrow`, `SPR_BackgroundMusicSettings`,
  `SPR_FunnyAlien`, barres de défilement), `Menus/Training` (cartes, icônes d'action, flèche),
  `Menus/Credits` (`SPR_BackgroundText`, `SPR_PlayingAlien`), `Menus/LoadingScreen`, `UI_ButtonPlay.png`,
  `UI_LockA/B.png`. Leur mise en page n'est pas décrite ici.

## 8. Dans le code

`UITools/Screens/Common.uss`, importée par les styles du menu (`MainMenu/UI/MainMenu.uss`) et ceux des
autres écrans (`UITools/Screens/Screens.uss`), porte ces valeurs : palette `--ui-ink`, `--ui-outline`,
`--ui-text`, `--ui-navy`, `--ui-navy-faded`, `--ui-locked`, `--ui-filigree` ; classes `ui-background`,
`ui-title` (`__fill`, `__border`, `__label`), `ui-legend` (`--back`, `--action`, `__label`,
`__glyph--after`), `ui-glyph` (sprite Kenney de 57 px = disque de 43 px, teinté `#BDDBE2`), `ui-text`
(Boogaloo, la police par défaut des écrans), `ui-heading` (BeepBoop), `ui-filigree`, et les choix de la
section 9 (`ui-button`, `ui-tile`, `ui-panel`, `ui-badge`, `ui-volume-row`). Ce qui n'appartient qu'à la
sélection des personnages reste en `cs-` (pastilles `cs-insert`, cercles, grille). Écarts assumés : gras
synthétique + `letter-spacing: 5px` pour BeepBoop Bold, glyphes Kenney à la place des disques Cooper
Black, lueurs des pastilles non reproduites.

## 9. Application aux autres écrans (choix d'intégration, pas la maquette)

Accueil, hub, paramètres, crédits et choix du niveau suivent la charte depuis octobre 2026. Ce que la
maquette ne définit pas (section 7) y a été tranché ainsi, **à faire valider par la graphiste** :

| Élément | Choix | Raison |
| --- | --- | --- |
| Fond | Fond de la maquette partout, sauf l'accueil qui garde la scène 3D du menu | Demande de l'utilisateur |
| Titre d'écran | Cartouche, en français et en capitales (« PARAMÈTRES », « CRÉDITS », « CHOIX DU NIVEAU », « MODE DE JEU ») | Textes du jeu en français ; la sélection des personnages garde « CHARACTER SELECTION » de la maquette |
| Titre du jeu (accueil) | BeepBoop gras 130 / 70 px, `#BCDFED`, contour encre 4 px + ombre | Lisible sur n'importe quel endroit de la scène 3D |
| Bouton (`ui-button`) | Pastille : encre, contour 3 px `#BDDBE2`, rayon 24 px, 68 px de haut, Boogaloo 35 px. Sélectionné : contour blanc 5 px, fond `#164F6C`, texte blanc, pop ×1,1. Appuyé : encre. Désactivé : 50 % | Le blanc = « en cours » de la maquette (anneau blanc d'une case visée) |
| Action principale (`ui-button--big`) | La même pastille en 150 px de haut, Boogaloo 88 px (« Jouer ») | |
| Tuile (`ui-tile`, cartes de niveau ; `ui-volume-row`, lignes de volume) | Forme sombre `#164F6C` + filigrane, rayon 30-48 px ; sélectionnée : anneau blanc 6 px, texte blanc, pop | Les cercles des joueurs : le contenu de l'écran est sombre et laisse voir le fond |
| Retour | Légende « Retour (B) » en bas à gauche, cliquable ; plus de bouton Retour à atteindre à la croix | Composition de la maquette |
| Curseur de volume | Rail encre 18 px, remplissage `#BDDBE2`, poignée `#BCDFED` cerclée d'encre (blanche et ×1,2 quand la ligne est sélectionnée) | |
| Défilement (choix du niveau) | Barre de 16 px : rail encre à 60 %, poignée `#BDDBE2`, sans flèches | |
| Crédits | Un panneau sombre, rubriques en BeepBoop gras 42 px capitales, noms en Boogaloo 40 px | |
| Panneau (`ui-panel`) | La forme sombre d'une tuile, jamais sélectionnée (règles du tutoriel, lignes des résultats du tournoi) | |
| Pastille de joueur (`ui-badge`) | « J1 » à « J4 » : blanche en attente, dans la couleur du robot une fois validé | Celles des cercles de la sélection |
| Annonces en partie | Pastille encre élargie, BeepBoop gras 48 px en capitales, qui se déroule à l'apparition | Lisible sur la scène de jeu |
| Écran de chargement | Fond de la maquette, aliens de `Menus/LoadingScreen` qui sautillent, « Chargement… » en bas à droite | Sprites livrés pour cet écran, sans maquette |

Sprites livrés sans maquette et non utilisés pour l'instant : `Menus/Training` (fond et contours de
carte, icônes d'action), `Menus/Settings` (flèches, barres, `SPR_FunnyAlien`), `Menus/Credits`
(`SPR_BackgroundText`, `SPR_PlayingAlien`), `UI_ButtonPlay.png`. Les formes simples (cartes, barres)
sont dessinées en USS aux mêmes proportions.
