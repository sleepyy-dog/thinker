using System.Drawing.Drawing2D;
using Thinker.Models;
using Thinker.Services;

namespace Thinker;

public sealed class PetForm : Form
{
    private static readonly Color TransparentColor = Color.FromArgb(1, 2, 3);
    private readonly Func<Task> toggleAsync;
    private readonly Action openMenu;
    private AppState state = AppState.Default();
    private bool leftDown;
    private bool dragging;
    private Point dragStartMouse;
    private Point dragStartLocation;

    public PetForm(Func<Task> toggleAsync, Action openMenu)
    {
        this.toggleAsync = toggleAsync;
        this.openMenu = openMenu;

        AutoScaleMode = AutoScaleMode.None;
        BackColor = TransparentColor;
        ClientSize = new Size(220, 238);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ThinkerPet";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Thinker 桌宠";
        TopMost = true;
        TransparencyKey = TransparentColor;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        Location = GetDefaultLocation(Size);
        MouseDown += OnPetMouseDown;
        MouseMove += OnPetMouseMove;
        MouseUp += OnPetMouseUp;
    }

    public void UpdateState(AppState value)
    {
        state = value;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(TransparentColor);

        var visual = PetVisualState.FromAppState(state);
        DrawPet(e.Graphics, ClientRectangle, visual);
    }

    private async void OnPetMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && leftDown)
        {
            leftDown = false;
            if (!dragging)
            {
                await toggleAsync();
            }

            dragging = false;
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            openMenu();
        }
    }

    private void OnPetMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        leftDown = true;
        dragging = false;
        dragStartMouse = Cursor.Position;
        dragStartLocation = Location;
    }

    private void OnPetMouseMove(object? sender, MouseEventArgs e)
    {
        if (!leftDown)
        {
            return;
        }

        var delta = new Size(Cursor.Position.X - dragStartMouse.X, Cursor.Position.Y - dragStartMouse.Y);
        if (!dragging && Math.Abs(delta.Width) + Math.Abs(delta.Height) < 5)
        {
            return;
        }

        dragging = true;
        Location = ClampToWorkingArea(Point.Add(dragStartLocation, delta), Size);
    }

    private static Point GetDefaultLocation(Size size)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        return new Point(area.Right - size.Width - 24, area.Bottom - size.Height - 24);
    }

    private static Point ClampToWorkingArea(Point location, Size size)
    {
        var area = Screen.FromPoint(location).WorkingArea;
        var x = Math.Min(Math.Max(location.X, area.Left), area.Right - size.Width);
        var y = Math.Min(Math.Max(location.Y, area.Top), area.Bottom - size.Height);
        return new Point(x, y);
    }

    private static void DrawPet(Graphics graphics, Rectangle bounds, PetVisualState visual)
    {
        var badgeColor = visual.Mood switch
        {
            PetMood.Sleepy => Color.FromArgb(112, 119, 128),
            PetMood.Alert => Color.FromArgb(230, 166, 31),
            PetMood.Steady => Color.FromArgb(48, 148, 91),
            PetMood.Error => Color.FromArgb(196, 55, 48),
            _ => Color.FromArgb(112, 119, 128)
        };

        using var yellow = new SolidBrush(Color.FromArgb(250, 195, 52));
        using var darkPen = new Pen(Color.FromArgb(54, 31, 25), 7f);
        graphics.FillEllipse(yellow, 12, 10, 196, 196);
        graphics.DrawEllipse(darkPen, 12, 10, 196, 196);

        DrawEnergyMarks(graphics);
        DrawDog(graphics, visual.Mood);
        DrawBadge(graphics, badgeColor, visual.BadgeText);
        DrawCaption(graphics, visual.Caption);
    }

    private static void DrawEnergyMarks(Graphics graphics)
    {
        using var pen = new Pen(Color.FromArgb(92, 54, 33), 4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawLine(pen, 36, 70, 10, 58);
        graphics.DrawLine(pen, 40, 92, 8, 91);
        graphics.DrawLine(pen, 166, 50, 185, 24);
        graphics.DrawLine(pen, 184, 78, 211, 62);
        graphics.DrawLine(pen, 176, 116, 211, 118);
    }

    private static void DrawDog(Graphics graphics, PetMood mood)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(129, 64, 44));
        using var bodyShadow = new SolidBrush(Color.FromArgb(98, 43, 33));
        using var highlight = new SolidBrush(Color.FromArgb(174, 96, 72));
        using var nose = new SolidBrush(Color.FromArgb(67, 34, 31));
        using var eyeWhite = new SolidBrush(Color.FromArgb(255, 248, 233));
        using var pupil = new SolidBrush(Color.FromArgb(48, 29, 28));
        using var mouthPen = new Pen(Color.FromArgb(59, 31, 28), 2f);

        graphics.FillEllipse(bodyBrush, 54, 96, 103, 82);
        graphics.FillEllipse(bodyBrush, 78, 52, 92, 85);
        graphics.FillEllipse(bodyShadow, 62, 128, 54, 44);
        graphics.FillEllipse(bodyBrush, 38, 140, 36, 35);
        graphics.FillEllipse(bodyBrush, 132, 142, 38, 32);
        graphics.FillEllipse(highlight, 75, 137, 22, 12);

        using var leftEar = new GraphicsPath();
        leftEar.AddEllipse(58, 64, 34, 62);
        using var rightEar = new GraphicsPath();
        rightEar.AddEllipse(146, 65, 34, 58);
        graphics.FillPath(bodyBrush, leftEar);
        graphics.FillPath(bodyBrush, rightEar);

        using var tailPen = new Pen(Color.FromArgb(118, 57, 42), 13f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawBezier(tailPen, 57, 136, 21, 118, 45, 89, 63, 105);

        var leftEye = mood == PetMood.Sleepy ? new Rectangle(102, 78, 21, 15) : new Rectangle(98, 74, 24, 24);
        var rightEye = mood == PetMood.Sleepy ? new Rectangle(139, 76, 21, 15) : new Rectangle(138, 73, 25, 25);
        graphics.FillEllipse(eyeWhite, leftEye);
        graphics.FillEllipse(eyeWhite, rightEye);

        var pupilOffset = mood switch
        {
            PetMood.Sleepy => new Point(7, 5),
            PetMood.Error => new Point(5, 4),
            PetMood.Steady => new Point(8, 7),
            _ => new Point(4, 7)
        };
        graphics.FillEllipse(pupil, leftEye.X + pupilOffset.X, leftEye.Y + pupilOffset.Y, 7, 7);
        graphics.FillEllipse(pupil, rightEye.X + pupilOffset.X + 2, rightEye.Y + pupilOffset.Y - 1, 7, 7);

        graphics.FillEllipse(nose, 119, 100, 28, 19);
        graphics.DrawArc(mouthPen, 110, 113, 22, 20, 15, 80);
        graphics.DrawArc(mouthPen, 134, 113, 22, 20, 85, 80);
    }

    private static void DrawBadge(Graphics graphics, Color color, string text)
    {
        using var badgeBrush = new SolidBrush(color);
        using var borderPen = new Pen(Color.White, 3f);
        graphics.FillEllipse(badgeBrush, 156, 158, 48, 48);
        graphics.DrawEllipse(borderPen, 156, 158, 48, 48);

        using var font = new Font("Segoe UI", text.Length > 2 ? 10f : 13f, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(Color.White);
        var rect = new RectangleF(156, 158, 48, 48);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, textBrush, rect, format);
    }

    private static void DrawCaption(Graphics graphics, string caption)
    {
        var rect = new RectangleF(36, 204, 148, 28);
        using var background = new SolidBrush(Color.FromArgb(230, 54, 31, 25));
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
        using var path = RoundedRectangle(rect, 12f);
        graphics.FillPath(background, path);

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(caption, font, textBrush, rect, format);
    }

    private static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
