namespace Jeu_de_point.Models
{
    public enum CanonSide { Top, Bottom, Left, Right }
    public enum CanonDirection { Horizontal, Vertical, DiagonalAsc, DiagonalDesc }

    public class Canon
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public CanonSide Side { get; set; }
        public int Position { get; set; } // index de la ligne/colonne
    }
}
