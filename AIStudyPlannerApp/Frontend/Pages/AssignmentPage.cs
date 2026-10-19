
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
    public class AssignmentsPage : UserControl
    {
        // Shared sizing values for assignment cards and their spacing
        private const int CardMinimumWidth = 300;
        private const int CardRightPadding = 50;
        private const int CardHeight = 66;
        private const int CardBottomMargin = 8;

        private readonly IAssignmentService _assignmentService;
        private readonly int _userId = AssignmentService.TestUserId;

        private readonly List<AssignmentRecord> assignments = new();
        private AssignmentRecord? selected;

        private readonly TextBox titleBox = new()
        {
            Width = 220,
            PlaceholderText = "Assignment name"
        };

        private readonly TextBox searchBox = new()
        {
            Width = 200,
            PlaceholderText = "Search..."
        };

        private readonly DateTimePicker duePicker = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd MMM yyyy",
            Width = 140
        };

        private readonly ComboBox statusFilter = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 130
        };

        private readonly FlowLayoutPanel list = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = ColorTranslator.FromHtml("#F7F9FC")
        };

        private readonly Color accent =
            ColorTranslator.FromHtml("#3478F6");

        private readonly Color background =
            ColorTranslator.FromHtml("#F7F9FC");

        private readonly Color selectedColor =
            ColorTranslator.FromHtml("#E5EEFF");

        private readonly Color cardColor = Color.White;



        // Polymorphic constructors, allowing for testing.
        public AssignmentsPage()
            : this(new AssignmentService(new DatabaseService()))
            {
            }
        public AssignmentsPage(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;

            Dock = DockStyle.Fill;
            BackColor = background;
            Padding = new Padding(8);

            BuildLayout();

            Load += async (_, _) => await LoadAssignmentsAsync();
        }

        // Loads assignments for the current user, updates the in-memory collection,
        // restores the selected record when it still exists, and refreshes the list
        private async Task LoadAssignmentsAsync()
        {
            try
            {
                List<AssignmentRecord> result =
                    await _assignmentService.GetAllAsync(_userId);

                assignments.Clear();
                assignments.AddRange(result);

                if (selected != null)
                {
                    selected = assignments.FirstOrDefault(
                        a => a.AssignmentId == selected.AssignmentId
                    );

                    if (selected == null)
                    {
                        titleBox.Clear();
                        duePicker.Value = DateTime.Today;
                    }
                    else
                    {
                        titleBox.Text = selected.Title;
                        duePicker.Value = selected.DueDate;
                    }
                }

                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load assignments from the database.\n\n"
                    + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // Creates and arranges the editor, action buttons, filters, and assignment list
        // This method configures the UI and event handlers without performing database I/O
        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = background
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(layout);

            // Editor panel (inputs + actions on one line)
            var editor = new RoundedPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 6) // reduced spacing above/below
            };

            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var assignmentLabel = new Label
            {
                Text = "Assignment",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 8)
            };
            row.Controls.Add(assignmentLabel);

            titleBox.Margin = new Padding(0, 5, 12, 5);
            titleBox.Anchor = AnchorStyles.Left;
            row.Controls.Add(titleBox);

            // Due date label + control
            var dueLabel = new Label
            {
                Text = "Due date",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 8)
            };
            row.Controls.Add(dueLabel);

            duePicker.Margin = new Padding(0, 5, 12, 5);
            duePicker.Anchor = AnchorStyles.Left;
            row.Controls.Add(duePicker);

            // Action buttons
            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(4, 4, 0, 0),
                Padding = Padding.Empty
            };

            actions.Controls.Add(
                RoundedButton.Create(
                    "Clear",
                    Color.FromArgb(229, 233, 240),
                    async () => await ClearAsync(),
                    true
                )
            );

            actions.Controls.Add(
                RoundedButton.Create(
                    "Delete",
                    Color.FromArgb(180, 35, 24),
                    async () => await DeleteAsync()
                )
            );

            actions.Controls.Add(
                RoundedButton.Create(
                    "Update",
                    Color.FromArgb(100, 116, 139),
                    async () => await UpdateAsync()
                )
            );

            actions.Controls.Add(
                RoundedButton.Create(
                    "Add",
                    accent,
                    async () => await AddAsync()
                )
            );

            // Add actions to the same row, left-aligned and vertically centered by margins
            row.Controls.Add(actions);

            editor.Controls.Add(row);
            layout.Controls.Add(editor, 0, 1);

            // Main list panel
            var panel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                Margin = new Padding(0, 12, 0, 12)
            };

            var listLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filters = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 10)
            };

            statusFilter.Items.AddRange(new object[] { "All", "Pending", "Completed" });
            statusFilter.SelectedIndex = 0;

            filters.Controls.Add(searchBox);
            filters.Controls.Add(statusFilter);

            searchBox.TextChanged += (_, _) => RefreshList();
            statusFilter.SelectedIndexChanged += (_, _) => RefreshList();

            listLayout.Controls.Add(filters, 0, 0);
            listLayout.Controls.Add(list, 0, 1);

            panel.Controls.Add(listLayout);
            layout.Controls.Add(panel, 0, 2);
        }

        /// Adds a label and its input control to the specified row of a table layout
        /// Applies consistent margins and left alignment to the input
        private static void AddField(
            TableLayoutPanel table,
            string label,
            Control input,
            int row)
        {
            table.RowStyles.Add(
                new RowStyle(SizeType.AutoSize)
            );

            table.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 8, 8)
            }, 0, row);

            input.Margin = new Padding(0, 5, 0, 5);
            input.Anchor = AnchorStyles.Left;

            table.Controls.Add(input, 1, row);
        }

        // Creates a styled rounded button whose asynchronous action runs on click
        // The button is disabled while the action is running and re-enabled afterward
        private static RoundedButton Button(
            string text,
            Color color,
            Func<Task> action,
            bool darkText = false)
        {
            var button = new RoundedButton
            {
                Text = text,
                Size = new Size(100, 36),
                BackColor = color,
                ForeColor = darkText
                    ? Color.FromArgb(51, 51, 51)
                    : Color.White,
                BorderColor = color,
                HoverColor = ControlPaint.Dark(color, 0.08f),
                HoverTextColor = darkText
                    ? Color.FromArgb(51, 51, 51)
                    : Color.White,
                CornerRadius = 8,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                Margin = new Padding(0, 0, 8, 0)
            };

            button.Click += async (_, _) =>
            {
                button.Enabled = false;

                try
                {
                    await action();
                }
                finally
                {
                    if (!button.IsDisposed)
                    {
                        button.Enabled = true;
                    }
                }
            };

            return button;
        }

        // Validates the editor, adds a new assignment through the service,
        // clears the editor, and reloads the assignment list
        private async Task AddAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                await _assignmentService.AddAsync(
                    _userId,
                    titleBox.Text.Trim(),
                    duePicker.Value.Date
                );

                Clear();
                await LoadAssignmentsAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError("add", ex);
            }
        }

        // Validates the editor and updates the selected assignment through the service.
        private async Task UpdateAsync()
        {
            if (selected == null)
            {
                MessageBox.Show(
                    "Please click an assignment card to select it first.",
                    "No Assignment Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }

            if (!ValidateInput())
                return;

            try
            {
                await _assignmentService.UpdateAsync(
                    _userId,
                    selected.AssignmentId,
                    titleBox.Text.Trim(),
                    duePicker.Value.Date,
                    selected.CourseName,
                    selected.Description,
                    selected.Priority,
                    selected.Status
                );

                await LoadAssignmentsAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError("update", ex);
            }
        }

        // Confirms deletion of the selected assignment, deletes it through the service,
        // clears the editor, and reloads the list
        private async Task DeleteAsync()
        {
            if (selected == null)
            {
                MessageBox.Show(
                    "Please click an assignment card to select it first.",
                    "No Assignment Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }

            DialogResult result = MessageBox.Show(
                $"Delete '{selected.Title}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            try
            {
                await _assignmentService.DeleteAsync(
                    _userId,
                    selected.AssignmentId
                );

                Clear();
                await LoadAssignmentsAsync();
            }
            catch (Exception ex)
            {
                ShowOperationError("delete", ex);
            }
        }

        // Clears the editor and selection, then returns a completed task for the async button handler
        private Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }


        private bool ValidateInput()
        {
            if (!string.IsNullOrWhiteSpace(titleBox.Text))
                return true;

            MessageBox.Show(
                "Please enter an assignment name.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            titleBox.Focus();
            return false;
        }

        private void Clear()
        {
            selected = null;
            titleBox.Clear();
            duePicker.Value = DateTime.Today;

            RefreshList();
        }

        // Selects an assignment, copies its title and due date into the editor

        private void Select(AssignmentRecord item)
        {
            selected = item;

            titleBox.Text = item.Title;
            duePicker.Value = item.DueDate;

            RefreshList();
        }

        private int CalculateCardWidth()
        {
            return Math.Max(CardMinimumWidth, list.ClientSize.Width - CardRightPadding);
        }

        // Rebuilds the visible assignment cards using the current search and status filters
        // Updates UI controls only; database operations are handled by service methods
        private void RefreshList()
        {
            list.SuspendLayout();

            try
            {
                list.Controls.Clear();

                string keyword = searchBox.Text.Trim();

                string status =
                    statusFilter.SelectedItem?.ToString() ?? "All";

                var filtered = assignments
                    .Where(a => a.Title.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    ))
                    .Where(a =>
                        status == "All" ||
                        (status == "Completed" && a.Completed) ||
                        (status == "Pending" && !a.Completed)
                    )
                    .OrderBy(a => a.DueDate)
                    .ToList();

                foreach (AssignmentRecord item in filtered)
                {
                    bool isSelected =
                        selected?.AssignmentId == item.AssignmentId;

                    var card = new RoundedPanel
                    {
                        Width = CalculateCardWidth(),
                        Height = CardHeight,
                        Margin = new Padding(0, 0, 0, CardBottomMargin),
                        Padding = new Padding(10),
                        BackColor = isSelected
                            ? selectedColor
                            : cardColor,
                        Cursor = Cursors.Hand
                    };

                    var check = new CheckBox
                    {
                        Text = item.Title,
                        Checked = item.Completed,
                        AutoSize = true,
                        Location = new Point(10, 8),
                        Font = new Font(
                            "Segoe UI",
                            10,
                            item.Completed
                                ? FontStyle.Strikeout
                                : FontStyle.Bold
                        ),
                        Cursor = Cursors.Hand
                    };

                    var due = new Label
                    {
                        Text = $"Due: {item.DueDate:dd MMM yyyy}",
                        AutoSize = true,
                        Location = new Point(34, 36),
                        ForeColor = Color.Gray,
                        BackColor = Color.Transparent,
                        Cursor = Cursors.Hand
                    };

                    // Clicking the card or due-date label selects the assignment
                    card.Click += (_, _) => Select(item);
                    due.Click += (_, _) => Select(item);

                    // The checkbox changes completion status only
                    check.CheckedChanged += async (_, _) =>
                    {
                        bool newChecked = check.Checked;
                        string previousStatus = item.Status;

                        item.Status = newChecked
                            ? "Completed"
                            : "Not Started";

                        check.Enabled = false;

                        try
                        {
                            await _assignmentService.UpdateAsync(
                                _userId,
                                item.AssignmentId,
                                item.Title,
                                item.DueDate,
                                item.CourseName,
                                item.Description,
                                item.Priority,
                                item.Status
                            );

                            await LoadAssignmentsAsync();
                        }
                        catch (Exception ex)
                        {
                            item.Status = previousStatus;

                            MessageBox.Show(
                                "Failed to update assignment status.\n\n"
                                + ex.Message,
                                "Update Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );

                            RefreshList();
                        }
                    };

                    card.Controls.Add(check);
                    card.Controls.Add(due);

                    list.Controls.Add(card);
                }
            }
            finally
            {
                list.ResumeLayout();
            }
        }

        // Displays a consistent error dialog for a failed assignment operation
        // Includes the operation name and exception details.
        private static void ShowOperationError(
            string operation,
            Exception ex)
        {
            MessageBox.Show(
                $"Failed to {operation} assignment.\n\n"
                + ex.Message,
                "Database Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
