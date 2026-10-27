---
name: unity-headless
description: Vérifier un changement C# et piloter Unity sans l'éditeur sur Beep Boop dans l'espace — compilation en 5 secondes avec le compilateur de Unity, compilation complète headless, détection de l'éditeur ouvert, lancement de Unity en arrière-plan pour les tests et les builds. À utiliser après TOUTE modification de code C# (avant de dire que c'est fini), avant un commit, quand il faut lancer Unity en ligne de commande, ou quand l'utilisateur demande « est-ce que ça compile », « vérifie », « teste » — même s'il ne parle pas de Unity.
---

# Unity sans l'éditeur

But : vérifier soi-même que le projet compile et lancer Unity en arrière-plan, sans demander à
l'utilisateur d'ouvrir l'éditeur. Validé sous Windows 11, Unity 6000.6.7f1, Python 3, Git Bash.

Tout passe par un seul outil, à lancer **depuis le dossier du projet Unity**
(`BeepBoopInSpaceUnityProject/`, celui qui contient `Assets/`) :

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py <commande>
```

| Commande | Rôle | Durée |
| --- | --- | --- |
| `status` | Version de Unity, exécutable trouvé, éditeur ouvert ou non | instantané |
| `quick-compile` | Compile tout le C# du projet avec le Roslyn de Unity, sans lancer Unity | ~5 s |
| `compile` | Import + compilation complète en headless (la vérification qui fait foi) | 1-3 min |
| `test` | Tests Unity Test Framework (voir le skill `unity-scenario-tests`) | 1-12 min |
| `wait <pid>` | Attend la fin d'un processus lancé en détaché | — |
| `results <xml>` | Résumé d'un fichier de résultats de tests | instantané |
| `log-errors <log>` | Erreurs distinctes d'un log Unity, avec leur nombre et leur ligne de code | instantané |
| `log-flow <log>` | Transitions des FlowMachine et étapes de scénario, dans l'ordre | instantané |
| `build` / `zip` / `run-build` | Build Windows, archive, lancement de contrôle (skill `unity-build`) | jusqu'à 17 min |

Chaque lancement de Unity écrit son log dans `Logs/claude-runs/<date>-<nom>/` (ignoré par git).
Codes de sortie : 0 succès, 1 échec, 3 projet déjà ouvert par Unity.

## 1. Après chaque modification de C#

1. `quick-compile` — toujours, c'est gratuit. Il prend la liste des fichiers, les références et les
   defines de la dernière compilation faite par Unity (`Library/Bee`), ajoute les nouveaux `.cs` à
   la bonne assembly (asmdef le plus proche, sinon `Assembly-CSharp`, `-Editor` dans un dossier
   `Editor/`), puis compile les assemblies du projet dans l'ordre de leurs dépendances.
   Viser **0 erreur et 0 warning** dans `Assets/` : le projet est propre, un warning nouveau vient
   du changement.
2. `compile` quand `quick-compile` ne suffit pas : nouvel `.asmdef` ou références modifiées,
   packages changés (`Packages/manifest.json`), nouveaux defines, ou avant un commit important.
   Elle détecte aussi les erreurs d'import, que le compilateur seul ne voit pas.
3. Pour le comportement en jeu : skill `unity-scenario-tests`.

## 2. Quand Unity a déjà le projet ouvert

`compile`, `test` et `build` s'arrêtent avec le code 3 si `Temp/UnityLockfile` est verrouillé :
deux Unity ne peuvent pas ouvrir le même projet. Un `UnityLockfile` présent mais non verrouillé
(éditeur fermé brutalement) est normal et ne bloque rien.

- C'est l'éditeur de l'utilisateur : lui demander de le fermer, en disant pourquoi (« pour que je
  lance les tests en arrière-plan »). En attendant, `quick-compile` marche.
- C'est un de nos runs headless : attendre sa fin (`wait <pid>` ou un Monitor).

## 3. Commandes longues (plus de 8 minutes)

Les outils Bash / PowerShell coupent au bout de 10 minutes. Pour une suite de tests complète ou un
build :

1. lancer en détaché : `test ... --detach` affiche le PID, le log et la commande de résumé ; pour
   `build`, passer par `Start-Process python ... -RedirectStandardOutput <fichier> -PassThru`
   (PowerShell) ;
2. attendre avec l'outil **Monitor** (`timeout_ms` jusqu'à 1 800 000), sur une boucle qui se termine
   quand le PID disparaît (`tasklist //FI "PID eq <pid>"`), puis afficher le résultat ;
3. ne pas sonder en boucle entre-temps : avancer sur autre chose.

## Pièges connus

- **Git Bash convertit les arguments qui commencent par `/`** en chemins Windows (`/nostdlib`
  devient `C:/Program Files/Git/nostdlib`). L'outil Python évite le problème. Pour un appel manuel,
  utiliser `-nostdlib` ou préfixer avec `MSYS_NO_PATHCONV=1`.
- **Ne jamais passer `-quit` avec `-runTests`** : Unity quitterait avant d'avoir lancé les tests.
- **Les écritures dans le registre faites depuis l'outil PowerShell restent dans son bac à sable** :
  la commande « réussit » et relire depuis PowerShell montre le changement, mais le vrai registre
  (celui que lit le jeu) n'a pas bougé. Modifier le registre de l'utilisateur depuis Python
  (`winreg`, lancé par l'outil Bash), puis vérifier avec `reg query` lancé depuis Bash.
- `quick-compile` ne voit que ce que Unity a déjà compilé une fois : sans `Library/Bee` (projet
  fraîchement cloné), lancer `compile` d'abord.
- Un projet ouvert pour la première fois dans une nouvelle version de Unity ré-écrit des centaines
  de `.meta` et de `.asset` : ce n'est pas une régression (voir le skill `unity-upgrade`).
- Le Test Runner copie `TestResults.xml` (plusieurs Mo) dans le dossier de données du jeu
  (`AppData/LocalLow/Confiture De Jeux/Beep Boop dans l'espace`) : `test`, `results` et
  `wait --cleanup-test-results` le suppriment.
- Les lignes `[Licensing::...] Error` en tête de log sont du bruit, pas des erreurs du projet.
- Le dossier `Library/` est un cache : en cas d'erreurs incompréhensibles après un changement de
  version, le supprimer (projet fermé) force un ré-import complet, long (10+ min).
