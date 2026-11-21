# Déconnexions et reconnexions : les cas limites

Liste de référence des cas de départ, de coupure et de retour d'une machine dans une salle en ligne,
d'abord ceux de tout jeu en réseau, puis ceux propres à Beep Boop. Pour chacun : ce qui doit se
passer, ce qu'on a trouvé en novembre 2026 (OK, ou le bug corrigé), et le test qui le provoque.

Tests : `OnlineMenu_LeavesAndReopensRightAway` (une machine) et `OnlineDrops_*` (l'éditeur héberge,
le build `Builds/OnlineGuest` joue l'invité avec `-online-test-guest-scenario <scénario>`). Une
coupure se simule en **gelant le fil principal** d'une machine (`Thread.Sleep`) : plus de battements
de cœur ni d'accusés de réception, exactement comme un câble débranché, une machine figée ou un
processus tué ; un gel plus court que le délai du transport est une micro-coupure.

Vérifié le 04/11/2026 : avant les correctifs, 8 des 9 tests échouaient chacun sur son bug (seul
`HostLeavesDuringAGame` passait) ; après, 9/9, et la suite complète 27/27.

Délai de détection d'une machine muette : `OnlineConnection.k_DisconnectTimeoutMs` (10 s ; 30 s par
défaut dans Unity Transport, trop long pour une partie de 2 min).

## Cas communs à tout jeu en réseau

### A. Connexion

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| A1 | Code faux, salle inexistante | Message « Aucune salle n'a ce code » | OK (`DescribeJoinError`) | à la main (services) |
| A2 | Hôte injoignable ou muet | Échec en 15 s (connexion) + 10 s (liste des joueurs), message | OK | — |
| A3 | Annuler pendant « Connexion… » | Retour à l'écran En ligne, rien ne reste ouvert | OK (`CancelConnecting`) | — |
| A4 | Refus : salle pleine, autre version, partie en cours | La raison de l'hôte s'affiche | OK | « partie en cours » : `GuestQuitsAGameWhileItsRobotIsDead` |
| A5 | Connexion perdue entre l'approbation et l'arrivée | La place réservée est libérée | OK (Netcode signale la déconnexion d'un client en attente) | — |
| A6 | Créer ou rejoindre juste après avoir quitté | La nouvelle salle s'ouvre | **Bug** : la fermeture (asynchrone, 0,2 s chez l'hôte) n'était pas attendue ; `StartHost` échouait ou l'ancien `Stop` coupait la nouvelle salle. Corrigé : `CreateRoom`/`JoinRoom` attendent la fermeture | `LeavesAndReopensRightAway` |
| A7 | Double appui sur Créer / Rejoindre | Une seule tentative | OK (`m_isConnecting`, `Status`) | — |

### B. Départ volontaire

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| B1 | Invité quitte le salon | Ses joueurs disparaissent, badges renumérotés, lancement grisé sous 2 joueurs | OK | `GuestLeavesTheRoomAndComesBack` |
| B2 | Hôte ferme le salon | Invités à l'écran En ligne : « L'hôte a fermé la salle. » | OK | tests `Online_Plays*` |
| B3 | Invité quitte une partie (pause → Menu principal) | Son robot meurt pour de bon, la partie continue | OK | `GuestQuitsAGameWhileItsRobotIsDead` |
| B4 | Hôte quitte une partie | Invités à l'écran En ligne avec la raison | OK | `HostLeavesDuringAGame` |
| B5 | Fenêtre fermée (Alt+F4) | Comme B1/B4 : Netcode envoie la déconnexion à l'arrêt | **Bug** trouvé ensuite (transitions.md, E1) : chez l'invité, l'arrêt de l'hôte affichait « Disconnected due to host shutting down. ». Corrigé | `OnlineTransitions_HostLosesTheRoom*` |
| B6 | Quitter au moment où l'hôte lance | Rien ne se lance chez celui qui part | **Bug** : un `LaunchTraining` arrivé après `LeaveRoom` (la session met du temps à se fermer) lançait une partie hors ligne. Corrigé : rien n'est traité hors salle | `LeavesAndReopensRightAway` (message forgé) |

### C. Coupure brutale (crash, réseau, machine figée)

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| C1 | Détection d'une machine muette | Quelques secondes | **Trop long** : 30 s de robots figés. Passé à 10 s | `GuestCutOffDuringAGame`, `HostCutOffDuringAGame` |
| C2 | Invité coupé pendant la partie | Chez l'hôte : robot retiré, la partie continue. Chez l'invité : retour à l'écran En ligne avec la raison | OK après C1 | `GuestCutOffDuringAGame` |
| C3 | Hôte coupé pendant la partie | Invités à l'écran En ligne (« connexion perdue ») ; l'hôte, revenu, finit seul | OK après C1 | `HostCutOffDuringAGame` |
| C4 | Micro-coupure, pic de lag (< délai) | Rien de perdu : les messages en retard se rejouent dans l'ordre, mêmes scores | OK (fiable et séquencé) | `GuestLeavesTheRoomAndComesBack` (gels de 3 s des deux côtés) |
| C5 | Coupure pendant le chargement | L'hôte n'attend pas l'invité parti ; l'invité revient au menu proprement | **Bug** : quitter pendant `LoadState` relâchait des handles Addressables vides (exception) et les scènes en cours de chargement arrivaient dans le menu. Corrigé : `LoadingManager.Unload` arrête le chargement, attend les opérations en cours, ne relâche que les handles valides ; le menu se charge après | `GuestCutOffWhileLoading` |
| C6 | Machine connectée qui ne finit jamais de charger | L'hôte ne reste pas bloqué | **Bug** : l'hôte attendait à jamais au chargement (pas de pause possible). Corrigé : après `OnlineManager.GuestLoadingTimeout` (60 s), l'hôte renvoie l'invité avec la raison | `GuestNeverLoads` |
| C7 | Envoi vers une machine qui vient de partir | Pas d'erreur | OK (destinataires lus à l'envoi) | — |

### D. Reconnexion

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| D1 | Un invité parti revient au salon avec le code | Nouvelle machine, aucun reste de l'ancienne, robot gardé s'il est libre, ses boutons pilotent son robot | OK | `GuestLeavesTheRoomAndComesBack` |
| D2 | Revenir pendant une partie ou un tournoi | Refus « Une partie est en cours… », accepté de retour au salon | OK | `GuestQuitsAGameWhileItsRobotIsDead` |
| D3 | Reprendre sa place dans la partie en cours | **Non pris en charge (choix)** : un robot parti reste mort jusqu'à la fin ; l'état complet d'une partie n'est pas transmissible | — | — |
| D4 | Hôte perdu : un invité prend le relais | **Non pris en charge (choix)** : la salle ferme | — | — |
| D5 | Nouvelle salle après en avoir perdu une | État remis à zéro (invité, numéro de partie, messages en attente, joueurs distants) | OK | `GuestLeavesTheRoomAndComesBack`, `LeavesAndReopensRightAway` |

### E. Messages en vol

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| E1 | Messages de jeu reçus avant la fin du chargement | Gardés puis rejoués | OK | tous |
| E2 | Messages reçus après avoir quitté ou perdu la salle | Ignorés | **Bug** (B6). Corrigé | `LeavesAndReopensRightAway` |
| E3 | Lancement reçu, puis salle perdue avant que le menu soit prêt | Pas de partie | **Bug** : l'invité chargeait la partie seul puis la quittait. Corrigé : `InMenu` vérifie la salle | `LeavesAndReopensRightAway` |
| E4 | Deux causes à la suite (`RoomClosed` puis déconnexion) | La première raison seule | OK | — |
| E5 | L'hôte relance avant que l'invité soit revenu au menu | L'invité rejoint la partie | **Bug** : l'ouverture du menu (`HandleBackInMenu`) effaçait la partie annoncée, l'invité la jouait hors ligne, seul. Corrigé : la partie ne commence chez l'invité qu'une fois le menu ouvert (`LaunchInMenu`) | — (dépend de la vitesse des machines) |

### F. Écrans après coup

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| F1 | Raison de la perte | Une fois, à l'écran En ligne, en français | **Bug** : après une coupure, le hub affichait la description technique de Netcode (« [Disconnect Event][Client-1]… »). Corrigé : seule la raison donnée par l'hôte s'affiche, sinon « La connexion avec l'hôte a été perdue. » | `*CutOff*`, `HostLeaves*`, `GuestNeverLoads` |
| F2 | Salle perdue entre la connexion et l'ouverture du salon | Écran En ligne avec la raison | **Bug** mineur : salon vide sans code. Corrigé : le salon renvoie au hub | — (quelques images) |
| F3 | Pause ouverte quand la salle est perdue | Fermée, retour au menu | OK | — |
| F4 | Joueurs locaux après la perte | Gardés avec leurs robots | OK | `*CutOff*` (l'invité rejoue ensuite) |
| F5 | Jeu en arrière-plan | Continue en salle, rendu ensuite | OK | `OnlineMenu_OnePlayerOpensARoom` |

## Cas propres à Beep Boop

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| G1 | Une machine à plusieurs joueurs part | Tous ses robots partent | OK (code) | — (l'invité de test a 1 joueur) |
| G2 | Départ pendant le tutoriel | Ses joueurs comptent comme prêts | OK | `GuestCutOffWhileLoading`, `GuestNeverLoads` (robot parti dès le tutoriel) |
| G3 | Départ pendant que son robot attend de réapparaître | Il reste mort, hors de la grille | **Bug** : la résurrection programmée le ramenait, robot fantôme immobile (vivant pour Last Man Standing, case occupée). Corrigé : `DeathController.KillForGood` annule la résurrection | `GuestQuitsAGameWhileItsRobotIsDead` |
| G4 | Départ en pleine manche (Last Man Standing) | Le robot meurt, reste hors grille aux manches suivantes | OK (`SetUpNewRoundState`) | — |
| G5 | Départ pendant la fin ou les résultats | Le salon s'ouvre sans lui | OK | — |
| G6 | Départ entre deux parties du tournoi | Robot mort dès le début des parties suivantes | OK (`RemoveLeftPlayers`) | `HostCutOffBetweenTournamentGames` |
| G7 | Il reste moins de 2 joueurs dans un tournoi | Le tournoi s'arrête | **Bug** : l'hôte jouait seul contre des robots fantômes jusqu'au score à atteindre (plusieurs parties). Corrigé : fin du tournoi après les résultats | `HostCutOffBetweenTournamentGames` |
| G8 | Hôte perdu pendant les résultats du tournoi | L'invité ferme le tournoi, écran En ligne avec la raison | OK | `HostCutOffBetweenTournamentGames` |
| G9 | Hôte perdu pendant que l'invité sort d'une partie du tournoi (état End) | Retour au hub | **Bug** : `GameplaySync` redemandait l'`EndState` (double déchargement) et les résultats attendaient l'hôte à jamais. Corrigé : pas de second départ depuis End (garde aussi dans `EndState`), les résultats ferment un tournoi dont la salle est perdue | — (quelques images) |
| G10 | Invité parti pendant que l'hôte choisit le niveau ou confirme | L'hôte revient au salon | **Bug** mineur : la confirmation ne lançait rien, sans rien dire. Corrigé : retour au salon, dialogue du tournoi fermé | `GuestLeavesTheRoomAndComesBack` |
| G11 | Invité qui arrive pendant le choix du niveau | Accepté, le lancement attend sa connexion | OK (`CanLaunch`) | — |
| G12 | Pause en ligne | Ne gèle pas la partie ; « Menu principal » quitte la salle | OK | `HostLeavesDuringAGame` |
| G13 | Joueur connecté mais absent au tutoriel | — | **Hors périmètre** : l'hôte attend, sans pause au tutoriel ; à décider (pause en ligne au tutoriel, ou délai) | — |
| G14 | Salle par Relay : Internet coupé chez l'hôte | Invités à l'écran En ligne ; l'hôte perd sa salle | Même chemin que C3 (`RemovedFromSession`, échec du transport) | à la main |
