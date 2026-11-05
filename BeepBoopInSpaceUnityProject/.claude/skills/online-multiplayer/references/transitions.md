# Transitions : les cas limites

Suite de [disconnections.md](disconnections.md) : une machine perdue, partie ou à la traîne (pic de
lag) **pendant une transition** : lancement, chargement, changement d'état, fin de partie,
déchargement, retour au menu, tournoi entre deux parties, interface ouverte par-dessus. D'abord les
risques de tout jeu en réseau, puis les cas de Beep Boop avec ce qui doit se passer, ce qu'on a
trouvé (OK, ou le bug corrigé) et le test qui le provoque.

Tests : `OnlineTransitions_*` (`OnlineTransitionsScenarioTests.cs`), avec l'invité buildé et ses
scénarios (`freeze-at-results`, `freeze-at-launch`, `freeze-as-finish-ends`, voir SKILL.md). Une course
d'une image se rend déterministe : l'invité **gèle** juste avant l'instant visé et l'hôte agit pendant
le gel (au réveil, tout arrive dans la même image), ou le test agit **dans l'événement** même
(`SceneManager.sceneLoaded`, `FlowMachine.OnStateEntering`, appel par réflexion dans la même image).
La perte de la salle côté hôte (transport, Relay) se simule par `OnlineManager.HandleConnectionLost`.

Deux contrôles valent pour **tous** les tests de scénario :
- l'éditeur (`ScenarioDriver`) et l'invité (`OnlineGuestBot`) échouent si un état de la partie entre
  après `EndState` (une partie qui revit pendant son déchargement) ;
- l'invité échoue si, après avoir perdu la salle, son menu ne s'ouvre pas sur le hub qui dit pourquoi.

Vérifié le 04/11/2026. Avant les correctifs, 6 des 9 tests échouaient, chacun sur son bug. Les 3
autres passaient (E5, chargement interrompu, minuteries d'états quittés) et restent comme garde-fous.
Après : 9/9, et la suite complète 35/35 (05/11/2026).

## Risques communs à tout jeu en réseau

| Famille | Ce qui casse | Où regarder ici |
| --- | --- | --- |
| Ordres croisés | L'ordre de lancer arrive avec la perte de la salle, ou après un départ | `LaunchInMenu`, `Status == None` |
| File de messages vidée trop tôt | Le début de la partie suivante efface les messages de la précédente pas encore joués | `StartGame` (`m_gameInbox.Clear`) |
| Rafale après un gel | Plusieurs changements d'état dans la même image | `GameplaySync.Update` (rejoue tout) |
| Demandes tardives | Un minuteur, un objectif, une manche demande un état pendant le déchargement | `FlowMachine.Lock` |
| Objet du jeu qui manque à l'appel | La partie se charge sans sa synchronisation (salle perdue entre-temps) | `GameplaySync.AttachTo` |
| Interface qui survit à sa raison d'être | Pause, dialogue, écran qui consomme un message sous l'écran de chargement | `PauseMenuManager`, `HubScreen.TakeLostRoomReason` |
| Objets persistants | Contexte de tournoi, d'entraînement, compteurs restés d'une partie quittée | `TournamentContext.Abandon`, `TournamentCloser` |
| Textes techniques | La couche réseau donne sa propre raison | `OnlineConnection.RefusalReason` |
| Double fermeture | Deux causes ferment la même chose (perte + appui) | gardes `m_isEnding`, `m_isClosing` |

## Lancement (menu → partie)

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| L1 | Salle perdue entre l'ordre de lancer et le menu de l'invité (encore dans la partie d'avant) | Pas de partie | OK (`LaunchInMenu` vérifie la salle) | `OnlineMenu_LeavesAndReopensRightAway` |
| L2 | Salle perdue pendant le lancement de l'invité (menu encore là, chargement commencé) | Hub qui dit pourquoi, une fois | **Bug** : le menu passait au hub sous l'écran de chargement, qui prenait la raison ; la partie se chargeait, repartait, et le menu rouvrait sur la sélection des personnages sans rien dire. Corrigé : le menu ne réagit plus pendant un lancement (`IsInGame`), la partie part une fois chargée, le menu s'ouvre sur le hub | `HostLosesTheRoomAsTheGuestLaunches` (gel de l'invité au lancement ; bug reproduit aussi avec une fermeture de salle, même chemin chez l'invité) |
| L3 | L'hôte perd sa salle pendant le chargement de sa partie | Hub qui dit pourquoi | **Bug** : la partie démarrait sans synchronisation (« pas en salle »), hors ligne, et attendait au tutoriel le joueur de l'invité parti, sans pause possible : hôte bloqué. Corrigé : toute partie lancée d'une salle s'attache (`IsInGame`) et repart si la salle est perdue | `HostLosesTheRoomAsTheGameLaunches` (perte au `sceneLoaded`) |
| L4 | Lancement reçu pendant un gel de l'invité | La partie se lance au réveil | OK | `GuestLagsAsTheHostRelaunches` |
| L5 | L'invité arrive pendant le lancement | Refus « partie en cours » | OK (`ApproveGuest`) | — |
| L6 | Lancement reçu à l'image où l'invité passe de « connexion » à « en salle » | Lancé | Non atteignable (l'hôte confirme bien après la liste des joueurs) | — |

## Chargement

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| C1 | Chargement interrompu (départ, salle perdue), puis partie suivante | Rien de la partie quittée ne gêne la suivante, sur les deux machines | OK | `OnlineDrops_GuestQuitsWhileLoading` (rejoue une partie entière ensuite) |
| C2 | Gel plus court que le délai pendant le chargement (activation de scène lente) | Rien de perdu, l'hôte attend | OK (60 s de marge) | — |
| C3 | Voir aussi C5, C6 de disconnections.md | | | |

## Pendant la partie : changements d'état

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| P1 | Salle perdue dans l'image où un minuteur (configuration, fin du chrono, dernière manche de Last Man Standing) demande un état | La partie part, rien ne démarre après End | **Bug** : la demande passait derrière End, et l'annonce démarrait dans une partie en déchargement. Corrigé : `EndState` verrouille la machine (`FlowMachine.Lock`), toute demande ensuite est ignorée | `HostLosesTheRoomAsAConfigurationChanges` |
| P2 | Minuterie d'un état déjà quitté (fin de partie 4 s, tutoriel 3 s, introduction) qui échoit après End | Rien | OK : la machine débranche un état quitté (`OnAnotherStateRequested`), sa demande ne va nulle part | `HostClosesTheRoomAsTheGuestsGameEnds` (gel de l'invité quand sa fin de partie échoit) |
| P3 | Pause ouverte en ligne quand la partie se termine (en ligne, elle n'arrête pas le jeu) | La pause se ferme, les résultats se jouent | **Bug** : elle restait ouverte sur la fin et les résultats ; « Menu principal » pendant End quittait la salle sans abandonner le tournoi, et l'hôte enchaînait sur les résultats hors ligne. Corrigé : la pause se ferme à l'entrée de Finish, Results ou End | `PauseOpenAsTheGameEnds` |
| P4 | Pause ouverte pendant les annonces de configuration ou de manche | Reste ouverte | OK (états avec `CanBePausedTag`) | `PauseOpenAsTheGameEnds` |
| P5 | Rafale d'états après un gel (Finish, Results, End) | Rejoués dans l'ordre | OK | `GuestLagsAs*` |
| P6 | Message tardif d'un invité après un changement d'état (prêt après le tutoriel, bouton après la partie) | Ignoré | OK (`PerformRemoteAction` exige un robot actif, statut du tutoriel sans abonné) | — |
| P7 | Départ pendant Finish ou Results | La fin continue | OK, mais le robot du partant meurt sur place (Least Deaths : un mort de plus) | — |

## Fin de partie et retour au menu

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| F1 | Salle perdue pendant End | Hub qui dit pourquoi | OK (pas de second End) | `HostLosesTheRoomAsATournamentGameEnds` (côté invité) |
| F2 | Salle perdue pendant le chargement du menu | Hub qui dit pourquoi | OK (`Run` : `HasLostRoomReason`) | — |
| F3 | L'hôte relance avant que l'invité soit revenu au menu | L'invité rejoint la partie | OK (corrigé avant, E5) | `GuestLagsAsTheHostRelaunches` (gel de l'invité couvrant la relance) |
| F4 | Pic de lag de l'invité au moment de quitter les résultats | Les résultats se ferment au réveil | OK | `GuestLagsAs*` |

## Tournoi entre deux parties

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| T1 | Pic de lag de l'invité pendant que l'hôte quitte les résultats puis choisit la partie suivante | L'invité finit la partie et joue la suivante | **Bug** : le choix de l'hôte démarrait la partie suivante à l'arrivée et vidait la file, avec le End de la partie en cours : l'invité restait dans ses résultats, l'hôte le renvoyait au bout de 60 s. Corrigé : la partie suivante ne commence chez l'invité qu'au moment où il la charge (`StartTheNextTournamentGame`) | `GuestLagsAsATournamentGameEnds` |
| T2 | L'hôte perd sa salle pendant la fin d'une partie du tournoi | Tournoi fermé, hub qui dit pourquoi | **Bug** : l'hôte enchaînait sur les résultats puis les parties suivantes hors ligne, contre des robots fantômes immobiles. Corrigé : un tournoi lancé en ligne se ferme dès que la salle n'est plus là (`HasLostTheRoom`), sur l'hôte aussi | `HostLosesTheRoomAsATournamentGameEnds` (perte à l'entrée de End) |
| T3 | Salle perdue pendant le chargement du niveau suivant | Retour au hub | OK après L3 (la partie s'attache et repart) | — |
| T4 | Salle perdue sur l'écran des résultats du tournoi, puis appui sur Continuer | Une seule fermeture | Corrigé par précaution (`m_isClosing`) | — |
| T5 | Choix de l'hôte reçu avant que l'invité ait affiché ses résultats | Gardé, appliqué ensuite | OK | `Online_PlaysATournamentWithAGuestBuild` |

## Écrans et textes

| # | Cas | Attendu | Trouvé | Test |
| --- | --- | --- | --- | --- |
| E1 | L'hôte arrête son jeu (fenêtre fermée, transport perdu) : Netcode dit à l'invité « Disconnected due to host shutting down. » | Une raison en français | **Bug** : ce texte anglais de Netcode passait pour une raison de l'hôte (le filtre ne connaissait que « [Disconnect Event] »). Corrigé : « La connexion avec l'hôte a été perdue. » | `HostLosesTheRoom*` |
| E2 | Raison de la perte lue sous l'écran de chargement | Lue au hub, visible | Corrigé avec L2 | `HostLosesTheRoomAsTheGuestLaunches` |
| E3 | Écran de chargement après une sortie en plein chargement | Caché au menu | OK (`MainMenuOrchestrator`) | — |

## Hors périmètre

- Une machine à plusieurs invités qui partent en même temps : le code traite chaque machine à part (`GuestIds`). Les tests n'ont qu'un invité.
- Manette débranchée pendant une transition : ce n'est pas une déconnexion réseau.
