---
name: online-multiplayer
description: Jeu en ligne de Beep Boop dans l'espace — salles avec un code (Multiplayer Services + Relay, ou connexion directe sur le réseau local), écrans « En ligne » et salle, synchronisation des parties (l'hôte joue, les invités rejouent ce qu'il leur dit), tournoi en ligne, départs et pertes de connexion, et tests à deux machines (éditeur hôte + build invité). À utiliser dès qu'on touche au réseau, au menu en ligne, à un système de gameplay qui décide quelque chose (mort, hasard, score, état suivant) — il doit alors être répliqué —, quand une partie en ligne se désynchronise, quand l'utilisateur parle de « en ligne », « online », « salle », « code », « Relay », « multijoueur à distance », ou veut tester en ligne.
---

# Jeu en ligne

But : jouer à plusieurs machines (4 joueurs au plus en tout, plusieurs par machine possible), dans une
salle privée qu'on rejoint avec un code. Pas de matchmaking public pour l'instant.

## Parcours dans le menu

Accueil → sélection des personnages (les joueurs **locaux** choisissent leur robot ; un joueur seul
peut continuer, le hub s'ouvre 3 s après son « prêt ») → Hub : Tournoi et Entraînement (2 joueurs
locaux au moins, sinon grisés) ou **En ligne** → écran En ligne : Créer une salle, ou Rejoindre avec
le code (6 cases : croix haut/bas, clavier, Ctrl+V) → **Salle** : code, 4 places, l'hôte lance
Tournoi ou Entraînement (même choix du niveau qu'en local), les invités attendent. Après chaque partie,
tout le monde revient dans la salle. Retour dans la salle = la quitter (confirmation) ; l'hôte qui part
la ferme. Salle perdue (hôte parti, connexion) : retour au hub, qui dit pourquoi.

## Architecture

| Quoi | Où | À savoir |
| --- | --- | --- |
| `OnlineConnection` | `Online/` | `NetworkManager` de Netcode for GameObjects créé au premier usage (racine, sans network objects ni gestion des scènes) ; messages nommés `BeepBoop` (octet de type + contenu `BinaryWriter`). Session Multiplayer Services avec Relay (`CreateSessionAsync` / `JoinSessionByCodeAsync`), ou `OnlineConnection.DirectAddress` (`-online-direct [ip:port]`, défaut 127.0.0.1:7777) sans services Unity. Un profil d'authentification par copie du jeu sur le PC (mutex) |
| `OnlineManager` | `Online/`, sur le prefab `GlobalContext` | La salle : `Roster` (`RoomPlayer` : machine, rang local, robot), ordre = joueurs de l'hôte puis de chaque invité. L'hôte approuve (version, places, pas de partie en cours), change un robot déjà pris, lance (`LaunchTraining`, `LaunchTournament`, `SendTournamentNextGame`). `IsGuest` reste vrai dans une partie dont la salle est perdue, jusqu'au menu |
| Joueurs distants | `AbstractPlayer` sans `PlayerInput` (`IsRemote`), enfants de l'`OnlineManager` | `PlayerManager.GamePlayers` = joueurs de la salle (locaux + distants) dans son ordre ; `Players` reste les joueurs de la machine (menus, navigation). Badge `Number` (J1-J4) = rang dans la salle |
| `Netplay` | `Online/Netplay.cs` | `HasAuthority` : hors ligne ou hôte. **Tout code de gameplay qui décide** (mort, case au hasard, score, état suivant, apparition) ne tourne que là |
| `GameplaySync` | `Online/Gameplay/`, ajouté au `GameplayContext` | Hôte : `Notify*` appelés par le gameplay **avant** la mutation (ce qu'elle déclenche vient après), `FlowState` à chaque état (sauf `LoadState`), chrono toutes les secondes, boutons des invités → `CharacterPlayerController.PerformRemoteAction`. Invité : `FlowMachine.IsRemotelyDriven` (ses demandes d'état sont ignorées), messages bufferisés jusqu'au chargement puis rejoués (`Replicate*`), « chargé » envoyé à l'hôte (l'hôte attend tout le monde : exigence de `LoadEventsHooker`) |
| Noms communs | | Robot = rang dans la partie ; case = coordonnées `GridBuilder.TryGetCoordinates` ; état = chemin sous la `FlowMachine` ; niveau = rang dans `Training - LevelListDataAsset` ou dans la config du tournoi ; robot du joueur = rang dans `CharacterBankManager` |
| Départ en cours de partie | `CharacterPawn.Leave` | Robot mort pour de bon (`HasLeft`), prêt au tutoriel, ignoré par la mise en place des manches ; aussi aux parties suivantes d'un tournoi |
| Pause | `PauseMenuManager` | En ligne elle n'arrête pas le jeu : le menu prend les boutons des joueurs de la machine. « Menu principal » quitte la salle |

## Ajouter ou modifier un système de gameplay

1. Il **décide** quelque chose ? Le garder derrière `Netplay.HasAuthority` et ajouter un message :
   valeur dans `EOnlineMessage`, `GameplaySync.NotifyXxx` (hôte, avant la mutation) et un `case`
   dans `Apply` qui appelle une méthode `ReplicateXxx` (invité, sans vérification ni hasard).
2. Il ne fait que **montrer** (son, animation, VFX, UI) en réaction à un état répliqué : rien à faire,
   il tourne aussi chez l'invité.
3. Un événement que les deux machines déclenchent (`OnDeath`, `OnPositionUpdated`...) et qu'un système
   écoute pour décider : la garde va dans l'écouteur.
4. Ajouter le cas au test en ligne si c'est un nouveau mode (le test joue Points Rush).

## Tester

```bash
CLI=".claude/skills/unity-headless/scripts/unity_cli.py"
python $CLI test --filter OnlineMenu                         # écrans, une machine (~1 min)
python $CLI build --output Builds/OnlineGuest                # le build qui joue l'invité (30 s à 2 min 30)
python $CLI test --filter "Online_Plays"                 # entraînement (~1 min) et tournoi (3-9 parties, 3-8 min)
python $CLI test --filter "OnlineMenu_Leaves|OnlineDrops" --detach   # départs, coupures, retours (9 tests, ~8 min)
```

- `OnlineMenu_OnePlayerOpensARoom` : joueur seul → hub (modes locaux grisés) → En ligne (code à la
  croix et au clavier) → salle seule → la quitter. Captures `16` à `18`.
- `Online_PlaysATrainingWithAGuestBuild` : l'éditeur ouvre une salle en direct sur 127.0.0.1:7792,
  lance `Builds/OnlineGuest/Beep Boop dans l'espace.exe -batchmode -nographics -no-analytics
  -online-direct 127.0.0.1:7792 -online-test-guest`, joue Points Rush avec lui, revient dans la salle,
  la ferme ; le build doit quitter avec le code 0. L'invité compte les déplacements des deux robots et
  logue les scores : le test vérifie que ses boutons ont fait bouger son robot chez l'hôte et que ses
  scores sont ceux de l'hôte. `Online_PlaysATournamentWithAGuestBuild` : pareil, un tournoi entier
  (l'hôte tire les niveaux et quitte les résultats pour tout le monde). Ignorés sans build. **Le build
  doit contenir le code testé** : le refaire après tout changement de gameplay ou de réseau (un build
  périmé fait échouer le test pour une raison déjà corrigée). Log de l'invité : `Logs/online-guest.log`.
- `OnlineGuestBot` (`Online/Testing/`) : ne s'active qu'avec `-online-test-guest`. Codes de sortie :
  0 ok, 1 erreur loguée, aucune partie (ou salle non perdue avec `expect-room-lost`), scène de jeu
  restée dans le menu, 2 trop long, 3 n'a pas pu rejoindre. `-online-test-guest-scenario a,b` ajoute
  ce qui tourne mal de son côté : `rejoin` (quitte la salle 6 s après y être entré, revient 3 s plus
  tard), `freeze-in-game:N` (gel de N s, 3 s après le début de la première partie), `quit-when-dead`
  (quitte la partie par la pause dès que son robot est mort, puis redemande la salle chaque seconde,
  refusé tant que la partie dure), `quit-while-loading` (pareil, dès que la partie commence à se
  charger), `stuck-loading` (charge sans jamais le dire à l'hôte), `expect-room-lost` (la salle doit
  être perdue ; aucune partie n'est exigée). Il logue `room lost: <raison>`, `joining refused: <raison>`,
  `freezing N s` / `unfrozen`, et écrit ces lignes `[GuestBot]` au fil de l'eau dans
  `-online-test-guest-report <fichier>` (`Logs/online-guest-report.txt`) : **le log du joueur n'arrive
  sur le disque que par blocs**, un test qui y cherche une ligne récente la rate. Après chaque test,
  le log et le rapport sont gardés en `Logs/online-guest-<test>.log` / `-report.txt`.
- **Départs, coupures, retours** : la liste des cas et leur test est dans
  [references/disconnections.md](references/disconnections.md) (à lire avant de toucher aux
  départs, au chargement ou à la fin d'une partie en ligne). Tests `OnlineDrops_*`
  (`OnlineDropsScenarioTests.cs`) et `OnlineMenu_LeavesAndReopensRightAway` (une machine : recréer
  une salle juste après l'avoir quittée, message de lancement arrivé après le départ). Une coupure se
  simule en gelant le fil principal (`Thread.Sleep`) d'une machine : côté invité par son scénario,
  côté hôte par le test (`FreezeHost`) ; au-delà du délai du transport
  (`OnlineConnection.k_DisconnectTimeoutMs`, 10 s) c'est une perte de connexion, en deçà un pic de lag.
- `-no-analytics` : les copies lancées par les tests n'envoient rien à GameAnalytics.
- À la main, deux copies du jeu buildé sur un PC : `-online-direct` sur les deux (127.0.0.1), ou
  l'adresse IP de l'hôte sur le réseau local (Windows demande alors d'ouvrir le pare-feu à l'hôte).

## Boucle de travail

Une itération coûte ~4 min : `quick-compile` (5 s) → `build --output Builds/OnlineGuest` → `test
--filter "OnlineMenu|Online_Plays"` (l'éditeur doit être fermé pour les deux). Pour diagnostiquer :

| Où | Quoi |
| --- | --- |
| `Logs/claude-runs/<run>/editor.log` (l'hôte) | `[Online]` (salle, machines, parties), `[FM] FlowMachine - <état> - ENTER`, `[Scenario]` |
| `Logs/online-guest.log` (l'invité) | `[GuestBot]` (prêt, résultats avec déplacements et scores, code de sortie), `[FM] FlowMachine - <état> - About to switch state remotely`, `[Tournament]`, exceptions |
| Les deux | Un invité bloqué : comparer la dernière ligne `[FM]` des deux logs. Une exception chez l'invité dans `HandleNamedMessage` vient du traitement d'un message de salle (pile complète dans son log) |

Ajouter un `Debug.Log("[Tournament] ...")` ou `[Online]` aux étapes douteuses plutôt que de deviner :
c'est ainsi qu'a été trouvé le niveau détruit du tournoi (voir Pièges). Les erreurs de l'invité
comptent : un seul `LogError` le fait sortir avec le code 1.

## Services Unity (codes et Relay)

Sans `-online-direct`, créer ou rejoindre une salle passe par Unity Gaming Services : le projet doit
être lié à un projet Unity Cloud (Edit > Project Settings > Services, `cloudProjectId` et
`organizationId` dans `ProjectSettings.asset` ; seul l'utilisateur peut le faire, avec son compte,
fait le 04/11/2026 au soir). Sans lien, `UnityServices.InitializeAsync` échoue (`UnityProjectNotLinkedException`,
interne au package) : `OnlineConnection.SignIn` le teste avant (`Application.cloudProjectId`) et
l'écran En ligne dit « le projet n'est pas lié à Unity Cloud ». Une fois lié : commiter
`ProjectSettings.asset`, vérifier sur le tableau de bord (cloud.unity.com) que Relay et Lobby sont
actifs pour le projet (un refus à la création de la salle le signale), refaire les builds. Le détail
des échecs des services est dans le log (`[Online] ...` en avertissement, éditeur ouvert :
`Logs/Editor.log`). Le format des codes (6 caractères A-Z 0-9, `RoomCodeField.k_Length`) est à
confirmer sur une vraie session : le code affiché par la première salle créée le dit.

Les tests n'utilisent jamais les services (connexion directe) : une salle par Relay se vérifie à la
main, deux copies du jeu sur le même PC (chaque copie se connecte avec son propre profil anonyme,
`instance0`, `instance1`...) ou deux PC.

## Pièges

- Un message de l'hôte envoyé **après** la mutation qu'il décrit arrive après ses conséquences (un
  téléporteur déclenché par le déplacement) : l'invité les rejouerait dans le mauvais ordre.
- `NetworkManager.Shutdown` est interdit avec une session : `ISession.LeaveAsync` (fait par `Stop`).
- Les rangs de la salle (`Roster`) ne sont pas ceux de la partie après un départ : la partie garde ses
  `GamePlayers` jusqu'au retour au menu (`IsPlayerOf`, `HasLeftRoom` passent par eux).
- `LoadState` n'est jamais envoyé : chaque machine y démarre et charge elle-même.
- **Les tests dans l'éditeur ne voient pas les bundles Addressables** (Asset Database) : un asset
  référencé depuis un objet persistant (DDOL) mais chargé avec une scène Addressables est détruit avec
  son bundle dans un build. Vu le 04/11/2026 grâce à l'invité buildé : config et niveaux du tournoi
  détruits avant la 2e partie (bug aussi du tournoi local en build) ; `TournamentContext` garde un
  handle sur sa config. Le build invité est le seul test qui joue dans les conditions d'un build.
- L'hôte qui quitte par la pause n'envoie pas `EndState` : il ferme la salle, les invités quittent
  de leur côté (`GameplaySync.HandleRoomLost`).
- **Approbation** : `ApproveGuest` rend `null` pour accepter. Un `?? "refus"` derrière son appel
  transformait chaque acceptation en refus (« La salle n'accepte personne », vu au premier test).
- La machine de l'hôte a l'id Netcode 0 (`k_HostMachineId`) ; un invité qui reçoit un message d'un
  autre id l'ignore.
- Le test lance l'hôte en ×4 (`k_TimeScale`), l'invité en temps réel : l'invité suit les états de
  l'hôte (ses propres minuteries de présentation sont coupées par les changements d'état), et ses
  animations peuvent être en retard sans que rien ne se désynchronise.
- Enregistrer un handler de message nommé deux fois (démarrage client + serveur de l'hôte) le remplace
  sans erreur : c'est voulu.
- **`NetworkManager.DisconnectReason`** rend la raison de l'hôte (refus, renvoi) **ou** une description
  technique fabriquée par Netcode (« [Disconnect Event][Client-1]… ») : ne jamais l'afficher telle
  quelle (`OnlineConnection.RefusalReason` la filtre).
- Une machine qui se réveille d'un gel traite d'abord les paquets reçus pendant le gel : elle ne voit
  la perte de connexion qu'un délai du transport (10 s) **après** son réveil. Les tests en tiennent
  compte (attendre la sortie de l'invité jusqu'à 60-90 s).
- `OnlineManager.GameNumber` compte les parties de **toutes** les salles de la session (l'invité logue
  le même numéro) : un test lit celui de l'hôte (`HostGameNumber`), jamais « partie 1 ».
- Une salle quittée se ferme en arrière-plan (0,2 s pour que `RoomClosed` parte, puis l'arrêt de la
  session) : `CreateRoom`/`JoinRoom` attendent `m_isClosing`. Un message arrivé pendant ce temps est
  ignoré (`Status == None`).
