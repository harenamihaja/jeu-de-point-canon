using Npgsql;
using Jeu_de_point.Data;
using Jeu_de_point.Models;

namespace Jeu_de_point.Repositories
{
    public class GameRepository
    {
        private readonly AppDbContext _context;

        public GameRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateGameAsync(int boardSize)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO games (board_size, status, current_player_number, created_at, updated_at)
                        VALUES (@boardSize, 'InProgress', 1, NOW(), NOW())
                        RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("boardSize", boardSize);
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<GameState?> GetGameAsync(int gameId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT * FROM games WHERE id = @id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", gameId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new GameState
            {
                Id = reader.GetInt32(0),
                BoardSize = reader.GetInt32(1),
                Status = Enum.Parse<GameStatus>(reader.GetString(2)),
                CurrentPlayerNumber = reader.GetInt32(3),
                WinnerPlayerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5),
                UpdatedAt = reader.GetDateTime(6)
            };
        }

        public async Task UpdateCurrentPlayerAsync(int gameId, int playerNumber)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE games SET current_player_number = @p, updated_at = NOW() WHERE id = @id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("p", playerNumber);
            cmd.Parameters.AddWithValue("id", gameId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SetWinnerAsync(int gameId, int winnerPlayerId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE games SET winner_player_id = @w, status = 'Finished', updated_at = NOW() WHERE id = @id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("w", winnerPlayerId);
            cmd.Parameters.AddWithValue("id", gameId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<GameState>> GetAllGamesAsync()
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, board_size, status, current_player_number, winner_player_id, created_at, updated_at FROM games ORDER BY created_at DESC;";
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var games = new List<GameState>();
            while (await reader.ReadAsync())
            {
                games.Add(new GameState
                {
                    Id = reader.GetInt32(0),
                    BoardSize = reader.GetInt32(1),
                    Status = Enum.Parse<GameStatus>(reader.GetString(2)),
                    CurrentPlayerNumber = reader.GetInt32(3),
                    WinnerPlayerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    CreatedAt = reader.GetDateTime(5),
                    UpdatedAt = reader.GetDateTime(6)
                });
            }
            return games;
        }
    }
}