using Npgsql;
using Jeu_de_point.Data;

namespace Jeu_de_point.Data
{
    public static class DatabaseInit
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            using var conn = context.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                CREATE TABLE IF NOT EXISTS games (
                    id SERIAL PRIMARY KEY,
                    board_size INTEGER NOT NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'InProgress',
                    current_player_number INTEGER NOT NULL DEFAULT 1,
                    winner_player_id INTEGER,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS players (
                    id SERIAL PRIMARY KEY,
                    game_id INTEGER NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                    name VARCHAR(100) NOT NULL,
                    player_number INTEGER NOT NULL,
                    score INTEGER NOT NULL DEFAULT 0,
                    canon_shoots_used INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS canons (
                    id SERIAL PRIMARY KEY,
                    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
                    side VARCHAR(20) NOT NULL,
                    position INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS points (
                    id SERIAL PRIMARY KEY,
                    game_id INTEGER NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
                    row INTEGER NOT NULL,
                    col INTEGER NOT NULL,
                    is_traced BOOLEAN NOT NULL DEFAULT FALSE,
                    is_dead BOOLEAN NOT NULL DEFAULT FALSE,
                    placed_at TIMESTAMP NOT NULL DEFAULT NOW()
                );

                ALTER TABLE points ADD COLUMN IF NOT EXISTS is_dead BOOLEAN NOT NULL DEFAULT FALSE;

                CREATE TABLE IF NOT EXISTS lines (
                    id SERIAL PRIMARY KEY,
                    game_id INTEGER NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
                    direction VARCHAR(20) NOT NULL,
                    start_row INTEGER NOT NULL,
                    start_col INTEGER NOT NULL,
                    end_row INTEGER NOT NULL,
                    end_col INTEGER NOT NULL,
                    traced_at TIMESTAMP NOT NULL DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS moves (
                    id SERIAL PRIMARY KEY,
                    game_id INTEGER NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
                    move_number INTEGER NOT NULL,
                    type VARCHAR(20) NOT NULL,
                    target_row INTEGER,
                    target_col INTEGER,
                    line_index INTEGER,
                    shoot_scale INTEGER,
                    destroyed_row INTEGER,
                    destroyed_col INTEGER,
                    played_at TIMESTAMP NOT NULL DEFAULT NOW()
                );
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}