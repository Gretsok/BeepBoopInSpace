# Publier la démo sur itch.io

Liste d'étapes pour la personne qui publie, sans développement. Les sources ont été consultées le 5 novembre 2026, et leur date est indiquée quand elle existe. Texte de la page : [page-itch.md](page-itch.md). Ce dossier n'est pas un avis juridique.

## À retenir d'abord

1. **Le build à publier est `Builds/BeepBoop_Win_20261119_1601.zip`** (178 Mo, 19/11/2026), fait après le lien avec Unity Cloud, avec `LISEZ-MOI.txt` et `THIRD-PARTY-NOTICES.txt` à côté de l'exe. Il a passé le lancement de contrôle et les deux tournois du smoke ; le résultat de la salle réelle (Relay) est noté dans la partie 5. Les builds plus anciens ne sont pas à publier.
2. **Unity est gratuit au départ, mais un dépassement sans moyen de paiement bloque tous les services Unity du projet.** Le mode en ligne s'arrêterait pour tout le monde. L'allocation gratuite de bande passante baisse quand il y a peu de joueurs : à mesurer avant d'ouvrir (partie 2).
3. **L'équipe est responsable de traitement** pour les données de GameAnalytics et de Unity. La page de confidentialité et le contact restent à écrire : GameAnalytics et Unity les demandent tous les deux.
4. **Le consentement va dans le bon sens** : le focus de départ est sur « Non » et le texte cite les données envoyées. Il manque encore l'éditeur et l'endroit où lire le détail (partie 3).
5. **Le champ « AI Disclosure » d'itch.io est à remplir honnêtement** : le projet a été développé en partie avec une IA générative (partie 1).
6. **Aucune source officielle ne dit que l'application itch.io ou `butler` évite SmartScreen.** La phrase de la page reste à confirmer par un test.

## 1. itch.io

### Créer le projet

- [ ] Se connecter au compte de l'équipe, ouvrir le tableau de bord, puis « Create new game » (libellé de la documentation).
- [ ] Recopier les réglages et les textes de `page-itch.md`, après avoir traité ses points « À relire ».
- [ ] Laisser la visibilité sur **Draft**, le mode par défaut.
- [ ] Remplir « AI Disclosure » : itch.io demande de signaler tout contenu produit par une IA générative et propose Graphics, Sound, Text & Dialog et Code. Ses règles ne tranchent pas pour le code. Le contrôle strict vise surtout les assets, bandes-son, BD et livres, mais répondre « non » ajoute le tag No AI : ce serait inexact si du code ou du texte généré reste dans le jeu ou sur la page. À décider par l'équipe.

### Téléverser le jeu

- [ ] Demander à l'équipe technique le build final et son zip (skill `unity-build`, avec `run-build --smoke`).
- [ ] Vérifier que le zip contient l'exe, `LISEZ-MOI.txt` et `THIRD-PARTY-NOTICES.txt`.
- [ ] Dans « Uploads », envoyer le zip et cocher **Windows**. Le zip du 19/11 pèse 178 Mo ; la limite par défaut serait de 1 Go par fichier (forum, non officiel), et la limite réelle s'affiche dans la zone d'envoi.

### Images

| Image | Ce que dit itch.io | Fichier |
| --- | --- | --- |
| Couverture | Ratio 315:250, minimum 315×250, recommandé 630×500. GIF animé accepté | `Demo/couverture-630x500.png` (PNG, 303 Ko), provisoire |
| Captures | 3 à 5 recommandées, facultatives, GIF accepté. Pas de dimension ni de format officiels trouvés | `Demo/captures/` : 7 captures choisies, détaillées dans `page-itch.md` (Visuels) |

Un guide communautaire annonce 3 Mo au maximum pour la couverture : non confirmé, se fier à la fenêtre d'envoi.

La couverture provisoire garde les quatre visages entiers, avec un cadrage décalé de 13 px de l'image d'origine vers le bas par rapport au centre, car le cadrage centré coupait un œil du robot vert. L'hélice du robot du haut reste coupée : au ratio 630:500, on ne peut pas garder à la fois l'hélice et cet œil. À signaler à l'équipe art.

### Tester avant d'ouvrir

| Mode | Qui accède | Usage |
| --- | --- | --- |
| Draft | Les éditeurs du projet et les détenteurs du lien secret. La documentation déconseille de diffuser ainsi : les autres ne pourraient ni télécharger ni acheter | Relire la page, tester son propre téléchargement |
| Restricted | Les personnes approuvées (clé de téléchargement), avec un mot de passe en option, qu'on peut aussi mettre dans l'URL. Absent de la recherche et des moteurs | **Playtest avec des testeurs extérieurs** |
| Public | Tout le monde. Options : « Disable new downloads » et « Unlisted in search & browse » | Ouverture |

- [ ] Passer en Restricted avec un mot de passe et le donner aux testeurs.
- [ ] Sur un PC qui n'a jamais eu le jeu : télécharger depuis le navigateur, décompresser, lancer, répondre à la question des statistiques, créer une salle.
- [ ] Refaire ce test avec l'application itch.io et noter si SmartScreen apparaît.

### Données personnelles sur la page

- **Sources :** ni les conditions d'itch.io (15 avril 2023) ni ses règles de qualité n'imposent explicitement au créateur d'afficher ses traitements de données. Elles demandent d'éviter les murs de connexion, surtout pour collecter des informations sur les joueurs : le jeu n'en a pas. Les conditions interdisent aussi de collecter des informations sur autrui, sans préciser le cas des statistiques de jeu : à faire lire.
- **À faire :** garder la section « Vos données » et y mettre le lien vers la page de confidentialité. L'obligation vient du RGPD et des fournisseurs (partie 3), pas d'itch.io.

### `butler` pour les mises à jour

Le zip par le site suffit pour une première version. À chaque mise à jour, il faudra le renvoyer en entier.

- [ ] Installer `butler` depuis https://itchio.itch.io/butler, le décompresser et ajouter son dossier au PATH. Vérifier avec `butler version`, puis `butler login` (connexion dans le navigateur). L'application itch.io (v26.12.0 ou plus) propose aussi une interface d'envoi.
- [ ] Envoyer avec `butler push <dossier-du-build> <compte>/<projet>:windows --userversion 0.1.0`. Un nom de canal contenant `windows` marque le build pour Windows.
- [ ] Ne garder ensuite qu'un seul fichier Windows sur la page (conseil personnel, pas une règle d'itch.io).

Intérêt : seules les données modifiées sont envoyées, un patch est disponible dès la fin de l'envoi, puis une version optimisée le remplace (environ 30 minutes pour un gros jeu), et le numéro de version est le vôtre.

### SmartScreen

- **Microsoft (23 avril 2026) :** SmartScreen se fonde sur la réputation du fichier, de l'application ou du certificat. Sans réputation, il avertit.
- **`butler` :** sa documentation ne parle ni de signature ni de SmartScreen. Rien ne montre qu'il change quoi que ce soit pour un zip téléchargé depuis le navigateur.
- **Application itch.io :** des messages de forum (2020, sans réponse d'un administrateur) disent qu'elle évite l'avertissement. Non confirmé.
- [ ] Garder les instructions « Informations complémentaires » puis « Exécuter quand même ».

**Sources :**
- https://itch.io/docs/creators/getting-started
- https://itch.io/docs/creators/quality-guidelines
- https://itch.io/docs/creators/access-control
- https://itch.io/docs/legal/terms
- https://itch.io/t/4309690/generative-ai-disclosure-tagging
- https://itch.io/docs/butler/installing.html
- https://itch.io/docs/butler/pushing.html
- https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/
- Forums et guide, non officiels : https://itch.io/t/706326/windows-smartscreen-unknown-publisher, https://itch.io/t/1925786/what-is-the-maximum-file-size-you-can-put-in-itchio, https://hedgiespresso.itch.io/itch-page-image-templates
- Tags vérifiés le 5/11/2026 : https://itch.io/games/tag-controller (5 553 résultats), https://itch.io/games/tag-controller-support (95 résultats)

## 2. Unity Relay et Lobby

### Vérifier qu'ils sont actifs

- [ ] Ouvrir https://cloud.unity.com avec le compte de l'organisation et choisir le projet lié au jeu.
- [ ] **Lobby :** Development, puis Lobby, puis l'onglet Sessions (documentation actuelle).
- [ ] **Relay :** Products, Gaming Services, Multiplayer, Relay, puis Launch ; la page Overview s'ouvre (ancienne documentation, libellés à confirmer sur place).
- [ ] **Preuve par l'usage :** avec le nouveau build, créer une salle et vérifier qu'elle apparaît dans Sessions (elle y reste une heure après sa dernière mise à jour). Un refus à la création de la salle signale un service inactif ; le détail est dans le log du jeu.

Je n'ai pas accès au tableau de bord : l'état réel du projet et de son organisation reste à constater.

### Ce qui est gratuit (chiffres relevés le 5/11/2026)

La grille de Unity n'indique pas de date ; l'article sur Relay a été mis à jour le 24 septembre 2026.

| Service | Gratuit par mois | Au-delà |
| --- | --- | --- |
| Relay, joueurs | 50 CCU moyens (environ 2 160 000 minutes de connexion) | 0,16 $ par CCU moyen supplémentaire |
| Relay, bande passante sortante | 3 GiB par CCU moyen, 150 GiB au plus | 0,09 $/GiB (US + UE), 0,16 $/GiB (Asie + Australie) |
| Lobby | 10 GiB par groupe de régions | Mêmes tarifs |
| Authentication | Gratuit | Gratuit |

Ordres de grandeur (mes calculs) :
- 50 CCU représentent 36 000 heures-joueur par mois, soit environ 12 salles de 4 machines ouvertes en permanence.
- Passer à 100 CCU moyens coûterait 8 $ par mois, plus la bande passante.
- **Le point à mesurer :** 3 GiB par CCU font environ 4,3 Mio par heure et par joueur connecté (environ 10 kbit/s). Si le jeu envoie plus, l'allocation de bande passante peut être dépassée même avec très peu de joueurs. Le trafic réel du jeu n'a pas été mesuré.

### Moyen de paiement

- **Pour démarrer :** pas besoin d'après la FAQ de facturation de Unity (juin 2022) pour Relay et Lobby. La documentation actuelle dit seulement que certains services s'en passent et renvoie à la page de chaque service : à constater sur le tableau de bord.
- **Au dépassement sans paiement (documentation, mise à jour il y a environ 6 mois) :** accès à tous les services et API bloqué. Il faut saisir un moyen de paiement ; sans cela sous 30 jours, les services sont désactivés pour le projet.
- **Avec un moyen de paiement :** facturation à l'usage et **aucun plafond trouvé**. Les budgets ne font que prévenir, et Relay ne peut pas y être inclus.

**Décision à prendre, avec ma recommandation :**
- [ ] Mesurer d'abord le trafic : partie en ligne entre deux PC, puis lecture de l'usage Relay dans Service usage (le relevé peut avoir du retard).
- [ ] Si le trafic dépasse nettement 4 Mio par heure et par joueur, enregistrer un moyen de paiement avant d'ouvrir : les montants seraient faibles, et cela évite la coupure.
- [ ] Sinon, on peut ouvrir sans carte, en acceptant qu'un succès inattendu coupe le mode en ligne (le jeu local n'utilise pas ces services : la connexion Unity n'a lieu qu'à la création ou à l'entrée dans une salle).

### Quoi surveiller après la sortie

- [ ] Chaque semaine : Manage organization, puis Service usage (CCU et bande passante de Relay, bande passante de Lobby).
- [ ] Courriels à 75 % et 100 % du palier gratuit (FAQ de 2022, à confirmer) : vérifier qu'ils arrivent à une adresse lue. Ils vont vraisemblablement aux responsables de l'organisation.
- [ ] Budget : Billing & Cost, puis Budget alerts. Utile pour Lobby seulement.
- [ ] Coût réel : rapport Cost and usage (rôle Owner requis).
- [ ] Retours des joueurs sur les salles : une panne pour tous peut signaler un blocage. Toutes les communications d'une salle passent par une seule région, choisie par l'hôte, donc des joueurs éloignés peuvent avoir de la latence.

**Sources :**
- https://unity.com/products/gaming-services/pricing
- https://support.unity.com/hc/en-us/articles/4410136449812-How-is-the-Relay-Service-Priced
- https://docs.unity.com/en-us/services/pricing-and-billing
- https://discussions.unity.com/t/unity-gaming-services-billing-faqs/884845
- https://docs.unity.com/en-us/cloud/billing/budget
- https://docs.unity.com/en-us/cloud/billing/budget/manage-budgets
- https://docs.unity.com/en-us/mps-sdk/sessions/view-sessions
- https://docs.unity.com/ugs/en-us/manual/relay/manual/get-started
- https://docs.unity.com/en-us/mps-sdk/relay-limitations

## 3. GameAnalytics et RGPD

### Ce que disent les sources

| Sujet | Contenu | Source et date |
| --- | --- | --- |
| Rôles | L'éditeur du jeu est responsable de traitement, GameAnalytics sous-traitant pour les données du SDK. Le DPA fait partie du contrat, y compris en gratuit, sans signature séparée | Notice et DPA v4.0, 2/11/2026 |
| Identifiant | Aléatoire sous Windows | Documentation du SDK Unity |
| Adresse IP | Sert à déduire une localisation approximative (pays, région, ville). Gardée seulement tronquée, 30 jours au plus | Notice et DPA, 2/11/2026 |
| Appareil et jeu | Informations d'appareil et de système, sessions, progression, erreurs | Notice, documentation du SDK |
| Conservation maximale | Événements bruts 12 mois, agrégats par joueur 1 an | DPA, 2/11/2026 |
| Transferts | Notamment vers les États-Unis et Hong Kong, sous clauses contractuelles types ou Data Privacy Framework selon le prestataire. GameAnalytics n'est pas lui-même certifié DPF | Notice et DPA, 2/11/2026 |
| Exigences de GameAnalytics | Information dans le jeu juste avant la question, accord avant tout envoi, retrait facile, politique de confidentialité et conditions d'utilisation accessibles, contact, preuve du consentement des joueurs, accord parental sous 16 ans, signaler un jeu destiné aux enfants | Developer Policy, 23/5/2023 |
| CNIL, consentement | Vaut aussi pour les logiciels. Préalable, libre, spécifique, éclairé (identité du responsable, finalités, droit de retrait), acte positif. Refuser et retirer aussi simples qu'accepter. Preuve à conserver | CNIL, 29/9/2020 et 3/8/2018 |
| CNIL, présentation | Éviter de mettre un choix en valeur plutôt que l'autre. Garder le choix 6 mois est une bonne pratique (contexte des sites web) | FAQ CNIL, 29/4/2026 |
| Âge | En France, consentement seul à partir de 15 ans ; en dessous, conjointement avec le parent. GameAnalytics demande 16 ans | CNIL, 2018 et 2021 |
| Information | Identité et contact du responsable, finalités, base légale, destinataires, durées, droits, retrait, réclamation | RGPD, article 13 (texte sur le site de la CNIL) |
| Registre | Les organismes de moins de 250 salariés inscrivent au moins les traitements non occasionnels. Modèle simplifié disponible | CNIL, 13/4/2018 |
| Unity Relay et Lobby | Adresse IP et identifiant de joueur. Unity sous-traitant, développeur responsable : base légale, politique propre, demandes d'accès et d'effacement passant par le support Unity. Conservation 30 jours (Relay), journaux de Lobby jusqu'à 30 jours | Documentation Unity, dates relatives (2 à 4 mois) |

### Ce que fait le jeu (lecture du code et de la capture 37, pas un test en conditions réelles)

**Dans le bon sens :**
- GameAnalytics ne démarre qu'après « Oui ». Jusque-là, l'objet du SDK reste inactif (`AnalyticsManager`) : il n'écoute pas les erreurs et n'appelle pas son code natif en quittant le jeu, ce que son `OnApplicationQuit` ferait sans vérifier que le SDK a démarré. À confirmer par le test du refus ci-dessous.
- Le focus de départ est sur « Non ». « Non », B et Échap valent refus, enregistré aussitôt.
- « Oui » ignore les appuis pendant 0,35 s, « Non » répond tout de suite, et les deux boutons sont sur la même ligne.
- La question cite les données envoyées : parties jouées, erreurs, appareil, identifiant aléatoire, localisation approximative déduite de l'adresse IP. `LISEZ-MOI.txt` aussi.
- « Statistiques : oui/non » est à deux actions depuis l'accueil (Paramètres, puis le bouton) et s'applique tout de suite.
- Pas d'identifiant personnalisé ni de statistiques d'images par seconde. Analytics, crash reporting et performance reporting de Unity sont désactivés dans `UnityConnectSettings`.
- La connexion à Unity n'a lieu qu'à la création ou à l'entrée dans une salle.

**À revoir :**
1. **Le texte de la question reste incomplet** : il ne cite ni l'éditeur ni où lire le détail, qui n'existent pas encore. `LISEZ-MOI.txt` a le même manque.
2. **Le choix n'expire jamais.** Le repère de 6 mois vient du web : à faire trancher.
3. **Après un retrait en cours de session**, les événements s'arrêtent, mais la documentation indique que les configurations à distance peuvent encore arriver, et l'objet du SDK reste actif jusqu'à la fermeture. Plus rien ne démarre au lancement suivant.
4. **Pas d'identifiant visible par le joueur** : une recherche dans `Assets/Game` ne trouve aucun usage de l'identifiant Unity ou GameAnalytics. Une demande d'accès ou d'effacement serait donc difficile à relier à une personne.

### À faire, sans développement

- [ ] **Décider qui est responsable de traitement** : quelle structure ou personne se cache derrière « Confiture De Jeux », avec quel statut. Aucun document du dossier ne le dit.
- [ ] **Créer une adresse de contact lue régulièrement.** Il n'y en a aucune dans `page-itch.md` ni dans `LISEZ-MOI.txt`.
- [ ] **Écrire la page de confidentialité, en français et en anglais, à une adresse publique stable**, avec :
  - le responsable et son contact ;
  - deux traitements : statistiques (consentement) et salles en ligne (base légale à déterminer) ;
  - les données, les finalités, les destinataires (GameAnalytics, Unity), les transferts et les durées ci-dessus ;
  - les droits : accès, rectification, effacement, limitation, opposition, portabilité, retrait, réclamation auprès de la CNIL ;
  - comment changer d'avis (Paramètres, puis Statistiques) ;
  - ce qui est prévu pour les mineurs, puis la date et la version.
- [ ] **Écrire de courtes conditions d'utilisation** : GameAnalytics les demande aussi.
- [ ] **Remplir une fiche par traitement** dans le registre simplifié de la CNIL.
- [ ] **Conserver la preuve du recueil** : capture datée de la question pour chaque version publiée, avec le numéro de version. Le choix n'existe que sur le PC du joueur.
- [ ] **Archiver** une copie datée de la notice et du DPA de GameAnalytics et des pages de confidentialité de Unity.
- [ ] **Tester le refus sur un PC neuf :** répondre « Non », jouer, quitter. Dans `%USERPROFILE%\AppData\LocalLow\Confiture De Jeux\Beep Boop dans l'espace\Player.log`, chercher des lignes GameAnalytics ; dans le Moniteur de ressources (onglet Réseau), observer les connexions de l'exe. Refaire avec « Oui », puis vérifier l'arrivée des événements sur le tableau de bord GameAnalytics.

### À demander à l'équipe technique (petites modifications, à arbitrer)

- [ ] Une fois le responsable et la page de confidentialité décidés : les ajouter à la question, en français et en anglais, et à `LISEZ-MOI.txt`. La CNIL admet une information sur deux niveaux.

### À faire vérifier par un juriste ou un référent RGPD

- Consentement seul suffisant pour les statistiques, sans recourir à l'exemption de mesure d'audience (peu probable avec un tiers qui reçoit les données).
- Base légale des salles en ligne, sans question préalable aujourd'hui : le joueur demande le service, mais son adresse IP et un identifiant partent chez Unity.
- Sens exact de l'exigence de GameAnalytics sur le choix par type de données, alors que le jeu pose une seule question.
- Public visé et mineurs : un jeu de groupe aux robots peut attirer des enfants. Le jeu ne demande pas l'âge. Seuil de 15 ou 16 ans, et faut-il prévenir GameAnalytics ?
- Traitement des demandes d'accès et d'effacement sans identifiant visible.
- Suffisance des garanties de transfert vers les États-Unis et Hong Kong.
- Durée du choix et moment où il faut reposer la question.
- Mentions légales éventuelles de la page qui héberge la politique.

**Sources :**
- https://www.gameanalytics.com/trust/privacy-notice
- https://www.gameanalytics.com/trust/eu-data-processing-addendum
- https://www.gameanalytics.com/trust/privacy-faq
- https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/configuration/
- https://docs.unity.com/en-us/mps-sdk/privacy/privacy-overview-relay
- https://docs.unity.com/en-us/mps-sdk/privacy/privacy-overview-lobby
- https://www.cnil.fr/fr/cookies-et-autres-traceurs/que-dit-la-loi
- https://www.cnil.fr/fr/cookies-et-autres-traceurs/regles/cookies/FAQ
- https://www.cnil.fr/fr/les-bases-legales/consentement
- https://www.cnil.fr/fr/recommandation-1-encadrer-la-capacite-dagir-des-mineurs-en-ligne
- https://www.cnil.fr/fr/reglement-europeen-protection-donnees/chapitre3
- https://www.cnil.fr/fr/RGPD-le-registre-des-activites-de-traitement

## 4. Licence Pixabay (musique et sons)

### Jeu distribué gratuitement

- **Permis :** usage commercial et non commercial, sans attribution obligatoire. La FAQ cite l'ajout à une œuvre plus large, dont un jeu, comme une transformation suffisante. Pour la musique, elle exige que le morceau fasse partie d'une œuvre plus large et ne soit pas distribué comme fichier isolé. La licence est décrite comme irrévocable, mondiale et perpétuelle (conditions du 20 octobre 2026).
- **Interdit :** vendre ou distribuer le contenu seul (sans effort créatif, sous une forme presque identique), l'utiliser comme marque ou logo, faire un usage trompeur ou illégal.
- **Attention :** Pixabay ne garantit rien. Vérifier les droits de tiers reste à la charge de l'utilisateur.
- **Lecture du projet :** les 7 éléments de `THIRD-PARTY-NOTICES.txt` affichent bien la Pixabay Content License (pages lues le 5/11/2026) et correspondent aux 4 musiques et 3 sons de `Assets/RawAssets`. Les autres sons sont dans un dossier `Selfmade`, dont je n'ai pas vérifié l'origine autrement que par ce nom.

### À faire

- [ ] Conserver une preuve pour chaque élément : lien, nom de fichier et résumé de la licence (conseil de la FAQ), avec une capture datée de la page.
- [ ] Regarder sur les pages des 4 musiques s'il y a une icône de bouclier, signe d'un enregistrement Content ID. Ma lecture du texte des pages n'en montre aucune, mais elle ne voit pas les icônes. Cela compte dès la bande-annonce sur YouTube : une réclamation reste possible et se conteste.
- [ ] Facultatif : dans les notices, créditer les auteurs d'origine indiqués sur Pixabay pour « Bottle Pop » (jackyyang09, Freesound) et « Pop! » (oldedgar, Freesound).

### À revérifier en cas de vente

- [ ] Relire les conditions à la date de la vente et garder la version datée.
- [ ] Le jeu vendu reste une œuvre plus large, mais ne pas vendre séparément la bande-son ou les sons (page Soundtrack, DLC audio, pack de sons).
- [ ] Faire lire la clause sur les droits de tiers : pour une vente ou une promotion, elle prévient qu'un accord ou une licence peut être nécessaire, et laisse la vérification à l'utilisateur.
- [ ] Vérifier l'origine des 2 sons publiés par `freesound_community` : la page Pixabay n'indique pas la licence d'origine sur Freesound.
- [ ] Revérifier Content ID avant toute campagne vidéo payante.

**Sources :**
- https://pixabay.com/service/license-summary/
- https://pixabay.com/service/terms/
- https://pixabay.com/service/faq/
- Les pages des éléments, listées dans `Distribution/THIRD-PARTY-NOTICES.txt`

## 5. Dernière liste de contrôle avant d'ouvrir

**Build**
- [x] Build final fait après le lien Unity Cloud, contrôlé (`run-build --smoke`), avec `LISEZ-MOI.txt` et `THIRD-PARTY-NOTICES.txt` dans le zip : `Builds/BeepBoop_Win_20261119_1601.zip`, le 19/11/2026. Même jour, sur un seul PC : salle réelle par Unity Relay entre deux copies de ce build réussie (`relay-test` : code de salle, deux joueurs anonymes, mêmes scores), et premier lancement sans réponse à la question des statistiques sans aucune connexion réseau ni ligne GameAnalytics dans le log.
- [ ] Version affichée dans le menu identique à celle annoncée.
- [ ] Partie en ligne réussie entre deux PC sur deux réseaux différents, avec ce build.
- [ ] Partie complète sur un PC modeste ; configuration requise remplie dans la page.
- [ ] Playtest fait (`playtest.md`) et retours bloquants traités.

**Page itch.io**
- [ ] Points « À relire » de `page-itch.md` traités, dont « planète marécageuse » et la phrase sur l'application itch.io.
- [ ] Couverture provisoire remplacée, ou gardée en connaissance de cause.
- [ ] 3 à 5 captures choisies, anglais et français relus, tags et métadonnées vérifiés dans le formulaire.
- [ ] Zip marqué Windows, téléchargement testé depuis la page en mode Restricted.
- [ ] AI Disclosure rempli.

**Services**
- [ ] Salle visible dans Lobby, puis Sessions.
- [ ] Trafic Relay mesuré et décision prise sur le moyen de paiement.
- [ ] Adresse de réception des alertes Unity vérifiée.
- [ ] Événements reçus sur GameAnalytics après « Oui », rien après « Non ».

**Données et droits**
- [ ] Responsable de traitement désigné, adresse de contact active.
- [ ] Page de confidentialité et conditions d'utilisation en ligne, liées depuis la page itch.io (et idéalement depuis le jeu).
- [ ] Éditeur et lien vers le détail ajoutés au texte de la question.
- [ ] Relecture juridique faite, ou risque accepté en connaissance de cause.
- [ ] Preuves archivées : capture de la question, licences Pixabay, versions des textes GameAnalytics et Unity.

**Ouverture**
- [ ] Passer en **Public**, ouvrir la page sans être connecté et télécharger le jeu une dernière fois.
- [ ] Surveiller les commentaires et Service usage pendant les premiers jours.

## Ce que je n'ai pas pu confirmer

- L'état réel du tableau de bord Unity, le chemin actuel de Relay et la présence d'un moyen de paiement sur l'organisation.
- Si Relay et Lobby démarrent toujours sans moyen de paiement (source de 2022), et si les courriels à 75 % et 100 % existent toujours.
- Si un petit dépassement de bande passante à faible CCU déclenche le blocage : la documentation parle d'un dépassement de la limite, sans cas chiffré.
- Le trafic réseau réel du jeu.
- L'effet de l'application itch.io sur SmartScreen : forums seulement.
- Les formats et poids maximum officiels des images itch.io, les libellés exacts du formulaire connecté et le nombre maximum de tags.
- Ce que fait le code natif de GameAnalytics (fichier compilé, non lisible). Sans accord il n'est plus appelé, selon la lecture du code ; après un retrait en cours de session, il l'est encore à la fermeture.
- La recommandation consolidée de la CNIL (PDF de janvier 2026, illisible avec mes outils) : la FAQ du 29 avril 2026 a servi à la place.
