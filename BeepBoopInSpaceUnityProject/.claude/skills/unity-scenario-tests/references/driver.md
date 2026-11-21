# ScenarioDriver : API et règles d'extension

`Assets/Tests/PlayMode/ScenarioDriver.cs`. Les membres statiques sont importés dans les tests par
`using static Game.Tests.PlayMode.ScenarioDriver;`.

## Manettes et inputs

| Membre | Usage |
| --- | --- |
| `Gamepads`, `MainPad` | 4 manettes : 1 et 3 XInput (Xbox), 2 et 4 DualShock 4. `MainPad` (manette 1) est le joueur principal : c'est lui qui pilote l'UI (`NavigationAuthorityManager`) |
| `Press(button, holdFrames = 2)` | Appui + relâchement (coroutine) |
| `Submit()` | A/Croix de la manette 1 |
| `Select(element)` | Amène le focus UI Toolkit sur cet élément à la croix : chaque appui va du côté où l'élément est à l'écran, un appui sans effet est retenté dans une autre direction (20 appuis au plus) |
| `DpadButton(direction)` | La flèche de la croix de la manette 1 (`MoveDirection`) |
| `AddKeyboardAndMouse()`, `Keyboard`, `Mouse` | Clavier et souris virtuels (test des menus seulement ; retirés par `RemoveKeyboardAndMouse()` dans le TearDown) |
| `PointAt(element)`, `Click()` | Amène la souris virtuelle au centre d'un élément (la souris focalise ce qu'elle survole), clic gauche |
| `MashButtonsUntil(stop, random, timeout)` | Chaque manette appuie au hasard sur les 6 boutons d'action jusqu'à `stop()` |
| `Set(button, value)` | Écriture brute d'un bouton (fonctionne quel que soit le format mémoire de la manette) |

Contrôles du jeu (`Global/PlayerManagement/PlayerControls.inputactions`) : MainMenu `Pop` (Ouest),
`SwitchToNextCharacter` (Nord) ; FlowControl `Ready` (Sud) ; Gameplay `Action_1..6` (Sud, Nord,
Est, Ouest, gâchette haute droite, gauche). L'UI utilise `DefaultInputActions` du package (Submit =
Sud, Navigate = croix, sticks, flèches et ZQSD/WASD, Cancel = Est ou Échap). Tant qu'aucun joueur n'a
rejoint, tous les périphériques pilotent l'UI ; ensuite, seuls ceux du joueur principal.

## Attendre et échouer

- `WaitUntil(what, condition, timeout)` : délai en secondes **réelles** (le jeu tourne en ×4).
  Écrire `what` comme une phrase lisible dans le rapport (« the hub », « player 3 to join... »).
- `Fail(message)` : échec avec l'étape, l'état du jeu (`DescribeGame()`) et les erreurs déjà vues.
- `Step(name)` : nomme l'étape (logue `[Scenario] name`, préfixe les erreurs suivantes).
- `Frames(n)`, `RealSeconds(s)`.

## Lire l'état du jeu (réflexion)

| Membre | Retour |
| --- | --- |
| `Manager("Namespace.Type")` | L'instance vivante d'un `AManager<T>` (propriété statique `Instance`), ou null |
| `Get(obj, "membre")` | Propriété ou champ, public ou privé, déclaré sur le type ou un parent |
| `FindActive("Namespace.Type")` | Le composant actif de ce type dans la scène |
| `CurrentStateOf(contexte)`, `MenuState`, `GameplayState` | Nom du type de l'état courant de la `FlowMachine` du contexte |
| `IsOnMainMenuHome`, `PlayerCount`, `CurrentLevelName`, `Scores` | Raccourcis |
| `FocusedElement`, `NameOf(focusable)` | L'élément UI Toolkit qui a le focus (la sélection du menu, tous périphériques confondus) et son nom |
| `ScreenElement("Namespace.Screen", "nom")` | Un élément de l'écran actif, par son nom dans l'UXML (`credits-button`...) |

Règles :
- Toujours un nom de type complet (`Game.MainMenu.Hub.HubScreen`), jamais une référence au type.
- Un champ privé lu par réflexion (`m_trainingButton`) casse si on le renomme : l'échec le dira
  (`MissingMemberException`). Mettre à jour le nom dans le test.
- Pour un nouvel écran : récupérer ses boutons par `ScreenElement("...Screen", "xxx-button")` (nom
  dans l'UXML) ou par un champ de l'écran (`Get(FindActive("...Screen"), "m_xxxButton")`), puis
  `Select` + `Submit`, puis `WaitUntil` sur l'état suivant (`MenuState == "XxxState"`).
- Les éléments sont reclonés à chaque ouverture d'un écran : relire l'élément après chaque changement
  d'écran, jamais le garder d'une visite à l'autre.

## Captures d'écran

`StartRenderingToTextures()` fait rendre la caméra et le panel des menus dans des textures 1920x1080
(le batchmode n'a pas de vue Game et son écran n'est pas en 16:9 : sans ça, la mise en page diffère),
`CaptureScreen("nom")` enregistre `Logs/screenshots/nom.png` (ignoré par git), `StopRenderingToTextures()`
remet tout en place (appelé par le TearDown). Attendre la fin des transitions de caméra avant de
capturer (`WaitForCameraBlend()` dans les tests). Lire les PNG pour vérifier un écran soi-même.

## Pièges des manettes virtuelles

- **Une seule écriture par manette et par frame** : deux événements de la même frame partent du
  même état de base, le second écrase le premier (un relâchement puis un appui = bouton resté
  enfoncé). `MashButtonsUntil` alterne appui et relâchement sur des frames différentes.
- **La croix d'une DualShock 4 HID ne s'écrit pas bouton par bouton** (c'est un « hat switch ») :
  naviguer avec la manette 1 (XInput).
- Les schémas de contrôle exigent `<XInputController>` ou `<DualShockGamepad>` : une simple
  `Gamepad` générique ne pourrait pas rejoindre.
- Les manettes sont créées une fois pour toute la classe (`OneTimeSetUp`) : les joueurs restent
  appairés entre deux scénarios, comme dans le vrai jeu. L'écran de sélection des personnages
  retire tous les joueurs à chaque entrée, puis chaque manette rejoint à nouveau.
- **Clavier et souris en batchmode** : l'application n'a jamais le focus et il n'y a pas de vue Game,
  donc l'Input System ignore clavier et souris par défaut. `AddKeyboardAndMouse()` passe
  `backgroundBehavior` à `IgnoreFocus` et `editorInputBehaviorInPlayMode` à
  `AllDeviceInputAlwaysGoesToGameView`, `RemoveKeyboardAndMouse()` les remet.
- `MouseState.WithButton(...)` modifie la structure sur laquelle on l'appelle : pour un relâchement,
  créer un nouveau `MouseState`.
