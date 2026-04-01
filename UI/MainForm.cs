using System.Windows.Forms;
using Jeu_de_point.Data;
using Jeu_de_point.GameLogic;
using Jeu_de_point.Repositories;
using Jeu_de_point.UI.Controls;
using Jeu_de_point.UI.Forms;
using Jeu_de_point.Models;

namespace Jeu_de_point.UI
{
    public class MainForm : Form
    {
        private GameEngine? _engine;
        private readonly AppDbContext _dbContext;
        private readonly GameRepository _gameRepo;
        private readonly PlayerRepository _playerRepo;
        private readonly MoveRepository _moveRepo;

        // Contrôles UI
        private BoardPanel _boardPanel = new();
        private CanonControl _canonControl = new();
        private Panel _topPanel = new();
        private Label _lblCurrentPlayer = new();
        private Label _lblScore = new();
        private Label _lblMessage = new();
        private Label _lblCanonPos = new();
        private Button _btnNewGame = new();
        private Button _btnLoadGame = new();
        private Button _btnSaveGame = new();
        private RadioButton _rbPlace = new();
        private RadioButton _rbCanon = new();

        private bool _canonModeActive = false;

        public MainForm(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _gameRepo = new GameRepository(dbContext);
            _playerRepo = new PlayerRepository(dbContext);
            _moveRepo = new MoveRepository(dbContext);

            Text = "Jeu de Points avec Canon";
            Size = new System.Drawing.Size(1200, 800);
            MinimumSize = new System.Drawing.Size(1000, 600);
            BackColor = System.Drawing.Color.LightYellow;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUI();
        }

        private void BuildUI()
        {
            // ── Panneau de droite (anciennement supérieur) ──────────────────────────────────
            _topPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240,
                BackColor = System.Drawing.Color.LightGreen,
                Padding = new Padding(15)
            };

            var titleLabel = new Label
            {
                Text = "Jeu de Points\navec Canon",
                ForeColor = System.Drawing.Color.DarkGreen,
                Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            // ── Boutons de jeu ────────────────────────────────────────────
            _btnNewGame = new Button
            {
                Text = "Nouvelle Partie",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = System.Drawing.Color.DarkGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold)
            };
            _btnNewGame.FlatAppearance.BorderSize = 0;
            _btnNewGame.Click += async (s, e) => await StartNewGameAsync();

            _btnLoadGame = new Button
            {
                Text = "Charger Partie",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = System.Drawing.Color.DarkGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold)
            };
            _btnLoadGame.FlatAppearance.BorderSize = 0;
            _btnLoadGame.Click += async (s, e) => await LoadGameRequestAsync();
            
            _btnSaveGame = new Button
            {
                Text = "Sauvegarder",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = System.Drawing.Color.SeaGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
                Enabled = false
            };
            _btnSaveGame.FlatAppearance.BorderSize = 0;
            _btnSaveGame.Click += async (s, e) => await SaveGameRequestAsync();

            // Separateur
            var separator1 = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = System.Drawing.Color.Transparent };

            // ── Infos Joueur ───────────────────────────────────────────────────
            _lblCurrentPlayer = new Label
            {
                Text = "Tour de : -",
                ForeColor = System.Drawing.Color.Black,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            _lblScore = new Label
            {
                Text = "Score : 0 - 0",
                ForeColor = System.Drawing.Color.Black,
                Font = new System.Drawing.Font("Segoe UI", 9f),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            _lblCanonPos = new Label
            {
                Text = "Canon : colonne -",
                ForeColor = System.Drawing.Color.DarkSlateGray,
                Font = new System.Drawing.Font("Segoe UI", 8.5f),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            
            // Separateur
            var separator2 = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = System.Drawing.Color.Transparent };

            // ── Mode de jeu ───────────────────────────────────────────────────────
            var lblMode = new Label
            {
                Text = "Action du tour :",
                ForeColor = System.Drawing.Color.Black,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold)
            };

            _rbPlace = new RadioButton
            {
                Text = "Placer (clic)",
                ForeColor = System.Drawing.Color.Black,
                Dock = DockStyle.Top,
                Height = 24,
                Checked = true,
                Font = new System.Drawing.Font("Segoe UI", 9f)
            };
            _rbPlace.CheckedChanged += (s, e) =>
            {
                if (_rbPlace.Checked) { _canonModeActive = false; UpdateUI(); }
            };

            _rbCanon = new RadioButton
            {
                Text = "Utiliser le canon",
                ForeColor = System.Drawing.Color.Black,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new System.Drawing.Font("Segoe UI", 9f)
            };
            _rbCanon.CheckedChanged += (s, e) =>
            {
                if (_rbCanon.Checked) { _canonModeActive = true; UpdateUI(); }
            };

            // ── CanonControl ──────────────────────────────────────────────────────
            _canonControl = new CanonControl
            {
                Dock = DockStyle.Top,
                ForeColor = System.Drawing.Color.Black,
                BackColor = System.Drawing.Color.Transparent
            };
            _canonControl.OnCanonFire += CanonFireAsync;

            // ── Message ───────────────────────────────────────────────────────────
            _lblMessage = new Label
            {
                Text = "Démarrez une nouvelle partie.",
                ForeColor = System.Drawing.Color.DarkSlateGray,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic),
                Dock = DockStyle.Bottom,
                Height = 80,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            _topPanel.Controls.AddRange(new Control[]
            {
                _lblMessage,
                _canonControl,
                _rbCanon,
                _rbPlace,
                lblMode,
                separator2,
                _lblCanonPos,
                _lblScore,
                _lblCurrentPlayer,
                separator1,
                _btnSaveGame,
                _btnLoadGame,
                _btnNewGame,
                titleLabel
            });

            // ── Zone du plateau (scrollable) ──────────────────────────────────────
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = System.Drawing.Color.LightYellow,
                Padding = new Padding(10)
            };

            _boardPanel = new BoardPanel();
            _boardPanel.OnIntersectionClicked += async (row, col) => await OnBoardClickAsync(row, col);
            _boardPanel.OnCanonMoved += async col => await OnCanonMovedAsync(col);

            scrollPanel.Controls.Add(_boardPanel);
            Controls.Add(scrollPanel);
            Controls.Add(_topPanel);

            UpdateUI();
        }

        // Séparateur horizontal (supprimé car non nécessaire en mode horizontal)
        private void Separator(int left, int top) { }

        // ─────────────────────────────────────────────────────────────────────────
        //  Démarrage d'une nouvelle partie
        // ─────────────────────────────────────────────────────────────────────────

        private async Task StartNewGameAsync()
        {
            using var setup = new GameSetupForm();
            if (setup.ShowDialog(this) != DialogResult.OK) return;

            _engine = new GameEngine(_gameRepo, _playerRepo, _moveRepo);

            _engine.OnMessage += msg => SafeInvoke(() => _lblMessage.Text = msg);
            _engine.OnPointPlaced += (r, c, p) => SafeInvoke(() => { _boardPanel.RefreshBoard(); UpdateUI(); });
            _engine.OnPointDestroyed += (r, c) => SafeInvoke(() => { _boardPanel.RefreshBoard(); UpdateUI(); });
            _engine.OnLineTraced += line => SafeInvoke(() =>
            {
                _engine.State.Lines.Add(line);
                _boardPanel.RefreshBoard();
                UpdateUI();
            });
            _engine.OnGameOver += wNum => SafeInvoke(() =>
            {
                var w = _engine.Players.FirstOrDefault(p => p.PlayerNumber == wNum);
                MessageBox.Show(
                    $"Partie terminée !\nVainqueur : {w?.Name ?? "?"}\nScore final : {w?.Score}",
                    "Fin de partie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateUI();
            });

            await _engine.StartNewGameAsync(setup.BoardSize, setup.Player1Name, setup.Player2Name);

            _boardPanel.SetEngine(_engine);
            _canonControl.SetBoardSize(setup.BoardSize);
            _rbPlace.Checked = true;
            _lblMessage.Text = "Partie démarrée ! Cliquez sur le plateau pour jouer.";
            UpdateUI();
        }

        private async Task LoadGameRequestAsync()
        {
            try
            {
                var games = await _gameRepo.GetAllGamesAsync();
                if (games.Count == 0)
                {
                    MessageBox.Show("Aucune partie sauvegardée trouvée.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var loadForm = new LoadGameForm(games);
                if (loadForm.ShowDialog(this) != DialogResult.OK) return;

                _engine = new GameEngine(_gameRepo, _playerRepo, _moveRepo);

                _engine.OnMessage += msg => SafeInvoke(() => _lblMessage.Text = msg);
                _engine.OnPointPlaced += (r, c, p) => SafeInvoke(() => { _boardPanel.RefreshBoard(); UpdateUI(); });
                _engine.OnPointDestroyed += (r, c) => SafeInvoke(() => { _boardPanel.RefreshBoard(); UpdateUI(); });
                _engine.OnLineTraced += line => SafeInvoke(() =>
                {
                    _engine.State.Lines.Add(line);
                    _boardPanel.RefreshBoard();
                    UpdateUI();
                });
                _engine.OnGameOver += wNum => SafeInvoke(() =>
                {
                    var w = _engine.Players.FirstOrDefault(p => p.PlayerNumber == wNum);
                    MessageBox.Show(
                        $"Partie terminée !\nVainqueur : {w?.Name ?? "?"}\nScore final : {w?.Score}",
                        "Fin de partie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateUI();
                });

                await _engine.LoadGameAsync(loadForm.SelectedGameId);
                _boardPanel.SetEngine(_engine);
                _canonControl.SetBoardSize(_engine.State.BoardSize);
                _rbPlace.Checked = true;
                _lblMessage.Text = $"Partie n°{loadForm.SelectedGameId} chargée avec succès.";
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveGameRequestAsync()
        {
            if (_engine == null) return;
            try
            {
                await _engine.SaveGameAsync();
                MessageBox.Show("Partie sauvegardée avec succès !", "Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _lblMessage.Text = "Partie sauvegardée.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_engine != null && _engine.State.Status == GameStatus.InProgress)
            {
                // Joueur 1 (Flèches)
                if (keyData == Keys.Left)
                {
                    _ = HandleKeyboardCanonMoveAsync(1, -1);
                    return true;
                }
                if (keyData == Keys.Right)
                {
                    _ = HandleKeyboardCanonMoveAsync(1, 1);
                    return true;
                }

                // Joueur 2 (Pavé numérique : 4 = gauche, 6 = droite)
                if (keyData == Keys.NumPad4)
                {
                    _ = HandleKeyboardCanonMoveAsync(2, -1);
                    return true;
                }
                if (keyData == Keys.NumPad6)
                {
                    _ = HandleKeyboardCanonMoveAsync(2, 1);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task HandleKeyboardCanonMoveAsync(int playerNumber, int delta)
        {
            if (_engine == null || _engine.State.Status == GameStatus.Finished) return;

            int currentCol = playerNumber == 1 ? _engine.Canon1Col : _engine.Canon2Col;
            int newCol = currentCol + delta;

            if (newCol >= 0 && newCol < _engine.State.BoardSize)
            {
                await _engine.MovePlayerCanonAsync(playerNumber, newCol);
                SafeInvoke(() =>
                {
                    _boardPanel.RefreshBoard();
                    UpdateUI();
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Handlers d'actions
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Le joueur clique sur une intersection pour poser un point.</summary>
        private async Task OnBoardClickAsync(int row, int col)
        {
            if (_engine == null) return;
            if (_engine.State.Status == GameStatus.Finished) return;
            if (_canonModeActive) return;

            bool ok = await _engine.PlacePointAsync(row, col);
            if (!ok) _lblMessage.Text = "Case déjà occupée.";
            UpdateUI();
        }

        /// <summary>Le joueur clique sur le bord du plateau pour déplacer son canon.</summary>
        private async Task OnCanonMovedAsync(int col)
        {
            if (_engine == null) return;
            if (_engine.State.Status == GameStatus.Finished) return;

            await _engine.MoveCanonAsync(col);
            _boardPanel.RefreshBoard();
            UpdateUI();
        }

        /// <summary>Le joueur appuie sur "Tirer !" avec la puissance choisie.</summary>
        private async Task CanonFireAsync(int power)
        {
            if (_engine == null) return;
            if (_engine.State.Status == GameStatus.Finished) return;
            if (!_canonModeActive) return;

            // Désactiver les contrôles pendant l'animation
            _canonControl.SetEnabled(false);

            bool ok = await _engine.CanonShootAsync(power);
            if (!ok) _lblMessage.Text = "Tir invalide.";

            _rbPlace.Checked = true;
            _boardPanel.RefreshBoard();
            UpdateUI();
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Mise à jour de l'interface
        // ─────────────────────────────────────────────────────────────────────────

        private void UpdateUI()
        {
            if (_engine == null)
            {
                _lblCurrentPlayer.Text = "Tour de : -";
                _lblScore.Text = "Score : - vs -";
                _lblCanonPos.Text = "Canon : -";
                _canonControl.SetEnabled(false);
                return;
            }

            var current = _engine.CurrentPlayer;
            _lblCurrentPlayer.Text = $"Tour de : {current.Name}";

            var p1 = _engine.Players.ElementAtOrDefault(0);
            var p2 = _engine.Players.ElementAtOrDefault(1);
            _lblScore.Text = $"Score :  {p1?.Name} {p1?.Score}  –  {p2?.Score} {p2?.Name}";

            int canonCol = current.PlayerNumber == 1 ? _engine.Canon1Col : _engine.Canon2Col;
            string side = current.PlayerNumber == 1 ? "haut" : "bas";
            _lblCanonPos.Text = $"Canon ({side}) : colonne {canonCol}";

            bool gameOn = _engine.State.Status == GameStatus.InProgress;
            _canonControl.SetEnabled(_canonModeActive && gameOn);
            _rbPlace.Enabled = gameOn;
            _rbCanon.Enabled = gameOn;
            _btnSaveGame.Enabled = true;

            _boardPanel.RefreshBoard();
        }

        // Thread-safe invoke pour les callbacks du moteur
        private void SafeInvoke(Action a)
        {
            if (InvokeRequired) Invoke(a);
            else a();
        }
    }
}