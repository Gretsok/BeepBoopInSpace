---
name: unity-error-triage
description: Diagnostiquer les erreurs Unity de Beep Boop dans l'espace — erreurs au lancement ou à l'ouverture du projet, Safe Mode, console rouge, exceptions pendant une partie, crash ou erreurs du jeu buildé (Player.log), échecs des tests de scénario. Trouve le bon log, regroupe les erreurs distinctes avec leur ligne de code, retrace le déroulé du jeu (FlowMachine) et sépare les vraies erreurs du bruit connu. À utiliser dès que l'utilisateur signale une erreur, une exception, « ça plante », « ça ne compile plus », « erreurs au démarrage », même sans parler de log.
---

# Diagnostiquer les erreurs Unity

But : passer de « il y a des erreurs » à « voici les causes, dans l'ordre où les corriger »,
sans demander à l'utilisateur de copier sa console.

Outil commun (depuis le dossier du projet Unity) :
`python .claude/skills/unity-headless/scripts/unity_cli.py <commande>`

## 1. Trouver le bon log

| Situation | Log |
| --- | --- |
| Éditeur ouvert ou dernière ouverture | `Logs/Editor.log` (session précédente : `Logs/Editor-prev.log`). `%LOCALAPPDATA%\Unity\Editor\Editor.log` ne contient que le début et renvoie vers celui du projet |
| Run headless de Claude (compile, tests, build) | `Logs/claude-runs/<date>-<nom>/editor.log` |
| Jeu buildé lancé par l'utilisateur | `%USERPROFILE%\AppData\LocalLow\Confiture De Jeux\Beep Boop dans l'espace\Player.log` (+ `Player-prev.log`) |
| Jeu buildé lancé par `run-build` | `Logs/claude-runs/<date>-player/player.log` |
| Résolution des packages | `Logs/upm.log`, `Logs/Packages-Update.log` |

Le log de l'éditeur ouvert peut être lu à tout moment, l'éditeur n'a pas besoin d'être fermé.

## 2. Lister les erreurs distinctes

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py log-errors Logs/Editor.log
python .claude/skills/unity-headless/scripts/unity_cli.py log-errors <log> --warnings   # avec les warnings
```

Sortie : les erreurs de compilation (fichier, ligne, code CS), puis chaque erreur ou exception
d'exécution avec son nombre d'occurrences et la première ligne du projet dans sa pile
(`at Assets/...:ligne`). Ouvrir cette ligne avant de conclure.

## 3. Corriger dans cet ordre

1. **Erreurs de compilation** : elles bloquent tout (l'éditeur s'ouvre en Safe Mode et aucun
   script ne tourne). Une erreur dans `Library/PackageCache/...` vient d'un package : voir le skill
   `unity-upgrade`, ne pas modifier le cache (il est régénéré).
2. **Packages** non résolus (`upm.log`) : registre injoignable, version absente, dépendance git.
3. **Import** : `.meta` invalides, assets corrompus (ex. « PluginImporter object at version 1 »).
4. **Exécution** : exceptions et `Debug.LogError` pendant le jeu.

Après chaque correction : `quick-compile`, puis reproduire (étape 5).

## 4. Comprendre une erreur d'exécution

Retracer où en était le jeu :

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py log-flow <log>
python .claude/skills/unity-headless/scripts/unity_cli.py log-flow <log> --machine FlowMachine   # seulement la partie en cours
```

Chaque `FlowMachine` logue ses transitions (`[FM] <objet> - <état> - ENTER`) : `Orchestrator` pour
le menu principal, `FlowMachine` pour une partie. Un état qui revient deux fois de suite, ou un
état de jeu après l'écran de résultats, signale une double demande d'état.

Causes déjà rencontrées dans ce projet (vérifier d'abord celles-ci) :

| Symptôme | Cause typique |
| --- | --- |
| `Attempting to load AssetReference that has already been loaded` | Un `AssetReference` d'un `LevelDataAsset` chargé puis jamais libéré : la partie précédente n'est pas passée par `EndState` → `LoadingManager.Unload` |
| `NullReferenceException` dans `CharacterMovementController.Walk*` | Personnage sans case (`GridWalker.CurrentCell == null`) : mort et détaché, ou téléportation refusée |
| `Cannot teleport to cell.` | Case cible occupée par un autre personnage au moment de la téléportation |
| `ArgumentOutOfRangeException` dans `ResultsState` | Écran de résultats rejoué : une fin de partie demandée deux fois |
| `Could not find a neighbour valid cell` | Recherche de respawn partie d'une case nulle ou grille pleine |
| `NullReferenceException` dans un `*ObjectiveManager.SetUp`, partie bloquée au chargement | Mode qui combine un objectif et une action spéciale incompatibles (ex. `MostKills` sans le fusil de sniper) : skill `game-mode-variants` |

## 5. Reproduire et vérifier

Rejouer la situation avec les tests de scénario (skill `unity-scenario-tests`), filtrés sur le mode
concerné. Une erreur aléatoire (dépend des positions) peut demander 2 ou 3 runs.

## Bruit connu (ne pas « corriger »)

- `[Licensing::Client] Error: HandshakeResponse ...` en tête de log.
- `GameAnalytics: Unsupported platform ... WindowsEditor` : normal dans l'éditeur.
- `GameAnalytics: DontDestroyOnLoad only works for root GameObjects` : le composant est enfant de
  `GlobalContext`, qui est lui-même persistant. Déjà présent avec les versions précédentes.
- `Destroying duplicate GameAnalytics object` au retour au menu : le `GlobalContext` de la scène
  est détruit au profit de celui déjà persistant.
- `There are no audio listeners in the scene` pendant les chargements de scène.
- `fallback shader 'VertexLit' not found`, `DependencyGraph file is either corrupted or from a previous version` (après un changement de version).
