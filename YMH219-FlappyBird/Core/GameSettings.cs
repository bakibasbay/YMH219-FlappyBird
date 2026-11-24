namespace FlappyBird.Core;

/// <summary>
/// Oyunun fizik ve hız ayarları. Değerler piksel/saniye cinsindendir.
/// Zorluğa göre hazır ayar nesnesi Create() fabrika metoduyla üretilir.
/// </summary>
public class GameSettings
{
    // Oyun dünyasının sabit mantıksal boyutu (ekran ölçeğinden bağımsız)
    public const float WorldWidth = 480f;
    public const float WorldHeight = 720f;
    public const float GroundHeight = 100f;

    /// <summary>Zeminin üst kenarının Y koordinatı.</summary>
    public static float GroundY => WorldHeight - GroundHeight;

    /// <summary>Bu ayarların ait olduğu zorluk.</summary>
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>Menüde gösterilen kısa açıklama.</summary>
    public string Description { get; init; } = "";

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

    /// <summary>
    /// Ardışık iki boşluk arasındaki en büyük yükseklik farkı.
    /// Kuşun zamanında yükselip alçalabilmesi için sınırlanır (adil oyun).
    /// </summary>
    public float MaxGapShift { get; init; } = 160f;

    /// <summary>Her seviyede boru hızına eklenen oran (0.08 = %8).</summary>
    public float SpeedIncreasePerLevel { get; init; } = 0.08f;

    /// <summary>Hızın başlangıç hızına göre en fazla kaç katına çıkabileceği.</summary>
    public const float MaxSpeedMultiplier = 1.7f;

    /// <summary>Verilen seviyedeki boru hızı: her seviyede biraz artar, bir sınırda durur.</summary>
    public float GetPipeSpeed(int level)
    {
        float multiplier = 1f + (Math.Max(1, level) - 1) * SpeedIncreasePerLevel;
        return BasePipeSpeed * Math.Min(MaxSpeedMultiplier, multiplier);
    }

    /// <summary>
    /// Seçilen zorluğa göre ayar nesnesi oluşturur (fabrika metodu).
    /// </summary>
    public static GameSettings Create(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Kolay => new GameSettings
        {
            Difficulty = difficulty,
            Description = "Geniş boşluk · yavaş borular · hafif yer çekimi",
            Gravity = 1350f,
            JumpVelocity = -410f,
            MaxFallSpeed = 650f,
            BasePipeSpeed = 135f,
            PipeGap = 200f,
            PipeSpacing = 240f,
            MaxGapShift = 180f
        },
        Difficulty.Zor => new GameSettings
        {
            Difficulty = difficulty,
            Description = "Dar boşluk · hızlı borular · güçlü yer çekimi",
            Gravity = 1650f,
            JumpVelocity = -460f,
            MaxFallSpeed = 760f,
            BasePipeSpeed = 190f,
            PipeGap = 148f,
            PipeSpacing = 205f,
            MaxGapShift = 135f
        },
        _ => new GameSettings
        {
            Difficulty = Difficulty.Normal,
            Description = "Dengeli boşluk, hız ve yer çekimi"
        }
    };
}
