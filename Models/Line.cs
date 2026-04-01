namespace Jeu_de_point.Models
{
    public enum LineDirection { Horizontal, Vertical, DiagonalAsc, DiagonalDesc }

    public class Line
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }

        public LineDirection Direction { get; set; }

        // Points de début et fin de la ligne
        public int StartRow { get; set; }
        public int StartCol { get; set; }
        public int EndRow { get; set; }
        public int EndCol { get; set; }

        public DateTime TracedAt { get; set; } = DateTime.UtcNow;
    }
}
