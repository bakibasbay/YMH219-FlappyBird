using FlappyBird.Core;
using FlappyBird.Rendering;

namespace FlappyBird.Entities;

/// <summary>
/// Gökyüzünde süzülen bulut. Birkaç üst üste binen daireden oluşur.
/// Derinliği (Depth) büyük olan bulut daha yakın: daha büyük, daha hızlı, daha opak.
/// </summary>
public class Cloud : GameObject
{
    /// <summary>0.4 (uzak) .. 1.0 (yakın) arası derinlik.</summary>
    public float Depth { get; private set; }

    /// <summary>Zeminin kayma hızı; bulut bunun bir kısmıyla kayar (parallax).</summary>
    public float ScrollSpeed { get; set; }

    /// <summary>Gün saatine göre bulut rengi (gün batımında pembe, gece koyu).</summary>
    public Color Tint { get; set; } = Color.White;

    // Bulutu oluşturan daireler: (göreli x, göreli y, yarıçap)
    private (float X, float Y, float R)[] _puffs = Array.Empty<(float, float, float)>();

    public Cloud(float x)
    {
        Respawn(x);
    }

    /// <summary>Bulutu verilen X konumunda rastgele şekil ve yükseklikle yeniden oluşturur.</summary>
    private void Respawn(float x)
    {
        Random rng = Random.Shared;
        Depth = 0.4f + (float)rng.NextDouble() * 0.6f;
        X = x;
        Y = 30f + (float)rng.NextDouble() * 230f;

        int count = rng.Next(4, 7);
        float baseR = 16f + 10f * Depth;
        _puffs = new (float, float, float)[count];
        float px = 0;
        for (int i = 0; i < count; i++)
        {
            // Ortadaki daireler daha büyük ve yukarıda: kabarık bulut silüeti
            float middle = 1f - Math.Abs(i - (count - 1) / 2f) / count;
            float r = baseR * (0.7f + 0.6f * middle) * (0.85f + 0.3f * (float)rng.NextDouble());
            _puffs[i] = (px + r, baseR * 1.2f - r * 0.9f, r);
            px += r * 1.15f;
        }
        Width = px + baseR;
        Height = baseR * 2.4f;
    }

    public override void Update(float dt)
    {
        // Rüzgâr (sabit) + zemin hızının derinliğe bağlı bir kısmı
        X -= (8f + ScrollSpeed * (0.15f + 0.3f * Depth)) * dt;

        // Ekrandan çıkınca sağdan yeniden doğ
        if (Right < 0)
            Respawn(GameSettings.WorldWidth + (float)Random.Shared.NextDouble() * 120f);
    }

    public override void Draw(Graphics g)
    {
        float alpha = 0.55f + 0.4f * Depth;
        using var shade = new SolidBrush(DrawingHelper.WithAlpha(DrawingHelper.Darken(Tint, 0.12f), alpha));
        using var fill = new SolidBrush(DrawingHelper.WithAlpha(Tint, alpha));

        // Daireler tek bir yol (path) olarak birleştirilir; Winding kipi sayesinde
        // üst üste binen yerler iki kez boyanmaz, yarı saydam bulut temiz görünür
        using var path = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
        foreach (var (px, py, r) in _puffs)
            path.AddEllipse(X + px - r, Y + py - r, r * 2, r * 2);

        // Önce aşağı kaydırılmış koyu gölge, sonra asıl bulut: hacim hissi
        var state = g.Save();
        g.TranslateTransform(0, 4);
        g.FillPath(shade, path);
        g.Restore(state);
        g.FillPath(fill, path);
    }
}
