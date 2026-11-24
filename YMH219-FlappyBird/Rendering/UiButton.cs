using System.Drawing.Drawing2D;

namespace FlappyBird.Rendering;

/// <summary>
/// Oyun ekranına GDI+ ile çizilen basit buton.
/// Fare ile üzerine gelinince açılır, tıklanınca Clicked olayını tetikler.
/// </summary>
public class UiButton
{
    public RectangleF Bounds { get; set; }
    public string Text { get; set; }
    public Color BaseColor { get; set; }
    public float FontSize { get; set; } = 22f;

    /// <summary>Seçili buton (ör. aktif zorluk) sarı çerçeveyle vurgulanır.</summary>
    public bool IsSelected { get; set; }

    /// <summary>Fare butonun üzerinde mi?</summary>
    public bool IsHovered { get; set; }

    /// <summary>Butona tıklandığında tetiklenir.</summary>
    public event Action? Clicked;

    public UiButton(string text, RectangleF bounds, Color baseColor)
    {
        Text = text;
        Bounds = bounds;
        BaseColor = baseColor;
    }

    public bool Contains(PointF p) => Bounds.Contains(p);

    /// <summary>Nokta butonun içindeyse Clicked olayını çalıştırır.</summary>
    public bool TryClick(PointF p)
    {
        if (!Contains(p)) return false;
        Clicked?.Invoke();
        return true;
    }

    /// <summary>Butonu çizer. alpha ile saydamlaştırılabilir (giriş animasyonları için).</summary>
    public void Draw(Graphics g, float alpha = 1f)
    {
        if (alpha <= 0.01f) return;
        RectangleF r = Bounds;
        float radius = Math.Min(14f, r.Height / 2.5f);
        Color baseColor = IsHovered ? DrawingHelper.Lighten(BaseColor, 0.15f) : BaseColor;

        // Alt gölge: butona kabartma (3B) görünümü verir
        var shadowRect = new RectangleF(r.X, r.Y + 5, r.Width, r.Height);
        using (GraphicsPath shadowPath = DrawingHelper.RoundedRect(shadowRect, radius))
        using (var shadowBrush = new SolidBrush(DrawingHelper.WithAlpha(DrawingHelper.Darken(BaseColor, 0.5f), alpha)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        using GraphicsPath path = DrawingHelper.RoundedRect(r, radius);

        // Gövde: üstten alta açık → koyu gradyan
        using (var body = new LinearGradientBrush(r,
                   DrawingHelper.WithAlpha(DrawingHelper.Lighten(baseColor, 0.25f), alpha),
                   DrawingHelper.WithAlpha(baseColor, alpha), LinearGradientMode.Vertical))
        {
            g.FillPath(body, path);
        }

        // Üst yarıda parlaklık şeridi
        var glossRect = new RectangleF(r.X + 4, r.Y + 3, r.Width - 8, r.Height * 0.42f);
        using (GraphicsPath gloss = DrawingHelper.RoundedRect(glossRect, radius * 0.8f))
        using (var glossBrush = new SolidBrush(Color.FromArgb((int)(60 * alpha), 255, 255, 255)))
        {
            g.FillPath(glossBrush, gloss);
        }

        // Kenar çizgisi; seçiliyse kalın sarı çerçeve
        Color border = IsSelected ? Color.FromArgb(255, 220, 60) : DrawingHelper.Darken(BaseColor, 0.45f);
        using (var pen = new Pen(DrawingHelper.WithAlpha(border, alpha), IsSelected ? 4f : 2.5f))
        {
            g.DrawPath(pen, path);
        }

        DrawingHelper.DrawOutlinedText(g, Text, FontSize,
            new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f + 1),
            DrawingHelper.WithAlpha(Color.White, alpha),
            DrawingHelper.WithAlpha(DrawingHelper.Darken(BaseColor, 0.6f), alpha), 4f, shadow: false);
    }
}
