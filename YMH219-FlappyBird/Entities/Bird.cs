using FlappyBird.Core;

namespace FlappyBird.Entities;

/// <summary>Kuşun hareket modu.</summary>
public enum BirdMode
{
    /// <summary>Menü/Hazır ekranında yerinde süzülür, yer çekimi yok.</summary>
    Hovering,
    /// <summary>Oyun sırasında: yer çekimi ve zıplama aktif.</summary>
    Flying,
    /// <summary>Çarptı: zemine düşer ve orada kalır.</summary>
    Dead
}

/// <summary>
/// Oyuncunun kontrol ettiği kuş. Yer çekimi ile düşer, Jump() ile yukarı zıplar.
/// </summary>
public class Bird : GameObject
{
    public const float DefaultWidth = 44f;
    public const float DefaultHeight = 34f;

    /// <summary>Dikey hız (px/s). Negatif değer yukarı hareket demektir.</summary>
    public float VelocityY { get; private set; }

    public BirdMode Mode { get; private set; } = BirdMode.Hovering;

    // Fizik değerleri GameSettings'ten atanır (zorluğa göre değişir)
    public float Gravity { get; set; } = 1500f;
    public float JumpVelocity { get; set; } = -430f;
    public float MaxFallSpeed { get; set; } = 700f;

    /// <summary>Kuşun düşebileceği en alt nokta (zeminin üstü).</summary>
    public float GroundY { get; set; } = GameSettings.GroundY;

    public bool IsOnGround => Bottom >= GroundY - 0.5f;

    /// <summary>
    /// Çarpışma kutusu: çizilen gövdeden biraz küçük tutulur,
    /// böylece borunun kenarını "sıyırmak" oyuncuya adil hissettirir.
    /// </summary>
    public RectangleF Hitbox
    {
        get
        {
            RectangleF r = Bounds;
            r.Inflate(-6f, -5f);
            return r;
        }
    }

    public Bird(float x, float y)
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        ResetTo(x, y);
    }

    /// <summary>Kuşu başlangıç konumuna alır ve süzülme moduna geçirir.</summary>
    public void ResetTo(float x, float y)
    {
        X = x;
        Y = y;
        VelocityY = 0;
        Mode = BirdMode.Hovering;
    }

    /// <summary>Yukarı doğru anlık hız verir.</summary>
    public void Jump()
    {
        if (Mode == BirdMode.Dead) return;
        Mode = BirdMode.Flying;
        VelocityY = JumpVelocity;
    }

    /// <summary>Kuş çarptı: küçük bir sekmeyle düşmeye başlar, artık zıplayamaz.</summary>
    public void Die()
    {
        if (Mode == BirdMode.Dead) return;
        Mode = BirdMode.Dead;
        if (!IsOnGround)
            VelocityY = -180f;
    }

    public override void Update(float dt)
    {
        if (Mode == BirdMode.Hovering) return;

        // Yer çekimi: hız ivme ile artar, konum hız ile değişir
        VelocityY = Math.Min(VelocityY + Gravity * dt, MaxFallSpeed);
        Y += VelocityY * dt;

        // Tavanın çok üstüne çıkmasın (oyun yine de Game Over olur)
        if (Y < -Height)
            Y = -Height;

        // Zemine değince orada dur
        if (Bottom > GroundY)
        {
            Y = GroundY - Height;
            VelocityY = 0;
        }
    }

    public override void Draw(Graphics g)
    {
        using var body = new SolidBrush(Color.FromArgb(255, 150, 60));
        g.FillEllipse(body, X, Y, Width, Height);
        g.FillEllipse(Brushes.White, X + Width * 0.55f, Y + 6, 12, 12);
        g.FillEllipse(Brushes.Black, X + Width * 0.55f + 6, Y + 10, 5, 5);
    }
}
