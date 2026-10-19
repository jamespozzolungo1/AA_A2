
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AIStudyPlannerApp.Frontend.Controls
{
    public class Sidebar : Panel
    {
        private readonly Color blue =
            ColorTranslator.FromHtml("#3478F6");

        private readonly Color textColor =
            ColorTranslator.FromHtml("#595959");

        private readonly Color borderColor =
            ColorTranslator.FromHtml("#E5E9F0");

        public event Action<string>? NavigationRequested;

        private readonly string[] pages =
        {
            "Dashboard",
            "Timetable",
            "Assignments",
            "Study Planner",
            "Analytics"
        };

        public Sidebar()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(16, 24, 16, 16);

            var title = new Label
            {
                Text = "· AI Study\n  Planner",
                Dock = DockStyle.Top,
                Height = 64,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Size = new Size(210, 70),
                ForeColor = ColorTranslator.FromHtml("#3478F6"),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            Controls.Add(title);

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0, 20, 0, 0)
            };

            Controls.Add(menu);
            menu.BringToFront();

            foreach (string page in pages)
            {
                var button = new RoundedButton
                {
                    Text = "   " + page,
                    Size = new Size(180, 42),
                    Margin = new Padding(0, 4, 0, 4),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = textColor,
                    Padding = new Padding(8, 0, 0, 0)
                };

                // Raise an event when a navigation item is clicked
                button.Click += (sender, e) =>
                {
                    NavigationRequested?.Invoke(page);
                    SetActivePage(page);
                };

                menu.Controls.Add(button);
            }

            SetActivePage("Dashboard");
        }

        public void SetActivePage(string page)
        {
            foreach (Control control in Controls)
            {
                if (control is FlowLayoutPanel menu)
                {
                    foreach (Control item in menu.Controls)
                    {
                        if (item is RoundedButton button)
                        {
                            bool isActive =
                                button.Text.Trim() == page;

                            button.BackColor = isActive
                                ? ColorTranslator.FromHtml("#EAF1FF")
                                : Color.White;

                            button.ForeColor = isActive
                                ? blue
                                : textColor;
                        }
                    }
                }
            }
        }
    }
}
