---
name: ui-toolkit-screens
description: Créer, modifier ou migrer depuis uGUI un écran UI Toolkit de Beep Boop dans l'espace — UXML et USS, prefab avec UIDocument, sélection partagée manette/clavier/souris, navigation à la croix, entrées propres à chaque joueur, sons, retour avec B/Échap, charte graphique du menu (tirée de la maquette de la sélection des personnages), motion design, et captures d'écran pour vérifier soi-même le rendu. À utiliser dès qu'on touche à un écran du menu principal (accueil, sélection des personnages, hub, choix du niveau, crédits, paramètres), qu'on ajoute un écran, qu'on touche aux écrans de partie (tutoriel, timer, annonces, pause, résultats), à l'écran de chargement ou aux résultats du tournoi, même si l'utilisateur dit seulement « change ce bouton », « ajoute une option », « le menu », « l'UI », « c'est moche », « ça ne marche pas à la manette ».
---

# Écrans UI Toolkit

But : modifier ou créer un écran qui marche du premier coup à la manette, au clavier et à la souris,
et le vérifier en image, sans ouvrir l'éditeur.

## Où sont les choses

| Quoi | Où |
| --- | --- |
| Base des écrans | `Assets/Game/UITools/Screens/AUIToolkitScreen.cs`, pour tous les écrans : activer le GameObject reclone l'UXML et appelle `HandleShown`, le désactiver détruit l'arbre (`HandleHidden`). `BindButton(nom, action, son)`, `Query<T>(nom)`, `DefaultFocus`, `FocusSilently`, `ReleaseFocus`, `HandleCancel`, `AddIndication` (joueur principal, ou un joueur donné) / `RefreshIndications` / `MainPlayer`. Gère seul : navigation directionnelle, focus au survol de la souris, clic sur le fond sans perte du focus, son de focus (`m_focusAudioPlayer`), sélection du panel dans l'EventSystem, bouton de l'élément `back-glyph` (champ `m_backIndicationAction` du prefab), alignement des `.ui-filigree` sur le fond. Hérite de `Panel` (`UIManager.GetPanel<T>`). Menu principal : `MainMenu/AMainMenuScreen.cs` en hérite ; son état l'active (`Activate` / `Deactivate`), l'écran lie ses éléments dans `HandleActivation` |
| Styles | `UITools/Screens/Common.uss` : palette `--ui-*` et composants de la charte (`ui-screen`, `ui-background`, `ui-shade`, `ui-text`, `ui-heading`, `ui-glyph`, `ui-filigree`, `ui-title`, `ui-legend`, `ui-button` (`--big`, `--with-glyph`), `ui-tile`, `ui-panel`, `ui-badge`, `ui-volume-row`, `ui-centered-row`, `ui-corner--*`). Importée en tête de `MainMenu/UI/MainMenu.uss` (une section par écran du menu, `cs-` : sélection des personnages) et de `UITools/Screens/Screens.uss` (une section par écran hors du menu) |
| Panel | `UITools/Screens/UIPanelSettings.asset`, un seul pour tous les écrans (1920×1080, suit la largeur), thème `UITheme.tss` = thème par défaut de Unity (pour ScrollView et Slider). Un panel, donc un focus ; l'ordre d'affichage est celui des documents (`m_SortingOrder` de l'UIDocument) |
| Écrans | `<Dossier>/<Écran>.uxml` à côté de `<Écran>.cs` ; prefab `<Écran>.prefab` (racine : script + UIDocument, enfants : lecteurs audio) placé sous son état dans `MainMenu.unity`, inactif |
| Gabarits | `LevelSelection/LevelWidget.uxml` (cloné par `LevelWidget`), `CharacterSelection/PlayerSlot.uxml` (`<ui:Instance>`), `UITools/Screens/VolumeRow.uxml` (ligne de volume des paramètres et de la pause : `<ui:Instance>` + `<ui:AttributeOverrides element-name="label" text="…" />`, liée par `VolumeRow`) ; éléments construits en code : `CharacterRoster` (grille des robots) |
| Indications d'input | `Global/InputIndicationsManagement/InputIndicationView.cs` (sprite du bouton selon le périphérique du joueur ; `ShowsLikelyDeviceWithoutPlayer` pour une légende affichée avant que quiconque ait rejoint) |
| Charte du menu | [references/main-menu-style.md](references/main-menu-style.md) + planche [main-menu-style.png](references/main-menu-style.png) : composition, couleurs, polices, composants et états, **tirés seulement** de `RawAssets/UI/Menus/UI_CharacterSelection.psd`, avec ce qu'elle ne définit pas |
| Motion design | [references/motion.md](references/motion.md) : transitions USS ou tweens (`UITools/UIToolkitTweens.cs`), et planche de film pour vérifier une animation |
| Maquettes PSD | skill `psd-mockups` : mesurer une maquette et comparer les captures avec elle |
| Polices | `RawAssets/UI/Fonts/Boogaloo-Regular.ttf` (tous les textes du menu), `BeepBoop.otf` (titres, titre du jeu) ; `RawAssets/Fonts/magnetar_2/Magnetar.otf` ne reste que dans les canvas World Space de la scène 3D |
| Test | `MainMenu_EveryScreenWorksWithGamepadKeyboardAndMouse` (skill `unity-scenario-tests`) |

### Écrans hors du menu principal

Partie, pause, chargement et tournoi : même base, mêmes composants, styles de `Screens.uss`.

| Quoi | Où |
| --- | --- |
| Écrans | Timer (`Gameplay/Timer/TimerPanel`), annonces (`BannerScreen` : `NewRoundPanel`, `ConfigurationShuffledBanner`, `FinishBanner`, `GameStartBanner` caché comme l'ancien), tutoriel (`TutorialPanel` + gabarit `TutorialPlayerCard.uxml`), résultats (`ResultsPanel`), « +1 » au-dessus des cases (`UITools/WorldOverlay/WorldOverlayPanel.PlayFloatingText`), pause (`PauseMenuPanel`, avec les volumes), chargement (`LoadingScreen/LoadingScreenPanel`), résultats du tournoi (`TournamentResultsCanvas` + gabarit `TournamentPlayerResultLine.uxml`). Les prefabs gardent leur nom `*Canvas` |
| Ordre des documents | menu 0, timer 0, overlay 5, annonces 10, tutoriel et résultats 20, pause 50, chargement 100 |
| Test | `GameScreens_WorkWithGamepadsAndKeyboard` (captures 20 à 29) et la capture 30 du test du tournoi |

Restent en uGUI, à garder : les canvas **World Space** dans la scène 3D (noms des robots à
l'introduction, `IntroSystem` ; podium des résultats, `ResultsCharacterPositionner` ;
`MeteoriteSystem`).

## Modifier un écran

Lire l'UXML, le script et la section du USS ensemble. Règles :

- **Noms** : le code retrouve les éléments par `name` (`play-button`) ; les tests aussi
  (`credits-button`, `settings-button`, `master-volume`, et les champs `m_trainingButton`,
  `m_tournamentButton`, `LevelWidgets`, `m_widgets` avec `IsReady` et `CharacterDataAsset` des
  `CharacterWidget`). Renommer partout, ou garder.
- **Lier à chaque activation** : l'arbre est neuf à chaque ouverture, ses callbacks partent avec lui.
  Dans `HandleDeactivation`, ne se désabonner que de ce qui vit hors de l'arbre (`PlayerManager`...).
- **Boutons** : toujours par `BindButton` (il retire la classe `unity-button` : tout le style vient
  du USS). Son de clic = un `AudioPlayer` enfant du prefab, référencé par un champ de l'écran.
- **À la croix**, seuls les `Button` (cartes de niveau comprises) et les sliders sont atteignables
  (`IsNavigationTarget`). Un autre contrôle focalisable doit y être ajouté.
- **Sélection = `:focus`** dans le USS ; le survol focalise, ne pas styler `:hover` à part. Effet
  « pop » : `scale` en transition (0,3 s `ease-out-back` en entrant, 0,1 s en sortant).
- Conteneurs et décor en `picking-mode="Ignore"`. Mise en page en px de 1920×1080 : positions
  absolues ou flex ; centrer avec `ui-centered-row` ou `translate: 0 -50%`.
- **Contre le thème par défaut** (contrôles Unity), sélecteurs d'au moins 3 classes :
  `.ui-screen .ui-volume-row .ui-volume-row__slider .unity-base-slider__dragger`.
- **Textes en français**, majuscule initiale (Boogaloo a minuscules et accents) ; titres en
  capitales accentuées (« PARAMÈTRES ») : BeepBoop les a, son I majuscule est pointé (« iDÉATION »).
  Texte clair seulement sur du sombre (cartouche, pastille, tuile, ou bas du fond, sous y ≈ 900) :
  sur le haut du fond, clair, il ne se lit pas.
- **Sons** : `m_focusAudioPlayer` joue à chaque focus donné par le joueur ; un focus donné par le
  code passe par `FocusSilently` (sinon son parasite, voire « No AudioManager found » à l'ouverture).
- **Retour** : légende « Retour (B) » en bas à gauche (`ui-legend--back`, `Button` non focalisable
  nommé `back-button` pour la souris, avec un `back-glyph`), et surcharger `HandleCancel` (son retour
  + même action). Ne pas écouter en plus l'action Cancel via `NavigationAuthorityManager` : deux
  `RequestBack`, et la FlowMachine ne dédoublonne pas.
- Un bouton qui doit attendre une condition : `SetEnabled`, et `DefaultFocus` à null tant qu'il est
  désactivé. Sans focus, Annuler n'arrive pas à l'écran (`PanelEventHandler` vise la racine du panel) :
  un écran sans bouton à atteindre garde le focus sur sa racine (`focusable`, `DefaultFocus`), comme
  la sélection des personnages et les crédits.
- **Police** : `.ui-screen .unity-text-element` impose Boogaloo à tous les textes ; une autre police
  (ou taille, couleur, marge sur un `ui-text`, qui remet ses marges à 0) demande un sélecteur d'au
  moins deux classes placé après dans le USS (`.ui-screen .ui-title__label`). Boogaloo encre ~5 px plus haut que le milieu de sa ligne
  (~12 % de sa taille) : `translate: 0 -5px` sur un texte à côté d'un bouton de manette, et dans un
  bouton plus de `padding-top` que de `padding-bottom` (9 px pour 35 px).
- **Sombre qui laisse voir le fond** (tuile, panneau) : fond `--ui-navy`, `border-radius` +
  `overflow: hidden`, et un enfant `ui-filigree` en premier ; un texte au-dessus est un `Label`
  enfant (le texte propre d'un `Button` est dessiné sous ses enfants).
- **Sprites des maquettes** (`RawAssets/UI/Menus/<écran>`) : en double taille, souvent blancs à teinter
  (`-unity-background-image-tint-color`), et parfois sans Sprite (mode Multiple vide) : les prendre
  comme texture, `url("/Assets/....png")` dans le USS et champ `Texture2D` en C#. Glyphes Kenney
  (64 px, bouton de 48 px) : élément de 57 px pour un disque de 43 px.
- **Atlas des menus** (`SPRA_*`, un par dossier de `RawAssets/UI`) : tight packing avec rotation. Un
  sprite pris par `url()` et dessiné avec `background-size` / `background-position` montre des
  morceaux de ses voisins d'atlas dans ses coins transparents (vu : une main de robot dans la
  silhouette grise). Une image aux coins vides passe en `Texture2D` depuis le C# (champ sérialisé,
  comme `m_missingCharacterIcon`) ; une texture plein écran (filigrane) reste en type Default, hors
  de l'atlas.
- **Rond qui découpe son contenu** : `border-radius` + `overflow: hidden`. Pour caler un enfant sur
  l'écran (texture plein écran qui raccorde avec le fond), additionner les `layout.position` des
  parents : `worldBound` inclut les scale et rotate en cours d'animation.
- **Sélection des personnages** : chaque joueur agit avec ses propres actions (carte `MainMenu` :
  Move, Ready, Back, Random, Call, `CharacterSelectionPlayerController`), pas par le focus ; le
  Annuler du joueur principal passe par `HandleCancel` (il annule d'abord son « prêt »). On rejoint
  avec Y/A/Espace/Entrée (action créée par l'écran), on lance quand tous sont prêts (2 au moins).
  Couleurs, icône et aperçu viennent du `CharacterDataAsset` (section Character selection) ; le
  motion design de la validation est dans `CharacterWidget.PlayReady` (tweens DOTween des styles,
  `UITools/UIToolkitTweens`) et les transitions `.cs-slot--ready` du USS.

## Intégrer une maquette

Mesurer la maquette et comparer les captures avec elle : skill `psd-mockups`. Pour un écran sans
maquette propre, suivre la charte ([references/main-menu-style.md](references/main-menu-style.md))
et signaler ce qu'elle ne définit pas (états des boutons sélectionnables, sliders, listes...) au
lieu de l'inventer. Les autres écrans du menu en ont tiré des choix d'intégration (bouton
sélectionné, tuiles, curseurs, défilement) : les reprendre (section 9 de la charte) plutôt qu'en
créer de nouveaux. Une classe `cs-*` de `MainMenu.uss` est à sortir vers `Common.uss` (`ui-`) avant d'être
réutilisée par un autre écran ; une classe d'un écran hors du menu, de `Screens.uss`, de même.

## Ajouter un écran au menu principal

1. UXML (copier un écran simple comme `Credits/CreditsScreen.uxml` : `<Style src="../UI/MainMenu.uss" />`,
   racine `class="ui-screen"`), puis `unity_yaml.py meta` dessus (skill `unity-yaml`).
2. Script `XxxScreen : AMainMenuScreen` et état `XxxState : AMainMenuFlowState` sur le modèle des
   Crédits ; champ, `Initialize` et `SwitchToXxxScreen` dans `MainMenuOrchestrator`.
3. Prefab : copier `Credits/CreditsScreen.prefab` (+ `meta` pour un nouveau guid), y remplacer le
   guid du script de l'écran et le `sourceAsset` de l'UIDocument (`tree --fields` pour vérifier).
4. Scène : un GameObject d'état sous `Orchestrator` et l'instance de l'écran dessous, inactive, liée
   au champ de l'état. Le plus sûr est de demander à l'utilisateur de le faire dans l'éditeur ; à la
   main, recopier les documents d'une instance existante (`overrides`, `tree` montrent lesquels) avec
   de nouveaux fileID — non encore fait dans ce projet.
5. Étendre le test des menus (`Select` / `Submit` / `WaitUntil` / `CaptureScreen`).

## Migrer un écran uGUI

Correspondances uGUI → UI Toolkit et étapes détaillées : [references/migration.md](references/migration.md).
En bref : `unity_yaml.py tree <prefab> --fields --rect` pour relire l'existant, écrire UXML et USS,
`unity_yaml.py uitk-prefab` pour convertir le prefab, remplacer le code uGUI, `clean-overrides` sur
les scènes qui l'utilisent, adapter les tests. Hors du menu principal, hériter d'`AUIToolkitScreen`.
Un canvas posé sur un enfant du prefab (écran de chargement) n'est pas géré par `uitk-prefab` :
retirer ses composants uGUI et ajouter UIDocument + écran à la main (skill `unity-yaml`).

## Vérifier

1. `unity_cli.py quick-compile` : 0 erreur, 0 warning.
2. `unity_cli.py compile` : importe UXML/USS ; une erreur d'UXML ou de USS sort dans le log.
3. `unity_cli.py screenshots` (~2 min) : joue le test des menus et liste les captures 1920×1080 de
   `Logs/screenshots/` ; **les lire toutes** (outil Read) : alignements, textes coupés, sélection
   visible, défilement. Les captures montrent la caméra et le panel UI Toolkit, pas les canvas uGUI.
   Écrans de partie : `unity_cli.py test --filter GameScreens` (~2 min), puis lire les captures 20 à 29.
4. Tests de scénario du menu touché, puis la suite complète avant un commit important.
5. Avant de partager une version : build + `run-build` (skill `unity-build`).

## Pièges vécus

- **Croix sans effet** : l'EventSystem n'envoie la navigation qu'à son objet sélectionné, qui doit
  être le GameObject du panel, créé une frame après le premier écran. `AUIToolkitScreen.Update` le
  resélectionne ; une UI Toolkit hors de cette base doit faire pareil.
- **Boutons dans les coins inatteignables** : la navigation native de UI Toolkit ne regarde que droit
  devant ; `AUIToolkitScreen.HandleNavigationMove` prend l'élément le plus proche dans la direction,
  diagonale comprise (comme uGUI).
- **Slider** : Unity attend un Submit avant que gauche/droite changent la valeur ; `VolumeRow`
  intercepte `NavigationMoveEvent` en TrickleDown (`StopPropagation` + `IgnoreEvent`).
- **Clic sur le fond** : UI Toolkit retire le focus ; `HandlePointerDown` l'empêche.
- **Focus programmatique** : les événements de focus peuvent être différés ; reconnaître le focus
  silencieux par l'élément visé (`FocusSilently`), pas par un booléen posé autour de `Focus()`.
- **`ScrollView.ScrollTo`** ignore les marges et l'agrandissement au focus : `ScrollIntoView`.
- **Ordre d'affichage** : un seul panel, les documents s'empilent par `m_SortingOrder` (l'écran de
  chargement, 100, passe au-dessus du menu et de la partie).
- **Données avant l'arbre** : un état du flow remplit son écran dans `HandleEnter`, avant que
  `GameObjectActivatorStateComponent` ne l'active (l'arbre n'existe pas encore) : l'écran garde les
  données et les applique dans `HandleShown` (`TutorialPanel`, `NewRoundPanel`).
- **Un panel, un focus** : tous les écrans partagent le focus ; `FocusedElement`
  ne rend que celui de l'écran, et `DefaultFocus` n'est rendu que si personne n'a le focus.
- **Un bouton qui apparaît** (classe qui retire `display: none`) ne prend pas le focus dans la même
  frame (`canGrabFocus` lit le style calculé) : poser `DefaultFocus`, la base le donne à la frame suivante.
- **Échap** ouvre la pause (action `PauseControls`) et envoie aussi Annuler : `PauseMenuPanel`
  ignore l'Annuler de la frame d'ouverture. Le joueur dont la carte du tutoriel n'a pas de souris ne
  la clique pas (`pickingMode`) : les manettes passent par leur action Ready, pas par le focus.
- **Batchmode** : écran virtuel 640×480 (4:3) ; les captures rendent donc le panel dans une texture
  1920×1080 pendant tout le test, et attendent la fin des transitions de caméra (Cinemachine).
- **`OnEnable` / `OnDisable` / `Update` dans un écran dérivé** : `AUIToolkitScreen` a les siens en
  `private` ; en déclarer dans l'écran masque ceux de la base (Unity n'appelle que le plus dérivé) :
  plus d'arbre lié, plus de focus rendu. Passer par `HandleShown` / `HandleHidden` / `HandleUpdate`
  (`HandleActivation` / `HandleDeactivation` dans le menu). Délais : `DOVirtual.DelayedCall`.
- **Tween et transition sur la même propriété** : le style inline du tween écrase le USS ; voir
  [references/motion.md](references/motion.md).
- **Outils** : un heredoc Bash contenant une apostrophe casse parfois la commande (écrire un script
  avec l'outil Write et le lancer) ; ne pas lancer `test` si `quick-compile` échoue :
  `quick-compile | grep -v ": ok" && echo erreurs || test ...`.
