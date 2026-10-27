---
name: unity-scenario-tests
description: Lancer, lire, déboguer et étendre les tests de scénario de Beep Boop dans l'espace — de vraies parties jouées en headless par 4 manettes virtuelles (menu, sélection des personnages, mode de jeu, partie complète, résultats, retour au menu), un test par mode Training plus un Tournoi. À utiliser pour vérifier un changement de gameplay, de menu, de flow ou de chargement « en jeu », avant un commit ou un build, quand l'utilisateur dit « teste le jeu », « vérifie que les modes marchent », « lance les tests », quand un scénario échoue, ou après l'ajout d'un mode de jeu (il faut alors ajouter son cas de test).
---

# Tests de scénario (4 manettes virtuelles)

But : vérifier soi-même, en jeu, qu'un changement ne casse rien, du menu jusqu'au retour au menu.
Validé sous Unity 6000.6.7f1 : la suite complète passe (8/8) en ~12 minutes.

Fichiers (dans `Assets/Tests/PlayMode/`, assembly `BeepBoop.Tests.PlayMode`) :

| Fichier | Rôle |
| --- | --- |
| `GameModesScenarioTests.cs` | Les scénarios : `Training_PlaysAGameThenReturnsToMenu(mode)` × 7, `Tournament_PlaysUntilAChampionThenReturnsToMenu` |
| `ScenarioDriver.cs` | Manettes virtuelles, navigation UI à la croix, attente avec délai, lecture de l'état du jeu par réflexion, collecte des erreurs |

Chaque scénario : Home → Play → les 4 manettes rejoignent (A/Croix) → Start → Hub → Training (ou
Tournoi) → map choisie à la croix → les 4 joueurs font Ready → appuis aléatoires sur les 6 boutons
d'action jusqu'aux résultats → Submit → retour au Home. Le jeu tourne en ×4 (`k_TimeScale`).
Un scénario échoue sur un délai dépassé **ou sur toute erreur/exception loguée**.

## 1. Lancer

L'éditeur doit être fermé (sinon code 3 : voir le skill `unity-headless`).

```bash
CLI=".claude/skills/unity-headless/scripts/unity_cli.py"
python $CLI test --filter 'Training.*"Shooters"'      # un mode (~1,5 min)
python $CLI test --filter Tournament                    # le tournoi seul (3 à 9 parties, 3-7 min)
python $CLI test --filter Game.Tests.PlayMode --detach  # toute la suite (~12 min) : en détaché
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
| Délai dépassé sur un écran de menu | Le menu a changé : nom d'un champ lu par réflexion (`m_trainingButton`, `m_startGameButton`...), état renommé, navigation UI modifiée. Relire l'écran concerné |
| `No UI navigation path from X to Y` | La navigation Unity (`Selectable` Up/Down/Left/Right) ne relie pas les deux boutons |
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
