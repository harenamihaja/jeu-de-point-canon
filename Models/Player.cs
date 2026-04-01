namespace Jeu_de_point.Models
{
    public class Player
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PlayerNumber { get; set; } // 1 ou 2
        public int Score { get; set; } = 0;
        public int CanonShootsUsed { get; set; } = 0;
    }
}
