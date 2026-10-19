using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIStudyPlannerApp.Backend
{

    // turns user data into text the ai can read
    public class StudyContextBuilder
    {
        // keeps prompt short to remaind lowcost
        private const int MaxItemsPerSection = 15;
        // deadlines to consider
        private const int DaysAhead = 14;
        private static readonly string[] DayNames =
            { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        
        private readonly IAssignmentService _assignmentService;
        private readonly TimetableService _timetableService;
        private readonly StudyTaskService _studyTaskService;

        public StudyContextBuilder()
        {
            var database = new DatabaseService();
            _assignmentService = new AssignmentService(database);
            _timetableService = new TimetableService(database);
            _studyTaskService = new StudyTaskService();
        }

        public async Task<string> BuildAsync(int userId)
        {
            DateTime today = DateTime.Today;
            var summary = new StringBuilder();

            summary.AppendLine($"Today is {today:dddd, dd MMM yyyy}.");
            summary.AppendLine();

            await AddAssignmentsAsync(summary, userId, today);
            await AddTimetableAsync(summary, userId);
            await AddStudyTasksAsync(summary, userId, today);
            
            return summary.ToString();
        }

        private async Task AddAssignmentsAsync(StringBuilder summary, int userId, DateTime today)
        {
            List<AssignmentRecord> assignments = await _assignmentService.GetAllAsync(userId);
            // unfinished ones due in 14d, overdue included
            List<AssignmentRecord> upcoming = assignments
                .Where(a => !a.Completed && a.DueDate.Date <= today.AddDays(DaysAhead))
                .OrderBy(a => a.DueDate)
                .Take(MaxItemsPerSection)
                .ToList();

            summary.AppendLine("Upcoming Assignments:");
            
            if (upcoming.Count == 0)
            {
                summary.AppendLine("- None");
            }
            
            foreach (AssignmentRecord a in upcoming)
            {
                string overdue = a.DueDate.Date < today ? " (OVERDUE)" : "";
                summary.AppendLine(
                    $"- {a.Title} ({a.CourseName}), due on {a.DueDate:dd MMM yyyy}{overdue}" +
                    $" [Priority: {a.Priority}, {a.Status}]");
            }
            summary.AppendLine();
        }

        private async Task AddTimetableAsync(StringBuilder summary, int userId)
        {
            List<TimetableRecord> classes = await _timetableService.GetAllAsync(userId);
            summary.AppendLine("Weekly classes:");

            if (classes.Count == 0)
            {
                summary.AppendLine("- None");
            }
            // already sorted by day and time from db
            foreach (TimetableRecord item in classes.Take(MaxItemsPerSection))
            {
                string day = DayNames[item.DayOfWeek - 1];
                string start = item.StartTime.ToString(@"hh\:mm");
                string end = item.EndTime.ToString(@"hh\:mm");
                summary.AppendLine($"- {day} {start} - {end} {item.CourseName}");
            }
            summary.AppendLine();
        }

        private async Task AddStudyTasksAsync(StringBuilder summary, int userId, DateTime today)
        {
            List<StudyTaskRecord> tasks = await _studyTaskService.GetTasksAsync(userId);
            
            DateTime weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            DateTime weekEnd = weekStart.AddDays(7);

            List<StudyTaskRecord> weekTasks = tasks
                .Where(t => t.StudyDate.Date >= weekStart && t.StudyDate.Date < weekEnd)
                .OrderBy(t => t.StudyDate)
                .ToList();

            int completed = weekTasks.Count(t => t.IsCompleted);
            decimal plannedHours = weekTasks.Sum(t => t.Hours);

            summary.AppendLine(
                $"Study tasks this week: {completed} of {weekTasks.Count} completed, " +
                $"{plannedHours:0.#} hours planned.");

            foreach (StudyTaskRecord t in weekTasks.Take(MaxItemsPerSection))
            {
                string done = t.IsCompleted ? "done" : "not done";
                summary.AppendLine($"- {t.StudyDate:ddd dd MMM}: {t.Title} ({t.Hours:0.#} h) [{done}]");
            }
        }
    }
}
