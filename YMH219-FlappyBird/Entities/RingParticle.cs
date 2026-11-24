using FlappyBird.Rendering;

namespace FlappyBird.Entities;

/// <summary>
/// Dışa doğru genişleyip incelen halka (şok dalgası). Hareket etmez.
/// </summary>
public class RingParticle : Particle
{
    public RingParticle(float x, float y, float life, float maxRadius, Color color)
        : base(x, y, 0, 0, life, maxRadius, color)
    {
    }

    public override void Draw(Graphics g)
    {
        float t = LifeRatio;
        float r = Size * DrawingHelper.EaseOutCubic(1f - t);
        if (r < 1f) return;
        using var pen = new Pen(Color.FromArgb((int)(Color.A * t), Color), 1f + 5f * t);
        g.DrawEllipse(pen, X - r, Y - r, r * 2, r * 2);
    }
}
