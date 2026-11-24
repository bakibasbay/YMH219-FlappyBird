namespace FlappyBird.Entities;

/// <summary>
/// Kısa ömürlü görsel parçacık (toz, kıvılcım). Diğer parçacık türleri
/// bu sınıftan türer ve Draw/Update metotlarını ezer (override → polimorfizm).
/// Not: Parçacıklarda X, Y nesnenin merkezini gösterir.
/// </summary>
public class Particle : GameObject
{
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }

    /// <summary>Kalan ömür (saniye).</summary>
    public float Life { get; protected set; }

    /// <summary>Toplam ömür (saniye).</summary>
    public float MaxLife { get; }

    public Color Color { get; set; }

    /// <summary>Başlangıç çapı (piksel).</summary>
    public float Size { get; set; }

    /// <summary>Parçacığa etki eden yer çekimi (px/s²).</summary>
    public float Gravity { get; set; }

    /// <summary>Hava sürtünmesi: her saniye hızın ne kadarının kaybolacağı.</summary>
    public float Drag { get; set; } = 1.5f;

    /// <summary>Dönme açısı (derece) ve dönme hızı (derece/s).</summary>
    public float Rotation { get; set; }
    public float Spin { get; set; }

    /// <summary>Ömrün ne kadarının kaldığı: 1 = yeni doğdu, 0 = bitti.</summary>
    public float LifeRatio => MaxLife <= 0 ? 0 : Math.Max(0, Life / MaxLife);

    public Particle(float x, float y, float vx, float vy, float life, float size, Color color)
    {
        X = x;
        Y = y;
        VelocityX = vx;
        VelocityY = vy;
        Life = MaxLife = life;
        Size = size;
        Width = Height = size;
        Color = color;
    }

    public override void Update(float dt)
    {
        Life -= dt;
        if (Life <= 0)
        {
            IsActive = false; // ParticleSystem bu parçacığı listeden çıkarır
            return;
        }

        VelocityY += Gravity * dt;
        float damping = Math.Max(0f, 1f - Drag * dt);
        VelocityX *= damping;
        VelocityY *= damping;

        X += VelocityX * dt;
        Y += VelocityY * dt;
        Rotation += Spin * dt;
    }

    /// <summary>Varsayılan görünüm: küçülerek ve saydamlaşarak kaybolan daire.</summary>
    public override void Draw(Graphics g)
    {
        float t = LifeRatio;
        float s = Size * (0.4f + 0.6f * t);
        using var brush = new SolidBrush(Color.FromArgb((int)(Color.A * t), Color));
        g.FillEllipse(brush, X - s / 2, Y - s / 2, s, s);
    }
}
