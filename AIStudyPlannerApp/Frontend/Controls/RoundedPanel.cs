
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AIStudyPlannerApp.Frontend.Controls
{
    public class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            // Set the default appearance of the panel.
            BackColor = Color.White;
            Padding = new Padding(16);

            // Enable custom painting and reduce flickering during resizing.
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Enable anti-aliasing for smoother rounded corners
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            int radius = 16;

            using GraphicsPath path = CreateRoundedPath(rect, radius);

            // Fill the panel with its background colour
            using SolidBrush brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);

            // Draw a subtle border around the panel
            using Pen borderPen = new Pen(
                ColorTranslator.FromHtml("#E5E9F0"),
                1
            );

            e.Graphics.DrawPath(borderPen, path);

            // Clip the panel to create rounded outer corners
            Region = new Region(path);
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(
                rect.Right - diameter,
                rect.Y,
                diameter,
                diameter,
                270,
                90
            );
            path.AddArc(
                rect.Right - diameter,
                rect.Bottom - diameter,
                diameter,
                diameter,
                0, 90
            );
            path.AddArc(
                rect.X,
                rect.Bottom - diameter,
                diameter,
                diameter,
                90, 90
            );

            path.CloseFigure();
            return path;
        }
    }
}
