using System.Windows.Forms;

namespace Jeu_de_point.UI.Controls
{
    /// <summary>
    /// Panneau de contrôle du canon.
    /// Le joueur choisit uniquement la puissance (1 à N) et tire.
    /// Le déplacement du canon se fait par clic directement sur le plateau.
    /// </summary>
    public class CanonControl : GroupBox
    {
        private NumericUpDown _powerInput = new();
        private Button _fireButton = new();
        private Label _lblPower = new();
        private Label _lblHint = new();

        /// <summary>Déclenché au clic sur "Tirer !", avec la puissance choisie.</summary>
        public event Func<int, Task>? OnCanonFire;

        public CanonControl()
        {
            Text = "Canon";
            Size = new System.Drawing.Size(240, 130);
            BuildUI();
        }

        private void BuildUI()
        {
            _lblHint = new Label
            {
                Text = "Cliquez sur votre bord pour déplacer le canon.",
                Left = 8,
                Top = 22,
                Width = 220,
                Height = 28,
                Font = new System.Drawing.Font("Segoe UI", 8f),
                ForeColor = System.Drawing.Color.DimGray
            };

            _lblPower = new Label
            {
                Text = "Puissance (1 à 9) :",
                Left = 8,
                Top = 58,
                Width = 130,
                Height = 20,
                Font = new System.Drawing.Font("Segoe UI", 9f),
                ForeColor = System.Drawing.Color.Black
            };

            _powerInput = new NumericUpDown
            {
                Left = 152,
                Top = 56,
                Width = 70,
                Minimum = 1,
                Maximum = 9,
                Value = 1
            };

            _fireButton = new Button
            {
                Text = "🔥 Tirer !",
                Left = 30,
                Top = 86,
                Width = 176,
                Height = 34,
                BackColor = System.Drawing.Color.Gray,
                ForeColor = System.Drawing.Color.White,
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold)
            };
            _fireButton.FlatAppearance.BorderSize = 0;
            _fireButton.Click += async (s, e) =>
            {
                if (OnCanonFire != null)
                    await OnCanonFire((int)_powerInput.Value);
            };

            Controls.AddRange(new Control[] { _lblHint, _lblPower, _powerInput, _fireButton });
        }

        public void SetBoardSize(int boardSize)
        {
            // La puissance est toujours entre 1 et 9, indépendamment du plateau
            _lblPower.Text = "Puissance (1 à 9) :";
        }

        public void SetEnabled(bool enabled)
        {
            _fireButton.Enabled = enabled;
            _powerInput.Enabled = enabled;
        }
    }
}