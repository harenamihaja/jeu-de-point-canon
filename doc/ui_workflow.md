# Workflow de l'Interface Utilisateur (UI)

Ce document détaille la structure et le fonctionnement de l'interface utilisateur du "Jeu de Points avec Canon", en se basant sur l'analyse des fichiers dans le dossier `UI`.

## Structure Générale

L'interface est construite autour d'une fenêtre principale (`MainForm`) qui orchestre plusieurs composants :

1.  **`MainForm.cs`** : La fenêtre principale. Elle contient la logique de l'interface, gère les événements du moteur de jeu (`GameEngine`) et met à jour l'affichage.
2.  **`Controls/BoardPanel.cs`** : Le composant le plus important. C'est un `Panel` personnalisé qui dessine la grille, les points, les lignes, les canons et gère l'animation des tirs. Il détecte également les clics des joueurs sur le plateau.
3.  **`Controls/CanonControl.cs`** : Un petit panneau qui permet au joueur de choisir la puissance de son tir et de déclencher l'action.
4.  **`Forms/GameSetupForm.cs`** : Une boîte de dialogue modale qui s'affiche au début pour permettre aux joueurs de configurer une nouvelle partie (noms, taille du plateau).
5.  **`Forms/LoadGameForm.cs`** : Une autre boîte de dialogue pour afficher la liste des parties sauvegardées et en charger une.

L'architecture suit un modèle **événementiel** :
*   L'**UI** capture les actions de l'utilisateur (clic, appui sur une touche).
*   Elle transmet ces actions au **`GameEngine`** en appelant ses méthodes (ex: `PlacePointAsync`, `CanonShootAsync`).
*   Le **`GameEngine`** traite la logique du jeu.
*   Une fois la logique traitée, le `GameEngine` déclenche des **événements** (ex: `OnPointPlaced`, `OnGameOver`).
*   La `MainForm` écoute ces événements et met à jour les composants graphiques en conséquence (`UpdateUI`, `_boardPanel.RefreshBoard()`).

---

## Workflow Détaillé

### 1. Démarrage de l'Application

1.  `Program.cs` crée une instance de `AppDbContext` (pour la base de données) et la passe à `MainForm`.
2.  `MainForm` s'initialise et construit son interface via `BuildUI()`. À ce stade, aucun jeu n'est chargé. L'interface est minimale, invitant l'utilisateur à commencer ou charger une partie.

### 2. Démarrer une Nouvelle Partie

1.  L'utilisateur clique sur le bouton **"Nouvelle Partie"**.
2.  `MainForm` crée et affiche une instance de `GameSetupForm`.
3.  L'utilisateur entre les noms des joueurs et choisit la taille du plateau, puis clique sur **"Démarrer"**.
4.  Si les informations sont valides, `GameSetupForm` se ferme avec un `DialogResult.OK`.
5.  `MainForm` récupère les informations et :
    a. Crée une nouvelle instance du `GameEngine`.
    b. S'abonne aux événements du `GameEngine` (`OnPointPlaced`, `OnGameOver`, etc.). Ces abonnements sont cruciaux pour lier la logique du jeu à l'affichage.
    c. Appelle `_engine.StartNewGameAsync(...)` pour initialiser la partie.
    d. Passe l'instance du `GameEngine` au `BoardPanel` (`_boardPanel.SetEngine(_engine)`).
    e. Met à jour toute l'interface avec les informations de la nouvelle partie (`UpdateUI()`).

### 3. Déroulement d'un Tour

L'utilisateur a le choix entre deux modes, sélectionnés via des boutons radio : **"Placer"** ou **"Utiliser le canon"**.

#### Mode "Placer" (par défaut)

1.  L'utilisateur clique sur une intersection de la grille sur le `BoardPanel`.
2.  `BoardPanel` détecte le clic (`OnMouseClick`) et déclenche son événement `OnIntersectionClicked` avec les coordonnées (ligne, colonne).
3.  `MainForm` reçoit cet événement et appelle `_engine.PlacePointAsync(row, col)`.
4.  Le `GameEngine` traite la logique :
    *   Vérifie si le coup est valide.
    *   Met à jour sa grille interne.
    *   Vérifie s'il y a un alignement (`HandleAlignmentsAsync`).
    *   Passe au joueur suivant.
    *   Déclenche l'événement `OnPointPlaced`.
5.  `MainForm` reçoit `OnPointPlaced` et appelle `_boardPanel.RefreshBoard()` et `UpdateUI()` pour rafraîchir l'affichage avec le nouveau point et les informations du joueur suivant.

#### Mode "Utiliser le canon"

1.  L'utilisateur sélectionne le mode "Utiliser le canon". La variable `_canonModeActive` passe à `true`.
2.  Le `CanonControl` devient actif. L'utilisateur peut y choisir une puissance de 1 à 9.
3.  L'utilisateur clique sur le bouton **"🔥 Tirer !"**.
4.  `CanonControl` déclenche son événement `OnCanonFire` avec la puissance choisie.
5.  `MainForm` reçoit cet événement et appelle `_engine.CanonShootAsync(power)`.
6.  Le `GameEngine` :
    a. Calcule la case cible.
    b. Déclenche l'événement `OnCanonFired` pour démarrer l'animation.
7.  `BoardPanel`, qui écoute cet événement, reçoit `OnCanonFired` et lance l'animation du projectile (`HandleCanonFired`).
8.  Pendant ce temps, le `GameEngine` attend la fin de l'animation.
9.  Une fois l'animation terminée dans `BoardPanel`, il exécute un `callback` qui signale au `GameEngine` de continuer.
10. Le `GameEngine` applique les effets du tir (destruction, ranimation...), puis passe au joueur suivant.
11. Il déclenche les événements appropriés (`OnPointDestroyed`, `OnPointPlaced`).
12. `MainForm` reçoit ces événements et met à jour l'affichage.

### 4. Déplacement du Canon

Le déplacement du canon est une action "gratuite" qui ne termine pas le tour.

1.  L'utilisateur déplace sa souris sur les bords supérieur ou inférieur du `BoardPanel`. Le curseur se transforme en main.
2.  Il clique sur la colonne où il souhaite déplacer son canon.
3.  `BoardPanel` (`OnMouseClick`) détecte que le clic a eu lieu dans la zone du canon et déclenche l'événement `OnCanonMoved` avec la nouvelle colonne.
4.  `MainForm` reçoit cet événement et appelle `_engine.MoveCanonAsync(col)`.
5.  Le `GameEngine` met à jour la position du canon en mémoire.
6.  `MainForm` rafraîchit l'affichage pour montrer le canon à sa nouvelle position.

### 5. Fin de Partie

1.  Dans le `GameEngine`, si un alignement est détecté (`HandleAlignmentsAsync`) ou si le plateau est plein, la fonction `EndGameAsync` est appelée.
2.  `EndGameAsync` met à jour le statut du jeu et déclenche l'événement `OnGameOver` avec le numéro du joueur gagnant.
3.  `MainForm` reçoit cet événement, affiche une `MessageBox` avec le résultat de la partie, et met à jour l'UI une dernière fois (par exemple, en désactivant les contrôles de jeu).
