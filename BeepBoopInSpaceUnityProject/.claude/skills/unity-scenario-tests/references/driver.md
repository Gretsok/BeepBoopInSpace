# ScenarioDriver : API et règles d'extension

`Assets/Tests/PlayMode/ScenarioDriver.cs`. Les membres statiques sont importés dans les tests par
`using static Game.Tests.PlayMode.ScenarioDriver;`.

## Manettes et inputs

| Membre | Usage |
| --- | --- |
| `Gamepads`, `MainPad` | 4 manettes : 1 et 3 XInput (Xbox), 2 et 4 DualShock 4. `MainPad` (manette 1) est le joueur principal : c'est lui qui pilote l'UI (`NavigationAuthorityManager`) |
| `Press(button, holdFrames = 2)` | Appui + relâchement (coroutine) |
| `Submit()` | A/Croix de la manette 1 |
| `Select(selectable)` | Amène la sélection UI sur ce `Selectable` à la croix, en suivant le graphe de navigation de Unity (chemin calculé, puis vérifié à chaque pas) |
| `MashButtonsUntil(stop, random, timeout)` | Chaque manette appuie au hasard sur les 6 boutons d'action jusqu'à `stop()` |
| `Set(button, value)` | Écriture brute d'un bouton (fonctionne quel que soit le format mémoire de la manette) |

Contrôles du jeu (`Global/PlayerManagement/PlayerControls.inputactions`) : MainMenu `Pop` (Ouest),
`SwitchToNextCharacter` (Nord) ; FlowControl `Ready` (Sud) ; Gameplay `Action_1..6` (Sud, Nord,
Est, Ouest, gâchette haute droite, gauche). L'UI utilise `DefaultInputActions` du package (Submit =
Sud, Navigate = croix et sticks).

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
| `IsOnMainMenuHome`, `PlayerCount`, `CurrentLevelName`, `Scores`, `CurrentSelection` | Raccourcis |

Règles :
- Toujours un nom de type complet (`Game.MainMenu.Hub.HubScreen`), jamais une référence au type.
- Un champ privé lu par réflexion (`m_trainingButton`) casse si on le renomme : l'échec le dira
  (`MissingMemberException`). Mettre à jour le nom dans le test.
- Pour un nouvel écran : récupérer ses boutons par `Get(FindActive("...Screen"), "m_xxxButton")`,
  puis `Select` + `Submit`, puis `WaitUntil` sur l'état suivant (`MenuState == "XxxState"`).

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
