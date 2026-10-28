---
name: unity-scenario-tests
description: Lancer, lire, déboguer et étendre les tests de scénario de Beep Boop dans l'espace — de vraies parties jouées en headless par 4 manettes virtuelles (menu, sélection des personnages, mode de jeu, partie complète, résultats, retour au menu), un test par mode Training, un Tournoi, et les écrans de partie (tutoriel, timer, annonces, pause, résultats, écran de chargement) avec leurs captures. À utiliser pour vérifier un changement de gameplay, de menu, de flow ou de chargement « en jeu », avant un commit ou un build, quand l'utilisateur dit « teste le jeu », « vérifie que les modes marchent », « lance les tests », quand un scénario échoue, après l'ajout d'un mode de jeu (il faut alors ajouter son cas de test), ou après un changement d'un écran de partie.
---

# Tests de scénario (4 manettes virtuelles)

But : vérifier soi-même, en jeu, qu'un changement ne casse rien, du menu jusqu'au retour au menu.
Validé sous Unity 6000.6.7f1 : la suite complète (12 tests) dure ~14 minutes (839 s le 2026-10-28).

Fichiers (dans `Assets/Tests/PlayMode/`, assembly `BeepBoop.Tests.PlayMode`) :

| Fichier | Rôle |
| --- | --- |
| `GameModesScenarioTests.cs` | Classe partielle, scénarios des menus et des modes : `MainMenu_EveryScreenWorksWithGamepadKeyboardAndMouse`, `Training_PlaysAGameThenReturnsToMenu(mode)` × 7, `Tournament_PlaysUntilAChampionThenReturnsToMenu` (capture `30-tournament-results` après la première partie), et les étapes partagées (`JoinFourPlayersAndOpenHub`, `Join`, `GetReady`, `PlayOneGame`) |
| `GameScreensScenarioTests.cs` | Même classe : les écrans de partie. `GameScreens_WorkWithGamepadsAndKeyboard`, `PauseMenu_BackToMenuLeavesTheGame("Training" / "Tournament")`, et les aides `StartTraining(mode)`, `CaptureGameScreen(nom)`, `SubmitAndSeeTheLoadingScreen()`, `SelectSlider(slider)`, `IsPaused` |
| `ScenarioDriver.cs` | Manettes, clavier et souris virtuels, navigation dans les menus UI Toolkit, captures d'écran, attente avec délai, lecture de l'état du jeu par réflexion, collecte des erreurs |

`MainMenu_EveryScreenWorksWithGamepadKeyboardAndMouse` (~13 s, à lancer après toute modification des menus) :
clavier et souris sur l'accueil (si aucun joueur n'a encore rejoint), puis à la manette Crédits et
Annuler, volume au d-pad, sélection des personnages (rejoindre, se déclarer prêt, un personnage déjà
choisi refusé, Annuler qui retire d'abord le « prêt », départ vers le hub quand tous sont prêts),
défilement du choix du niveau ; il enregistre une capture de chaque écran dans `Logs/screenshots/`
(`04` à `10` : chaque configuration de la sélection des personnages) : les regarder.

`GameScreens_WorkWithGamepadsAndKeyboard` (~50 s) joue une partie de Points Rush : cartes du tutoriel
pendant que les joueurs se déclarent prêts, timer, pause ouverte avec P puis pilotée par la manette
du joueur principal (un volume au d-pad, B pour reprendre), chaque annonce, bouton des résultats,
écran de chargement ; captures `20` à `29`. `PauseMenu_BackToMenuLeavesTheGame` quitte une partie
par « Menu principal » de la pause, en Training et en tournoi (abandonné : ni résultats, ni partie
suivante).

Chaque scénario de partie : Home → Play → les 4 manettes rejoignent (A/Croix) puis se déclarent prêtes
(Y/Triangle, Aléatoire avec X si le personnage est déjà pris) → Hub → Training (ou Tournoi) → map choisie à la croix → les 4 joueurs font Ready → appuis aléatoires sur les 6 boutons
d'action jusqu'aux résultats → Submit → retour au Home. Le jeu tourne en ×4 (`k_TimeScale`).
Un scénario échoue sur un délai dépassé **ou sur toute erreur/exception loguée**.

## 1. Lancer

L'éditeur doit être fermé (sinon code 3 : voir le skill `unity-headless`).

```bash
CLI=".claude/skills/unity-headless/scripts/unity_cli.py"
python $CLI screenshots                                 # les menus + liste des captures (~2 min avec le démarrage)
python $CLI test --filter 'Training.*"Shooters"'      # un mode (~1,5 min)
python $CLI test --filter Tournament                    # le tournoi seul (3 à 9 parties, 3-7 min)
python $CLI test --filter GameScreens                   # écrans de partie + captures 20-29 (~2 min)
python $CLI test --filter PauseMenu                     # quitter une partie depuis la pause (~1,5 min)
python $CLI test --filter Game.Tests.PlayMode --detach  # toute la suite (~14 min) : en détaché
```

`--filter` est le `-testFilter` de Unity : un nom complet ou une regex sur le nom complet
(`Game.Tests.PlayMode.GameModesScenarioTests.Training_PlaysAGameThenReturnsToMenu("Shooters")`).
Les guillemets autour du mode évitent que `Shooters` attrape aussi `Shooters in Hell`.

La suite complète dépasse la limite de 10 minutes des outils : `--detach`, puis un **Monitor** qui
attend la fin du PID affiché, puis la commande `results ...` que `--detach` a affichée.

## 2. Lire le résultat

`test` (ou `results <xml> --log <log>`) affiche une ligne par test, puis pour chaque échec :

- l'étape en cours (`[Hub: open Training]`, `[<niveau>: playing]`...) ;
- pour un délai dépassé, l'état du jeu à ce moment : scène, état du menu, état de la partie,
  niveau, nombre de joueurs, élément UI sélectionné ;
- les erreurs loguées, chacune préfixée de l'étape où elle est apparue, avec sa ligne de code ;
- la liste des parties jouées et leurs scores (les scores non nuls prouvent que les inputs ont agi).

## 3. Déboguer un échec

| Échec | Piste |
| --- | --- |
| Erreurs loguées pendant `playing` | Vrai bug de jeu : skill `unity-error-triage` sur le log du run |
| Délai dépassé sur un écran de menu | Le menu a changé : nom d'un champ lu par réflexion (`m_trainingButton`, `m_widgets` de la sélection des personnages...), état renommé, navigation UI modifiée. Relire l'écran concerné |
| `No UI navigation path from X to Y` | La croix n'amène pas le focus de X à Y : navigation directionnelle de `AUIToolkitScreen` (élément le plus proche dans la direction), élément non focalisable ou désactivé, ou l'EventSystem ne sélectionne pas le panel |
| Délai dépassé dans la sélection des personnages | On rejoint seulement avec Y/A/Espace/Entrée (action créée par l'écran) ; les boutons d'un joueur ne comptent qu'une fois tous relâchés après qu'il a rejoint ; la croix d'une manette PS virtuelle ne bouge pas le curseur (manettes 1 et 3 pour la grille) ; le hub s'ouvre 1,2 s après le dernier « prêt » |
| Délai dépassé sur `the game to load` | Chargement Addressables bloqué : `log-errors` sur le log, souvent une erreur au `LoadState` |
| Échec qui disparaît au run suivant | Bug dépendant du hasard (positions, météorites) : relancer le même filtre 2-3 fois avant de conclure, et le signaler comme bug intermittent |
| Échecs en cascade | Un scénario cassé en pleine partie pollue le suivant ; `GoToMainMenuHome` décharge le niveau pour l'éviter. Relancer le test suivant seul pour confirmer |

`log-flow <log>` montre l'enchaînement des étapes `[Scenario]`, des scènes et des états.

## 4. Étendre

- **Nouveau mode Training** : ajouter son nom à `TrainingModes`, exactement la partie du milieu
  du nom de l'asset `<Arène> - <Mode> - LevelDataAsset`. Le skill `game-mode-variants` le fait
  quand il crée un mode.
- **Nouvel écran, nouvelle commande, nouvel état à lire** : lire `references/driver.md` (API du
  driver, règles de réflexion, pièges des manettes virtuelles).
- `k_KnownErrors` (tests) : seulement pour une erreur connue, documentée en commentaire, que
  l'utilisateur accepte de laisser pour l'instant. Toute entrée masque un vrai bug : la retirer
  dès qu'il est corrigé.

## Pièges connus

- Le Test Framework remet `LogAssert.ignoreFailingMessages` à `false` entre le set-up et le corps
  du test : il est donc mis dans le corps de chaque test. Sans lui, Unity fait échouer le test
  à la première erreur, avant le rapport complet.
- Les tests ne peuvent pas référencer le code du jeu (`Assembly-CSharp` n'est pas référençable par
  un `.asmdef`) : tout passe par les noms de types et la réflexion du driver.
- Le jeu n'écoute le Submit de l'écran de résultats qu'après les gros plans sur les personnages :
  le scénario appuie toutes les 0,5 s tant que l'état est `ResultsState`.
- Les scores et l'issue des parties varient d'un run à l'autre (hasard du jeu non fixé), même si
  les appuis aléatoires du test sont reproductibles (graine fixe par mode).
- **`WaitForEndOfFrame` ne revient jamais en batchmode** : le test reste bloqué jusqu'à son
  `Timeout`. Attendre avec `yield return null`.
- Juste après `StartRenderingToTextures()`, un `Submit` immédiat s'est perdu : laisser passer une
  seconde (`RealSeconds(1f)`) avant le premier appui.
- **Un état fugace se guette dès l'action**, pas après : un `Submit()` dure 4 frames, pendant
  lesquelles le menu peut se charger et cacher l'écran de chargement. `SubmitAndSeeTheLoadingScreen`
  regarde à chaque frame dès l'appui ; faire pareil pour tout ce qui ne dure que quelques frames.
- **La pause remet `Time.timeScale` à 1** en reprenant : le remettre à `k_TimeScale` après. Une
  annonce dure 1 à 2 s de temps de jeu : passer à 1 le temps de sa capture.
- **Captures hors du menu** : `CaptureGameScreen` recommence le rendu sur la caméra de la scène
  courante (une scène sans caméra principale, résultats du tournoi : le panel seul). Tous les
  écrans partagent `UIPanelSettings` : le panel capturé est le même partout.
- **Clavier pendant une partie** : `AddKeyboardAndMouse()` (P ouvre la pause) puis
  `RemoveKeyboardAndMouse()` ; ajouter le clavier ne fait pas rejoindre de joueur hors de la
  sélection des personnages.
- **Curseurs** : gauche/droite changent la valeur au lieu de déplacer le focus ; atteindre un
  curseur à la croix avec `SelectSlider` (droite depuis les boutons, puis haut/bas).
- `GameplayState` est le **nom du type** de l'état : `AnnouncementState` (nouvelle configuration
  de touches), `RoundAnnouncementState`, `FinishState`, `ResultsState`, `EndState`...
- Un run interrompu (blocage, Unity tué) laisse `Assets/InitTestScene<guid>.unity` et son `.meta` :
  les supprimer. Un fichier ajouté sous `Assets/` pendant un run produit l'avertissement « Files
  generated by test without cleanup » : ne pas toucher au projet pendant un run.
