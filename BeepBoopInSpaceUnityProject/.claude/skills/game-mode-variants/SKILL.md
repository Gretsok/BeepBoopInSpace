---
name: game-mode-variants
description: Créer, modifier et enregistrer les modes de jeu de Beep Boop dans l'espace — un mode est un LevelDataAsset qui combine un objectif (Last Man Standing, Least Deaths, Most Kills, Points Rush), une action spéciale (Bomb, Sniper Rifle), des modificateurs (Meteorites) et une arène ; il doit être inscrit dans la liste Training, la config du Tournoi et les tests de scénario. À utiliser quand l'utilisateur veut un nouveau mode ou une variante (« un mode tir avec des bombes », « ajoute les météorites à X »), changer l'arme ou l'objectif d'un mode, retirer un mode du tournoi, renommer un mode, ou ajouter un nouveau type d'objectif ou de modificateur.
---

# Modes de jeu (LevelDataAsset)

But : ajouter ou modifier un mode sans ouvrir l'éditeur, l'inscrire partout où il doit l'être, et
le vérifier en jeu.

## Modèle

Un mode = un `LevelDataAsset` (`Assets/Game/Gameplay/Levels/Arenas/<Arène>/<Arène> - <Mode> - LevelDataAsset.asset`) :

| Champ | Contenu |
| --- | --- |
| `NameKey` | Nom affiché tel quel dans le jeu (pas de localisation : texte français direct) |
| `Thumbnail` | Vignette de la sélection de niveau |
| `GameModeScene` / `EnvironmentScene` / `LevelScene` | `GM_BaseGame` / `Swamp_Enviro` / `TestArena_Level` pour toutes les arènes actuelles |
| `ObjectiveManagerPrefab` | Comment on gagne : `LastManStanding`, `LeastDeaths`, `MostKills`, `PointsRush` |
| `SpecialActionPrefab` | L'action spéciale : `Bomb`, `SniperRifle` |
| `AdditionalSystemsToInstantiate` | Modificateurs (`AGameplayModifier`) : `Meteorites` |
| `GlobalGameplayDataAsset` | Réglages de respawn (case valide / invalide : au plus proche, aléatoire) |

Il apparaît dans le jeu s'il est inscrit dans `Assets/Game/Training/Training - LevelListDataAsset.asset`
(écran de sélection Training) et/ou `Assets/Game/Tournament/Config/TournamentConfigDataAsset.asset`
(maps tirées au sort par le Tournoi, `ScoreToReach` = victoires pour gagner). Le `LevelDataAsset`
lui-même n'est pas Addressable ; les prefabs et scènes qu'il référence le sont déjà.

## 1. Voir l'existant

```bash
python .claude/skills/game-mode-variants/scripts/level_variant.py list
```

Liste les briques disponibles et chaque mode : composition, nom affiché, où il est inscrit
(`training`, `tournament`, `tests`).

## 2. Créer une variante

Éditeur Unity fermé (sinon il ne voit pas les fichiers modifiés et peut ré-écrire les listes) :

```bash
python .claude/skills/game-mode-variants/scripts/level_variant.py create \
  --from "Shooters" --mode "Bomb Shooters" --name "Tir à la bombe" \
  --special Bomb --modifiers none --training --tournament
```

- `--from` : mode copié (tous les champs non précisés viennent de lui, vignette comprise).
- `--mode` : nom dans le nom de fichier, en anglais comme les autres ; `--name` : nom affiché.
- `--objective`, `--special`, `--modifiers` (`a,b` ou `none`) : noms courts donnés par `list`.
- `--training` inscrit aussi le mode dans `TrainingModes` des tests (sauf `--no-tests`).

Le script écrit le YAML comme Unity (fins de ligne LF, accents en `\xE9`), avec un nouveau GUID.
`register <mode> --training/--tournament` inscrit un mode existant.

**Toutes les combinaisons ne marchent pas.** Un objectif peut dépendre d'une action spéciale :
`MostKills` compte les éliminations via le `SniperBulletShooter` du fusil de sniper, et avec la
bombe la partie ne se charge pas (`NullReferenceException` dans `MostKillsObjectiveManager.SetUp`).
Le script refuse les combinaisons connues pour être incompatibles (`REQUIRED_SPECIAL`) ; pour les
autres, seul le test en jeu (étape 3) le dit. Un nouvel incompatible découvert : l'ajouter à
`REQUIRED_SPECIAL` avec la raison en commentaire.

Retirer un mode d'une liste, ou modifier un champ d'un mode existant : éditer le `.asset`
directement (une ligne `- {fileID: 11400000, guid: <guid>, type: 2}` par mode dans les listes ;
le GUID d'un fichier est dans son `.meta`). Supprimer un mode = supprimer `.asset` + `.meta`, après
l'avoir retiré des deux listes et de `TrainingModes`.

## 3. Vérifier

1. `python .claude/skills/game-mode-variants/scripts/level_variant.py list` : composition attendue.
2. `python .claude/skills/unity-headless/scripts/unity_cli.py quick-compile` (le test a changé).
3. Jouer le mode : `python .claude/skills/unity-headless/scripts/unity_cli.py test --filter 'Training.*"<Mode>"'`
   (skill `unity-scenario-tests`) : il doit être sélectionnable, se charger, se jouer et revenir au
   menu sans erreur.
4. Dire à l'utilisateur ce qui reste à faire dans l'éditeur : la vignette est celle du mode copié.

## 4. Nouveau type d'objectif ou de modificateur (code)

Plus lourd : nouveau script, nouveau prefab, et une entrée Addressables (à créer dans l'éditeur,
groupes `Game-Gameplay-GameModes-Objective-<Nom>` / `-Modifiers-<Nom>`). S'inspirer de
`LastManStandingObjectiveManager` (objectif) et `MeteoritesModifier` (modificateur), et respecter
ce que les bugs passés ont appris :

- n'agir que pendant le jeu : suivre `GameplayEventsHooker.OnGameplayResumed/Paused` (exposé en
  `IsTicking`) et ignorer morts et événements en dehors de `GameplayState` ;
- ne demander la fin de partie (`FinishStateGrabber.Instance.FinishState`) qu'une fois, et pas si
  le chrono l'a déjà fait ;
- déclarer le calcul du score dans `Initialize` :
  `ResultsManager.RegisterPostInitializationCallback(m => m.SetScoreCalculationMethod(...))`.

Le prefab et son entrée Addressables se créent dans l'éditeur : préparer le code, puis donner à
l'utilisateur les étapes précises (créer le prefab, l'ajouter au groupe), et vérifier ensuite
avec un mode qui l'utilise.
