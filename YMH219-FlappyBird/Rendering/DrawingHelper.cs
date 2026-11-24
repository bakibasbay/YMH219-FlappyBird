using System.Drawing.Drawing2D;

namespace FlappyBird.Rendering;

/// <summary>
/// Ortak çizim yardımcıları: konturlu yazı, yuvarlak köşeli dikdörtgen,
/// renk geçişi ve animasyon yumuşatma (easing) fonksiyonları.
/// </summary>
public static class DrawingHelper
{
    /// <summary>Başlık ve skor için kalın yazı ailesi.</summary>
    public static FontFamily HeavyFamily { get; }
    public static FontStyle HeavyStyle { get; }

    /// <summary>Normal arayüz yazıları için yazı ailesi.</summary>
    public static FontFamily UiFamily { get; }

    static DrawingHelper()
    {
        // Segoe UI Black yoksa Segoe UI Bold, o da yoksa sistemin varsayılanı kullanılır
        FontFamily? black = TryFamily("Segoe UI Black");
        FontFamily? segoe = TryFamily("Segoe UI");
        UiFamily = segoe ?? FontFamily.GenericSansSerif;
        HeavyFamily = black ?? UiFamily;
        HeavyStyle = black != null ? FontStyle.Regular : FontStyle.Bold;
    }

    private static FontFamily? TryFamily(string name)
    {
        try
        {
            var family = new FontFamily(name);
            return family.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ? family : null;
        }
        catch (ArgumentException)
        {
            return null; // Yazı tipi bu bilgisayarda yüklü değil
        }
    }

    /// <summary>Yuvarlak köşeli dikdörtgen yolu oluşturur.</summary>
    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0.5f)
        {
            path.AddRectangle(r);
            return path;
        }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Yazıyı vektörel yol olarak oluşturur (ölçekte bulanıklaşmaz).</summary>
    public static GraphicsPath CreateTextPath(string text, float size, PointF anchor,
        bool heavy = true, StringAlignment align = StringAlignment.Center)
    {
        var path = new GraphicsPath();
        using var format = new StringFormat
        {
            Alignment = align,
            LineAlignment = StringAlignment.Center
        };
        FontFamily family = heavy ? HeavyFamily : UiFamily;
        FontStyle style = heavy ? HeavyStyle : FontStyle.Bold;
        path.AddString(text, family, (int)style, size, anchor, format);
        return path;
    }

    /// <summary>
    /// Kalın konturlu (dış çizgili) yazı çizer. anchor: yazının hizalama noktası.
    /// </summary>
    public static void DrawOutlinedText(Graphics g, string text, float size, PointF anchor,
        Color fill, Color outline, float outlineWidth, bool heavy = true,
        StringAlignment align = StringAlignment.Center, bool shadow = true)
    {
        using GraphicsPath path = CreateTextPath(text, size, anchor, heavy, align);
        using var brush = new SolidBrush(fill);
        DrawOutlinedPath(g, path, brush, outline, outlineWidth, shadow);
    }

    /// <summary>Hazır bir yazı yolunu gölge + kontur + dolgu ile çizer.</summary>
    public static void DrawOutlinedPath(Graphics g, GraphicsPath path, Brush fill,
        Color outline, float outlineWidth, bool shadow = true)
    {
        if (shadow && outline.A > 0)
        {
            // Yazının altına hafif kaydırılmış koyu gölge
            using var shadowPath = (GraphicsPath)path.Clone();
            using var m = new Matrix();
            m.Translate(0, outlineWidth * 0.8f);
            shadowPath.Transform(m);
            using var shadowPen = new Pen(Color.FromArgb(outline.A / 2, 0, 0, 0), outlineWidth) { LineJoin = LineJoin.Round };
            g.DrawPath(shadowPen, shadowPath);
        }

        if (outlineWidth > 0)
        {
            using var pen = new Pen(outline, outlineWidth) { LineJoin = LineJoin.Round };
            g.DrawPath(pen, path);
        }
        g.FillPath(fill, path);
    }

    /// <summary>İki renk arasında t (0..1) oranında geçiş yapar.</summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Clamp01(t);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    /// <summary>Rengin saydamlığını 0..1 aralığındaki bir oranla çarpar.</summary>
    public static Color WithAlpha(Color c, float alpha) =>
        Color.FromArgb((int)(c.A * Clamp01(alpha)), c.R, c.G, c.B);

    /// <summary>Rengi açar (amount: 0..1).</summary>
    public static Color Lighten(Color c, float amount) => Lerp(c, Color.FromArgb(c.A, 255, 255, 255), amount);

    /// <summary>Rengi koyulaştırır (amount: 0..1).</summary>
    public static Color Darken(Color c, float amount) => Lerp(c, Color.FromArgb(c.A, 0, 0, 0), amount);

    public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

    /// <summary>Yavaşlayarak duran geçiş (0..1 → 0..1).</summary>
    public static float EaseOutCubic(float t)
    {
        t = Clamp01(t);
        float u = 1 - t;
        return 1 - u * u * u;
    }

    /// <summary>Hedefi biraz geçip geri gelen "yaylı" geçiş.</summary>
    public static float EaseOutBack(float t)
    {
        t = Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        float u = t - 1;
        return 1 + c3 * u * u * u + c1 * u * u;
    }
}
