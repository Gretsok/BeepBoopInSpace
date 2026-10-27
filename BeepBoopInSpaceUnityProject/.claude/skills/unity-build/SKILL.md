---
name: unity-build
description: Construire Beep Boop dans l'espace pour Windows en headless — build du joueur (Addressables compris), dossier daté dans Builds/, zip prêt à partager, puis lancement de contrôle du jeu buildé avec analyse de son log. À utiliser quand l'utilisateur demande un build, un exe, une version à envoyer ou à faire tester, un zip pour des testeurs, un build de debug ou de développement, ou veut savoir si le jeu buildé démarre sans erreur.
---

# Build Windows

But : produire un build jouable et partageable sans ouvrir l'éditeur, et vérifier qu'il démarre.

Outil commun (depuis le dossier du projet Unity) :
`python .claude/skills/unity-headless/scripts/unity_cli.py <commande>`.
Le build passe par `Assets/Game/Tools/Build/Editor/CommandLineBuild.cs`
(`Game.Tools.Build.Editor.CommandLineBuild.BuildWindows`).

## 1. Avant de builder

- Éditeur fermé (sinon code 3).
- Idéalement : la suite de tests de scénario passe (skill `unity-scenario-tests`), et l'arbre git
  est propre ou les changements sont commités, pour savoir ce que contient le build.
- Version : `bundleVersion` dans `ProjectSettings/ProjectSettings.asset` (actuellement `0.1.0`).
  Ne la changer que si l'utilisateur le demande.

## 2. Builder

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py build --zip                # release
python .claude/skills/unity-headless/scripts/unity_cli.py build --development --zip  # dossier suffixé _Debug
```

- Sortie : `Builds/BeepBoop_Win_<aaaammjj>_<hhmm>[_Debug]/Beep Boop dans l'espace.exe`, et avec
  `--zip` l'archive `Builds/BeepBoop_Win_<...>.zip` à côté (le dossier du build à la racine de
  l'archive, comme les builds précédents). `--output <dossier>` pour un autre emplacement.
- Le dossier que Unity marque « à ne pas distribuer » (infos de debug Burst :
  `*_BackUpThisFolder_ButDontShipItWithYourGame` depuis Unity 6.6, `*_DoNotShip` avant) reste dans
  le dossier du build mais est exclu du zip. `zip <dossier>` refait l'archive d'un build existant.
- Addressables : le contenu est construit avec le joueur quand le réglage du projet (ou la
  préférence de l'éditeur) le prévoit ; sinon `CommandLineBuild` le construit d'abord. Un build sans
  contenu Addressables démarrerait sur un menu vide.
- Durée : jusqu'à ~17 minutes quand les shaders sont recompilés (premier build après un changement
  de version de Unity ou d'URP, `Library/` vidé), nettement moins ensuite. Toujours lancer en
  détaché et attendre avec Monitor (skill `unity-headless`, section 3) :
  ```powershell
  Start-Process python -ArgumentList @('".claude/skills/unity-headless/scripts/unity_cli.py"','build','--zip') -RedirectStandardOutput Logs/build-out.txt -WindowStyle Hidden -PassThru
  ```
  puis un Monitor qui attend la fin du PID et affiche `Logs/build-out.txt`.

En cas d'échec : la commande affiche les erreurs distinctes du log (`log-errors`) ; le log
complet est dans `Logs/claude-runs/<date>-build/editor.log`.

## 3. Vérifier que le build démarre

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py run-build "Builds/<dossier>/Beep Boop dans l'espace.exe" --seconds 20
```

Lance le jeu **dans une petite fenêtre** (960×540, fenêtré) pendant 20 s, l'arrête, affiche le
dernier état atteint et liste les erreurs de son log (`Logs/claude-runs/<date>-player/player.log`).
Prévenir l'utilisateur qu'une fenêtre va s'ouvrir quelques secondes.

- Unity enregistre les options d'écran avec lesquelles le jeu est lancé dans ses préférences
  (registre `HKCU\Software\Confiture De Jeux\Beep Boop dans l'espace`, valeurs `Screenmanager ...`) :
  sans précaution, le jeu de l'utilisateur rouvrirait en fenêtre 960×540 (arrivé le 27/10/2026).
  `run-build` copie ces préférences avant le lancement et les remet à l'identique après.
- Le jeu est arrêté de force : son log s'arrête net, ce n'est pas un crash. Un vrai crash laisse
  `Crash!!!` dans le log et un dossier `%LOCALAPPDATA%\Temp\Confiture De Jeux\Beep Boop dans l'espace\Crashes`.
- Chaque lancement envoie une session à GameAnalytics (le jeu buildé envoie ses événements) : ne
  pas multiplier les lancements de contrôle inutiles.

La fenêtre est indispensable : un joueur en `-batchmode` ne fait pas tourner sa boucle de rendu,
donc un build qui n'affiche qu'un **écran noir** (pipeline URP qui ne se crée pas) y passe sans
erreur. C'est arrivé le 27/10/2026 avec le build de 12h44 : contrôle `-batchmode` vert, écran noir
chez l'utilisateur. `--headless` (`-batchmode -nographics`) reste disponible pour un contrôle sans
fenêtre, mais il ne vérifie que le démarrage logique.

Ce contrôle couvre le démarrage et le menu, pas une partie : les parties se vérifient avec les
tests de scénario, dans l'éditeur (où les ressources ne sont pas strippées comme dans un build).

## 4. Rendre compte

Donner le chemin du dossier et du zip, leur taille, la durée, et le résultat du lancement de
contrôle. `Builds/` pèse vite plusieurs Go : proposer de supprimer les anciens builds, sans le faire
sans accord.

## Repères

- 27/10/2026, Unity 6000.6.7f1, premier build après la migration : 1034 s (dont 982 s de build,
  surtout des shaders), 268 Mo, zip de 148 Mo. Écran noir au lancement : URP 17.6 strippe les
  ressources des render features **désactivées** (ici le SSAO de `PC_Renderer`) mais les initialise
  quand même, d'où une exception à la création du pipeline puis `Blitter is already initialized` à
  chaque frame. Corrigé en retirant la feature désactivée. Une render feature désactivée qui ne sert
  plus doit être retirée du renderer, pas seulement décochée.
- Pendant le build, Unity peut ré-écrire `Assets/Settings/PC_RPAsset.asset` (nouveaux champs
  d'URP) : c'est la mise à jour du format, à commiter avec le reste.
