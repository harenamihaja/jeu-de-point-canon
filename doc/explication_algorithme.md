# Explication de l'Algorithme du Jeu de Point

Ce document est basé sur l'analyse du fichier `GameLogic/GameEngine.cs`.

## Fonctionnement Général

Le cœur du jeu est la classe `GameEngine`. Elle gère toute la logique, de la création d'une partie aux actions des joueurs, jusqu'à la fin du jeu.

Voici les concepts clés :

1.  **État du Jeu** : Le jeu est représenté par une grille (`Board`) où chaque case peut être vide (0), occupée par le joueur 1 (1) ou par le joueur 2 (2). Des grilles supplémentaires (`Traced`, `DeadP1`, `DeadP2`) suivent l'état des points (s'ils font partie d'une ligne ou s'ils sont "morts").
2.  **Tours des Joueurs** : Les joueurs jouent l'un après l'autre. Le `State.CurrentPlayerNumber` détermine qui a le droit de jouer.
3.  **Actions par Tour** : À chaque tour, un joueur peut effectuer **une seule** des trois actions principales :
    *   **Placer un point** sur une case vide.
    *   **Tirer avec son canon** pour détruire un point adverse.
    *   **Déplacer son canon** (cette action est spéciale car elle ne termine pas le tour).
4.  **Objectif** : Le but est de créer un **alignement exact de 5 points**. Dès qu'un joueur y parvient, la partie se termine et il est déclaré vainqueur.
5.  **Interaction avec la Base de Données** : Chaque action (placer un point, déplacer le canon, etc.) est enregistrée en base de données via les `Repositories` (`GameRepository`, `PlayerRepository`, `MoveRepository`). Cela permet de sauvegarder et de charger des parties.
6.  **Communication avec l'Interface (UI)** : Le `GameEngine` utilise des `events` (comme `OnPointPlaced`, `OnGameOver`) pour notifier l'interface utilisateur des changements, qui peut alors mettre à jour l'affichage.

---

## Explication Détaillée Étape par Étape

Analyse des fonctions principales de `GameEngine.cs`.

### 1. Initialisation et Démarrage

*   **`GameEngine(GameRepository, PlayerRepository, MoveRepository)` (Constructeur)**
    *   **Rôle** : Initialise le moteur de jeu.
    *   **Étapes** :
        1.  Récupère les "repositories" pour communiquer avec la base de données.
        2.  Crée un état de jeu (`GameState`) et des grilles (`Board`, `Traced`, etc.) vides en attendant le début ou le chargement d'une partie.

*   **`StartNewGameAsync(boardSize, player1Name, player2Name)`**
    *   **Rôle** : Crée une toute nouvelle partie.
    *   **Étapes** :
        1.  Crée une nouvelle partie (`Game`) dans la base de données et récupère son ID.
        2.  Crée les deux joueurs (`Player`) associés à cet ID de partie.
        3.  Crée un canon pour chaque joueur, initialement à la colonne 0.
        4.  Initialise les grilles en mémoire (`Board`, `Traced`, etc.) avec la taille demandée.

*   **`LoadGameAsync(gameId)`**
    *   **Rôle** : Charge une partie existante depuis la base de données.
    *   **Étapes** :
        1.  Récupère les informations de la partie et des joueurs.
        2.  Initialise les grilles en mémoire.
        3.  Récupère tous les points (`Point`) de la partie et remplit les grilles `Board`, `Traced` et `DeadP1`/`DeadP2` en fonction de l'état de chaque point (vivant, mort, tracé).
        4.  Restaure la dernière position connue des canons de chaque joueur.

### 2. Actions du Joueur

*   **`MoveCanonAsync(newCol)`**
    *   **Rôle** : Déplacer le canon du joueur actuel. C'est une action "gratuite" qui ne termine pas le tour.
    *   **Étapes** :
        1.  Vérifie si la nouvelle colonne `newCol` est valide.
        2.  Met à jour la position du canon (`Canon1Col` ou `Canon2Col`) en mémoire.
        3.  Ajoute une tâche à une file d'attente (`_pendingSaves`) pour sauvegarder cette nouvelle position en base de données plus tard.

*   **`PlacePointAsync(row, col)`**
    *   **Rôle** : Action principale où le joueur place un point.
    *   **Étapes** :
        1.  Vérifie si la case est bien vide.
        2.  Met à jour la grille `Board` en mémoire avec le numéro du joueur (`1` ou `2`).
        3.  Sauvegarde le coup et le nouveau point en base de données (via `_pendingSaves`).
        4.  Déclenche l'événement `OnPointPlaced` pour que l'interface graphique dessine le point.
        5.  **Appelle `HandleAlignmentsAsync`**, la fonction la plus importante pour vérifier si ce nouveau point crée un alignement.
        6.  Vérifie si la grille est pleine (`IsBoardFull`) pour déclarer un match nul.
        7.  Passe au joueur suivant (`SwitchPlayerAsync`).

*   **`CanonShootAsync(power)`**
    *   **Rôle** : Action où le joueur tire pour détruire un point.
    *   **Étapes** :
        1.  Détermine la colonne de tir (la position actuelle du canon du joueur).
        2.  Calcule la `targetRow` (la case d'arrivée) en fonction de la `power` et du joueur (le joueur 1 tire du haut vers le bas, le joueur 2 du bas vers le haut).
        3.  Déclenche l'événement `OnCanonFired` pour que l'interface lance une animation de tir.
        4.  Une fois l'animation terminée, la fonction `ApplyShotEffectAsync` est appelée.
        5.  **`ApplyShotEffectAsync(col, targetRow)`** :
            *   Vérifie ce qui se trouve sur la case cible.
            *   Si c'est un point adverse **non tracé**, il est détruit (la case devient vide).
            *   Si c'est un point allié, il est détruit (pénalité).
            *   Si la case est vide ou contient un point déjà tracé, le tir n'a aucun effet.
            *   La destruction est enregistrée en base de données.
        6.  Passe au joueur suivant.

### 3. Logique de Victoire

*   **`HandleAlignmentsAsync(player, row, col, playerNum)`**
    *   **Rôle** : Détecter si un alignement de 5 points est formé après la pose d'un point.
    *   **Étapes** :
        1.  Appelle `CheckForAlignment(row, col, playerNum)` qui va chercher dans 4 directions (horizontal, vertical, et les deux diagonales) à partir du point qui vient d'être posé.
        2.  **`CheckForAlignment`** parcourt chaque direction pour compter le nombre de points consécutifs appartenant au joueur.
        3.  Si un alignement d'**exactement 5 points** est trouvé :
            *   Il est ajouté à une liste de lignes (`linesFound`).
        4.  Pour chaque ligne trouvée :
            *   Les 5 points de la ligne sont marqués comme `Traced` (tracés) en mémoire et en base de données.
            *   L'événement `OnLineTraced` est déclenché pour que l'UI dessine la ligne.
        5.  Si au moins une ligne a été trouvée, la partie est gagnée. La fonction `EndGameAsync` est appelée.

*   **`EndGameAsync(winner)`**
    *   **Rôle** : Terminer la partie.
    *   **Étapes** :
        1.  Met à jour le statut de la partie à `Finished`.
        2.  Enregistre le gagnant en base de données.
        3.  Déclenche l'événement `OnGameOver` avec le numéro du joueur gagnant pour que l'interface affiche l'écran de fin de partie.

---
### `CanonShootAsync(int power)` : Explication Détaillée

Cette fonction est appelée lorsque le joueur choisit de tirer avec son canon. Elle est complexe car elle gère le calcul de la trajectoire, l'animation, et les différentes conséquences possibles du tir (détruire un point, ranimer un point mort, etc.).

Voici le déroulement, étape par étape :

1.  **Vérifications Initiales**
    *   `if (State.Status == GameStatus.Finished) return false;`
        *   On vérifie si la partie est déjà terminée. Si oui, on ne peut plus tirer.
    *   `if (power < 1 || power > 9) return false;`
        *   La puissance de tir doit être comprise entre 1 et 9. Toute autre valeur est invalide.

2.  **Identification des Acteurs**
    *   `var player = CurrentPlayer;`
    *   `var opponent = Players[player.PlayerNumber == 1 ? 1 : 0];`
        *   On identifie qui est le joueur qui tire (`player`) et qui est son adversaire (`opponent`).

3.  **Calcul de la Cible (la partie la plus complexe)**
    *   `int col = player.PlayerNumber == 1 ? Canon1Col : Canon2Col;`
        *   On récupère la colonne actuelle du canon du joueur. C'est dans cette colonne que le tir aura lieu.
    *   `int targetPos = (int)Math.Ceiling((double)(size * power) / 9.0);`
        *   C'est le cœur du calcul. Il traduit la `power` (entre 1 et 9) en une position sur la grille (entre 1 et `size`). Par exemple, pour une grille de taille 19 :
            *   Si `power` = 9, `targetPos` = 19.
            *   Si `power` = 1, `targetPos` = `Ceiling(19/9)` = 3.
    *   `int index = targetPos - 1;`
        *   On convertit cette position "humaine" (de 1 à 19) en un index de tableau (de 0 à 18).
    *   `int targetRow = player.PlayerNumber == 1 ? index : (size - 1 - index);`
        *   On détermine la ligne (`row`) exacte de la cible. C'est ici qu'on prend en compte le sens du tir :
            *   **Joueur 1 (en haut)** : Tire vers le bas. Un petit index correspond à une ligne du haut. `targetRow` est donc `index`.
            *   **Joueur 2 (en bas)** : Tire vers le haut. Un petit index (faible puissance) doit correspondre à une ligne du bas. On inverse donc la logique : `targetRow` est `(size - 1 - index)`.

4.  **Déclenchement de l'Animation**
    *   `if (OnCanonFired != null)`
        *   Le moteur de jeu ne gère pas l'animation lui-même. Il délègue cette tâche à l'interface graphique.
        *   Il déclenche l'événement `OnCanonFired`, en lui envoyant toutes les informations nécessaires pour l'animation (colonne, point de départ, point d'arrivée, direction).
        *   `await tcs.Task;` : Le code attend ici que l'animation soit terminée avant de continuer et d'appliquer les effets du tir.

5.  **Application des Effets du Tir**
    *   Le code vérifie ce qui se trouve sur la case cible (`Board[targetRow, col]`) et s'il y a des "points morts" (`DeadP1`, `DeadP2`) sur cette case.
    *   Plusieurs scénarios sont possibles :
        *   **Tir sur un point adverse** (`isOpponentPoint`) :
            1.  Le point adverse est détruit (`Board[targetRow, col] = 0;`).
            2.  Son "compteur de mort" est incrémenté (`DeadP1` ou `DeadP2`).
            3.  **Si le tireur avait un point mort sur cette même case** (`hasDeadPoint`), ce point est "ranimé" : il prend la place du point adverse détruit. C'est un coup double !
            4.  Si un point est ranimé, on vérifie immédiatement s'il crée un nouvel alignement (`HandleAlignmentsAsync`).
        *   **Tir sur une case vide** (`cell is empty`) :
            1.  **Si le tireur avait un point mort sur cette case**, il est ranimé.
            2.  Là aussi, on vérifie si la ranimation crée un alignement.
            3.  Sinon, le tir est simplement raté.
        *   **Tir sur un point déjà tracé** (`isTraced`) : Le tir n'a aucun effet. Les points tracés sont indestructibles.
        *   **Tir sur un de ses propres points** : Le tir est sans effet (considéré comme une erreur du joueur).

6.  **Sauvegarde et Fin du Tour**
    *   `_pendingSaves.Add(...)` : Toutes les modifications (destruction, ranimation, etc.) sont ajoutées à une file d'attente pour être sauvegardées en base de données.
    *   `await SwitchPlayerAsync();` : Le tour se termine, et c'est maintenant à l'adversaire de jouer.
