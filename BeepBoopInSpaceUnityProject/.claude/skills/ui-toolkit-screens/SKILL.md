---
name: ui-toolkit-screens
description: Créer, modifier ou migrer depuis uGUI un écran UI Toolkit de Beep Boop dans l'espace — UXML et USS, prefab avec UIDocument, sélection partagée manette/clavier/souris, navigation à la croix, sons, retour avec B/Échap, et captures d'écran pour vérifier soi-même le rendu. À utiliser dès qu'on touche à un écran du menu principal (accueil, sélection des personnages, hub, choix du niveau, crédits, paramètres), qu'on ajoute un écran, ou qu'on migre une UI uGUI restante (menu pause, résultats, tournoi, HUD), même si l'utilisateur dit seulement « change ce bouton », « ajoute une option », « le menu », « l'UI », « c'est moche », « ça ne marche pas à la manette ».
---

# Écrans UI Toolkit

But : modifier ou créer un écran qui marche du premier coup à la manette, au clavier et à la souris,
et le vérifier en image, sans ouvrir l'éditeur.

## Où sont les choses

| Quoi | Où |
| --- | --- |
| Base des écrans | `Assets/Game/MainMenu/AMainMenuScreen.cs` : `Activate` active le GameObject (l'UIDocument reclone l'UXML), puis `HandleActivation` lie les éléments ; `BindButton(nom, action, son)`, `Query<T>(nom)`, `DefaultFocus`, `FocusSilently`, `HandleCancel`. Gère seul : navigation directionnelle, focus au survol de la souris, clic sur le fond sans perte du focus, son de focus, sélection du panel dans l'EventSystem |
| Styles | `Assets/Game/MainMenu/UI/MainMenu.uss` : variables `--mm-*`, boutons `mm-button` (`--primary`), `mm-play-button`, placements `mm-corner--*`, `mm-centered-row`, puis une section par écran |
| Panel | `UI/MainMenuPanelSettings.asset` (1920×1080, suit la largeur, `sortingOrder` 0), thème `UI/MainMenuTheme.tss` = thème par défaut de Unity (pour ScrollView et Slider) |
| Écrans | `<Dossier>/<Écran>.uxml` à côté de `<Écran>.cs` ; prefab `<Écran>.prefab` (racine : script + UIDocument, enfants : lecteurs audio) placé sous son état dans `MainMenu.unity`, inactif |
| Gabarits | `LevelSelection/LevelWidget.uxml` (cloné par `LevelWidget`), `CharacterSelection/PlayerSlot.uxml` (`<ui:Instance>`) |
| Indications d'input | `Global/InputIndicationsManagement/InputIndicationView.cs` (sprite du bouton selon le périphérique du joueur) |
| Test | `MainMenu_EveryScreenWorksWithGamepadKeyboardAndMouse` (skill `unity-scenario-tests`) |

Encore en uGUI : menu pause (+ `Global/Settings/UI/SettingsScreen`), canvas de partie (`Gameplay/Flows/*Canvas`,
`TimerCanvas`, `NewRoundCanvas`, `ConfigurationSuffledCanvas`), résultats du tournoi, écran de chargement,
`WorldOverlayCanvas`. `InputIndicationWidget` (uGUI) n'est plus utilisé.

## Modifier un écran

Lire l'UXML, le script et la section du USS ensemble. Règles :

- **Noms** : le code retrouve les éléments par `name` (`play-button`) ; les tests aussi
  (`credits-button`, `settings-button`, `master-volume`, et les champs `m_trainingButton`,
  `m_tournamentButton`, `m_startGameButton`, `LevelWidgets`). Renommer partout, ou garder.
- **Lier à chaque activation** : l'arbre est neuf à chaque ouverture, ses callbacks partent avec lui.
  Dans `HandleDeactivation`, ne se désabonner que de ce qui vit hors de l'arbre (`PlayerManager`...).
- **Boutons** : toujours par `BindButton` (il retire la classe `unity-button` : tout le style vient
  du USS). Son de clic = un `AudioPlayer` enfant du prefab, référencé par un champ de l'écran.
- **À la croix**, seuls les `Button` (cartes de niveau comprises) et les sliders sont atteignables
  (`IsNavigationTarget`). Un autre contrôle focalisable doit y être ajouté.
- **Sélection = `:focus`** dans le USS ; le survol focalise, ne pas styler `:hover` à part. Effet
  « pop » : `scale` en transition (0,3 s `ease-out-back` en entrant, 0,1 s en sortant).
- Conteneurs et décor en `picking-mode="Ignore"`. Mise en page en px de 1920×1080 : positions
  absolues ou flex ; centrer avec `mm-centered-row` ou `translate: 0 -50%`.
- **Contre le thème par défaut** (contrôles Unity), sélecteurs d'au moins 3 classes :
  `.mm-screen .settings-row .settings-row__slider .unity-base-slider__dragger`.
- **Textes en français** ; la police Magnetar n'a pas de majuscules accentuées (« Crédits »
  s'affiche CREDITS) et ses chiffres sont stylisés : regarder la capture.
- **Sons** : `m_focusAudioPlayer` joue à chaque focus donné par le joueur ; un focus donné par le
  code passe par `FocusSilently` (sinon son parasite, voire « No AudioManager found » à l'ouverture).
- **Retour** : surcharger `HandleCancel` (son retour + même action que le bouton Retour). Ne pas
  écouter en plus l'action Cancel via `NavigationAuthorityManager` : deux `RequestBack`, et la
  FlowMachine ne dédoublonne pas.
- Un bouton qui doit attendre une condition (Lancer la partie) : `SetEnabled`, et `DefaultFocus` à
  null tant qu'il est désactivé (rien n'a le focus : le A des joueurs qui rejoignent ne clique rien).

## Ajouter un écran au menu principal

1. UXML (copier un écran simple comme `Credits/CreditsScreen.uxml` : `<Style src="../UI/MainMenu.uss" />`,
   racine `class="mm-screen"`), puis `unity_yaml.py meta` dessus (skill `unity-yaml`).
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
les scènes qui l'utilisent, adapter les tests. Hors du menu principal, extraire d'abord la mécanique
de `AMainMenuScreen` dans une base de `Game.UITools` plutôt que la recopier.

## Vérifier

1. `unity_cli.py quick-compile` : 0 erreur, 0 warning.
2. `unity_cli.py compile` : importe UXML/USS ; une erreur d'UXML ou de USS sort dans le log.
3. `unity_cli.py screenshots` (~2 min) : joue le test des menus et liste les captures 1920×1080 de
   `Logs/screenshots/` ; **les lire toutes** (outil Read) : alignements, textes coupés, sélection
   visible, défilement. Les captures montrent la caméra et le panel UI Toolkit, pas les canvas uGUI
   (écran de chargement).
4. Tests de scénario du menu touché, puis la suite complète avant un commit important.
5. Avant de partager une version : build + `run-build` (skill `unity-build`).

## Pièges vécus

- **Croix sans effet** : l'EventSystem n'envoie la navigation qu'à son objet sélectionné, qui doit
  être le GameObject du panel, créé une frame après le premier écran. `AMainMenuScreen.Update` le
  resélectionne ; une UI Toolkit hors de cette base doit faire pareil.
- **Boutons dans les coins inatteignables** : la navigation native de UI Toolkit ne regarde que droit
  devant ; `AMainMenuScreen.HandleNavigationMove` prend l'élément le plus proche dans la direction,
  diagonale comprise (comme uGUI).
- **Slider** : Unity attend un Submit avant que gauche/droite changent la valeur ; `BindVolume`
  intercepte `NavigationMoveEvent` en TrickleDown (`StopPropagation` + `IgnoreEvent`).
- **Clic sur le fond** : UI Toolkit retire le focus ; `HandlePointerDown` l'empêche.
- **Focus programmatique** : les événements de focus peuvent être différés ; reconnaître le focus
  silencieux par l'élément visé (`FocusSilently`), pas par un booléen posé autour de `Focus()`.
- **`ScrollView.ScrollTo`** ignore les marges et l'agrandissement au focus : `ScrollIntoView`.
- **Ordre d'affichage** panel UI Toolkit / canvas uGUI overlay (écran de chargement, `sortingOrder` 100)
  : trié ensemble d'après la doc, **pas vérifié à l'écran**.
- **Batchmode** : écran virtuel 640×480 (4:3) ; les captures rendent donc le panel dans une texture
  1920×1080 pendant tout le test, et attendent la fin des transitions de caméra (Cinemachine).
- **Outils** : un heredoc Bash contenant une apostrophe française casse la commande (écrire le
  fichier avec l'outil Write) ; ne pas lancer `test` si `quick-compile` échoue :
  `quick-compile | grep -v ": ok" && echo erreurs || test ...`.
