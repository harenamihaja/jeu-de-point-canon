# Jeu de Points avec Canon — Guide d'installation

## Prérequis

- .NET 9 SDK : https://dotnet.microsoft.com/download
- PostgreSQL 14+ installé et démarré
- Visual Studio 2022 ou VS Code

---

## 1. Configurer PostgreSQL

Créez la base de données :

```sql
CREATE DATABASE canon_game;
```

Puis ouvrez le fichier `Data/AppDbContext.cs` et modifiez la chaîne de connexion :

```csharp
return "Host=localhost;Port=5432;Database=canon_game;Username=postgres;Password=VOTRE_MOT_DE_PASSE;";
```

---

## 2. Installer les dépendances

```bash
dotnet restore
```

---

## 3. Lancer le jeu

```bash
dotnet run
```

Les tables PostgreSQL sont créées automatiquement au premier démarrage.

---

## Structure du projet

```
CanonGame/
├── Models/
│   ├── GameState.cs      — État global de la partie
│   ├── Player.cs         — Joueur (nom, score, canon)
│   ├── Point.cs          — Point placé sur le plateau
│   ├── Canon.cs          — Canon (côté, position)
│   ├── Move.cs           — Historique d'un coup
│   └── Line.cs           — Alignement de 5 tracé
│
├── Data/
│   ├── AppDbContext.cs   — Connexion PostgreSQL (Npgsql)
│   └── DatabaseInit.cs   — Création automatique des tables
│
├── Repositories/
│   ├── GameRepository.cs   — CRUD parties
│   ├── PlayerRepository.cs — CRUD joueurs et canons
│   └── MoveRepository.cs   — Historique coups, points, lignes
│
├── GameLogic/
│   └── GameEngine.cs     — Toute la logique du jeu
│
├── UI/
│   ├── MainForm.cs               — Fenêtre principale
│   ├── Controls/
│   │   ├── BoardPanel.cs         — Plateau dessiné (GDI+)
│   │   └── CanonControl.cs       — Interface du canon
│   └── Forms/
│       └── GameSetupForm.cs      — Formulaire nouvelle partie
│
├── Program.cs            — Point d'entrée
└── CanonGame.csproj      — Projet WinForms .NET 9
```

---

## Règles du jeu rappelées

| Action | Description |
|--------|-------------|
| Placer un point | Cliquer sur une intersection libre |
| Utiliser le canon | Cocher "Utiliser le canon", choisir colonne + échelle |
| Aligner 5 | La ligne est tracée automatiquement (+1 point) |
| Points tracés | Indestructibles par le canon |
| Fin de partie | Plateau plein → vainqueur = plus grand score |

### Comment fonctionne le canon ?

- Joueur 1 : canon en **haut**, tire vers le bas
- Joueur 2 : canon en **bas**, tire vers le haut
- Vous choisissez la **colonne** (0 à N-1) et l'**échelle** (1 à N)
- Échelle 1 = première rangée depuis votre bord
- Seuls les points adverses **non tracés** peuvent être détruits

---

## Base de données PostgreSQL

Les tables créées automatiquement :

| Table | Contenu |
|-------|---------|
| `games` | Parties (taille plateau, statut, joueur courant) |
| `players` | Joueurs (nom, score, coups de canon) |
| `canons` | Canon de chaque joueur (côté, position) |
| `points` | Points placés (position, tracé ou non) |
| `lines` | Alignements de 5 tracés |
| `moves` | Historique complet de tous les coups |

Tout est sauvegardé automatiquement après chaque coup (autosave).
