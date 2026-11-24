using System.Drawing.Drawing2D;

namespace FlappyBird.Entities;

/// <summary>
/// Skor alındığında saçılan, dönerek parlayan yıldız parçacığı.
/// </summary>
public class StarParticle : Particle
{
    public StarParticle(float x, float y, float vx, float vy, float life, float size, Color color)
        : base(x, y, vx, vy, life, size, color)
    {
        Drag = 3f;
        Spin = Random.Shared.Next(-360, 360);
    }

    /// <summary>Beş köşeli yıldız: önce hızla büyür, sonra küçülerek söner.</summary>
    public override void Draw(Graphics g)
    {
        float t = LifeRatio;
        // Doğduktan hemen sonra "pop" etkisi: 0→1.2→1 arası büyüme
        float grow = t > 0.8f ? (1f - t) / 0.2f * 1.2f : 0.4f + 0.8f * (t / 0.8f);
        float outer = Size * grow / 2f;
        if (outer < 0.5f) return;
        float inner = outer * 0.45f;

        var points = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? outer : inner;
            float a = (Rotation + i * 36f - 90f) * MathF.PI / 180f;
            points[i] = new PointF(X + MathF.Cos(a) * r, Y + MathF.Sin(a) * r);
        }

        int alpha = (int)(255 * Math.Min(1f, t * 1.5f));
        using (var glow = new SolidBrush(Color.FromArgb(alpha / 4, Color)))
            g.FillEllipse(glow, X - outer * 1.3f, Y - outer * 1.3f, outer * 2.6f, outer * 2.6f);
        using (var brush = new SolidBrush(Color.FromArgb(alpha, Color)))
            g.FillPolygon(brush, points);
        using (var pen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 1f) { LineJoin = LineJoin.Round })
            g.DrawPolygon(pen, points);
    }
}
