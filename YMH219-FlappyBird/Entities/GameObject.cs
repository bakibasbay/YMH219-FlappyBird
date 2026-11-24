namespace FlappyBird.Entities;

/// <summary>
/// Oyundaki tüm nesnelerin soyut (abstract) temel sınıfı.
/// Konum, boyut ve sınır kutusunu tutar; her alt sınıf kendi
/// Update (hareket) ve Draw (çizim) davranışını tanımlar (polimorfizm).
/// </summary>
public abstract class GameObject
{
    /// <summary>Sol üst köşenin X koordinatı.</summary>
    public float X { get; set; }

    /// <summary>Sol üst köşenin Y koordinatı.</summary>
    public float Y { get; set; }

    public float Width { get; set; }
    public float Height { get; set; }

    /// <summary>Nesne hâlâ oyunda mı? (Ör. ömrü biten parçacık false olur.)</summary>
    public bool IsActive { get; set; } = true;

    public float CenterX => X + Width / 2f;
    public float CenterY => Y + Height / 2f;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    /// <summary>Nesnenin kapladığı dikdörtgen (çarpışma için temel kutu).</summary>
    public virtual RectangleF Bounds => new(X, Y, Width, Height);

    /// <summary>Nesneyi dt saniye kadar ilerletir.</summary>
    public abstract void Update(float dt);

    /// <summary>Nesneyi ekrana çizer.</summary>
    public abstract void Draw(Graphics g);
}
