# Beep Boop dans l'espace

Jeu multijoueur local (2 à 4 joueurs à la manette, ou clavier/souris) de Confiture De Jeux.
Unity **6000.6.7f1**, URP 17.6, Input System, Addressables, UniTask, DOTween, NaughtyAttributes,
GameAnalytics 8.3. Windows uniquement pour l'instant.

L'utilisateur écrit en français ou en anglais : répondre dans la langue de son dernier message.

## Dépôt

- Racine git : `BeepBoopInSpace/` (`README.md`, `.gitignore`). Projet Unity : `BeepBoopInSpaceUnityProject/`
  (ce dossier, d'où partent les sessions et les commandes des skills).
- Ignorés : `Library/` (cache, régénérable), `Logs/` (dont `Logs/claude-runs/`, les runs headless),
  `Builds/` (builds Windows), `Temp/`, `UserSettings/`.
- Code du jeu : `Assets/Game/<Domaine>/`, sans `.asmdef` (tout dans `Assembly-CSharp` / `-Editor`).
  Tests : `Assets/Tests/PlayMode/` (`BeepBoop.Tests.PlayMode`). Tiers : `Assets/PinwheelStudio`
  (Contour, post-process non utilisé en jeu), `Assets/Plugins/Demigiant` (DOTween).

## Architecture

| Notion | Où | À savoir |
| --- | --- | --- |
| `AManager<T>` | `ArchitectureTools/Manager` | Singleton par contexte (`Instance`), `Initialize()` en coroutine, `RegisterPostInitializationCallback` pour attendre l'init |
| Contextes | `GlobalContext` (persistant : sauvegarde, réglages, audio, joueurs, EventSystem, analytics), `MainMenuContext`, `GameplayContext`, `TrainingContext` / `TournamentContext` (persistants, instanciés par Addressables) | Le `GlobalContext` en double d'une scène rechargée se détruit |
| `FlowMachine` + `AFlowState` | `ArchitectureTools/FlowMachine` | File de demandes d'état traitées dans `Update`. Logue `[FM] <objet> - <état> - ENTER`. Les demandes ne sont pas dédoublonnées : protéger les doubles demandes |
| Menu principal | `MainMenu/` | `MainMenuOrchestrator` : Home → CharacterSelection → Hub → LevelSelection (Training) ou Tournoi, + Settings, Credits |
| UI du menu principal | `MainMenu/UI` (USS, thème, PanelSettings), un UXML par écran | UI Toolkit : chaque écran (`AMainMenuScreen`) a un `UIDocument`, son arbre est recloné à chaque activation. Le focus est la sélection de tous les périphériques (croix/clavier le déplacent, la souris focalise ce qu'elle survole) ; navigation directionnelle maison façon uGUI, Annuler = `HandleCancel`. Le reste du jeu (pause, résultats, tournoi) est encore en uGUI |
| Partie | `Gameplay/Flows` | Load → SetUp → Introduction → Tutorial (Ready) → Gameplay (→ annonces de manche / configuration) → Finish → Results → End |
| Scènes | Seul `MainMenu` est dans les Build Settings | Tout le reste passe par Addressables. `LoadingManager` charge les scènes et prefabs du mode, `EndState` les libère : toute sortie de partie doit passer par `LoadingManager.Unload` |
| Modes de jeu | `Gameplay/Levels`, `Gameplay/GameModes` | Un mode = un `LevelDataAsset` (objectif + action spéciale + modificateurs + arène) : skill `game-mode-variants` |
| Joueurs | `Global/PlayerManagement`, `Global/NavigationAuthority` | `PlayerInputManager` (rejoindre avec Y/A/Espace dans la sélection des personnages, puis chacun se déclare prêt), schémas Xbox / PS / clavier-souris ; l'UI suit les manettes du joueur principal |

## Conventions

- C# : namespaces `Game.<Dossier>`, champs privés `m_camelCase`, statiques `s_`, constantes `k_`,
  `[field: SerializeField]` sur les auto-propriétés, commentaires rares et en anglais. Code,
  commentaires et noms d'assets en anglais ; textes affichés aux joueurs en français.
- `Debug.LogError` seulement pour un vrai problème : les tests de scénario échouent sur toute erreur
  loguée. Une situation prévue et gérée se logue en `Debug.Log` ou `LogWarning`.
- Fins de ligne : YAML Unity (`.asset`, `.meta`, `.unity`, `.prefab`) en LF dans le dépôt, même si
  la copie de travail est en CRLF ; `.cs` surtout en CRLF. Garder celles du fichier modifié.
- Un `.meta` voyage toujours avec son fichier (création, déplacement, suppression). Ne jamais
  modifier `Library/PackageCache` (régénéré) : voir le skill `unity-upgrade`.
- Les tests ne peuvent pas référencer `Assembly-CSharp` : ils lisent l'état du jeu par réflexion.

## Vérifier un changement

L'utilisateur ne doit pas avoir à tester à notre place :

1. Toute modification C# : `python .claude/skills/unity-headless/scripts/unity_cli.py quick-compile`
   (5 s, viser 0 erreur et 0 warning dans `Assets/`).
2. Gameplay, menus, flow, chargement : tests de scénario filtrés sur ce qui est touché, la suite
   complète avant un commit important (skill `unity-scenario-tests`). Écran modifié : regarder ses
   captures (`unity_cli.py screenshots`, skill `ui-toolkit-screens`).
3. Avant de partager une version : build + lancement de contrôle (skill `unity-build`).

Unity en headless exige l'éditeur fermé : le demander à l'utilisateur en disant pourquoi.

## Skills du projet

| Skill | Quand |
| --- | --- |
| `unity-headless` | Compiler, lancer Unity sans l'éditeur, commandes longues |
| `unity-error-triage` | Erreurs au démarrage, en jeu, dans un log ou un build |
| `unity-scenario-tests` | Jouer de vraies parties à 4 manettes virtuelles, lire et étendre les tests |
| `unity-build` | Build Windows, zip, lancement de contrôle |
| `unity-upgrade` | Nouvelle version de Unity ou des packages |
| `game-mode-variants` | Créer ou modifier un mode de jeu |
| `ui-toolkit-screens` | Modifier, créer ou migrer (depuis uGUI) un écran UI Toolkit, et le vérifier en captures ; charte graphique du menu et motion design |
| `psd-mockups` | Lire une maquette Photoshop (.psd), mesurer ses éléments, comparer les captures du jeu avec elle |
| `unity-yaml` | Lire ou modifier scènes, prefabs, `.asset` et `.meta` sans l'éditeur (arbre, surcharges, références, conversion d'un prefab uGUI) |
| `unity-api-check` | Vérifier une API dans la version de Unity du projet avant de coder dessus |

## Git

- Commiter seulement quand l'utilisateur le demande, sur `main` (pas de branche par défaut).
  Ne jamais pousser sans demande explicite.
- Messages en anglais : un titre court au participe ou nominal (« Fixing respawn placement… »,
  « Upgrading to Unity 6000.6.7f1 », « Game modes scenario tests »), puis une liste à puces des
  changements si besoin.
- Séparer les sujets indépendants en commits distincts (ex. correctifs d'un côté, tests de l'autre).
