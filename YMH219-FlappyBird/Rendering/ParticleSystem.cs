using FlappyBird.Core;
using FlappyBird.Entities;

namespace FlappyBird.Rendering;

/// <summary>
/// Tüm parçacıkları tek listede tutar, günceller ve çizer.
/// Liste Particle türünde olduğu için yıldız, tüy, halka, yazı gibi alt sınıflar
/// aynı döngüde kendi Draw metotlarıyla çizilir (polimorfizm).
/// </summary>
public class ParticleSystem
{
    /// <summary>Performans için aynı anda en fazla parçacık sayısı.</summary>
    public const int MaxParticles = 400;

    private readonly List<Particle> _particles = new();
    private readonly Random _rng = new();

    public int Count => _particles.Count;

    public void Add(Particle p)
    {
        if (_particles.Count < MaxParticles)
            _particles.Add(p);
    }

    public void Clear() => _particles.Clear();

    private float Rand(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

    /// <summary>Zıplarken kuşun arkasından çıkan küçük toz ve ara sıra tüy.</summary>
    public void EmitJumpDust(float x, float y, Color featherColor)
    {
        for (int i = 0; i < 7; i++)
        {
            Add(new Particle(x + Rand(-4, 4), y + Rand(-3, 6), Rand(-140, -60), Rand(40, 130),
                Rand(0.3f, 0.5f), Rand(5, 10), Color.FromArgb(210, 255, 255, 255)));
        }
        if (_rng.NextDouble() < 0.5)
            Add(new FeatherParticle(x, y, Rand(-90, -40), Rand(-20, 40), Rand(0.6f, 0.9f), Rand(9, 12), featherColor));
    }

    /// <summary>Skor alınınca yıldız patlaması, altın halka ve "+1" yazısı.</summary>
    public void EmitScoreSparkle(float x, float y)
    {
        Color[] colors = { Color.FromArgb(255, 230, 80), Color.FromArgb(255, 255, 190), Color.FromArgb(255, 180, 60) };
        for (int i = 0; i < 9; i++)
        {
            float angle = i / 9f * MathF.Tau + Rand(-0.2f, 0.2f);
            float speed = Rand(120, 220);
            Add(new StarParticle(x, y, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed,
                Rand(0.45f, 0.7f), Rand(10, 16), colors[i % colors.Length]));
        }
        Add(new RingParticle(x, y, 0.4f, 38f, Color.FromArgb(220, 255, 225, 120)));
        Add(new TextParticle(x + 18, y - 20, "+1", 26f, Color.FromArgb(255, 240, 110)));
    }

    /// <summary>Çarpışma patlaması: kıvılcımlar, kuştan kopan tüyler ve şok halkası.</summary>
    public void EmitExplosion(float x, float y, Color featherColor)
    {
        Color[] sparks = { Color.FromArgb(255, 245, 200), Color.FromArgb(255, 200, 60), Color.FromArgb(255, 120, 40), Color.FromArgb(240, 70, 40) };
        for (int i = 0; i < 26; i++)
        {
            float angle = Rand(0, MathF.Tau);
            float speed = Rand(120, 380);
            Add(new Particle(x, y, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed,
                Rand(0.35f, 0.75f), Rand(4, 10), sparks[_rng.Next(sparks.Length)]) { Gravity = 380f, Drag = 2.2f });
        }
        for (int i = 0; i < 10; i++)
        {
            Add(new FeatherParticle(x + Rand(-8, 8), y + Rand(-8, 8), Rand(-160, 160), Rand(-220, -40),
                Rand(1.0f, 1.6f), Rand(10, 15), i % 3 == 0 ? Color.FromArgb(255, 238, 200) : featherColor));
        }
        Add(new RingParticle(x, y, 0.45f, 70f, Color.FromArgb(230, 255, 255, 255)));
    }

    /// <summary>Zemine çarpınca yere yayılan toz bulutu.</summary>
    public void EmitGroundDust(float x, float y)
    {
        for (int i = 0; i < 12; i++)
        {
            float dir = i % 2 == 0 ? 1 : -1;
            Add(new Particle(x + Rand(-10, 10), y - Rand(0, 6), dir * Rand(40, 160), Rand(-90, -20),
                Rand(0.4f, 0.7f), Rand(7, 13), Color.FromArgb(200, 225, 205, 160)) { Gravity = 150f });
        }
    }

    /// <summary>Seviye atlarken ekranın üstünden yağan renkli konfeti.</summary>
    public void EmitConfetti()
    {
        Color[] colors =
        {
            Color.FromArgb(255, 90, 90), Color.FromArgb(255, 210, 60), Color.FromArgb(90, 210, 120),
            Color.FromArgb(80, 170, 255), Color.FromArgb(200, 110, 255)
        };
        for (int i = 0; i < 40; i++)
        {
            Add(new FeatherParticle(Rand(0, GameSettings.WorldWidth), Rand(-40, 0), Rand(-30, 30), Rand(60, 160),
                Rand(1.4f, 2.2f), Rand(8, 12), colors[i % colors.Length]) { Gravity = 120f, Drag = 0.8f });
        }
    }

    /// <summary>Tüm parçacıkları ilerletir, ömrü bitenleri listeden çıkarır.</summary>
    public void Update(float dt)
    {
        foreach (Particle p in _particles)
            p.Update(dt);
        _particles.RemoveAll(p => !p.IsActive);
    }

    public void Draw(Graphics g)
    {
        foreach (Particle p in _particles)
            p.Draw(g); // sanal (virtual) çağrı: her tür kendi şeklini çizer
    }
}
