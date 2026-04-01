using Jeu_de_point.Models;
using Jeu_de_point.Repositories;

namespace Jeu_de_point.GameLogic
{
    /// <summary>
    /// Logique du jeu : placement de points, déplacement du canon,
    /// tir par puissance, détection d'alignements exacts de 5, autosave.
    /// </summary>
    public class GameEngine
    {
        private readonly GameRepository _gameRepo;
        private readonly PlayerRepository _playerRepo;
        private readonly MoveRepository _moveRepo;

        public GameState State { get; private set; }
        public List<Player> Players { get; private set; } = new();

        // Grille : 0 = vide, 1 = joueur 1, 2 = joueur 2
        public int[,] Board { get; private set; }
        // true = point tracé (indestructible, mais réutilisable pour un nouvel alignement)
        public bool[,] Traced { get; private set; }

        // Compteur de points morts par cellule pour ranimation
        public int[,] DeadP1 { get; private set; }
        public int[,] DeadP2 { get; private set; }

        public int MoveCount { get; private set; } = 0;

        // Position courante du canon de chaque joueur (colonne)
        // Canon J1 : bord supérieur, tire vers le bas
        // Canon J2 : bord inférieur, tire vers le haut
        public int Canon1Col { get; private set; } = 0;
        public int Canon2Col { get; private set; } = 0;

        // File d'attente pour la sauvegarde manuelle
        private List<Func<Task>> _pendingSaves = new();

        // Événements pour l'UI
        public event Action<string>? OnMessage;
        public event Action<int, int, int>? OnPointPlaced;    // (row, col, playerNum)
        public event Action<int, int>? OnPointDestroyed; // (row, col)
        public event Action<Line>? OnLineTraced;
        public event Action<int>? OnGameOver;       // (winnerPlayerNumber)

        // Événement pour lancer l'animation du projectile dans le BoardPanel
        // Paramètres : (colonne, rowDépart, rowArrivée, direction +1/-1, callbackFinAnimation)
        public event Action<int, int, int, int, Func<Task>>? OnCanonFired;

        public GameEngine(GameRepository gameRepo, PlayerRepository playerRepo, MoveRepository moveRepo)
        {
            _gameRepo = gameRepo;
            _playerRepo = playerRepo;
            _moveRepo = moveRepo;
            State = new GameState();
            Board = new int[0, 0];
            Traced = new bool[0, 0];
            DeadP1 = new int[0, 0];
            DeadP2 = new int[0, 0];
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Initialisation / chargement
        // ─────────────────────────────────────────────────────────────────────────

        public async Task StartNewGameAsync(int boardSize, string player1Name, string player2Name)
        {
            int gameId = await _gameRepo.CreateGameAsync(boardSize);

            int p1Id = await _playerRepo.CreatePlayerAsync(gameId, player1Name, 1);
            int p2Id = await _playerRepo.CreatePlayerAsync(gameId, player2Name, 2);

            // Un seul canon par joueur, colonne 0 au départ
            await _playerRepo.CreateCanonAsync(p1Id, CanonSide.Top, 0);
            await _playerRepo.CreateCanonAsync(p2Id, CanonSide.Bottom, 0);

            State = (await _gameRepo.GetGameAsync(gameId))!;
            Players = await _playerRepo.GetPlayersByGameAsync(gameId);

            Canon1Col = 0;
            Canon2Col = 0;

            Board = new int[boardSize, boardSize];
            Traced = new bool[boardSize, boardSize];
            DeadP1 = new int[boardSize, boardSize];
            DeadP2 = new int[boardSize, boardSize];
        }

        public async Task LoadGameAsync(int gameId)
        {
            State = (await _gameRepo.GetGameAsync(gameId))!;
            Players = await _playerRepo.GetPlayersByGameAsync(gameId);

            int size = State.BoardSize;
            Board = new int[size, size];
            Traced = new bool[size, size];
            DeadP1 = new int[size, size];
            DeadP2 = new int[size, size];

            var points = await _moveRepo.GetPointsByGameAsync(gameId);
            foreach (var p in points)
            {
                int playerNum = p.PlayerId == Players[0].Id ? 1 : 2;
                if (p.IsDead)
                {
                    if (playerNum == 1) DeadP1[p.Row, p.Col]++;
                    else DeadP2[p.Row, p.Col]++;
                }
                else
                {
                    Board[p.Row, p.Col] = playerNum;
                    if (p.IsTraced) Traced[p.Row, p.Col] = true;
                }
            }

            var moves = await _moveRepo.GetMovesByGameAsync(gameId);
            MoveCount = moves.Count;

            // Restaurer la position des canons
            var c1 = await _playerRepo.GetCanonByPlayerAsync(Players[0].Id);
            var c2 = await _playerRepo.GetCanonByPlayerAsync(Players[1].Id);
            Canon1Col = c1?.Position ?? 0;
            Canon2Col = c2?.Position ?? 0;
        }

        public Player CurrentPlayer => Players[State.CurrentPlayerNumber - 1];

        // ─────────────────────────────────────────────────────────────────────────
        //  Action 1 : Déplacer le canon (sans consommer le tour)
        // ─────────────────────────────────────────────────────────────────────────

        public async Task MoveCanonAsync(int newCol)
        {
            await MovePlayerCanonAsync(CurrentPlayer.PlayerNumber, newCol);
        }

        public async Task MovePlayerCanonAsync(int playerNumber, int newCol)
        {
            int size = State.BoardSize;
            if (newCol < 0 || newCol >= size) return;

            var player = Players.FirstOrDefault(p => p.PlayerNumber == playerNumber);
            if (player == null) return;

            if (playerNumber == 1) Canon1Col = newCol;
            else Canon2Col = newCol;

            int pId = player.Id;
            _pendingSaves.Add(async () => await _playerRepo.UpdateCanonPositionAsync(pId, newCol));
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Action 2 : Placer un point
        // ─────────────────────────────────────────────────────────────────────────

        public async Task<bool> PlacePointAsync(int row, int col)
        {
            if (State.Status == GameStatus.Finished) return false;
            if (Board[row, col] != 0) return false;

            var player = CurrentPlayer;
            int playerNum = player.PlayerNumber;

            Board[row, col] = playerNum;
            MoveCount++;

            int gId = State.Id;
            int pId = player.Id;
            int mCount = MoveCount;
            int r = row;
            int c = col;

            _pendingSaves.Add(async () =>
            {
                await _moveRepo.SavePlacePointAsync(gId, pId, mCount, r, c);
                await _moveRepo.SavePointToDbAsync(gId, pId, r, c);
            });

            OnPointPlaced?.Invoke(row, col, playerNum);

            await HandleAlignmentsAsync(player, row, col, playerNum);

            if (IsBoardFull()) { await EndGameAsync(); return true; }

            await SwitchPlayerAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Action 3 : Tirer avec le canon (puissance seule)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tire dans la colonne courante du joueur.
        /// Puissance 1 = case la plus proche du canon, N = case la plus éloignée.
        /// J1 (haut) : puissance 1 => row 0, puissance N => row N-1
        /// J2 (bas)  : puissance 1 => row N-1, puissance N => row 0
        /// Déclenche l'animation puis applique l'effet.
        /// </summary>
        public async Task<bool> CanonShootAsync(int power)
        {
            if (State.Status == GameStatus.Finished) return false;

            int size = State.BoardSize;
            if (power < 1 || power > 9) return false;

            var player = CurrentPlayer;
            var opponent = Players[player.PlayerNumber == 1 ? 1 : 0];

            int col = player.PlayerNumber == 1 ? Canon1Col : Canon2Col;

            // ✅ 1. Calcul en position "humaine" (1 → size)
            int targetPos = (int)Math.Ceiling((double)(size * power) / 9.0);
            targetPos = Math.Clamp(targetPos, 1, size);

            // ✅ 2. Conversion en index (0 → size-1)
            int index = targetPos - 1;

            // ✅ 3. Adapter selon le joueur (haut → bas ou bas → haut)
            int targetRow = player.PlayerNumber == 1
                ? index
                : (size - 1 - index);

            int startRow = player.PlayerNumber == 1 ? -1 : size;
            int direction = player.PlayerNumber == 1 ? 1 : -1;

            // Animation
            if (OnCanonFired != null)
            {
                var tcs = new TaskCompletionSource();
                OnCanonFired.Invoke(col, startRow, targetRow, direction, () =>
                {
                    tcs.SetResult();
                    return Task.CompletedTask;
                });
                await tcs.Task;
            }

            // Effet du tir
            MoveCount++;

            int gId = State.Id;
            int pId = player.Id;
            int mCount = MoveCount;
            int tRow = targetRow;
            int tCol = col;
            int pwr = power;

            _pendingSaves.Add(async () => await _playerRepo.IncrementCanonShootsAsync(pId));

            int cellValue = Board[targetRow, col]; 
            bool isOpponentPoint = cellValue == opponent.PlayerNumber;
            bool isTraced = Traced[targetRow, col];

            bool hasDeadPoint = player.PlayerNumber == 1 ? DeadP1[targetRow, col] > 0 : DeadP2[targetRow, col] > 0;

            if (isTraced)
            {
                _pendingSaves.Add(async () => await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, null, null));
                OnMessage?.Invoke("Point tracé – indestructible !");
            }
            else if (cellValue == player.PlayerNumber)
            {
                _pendingSaves.Add(async () => await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, null, null));
                OnMessage?.Invoke("Ce point vous appartient déjà !");
            }
            else if (isOpponentPoint)
            {
                if (opponent.PlayerNumber == 1) DeadP1[targetRow, col]++;
                else DeadP2[targetRow, col]++;
                Board[targetRow, col] = 0;
                OnPointDestroyed?.Invoke(targetRow, col);

                if (hasDeadPoint)
                {
                    if (player.PlayerNumber == 1) DeadP1[targetRow, col]--;
                    else DeadP2[targetRow, col]--;
                    Board[targetRow, col] = player.PlayerNumber;

                    _pendingSaves.Add(async () =>
                    {
                        await _moveRepo.KillPointAsync(gId, tRow, tCol);
                        await _moveRepo.RevivePointAsync(gId, pId, tRow, tCol);
                        await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, tRow, tCol);
                    });

                    OnPointPlaced?.Invoke(targetRow, col, player.PlayerNumber);
                    OnMessage?.Invoke($"{player.Name} détruit le point adverse et ranime le sien en ({targetRow},{col}) !");
                    await HandleAlignmentsAsync(player, targetRow, col, player.PlayerNumber);
                }
                else
                {
                    _pendingSaves.Add(async () =>
                    {
                        await _moveRepo.KillPointAsync(gId, tRow, tCol);
                        await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, tRow, tCol);
                    });
                    OnMessage?.Invoke($"{player.Name} détruit le point adverse en ({targetRow},{col}) !");
                }
            }
            else // cell is empty (0)
            {
                if (hasDeadPoint)
                {
                    if (player.PlayerNumber == 1) DeadP1[targetRow, col]--;
                    else DeadP2[targetRow, col]--;
                    Board[targetRow, col] = player.PlayerNumber;

                    _pendingSaves.Add(async () =>
                    {
                        await _moveRepo.RevivePointAsync(gId, pId, tRow, tCol);
                        await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, tRow, tCol);
                    });

                    OnPointPlaced?.Invoke(targetRow, col, player.PlayerNumber);
                    OnMessage?.Invoke($"{player.Name} ranime son point mort en ({targetRow},{col}) !");
                    await HandleAlignmentsAsync(player, targetRow, col, player.PlayerNumber);
                }
                else
                {
                    _pendingSaves.Add(async () => await _moveRepo.SaveCanonShootAsync(gId, pId, mCount, tCol, pwr, null, null));
                    OnMessage?.Invoke("Case vide – tir raté !");
                }
            }

            await SwitchPlayerAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Détection alignements exacts de 5
        // ─────────────────────────────────────────────────────────────────────────

        private async Task HandleAlignmentsAsync(Player player, int row, int col, int playerNum)
        {
            var newLines = CheckAlignment(row, col, playerNum);

            int gId = State.Id;
            int pId = player.Id;

            foreach (var line in newLines)
            {
                player.Score++;

                var positions = GetLinePositions(line);
                foreach (var pos in positions) Traced[pos.row, pos.col] = true;

                _pendingSaves.Add(async () =>
                {
                    await _playerRepo.IncrementScoreAsync(pId);
                    await _moveRepo.SaveLineAsync(gId, pId, line.Direction,
                        line.StartRow, line.StartCol, line.EndRow, line.EndCol);
                    await _moveRepo.MarkPointsAsTracedAsync(gId, positions);
                });

                OnLineTraced?.Invoke(line);
                OnMessage?.Invoke($"{player.Name} marque un point !");
            }
        }

        private List<Line> CheckAlignment(int row, int col, int playerNum)
        {
            var foundLines = new List<Line>();
            int size = State.BoardSize;

            var directions = new[]
            {
                (LineDirection.Horizontal,   0,  1),
                (LineDirection.Vertical,     1,  0),
                (LineDirection.DiagonalAsc,  1,  1),
                (LineDirection.DiagonalDesc, 1, -1)
            };

            foreach (var (dir, dr, dc) in directions)
            {
                // Remonter jusqu'au début de la séquence contiguë
                int r1 = row, c1 = col;
                while (r1 - dr >= 0 && r1 - dr < size &&
                       c1 - dc >= 0 && c1 - dc < size &&
                       Board[r1 - dr, c1 - dc] == playerNum)
                { r1 -= dr; c1 -= dc; }

                // Collecter la séquence complète
                var run = new List<(int row, int col)>();
                int r = r1, c = c1;
                while (r >= 0 && r < size && c >= 0 && c < size && Board[r, c] == playerNum)
                { run.Add((r, c)); r += dr; c += dc; }

                // Fenêtres de exactement 5 (aucun 6e voisin de chaque côté)
                for (int start = 0; start <= run.Count - 5; start++)
                {
                    var five = run.Skip(start).Take(5).ToList();

                    bool leftOk = start == 0 ||
                                   Board[five[0].row - dr, five[0].col - dc] != playerNum;
                    bool rightOk = (start + 5) >= run.Count ||
                                   Board[five[4].row + dr, five[4].col + dc] != playerNum;

                    if (!leftOk || !rightOk) continue;

                    if (foundLines.Any(l => l.StartRow == five[0].row &&
                                           l.StartCol == five[0].col &&
                                           l.Direction == dir))
                        continue;

                    foundLines.Add(new Line
                    {
                        GameId = State.Id,
                        PlayerId = CurrentPlayer.Id,
                        Direction = dir,
                        StartRow = five[0].row,
                        StartCol = five[0].col,
                        EndRow = five[4].row,
                        EndCol = five[4].col
                    });
                }
            }
            return foundLines;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Utilitaires
        // ─────────────────────────────────────────────────────────────────────────

        private List<(int row, int col)> GetLinePositions(Line line)
        {
            int dr = line.EndRow == line.StartRow ? 0 : (line.EndRow > line.StartRow ? 1 : -1);
            int dc = line.EndCol == line.StartCol ? 0 : (line.EndCol > line.StartCol ? 1 : -1);
            var list = new List<(int, int)>();
            int r = line.StartRow, c = line.StartCol;
            while (true)
            {
                list.Add((r, c));
                if (r == line.EndRow && c == line.EndCol) break;
                r += dr; c += dc;
                if (list.Count > 100) break;
            }
            return list;
        }

        private bool IsBoardFull()
        {
            for (int r = 0; r < State.BoardSize; r++)
                for (int c = 0; c < State.BoardSize; c++)
                    if (Board[r, c] == 0) return false;
            return true;
        }

        private async Task EndGameAsync()
        {
            State.Status = GameStatus.Finished;
            var winner = Players.OrderByDescending(p => p.Score).First();
            State.WinnerPlayerId = winner.Id;

            int gId = State.Id;
            int wId = winner.Id;
            _pendingSaves.Add(async () => await _gameRepo.SetWinnerAsync(gId, wId));

            OnGameOver?.Invoke(winner.PlayerNumber);
        }

        private async Task SwitchPlayerAsync()
        {
            State.CurrentPlayerNumber = State.CurrentPlayerNumber == 1 ? 2 : 1;

            int gId = State.Id;
            int pNum = State.CurrentPlayerNumber;
            _pendingSaves.Add(async () => await _gameRepo.UpdateCurrentPlayerAsync(gId, pNum));
        }

        public async Task SaveGameAsync()
        {
            foreach (var saveAction in _pendingSaves)
            {
                await saveAction();
            }
            _pendingSaves.Clear();
        }
    }
}