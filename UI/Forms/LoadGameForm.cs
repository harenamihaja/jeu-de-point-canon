using System.Windows.Forms;
using Jeu_de_point.Models;

namespace Jeu_de_point.UI.Forms
{
    public class LoadGameForm : Form
    {
        public int SelectedGameId { get; private set; } = -1;

        private ListBox _listBox = new();
        private Button _btnLoad = new();
        private Button _btnCancel = new();

        public LoadGameForm(List<GameState> games)
        {
            Text = "Charger une partie";
            Size = new System.Drawing.Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = System.Drawing.Color.White;

            var lbl = new Label
            {
                Text = "Sélectionnez une partie à reprendre :",
                Left = 20,
                Top = 20,
                Width = 300,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular),
                ForeColor = System.Drawing.Color.Black
            };

            _listBox = new ListBox
            {
                Left = 20,
                Top = 50,
                Width = 340,
                Height = 180,
                Font = new System.Drawing.Font("Segoe UI", 9f),
                DisplayMember = "DisplayString" // We need a wrapper class maybe
            };

            foreach (var g in games)
            {
                string status = g.Status == GameStatus.InProgress ? "En cours" : "Terminée";
                _listBox.Items.Add(new GameItem { Game = g, DisplayText = $"Partie n°{g.Id} - {g.CreatedAt:g} [{status}]" });
            }

            _btnLoad = new Button
            {
                Text = "Charger",
                Left = 20,
                Top = 250,
                Width = 160,
                Height = 40,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold)
            };
            _btnLoad.Click += (s, e) =>
            {
                if (_listBox.SelectedItem is GameItem gi)
                {
                    SelectedGameId = gi.Game.Id;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Veuillez sélectionner une partie.", "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            _btnCancel = new Button
            {
                Text = "Annuler",
                Left = 200,
                Top = 250,
                Width = 160,
                Height = 40,
                BackColor = System.Drawing.Color.LightGray,
                ForeColor = System.Drawing.Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold)
            };
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[] { lbl, _listBox, _btnLoad, _btnCancel });
        }

        private class GameItem
        {
            public GameState Game { get; set; } = null!;
            public string DisplayText { get; set; } = "";
            public override string ToString() => DisplayText;
        }
    }
}
