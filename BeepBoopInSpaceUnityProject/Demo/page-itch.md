# Brouillon de la page itch.io

À copier dans le formulaire de création de projet sur itch.io. Relire chaque phrase : le ton est une proposition.

Étapes de publication, sources et liste de contrôle : [publication.md](publication.md).

## À relire avant de copier

Retirer les crochets une fois chaque point tranché.

- **« planète marécageuse » / « swampy planet »** : supposition, pas un fait établi. Elle ne repose que sur des noms internes (`SwampArena`, `M_SwampWater`). Confirmer ou réécrire.
- **Application itch.io et SmartScreen** : seuls des messages de forum l'affirment. Garder la phrase seulement si le test sur un PC neuf la confirme.
- **Lien de confidentialité** : la page reste à écrire (`publication.md`, partie 3).
- **« 5 robots » / « 5 robots »** : la banque en a 5, mais Volto n'a pas encore d'image de sélection et apparaît en silhouette grise dans le choix des robots ; les robots de l'ordinateur ne le prennent jamais. Écrire 4, ou terminer Volto avant la publication.

## Réglages du projet

| Champ | Valeur proposée |
| --- | --- |
| Title | Beep Boop dans l'espace (Demo) |
| Kind of project | Downloadable |
| Classification | Games |
| Release status | In development |
| Pricing | No payments |
| Fichier | [`Builds/BeepBoop_Win_20261119_1601.zip`](../Builds/BeepBoop_Win_20261119_1601.zip) (178 Mo, version 0.1.0 du 19/11/2026), marqué Windows. Contrôlé : démarrage, deux tournois joués par le build, salle en ligne réelle (voir `publication.md`) |
| Genre | Action |
| Tags | party-game, robots, funny, 3d, controller, local-multiplayer, multiplayer |
| Languages | French, English |
| Multiplayer | Local multiplayer, et pour le jeu en ligne « Server-based networked multiplayer » (proposition : les salles passent par les serveurs Unity), 1-4 joueurs |
| Controllers | Xbox, PlayStation, clavier |
| AI Disclosure | À décider par l'équipe (voir `publication.md`, partie 1) |

`controller-support` est remplacé par `controller`, bien plus utilisé (5 553 résultats contre 95 le 5/11/2026) : itch.io préfère les tags suggérés et déconseille les synonymes. Il déconseille aussi de répéter en tag ce que propose l'onglet des métadonnées : retirer `controller`, `local-multiplayer` et `multiplayer` si le formulaire les couvre déjà.

## Texte en français

**Des robots. Une grille. Et des touches qui changent toutes les 10 secondes.**

Beep Boop dans l'espace est un jeu de groupe où chacun pilote un petit robot perdu sur une [À RELIRE, supposition : planète marécageuse]. Le problème : à chaque nouvelle configuration, vos boutons changent de rôle. Avancer devient pivoter, pivoter devient tirer, et tout le monde cherche ses touches en même temps.

- **De 1 à 4 joueurs** : sur le même PC, contre les robots de l'ordinateur, ou en ligne avec un simple code de salle.
- **7 modes de jeu** : course aux points, tir, dernier survivant, pluie de météorites…
- **Un tournoi** qui enchaîne les modes jusqu'au sacre d'un champion.
- **5 robots** au caractère bien trempé.

Se joue à la manette (Xbox, PlayStation) ou au clavier pour un joueur.

*Ceci est une démo, votre avis nous intéresse : dites-nous en commentaire ce qui vous a fait rire et ce qui vous a agacé.*

### Comment lancer le jeu

Décompressez le dossier et lancez `Beep Boop dans l'espace.exe`. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur « Informations complémentaires » puis « Exécuter quand même » : le jeu n'est pas signé. [À CONFIRMER PAR UN TEST : L'application itch.io évite cet avertissement.]

### Vos données

Au premier lancement, le jeu demande s'il peut envoyer des statistiques d'utilisation à GameAnalytics : parties jouées, erreurs, informations sur l'appareil, identifiant aléatoire et localisation approximative déduite de l'adresse IP. Sans votre accord, rien n'est envoyé à GameAnalytics, et vous pouvez changer d'avis dans les Paramètres. Les salles en ligne passent par les services Unity, avec une connexion anonyme : Unity traite alors votre adresse IP et un identifiant de joueur.

Détails et contact : [LIEN À AJOUTER vers la page de confidentialité].

## English text

**Robots. A grid. And buttons that change every 10 seconds.**

Beep Boop in Space is a party game where everyone drives a little robot lost on a [TO REVIEW, assumption: swampy planet]. The catch: with every new layout, your buttons swap roles. Walk becomes turn, turn becomes shoot, and everybody looks for their buttons at the same time.

- **1 to 4 players**: on the same PC, against computer robots, or online with a simple room code.
- **7 game modes**: points rush, shooting, last robot standing, meteorite showers…
- **A tournament** that chains the modes until a champion is crowned.
- **5 robots** with plenty of character.

Play with a gamepad (Xbox, PlayStation) or with the keyboard for one player.

*This is a demo and we'd love your feedback: tell us in the comments what made you laugh and what annoyed you.*

### Starting the game

Unzip the folder and run `Beep Boop dans l'espace.exe`. If Windows says "Windows protected your PC", click "More info" then "Run anyway": the game isn't signed. [TO CONFIRM BY A TEST: The itch.io app avoids this warning.]

### Your data

On first launch, the game asks whether it may send usage statistics to GameAnalytics: games played, errors, device information, a random identifier and an approximate location derived from your IP address. Without your agreement, nothing is sent to GameAnalytics, and you can change your mind in the Settings. Online rooms go through Unity's services, with an anonymous sign-in: Unity then processes your IP address and a player identifier.

Details and contact: [LINK TO ADD to the privacy page].

## Visuels

| Visuel | Source | État |
| --- | --- | --- |
| Image de couverture (recommandé 630×500, minimum 315×250, ratio 315:250) | `Demo/couverture-630x500.png`, recadrée depuis `Assets/StudioIcon/Icon.png` (hélice du robot du haut coupée) | Provisoire, à remplacer par l'équipe art |
| Captures d'écran (3 à 5 recommandées) | `Demo/captures/`, 7 proposées, dans l'ordre : `01-accueil-anglais` (accueil en anglais), `02-choix-des-robots` (4 joueurs), `03-tutoriel` (objectif et touches), `04-partie-meteorites-build` (vraie partie dans le build, contre les robots), `05-partie-course-aux-points-en-enfer` (vraie partie, « +1 » et chrono), `06-resultats-build` (podium), `07-champion-du-tournoi-build`. Les trois « build » sont en 1280×720, tirées du build final ; les autres en 1920×1080, tirées des tests | Choisies, à réduire à 5 si besoin (03 et 07 sont les moins indispensables) |
| Bande-annonce | À enregistrer pendant le playtest | À faire |

## Configuration requise

À compléter après un essai sur un PC modeste : la seule mesure vient d'un PC portable puissant (RTX 3070 Ti Laptop, i9-12900H, 16 Go : 235 images/s en moyenne en 1280×720, plafonnées par l'écran à 240 Hz). En attendant : Windows 10 ou 11, 64 bits, 340 Mo d'espace une fois décompressé (zip de 178 Mo).
