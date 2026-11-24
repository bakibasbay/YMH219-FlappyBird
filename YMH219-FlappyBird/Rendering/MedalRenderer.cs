using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Bronz / gümüş / altın madalyayı tamamen kodla çizer:
/// kurdele, ışıltılı disk, iç halka, ortada yıldız ve parlayan pırıltılar.
/// </summary>
public static class MedalRenderer
{
    /// <summary>Madalyanın Türkçe adı.</summary>
    public static string GetName(Medal medal) => medal switch
    {
        Medal.Gold => "ALTIN",
        Medal.Silver => "GÜMÜŞ",
        Medal.Bronze => "BRONZ",
        _ => "YOK"
    };

    /// <summary>Madalya renkleri: (ana, açık, koyu).</summary>
    private static (Color Main, Color Light, Color Dark) GetColors(Medal medal) => medal switch
    {
        Medal.Gold => (Color.FromArgb(250, 195, 35), Color.FromArgb(255, 245, 160), Color.FromArgb(160, 105, 10)),
        Medal.Silver => (Color.FromArgb(190, 195, 210), Color.FromArgb(250, 252, 255), Color.FromArgb(105, 110, 130)),
        _ => (Color.FromArgb(205, 125, 55), Color.FromArgb(245, 185, 120), Color.FromArgb(115, 60, 25))
    };

    /// <param name="center">Madalyanın merkezi.</param>
    /// <param name="radius">Disk yarıçapı.</param>
    /// <param name="time">Pırıltı animasyonu için geçen süre.</param>
    public static void Draw(Graphics g, PointF center, float radius, Medal medal, float time)
    {
        if (radius < 1f) return;

        if (medal == Medal.None)
        {
            DrawEmptySlot(g, center, radius);
            return;
        }

        var (main, light, dark) = GetColors(medal);
        DrawRibbon(g, center, radius);

        // Disk: merkezde açık, kenara doğru ana renk (ışıltılı metal görünümü)
        var disk = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(disk);
            using var brush = new PathGradientBrush(path)
            {
                CenterPoint = new PointF(center.X - radius * 0.3f, center.Y - radius * 0.3f),
                CenterColor = light,
                SurroundColors = new[] { main }
            };
            g.FillPath(brush, path);
        }
        using (var rim = new Pen(dark, 3f))
            g.DrawEllipse(rim, disk);

        // İç halka
        float ir = radius * 0.74f;
        using (var inner = new Pen(Color.FromArgb(150, dark), 2f))
            g.DrawEllipse(inner, center.X - ir, center.Y - ir, ir * 2, ir * 2);

        DrawStar(g, center, radius * 0.48f, light, dark);

        // Sol üstte yansıma parlaması
        using (var shine = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
            g.FillEllipse(shine, center.X - radius * 0.65f, center.Y - radius * 0.7f, radius * 0.55f, radius * 0.35f);

        DrawSparkles(g, center, radius, time);
    }

    /// <summary>Madalya kazanılmadıysa boş yuva.</summary>
    private static void DrawEmptySlot(Graphics g, PointF c, float r)
    {
        using (var fill = new SolidBrush(Color.FromArgb(235, 215, 175)))
            g.FillEllipse(fill, c.X - r, c.Y - r, r * 2, r * 2);
        using (var pen = new Pen(Color.FromArgb(200, 165, 115), 3f) { DashStyle = DashStyle.Dash })
            g.DrawEllipse(pen, c.X - r, c.Y - r, r * 2, r * 2);
        DrawingHelper.DrawOutlinedText(g, "?", r, c, Color.FromArgb(200, 165, 115), Color.Empty, 0f, shadow: false);
    }

    /// <summary>Diskin arkasından aşağı sarkan iki renkli kurdele.</summary>
    private static void DrawRibbon(Graphics g, PointF c, float r)
    {
        using var red = new SolidBrush(Color.FromArgb(220, 60, 70));
        using var blue = new SolidBrush(Color.FromArgb(50, 110, 200));
        using var edge = new Pen(Color.FromArgb(90, 30, 40), 2f) { LineJoin = LineJoin.Round };
        PointF[] left =
        {
            new(c.X - r * 0.55f, c.Y), new(c.X - r * 0.05f, c.Y),
            new(c.X - r * 0.35f, c.Y + r * 1.35f), new(c.X - r * 0.6f, c.Y + r * 1.15f), new(c.X - r * 0.85f, c.Y + r * 1.35f)
        };
        PointF[] right =
        {
            new(c.X + r * 0.05f, c.Y), new(c.X + r * 0.55f, c.Y),
            new(c.X + r * 0.85f, c.Y + r * 1.35f), new(c.X + r * 0.6f, c.Y + r * 1.15f), new(c.X + r * 0.35f, c.Y + r * 1.35f)
        };
        g.FillPolygon(red, left);
        g.DrawPolygon(edge, left);
        g.FillPolygon(blue, right);
        g.DrawPolygon(edge, right);
    }

    private static void DrawStar(Graphics g, PointF c, float outer, Color fill, Color outline)
    {
        float inner = outer * 0.45f;
        var points = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            float rr = i % 2 == 0 ? outer : inner;
            float a = (i * 36f - 90f) * MathF.PI / 180f;
            points[i] = new PointF(c.X + MathF.Cos(a) * rr, c.Y + MathF.Sin(a) * rr);
        }
        using var b = new SolidBrush(fill);
        using var p = new Pen(outline, 2f) { LineJoin = LineJoin.Round };
        g.FillPolygon(b, points);
        g.DrawPolygon(p, points);
    }

    /// <summary>Disk kenarında sırayla parlayıp sönen dört köşeli pırıltılar.</summary>
    private static void DrawSparkles(Graphics g, PointF c, float r, float time)
    {
        for (int i = 0; i < 3; i++)
        {
            float phase = time * 2.2f + i * 2.1f;
            float k = MathF.Max(0, MathF.Sin(phase));
            if (k <= 0.05f) continue;
            float angle = -2.2f + i * 1.4f;
            float x = c.X + MathF.Cos(angle) * r * 0.85f;
            float y = c.Y + MathF.Sin(angle) * r * 0.85f;
            float s = 9f * k;
            PointF[] sparkle =
            {
                new(x, y - s), new(x + s * 0.25f, y - s * 0.25f), new(x + s, y), new(x + s * 0.25f, y + s * 0.25f),
                new(x, y + s), new(x - s * 0.25f, y + s * 0.25f), new(x - s, y), new(x - s * 0.25f, y - s * 0.25f)
            };
            using var b = new SolidBrush(Color.FromArgb((int)(255 * k), 255, 255, 255));
            g.FillPolygon(b, sparkle);
        }
    }
}
