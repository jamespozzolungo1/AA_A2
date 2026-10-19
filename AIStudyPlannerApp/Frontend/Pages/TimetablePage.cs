
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIStudyPlannerApp.Backend;
using AIStudyPlannerApp.Frontend.Controls;

namespace AIStudyPlannerApp.Frontend.Pages
{
    // Timetable page: MySQL-backed weekly schedule, editor controls, and calendar rendering
    public class TimetablePage : UserControl
    {
        private sealed class Plan
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string? Location { get; set; }
            public string? Notes { get; set; }
        }

        private readonly TimetableService _timetableService;
        private readonly int _userId = TimetableService.TestUserId;
        private readonly List<Plan> plans = new();
        private Plan? selectedPlan;
        private DateTime weekStart = StartOfWeek(DateTime.Today);

        private readonly TextBox nameBox = new()
        {
            Width = 190,
            PlaceholderText = "Enter class or plan name"
        };

        private readonly TextBox searchBox = new()
        {
            Width = 170,
            PlaceholderText = "Search plans..."
        };

        private readonly DateTimePicker datePicker = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd MMM yyyy",
            Width = 130
        };

        private readonly DateTimePicker startPicker = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Width = 85
        };

        private readonly DateTimePicker endPicker = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Width = 85
        };

        private readonly FlowLayoutPanel weekGrid = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White,
            Padding = new Padding(4)
        };

        private readonly Label weekLabel = new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 81),
            Margin = new Padding(8, 8, 8, 0)
        };

        private readonly Color accent = ColorTranslator.FromHtml("#7FA8F8");
        private readonly Color background = ColorTranslator.FromHtml("#F8FAFF");
        private readonly Color borderColor = ColorTranslator.FromHtml("#D9E5FA");
        private readonly Color textColor = ColorTranslator.FromHtml("#334155");
        private readonly Color[] courseColors =
        {
            ColorTranslator.FromHtml("#EAF2FF"),
            ColorTranslator.FromHtml("#E9F8EF"),
            ColorTranslator.FromHtml("#FFF1E5"),
            ColorTranslator.FromHtml("#F5ECFF"),
            ColorTranslator.FromHtml("#FFECEF"),
            ColorTranslator.FromHtml("#FFF8DF"),
            ColorTranslator.FromHtml("#E5F8F7")
        };

        // Initialize the TimetablePage, configure services and default control values
        public TimetablePage()
        {
            _timetableService = new TimetableService(new DatabaseService());
            Dock = DockStyle.Fill;
            BackColor = background;
            Padding = new Padding(8);

            startPicker.Value = DateTime.Today.AddHours(9);
            endPicker.Value = DateTime.Today.AddHours(10);
            datePicker.Value = DateTime.Today;

            BuildLayout();
            Load += async (_, _) => await LoadPlansAsync();
        }

        // Asynchronously load timetable records for the current user from the service
        // Convert database rows into local Plan objects, restore selection when possible
        private async Task LoadPlansAsync()
        {
            try
            {
                List<TimetableRecord> records = await _timetableService.GetAllAsync(_userId);
                plans.Clear();

                foreach (TimetableRecord record in records)
                {
                    DateTime date = weekStart.AddDays(record.DayOfWeek - 1);
                    plans.Add(new Plan
                    {
                        Id = record.TimetableItemId,
                        Name = record.CourseName,
                        Date = date,
                        Start = date.Date.Add(record.StartTime),
                        End = date.Date.Add(record.EndTime),
                        Location = record.Location,
                        Notes = record.Notes
                    });
                }

                if (selectedPlan != null)
                {
                    selectedPlan = plans.FirstOrDefault(p => p.Id == selectedPlan.Id);
                    if (selectedPlan == null)
                        ClearFields();
                }

                RefreshPlans();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load chat history from MySQL:\n\n"
                    + ex.ToString(),
                    "Chat History Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Construct and arrange the timetable UI controls, including the editor
        // weekly calendar, navigation toolbar, and search filter
        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = background
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);


            var editor = new RoundedPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.White
            };

            var editorLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            editorLayout.Controls.Add(Field("Course", nameBox));
            editorLayout.Controls.Add(Field("Date", datePicker));
            editorLayout.Controls.Add(Field("Start", startPicker));
            editorLayout.Controls.Add(Field("End", endPicker));
            editorLayout.Controls.Add(CreateButton("Add", accent, AddPlan));
            editorLayout.Controls.Add(CreateButton("Update", Color.FromArgb(100, 116, 139), UpdatePlan));
            editorLayout.Controls.Add(CreateButton("Delete", Color.FromArgb(180, 35, 24), DeletePlan));
            editorLayout.Controls.Add(CreateButton("Clear", Color.FromArgb(229, 233, 240), ClearFields, true));
            editor.Controls.Add(editorLayout);
            layout.Controls.Add(editor, 0, 0);

            var calendarPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                Margin = Padding.Empty,
                BackColor = Color.White
            };

            var calendarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            calendarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calendarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            toolbar.Controls.Add(CreateButton("‹ Previous", Color.FromArgb(229, 233, 240), () => ChangeWeek(-7), true));
            toolbar.Controls.Add(CreateButton("Today", accent, GoToToday));
            toolbar.Controls.Add(CreateButton("Next ›", Color.FromArgb(229, 233, 240), () => ChangeWeek(7), true));
            weekLabel.Margin = new Padding(8, 8, 16, 0);
            toolbar.Controls.Add(weekLabel);
            toolbar.Controls.Add(new Label
            {
                Text = "Search",
                AutoSize = true,
                Margin = new Padding(8, 8, 4, 0)
            });
            toolbar.Controls.Add(searchBox);
            searchBox.Margin = new Padding(0, 3, 8, 0);
            searchBox.TextChanged += (_, _) => RefreshPlans();

            calendarLayout.Controls.Add(toolbar, 0, 0);
            calendarLayout.Controls.Add(weekGrid, 0, 1);
            calendarPanel.Controls.Add(calendarLayout);
            layout.Controls.Add(calendarPanel, 0, 1);
        }

        private static Control Field(string label, Control input)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 12, 6),
                BackColor = Color.White
            };
            panel.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                ForeColor = ColorTranslator.FromHtml("#64748B"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            });
            input.Margin = Padding.Empty;
            panel.Controls.Add(input);
            return panel;
        }

        private static RoundedButton CreateButton(
            string text, Color color, Action action, bool darkText = false)
        {
            var button = new RoundedButton
            {
                Text = text,
                Size = new Size(text.Length > 8 ? 112 : 92, 38),
                BackColor = color,
                ForeColor = darkText ? ColorTranslator.FromHtml("#52627A") : Color.White,
                BorderColor = ColorTranslator.FromHtml("#D8E4F7"),
                HoverColor = ControlPaint.Light(color, 0.08f),
                HoverTextColor = darkText ? ColorTranslator.FromHtml("#52627A") : Color.White,
                CornerRadius = 12,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                Margin = new Padding(0, 18, 8, 0)
            };
            button.Click += (_, _) => action();
            return button;
        }

        // Validate the editor fields, including the plan name and start/end times.
        // Return true when the input is valid; otherwise show a user-friendly message.
        private bool TryReadFields(out string name, out DateTime start, out DateTime end)
        {
            name = nameBox.Text.Trim();
            start = datePicker.Value.Date.Add(startPicker.Value.TimeOfDay);
            end = datePicker.Value.Date.Add(endPicker.Value.TimeOfDay);

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a course name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameBox.Focus();
                return false;
            }
            if (end <= start)
            {
                MessageBox.Show("End time must be later than start time.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                endPicker.Focus();
                return false;
            }
            return true;
        }

        // Validate the editor input and asynchronously create a timetable record through TimetableService
        private async void AddPlan()
        {
            if (!TryReadFields(out string name, out DateTime start, out DateTime end)) return;
            try
            {
                int dayOfWeek = ((int)datePicker.Value.DayOfWeek + 6) % 7 + 1;
                await _timetableService.AddAsync(
                    _userId, name, dayOfWeek, start.TimeOfDay, end.TimeOfDay);
                weekStart = StartOfWeek(datePicker.Value.Date);
                selectedPlan = null;
                await LoadPlansAsync();
                ClearFields();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("add", ex);
            }
        }

        // Validate the editor input and update the currently selected timetable plan through TimetableService
        private async void UpdatePlan()
        {
            if (selectedPlan == null)
            {
                MessageBox.Show("Please select a course card to update.", "No Course Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryReadFields(out string name, out DateTime start, out DateTime end)) return;
            try
            {
                int dayOfWeek = ((int)datePicker.Value.DayOfWeek + 6) % 7 + 1;
                await _timetableService.UpdateAsync(
                    _userId, selectedPlan.Id, name, dayOfWeek,
                    start.TimeOfDay, end.TimeOfDay,
                    selectedPlan.Location, selectedPlan.Notes);
                weekStart = StartOfWeek(datePicker.Value.Date);
                selectedPlan = null;
                await LoadPlansAsync();
                ClearFields();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("update", ex);
            }
        }

        // Confirm deletion of the selected plan, delete it through the service,
        // and refresh the local collection and calendar
        private async void DeletePlan()
        {
            if (selectedPlan == null)
            {
                MessageBox.Show("Please select a course card to delete.", "No Course Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Delete '{selectedPlan.Name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                await _timetableService.DeleteAsync(_userId, selectedPlan.Id);
                selectedPlan = null;
                await LoadPlansAsync();
                ClearFields();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("delete", ex);
            }
        }

        private void SelectPlan(Plan plan)
        {
            selectedPlan = plan;
            nameBox.Text = plan.Name;
            datePicker.Value = plan.Date;
            startPicker.Value = DateTime.Today.Add(plan.Start.TimeOfDay);
            endPicker.Value = DateTime.Today.Add(plan.End.TimeOfDay);
            RefreshPlans();
        }

        // Rebuild the visual timetable from the in-memory plans collection.
        // Apply the current search filter and selection, then update the week grid layout.
        private void RefreshPlans()
        {
            weekGrid.SuspendLayout();

            try
            {
                weekGrid.Controls.Clear();
                weekLabel.Text = $"{weekStart:dd MMM} – {weekStart.AddDays(6):dd MMM yyyy}";

                string keyword = searchBox.Text.Trim();

                var filtered = plans
                    .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .Where(p => p.Date.Date >= weekStart.Date &&
                                p.Date.Date < weekStart.AddDays(7).Date)
                    .OrderBy(p => p.Start)
                    .ToList();

                const int timeColumnWidth = 62;
                const int dayColumnWidth = 136;
                const int headerHeight = 42;
                const int hourHeight = 58;
                const int firstHour = 8;
                const int lastHour = 22;

                Color gridColor = ColorTranslator.FromHtml("#B7D0F5");
                Color[] textColors =
                {
            ColorTranslator.FromHtml("#355A8A"),
            ColorTranslator.FromHtml("#694A8A"),
            ColorTranslator.FromHtml("#80621C"),
            ColorTranslator.FromHtml("#2F6B57"),
            ColorTranslator.FromHtml("#89583D"),
            ColorTranslator.FromHtml("#8A4D63")
        };

                Color[] cardColors = courseColors;

                var canvas = new Panel
                {
                    Width = timeColumnWidth + dayColumnWidth * 7,
                    Height = headerHeight + (lastHour - firstHour) * hourHeight,
                    BackColor = ColorTranslator.FromHtml("#F8FAFF"),
                    Margin = Padding.Empty
                };

                for (int day = 0; day < 7; day++)
                {
                    DateTime date = weekStart.AddDays(day);

                    var header = new Label
                    {
                        Text = $"{date:ddd}\n{date:dd MMM}",
                        Location = new Point(timeColumnWidth + day * dayColumnWidth, 0),
                        Size = new Size(dayColumnWidth, headerHeight),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = ColorTranslator.FromHtml("#EEF4FF"),
                        ForeColor = ColorTranslator.FromHtml("#52627A"),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold)
                    };

                    canvas.Controls.Add(header);
                }

                var timeHeader = new Label
                {
                    Text = "Time",
                    Location = new Point(0, 0),
                    Size = new Size(timeColumnWidth, headerHeight),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = ColorTranslator.FromHtml("#EEF4FF"),
                    ForeColor = ColorTranslator.FromHtml("#52627A"),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold)
                };

                canvas.Controls.Add(timeHeader);

                // Create time labels once
                for (int hour = firstHour; hour <= lastHour; hour++)
                {
                    int y = headerHeight + (hour - firstHour) * hourHeight;

                    var timeLabel = new Label
                    {
                        Text = $"{hour:00}:00",
                        Location = new Point(0, y - 10),
                        Size = new Size(timeColumnWidth - 2, 20),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = ColorTranslator.FromHtml("#F8FAFF"),
                        ForeColor = ColorTranslator.FromHtml("#52627A"),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold)
                    };

                    canvas.Controls.Add(timeLabel);
                    timeLabel.BringToFront();
                }

                canvas.Paint += (_, e) =>
                {
                    using var pen = new Pen(gridColor, 1.5f);

                    for (int day = 0; day <= 7; day++)
                    {
                        int x = timeColumnWidth + day * dayColumnWidth;
                        e.Graphics.DrawLine(
                            pen, x, headerHeight,
                            x, canvas.Height - 1);
                    }

                    for (int hour = firstHour; hour <= lastHour; hour++)
                    {
                        int y = headerHeight + (hour - firstHour) * hourHeight;
                        e.Graphics.DrawLine(
                            pen, timeColumnWidth, y,
                            canvas.Width - 1, y);
                    }
                };

                int colorIndex = 0;

                foreach (Plan plan in filtered)
                {
                    double startMinutes = (plan.Start - plan.Date.Date).TotalMinutes;
                    double endMinutes = (plan.End - plan.Date.Date).TotalMinutes;

                    if (endMinutes <= startMinutes)
                        continue;

                    if (startMinutes < firstHour * 60 ||
                        startMinutes >= lastHour * 60)
                        continue;

                    int dayIndex = ((int)plan.Date.DayOfWeek + 6) % 7;

                    int y = headerHeight +
                        (int)Math.Round((startMinutes - firstHour * 60) / 60.0 * hourHeight);

                    int bottom = headerHeight +
                        (int)Math.Round((Math.Min(endMinutes, lastHour * 60) -
                                         firstHour * 60) / 60.0 * hourHeight);

                    int cardHeight = Math.Max(20, bottom - y);

                    int paletteIndex = colorIndex++ % cardColors.Length;
                    bool isSelected = selectedPlan?.Id == plan.Id;

                    var card = new RoundedScheduleCard
                    {
                        Location = new Point(
                            timeColumnWidth + dayIndex * dayColumnWidth + 3,
                            y + 2),
                        Size = new Size(dayColumnWidth - 6, Math.Max(18, cardHeight - 4)),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left,
                        BackColor = isSelected
                            ? ColorTranslator.FromHtml("#DCE9FF")
                            : cardColors[paletteIndex],
                        BorderColor = isSelected
                            ? ColorTranslator.FromHtml("#8FB4F8")
                            : gridColor,
                        CornerRadius = 9,
                        Padding = new Padding(5),
                        Cursor = Cursors.Hand
                    };

                    string cardText = $"{plan.Name}\n{plan.Start:HH:mm}-{plan.End:HH:mm}";

                    if (!string.IsNullOrWhiteSpace(plan.Location))
                        cardText += $"\n{plan.Location}";

                    var title = new Label
                    {
                        Text = cardText,
                        Dock = DockStyle.Fill,
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        ForeColor = isSelected
                            ? ColorTranslator.FromHtml("#355A8A")
                            : textColors[paletteIndex % textColors.Length],
                        BackColor = Color.Transparent,
                        AutoEllipsis = true,
                        Cursor = Cursors.Hand
                    };

                    card.Controls.Add(title);
                    card.Click += (_, _) => SelectPlan(plan);
                    title.Click += (_, _) => SelectPlan(plan);

                    canvas.Controls.Add(card);
                    card.BringToFront();
                }

                weekGrid.Controls.Add(canvas);
            }
            finally
            {
                weekGrid.ResumeLayout();
            }
        }

        private static void AddCell(TableLayoutPanel table, string text, int column, int row, bool header)
        {
            table.Controls.Add(new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8, header ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = header ? ColorTranslator.FromHtml("#52627A") : ColorTranslator.FromHtml("#94A3B8"),
                BackColor = header ? ColorTranslator.FromHtml("#EEF4FF") : ColorTranslator.FromHtml("#FCFDFF"),
                Margin = Padding.Empty,
                AutoSize = false
            }, column, row);
        }

        private void ChangeWeek(int days)
        {
            weekStart = weekStart.AddDays(days);
            selectedPlan = null;
            _ = LoadPlansAsync();
        }

        private void GoToToday()
        {
            weekStart = StartOfWeek(DateTime.Today);
            selectedPlan = null;
            _ = LoadPlansAsync();
        }

        // Calculate the Monday that starts the week containing the specified date
        private static DateTime StartOfWeek(DateTime date)
        {
            int offset = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-offset);
        }

        // Reset the editor controls to their default values and clear the current selection
        private void ClearFields()
        {
            selectedPlan = null;
            nameBox.Clear();
            datePicker.Value = DateTime.Today;
            startPicker.Value = DateTime.Today.AddHours(9);
            endPicker.Value = DateTime.Today.AddHours(10);
            RefreshPlans();
        }

        private static void ShowDatabaseError(string operation, Exception ex)
        {
            MessageBox.Show($"Failed to {operation} timetable item in MySQL.\n\n{ex.Message}",
                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private sealed class RoundedScheduleCard : Panel
        {
            [System.ComponentModel.Browsable(false)]
            [System.ComponentModel.DesignerSerializationVisibility(
                System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Color BorderColor { get; set; } = ColorTranslator.FromHtml("#D8E4F7");

            [System.ComponentModel.Browsable(false)]
            [System.ComponentModel.DesignerSerializationVisibility(
                System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public int CornerRadius { get; set; } = 9;

            public RoundedScheduleCard()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                using GraphicsPath path = CreateRoundedPath(bounds, CornerRadius);
                using SolidBrush brush = new SolidBrush(BackColor);
                using Pen pen = new Pen(BorderColor, 1.8f);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
                Region = new Region(path);
            }

            private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
            {
                int diameter = radius * 2;
                var path = new GraphicsPath();
                path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}

