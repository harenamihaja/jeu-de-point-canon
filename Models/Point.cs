namespace Jeu_de_point.Models
{
    public class Point
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsTraced { get; set; } = false;
        public bool IsDead { get; set; } = false;
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    }
}
