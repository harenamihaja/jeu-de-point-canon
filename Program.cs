using System;
using System.Windows.Forms;
using Jeu_de_point.Data;
using Jeu_de_point.UI;

namespace Jeu_de_point
{
    internal static class Program
    {
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialisation de la base de données
            var connectionString = AppDbContext.GetDefaultConnectionString();
            var dbContext = new AppDbContext(connectionString);

            try
            {
                await DatabaseInit.InitializeAsync(dbContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible de se connecter à PostgreSQL.\n\n{ex.Message}\n\n" +
                    "Vérifiez votre connexion dans Data/AppDbContext.cs",
                    "Erreur base de données",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm(dbContext));
        }
    }
}