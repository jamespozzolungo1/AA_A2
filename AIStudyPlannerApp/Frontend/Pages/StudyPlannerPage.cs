
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIStudyPlannerApp.Backend;
using AIStudyPlannerApp.Frontend.Controls;

namespace AIStudyPlannerApp.Frontend.Pages
{
    public class StudyPlannerPage : UserControl
    {
        private const int DemoUserId = 1;
        private const int MinimumTaskCardWidth = 300;
        private const int TaskCardWidthPadding = 46;
        private const int TaskCardHeight = 52;
        private const int TaskCardLeftMargin = 16;
        private const int TaskCardBottomMargin = 6;
        private const int TaskCardPadding = 8;
        private const int TaskCheckBoxLeft = 13;
        private const int TaskCheckBoxTop = 14;

        private StudyTaskService? _studyTaskService;
        private List<StudyTaskRecord> tasks = new();
        private bool _isRenderingTasks;
        private int? _selectedTaskId;
        private readonly EventHandler _taskListSizeChangedHandler;
        private readonly EventHandler _pageLoadHandler;

        private readonly TextBox taskBox = new()
        {
            Width = 220,
            PlaceholderText = "Enter a study task"
        };

        private readonly DateTimePicker datePicker = new()
        {
            Format = DateTimePickerFormat.Short,
            Width = 130
        };

        private readonly NumericUpDown hoursPicker = new()
        {
            Minimum = 0.5m,
            Maximum = 24,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 1,
            Width = 75
        };

        private readonly FlowLayoutPanel taskList = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = ColorTranslator.FromHtml("#F7F9FC"),
            Padding = Padding.Empty
        };

        private readonly Label todayCount = new();
        private readonly Label weekCount = new();
        private readonly Label hoursCount = new();

        private readonly ProgressBar todayProgress = new()
        {
            Dock = DockStyle.Fill,
            Maximum = 100,
            Height = 14
        };

        private readonly ProgressBar weekProgress = new()
        {
            Dock = DockStyle.Fill,
            Maximum = 100,
            Height = 14
        };

        private readonly Color accent =
            ColorTranslator.FromHtml("#3478F6");

        private readonly Color background =
            ColorTranslator.FromHtml("#F7F9FC");

        // Initialize the StudyPlannerPage UI and state
        // Configure layout, wire UI events, and prepare initial progress values
        public StudyPlannerPage()
        {
            _taskListSizeChangedHandler = (_, _) => RenderTaskList();
            _pageLoadHandler = async (_, _) => await LoadTasksAsync();

            Dock = DockStyle.Fill;
            BackColor = background;
            Padding = new Padding(8);

            BuildLayout();

            taskList.SizeChanged += _taskListSizeChangedHandler;

            UpdateProgress();

            Load += _pageLoadHandler;
        }

        // Create and return the StudyTaskService instance
        // Defer construction so configuration errors can be handled by callers
        private StudyTaskService GetStudyTaskService()
        {
            // Create the service lazily so connection configuratio
            // errors can be shown instead of crashing page construction
            _studyTaskService ??= new StudyTaskService();
            return _studyTaskService;
        }

        // Create and arrange the page controls and connect their UI events
        // Keep database access in asynchronous methods rather than UI construction
        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = background
            };

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 60));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 40));

            Controls.Add(layout);

            var tasksPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 12)
            };

            var tasksLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            tasksLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            tasksLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            tasksLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            tasksPanel.Controls.Add(tasksLayout);

            tasksLayout.Controls.Add(new Label
            {
                Text = "Today's Study Tasks",
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 14, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 12)
            }, 0, 0);

            var editor = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 12),
                Padding = Padding.Empty
            };

            editor.Controls.Add(taskBox);
            editor.Controls.Add(datePicker);
            editor.Controls.Add(hoursPicker);

            editor.Controls.Add(
                RoundedButton.Create(
                    "Add",
                    accent,
                    AddTask));

            editor.Controls.Add(
                RoundedButton.Create(
                    "Delete",
                    Color.FromArgb(180, 35, 24),
                    DeleteSelected));

            // Open an editor for the selected task
            editor.Controls.Add(
                RoundedButton.Create(
                    "Update",
                    Color.FromArgb(70, 70, 70),
                    EditSelected));

            // Reload the task list from MySQL
            var refreshButton = RoundedButton.Create(
                "Refresh",
                Color.FromArgb(220, 220, 220),
                RefreshTasks);
            refreshButton.ForeColor = Color.FromArgb(55, 55, 55);
            editor.Controls.Add(refreshButton);

            taskBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddTask();
                }
            };

            tasksLayout.Controls.Add(editor, 0, 1);
            tasksLayout.Controls.Add(taskList, 0, 2);

            layout.Controls.Add(tasksPanel, 0, 0);

            var progressPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                Margin = Padding.Empty
            };

            var progressLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 20));

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 20));

            progressLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            progressPanel.Controls.Add(progressLayout);

            progressLayout.Controls.Add(new Label
            {
                Text = "Study Progress",
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 14, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 12)
            }, 0, 0);

            todayCount.AutoSize = true;
            todayCount.Margin = new Padding(0, 0, 0, 4);
            progressLayout.Controls.Add(todayCount, 0, 1);
            progressLayout.Controls.Add(todayProgress, 0, 2);

            weekCount.AutoSize = true;
            weekCount.Margin = new Padding(0, 8, 0, 4);
            progressLayout.Controls.Add(weekCount, 0, 3);
            progressLayout.Controls.Add(weekProgress, 0, 4);

            hoursCount.AutoSize = true;
            hoursCount.Font = new Font(
                "Segoe UI", 11, FontStyle.Bold);
            hoursCount.Margin = new Padding(0, 12, 0, 0);
            progressLayout.Controls.Add(hoursCount, 0, 5);

            layout.Controls.Add(progressPanel, 0, 1);
        }

        // Read tasks from MySQL
        // Load tasks for the demo user from the database
        // Refresh the task list and progress summary, and display errors if loading fails
        private async Task LoadTasksAsync()
        {
            try
            {
                List<StudyTaskRecord> loadedTasks =
                    await GetStudyTaskService()
                        .GetTasksAsync(DemoUserId);

                tasks = loadedTasks;

                if (_selectedTaskId.HasValue &&
                    !tasks.Any(task => task.StudyTaskId == _selectedTaskId.Value))
                {
                    _selectedTaskId = null;
                }

                RenderTaskList();
                UpdateProgress();
            }
            catch (Exception ex)
            {
                tasks = new List<StudyTaskRecord>();
                RenderTaskList();
                UpdateProgress();

                MessageBox.Show(
                    "Failed to load study tasks from MySQL.\n\n"
                    + ex.Message,
                    "Database Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Start the asynchronous task-creation workflow
        private void AddTask()
        {
            _ = AddTaskAsync();
        }

        // Save a new task to MySQL
        // Validate the task input, save the task, and refresh the displayed data
        // Restore the input focus after a successful save
        private async Task AddTaskAsync()
        {
            string title = taskBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Please enter a study task.",
                    "Missing Task",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                taskBox.Focus();
                return;
            }

            try
            {
                await GetStudyTaskService().AddTaskAsync(
                    DemoUserId,
                    title,
                    datePicker.Value.Date,
                    hoursPicker.Value);

                taskBox.Clear();
                datePicker.Value = DateTime.Today;
                hoursPicker.Value = 1;

                await LoadTasksAsync();
                taskBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to save the task to MySQL.\n\n"
                    + ex.Message,
                    "Database Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Start the asynchronous deletion workflow for checked tasks
        private void DeleteSelected()
        {
            _ = DeleteSelectedAsync();
        }

        // Delete the currently selected task from MySQL
        // The completion checkbox is separate and does not control task selection
        private async Task DeleteSelectedAsync()
        {
            StudyTaskRecord? selectedTask = tasks
                .FirstOrDefault(task => task.StudyTaskId == _selectedTaskId);

            if (selectedTask == null)
            {
                MessageBox.Show(
                    "Click a task card to select it before deleting.",
                    "No Task Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Delete the selected task \"{selectedTask.Title}\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                bool deleted = await GetStudyTaskService().DeleteTaskAsync(
                    DemoUserId,
                    selectedTask.StudyTaskId);

                if (!deleted)
                {
                    throw new InvalidOperationException(
                        "The task was not found in the database.");
                }

                _selectedTaskId = null;
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to delete the task from MySQL.\\n\\n" + ex.Message,
                    "Database Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                await LoadTasksAsync();
            }
        }

        // Open the edit dialog for the single selected task
        private void EditSelected()
        {
            StudyTaskRecord? selectedTask = tasks
                .FirstOrDefault(task => task.StudyTaskId == _selectedTaskId);

            if (selectedTask == null)
            {
                MessageBox.Show(
                    "Click a task card to select it before updating.",
                    "No Task Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _ = EditTaskAsync(selectedTask);
        }

        // Save the edited task to MySQL and refresh the displayed list
        private async Task EditTaskAsync(StudyTaskRecord task)
        {
            using var dialog = new Form
            {
                Text = "Edit Study Task",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(390, 205),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Font = new Font("Segoe UI", 10F)
            };

            var titleLabel = new Label
            {
                Text = "Task title",
                Location = new Point(18, 18),
                AutoSize = true
            };

            var titleInput = new TextBox
            {
                Text = task.Title,
                Location = new Point(125, 15),
                Width = 240
            };

            var dateLabel = new Label
            {
                Text = "Study date",
                Location = new Point(18, 58),
                AutoSize = true
            };

            var dateInput = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = task.StudyDate,
                Location = new Point(125, 55),
                Width = 150
            };

            var hoursLabel = new Label
            {
                Text = "Hours",
                Location = new Point(18, 98),
                AutoSize = true
            };

            var hoursInput = new NumericUpDown
            {
                Minimum = 0.5m,
                Maximum = 24m,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = Math.Clamp(task.Hours, 0.5m, 24m),
                Location = new Point(125, 95),
                Width = 100
            };

            var saveButton = new Button
            {
                Text = "Save Changes",
                DialogResult = DialogResult.OK,
                Location = new Point(125, 145),
                Size = new Size(120, 32)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(255, 145),
                Size = new Size(90, 32)
            };

            dialog.Controls.AddRange(new Control[]
            {
                titleLabel, titleInput, dateLabel, dateInput,
                hoursLabel, hoursInput, saveButton, cancelButton
            });
            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string updatedTitle = titleInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(updatedTitle))
            {
                MessageBox.Show(
                    "Task title cannot be empty.",
                    "Invalid Task Title",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool updated = await GetStudyTaskService().UpdateTaskAsync(
                    DemoUserId,
                    task.StudyTaskId,
                    updatedTitle,
                    dateInput.Value.Date,
                    hoursInput.Value);

                if (!updated)
                {
                    throw new InvalidOperationException(
                        "The task was not found in the database.");
                }

                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to update the task in MySQL.\n\n" + ex.Message,
                    "Database Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Query MySQL again and refresh the visible task list
        private void RefreshTasks()
        {
            _ = LoadTasksAsync();
        }

        // Draw the task cards using the records loaded from MySQL
        // Rebuild the visible task cards from the in-memory task list
        private void RenderTaskList()
        {
            if (_isRenderingTasks || taskList.IsDisposed)
            {
                return;
            }

            _isRenderingTasks = true;
            taskList.SuspendLayout();

            try
            {
                taskList.Controls.Clear();

                foreach (StudyTaskRecord task in tasks
                    .OrderBy(t => t.StudyDate)
                    .ThenBy(t => t.Title))
                {
                    int cardWidth = CalculateCardWidth(taskList.ClientSize.Width);

                    bool isSelected = task.StudyTaskId == _selectedTaskId;

                    var card = new RoundedPanel
                    {
                        Width = cardWidth,
                        Height = TaskCardHeight,
                        Margin = GetTaskCardMargin(),
                        Padding = new Padding(TaskCardPadding),
                        BackColor = isSelected
                            ? ColorTranslator.FromHtml("#E2E5E9")
                            : Color.White,
                        Cursor = Cursors.Hand
                    };

                    // Clicking the card background selects a task without changing completion
                    card.Click += (_, _) => SelectTask(task.StudyTaskId);

                    var check = new CheckBox
                    {
                        Text =
                            $"{task.Title}  |  "
                            + $"{task.StudyDate:dd MMM}  |  "
                            + $"{task.Hours:0.#} h",
                        Checked = task.IsCompleted,
                        Tag = task,
                        AutoSize = true,
                        Location = new Point(TaskCheckBoxLeft, TaskCheckBoxTop),
                        Font = new Font(
                            "Segoe UI",
                            10,
                            task.IsCompleted
                                ? FontStyle.Strikeout
                                : FontStyle.Regular)
                    };

                    check.BackColor = card.BackColor;

                    bool restoringCheck = false;

                    check.CheckedChanged += async (_, _) =>
                    {
                        if (restoringCheck)
                        {
                            return;
                        }

                        bool requestedValue = check.Checked;
                        bool previousValue = task.IsCompleted;

                        check.Enabled = false;

                        try
                        {
                            bool updated =
                                await GetStudyTaskService()
                                    .UpdateCompletionAsync(
                                        DemoUserId,
                                        task.StudyTaskId,
                                        requestedValue);

                            if (!updated)
                            {
                                throw new InvalidOperationException(
                                    "The task was not found in the database.");
                            }

                            task.IsCompleted = requestedValue;

                            check.Font = new Font(
                                "Segoe UI",
                                10,
                                requestedValue
                                    ? FontStyle.Strikeout
                                    : FontStyle.Regular);

                            UpdateProgress();
                        }
                        catch (Exception ex)
                        {
                            restoringCheck = true;
                            check.Checked = previousValue;
                            restoringCheck = false;

                            MessageBox.Show(
                                "Failed to update task completion.\n\n"
                                + ex.Message,
                                "Database Update Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                        finally
                        {
                            if (!check.IsDisposed)
                            {
                                check.Enabled = true;
                            }
                        }
                    };

                    card.Controls.Add(check);
                    taskList.Controls.Add(card);
                }

                UpdateProgress();
            }
            finally
            {
                taskList.ResumeLayout(true);
                _isRenderingTasks = false;
            }
        }

        private void SelectTask(int taskId)
        {
            _selectedTaskId = taskId;
            RenderTaskList();
        }

        private int CalculateCardWidth(int containerWidth)
        {
            return Math.Max(
                MinimumTaskCardWidth,
                containerWidth - TaskCardWidthPadding);
        }

        private Padding GetTaskCardMargin()
        {
            return new Padding(TaskCardLeftMargin, 0, 0, TaskCardBottomMargin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                taskList.SizeChanged -= _taskListSizeChangedHandler;
                Load -= _pageLoadHandler;
            }

            base.Dispose(disposing);
        }

        private void UpdateProgress()
        {
            DateTime today = DateTime.Today;

            DateTime weekStart = today.AddDays(
                -(((int)today.DayOfWeek + 6) % 7));

            DateTime weekEnd = weekStart.AddDays(7);

            var todayTasks = tasks
                .Where(t => t.StudyDate.Date == today)
                .ToList();

            var weekTasks = tasks
                .Where(t =>
                    t.StudyDate.Date >= weekStart &&
                    t.StudyDate.Date < weekEnd)
                .ToList();

            int todayCompleted =
                todayTasks.Count(t => t.IsCompleted);

            int weekCompleted =
                weekTasks.Count(t => t.IsCompleted);

            todayCount.Text =
                $"Today's progress: {todayCompleted}/{todayTasks.Count} tasks";

            todayProgress.Value = todayTasks.Count == 0
                ? 0
                : Math.Clamp(
                    (int)Math.Round(
                        todayCompleted * 100.0 / todayTasks.Count),
                    0,
                    100);

            weekCount.Text =
                $"Weekly progress: {weekCompleted}/{weekTasks.Count} tasks";

            weekProgress.Value = weekTasks.Count == 0
                ? 0
                : Math.Clamp(
                    (int)Math.Round(
                        weekCompleted * 100.0 / weekTasks.Count),
                    0,
                    100);

            hoursCount.Text =
                "Planned study hours this week: "
                + $"{weekTasks.Sum(t => t.Hours):0.#} hours";
        }
    }
}
