
using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace AIStudyPlannerApp.Backend
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            string password = Environment.GetEnvironmentVariable(
                "AI_STUDY_PLANNER_DB_PASSWORD"
            ) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Database password is not configured. " +
                    "Please set the AI_STUDY_PLANNER_DB_PASSWORD environment variable."
                );
            }

            var builder = new MySqlConnectionStringBuilder
            {
                Server = "localhost",
                Port = 3306,
                Database = "ai_study_planner",
                UserID = "root",
                Password = password,
                CharacterSet = "utf8mb4",
                ConnectionTimeout = 5
            };

            _connectionString = builder.ConnectionString;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<bool> TestConnectionAsync()
        {
            await using var connection = CreateConnection();

            await connection.OpenAsync();

            await using var command = new MySqlCommand(
                "SELECT 1;",
                connection
            );

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) == 1;
        }
    }
}

