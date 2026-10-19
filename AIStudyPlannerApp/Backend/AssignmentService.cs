using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace AIStudyPlannerApp.Backend
{
    public class AssignmentRecord
    {
        public int AssignmentId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Not Started";

        public bool Completed => Status == "Completed";
    }

    public class AssignmentService : IAssignmentService
    {
        private readonly DatabaseService _database;

        // temp test user
        public const int TestUserId = 1;

        public AssignmentService(DatabaseService database)
        {
            _database = database
                ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<List<AssignmentRecord>> GetAllAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            var assignments = new List<AssignmentRecord>();

            await using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT AssignmentId, UserId, Title, CourseName,
                       Description, DueDate, Priority, Status
                FROM Assignments
                WHERE UserId = @UserId
                ORDER BY DueDate;";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                assignments.Add(new AssignmentRecord
                {
                    AssignmentId = reader.GetInt32("AssignmentId"),
                    UserId = reader.GetInt32("UserId"),
                    Title = reader.GetString("Title"),
                    CourseName = reader.GetString("CourseName"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? string.Empty
                        : reader.GetString("Description"),
                    DueDate = reader.GetDateTime("DueDate"),
                    Priority = reader.GetString("Priority"),
                    Status = reader.GetString("Status")
                });
            }

            return assignments;
        }

        public async Task<int> AddAsync(
            int userId,
            string title,
            DateTime dueDate,
            string courseName = "General",
            string description = "",
            string priority = "Medium",
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateTitle(title);
            ValidateCourseName(courseName);
            ValidatePriority(priority);

            const string sql = @"
                INSERT INTO Assignments
                    (UserId, Title, CourseName, Description,
                     DueDate, Priority, Status)
                VALUES
                    (@UserId, @Title, @CourseName, @Description,
                     @DueDate, @Priority, 'Not Started');

                SELECT LAST_INSERT_ID();";

            await using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Title", title.Trim());
            command.Parameters.AddWithValue("@CourseName", courseName.Trim());
            command.Parameters.AddWithValue("@Description", description ?? string.Empty);
            command.Parameters.AddWithValue("@DueDate", dueDate);
            command.Parameters.AddWithValue("@Priority", priority);

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(
            int userId,
            int assignmentId,
            string title,
            DateTime dueDate,
            string courseName = "General",
            string description = "",
            string priority = "Medium",
            string status = "Not Started",
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateAssignmentId(assignmentId);
            ValidateTitle(title);
            ValidateCourseName(courseName);
            ValidatePriority(priority);
            ValidateStatus(status);

            const string sql = @"
                UPDATE Assignments
                SET Title = @Title,
                    CourseName = @CourseName,
                    Description = @Description,
                    DueDate = @DueDate,
                    Priority = @Priority,
                    Status = @Status
                WHERE AssignmentId = @AssignmentId
                  AND UserId = @UserId;";

            await using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Title", title.Trim());
            command.Parameters.AddWithValue("@CourseName", courseName.Trim());
            command.Parameters.AddWithValue("@Description", description ?? string.Empty);
            command.Parameters.AddWithValue("@DueDate", dueDate);
            command.Parameters.AddWithValue("@Priority", priority);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@AssignmentId", assignmentId);
            command.Parameters.AddWithValue("@UserId", userId);

            int affected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (affected == 0)
            {
                throw new InvalidOperationException(
                    "Assignment not found, unchanged, or it does not belong to this user.");
            }
        }

        public async Task DeleteAsync(
            int userId,
            int assignmentId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateAssignmentId(assignmentId);

            const string sql = @"
                DELETE FROM Assignments
                WHERE AssignmentId = @AssignmentId
                  AND UserId = @UserId;";

            await using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AssignmentId", assignmentId);
            command.Parameters.AddWithValue("@UserId", userId);

            int affected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (affected == 0)
            {
                throw new InvalidOperationException(
                    "Assignment not found or it does not belong to this user.");
            }
        }

        private static void ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(userId), "User ID must be greater than zero.");
            }
        }

        private static void ValidateAssignmentId(int assignmentId)
        {
            if (assignmentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(assignmentId),
                    "Assignment ID must be greater than zero.");
            }
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Assignment title cannot be empty.", nameof(title));
            }

            if (title.Trim().Length > 200)
            {
                throw new ArgumentException(
                    "Assignment title cannot exceed 200 characters.", nameof(title));
            }
        }

        private static void ValidateCourseName(string courseName)
        {
            if (string.IsNullOrWhiteSpace(courseName))
            {
                throw new ArgumentException(
                    "Course name cannot be empty.", nameof(courseName));
            }

            if (courseName.Trim().Length > 150)
            {
                throw new ArgumentException(
                    "Course name cannot exceed 150 characters.", nameof(courseName));
            }
        }

        private static void ValidatePriority(string priority)
        {
            if (priority is not ("Low" or "Medium" or "High"))
            {
                throw new ArgumentException(
                    "Priority must be Low, Medium, or High.", nameof(priority));
            }
        }

        private static void ValidateStatus(string status)
        {
            if (status is not ("Not Started" or "In Progress" or "Completed"))
            {
                throw new ArgumentException(
                    "Status must be Not Started, In Progress, or Completed.",
                    nameof(status));
            }
        }
    }
}
