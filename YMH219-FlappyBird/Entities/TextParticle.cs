using FlappyBird.Rendering;

namespace FlappyBird.Entities;

/// <summary>
/// Yukarı süzülerek kaybolan yazı (ör. skor alınınca "+1").
/// </summary>
public class TextParticle : Particle
{
    public string Text { get; }

    public TextParticle(float x, float y, string text, float size, Color color)
        : base(x, y, 0, -70f, 0.8f, size, color)
    {
        Text = text;
        Drag = 1.2f;
    }

    public override void Draw(Graphics g)
    {
        float t = LifeRatio;
        float alpha = Math.Min(1f, t * 2f);
        DrawingHelper.DrawOutlinedText(g, Text, Size, new PointF(X, Y),
            DrawingHelper.WithAlpha(Color, alpha), Color.FromArgb((int)(200 * alpha), 60, 40, 10), 4f, shadow: false);
    }
}
