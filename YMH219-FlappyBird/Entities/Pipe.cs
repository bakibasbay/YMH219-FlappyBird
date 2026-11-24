using System.Drawing.Drawing2D;
using FlappyBird.Rendering;

namespace FlappyBird.Entities;

/// <summary>
/// Tek bir boru (üst ya da alt). Gövde + ağız kısmından (kapak) oluşur.
/// Hareketini ait olduğu PipePair yönetir.
/// </summary>
public class Pipe : GameObject
{
    public const float PipeWidth = 78f;
    public const float CapHeight = 28f;
    /// <summary>Kapağın gövdeden her iki yana taşma miktarı.</summary>
    public const float CapOverhang = 5f;

    /// <summary>true: yukarıdan sarkan boru, false: yerden yükselen boru.</summary>
    public bool IsTop { get; }

    public Pipe(bool isTop)
    {
        IsTop = isTop;
        Width = PipeWidth;
    }

    /// <summary>Borunun ağız kısmı (boşluğa bakan uçtaki geniş kapak).</summary>
    public RectangleF CapRect => IsTop
        ? new RectangleF(X - CapOverhang, Bottom - CapHeight, Width + 2 * CapOverhang, CapHeight)
        : new RectangleF(X - CapOverhang, Y, Width + 2 * CapOverhang, CapHeight);

    /// <summary>Verilen kutu gövdeye veya kapağa değiyor mu?</summary>
    public bool Intersects(RectangleF box) =>
        Bounds.IntersectsWith(box) || CapRect.IntersectsWith(box);

    /// <summary>Boru kendi başına hareket etmez; konumunu PipePair günceller.</summary>
    public override void Update(float dt) { }

    public override void Draw(Graphics g)
    {
        RectangleF body = Bounds;
        RectangleF cap = CapRect;

        // 1) Arka plana düşen gölge (borunun sağında yarı saydam şerit)
        using (var cast = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
        {
            g.FillRectangle(cast, body.Right, body.Y, 8, body.Height);
            g.FillRectangle(cast, cap.Right, cap.Y + 4, 6, cap.Height);
        }

        // 2) Gövde: silindir görünümü için yatay çok renkli gradyan
        using (var bodyBrush = CreateCylinderBrush(body))
            g.FillRectangle(bodyBrush, body);

        // Parlak dikey şerit (ışık yansıması)
        using (var shine = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
            g.FillRectangle(shine, body.X + 10, body.Y, 5, body.Height);

        // Kapağın gövde üzerine düşürdüğü gölge
        using (var capShadow = new SolidBrush(Color.FromArgb(60, 0, 40, 10)))
        {
            float sy = IsTop ? cap.Y - 8 : cap.Bottom;
            g.FillRectangle(capShadow, body.X, sy, body.Width, 8);
        }

        using var outline = new Pen(Color.FromArgb(25, 85, 45), 2.5f) { LineJoin = LineJoin.Round };
        g.DrawRectangle(outline, body.X, body.Y, body.Width, body.Height);

        // 3) Ağız kısmı (kapak): biraz daha geniş, yuvarlak köşeli
        using (GraphicsPath capPath = DrawingHelper.RoundedRect(cap, 4f))
        {
            using (var capBrush = CreateCylinderBrush(cap))
                g.FillPath(capBrush, capPath);
            using (var capShine = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                g.FillRectangle(capShine, cap.X + 9, cap.Y + 3, 6, cap.Height - 6);
            g.DrawPath(outline, capPath);
        }
    }

    /// <summary>
    /// Soldan sağa: koyu kenar → parlak → orta → koyu kenar. Boruya yuvarlak (silindir) görünüm verir.
    /// </summary>
    private static LinearGradientBrush CreateCylinderBrush(RectangleF r)
    {
        var brush = new LinearGradientBrush(r, Color.Black, Color.Black, LinearGradientMode.Horizontal);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = new[]
            {
                Color.FromArgb(40, 135, 70),
                Color.FromArgb(160, 240, 140),
                Color.FromArgb(85, 200, 105),
                Color.FromArgb(55, 160, 85),
                Color.FromArgb(30, 110, 60)
            },
            Positions = new[] { 0f, 0.22f, 0.5f, 0.8f, 1f }
        };
        return brush;
    }
}
