namespace Jeu_de_point.Models
{
    public enum GameStatus { InProgress, Finished }

    public class GameState
    {
        public int Id { get; set; }
        public int BoardSize { get; set; } // ex: 9 => plateau 9x9 intersections
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public int CurrentPlayerNumber { get; set; } = 1;
        public int? WinnerPlayerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Player> Players { get; set; } = new();
        public List<Point> Points { get; set; } = new();
        public List<Line> Lines { get; set; } = new();
        public List<Move> Moves { get; set; } = new();
    }
}