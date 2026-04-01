using Npgsql;
using Jeu_de_point.Data;
using Jeu_de_point.Models;

namespace Jeu_de_point.Repositories
{
    public class PlayerRepository
    {
        private readonly AppDbContext _context;

        public PlayerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreatePlayerAsync(int gameId, string name, int playerNumber)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO players (game_id, name, player_number, score, canon_shoots_used)
                        VALUES (@gameId, @name, @num, 0, 0) RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gameId", gameId);
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("num", playerNumber);

            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<List<Player>> GetPlayersByGameAsync(int gameId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, game_id, name, player_number, score, canon_shoots_used FROM players WHERE game_id = @gameId ORDER BY player_number;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gameId", gameId);

            var players = new List<Player>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                players.Add(new Player
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    PlayerNumber = reader.GetInt32(3),
                    Score = reader.GetInt32(4),
                    CanonShootsUsed = reader.GetInt32(5)
                });
            }
            return players;
        }

        public async Task IncrementScoreAsync(int playerId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "UPDATE players SET score = score + 1 WHERE id = @id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", playerId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task IncrementCanonShootsAsync(int playerId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "UPDATE players SET canon_shoots_used = canon_shoots_used + 1 WHERE id = @id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", playerId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CreateCanonAsync(int playerId, CanonSide side, int position)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "INSERT INTO canons (player_id, side, position) VALUES (@pId, @side, @pos);";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("side", side.ToString());
            cmd.Parameters.AddWithValue("pos", position);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateCanonPositionAsync(int playerId, int newCol)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();
            var sql = "UPDATE canons SET position = @pos WHERE player_id = @pId;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("pos",  newCol);
            cmd.Parameters.AddWithValue("pId",  playerId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Canon?> GetCanonByPlayerAsync(int playerId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, player_id, side, position FROM canons WHERE player_id = @pId LIMIT 1;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("pId", playerId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Canon
            {
                Id = reader.GetInt32(0),
                PlayerId = reader.GetInt32(1),
                Side = Enum.Parse<CanonSide>(reader.GetString(2)),
                Position = reader.GetInt32(3)
            };
        }
    }
}