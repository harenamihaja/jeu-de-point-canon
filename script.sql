CREATE DATABASE JeuDePoint;

\c JeuDePoint;

CREATE TABLE Joueur (
    id SERIAL PRIMARY KEY,
    nom VARCHAR(50)
);

CREATE TABLE Partie (
    id SERIAL PRIMARY KEY,
    date_debut TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Mouvement (
    id SERIAL PRIMARY KEY,
    joueur_id INT REFERENCES Joueur(id),
    partie_id INT REFERENCES Partie(id),
    action VARCHAR(50),
    points INT,
    tour INT
);