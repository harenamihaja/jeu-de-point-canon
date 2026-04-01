using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Jeu_de_point.GameLogic;
using Jeu_de_point.Models;

namespace Jeu_de_point.UI.Controls
{
    /// <summary>
    /// Plateau de jeu : grille, points, lignes tracées, un canon par joueur
    /// déplaçable par clic sur le bord, animation du projectile.
    /// </summary>
    public class BoardPanel : Panel
    {
        private GameEngine? _engine;
        private readonly int _cellSize = 40;
        private readonly int _marginTop = 70;   // espace bord supérieur (canon J1)
        private readonly int _marginBot = 70;   // espace bord inférieur (canon J2)
        private readonly int _marginSide = 40;   // espace latéral (numéros)

        // ── Couleurs ──────────────────────────────────────────────────────────────
        private readonly Color P1Color = Color.Red;
        private readonly Color P2Color = Color.Green;
        private readonly Color GridColor = Color.DarkGray;
        private readonly Color Line1Color = Color.FromArgb(255, 120, 120);
        private readonly Color Line2Color = Color.FromArgb(120, 255, 120);
        private readonly Color Canon1Color = Color.DarkRed;
        private readonly Color Canon2Color = Color.DarkGreen;
        private readonly Color BallColor = Color.Black;

        // ── Animation projectile ──────────────────────────────────────────────────
        private bool _animating = false;
        private float _ballCol = 0;       // colonne (pixel X)
        private float _ballRowFloat = 0;       // position Y continue
        private int _ballTargetRow = 0;
        private int _ballDirection = 1;        // +1 vers le bas, -1 vers le haut
        private System.Windows.Forms.Timer? _animTimer;
        private Func<Task>? _animCallback;

        // ── Événements ────────────────────────────────────────────────────────────
        /// <summary>Déclenché quand le joueur clique pour déplacer son canon.</summary>
        public event Func<int, Task>? OnCanonMoved;
        /// <summary>Déclenché quand le joueur clique sur une intersection.</summary>
        public event Action<int, int>? OnIntersectionClicked;

        public BoardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        public void SetEngine(GameEngine engine)
        {
            _engine = engine;

            // Brancher l'événement d'animation du moteur
            _engine.OnCanonFired -= HandleCanonFired;
            _engine.OnCanonFired += HandleCanonFired;

            RecalcSize();
            Invalidate();
        }

        private void RecalcSize()
        {
            if (_engine == null) return;
            int n = _engine.State.BoardSize;
            Width = _marginSide * 2 + _cellSize * (n - 1);
            Height = _marginTop + _marginBot + _cellSize * (n - 1);
        }

        // ── Conversion coordonnées ────────────────────────────────────────────────
        private int ColToX(int col) => _marginSide + col * _cellSize;
        private int RowToY(int row) => _marginTop + row * _cellSize;
        private int CanonYTop() => _marginTop / 2;                  // centre du canon J1
        private int CanonYBot() => Height - _marginBot / 2;          // centre du canon J2

        // ─────────────────────────────────────────────────────────────────────────
        //  Dessin
        // ─────────────────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_engine == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int n = _engine.State.BoardSize;

            DrawGrid(g, n);
            DrawCoordinates(g, n);
            DrawTracedLines(g);
            DrawPoints(g, n);
            DrawCanon(g, n);

            if (_animating)
                DrawBall(g);
        }

        private void DrawGrid(Graphics g, int n)
        {
            using var pen = new Pen(GridColor, 1f);
            for (int i = 0; i < n; i++)
            {
                g.DrawLine(pen, ColToX(i), RowToY(0), ColToX(i), RowToY(n - 1));
                g.DrawLine(pen, ColToX(0), RowToY(i), ColToX(n - 1), RowToY(i));
            }
        }

        private void DrawCoordinates(Graphics g, int n)
        {
            using var font = new Font("Segoe UI", 8f);
            using var brush = new SolidBrush(GridColor);
            for (int i = 0; i < n; i++)
            {
                g.DrawString(i.ToString(), font, brush, ColToX(i) - 5, RowToY(0) - 20);
                g.DrawString(i.ToString(), font, brush, ColToX(0) - 26, RowToY(i) - 7);
            }
        }

        private void DrawTracedLines(Graphics g)
        {
            if (_engine == null) return;
            foreach (var line in _engine.State.Lines)
            {
                var player = _engine.Players.FirstOrDefault(p => p.Id == line.PlayerId);
                var color = player?.PlayerNumber == 1 ? Line1Color : Line2Color;
                using var pen = new Pen(color, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen,
                    ColToX(line.StartCol), RowToY(line.StartRow),
                    ColToX(line.EndCol), RowToY(line.EndRow));
            }
        }

        private void DrawPoints(Graphics g, int n)
        {
            if (_engine == null) return;
            int radius = _cellSize / 3;
            for (int row = 0; row < n; row++)
                for (int col = 0; col < n; col++)
                {
                    int cell = _engine.Board[row, col];
                    if (cell == 0) continue;

                    int cx = ColToX(col), cy = RowToY(row);
                    var color = cell == 1 ? P1Color : P2Color;

                    using var fill = new SolidBrush(color);
                    g.FillEllipse(fill, cx - radius, cy - radius, radius * 2, radius * 2);

                    if (_engine.Traced[row, col])
                    {
                        using var wp = new Pen(Color.White, 2f);
                        int ir = radius / 2;
                        g.DrawEllipse(wp, cx - ir, cy - ir, ir * 2, ir * 2);
                    }
                }
        }

        /// <summary>
        /// Dessine un seul canon par joueur à la position actuelle (Canon1Col / Canon2Col).
        /// Le canon est cliquable : clic gauche sur un autre emplacement du bord déplace le canon.
        /// </summary>
        private void DrawCanon(Graphics g, int n)
        {
            if (_engine == null) return;

            int c1 = _engine.Canon1Col;
            int c2 = _engine.Canon2Col;

            // ── J1 en haut ────────────────────────────────────────────────────────
            DrawOneCanon(g,
                cx: ColToX(c1),
                cy: CanonYTop(),
                color: Canon1Color,
                pointDown: true);

            // Nom du joueur
            using var f = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var b1 = new SolidBrush(Canon1Color);
            using var b2 = new SolidBrush(Canon2Color);
            string p1 = _engine.Players.ElementAtOrDefault(0)?.Name ?? "J1";
            string p2 = _engine.Players.ElementAtOrDefault(1)?.Name ?? "J2";
            g.DrawString($"▼ {p1}", f, b1, 4, 4);

            // ── J2 en bas ─────────────────────────────────────────────────────────
            DrawOneCanon(g,
                cx: ColToX(c2),
                cy: CanonYBot(),
                color: Canon2Color,
                pointDown: false);
            g.DrawString($"▲ {p2}", f, b2, 4, Height - 20);

            // Indicateur de colonne cible (tiret sous/sur le canon actif)
            int currentCol = _engine.CurrentPlayer.PlayerNumber == 1 ? c1 : c2;
            using var indPen = new Pen(Color.Black, 2f);
            if (_engine.CurrentPlayer.PlayerNumber == 1)
                g.DrawLine(indPen, ColToX(c1) - 10, CanonYTop() + 26, ColToX(c1) + 10, CanonYTop() + 26);
            else
                g.DrawLine(indPen, ColToX(c2) - 10, CanonYBot() - 26, ColToX(c2) + 10, CanonYBot() - 26);
        }

        private void DrawOneCanon(Graphics g, int cx, int cy, Color color, bool pointDown)
        {
            int bodyW = 16, bodyH = 24, nozzleW = 9, nozzleH = 12, wheelR = 5;

            using var fill = new SolidBrush(color);
            using var dark = new SolidBrush(Color.FromArgb(
                Math.Max(0, color.R - 80),
                Math.Max(0, color.G - 80),
                Math.Max(0, color.B - 80)));
            using var pen = new Pen(Color.FromArgb(60, 0, 0, 0), 1f);

            if (pointDown)
            {
                // Corps
                var body = new Rectangle(cx - bodyW / 2, cy - bodyH, bodyW, bodyH);
                g.FillRectangle(fill, body);
                g.DrawRectangle(pen, body);
                // Bouche (bas)
                var nozzle = new Rectangle(cx - nozzleW / 2, cy - nozzleH, nozzleW, nozzleH);
                g.FillRectangle(dark, nozzle);
                g.DrawRectangle(pen, nozzle);
                // Roues (haut)
                g.FillEllipse(dark, cx - bodyW / 2 - wheelR, cy - bodyH - wheelR, wheelR * 2, wheelR * 2);
                g.FillEllipse(dark, cx + bodyW / 2 - wheelR, cy - bodyH - wheelR, wheelR * 2, wheelR * 2);
            }
            else
            {
                // Corps
                var body = new Rectangle(cx - bodyW / 2, cy, bodyW, bodyH);
                g.FillRectangle(fill, body);
                g.DrawRectangle(pen, body);
                // Bouche (haut)
                var nozzle = new Rectangle(cx - nozzleW / 2, cy, nozzleW, nozzleH);
                g.FillRectangle(dark, nozzle);
                g.DrawRectangle(pen, nozzle);
                // Roues (bas)
                g.FillEllipse(dark, cx - bodyW / 2 - wheelR, cy + bodyH - wheelR, wheelR * 2, wheelR * 2);
                g.FillEllipse(dark, cx + bodyW / 2 - wheelR, cy + bodyH - wheelR, wheelR * 2, wheelR * 2);
            }
        }

        private void DrawBall(Graphics g)
        {
            int ballR = 7;
            float bx = _ballCol;
            float by = _marginTop + _ballRowFloat * _cellSize;

            using var fill = new SolidBrush(BallColor);
            using var glow = new SolidBrush(Color.FromArgb(80, 100, 100, 100));

            // Halo
            g.FillEllipse(glow, bx - ballR - 3, by - ballR - 3, (ballR + 3) * 2, (ballR + 3) * 2);
            // Balle
            g.FillEllipse(fill, bx - ballR, by - ballR, ballR * 2, ballR * 2);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Animation du projectile
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Appelé par GameEngine.OnCanonFired.
        /// Lance une animation de la balle de col, de startRow vers targetRow.
        /// </summary>
        private void HandleCanonFired(int col, int startRow, int targetRow,
                                      int direction, Func<Task> onDone)
        {
            if (InvokeRequired)
            {
                Invoke(() => HandleCanonFired(col, startRow, targetRow, direction, onDone));
                return;
            }

            _animating = true;
            _ballCol = ColToX(col);
            _ballRowFloat = startRow == -1 ? -0.5f : _engine!.State.BoardSize - 0.5f;
            _ballTargetRow = targetRow;
            _ballDirection = direction;
            _animCallback = onDone;

            _animTimer?.Stop();
            _animTimer?.Dispose();

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 fps
            _animTimer.Tick += AnimTick;
            _animTimer.Start();
        }

        private void AnimTick(object? sender, EventArgs e)
        {
            float speed = 0.18f; // cases par tick
            _ballRowFloat += speed * _ballDirection;

            bool arrived = _ballDirection > 0
                ? _ballRowFloat >= _ballTargetRow
                : _ballRowFloat <= _ballTargetRow;

            if (arrived)
            {
                _ballRowFloat = _ballTargetRow;
                Invalidate();

                _animTimer!.Stop();
                _animTimer.Dispose();
                _animTimer = null;

                // Bref flash sur la case cible, puis terminer
                var flashTimer = new System.Windows.Forms.Timer { Interval = 120 };
                flashTimer.Tick += (s, ea) =>
                {
                    flashTimer.Stop();
                    flashTimer.Dispose();
                    _animating = false;
                    Invalidate();
                    var cb = _animCallback; if (cb != null) Task.Run(() => cb());
                };
                flashTimer.Start();
            }
            else
            {
                Invalidate();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Gestion des clics
        // ─────────────────────────────────────────────────────────────────────────

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_engine == null || _animating) return;

            int n = _engine.State.BoardSize;

            // Zone de clic sur le bord supérieur (canon J1)
            if (_engine.CurrentPlayer.PlayerNumber == 1 && e.Y < _marginTop - 5)
            {
                int col = (int)Math.Round((float)(e.X - _marginSide) / _cellSize);
                if (col >= 0 && col < n)
                {
                    _ = OnCanonMoved?.Invoke(col);
                    return;
                }
            }

            // Zone de clic sur le bord inférieur (canon J2)
            if (_engine.CurrentPlayer.PlayerNumber == 2 && e.Y > Height - _marginBot + 5)
            {
                int col = (int)Math.Round((float)(e.X - _marginSide) / _cellSize);
                if (col >= 0 && col < n)
                {
                    _ = OnCanonMoved?.Invoke(col);
                    return;
                }
            }

            // Clic sur le plateau → placer un point
            int pcol = (int)Math.Round((float)(e.X - _marginSide) / _cellSize);
            int prow = (int)Math.Round((float)(e.Y - _marginTop) / _cellSize);

            if (prow >= 0 && prow < n && pcol >= 0 && pcol < n)
                OnIntersectionClicked?.Invoke(prow, pcol);
        }

        // Curseur pour indiquer les zones cliquables
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_engine == null) return;
            int n = _engine.State.BoardSize;
            bool onBorder = (e.Y < _marginTop - 5 && _engine.CurrentPlayer.PlayerNumber == 1) ||
                            (e.Y > Height - _marginBot + 5 && _engine.CurrentPlayer.PlayerNumber == 2);
            Cursor = onBorder ? Cursors.Hand : Cursors.Default;
        }

        public void RefreshBoard()
        {
            RecalcSize();
            Invalidate();
        }
    }
}