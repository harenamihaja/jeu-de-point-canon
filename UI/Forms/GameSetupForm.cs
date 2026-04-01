using System.Windows.Forms;

namespace Jeu_de_point.UI.Forms
{
    public class GameSetupForm : Form
    {
        private TextBox _player1NameBox = new();
        private TextBox _player2NameBox = new();
        private NumericUpDown _boardSizeInput = new();
        private Button _startButton = new();
        private Button _cancelButton = new();

        public string Player1Name => _player1NameBox.Text.Trim();
        public string Player2Name => _player2NameBox.Text.Trim();
        public int BoardSize => (int)_boardSizeInput.Value;

        public GameSetupForm()
        {
            Text = "Nouvelle Partie";
            Size = new System.Drawing.Size(340, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = System.Drawing.Color.FromArgb(245, 245, 245);

            BuildUI();
        }

        private void BuildUI()
        {
            var titleLabel = new Label
            {
                Text = "Configuration de la Partie",
                Left = 20, Top = 15, Width = 280, Height = 24,
                Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 80)
            };

            var lbl1 = new Label { Text = "Nom Joueur 1 :", Left = 20, Top = 55, Width = 120, Height = 20 };
            _player1NameBox = new TextBox { Left = 150, Top = 52, Width = 150, Text = "Joueur 1" };

            var lbl2 = new Label { Text = "Nom Joueur 2 :", Left = 20, Top = 90, Width = 120, Height = 20 };
            _player2NameBox = new TextBox { Left = 150, Top = 87, Width = 150, Text = "Joueur 2" };

            var lbl3 = new Label { Text = "Taille du plateau :", Left = 20, Top = 125, Width = 130, Height = 20 };
            _boardSizeInput = new NumericUpDown
            {
                Left = 150, Top = 122, Width = 80,
                Minimum = 5, Maximum = 25, Value = 9
            };
            var lblHint = new Label
            {
                Text = "(ex: 9 = plateau 9×9)",
                Left = 240, Top = 127, Width = 80, Height = 16,
                Font = new System.Drawing.Font("Segoe UI", 7.5f),
                ForeColor = System.Drawing.Color.Gray
            };

            _startButton = new Button
            {
                Text = "Démarrer",
                Left = 60, Top = 170, Width = 100, Height = 34,
                BackColor = System.Drawing.Color.FromArgb(60, 140, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            _startButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(Player1Name) || string.IsNullOrWhiteSpace(Player2Name))
                {
                    MessageBox.Show("Veuillez saisir les noms des deux joueurs.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _cancelButton = new Button
            {
                Text = "Annuler",
                Left = 180, Top = 170, Width = 100, Height = 34,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            _cancelButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                titleLabel, lbl1, _player1NameBox, lbl2, _player2NameBox,
                lbl3, _boardSizeInput, lblHint, _startButton, _cancelButton
            });

            AcceptButton = _startButton;
            CancelButton = _cancelButton;
        }
    }
}