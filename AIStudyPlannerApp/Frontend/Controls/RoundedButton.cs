
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AIStudyPlannerApp.Frontend.Controls
{
    public class RoundedButton : Button
    {
        private Color normalBorderColor =
            ColorTranslator.FromHtml("#D5E3FF");

        private Color hoverColor =
            ColorTranslator.FromHtml("#EAF1FF");

        private Color hoverTextColor =
            ColorTranslator.FromHtml("#3478F6");

        private bool isHovered;

        // Configure custom properties without Designer serialization
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float BorderWidth { get; set; } = 3F;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 12;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => normalBorderColor;
            set
            {
                normalBorderColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor
        {
            get => hoverColor;
            set
            {
                hoverColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverTextColor
        {
            get => hoverTextColor;
            set
            {
                hoverTextColor = value;
                Invalidate();
            }
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;

            BackColor = Color.White;
            ForeColor = ColorTranslator.FromHtml("#595959");

            Font = new Font("Segoe UI", 10F);
            Cursor = Cursors.Hand;

            UseVisualStyleBackColor = false;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(12, 0, 0, 0);

            // Enable custom painting and reduce flickering
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
        }



        public static RoundedButton Create(
            string text,
            Color color,
            Action action,
            bool darkText = false)
        {
            var button = new RoundedButton
            {
                Text = text,

                // Match the TimetablePage button style
                Size = new Size(text.Length > 8 ? 112 : 92, 38),
                BackColor = color,
                ForeColor = darkText
                    ? ColorTranslator.FromHtml("#52627A")
                    : Color.White,
                BorderColor = ColorTranslator.FromHtml("#D8E4F7"),
                HoverColor = ControlPaint.Light(color, 0.08f),
                HoverTextColor = darkText
                    ? ColorTranslator.FromHtml("#52627A")
                    : Color.White,

                CornerRadius = 12,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                Margin = new Padding(0, 4, 8, 0)
            };

            button.Click += (_, _) => action();

            return button;
        }



        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            int radius = Math.Max(
                1,
                Math.Min(CornerRadius, Math.Min(Width, Height) / 2)
            );

            using GraphicsPath path = CreateRoundedPath(
                new Rectangle(0, 0, Width, Height),
                radius
            );

            Region? oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();
        }


        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            isHovered = false;
            Invalidate();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            // Enable high quality rendering
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            Rectangle rect = ClientRectangle;

            if (rect.Width <= 1 || rect.Height <= 1)
            {
                return;
            }

            rect.Width -= 1;
            rect.Height -= 1;

            int radius = Math.Max(
                0,
                Math.Min(
                    CornerRadius,
                    Math.Min(rect.Width, rect.Height) / 2
                )
            );

            using GraphicsPath path = CreateRoundedPath(rect, radius);

            // Draw the button background
            Color backgroundColor = isHovered
                ? HoverColor
                : BackColor;

            using SolidBrush backgroundBrush =
                new SolidBrush(backgroundColor);

            e.Graphics.FillPath(backgroundBrush, path);

            // Draw the button border
            if (BorderWidth > 0)
            {
                using Pen borderPen = new Pen(BorderColor, BorderWidth);
                borderPen.Alignment = PenAlignment.Center;
                e.Graphics.DrawPath(borderPen, path);
            }

            // Draw the button text
            Color textColor = isHovered
                ? HoverTextColor
                : ForeColor;

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                rect,
                textColor,
                (TextAlign == ContentAlignment.MiddleCenter
                    ? TextFormatFlags.HorizontalCenter
                    : TextFormatFlags.Left) |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding
            );

            // Draw the focus rectangle when the button is focused
            if (Focused && ShowFocusCues)
            {
                Rectangle focusRect = Rectangle.Inflate(rect, -4, -4);

                ControlPaint.DrawFocusRectangle(
                    e.Graphics,
                    focusRect,
                    textColor,
                    backgroundColor
                );
            }
        }


        private GraphicsPath CreateRoundedPath(
            Rectangle rect,
            int radius)
        {
            var path = new GraphicsPath();

            // Draw a rectangle when the corner radius is zero
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;

            // Draw the four rounded corners
            path.AddArc(
                rect.X,
                rect.Y,
                diameter,
                diameter,
                180,
                90
            );

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
                0,
                90
            );

            path.AddArc(
                rect.X,
                rect.Bottom - diameter,
                diameter,
                diameter,
                90,
                90
            );

            path.CloseFigure();

            return path;
        }

    }
}
