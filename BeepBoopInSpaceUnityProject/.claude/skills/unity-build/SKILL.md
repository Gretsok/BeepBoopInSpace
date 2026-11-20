---
name: unity-build
description: Construire Beep Boop dans l'espace pour Windows en headless — build du joueur (Addressables compris), dossier daté dans Builds/, zip prêt à partager, puis lancement de contrôle du jeu buildé avec analyse de son log. À utiliser quand l'utilisateur demande un build, un exe, une version à envoyer ou à faire tester, un zip pour des testeurs, un build de debug ou de développement, ou veut savoir si le jeu buildé démarre sans erreur, et quand un build ou un run de Unity laisse des fichiers modifiés dans git status (link.xml, ProjectSettings.asset, police Magnetar SDF).
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

- **Ne partager que les builds release.** Un build de développement ouvre une connexion pour le
  profiler (écoute TCP + broadcast UDP, « Player connection » dans son log) : Windows demande alors
  d'autoriser le jeu dans le pare-feu (vu le 28/10/2026, règles créées pour les seuls dossiers
  `_Debug`). Un build release n'ouvre aucun port. L'avertissement SmartScreen (« Windows a protégé
  votre ordinateur ») est autre chose : exe non signé téléchargé d'Internet ; voir § 4.

- Sortie : `Builds/BeepBoop_Win_<aaaammjj>_<hhmm>[_Debug]/Beep Boop dans l'espace.exe`, et avec
  `--zip` l'archive `Builds/BeepBoop_Win_<...>.zip` à côté (le dossier du build à la racine de
  l'archive, comme les builds précédents). `--output <dossier>` pour un autre emplacement.
- Le dossier que Unity marque « à ne pas distribuer » (infos de debug Burst :
  `*_BackUpThisFolder_ButDontShipItWithYourGame` depuis Unity 6.6, `*_DoNotShip` avant) reste dans
  le dossier du build mais est exclu du zip. `zip <dossier>` refait l'archive d'un build existant.
- Après un build réussi, les fichiers de `Distribution/` (`LISEZ-MOI.txt`, `THIRD-PARTY-NOTICES.txt`) sont
  copiés à côté de l'exe, donc dans le zip.
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

### Ce que le build écrit dans le projet

Un build, headless ou lancé depuis l'éditeur, ne doit laisser **aucun fichier modifié** dans
`git status`. Trois packages écrivent pourtant dans le projet pendant un build ; chacun est
neutralisé (vérifié le 04/11/2026 : build puis `git status` identique) :

| Fichier | Qui l'écrit et pourquoi | Ce qui le neutralise |
| --- | --- | --- |
| `Assets/AddressableAssetsData/link.xml` (+ `.meta`) | Addressables le copie avant le build pour le stripping du code (`AddressablesPlayerBuildProcessor.PrepareForPlayerbuild`) et ne le supprime qu'au démarrage suivant de Unity | Ignoré par le `.gitignore` du dépôt. Le laisser : il disparaît tout seul |
| `ProjectSettings/ProjectSettings.asset`, `preloadedAssets` | Input System ajoute `InputSystem_Actions` aux preloaded assets avant le build et les retire après, **en mémoire seulement**, alors que Unity enregistre les réglages pendant le build. Une fois enregistrées (build depuis l'éditeur sans sauvegarde ensuite), il ne les ajoute ni ne les retire plus jamais : fichier modifié à chaque build du 27/10 au 04/11/2026 | `Assets/Game/Tools/Build/Editor/PreloadedAssetsBuildCleanup.cs` : avant le build, retire les actions si elles sont restées dans les réglages ; après, ré-enregistre les réglages. Ne jamais commiter un `preloadedAssets` non vide |
| `Assets/RawAssets/UI/Fonts/BeepBoop SDF.asset` (et `Assets/RawAssets/Fonts/magnetar_2/Magnetar SDF.asset`, plus utilisée en jeu) | Police TMP dynamique avec « Clear Dynamic Data On Build » : TMP vide ses tables de glyphes et réduit son atlas à 1×1 avant chaque build (`TMP_PreBuildProcessor`) **et à la fermeture de Unity** si l'atlas a servi (`EditorApplication.quitting`, `TMP_EditorResourceManager.cs`), donc aussi après `compile`, `test` ou l'éditeur | L'état vidé est l'état de repos, celui qui est commité : un build ou une fermeture le réécrit à l'octet près. La police ne se remplit que pendant une session (éditeur, tests) et se vide à la fermeture (vérifié le 05/11/2026 : `Clearing [BeepBoop SDF]` dans le log, `git status` sans la police après chaque run de tests) |

- Ne jamais commiter la police **remplie** (`m_Width: 1024` sur sa texture, tables de glyphes
  pleines) : fermer Unity la vide. Une nouvelle police TMP dynamique avec « Clear Dynamic Data On
  Build » se comporte pareil : la commiter vidée, après une fermeture de Unity.
- Un autre fichier modifié après un build vient d'un nouveau mécanisme : trouver son auteur dans
  `Library/PackageCache` avant de le commiter ou de l'ignorer. Exception connue : la mise à jour du
  format de `Assets/Settings/PC_RPAsset.asset` (voir Repères).

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
- GameAnalytics ne démarre qu'après « Oui » à la question des statistiques, et jamais avec `-no-analytics`
  (que `run-build` et `relay-test` passent toujours, sauf `run-build --analytics`). La sauvegarde de la machine
  (`saveProfile.sav`, `AnalyticsConsent` : 1 oui, -1 non, 0 pas de réponse) dit oui sur le PC de l'utilisateur :
  sans `-no-analytics`, un lancement enverrait une session. Pour voir la question au premier lancement, mettre
  `AnalyticsConsent` à 0 le temps du run puis remettre le fichier à l'octet près. Vérifié le 19/11/2026 sur un premier lancement sans sauvegarde : aucune
  connexion réseau du jeu, aucune ligne GameAnalytics dans son log, fermeture normale.

La fenêtre est indispensable : un joueur en `-batchmode` ne fait pas tourner sa boucle de rendu,
donc un build qui n'affiche qu'un **écran noir** (pipeline URP qui ne se crée pas) y passe sans
erreur. C'est arrivé le 27/10/2026 avec le build de 12h44 : contrôle `-batchmode` vert, écran noir
chez l'utilisateur. `--headless` (`-batchmode -nographics`) reste disponible pour un contrôle sans
fenêtre, mais il ne vérifie que le démarrage logique.

Ce contrôle couvre le démarrage et le menu, pas une partie. Pour une partie dans le build :

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py run-build "Builds/<dossier>/Beep Boop dans l'espace.exe" --smoke --seconds 1200
```

(`--smoke` seul attend aussi 1200 s ; sans `--smoke`, le délai par défaut est de 20 s.)

Le jeu (`Game.Tools.Smoke.DemoSmokeTest`, argument `-demo-smoke`) joue seul deux tournois : un joueur au clavier
qui ne bouge pas et 3 robots, en ×3, en 1280×720 et en vrai rendu, puis quitte (code 0 si aucune erreur
loguée, 1 sinon, 10 s après une exception, 2 après 20 min). Le premier part du menu intégré au build et va à son
terme ; le second part du menu revenu par son bundle (voir Repères), passe par le hub et finit à la première
victoire (`TournamentResultsManager.ScoreToReachOverride`). À chaque lancement il contrôle les personnages : ceux de
la banque, tous différents, aucun robot sans ses images (lignes `[DemoSmoke] Tournament <n> with: ...`). Ses images
sont dans `Logs/claude-runs/<date>-player/captures/` (`tournament-2-...` pour le second) : les regarder. C'est le
seul contrôle d'une partie locale avec les bundles Addressables et l'affichage réels.

La commande affiche aussi la ligne `[DemoSmoke] Performance: ...` du log : images par seconde en moyenne et sur le
1 % des images les plus lentes (temps réel, `unscaledDeltaTime`, pendant `GameplayState` seulement), pire image,
résolution, vSync, fréquence de l'écran, carte graphique, processeur, mémoire. Avec `vSync 1`, la moyenne plafonne à
la fréquence de l'écran : elle dit si le jeu tient cette cadence, pas sa marge. Le ×3 ne change pas le rendu, mais les
mises à jour à pas fixe tournent plus souvent. Hors focus le jeu s'arrête (`runInBackground: 0`) : garder la fenêtre
au premier plan.

Pour le jeu en ligne par les vrais services :

```bash
python .claude/skills/unity-headless/scripts/unity_cli.py relay-test "Builds/<dossier>/Beep Boop dans l'espace.exe"
```

Deux copies du build jouent seules une salle réelle (Multiplayer Services, Relay, code), sans fenêtre, en ~4 min.
C'est aussi le seul contrôle d'un **hôte** dans un build. Détails et limites : skill `online-multiplayer`, section
Services Unity.

## 4. Rendre compte

Donner le chemin du dossier et du zip, leur taille, la durée, et le résultat du lancement de
contrôle. `Builds/` pèse vite plusieurs Go : proposer de supprimer les anciens builds, sans le faire
sans accord.

- Les testeurs qui téléchargent le zip voient SmartScreen (« Windows a protégé votre ordinateur »,
  éditeur inconnu) : l'exe n'est pas signé et Windows marque les fichiers venus d'Internet. Leur dire :
  clic droit sur le zip → Propriétés → cocher « Débloquer » **avant** d'extraire, ou « Informations
  complémentaires → Exécuter quand même ». Le supprimer demande une signature de code (certificat
  d'éditeur, payant, à la charge de l'utilisateur) puis de la réputation auprès de Microsoft, ou une
  distribution par Steam / l'app itch.io, qui ne marquent pas les fichiers.

## Repères

- **Les tests dans l'éditeur ne voient pas les bundles Addressables** (Asset Database). Dans un
  build, un bundle se décharge quand plus aucune scène ni handle ne le tient, et **détruit ses
  assets même si un objet persistant (DontDestroyOnLoad) les référence encore** : ils deviennent
  « null » pour Unity. Vu le 04/11/2026 : la config du tournoi et ses `LevelDataAsset` (dans le bundle
  du groupe `Game-Tournament`, avec ses scènes) détruits avant la 2e partie, « No current level data
  asset » et partie jamais chargée. Corrigé : `TournamentContext` garde un handle Addressables sur sa
  config. Un nouvel objet persistant qui garde des assets d'un groupe Addressables doit en faire
  autant. Seul un test qui joue dans un build le voit : le build invité des tests en ligne (skill
  `online-multiplayer`).
- **Deux copies des personnages, et leur bundle à tenir.** `MainMenu.unity` est dans les Build Settings (premier
  menu d'une session : les copies du build) et dans un groupe Addressables (menus suivants : les copies du bundle
  `Game-Characters`). Un joueur garde son `CharacterDataAsset` d'un menu à l'autre : `MainMenuOrchestrator.Initialize`
  le remplace par celui de la banque du menu, de même nom (`CharacterBankManager.RemapCharactersOf`), et
  l'`OnlineManager` reprend la banque en revenant dans sa salle. Sinon : un robot prend le personnage de son joueur,
  une salle ouverte ensuite donne le rang -1 donc le robot 0, la grille perd le joueur et la croix ne le déplace plus.
  Chaque scène jouée avec ces personnages doit tenir le bundle, par un `AddressablesAnchorInSceneReferencer` :
  `GM_BaseGame` et `TournamentBootScene`, qui vaut pour tout le bundle de ses scènes (les scènes d'un bundle chargent
  toutes ses dépendances). Sans elle, passer du menu revenu par son bundle au tournoi détruit les personnages de
  ses joueurs, et `CharacterReferencesHolder.SetCharacterData` lève une exception (analyse du layout du 05/11/2026).
  Contrôle après un build : dans `Library/com.unity.addressables/buildlayout.json`, `game-tournament_scenes` dépend
  du bundle de `Game-Characters`. `run-build --smoke` joue ce parcours.
- **`GameplayContext.IsInitialized` arrive avant son `CurrentLevelInfoManager`**, posé seulement quand le niveau
  est chargé (`GameplayContext.Start`) : tout code qui lit le niveau en cours dès l'initialisation l'attend. Le smoke
  le lisait trop tôt (`NullReferenceException` dans `DemoSmokeTest.PlayGame`, 19/11/2026).
- Build vers un dossier fixe pour un test : `build --output Builds/OnlineGuest` (écrase le
  précédent). Le 04/11/2026, un build release sans changement de shaders a pris 30 s à 2 min 30
  (152 s pour le premier après l'ajout des packages réseau).

- 27/10/2026, Unity 6000.6.7f1, premier build après la migration : 1034 s (dont 982 s de build,
  surtout des shaders), 268 Mo, zip de 148 Mo. Écran noir au lancement : URP 17.6 strippe les
  ressources des render features **désactivées** (ici le SSAO de `PC_Renderer`) mais les initialise
  quand même, d'où une exception à la création du pipeline puis `Blitter is already initialized` à
  chaque frame. Corrigé en retirant la feature désactivée. Une render feature désactivée qui ne sert
  plus doit être retirée du renderer, pas seulement décochée.
- Pendant le build, Unity peut ré-écrire `Assets/Settings/PC_RPAsset.asset` (nouveaux champs
  d'URP) : c'est la mise à jour du format, à commiter avec le reste.
