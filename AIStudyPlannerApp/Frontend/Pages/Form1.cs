using System;
using System.Drawing;
using System.Windows.Forms;
using AIStudyPlannerApp.Frontend.Controls;
using AIStudyPlannerApp.Frontend.Pages;

namespace AIStudyPlannerApp
{
    public partial class Form1 : Form
    {
        private const int SidebarWidth = 230;
        private const int HeaderLeftPadding = 32;
        private const int HeaderTitleTop = 24;
        private const int HeaderDescriptionLeft = 34;
        private const int HeaderDescriptionTop = 90;
        private const int ContentPaddingTop = 122;
        private const int ContentPaddingRight = 32;
        private const int ContentPaddingBottom = 32;
        private const int DashboardCardTop = 145;
        private const int DashboardButtonTop = 285;

        private readonly Color backgroundColor =
            ColorTranslator.FromHtml("#F7F9FC");

        private readonly Color textColor =
            ColorTranslator.FromHtml("#595959");

        private Sidebar sidebar = null!;
        private Panel contentPanel = null!;
        private Label pageTitle = null!;
        private Label pageDescription = null!;

        public Form1()
        {
            InitializeComponent();

            Text = "AI Study Planner";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1400, 900);
            MinimumSize = new Size(1100, 700);
            BackColor = backgroundColor;
            Font = new Font("Segoe UI", 10F);

            BuildLayout();
        }

        // Handle the Form load event. Keep the base behavior intact for future startup tasks.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        // Build the top-level layout with the sidebar and content area, then connect navigation.
        // This method creates and arranges controls only; it does not perform I/O.
        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = backgroundColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, SidebarWidth));

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            sidebar = new Sidebar();
            sidebar.NavigationRequested += ShowPage;

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                Padding = new Padding(
                    HeaderLeftPadding,
                    ContentPaddingTop,
                    ContentPaddingRight,
                    ContentPaddingBottom)
            };

            layout.Controls.Add(sidebar, 0, 0);
            layout.Controls.Add(contentPanel, 1, 0);

            Controls.Add(layout);

            ShowPage("Dashboard");
        }

        // Switch the content area to the requested page, update navigation, and add the page header.
        private void ShowPage(string page)
        {
            contentPanel.Controls.Clear();

            pageTitle = new Label
            {
                Text = page,
                Location = new Point(HeaderLeftPadding, HeaderTitleTop),
                AutoSize = true,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#3478F6")
            };

            pageDescription = new Label
            {
                Text = GetPageDescription(page),
                Location = new Point(HeaderDescriptionLeft, HeaderDescriptionTop),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = textColor
            };

            contentPanel.Controls.Add(pageTitle);
            contentPanel.Controls.Add(pageDescription);

            sidebar.SetActivePage(page);

            // Display the selected page content
            switch (page)
            {
                case "Dashboard":
                    ShowDashboard();
                    break;

                case "Timetable":
                    contentPanel.Controls.Add(new TimetablePage
                    {
                        Dock = DockStyle.Fill
                    });
                    break;

                case "Assignments":
                    contentPanel.Controls.Add(new AssignmentsPage
                    {
                        Dock = DockStyle.Fill
                    });
                    break;

                case "Study Planner":
                    contentPanel.Controls.Add(new StudyPlannerPage
                    {
                        Dock = DockStyle.Fill
                    });
                    break;

                case "Analytics":
                    contentPanel.Controls.Add(new AIChatPage
                    {
                        Dock = DockStyle.Fill
                    });
                    break;
            }
        }

        // Return the short description shown beneath the title for each navigation page
        private string GetPageDescription(string page)
        {
            return page switch
            {
                "Dashboard" => "Your learning overview at a glance.",
                "Timetable" => "Manage your weekly class schedule.",
                "Assignments" => "Keep track of tasks and deadlines.",
                "Study Planner" => "Plan your learning sessions.",
                "Analytics" => "Track your learning progress.",
                _ => "Welcome to AI Study Planner."
            };
        }

        // Build and add Dashboard-specific UI components to the content area
        private void ShowDashboard()
        {
            var welcomeCard = new RoundedPanel
            {
                Location = new Point(HeaderLeftPadding, DashboardCardTop),
                Size = new Size(650, 130),
                BackColor = Color.White
            };

            var welcomeLabel = new Label
            {
                Text = "Welcome to AI Study Planner",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#3478F6")
            };

            var subtitleLabel = new Label
            {
                Text = "Plan your learning. Make every study session count.",
                Location = new Point(22, 62),
                AutoSize = true,
                ForeColor = textColor
            };

            welcomeCard.Controls.Add(welcomeLabel);
            welcomeCard.Controls.Add(subtitleLabel);
            contentPanel.Controls.Add(welcomeCard);

            // Create the CSV import button
            var importButton = new RoundedButton
            {
                Text = "Import CSV",
                Location = new Point(HeaderLeftPadding, DashboardButtonTop),
                Size = new Size(150, 42),
                BackColor = ColorTranslator.FromHtml("#3478F6"),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty
            };

            importButton.Click += ImportTimetable_Click;
            contentPanel.Controls.Add(importButton);
        }

        // Display a temporary message until the requested page is implemented
        private void ShowPlaceholder(string message)
        {
            var placeholder = new Label
            {
                Text = message,
                Location = new Point(HeaderDescriptionLeft, 135),
                AutoSize = true,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 11F)
            };

            contentPanel.Controls.Add(placeholder);
        }

        private void ImportTimetable_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select your timetable",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show(
                    $"Selected file:\n{dialog.FileName}",
                    "Timetable Import",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (sidebar != null)
            {
                sidebar.NavigationRequested -= ShowPage;
            }

            base.OnFormClosed(e);
        }
    }
}
