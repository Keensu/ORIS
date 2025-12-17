using Microsoft.Maui.Graphics;

namespace GameAndDot.Maui;

public class GameDrawable : IDrawable
{
    private readonly List<(int X, int Y, string Color)> _points;

    public GameDrawable(List<(int X, int Y, string Color)> points)
    {
        _points = points;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Фон
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        // Точки
        foreach (var (x, y, hex) in _points)
        {
            try
            {
                canvas.FillColor = Color.FromArgb(hex);
            }
            catch
            {
                canvas.FillColor = Colors.Red;
            }
            canvas.FillCircle(x, y, 6); // радиус 6
        }
    }
}