using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace AIStudyPlannerApp.Backend
{
    // store the data for one study task retrieved from the db
    public sealed class StudyTaskRecord
    {
        public int StudyTaskId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public DateTime StudyDate { get; set; }
        public decimal Hours { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class StudyTaskService
    {
        private readonly DatabaseService _databaseService;

        public StudyTaskService()
        {
            _databaseService = new DatabaseService();
        }

        public async Task<List<StudyTaskRecord>> GetTasksAsync(
            int userId)
        {
            var tasks = new List<StudyTaskRecord>();

            const string sql = @"
                SELECT StudyTaskId, UserId, Title,
                       StudyDate, Hours, IsCompleted
                FROM studytasks
                WHERE UserId = @UserId
                ORDER BY StudyDate, StudyTaskId;";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            // convert each db row into a StudyTaskRecord object
            while (await reader.ReadAsync())
            {
                tasks.Add(new StudyTaskRecord
                {
                    StudyTaskId = reader.GetInt32(
                        reader.GetOrdinal("StudyTaskId")),

                    UserId = reader.GetInt32(
                        reader.GetOrdinal("UserId")),

                    Title = reader.GetString(
                        reader.GetOrdinal("Title")),

                    StudyDate = reader.GetDateTime(
                        reader.GetOrdinal("StudyDate")),

                    Hours = Convert.ToDecimal(
                        reader["Hours"]),

                    IsCompleted = Convert.ToBoolean(
                        reader["IsCompleted"])
                });
            }

            return tasks;
        }

        public async Task<int> AddTaskAsync(
            int userId,
            string title,
            DateTime studyDate,
            decimal hours)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Task title cannot be empty.",
                    nameof(title));
            }

            // keep the planned duration within the supported range
            if (hours < 0.5m || hours > 24m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hours),
                    "Study hours must be between 0.5 and 24.");
            }

            const string sql = @"
                INSERT INTO studytasks
                    (UserId, Title, StudyDate, Hours, IsCompleted)
                VALUES
                    (@UserId, @Title, @StudyDate, @Hours, FALSE);

                SELECT LAST_INSERT_ID();";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Title", title.Trim());

            command.Parameters.Add(
                "@StudyDate",
                MySqlDbType.Date
            ).Value = studyDate.Date;

            command.Parameters.Add(
                "@Hours",
                MySqlDbType.Decimal
            ).Value = hours;

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateCompletionAsync(
            int userId,
            int studyTaskId,
            bool isCompleted)
        {
            const string sql = @"
                UPDATE studytasks
                SET IsCompleted = @IsCompleted
                WHERE StudyTaskId = @StudyTaskId
                  AND UserId = @UserId;";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@IsCompleted", isCompleted);

            command.Parameters.AddWithValue(
                "@StudyTaskId", studyTaskId);

            command.Parameters.AddWithValue(
                "@UserId", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();

            return affectedRows > 0;
        }

        // update a tasks title, date, and planned study duration
        public async Task<bool> UpdateTaskAsync(
            int userId,
            int studyTaskId,
            string title,
            DateTime studyDate,
            decimal hours)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Task title cannot be empty.",
                    nameof(title));
            }

            if (hours < 0.5m || hours > 24m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hours),
                    "Study hours must be between 0.5 and 24.");
            }

            const string sql = @"
                UPDATE studytasks
                SET Title = @Title,
                    StudyDate = @StudyDate,
                    Hours = @Hours
                WHERE StudyTaskId = @StudyTaskId
                  AND UserId = @UserId;";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            // bind the edited values and identify the task by ID and user
            command.Parameters.AddWithValue("@Title", title.Trim());

            command.Parameters.Add(
                "@StudyDate",
                MySqlDbType.Date
            ).Value = studyDate.Date;

            command.Parameters.Add(
                "@Hours",
                MySqlDbType.Decimal
            ).Value = hours;

            command.Parameters.AddWithValue(
                "@StudyTaskId", studyTaskId);

            command.Parameters.AddWithValue(
                "@UserId", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();

            return affectedRows > 0;
        }

        // delete a study task owned by the specified user
        public async Task<bool> DeleteTaskAsync(
            int userId,
            int studyTaskId)
        {
            const string sql = @"
                DELETE FROM studytasks
                WHERE StudyTaskId = @StudyTaskId
                  AND UserId = @UserId;";

            await using var connection =
                _databaseService.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@StudyTaskId", studyTaskId);

            command.Parameters.AddWithValue(
                "@UserId", userId);

            // return true when a database row was deleted
            int affectedRows = await command.ExecuteNonQueryAsync();

            return affectedRows > 0;
        }
    }
}
