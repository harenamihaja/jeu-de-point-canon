using Npgsql;
using Jeu_de_point.Models;

namespace Jeu_de_point.Data
{
    /// <summary>
    /// Gestion de la connexion PostgreSQL via Npgsql (sans Entity Framework).
    /// </summary>
    public class AppDbContext
    {
        private readonly string _connectionString;

        public AppDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        /// <summary>
        /// Retourne la chaîne de connexion par défaut depuis la config.
        /// </summary>
        public static string GetDefaultConnectionString()
        {
            // Modifiez ces valeurs selon votre configuration PostgreSQL
            return "Host=localhost;Port=5432;Database=canon_game1;Username=postgres;Password=harena;";
        }
    }
}
