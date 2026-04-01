using Npgsql;
using Jeu_de_point.Data;
using Jeu_de_point.Models;

namespace Jeu_de_point.Repositories
{
    public class MoveRepository
    {
        private readonly AppDbContext _context;

        public MoveRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SavePlacePointAsync(int gameId, int playerId, int moveNumber, int row, int col)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO moves (game_id, player_id, move_number, type, target_row, target_col, played_at)
                        VALUES (@gId, @pId, @num, 'PlacePoint', @row, @col, NOW());";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("num", moveNumber);
            cmd.Parameters.AddWithValue("row", row);
            cmd.Parameters.AddWithValue("col", col);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveCanonShootAsync(int gameId, int playerId, int moveNumber,
            int lineIndex, int shootScale, int? destroyedRow, int? destroyedCol)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO moves (game_id, player_id, move_number, type,
                            line_index, shoot_scale, destroyed_row, destroyed_col, played_at)
                        VALUES (@gId, @pId, @num, 'CanonShoot',
                            @lineIdx, @scale, @dRow, @dCol, NOW());";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("num", moveNumber);
            cmd.Parameters.AddWithValue("lineIdx", lineIndex);
            cmd.Parameters.AddWithValue("scale", shootScale);
            cmd.Parameters.AddWithValue("dRow", (object?)destroyedRow ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dCol", (object?)destroyedCol ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Move>> GetMovesByGameAsync(int gameId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, game_id, player_id, move_number, type,
                               target_row, target_col, line_index, shoot_scale,
                               destroyed_row, destroyed_col, played_at
                        FROM moves WHERE game_id = @gId ORDER BY move_number;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);

            var moves = new List<Move>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                moves.Add(new Move
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    PlayerId = reader.GetInt32(2),
                    MoveNumber = reader.GetInt32(3),
                    Type = Enum.Parse<MoveType>(reader.GetString(4)),
                    TargetRow = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TargetCol = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    LineIndex = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    ShootScale = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    DestroyedRow = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    DestroyedCol = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    PlayedAt = reader.GetDateTime(11)
                });
            }
            return moves;
        }

        public async Task SavePointToDbAsync(int gameId, int playerId, int row, int col)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO points (game_id, player_id, row, col, is_traced, is_dead, placed_at)
                        VALUES (@gId, @pId, @row, @col, FALSE, FALSE, NOW());";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("row", row);
            cmd.Parameters.AddWithValue("col", col);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task KillPointAsync(int gameId, int row, int col)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            // On marque le point comme mort
            var sql = "UPDATE points SET is_dead = TRUE WHERE game_id = @gId AND row = @row AND col = @col AND is_traced = FALSE AND is_dead = FALSE;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("row", row);
            cmd.Parameters.AddWithValue("col", col);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RevivePointAsync(int gameId, int playerId, int row, int col)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            // On ressuscite un de ses points morts (le plus récent par exemple, id max)
            var sql = @"UPDATE points SET is_dead = FALSE, placed_at = NOW() 
                        WHERE id = (
                            SELECT id FROM points 
                            WHERE game_id = @gId AND player_id = @pId AND row = @row AND col = @col AND is_dead = TRUE 
                            ORDER BY id DESC LIMIT 1
                        );";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("row", row);
            cmd.Parameters.AddWithValue("col", col);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkPointsAsTracedAsync(int gameId, List<(int row, int col)> positions)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            foreach (var (row, col) in positions)
            {
                var sql = "UPDATE points SET is_traced = TRUE WHERE game_id = @gId AND row = @row AND col = @col;";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("gId", gameId);
                cmd.Parameters.AddWithValue("row", row);
                cmd.Parameters.AddWithValue("col", col);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task SaveLineAsync(int gameId, int playerId, LineDirection dir,
            int sRow, int sCol, int eRow, int eCol)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO lines (game_id, player_id, direction, start_row, start_col, end_row, end_col, traced_at)
                        VALUES (@gId, @pId, @dir, @sr, @sc, @er, @ec, NOW());";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);
            cmd.Parameters.AddWithValue("pId", playerId);
            cmd.Parameters.AddWithValue("dir", dir.ToString());
            cmd.Parameters.AddWithValue("sr", sRow);
            cmd.Parameters.AddWithValue("sc", sCol);
            cmd.Parameters.AddWithValue("er", eRow);
            cmd.Parameters.AddWithValue("ec", eCol);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Models.Point>> GetPointsByGameAsync(int gameId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, game_id, player_id, row, col, is_traced, placed_at, is_dead FROM points WHERE game_id = @gId;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);

            var points = new List<Models.Point>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                points.Add(new Models.Point
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    PlayerId = reader.GetInt32(2),
                    Row = reader.GetInt32(3),
                    Col = reader.GetInt32(4),
                    IsTraced = reader.GetBoolean(5),
                    PlacedAt = reader.GetDateTime(6),
                    IsDead = reader.GetBoolean(7)
                });
            }
            return points;
        }

        public async Task<List<Line>> GetLinesByGameAsync(int gameId)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, game_id, player_id, direction, start_row, start_col, end_row, end_col, traced_at FROM lines WHERE game_id = @gId;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gId", gameId);

            var lines = new List<Line>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lines.Add(new Line
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    PlayerId = reader.GetInt32(2),
                    Direction = Enum.Parse<LineDirection>(reader.GetString(3)),
                    StartRow = reader.GetInt32(4),
                    StartCol = reader.GetInt32(5),
                    EndRow = reader.GetInt32(6),
                    EndCol = reader.GetInt32(7),
                    TracedAt = reader.GetDateTime(8)
                });
            }
            return lines;
        }
    }
}