
using System;

namespace AIStudyPlannerApp.Backend
{
    public abstract class StudyItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Today;

        public bool IsCompleted { get; set; }

        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }
    }

    public class TimetableItem : StudyItem
    {
        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public override bool IsValid()
        {
            return base.IsValid()
                && EndTime > StartTime;
        }
    }

    public class StudyTaskItem : StudyItem
    {
        public double PlannedHours { get; set; }

        public override bool IsValid()
        {
            return base.IsValid()
                && PlannedHours > 0
                && PlannedHours <= 24;
        }
    }
}
