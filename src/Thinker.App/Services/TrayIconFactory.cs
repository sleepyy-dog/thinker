using System.Drawing;
using Thinker.Models;

namespace Thinker.Services;

public static class TrayIconFactory
{
    public static Icon Create(ModeStatus status)
    {
        var color = status switch
        {
            ModeStatus.Normal => Color.Gray,
            ModeStatus.ActiveTimed => Color.Goldenrod,
            ModeStatus.ActivePermanent => Color.SeaGreen,
            ModeStatus.Error => Color.Firebrick,
            _ => Color.Gray
        };

        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, 4, 4, 24, 24);
        using var pen = new Pen(Color.White, 3);
        graphics.DrawEllipse(pen, 4, 4, 24, 24);
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
