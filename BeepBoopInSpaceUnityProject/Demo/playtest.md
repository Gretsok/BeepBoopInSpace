# Fiche de playtest de la démo

But : savoir si des gens qui ne connaissent pas le jeu le comprennent et s'amusent, avant de publier.
Compter 30 minutes par session, à 2 ou 4 joueurs, plus une session à une personne seule contre les robots.

## Avant de commencer

- Prendre le dernier build (`Builds/BeepBoop_Win_...`, avec `LISEZ-MOI.txt` à côté de l'exe).
- Ne rien expliquer du jeu. Noter ce qui bloque plutôt que d'aider.
- Prévoir de quoi noter, ou filmer l'écran avec l'accord des joueurs.

## Ce qu'il faut observer

| Moment | Question | Ce qui décide |
| --- | --- | --- |
| Écran du tutoriel | Ont-ils compris l'objectif et que les touches vont changer ? | Leur demander de l'expliquer après 1 partie |
| Première partie | Combien de temps avant de faire ce qu'ils veulent ? | Moins d'une minute : bon signe. Sinon, essayer les contrôles simplifiés |
| 6 boutons | Trop difficile, ou drôle ? | Comparer avec un mode en contrôles simplifiés (voir plus bas) |
| Mélange des touches | Rires ou agacement ? Les 10 secondes sont-elles bien dosées ? | Noter les réactions au bandeau « Nouvelle configuration » |
| Robots adversaires | Trop forts, trop faibles, crédibles ? | Le joueur seul doit gagner une partie sur trois à peu près |
| Tournoi | Trop long ? À quel moment l'attention baisse ? | Noter la durée réelle ; aujourd'hui 3 victoires pour gagner, de 3 à 9 parties de 2 minutes |
| Modes | Lesquels donnent envie de rejouer, lesquels ennuient ? | Faire classer les modes à la fin |
| Lisibilité | Voit-on son robot, l'eau, les dalles, les météorites ? | Noter chaque confusion |

## Questions à la fin

1. Quel moment était le plus drôle ? Le plus frustrant ?
2. Qu'avez-vous mis du temps à comprendre ?
3. Y rejoueriez-vous ? Avec qui ?
4. Quel mode enlèveriez-vous ?

## Réglages faciles à changer ensuite

| Réglage | Où |
| --- | --- |
| Force des robots (vitesse, erreurs, esquive, tir) | Constantes en tête de `Assets/Game/Gameplay/CharactersManagement/Bots/BotController.cs` |
| Victoires pour gagner un tournoi | `ScoreToReach` dans `Assets/Game/Tournament/Config/TournamentConfigDataAsset.asset` |
| Durée d'une partie | `m_gameDuration` dans `Assets/Game/Gameplay/Timer/TimerManager.prefab` |
| Contrôles d'un mode | `ControlMode` du `LevelDataAsset` du mode (skill `game-mode-variants`) |

## À tester en plus, sans joueurs extérieurs

- Une partie en ligne entre deux PC sur deux réseaux différents, avec le build final.
- Une partie complète sur un PC modeste : noter si le jeu saccade et à quels moments.
- Une manette Switch ou générique, si vous en avez une : les tests n'utilisent que des manettes virtuelles.
