using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace AIStudyPlannerApp.Backend
{
    // repesents 1 msg saved in chat history
    public sealed class ChatMessageRecord
    {
        public long ChatMessageId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    // loads and saves chat msgs in the db
    public class ChatHistoryService
    {
        private readonly DatabaseService _databaseService = new();

        public async Task<List<ChatMessageRecord>> LoadAsync(int userId)
        {
            var messages = new List<ChatMessageRecord>();

            // selects requested users messages from oldest to newest
            const string sql = @"
                SELECT ChatMessageId, UserId, Role, Content, CreatedAt
                FROM chatmessages
                WHERE UserId = @UserId
                ORDER BY ChatMessageId ASC;";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                messages.Add(new ChatMessageRecord
                {
                    ChatMessageId = reader.GetInt64(0),
                    UserId = reader.GetInt32(1),
                    Role = reader.GetString(2),
                    Content = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }

            return messages;
        }

        // saves a user or assistant message to the chat history
        public async Task SaveAsync(
            int userId, string role, string content)
        {
            // allows only the two roles supported by the chat history
            if (role != "user" && role != "assistant")
            {
                throw new ArgumentException(
                    "Role must be user or assistant.",
                    nameof(role));
            }

            if (string.IsNullOrWhiteSpace(content))
                return;

            // inserts the message into the chatmessages table
            const string sql = @"
                INSERT INTO chatmessages (UserId, Role, Content)
                VALUES (@UserId, @Role, @Content);";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            // supplies values using SQL params
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Role", role);
            command.Parameters.AddWithValue("@Content", content);

            await command.ExecuteNonQueryAsync();
        }
    }
}