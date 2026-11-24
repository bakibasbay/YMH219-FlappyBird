using FlappyBird.Entities;

namespace FlappyBird.Core;

/// <summary>
/// Demo / test amaçlı otopilot (F2). Kural basittir: kuşun alt kenarı hedef
/// çizginin altına inmek üzereyse zıpla. Hedef çizgi, önümüzdeki boşluğun alt
/// kenarının biraz üstüdür; ama zıplayınca üst boruya çarpmayacak kadar aşağıda tutulur.
/// Otopilotla yapılan skor rekor sayılmaz.
/// </summary>
public class Autopilot
{
    /// <summary>Otopilot açık mı?</summary>
    public bool IsEnabled { get; set; }

    private const float Margin = 10f;      // boşluk kenarlarına bırakılan pay
    private const float LookAhead = 2f / 60f; // iki kare sonrasına göre karar ver

    /// <summary>Bu karede zıplanmalı mı?</summary>
    public bool ShouldJump(Bird bird, IReadOnlyList<PipePair> pipes, float dt)
    {
        if (!IsEnabled) return false;

        // Az önce zıpladıysa (hâlâ hızla yükseliyorsa) tekrar zıplama
        if (bird.VelocityY < bird.JumpVelocity * 0.3f) return false;

        float predictedBottom = bird.Bottom + bird.VelocityY * LookAhead;
        if (predictedBottom <= GetJumpLine(bird, pipes))
            return false;

        // Sonraki boşluk daha aşağıdaysa ve kuş zıplamadan da bu borudan
        // güvenle çıkabiliyorsa zıplama: düşmeye devam etsin
        return !CanFallOutOfPipe(bird, pipes);
    }

    /// <summary>
    /// Kuş şu an bir borunun içindeyse, zıplamadan düşerek borunun alt kenarına
    /// değmeden çıkabilir mi? (Yalnızca sonraki boşluk daha aşağıdaysa işe yarar.)
    /// </summary>
    private static bool CanFallOutOfPipe(Bird bird, IReadOnlyList<PipePair> pipes)
    {
        float hitLeft = bird.Hitbox.Left;
        var ahead = pipes.Where(p => p.Right + Pipe.CapOverhang > hitLeft).OrderBy(p => p.X).ToList();
        if (ahead.Count < 2) return false;

        PipePair current = ahead[0], following = ahead[1];
        bool insidePipe = bird.Hitbox.Right > current.X - Pipe.CapOverhang;
        float gapBottom = current.GapCenterY + current.GapSize / 2f;
        float followingBottom = following.GapCenterY + following.GapSize / 2f;
        if (!insidePipe || followingBottom <= gapBottom || current.Speed < 1f) return false;

        // Borudan çıkana kadar geçecek süre ve o andaki kuşun alt kenarı (serbest düşüş)
        float t = (current.Right + Pipe.CapOverhang - hitLeft) / current.Speed;
        float v = bird.VelocityY;
        float fall = v * t + 0.5f * bird.Gravity * t * t;
        float hitBottom = bird.Hitbox.Bottom + fall;
        return hitBottom < gapBottom - 2f;
    }

    /// <summary>Kuşun alt kenarının altına inmemesi gereken hedef çizgi (Y).</summary>
    public static float GetJumpLine(Bird bird, IReadOnlyList<PipePair> pipes)
    {
        float hitLeft = bird.Hitbox.Left;
        var ahead = pipes.Where(p => p.Right + Pipe.CapOverhang > hitLeft).OrderBy(p => p.X).ToList();
        if (ahead.Count == 0)
            return GameSettings.GroundY - 120f;

        PipePair next = ahead[0];
        float gapTop = next.GapCenterY - next.GapSize / 2f;
        float gapBottom = next.GapCenterY + next.GapSize / 2f;

        // Zıplamada kuşun yükseleceği mesafe: v² / (2g)
        float rise = bird.JumpVelocity * bird.JumpVelocity / (2f * bird.Gravity);

        // Bu çizgiden zıplanırsa kuşun tepesi boşluğun üst kenarına çarpmaz
        float highest = gapTop + bird.Height + rise + Margin;
        float lowest = gapBottom - Margin;
        float line = lowest;

        // Kuş borunun içindeyken sonraki boşluk daha yukarıdaysa önceden yüksel
        bool insidePipe = bird.Hitbox.Right > next.X - Pipe.CapOverhang;
        if (insidePipe && ahead.Count > 1)
        {
            PipePair following = ahead[1];
            line = following.GapCenterY + following.GapSize / 2f - Margin;
        }

        return Math.Clamp(line, Math.Min(highest, lowest), lowest);
    }
}
