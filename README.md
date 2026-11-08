# Projet .NET — GroupSwitch v1.1

## Gestion simplifiée des groupes et des changements d'affectation

## 1. Contexte et objectif

Une école répartit ses étudiants dans plusieurs groupes pédagogiques. Après la répartition, certains étudiants souhaitent changer de groupe. Un changement direct n'est pas toujours possible : le groupe demandé peut être complet, mais un échange entre plusieurs étudiants peut permettre de trouver une solution.

Le cursus d'un étudiant n'est pas nécessairement linéaire. Un étudiant inscrit principalement en bloc 3 peut, par exemple, devoir encore suivre un ou plusieurs cours du bloc 2. Il peut donc appartenir simultanément à des groupes relevant de plusieurs années d'études.

**Un étudiant n'appartient pas à un groupe unique : il possède des affectations distinctes pour les différentes activités pédagogiques qu'il doit suivre.**

L'objectif de **GroupSwitch** est de développer un prototype web permettant de gérer les étudiants, les activités pédagogiques et les groupes, d'enregistrer les demandes de changement, de proposer des échanges cohérents et de vérifier les conflits d'horaires. Une fonctionnalité d'intelligence artificielle assistera le coordinateur dans l'analyse des demandes et la recherche de solutions.

L'application doit notamment permettre de :

- gérer les étudiants et les activités pédagogiques ;
- organiser les groupes associés aux différentes activités ;
- gérer les affectations indépendamment pour chaque activité ;
- tenir compte des étudiants qui suivent des cours de plusieurs blocs ;
- rechercher des échanges entre étudiants sans modifier leurs autres affectations ;
- vérifier les capacités des groupes et les conflits d'horaires ;
- assister le coordinateur dans la recherche de solutions.

## 2. Architecture et technologies obligatoires

L'application comprend deux applications distinctes :

- **Frontend web** : React, Angular, Vue ou une technologie équivalente.
- **Backend** : API REST ASP.NET Core, communiquant avec le frontend en HTTP/JSON.

Le backend doit être organisé en couches. Pour garder le projet à une taille raisonnable, l'architecture classique suivante est recommandée :

```text
GroupSwitch/
├── frontend/                  # Application web indépendante
└── backend/
    ├── GroupSwitch.Api/       # Endpoints HTTP
    ├── GroupSwitch.Business/  # Règles métier
    ├── GroupSwitch.Data/      # EF Core et accès aux données
    └── GroupSwitch.Tests/     # Tests automatisés
```

Technologies obligatoires :

- ASP.NET Core pour l'API ;
- un frontend distinct ;
- Entity Framework Core ;
- une base de données relationnelle, par exemple SQLite ;
- Git ;
- authentification par JWT, avec gestion des comptes, connexion et autorisations par rôle ;
- SignalR pour la communication en temps réel entre le backend et le frontend ;
- un mécanisme de webscraping permettant de récupérer réellement les horaires depuis l'instance Hyperplanning ciblée ;
- un service d'IA accessible par API ou un modèle local ;
- des tests automatisés des règles métier et des mécanismes de sécurité.

## 3. Fonctionnalités obligatoires

### 3.1. Gestion des étudiants, des activités et des groupes

Le coordinateur doit pouvoir, depuis le frontend :

- ajouter, supprimer et modifier un étudiant ;
- consulter la liste des étudiants et leurs affectations a un ou plusieurs blocs d'études;
- créer et modifier des blocs d'études ;
- créer et modifier les groupes associés à un bloc ;
- définir une capacité maximale pour chaque groupe de chaque bloc d'études;
- importer les étudiants depuis un ou plusieurs fichiers CSV documentés ;
- gérer les affectations des étudiants pour chaque bloc ;
- modifier une affectation sans modifier les autres affectations de l'étudiant ;
- associer un étudiant à une ou plusieurs catégories virtuelles, par exemple B1, B2, B3, dispenses externes ou allègement ;
- générer une première répartition des étudiants dans les groupes.

Les groupes virtuels sont gérés exclusivement par l'administrateur et ne sont pas visibles par les étudiants. Ils servent notamment à caractériser le parcours d'un étudiant et à guider la répartition. Ils ne remplacent pas les affectations pédagogiques réelles.

#### Modèle métier attendu

Le modèle de données doit distinguer au minimum les concepts suivants :

- **Étudiant** : personne inscrite dans la formation.
- **Groupe pédagogique** : groupe d'un bloc d'études, avec une capacité maximale.
- **Affectation** : association entre un étudiant et un groupe de bloc d'études.
- **Catégorie virtuelle** : caractéristique administrative ou pédagogique d'un étudiant, utilisée pour la gestion et la répartition.

Une affectation doit donc référencer au minimum un étudiant et un groupe. Le modèle peut être enrichi avec une période, une année académique ou un statut si cela est nécessaire.

#### Importation et première répartition

Le format CSV doit être documenté. L'import doit signaler les lignes invalides, les doublons et les références inconnues sans interrompre tout le traitement.

La structure de l'import doit permettre de représenter les étudiants et leurs affectations pour plusieurs activités. Il est possible d'utiliser plusieurs fichiers CSV liés par des identifiants stables, par exemple un fichier pour les étudiants, un pour les activités et les groupes, et un pour les affectations.

La génération initiale des groupes doit respecter les règles suivantes :

- répartir les étudiants éligibles dans les groupes de bloc ;
- respecter la capacité maximale de chaque groupe ;
- répartir équitablement les étudiants issus des différentes catégories virtuelles ;
- répartir les autres étudiants de manière aléatoire, tout en respectant les contraintes précédentes ;
- ne pas affecter un étudiant à un groupe pour lequel il n'est pas concerné ;
- tenir compte, lorsque les informations sont disponibles, des dispenses et des activités déjà acquises.

Les règles de répartition doivent être déterministes dans leurs contraintes et testables. La partie aléatoire peut être contrôlée ou reproduite pour faciliter les tests.

### 3.2. Demandes de changement de la part d'un étudiant

Une demande de changement concerne **une affectation précise**, et non l'ensemble des affectations de l'étudiant.

Une demande contient au minimum :

- l'étudiant concerné ;
- le bloc concernée ;
- son affectation actuelle ;
- le groupe souhaité ;
- un motif libre obligatoire ;
- un statut : en attente, acceptée ou refusée.

Le groupe souhaité doit correspondre au même bloc que le groupe actuel. Un étudiant ne peut pas demander à remplacer son groupe de bloc 1 par un groupe de bloc 2.

Le coordinateur peut consulter les demandes et les accepter ou les refuser.

Lors de l'acceptation, le backend doit vérifier que :

- l'étudiant possède toujours l'affectation concernée ;
- le groupe souhaité appartient au bon bloc ;
- le groupe souhaité dispose d'une capacité suffisante ;
- l'opération respecte les règles métier et les éventuelles contraintes horaires applicables.

L'acceptation d'une demande ne doit modifier que l'affectation concernée. Toutes les autres affectations de l'étudiant doivent rester inchangées.

Si le groupe souhaité est complet, le système peut rechercher un échange avec un autre étudiant ou proposer une chaîne d'échanges.

### 3.3. Recherche d'un échange en chaîne

Le backend doit être capable de rechercher des échanges impliquant **trois étudiants ou plus**.

La recherche doit porter sur les demandes concernant un même bloc.

Exemple :

- Patrick souhaite passer du groupe A au groupe B ;
- Bob souhaite passer du groupe B au groupe C ;
- Carlo souhaite passer du groupe C au groupe A.

Le système peut alors proposer un cycle d'échange entre ces trois étudiants.

Actions à réaliser dans cette situation :

- rechercher un cycle simple à partir des demandes en attente ;
- présenter les étudiants et les affectations avant/après ;
- permettre au coordinateur de valider ou de refuser la proposition ;
- appliquer l'ensemble des changements ou aucun changement : une erreur ne doit pas laisser les affectations dans un état partiellement modifié.

Le backend doit utiliser une transaction adaptée pour garantir l'atomicité de l'échange.

Le système doit également éviter qu'une même demande ou affectation soit utilisée plusieurs fois dans une même proposition d'échange.

### 3.4. Exportation des groupes au format CSV et PDF

Le coordinateur doit pouvoir exporter les groupes pédagogiques afin de partager, d'archiver ou d'imprimer les listes d'étudiants.

L'exportation doit être accessible depuis le frontend et générée par le backend. Elle doit permettre de sélectionner un bloc d'études et un groupe particulier, ou d'exporter l'ensemble des groupes d'un bloc.

#### Formats obligatoires

- **CSV** : fichier exploitable dans Excel, LibreOffice Calc ou un autre tableur.
- **PDF** : document lisible et imprimable, présentant les listes d'étudiants de manière structurée.

#### Informations à exporter

Chaque export doit contenir au minimum :

- le bloc d'études ;
- le nom du groupe ;
- la capacité maximale du groupe ;
- le nombre d'étudiants actuellement affectés ;
- la liste des étudiants du groupe, avec leur nom et leur prénom.

L'application peut proposer des informations complémentaires, telles que l'identifiant de l'étudiant ou les catégories virtuelles pertinentes, à condition de respecter les autorisations d'accès et de limiter les données personnelles exportées au nécessaire.

#### Règles de fonctionnement

- Le coordinateur peut exporter un groupe précis ou tous les groupes d'un bloc.
- L'export reprend les affectations réellement enregistrées au moment de sa génération.
- Un étudiant apparaît dans chaque groupe auquel il est effectivement affecté, y compris si ces groupes relèvent de blocs différents. L'export d'un bloc ne doit cependant pas inclure les affectations d'autres blocs.
- Les étudiants sont présentés dans un ordre cohérent, par exemple par nom puis par prénom.
- Les groupes doivent être clairement séparés et identifiés dans les exports contenant plusieurs groupes.
- Les groupes vides doivent également pouvoir apparaître dans l'export, avec un effectif nul.
- Les fichiers générés doivent avoir un nom explicite comprenant, si possible, le bloc et la date d'exportation.
- Les erreurs de génération ou de téléchargement doivent être signalées clairement dans le frontend.

#### Exigences propres au CSV

- Utiliser un séparateur documenté, compatible avec les paramètres régionaux ciblés, par exemple le point-virgule.
- Définir explicitement l'encodage, de préférence UTF-8 avec BOM pour faciliter l'ouverture dans Excel.
- Inclure des en-têtes de colonnes explicites.
- Échapper correctement les séparateurs, les guillemets et les retours à la ligne présents dans les valeurs.
- Éviter les problèmes d'interprétation des valeurs comme des formules par les tableurs lorsque les données commencent par des caractères tels que `=`, `+`, `-` ou `@`.

Le format choisi doit être documenté dans le README.

#### Exigences propres au PDF

- Présenter un titre indiquant le bloc d'études et, le cas échéant, le groupe sélectionné.
- Afficher la date de génération du document.
- Présenter les groupes et leurs effectifs de manière lisible.
- Répéter les en-têtes de tableau lorsque la liste d'un groupe s'étend sur plusieurs pages.
- Gérer les listes longues, les noms composés et les caractères accentués sans perte d'information.
- Éviter de couper inutilement les informations d'un étudiant entre plusieurs pages.

#### Sécurité

L'exportation est réservée aux utilisateurs autorisés, notamment au coordinateur. Les autorisations doivent être vérifiées par le backend à chaque demande d'exportation. Le frontend ne doit pas pouvoir contourner ces restrictions en appelant directement l'API.

#### Tests attendus

Les tests automatisés doivent vérifier au minimum :

1. l'export CSV d'un groupe contenant plusieurs étudiants ;
2. l'export PDF d'un groupe contenant plusieurs étudiants ;
3. l'export de plusieurs groupes d'un même bloc ;
4. la présence correcte des groupes vides ;
5. la conservation des caractères accentués et des noms composés ;
6. l'absence d'affectations appartenant à d'autres blocs dans un export limité à un bloc ;
7. le refus d'une demande d'exportation par un utilisateur non autorisé.

## 4. Hyperplanning : webscraping et conflits horaires

Le scraping d'Hyperplanning doit être isolé dans un composant ou service dédié, distinct des règles métier.

L'application doit permettre :

- de lancer la récupération des horaires depuis Hyperplanning ;
- de transformer les données récupérées en un modèle interne stable ;
- d'identifier les activités, séances, dates, heures et groupes pertinents ;
- de consulter les cours que doit suivre un étudiant, y compris lorsqu'ils relèvent de plusieurs blocs ;
- de détecter si deux cours se chevauchent ;
- de vérifier la compatibilité d'un ensemble d'activités avec les horaires récupérés.

La détection des chevauchements doit être réalisée par le backend en C# à partir des dates et heures récupérées. Elle doit être déterministe, explicable et testable. **L’IA ne doit pas décider si deux cours se chevauchent ni déclarer un groupe compatible** : elle peut aider à interpréter les préférences formulées en langage naturel, mais les vérifications factuelles reposent sur les données Hyperplanning et les règles métier.

Les règles de calcul doivent être déterministes et testables.

## 5. SignalR : communication en temps réel

Le projet doit démontrer au moins un scénario métier dans lequel une action effectuée côté backend déclenche une mise à jour visible dans le frontend sans rechargement manuel de la page.

Scénario minimal attendu : lorsqu'une demande de changement est créée, acceptée ou refusée, le backend publie un événement SignalR et l'interface autorisée actualise la liste ou le statut concerné.

Vous devez :

- créer un Hub SignalR côté ASP.NET Core ;
- établir la connexion depuis le frontend ;
- publier un événement après la réussite d'une opération métier ;
- mettre à jour l'interface à la réception de l'événement ;
- gérer au minimum l'échec ou la perte de connexion de manière compréhensible ;
- diffuser les événements uniquement aux utilisateurs autorisés.

SignalR ne remplace pas l'API REST : les opérations de lecture et de modification restent effectuées par l'API, tandis que SignalR sert à informer les clients connectés des changements.

Les événements liés aux demandes doivent identifier l'activité concernée afin que le frontend actualise les informations pertinentes sans modifier incorrectement les autres affectations de l'étudiant.

## 6. Intelligence artificielle : interprétation des besoins et aide à la recherche de solutions

**Aucune interface de chatbot n'est demandée.**

L'IA est intégrée au workflow de traitement des demandes afin d'aider le coordinateur à comprendre les besoins exprimés et à explorer des solutions. Elle ne remplace ni les règles métier ni la décision humaine.

### 6.1. Objectif fonctionnel

Le motif libre d'une demande peut contenir des préférences ou contraintes difficiles à exploiter directement par le programme.

L'application doit utiliser un service d'IA pour transformer ce texte en une analyse structurée, puis transmettre les éléments pertinents au moteur de recherche de solutions du backend.

Par exemple, pour le motif :

« Je voudrais éviter les cours le mercredi après-midi, mais je dois absolument pouvoir suivre le cours de programmation avancée. Il me reste également un cours de mathématiques du bloc 2 à réussir. »

L'IA peut proposer :

- **Résumé** : l'étudiant souhaite éviter le mercredi après-midi tout en conservant l'accès au cours de programmation avancée et en tenant compte de son cours de mathématiques du bloc 2.
- **Préférence** : éviter les cours le mercredi après-midi.
- **Contrainte impérative déclarée** : pouvoir suivre le cours de programmation avancée.
- **Activités à prendre en compte** : programmation avancée et mathématiques du bloc 2.
- **Points à vérifier** : présence réelle des cours dans les groupes envisagés et compatibilité de l'emploi du temps complet.
- **Informations manquantes** : précision sur le caractère impératif ou négociable d'une préférence ambiguë.

L'IA doit distinguer les contraintes explicitement exprimées des préférences et des interprétations incertaines. Elle ne doit pas inventer des contraintes qui ne figurent pas dans le motif.

Le coordinateur doit pouvoir consulter cette analyse dans la fiche de demande.

### 6.2. Utilisation des contraintes pour proposer des solutions

Le niveau attendu est une **aide à la recherche de solutions**, et non une simple génération de résumé. Le workflow minimal est le suivant :

1. Un étudiant crée une demande avec un motif libre.
2. Le backend envoie au service d’IA le texte nécessaire, en limitant les données personnelles transmises.
3. L’IA renvoie une structure exploitable contenant au minimum un résumé, des préférences, des contraintes déclarées et les incertitudes éventuelles.
4. Le backend valide et normalise cette structure, puis l’utilise comme critères de recherche lorsqu’il examine les groupes ou les échanges possibles.
5. Le moteur métier recherche des solutions candidates en tenant compte des demandes en attente, des capacités des groupes et des règles d’échange. Il vérifie ensuite les horaires à partir des données réellement récupérées depuis Hyperplanning.
6. Le coordinateur consulte les solutions proposées, les contraintes prises en compte et les éventuels points à vérifier, puis décide de la suite à donner.

Le backend doit faire la différence entre une **contrainte impérative** et une **préférence**. Si une préférence ne peut pas être satisfaite, une solution peut éventuellement rester proposée avec cette limite clairement signalée. Une contrainte impérative déclarée comme telle doit être vérifiée par des règles déterministes avant qu’une solution soit présentée comme compatible.

Le moteur de recherche n’a pas à trouver toutes les solutions possibles. Il suffit de proposer un ensemble limité de solutions candidates pertinentes et de justifier, pour chacune, les vérifications réalisées et les contraintes satisfaites ou non satisfaites.

### 6.3. Répartition des responsabilités

- **IA** : interpréter le motif libre, en extraire des critères structurés, signaler les ambiguïtés et aider à expliquer les solutions candidates.
- **Backend C#** : valider les données structurées, rechercher les échanges, vérifier les capacités, gérer les affectations, calculer les conflits d'horaires à partir d'Hyperplanning et décider si une solution respecte les règles métier.
- **Coordinateur** : examiner les propositions et accepter ou refuser les demandes ou les échanges.

L'IA ne doit pas calculer elle-même les chevauchements d'horaires, inventer des données Hyperplanning, modifier directement les affectations, accepter ou refuser une demande, ni prendre la décision finale à la place du coordinateur.

### 6.4. Contraintes techniques et robustesse

- L'appel au service d'IA doit être effectué depuis le backend, jamais directement depuis le frontend avec une clé secrète.
- Le résultat doit suivre un format structuré documenté, par exemple un objet JSON avec des champs connus. Le backend doit vérifier les champs, types et valeurs avant toute utilisation.
- Une réponse absente, mal formée, contradictoire ou indisponible ne doit pas bloquer la gestion classique des demandes.
- Le texte de la demande et la sortie du modèle doivent être considérés comme des données non fiables.
- Aucune donnée personnelle inutile ne doit être envoyée au fournisseur d'IA. Ne transmettre ni identifiant d'étudiant ni information d'identité si le traitement peut être réalisé sans ces données.
- Le service choisi, sa configuration, le format de sortie, la validation, les limites et le comportement en cas d'échec doivent être documentés.
- Aucun entraînement de modèle n'est demandé.
- Les clés et secrets doivent être configurés hors du dépôt Git.

Un mode simulé clairement identifié peut être utilisé pour tester l'interface lorsque le service est indisponible, mais l'intégration réelle doit être présente dans le code et documentée.

### 6.5. Tests attendus pour l'IA

Les tests ne doivent pas dépendre systématiquement d'un appel payant ou distant à un modèle.

Il faut au minimum tester :

- la validation d'une réponse structurée correcte ;
- le rejet ou la gestion contrôlée d'une réponse mal formée ;
- le fonctionnement du workflow si le service d'IA est indisponible ;
- l'utilisation de critères extraits pour rechercher des solutions candidates ;
- la vérification déterministe de la compatibilité réelle des solutions avec les horaires et les capacités ;
- la prise en compte des activités suivies par un étudiant dans plusieurs blocs.

Les tests du moteur métier doivent pouvoir utiliser une réponse d'IA préparée et contrôlée, indépendamment du fournisseur.

## 7. Règles métier et qualité

Les règles suivantes doivent être respectées :

- un étudiant peut posséder plusieurs affectations actives pour blocs différentes ;
- pour un même bloc d'études, un étudiant ne peut pas être affecté simultanément à plusieurs groupes ;
- une affectation doit référencer un étudiant et un groupe existants et cohérents ;
- la capacité maximale d'un groupe ne peut pas être dépassée ;
- une demande de changement concerne un bloc détemriné ;
- l'acceptation d'une demande ne modifie pas les autres affectations de l'étudiant ;
- un échange en chaîne doit concerner des groupes d'un même bloc et être appliqué intégralement ou pas du tout ;
- les conflits d'horaires sont calculés de manière déterministe à partir des données récupérées depuis Hyperplanning, jamais par l'IA ;
- les contraintes horaires sont évaluées en tenant compte des activités suivies par l'étudiant, y compris dans plusieurs blocs ;
- les critères extraits par l'IA sont validés avant leur utilisation et ne contournent jamais les règles métier ;
- l'indisponibilité de l'IA ne doit pas empêcher la consultation ni le traitement classique des demandes ;
- les événements SignalR sont publiés après la réussite de l'opération métier correspondante ;
- une erreur de validation doit être communiquée clairement au frontend.

Au minimum, des tests automatisés doivent vérifier :

1. le respect de la capacité d'un groupe ;
2. la possibilité pour un étudiant d'avoir des affectations dans plusieurs blocs ;
3. la détection d'un chevauchement entre deux cours ;
4. la recherche d'un échange impliquant au moins trois étudiants ;
5. l'absence de modification des autres affectations lors d'un changement de groupe ;
6. l'absence de modification partielle si un échange échoue ;
7. la validation d'une réponse structurée de l'IA et la gestion d'une réponse invalide ;
8. la poursuite du workflow si le service d'IA est indisponible ;
9. l'utilisation de critères structurés pour rechercher des solutions, puis leur validation par les règles métier.

## 8. Sécurité et authentification

La sécurité fait partie intégrante du projet.

L'application doit distinguer au minimum deux rôles :

- **Étudiant** : peut consulter les informations qui lui sont accessibles et gérer ses propres demandes de changement.
- **Coordinateur / administrateur** : peut gérer les étudiants, les groupes, les affectations, les demandes et les échanges. Il peut également consulter les analyses IA et les propositions de solutions.

Les droits doivent être appliqués par le backend. Masquer un bouton dans le frontend ne constitue pas une protection suffisante.

### 8.1. Authentification des utilisateurs

L'application doit proposer un mécanisme complet d'authentification comprenant :

- une interface de connexion ;
- une vérification des identifiants par le backend ;
- un stockage sécurisé des mots de passe sous forme de hachages adaptés, jamais en clair ;
- une génération de jetons JWT après une authentification réussie ;
- une validation des jetons JWT pour les requêtes vers les endpoints protégés ;
- une gestion des erreurs de connexion sans révéler si un compte existe ou si son mot de passe est incorrect ;
- une déconnexion côté frontend et la suppression des informations d'authentification conservées par le client.

Le backend doit vérifier la signature, l'émetteur et l'audience du JWT, ainsi que sa date d'expiration. Les jetons doivent être signés avec une clé secrète suffisamment robuste, configurée en dehors du dépôt Git.

Les jetons doivent avoir une durée de validité limitée. Le mécanisme de renouvellement des jetons (_refresh tokens_) est recommandé si l'application doit maintenir une session au-delà de la durée de vie du JWT.

L'utilisation d'ASP.NET Core Identity est recommandée pour gérer les comptes, les mots de passe et les fonctionnalités associées.

### 8.2. Gestion des comptes et des rôles

L'application doit disposer d'un mécanisme permettant de créer et de gérer les comptes utilisateurs.

Les exigences minimales sont les suivantes :

- associer chaque compte à un rôle ;
- empêcher un utilisateur non authentifié d'accéder aux fonctionnalités protégées ;
- empêcher un étudiant d'accéder aux fonctionnalités réservées au coordinateur ;
- empêcher un étudiant de consulter ou de modifier les demandes personnelles d'un autre étudiant ;
- réserver la gestion des groupes virtuels à l'administrateur ;
- protéger les opérations d'import CSV, de modification des affectations et de validation des échanges ;
- vérifier les droits à chaque requête côté backend.

Le projet doit prévoir une manière documentée de créer le premier compte administrateur, par exemple au moyen d'une commande d'initialisation ou d'une procédure de configuration sécurisée.

### 8.3. Autorisation et protection de l'API

L'API REST doit être protégée par défaut : seuls les endpoints explicitement prévus comme publics, tels que la connexion, peuvent être accessibles sans authentification.

Les étudiants doivent notamment pouvoir uniquement :

- consulter les données autorisées par les règles métier ;
- créer une demande pour leur propre compte et pour une affectation existante ;
- consulter le statut de leurs propres demandes ;
- accéder aux informations nécessaires pour choisir un groupe, sans obtenir les données administratives ou personnelles auxquelles ils n'ont pas droit.

Le coordinateur doit pouvoir effectuer les opérations de gestion correspondant à son rôle.

Le backend doit vérifier les autorisations sur chaque ressource. Il ne suffit pas de contrôler le rôle général de l'utilisateur : il faut également vérifier que celui-ci est autorisé à consulter ou modifier la ressource concernée.

Les rôles et les identifiants d'utilisateur transmis par le frontend ne doivent jamais être considérés comme fiables. Le backend doit déterminer l'identité et les droits de l'utilisateur à partir de son authentification validée.

### 8.4. Sécurité de SignalR

Les connexions SignalR doivent être authentifiées et les groupes ou canaux de diffusion doivent respecter les droits des utilisateurs.

Le système doit empêcher :

- un utilisateur non authentifié de recevoir des événements privés ;
- un étudiant de recevoir les notifications destinées aux coordinateurs ;
- un utilisateur de rejoindre librement un groupe SignalR réservé à un autre utilisateur ou à un rôle particulier.

Les événements doivent être diffusés uniquement aux destinataires autorisés. La perte ou l'expiration de l'authentification doit être gérée proprement, notamment lors de la reconnexion.

### 8.5. Protection des données et des secrets

Le projet doit respecter les principes suivants :

- ne jamais enregistrer de mot de passe en clair ;
- ne jamais déposer de mot de passe, de clé JWT, de clé API IA ou d'identifiant sensible dans Git ;
- utiliser des variables d'environnement, les secrets utilisateur de .NET en développement ou un mécanisme équivalent ;
- fournir un fichier `.env.example` ou un exemple de configuration ne contenant que des valeurs fictives, si nécessaire ;
- limiter les données personnelles retournées par l'API aux informations réellement nécessaires ;
- ne pas transmettre de données personnelles inutiles au service d'IA ;
- éviter d'inscrire des mots de passe, des jetons JWT ou des données personnelles sensibles dans les journaux ;
- valider les données reçues par l'API et retourner des messages d'erreur qui ne révèlent ni secrets ni détails internes ;
- utiliser HTTPS dans tout environnement de déploiement.

Si les jetons sont stockés dans des cookies, les protections adaptées contre les attaques CSRF doivent être mises en place. Si le frontend utilise le stockage local du navigateur, les étudiants doivent documenter les risques liés au vol de jetons.

### 8.6. Sécurité des opérations métier

L'authentification ne remplace pas les contrôles métier.

Même pour un utilisateur authentifié et autorisé, le backend doit vérifier :

- que les groupes et les étudiants concernés existent ;
- que les capacités des groupes sont respectées ;
- que l'utilisateur a le droit de réaliser l'opération ;
- que les demandes peuvent encore être traitées dans leur état actuel ;
- qu'un échange en chaîne est appliqué intégralement ou pas du tout.

Les données issues du frontend, du scraping Hyperplanning et du service d'IA doivent être validées avant leur utilisation. Aucun résultat fourni par l'IA ne doit permettre de contourner les autorisations ou les règles métier.

### 8.7. Tests de sécurité attendus

Des tests automatisés doivent vérifier au minimum :

1. qu'un utilisateur non authentifié ne peut pas accéder aux endpoints protégés ;
2. qu'un étudiant ne peut pas effectuer une opération réservée au coordinateur ;
3. qu'un étudiant ne peut pas consulter ou modifier les demandes d'un autre étudiant ;
4. qu'un JWT invalide ou expiré est refusé ;
5. qu'un utilisateur ne peut pas modifier son rôle en manipulant une requête ;
6. que les endpoints sensibles restent protégés lorsqu'ils sont appelés directement ;
7. que les événements SignalR ne sont pas diffusés à des utilisateurs non autorisés.

### 8.8. Périmètre et attentes

Il n'est pas demandé de mettre en place une infrastructure d'entreprise complète, une authentification multifacteur ou un fournisseur d'identité externe.

En revanche, **l'authentification, la validation des JWT, la gestion des rôles et la protection des ressources doivent réellement fonctionner**. Une simple page de connexion simulée ou un rôle uniquement géré dans le frontend ne satisfait pas cette exigence.

Vous devez pouvoir expliquer :

- comment un utilisateur s'authentifie ;
- comment le backend valide un JWT ;
- comment les rôles et les autorisations sont vérifiés ;
- comment les mots de passe et les secrets sont protégés ;
- comment les connexions et les événements SignalR sont sécurisés ;
- comment les tests démontrent que les accès non autorisés sont refusés.

## 9. Livrables

Vous devez remettre :

1. un dépôt Git contenant le frontend et le backend ;
2. un fichier README expliquant comment installer et lancer les applications séparément ;
3. des exemples de fichiers CSV d'import et, si nécessaire, un jeu de données de test issu d'une récupération d'Hyperplanning ;
4. une courte description de l'architecture et des responsabilités de chaque couche ;
5. les tests automatisés ;
6. une description de l'intégration IA, de son format de sortie, de la façon dont ses critères alimentent la recherche de solutions, de sa configuration et de ses limites ;
7. une description du scraping Hyperplanning, de son fonctionnement et des données récupérées ;
8. une description du Hub SignalR, des événements publiés et du comportement du frontend ;
9. les instructions de configuration nécessaires, sans déposer de clés API, identifiants ou autres secrets dans Git ;
10. la description du mécanisme d'authentification, la matrice des droits, la configuration JWT et les résultats des tests de sécurité ;
11. une description du modèle de données précisant comment sont représentées les groupes et les affectations multiples d'un étudiant ;
12. la documentation du format CSV et de la méthode de répartition initiale.
13. quelques captures d'écran de votre application

## 10. Démonstration

La démonstration doit permettre de :

1. lancer le frontend et l'API séparément ;
2. importer des étudiants, des activités et des groupes ;
3. montrer qu'un même étudiant peut être affecté à des groupes relevant de plusieurs blocs ;
4. modifier une affectation sans modifier les autres affectations de l'étudiant ;
5. présenter un échange en chaîne impliquant au moins trois étudiants ;
6. valider cet échange et montrer les nouvelles affectations ;
7. lancer une récupération réelle des horaires depuis Hyperplanning et montrer les données obtenues ;
8. consulter les horaires et montrer la détection d'un conflit entre des activités de plusieurs blocs ;
9. démontrer une mise à jour du frontend déclenchée par SignalR sans rechargement manuel ;
10. montrer l'analyse IA d'un motif libre et expliquer comment les critères extraits sont utilisés pour rechercher des solutions candidates ;
11. montrer que les solutions candidates sont vérifiées par le backend à partir des capacités et des horaires Hyperplanning ;
12. exécuter les tests automatisés, y compris les cas d'échec de l'IA ;
13. montrer qu'un étudiant se voit refuser l'accès à une fonctionnalité réservée au coordinateur, y compris en appelant directement l'API.
14. Exporter les groupes d'un bloc au format CSV et vérifier que le fichier s'ouvre correctement dans un tableur.
15. Exporter les mêmes groupes au format PDF et présenter un document lisible, imprimable et correctement paginé.

Vous devez pouvoir expliquer les règles métier, la séparation des couches, le modèle d'affectation et le rôle précis de l'IA.

## 11. Évaluation

L'évaluation portera principalement sur :

- la séparation effective entre frontend et API ;
- la clarté de l'architecture en couches ;
- la qualité du modèle de données et de la persistance EF Core ;
- la bonne gestion des étudiants suivant des activités de plusieurs blocs ;
- la cohérence des affectations et des demandes de changement ;
- la cohérence de l'échange en chaîne ;
- le fonctionnement réel du webscraping Hyperplanning et la qualité des données récupérées ;
- la détection correcte des conflits d'horaires, y compris entre activités de plusieurs blocs ;
- une démonstration fonctionnelle de SignalR ;
- l'intégration réelle et prudente de l'IA comme aide interne ;
- la qualité de l'authentification et des autorisations ;
- les tests, la gestion des erreurs et la documentation ;
- la capacité à expliquer le code et les choix techniques;
- le fonctionnement des exportations CSV et PDF ;
- la qualité et la lisibilité des documents produits ;
- la cohérence des données exportées avec les affectations enregistrées ;
- le respect de la séparation entre les blocs et des autorisations d'exportation.

Une application simple, stable et correctement testée sera mieux évaluée qu'une application plus ambitieuse dont les fonctionnalités essentielles sont incomplètes.

## 12. Utilisation de l'intelligence artificielle comme aide à la conception et au développement\*\*

L'utilisation d'outils d'intelligence artificielle est autorisée et encouragée pour vous aider à concevoir, développer, tester et documenter GroupSwitch.

L'objectif n'est pas de produire un maximum de code à l'aide de l'IA, mais de réaliser une application de qualité, dont vous comprenez le fonctionnement et pouvez justifier les choix techniques.

Cette utilisation est distincte de l'intégration de l'IA dans l'application elle-même, décrite au point 6.

### 12.1. Utilisations recommandées

L'IA peut notamment vous aider à :

- **Concevoir l'architecture** : comparer plusieurs architectures en couches, identifier les responsabilités des projets et définir les dépendances entre les composants.
- **Modéliser les données** : proposer un modèle de classes et de relations EF Core, puis identifier les contraintes d'intégrité à appliquer.
- **Préparer le développement** : découper les fonctionnalités en tâches petites, indépendantes et vérifiables.
- **Comprendre les technologies** : obtenir des explications sur ASP.NET Core, React ou une autre technologie frontend, EF Core, JWT, SignalR, les tests et le webscraping.
- **Écrire et améliorer le code** : obtenir des exemples, simplifier une méthode, améliorer la lisibilité ou repérer des problèmes potentiels.
- **Préparer les tests** : identifier les cas limites, les scénarios d'erreur et les risques d'incohérence des affectations.
- **Déboguer** : analyser un message d'erreur, comprendre une exception ou comparer plusieurs pistes de résolution.
- **Améliorer la sécurité** : rechercher des risques dans les mécanismes d'authentification, d'autorisation et de gestion des secrets.
- **Rédiger la documentation** : clarifier le README, expliquer l'architecture et documenter les formats CSV, les API et les choix techniques.
- **Préparer la démonstration** : élaborer des scénarios réalistes et vérifier que les fonctionnalités essentielles sont démontrables.
  \*### 12.2. Bonnes pratiques

1. Comprendre le problème avant de demander du code

Avant de solliciter l'IA, formulez le besoin, les contraintes et le résultat attendu. Une demande précise produit généralement une réponse plus exploitable qu'une demande vague comme « fais-moi toute l'application ».

2. Demander des propositions argumentées

Ne vous contentez pas de demander une solution. Demandez à l'IA de comparer les options possibles, d'expliquer leurs avantages et leurs limites, puis de justifier une recommandation adaptée à la taille du projet.

3. Avancer par petites étapes

Évitez de générer toute l'application en une seule fois. Procédez progressivement : modèle de données, règles métier, tests, endpoints API, interface, authentification, SignalR et exportations. Vérifiez chaque étape avant de poursuivre.

4. Faire relire le code généré

Le code proposé par l'IA peut être incomplet, inutilement complexe ou incorrect. Vérifiez notamment :

- les règles métier et les contraintes d'intégrité ;
- la gestion des erreurs ;
- les cas limites ;
- la sécurité ;
- la cohérence avec l'architecture du projet ;
- la lisibilité et la facilité de maintenance.

5. Demander des tests en même temps que les fonctionnalités

Pour chaque fonctionnalité importante, demandez quels scénarios doivent être testés. Par exemple, lors d'un échange en chaîne, vérifiez qu'une erreur ne peut pas laisser les affectations partiellement modifiées.

Les tests doivent vérifier le comportement réel de l'application, et pas seulement confirmer que le code s'exécute sans exception.

6. Vérifier les informations techniques

L'IA peut proposer des méthodes inexistantes, des API obsolètes ou des exemples incompatibles avec la version des bibliothèques utilisées. Vérifiez les informations importantes dans la documentation officielle et dans votre environnement de développement.

7. Protéger les données et les secrets

Ne transmettez jamais à un service d'IA des mots de passe, des jetons JWT, des clés API ou d'autres secrets. Évitez également de transmettre des données personnelles réelles d'étudiants. Utilisez des données fictives ou anonymisées.

8. Conserver la maîtrise du projet

Vous devez être capables d'expliquer le code généré, de le modifier et de corriger un problème sans dépendre systématiquement de l'IA. Une solution plus simple que vous maîtrisez vaut mieux qu'une solution sophistiquée que vous ne comprenez pas.

### 12.3. Astuces pour obtenir de meilleures réponses

Astuce 1 — Fournir le contexte utile

Précisez le langage, les versions utilisées, l'architecture, le code concerné et le comportement attendu. Si vous demandez de l'aide sur une erreur, fournissez le message complet, les étapes pour la reproduire et le résultat obtenu.

Astuce 2 — Demander une conception avant l'implémentation

Exemple de demande :

« Propose un modèle de données EF Core pour GroupSwitch. Un étudiant peut suivre plusieurs activités relevant de blocs différents, mais ne peut avoir qu'une seule affectation active par activité. Présente les entités, leurs relations, les contraintes d'intégrité et les risques de conception avant de proposer le code. »

Astuce 3 — Faire identifier les cas oubliés

Après avoir conçu une fonctionnalité, demandez :

« Analyse cette solution comme un développeur chargé de la tester. Identifie les cas limites, les erreurs possibles, les problèmes de concurrence et les risques de sécurité. Propose des tests pour chacun de ces problèmes. »

Astuce 4 — Utiliser l'IA pour challenger ses propres choix

Ne demandez pas uniquement une confirmation. Demandez à l'IA de rechercher les défauts de votre proposition, de présenter une alternative et d'expliquer dans quelles circonstances elle serait préférable.

Astuce 5 — Demander des modifications limitées

Lorsque vous disposez déjà d'un code fonctionnel, précisez les changements attendus et demandez de conserver le reste. Les réécritures complètes augmentent le risque de régression et compliquent la vérification.

Astuce 6 — Faire expliquer les compromis

Pour une décision importante, demandez une comparaison adaptée au projet : complexité, sécurité, testabilité, maintenance et temps de développement. Une technologie n'est pas automatiquement meilleure parce qu'elle est plus récente ou plus avancée.

### 12.4. Vérifications particulières pour GroupSwitch

Certains aspects du projet nécessitent une vigilance particulière lorsque l'IA est utilisée :

- **Affectations** : vérifier qu'un changement de groupe ne modifie pas les affectations de l'étudiant dans les autres blocs.
- **Échanges en chaîne** : vérifier l'atomicité de l'opération et le respect des capacités.
- **Horaires** : faire calculer les chevauchements par le code C# à partir des données Hyperplanning, et non par l'IA.
- **Authentification** : vérifier les autorisations côté backend, et pas uniquement dans l'interface.
- **SignalR** : vérifier que les notifications ne sont envoyées qu'aux destinataires autorisés.
- **Exportations** : vérifier que les fichiers CSV et PDF contiennent les données attendues, sans inclure les affectations d'autres blocs par erreur.
- **Webscraping** : vérifier que les données récupérées correspondent réellement à Hyperplanning et que les changements de structure du site sont gérés de manière compréhensible.
- **Intégration IA** : vérifier que les réponses du modèle sont validées et que leur indisponibilité ne bloque pas le fonctionnement classique de l'application.

### 12.5. Transparence et responsabilité

Vous devez pouvoir identifier les principales utilisations de l'IA dans votre travail et expliquer les décisions importantes prises à partir de ses suggestions.

Dans le README ou dans un document complémentaire, fournissez une courte synthèse précisant :

- les outils d'IA utilisés ;
- les principales tâches pour lesquelles ils ont été utilisés ;
- un ou deux exemples significatifs d'aide apportée ;
- les vérifications, corrections ou adaptations que vous avez effectuées ;
- les choix techniques importants que vous avez finalement retenus et pourquoi.

Il n'est pas nécessaire de fournir l'intégralité de vos conversations avec les outils d'IA. L'objectif est de montrer une utilisation réfléchie et responsable.

### 12.6. Évaluation de l'utilisation de l'IA

L'utilisation de l'IA ne constitue pas, à elle seule, un critère de qualité. L'évaluation portera sur votre capacité à :

- utiliser l'IA de manière pertinente pour résoudre des problèmes concrets ;
- vérifier les propositions et corriger les erreurs ;
- produire un code cohérent, sécurisé, testé et maintenable ;
- justifier les choix techniques ;
- expliquer le fonctionnement du code et le modifier en cas de besoin ;
- identifier les limites des réponses générées.

Une solution générée par l'IA mais mal comprise, non testée ou incompatible avec les exigences du projet ne sera pas considérée comme une réalisation de qualité.

Lors de la démonstration, vous pourrez être amenés à expliquer ou à modifier une partie du code, y compris une partie initialement proposée par un outil d'IA.
