namespace FlappyBird.Core;

/// <summary>
/// Oyunun fizik ve hız ayarları. Değerler piksel/saniye cinsindendir.
/// </summary>
public class GameSettings
{
    // Oyun dünyasının sabit mantıksal boyutu (ekran ölçeğinden bağımsız)
    public const float WorldWidth = 480f;
    public const float WorldHeight = 720f;
    public const float GroundHeight = 100f;

    /// <summary>Zeminin üst kenarının Y koordinatı.</summary>
    public static float GroundY => WorldHeight - GroundHeight;

    /// <summary>Yer çekimi ivmesi (px/s²).</summary>
    public float Gravity { get; init; } = 1500f;

    /// <summary>Zıplamada verilen dikey hız (negatif = yukarı).</summary>
    public float JumpVelocity { get; init; } = -430f;

    /// <summary>Kuşun ulaşabileceği en yüksek düşüş hızı.</summary>
    public float MaxFallSpeed { get; init; } = 700f;

    /// <summary>Boruların başlangıç kayma hızı.</summary>
    public float BasePipeSpeed { get; init; } = 160f;

    /// <summary>Üst ve alt boru arasındaki boşluk.</summary>
    public float PipeGap { get; init; } = 170f;

    /// <summary>Ardışık iki boru çifti arasındaki yatay mesafe.</summary>
    public float PipeSpacing { get; init; } = 220f;
}
