using System.Drawing.Drawing2D;
using FlappyBird.Entities;

namespace FlappyBird.Core;

/// <summary>
/// Oyunun beyni: oyun döngüsünü (Update/Draw) ve oyun durumunu yönetir.
/// Form yalnızca girdileri buraya iletir ve her karede Update + Draw çağırır.
/// </summary>
public class GameManager
{
    // Kuşun oyundaki sabit yatay konumu ve başlangıç yüksekliği
    public const float BirdStartX = 110f;
    public const float BirdStartY = 320f;

    /// <summary>Oyunun şu anki durumu.</summary>
    public GameState State { get; private set; } = GameState.Ready;

    /// <summary>Aktif fizik/hız ayarları.</summary>
    public GameSettings Settings { get; private set; } = new();

    /// <summary>Oyuncunun kuşu.</summary>
    public Bird Bird { get; }

    /// <summary>Ekranda aynı anda dolaşan boru çifti sayısı (föy: en az 2).</summary>
    public const int PipePairCount = 3;

    // Boşluğun ekran kenarlarına ve zemine olan en küçük mesafesi
    private const float GapMargin = 60f;
    // Ardışık iki boşluk arasındaki en büyük yükseklik farkı (adil oyun için)
    private const float MaxGapShift = 190f;

    /// <summary>Sağdan sola kayan boru çiftleri.</summary>
    public List<PipePair> Pipes { get; } = new();

    /// <summary>Boruların (ve zeminin) şu anki kayma hızı.</summary>
    public float PipeSpeed { get; private set; }

    /// <summary>Oyun başladığından beri geçen toplam süre (animasyonlar için).</summary>
    public float TotalTime { get; private set; }

    private readonly Random _random = new();

    public GameManager()
    {
        Bird = new Bird(BirdStartX, BirdStartY);
        ApplySettingsToBird();

        for (int i = 0; i < PipePairCount; i++)
            Pipes.Add(new PipePair(0, GameSettings.GroundY / 2f, Settings.PipeGap));
        ResetPipes();
    }

    /// <summary>Boruları ekranın sağına, eşit aralıklarla diz.</summary>
    private void ResetPipes()
    {
        PipeSpeed = Settings.BasePipeSpeed;
        float x = GameSettings.WorldWidth + 80f;
        float gap = GameSettings.GroundY / 2f;
        foreach (PipePair pair in Pipes)
        {
            gap = RandomGapCenter(gap);
            pair.Reposition(x, gap, Settings.PipeGap);
            x += Settings.PipeSpacing;
        }
    }

    /// <summary>
    /// Rastgele bir boşluk yüksekliği üretir. Bir önceki boşluktan
    /// çok uzak olmaması sağlanır, böylece geçilemez dizilim oluşmaz.
    /// </summary>
    private float RandomGapCenter(float previous)
    {
        float half = Settings.PipeGap / 2f;
        float min = GapMargin + half;
        float max = GameSettings.GroundY - GapMargin - half;
        float low = Math.Max(min, previous - MaxGapShift);
        float high = Math.Min(max, previous + MaxGapShift);
        return low + (float)_random.NextDouble() * (high - low);
    }

    /// <summary>
    /// Ekrandan çıkan boru çiftini en sağdaki çiftin arkasına taşır
    /// ve yeni rastgele boşluk verir (föy: yeniden konumlandırma).
    /// </summary>
    private void RecyclePipe(PipePair pair)
    {
        PipePair last = Pipes.MaxBy(p => p.X)!;
        pair.Reposition(last.X + Settings.PipeSpacing, RandomGapCenter(last.GapCenterY), Settings.PipeGap);
    }

    private void ApplySettingsToBird()
    {
        Bird.Gravity = Settings.Gravity;
        Bird.JumpVelocity = Settings.JumpVelocity;
        Bird.MaxFallSpeed = Settings.MaxFallSpeed;
    }

    // ---------------- Girdi ----------------

    /// <summary>Klavye tuşlarını işler (Form KeyDown olayından çağrılır).</summary>
    public void HandleKeyDown(Keys key)
    {
        if (key == Keys.Space || key == Keys.Up)
            Flap();
    }

    /// <summary>Fare tıklamasını işler (dünya koordinatında).</summary>
    public void HandleMouseDown(PointF worldPoint)
    {
        Flap();
    }

    /// <summary>Zıplama eylemi: Hazır ekranındaysa oyunu başlatır.</summary>
    private void Flap()
    {
        if (State == GameState.Ready)
            State = GameState.Playing;

        if (State == GameState.Playing)
            Bird.Jump();
    }

    // ---------------- Döngü ----------------

    /// <summary>
    /// Bir kare ilerletir. dt: önceki kareden bu yana geçen süre (saniye).
    /// Hareketler dt ile çarpıldığı için FPS düşse de hız sabit kalır.
    /// </summary>
    public void Update(float dt)
    {
        TotalTime += dt;

        if (State == GameState.Playing)
        {
            Bird.Update(dt);
            UpdatePipes(dt);
        }
    }

    /// <summary>Boruları kaydırır, ekrandan çıkanları yeniden konumlandırır.</summary>
    private void UpdatePipes(float dt)
    {
        foreach (PipePair pair in Pipes)
        {
            pair.Speed = PipeSpeed;
            pair.Update(dt);
            if (pair.IsOffScreen)
                RecyclePipe(pair);
        }
    }

    /// <summary>Tüm sahneyi verilen Graphics nesnesine çizer.</summary>
    public void Draw(Graphics g)
    {
        // Gökyüzü: yukarıdan aşağıya dikey gradyan
        var skyRect = new RectangleF(0, 0, GameSettings.WorldWidth, GameSettings.GroundY);
        using (var sky = new LinearGradientBrush(skyRect, Color.FromArgb(74, 168, 240),
                   Color.FromArgb(190, 232, 255), LinearGradientMode.Vertical))
        {
            g.FillRectangle(sky, skyRect);
        }

        foreach (PipePair pair in Pipes)
            pair.Draw(g);

        // Zemin (boruların alt ucunu örter)
        using (var ground = new SolidBrush(Color.FromArgb(222, 200, 140)))
        {
            g.FillRectangle(ground, 0, GameSettings.GroundY, GameSettings.WorldWidth, GameSettings.GroundHeight);
        }

        Bird.Draw(g);
    }
}
