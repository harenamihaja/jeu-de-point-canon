namespace Jeu_de_point.Models
{
    public enum MoveType { PlacePoint, CanonShoot }

    public class Move
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int MoveNumber { get; set; }
        public MoveType Type { get; set; }

        // Pour PlacePoint
        public int? TargetRow { get; set; }
        public int? TargetCol { get; set; }

        // Pour CanonShoot
        public int? LineIndex { get; set; }
        public int? ShootScale { get; set; } // valeur choisie par le joueur (1 à boardSize)
        public int? DestroyedRow { get; set; }
        public int? DestroyedCol { get; set; }

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }
}
