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
        using var fill = new SolidBrush(Color.FromArgb(46, 190, 110));
        using var outline = new Pen(Color.FromArgb(20, 90, 50), 2.5f);
        g.FillRectangle(fill, X, Y, Width, Height);
        g.DrawRectangle(outline, X, Y, Width, Height);
        RectangleF cap = CapRect;
        g.FillRectangle(fill, cap);
        g.DrawRectangle(outline, cap.X, cap.Y, cap.Width, cap.Height);
    }
}
