namespace FlappyBird.Entities;

/// <summary>
/// Kuştan kopan tüy: yavaşça düşerken sağa-sola salınır.
/// Seviye atlarken renkli konfeti olarak da kullanılır.
/// </summary>
public class FeatherParticle : Particle
{
    private readonly float _swayPhase = (float)(Random.Shared.NextDouble() * MathF.Tau);
    private float _age;

    public FeatherParticle(float x, float y, float vx, float vy, float life, float size, Color color)
        : base(x, y, vx, vy, life, size, color)
    {
        Gravity = 260f;
        Drag = 2.5f;
    }

    /// <summary>Taban sınıfın fiziğine ek olarak yaprak gibi salınma hareketi.</summary>
    public override void Update(float dt)
    {
        base.Update(dt);
        _age += dt;
        X += MathF.Sin(_age * 6f + _swayPhase) * 28f * dt;
        Rotation = MathF.Sin(_age * 5f + _swayPhase) * 45f;
    }

    public override void Draw(Graphics g)
    {
        float t = LifeRatio;
        int alpha = (int)(255 * Math.Min(1f, t * 2f));
        var state = g.Save();
        g.TranslateTransform(X, Y);
        g.RotateTransform(Rotation);

        using (var brush = new SolidBrush(Color.FromArgb(alpha, Color)))
            g.FillEllipse(brush, -Size / 2, -Size / 5, Size, Size * 0.4f);
        using (var quill = new Pen(Color.FromArgb(alpha / 2, 90, 40, 20), 1f))
            g.DrawLine(quill, -Size / 2, 0, Size / 2, 0);

        g.Restore(state);
    }
}
