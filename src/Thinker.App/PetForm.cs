using System.Drawing.Drawing2D;
using Thinker.Models;

namespace Thinker;

public sealed class PetForm : Form
{
    private static readonly Color TransparentColor = Color.FromArgb(1, 2, 3);
    public static Size PetWindowSize { get; } = new(130, 165);

    private readonly Func<Task> toggleAsync;
    private readonly Action openMenu;
    private readonly Image petImage;
    private bool leftDown;
    private bool dragging;
    private Point dragStartMouse;
    private Point dragStartLocation;

    public PetForm(Func<Task> toggleAsync, Action openMenu)
    {
        this.toggleAsync = toggleAsync;
        this.openMenu = openMenu;
        petImage = Image.FromFile(GetPetImagePath());

        AutoScaleMode = AutoScaleMode.None;
        BackColor = TransparentColor;
        ClientSize = PetWindowSize;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        MaximumSize = PetWindowSize;
        MinimumSize = PetWindowSize;
        Name = "ThinkerPet";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Thinker 桌宠";
        TopMost = true;
        TransparencyKey = TransparentColor;
        Size = PetWindowSize;

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
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            petImage.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(TransparentColor);
        e.Graphics.DrawImage(petImage, ClientRectangle);
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

    private static string GetPetImagePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", "thinker-pet.png");
    }

    private static Point GetDefaultLocation(Size size)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        return new Point(area.Right - size.Width + 2, area.Bottom - size.Height - 24);
    }

    private static Point ClampToWorkingArea(Point location, Size size)
    {
        var area = Screen.FromPoint(location).WorkingArea;
        var x = Math.Min(Math.Max(location.X, area.Left), area.Right - size.Width + 2);
        var y = Math.Min(Math.Max(location.Y, area.Top), area.Bottom - size.Height);
        return new Point(x, y);
    }
}
