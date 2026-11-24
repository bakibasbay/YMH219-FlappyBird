using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Entities;

/// <summary>Kuşun hareket modu.</summary>
public enum BirdMode
{
    /// <summary>Menü/Hazır ekranında yerinde süzülür, yer çekimi yok.</summary>
    Hovering,
    /// <summary>Oyun sırasında: yer çekimi ve zıplama aktif.</summary>
    Flying,
    /// <summary>Çarptı: zemine düşer ve orada kalır.</summary>
    Dead
}

/// <summary>
/// Oyuncunun kontrol ettiği kuş. Yer çekimi ile düşer, Jump() ile yukarı zıplar.
/// Tamamen kodla çizilir: gövde, karın, göz, gaga, tepe tüyü, kuyruk ve çırpan kanat.
/// </summary>
public class Bird : GameObject
{
    public const float DefaultWidth = 44f;
    public const float DefaultHeight = 34f;

    // Gövde açısı sınırları (derece): yukarı çıkarken burun yukarı, düşerken aşağı
    private const float UpAngle = -25f;
    private const float DownAngle = 90f;

    // Renk paleti (orijinal karakter: turuncu, krem karınlı, tepeli kuş)
    private static readonly Color BodyLight = Color.FromArgb(255, 178, 72);
    private static readonly Color BodyDark = Color.FromArgb(238, 96, 48);
    private static readonly Color Outline = Color.FromArgb(110, 45, 25);
    private static readonly Color Belly = Color.FromArgb(255, 238, 200);
    private static readonly Color Beak = Color.FromArgb(255, 205, 45);
    private static readonly Color WingColor = Color.FromArgb(255, 205, 110);

    /// <summary>Dikey hız (px/s). Negatif değer yukarı hareket demektir.</summary>
    public float VelocityY { get; private set; }

    /// <summary>Gövdenin dönüş açısı (derece). Hıza göre yumuşakça değişir.</summary>
    public float Rotation { get; private set; }

    public BirdMode Mode { get; private set; } = BirdMode.Hovering;

    // Fizik değerleri GameSettings'ten atanır (zorluğa göre değişir)
    public float Gravity { get; set; } = 1500f;
    public float JumpVelocity { get; set; } = -430f;
    public float MaxFallSpeed { get; set; } = 700f;

    /// <summary>Kuşun düşebileceği en alt nokta (zeminin üstü).</summary>
    public float GroundY { get; set; } = GameSettings.GroundY;

    public bool IsOnGround => Bottom >= GroundY - 0.5f;

    /// <summary>
    /// Çarpışma kutusu: çizilen gövdeden biraz küçük tutulur,
    /// böylece borunun kenarını "sıyırmak" oyuncuya adil hissettirir.
    /// </summary>
    public RectangleF Hitbox
    {
        get
        {
            RectangleF r = Bounds;
            r.Inflate(-6f, -5f);
            return r;
        }
    }

    // Animasyon durumları
    private float _time;
    private float _hoverBaseY;     // süzülmenin merkez yüksekliği
    private float _wingPhase;      // kanat çırpma açısı için sinüs fazı
    private float _flapBoost;      // zıpladıktan hemen sonra hızlı çırpma süresi
    private float _blinkTimer = 2f; // bir sonraki göz kırpmaya kalan süre
    private float _blinkTime;      // göz kapalı kalma süresi

    public Bird(float x, float y)
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        ResetTo(x, y);
    }

    /// <summary>Kuşu başlangıç konumuna alır ve süzülme moduna geçirir.</summary>
    public void ResetTo(float x, float y)
    {
        X = x;
        Y = y;
        _hoverBaseY = y;
        VelocityY = 0;
        Rotation = 0;
        Mode = BirdMode.Hovering;
    }

    /// <summary>Yukarı doğru anlık hız verir.</summary>
    public void Jump()
    {
        if (Mode == BirdMode.Dead) return;
        Mode = BirdMode.Flying;
        VelocityY = JumpVelocity;
        _flapBoost = 0.3f;
    }

    /// <summary>Kuş çarptı: küçük bir sekmeyle düşmeye başlar, artık zıplayamaz.</summary>
    public void Die()
    {
        if (Mode == BirdMode.Dead) return;
        Mode = BirdMode.Dead;
        if (!IsOnGround)
            VelocityY = -180f;
    }

    public override void Update(float dt)
    {
        _time += dt;
        UpdateBlink(dt);

        float wingSpeed;
        if (Mode == BirdMode.Hovering)
        {
            // Bekleme ekranı: sinüs dalgasıyla hafifçe yukarı-aşağı süzülme
            Y = _hoverBaseY + MathF.Sin(_time * 3.2f) * 7f;
            Rotation += (0 - Rotation) * Math.Min(1f, dt * 8f);
            wingSpeed = 11f;
        }
        else
        {
            ApplyPhysics(dt);
            UpdateRotation(dt);
            _flapBoost = Math.Max(0, _flapBoost - dt);
            wingSpeed = _flapBoost > 0 ? 24f : 12f;

            // Dalışta ve ölüyken kanatlar çırpılmaz
            if (Mode == BirdMode.Dead || Rotation > 55f)
                wingSpeed = 0f;
        }
        _wingPhase += wingSpeed * dt;
    }

    /// <summary>Yer çekimi: hız ivme ile artar, konum hız ile değişir.</summary>
    private void ApplyPhysics(float dt)
    {
        VelocityY = Math.Min(VelocityY + Gravity * dt, MaxFallSpeed);
        Y += VelocityY * dt;

        // Tavanın çok üstüne çıkmasın (oyun yine de Game Over olur)
        if (Y < -Height)
            Y = -Height;

        // Zemine değince orada dur
        if (Bottom > GroundY)
        {
            Y = GroundY - Height;
            VelocityY = 0;
        }
    }

    /// <summary>
    /// Gövde açısı hıza göre: yükselirken -25°, düşüş hızlandıkça +90°'ye kadar.
    /// Açı hedefe birden atlamaz, her karede biraz yaklaşır (yumuşak dönüş).
    /// </summary>
    private void UpdateRotation(float dt)
    {
        float target;
        if (Mode == BirdMode.Dead)
            target = DownAngle;
        else if (VelocityY < 0)
            target = UpAngle;
        else
        {
            float t = Math.Clamp((VelocityY - 150f) / (MaxFallSpeed - 150f), 0f, 1f);
            target = UpAngle + (DownAngle - UpAngle) * t;
        }

        float rate = target < Rotation ? 14f : 5f; // yukarı hızlı, aşağı yavaş döner
        Rotation += (target - Rotation) * Math.Min(1f, rate * dt);
    }

    /// <summary>Ara sıra göz kırpma (karaktere canlılık katar).</summary>
    private void UpdateBlink(float dt)
    {
        if (_blinkTime > 0)
        {
            _blinkTime -= dt;
            return;
        }
        _blinkTimer -= dt;
        if (_blinkTimer <= 0)
        {
            _blinkTime = 0.12f;
            _blinkTimer = 2f + (float)Random.Shared.NextDouble() * 2.5f;
        }
    }

    public override void Draw(Graphics g)
    {
        GraphicsState state = g.Save();

        // Kuşu kendi merkezine taşıyıp gövde açısı kadar döndür; çizim yerel koordinatta yapılır
        g.TranslateTransform(CenterX, CenterY);
        g.RotateTransform(Rotation);

        using var outlinePen = new Pen(Outline, 2f) { LineJoin = LineJoin.Round };

        DrawTailAndCrest(g, outlinePen);
        DrawBody(g, outlinePen);
        DrawEye(g, outlinePen);
        DrawBeak(g, outlinePen);
        DrawWing(g, outlinePen);

        g.Restore(state);
    }

    private static void DrawTailAndCrest(Graphics g, Pen pen)
    {
        using var tailBrush = new SolidBrush(BodyDark);

        // Kuyruk: arkada iki sivri tüy
        PointF[] tail1 = { new(-17, -4), new(-30, -10), new(-26, 2) };
        PointF[] tail2 = { new(-17, 2), new(-29, 6), new(-20, 9) };
        g.FillPolygon(tailBrush, tail1);
        g.DrawPolygon(pen, tail1);
        g.FillPolygon(tailBrush, tail2);
        g.DrawPolygon(pen, tail2);

        // Tepe tüyü: başın üstünde iki kıvrık tüy
        PointF[] crest1 = { new(-2, -14), new(2, -26), new(6, -14) };
        PointF[] crest2 = { new(4, -14), new(11, -23), new(11, -12) };
        g.FillPolygon(tailBrush, crest1);
        g.DrawPolygon(pen, crest1);
        g.FillPolygon(tailBrush, crest2);
        g.DrawPolygon(pen, crest2);
    }

    private static void DrawBody(Graphics g, Pen pen)
    {
        var bodyRect = new RectangleF(-22, -17, 44, 34);
        using (var body = new LinearGradientBrush(bodyRect, BodyLight, BodyDark, LinearGradientMode.Vertical))
            g.FillEllipse(body, bodyRect);

        // Krem renkli karın
        using (var belly = new SolidBrush(Belly))
            g.FillEllipse(belly, -12, 0, 26, 15);

        // Pembe yanak
        using (var cheek = new SolidBrush(Color.FromArgb(120, 255, 110, 130)))
            g.FillEllipse(cheek, 9, 1, 9, 6);

        g.DrawEllipse(pen, bodyRect);
    }

    private void DrawEye(Graphics g, Pen pen)
    {
        var eye = new RectangleF(3, -14, 16, 16);

        if (Mode == BirdMode.Dead)
        {
            // Bayılmış kuş: göz yerine çarpı işareti
            using var x = new Pen(Outline, 2.5f);
            g.DrawLine(x, 7, -10, 15, -2);
            g.DrawLine(x, 15, -10, 7, -2);
            return;
        }

        if (_blinkTime > 0)
        {
            // Göz kırpma: göz kapalı çizgi
            using var lid = new Pen(Outline, 2.5f);
            g.DrawArc(lid, eye.X, eye.Y + 4, eye.Width, eye.Height - 6, 20, 140);
            return;
        }

        g.FillEllipse(Brushes.White, eye);
        g.DrawEllipse(pen, eye);
        g.FillEllipse(Brushes.Black, 11, -10, 7, 8);       // göz bebeği (ileri bakar)
        g.FillEllipse(Brushes.White, 14, -9, 2.6f, 2.6f);  // parlama noktası
    }

    private static void DrawBeak(Graphics g, Pen pen)
    {
        using var upper = new SolidBrush(Beak);
        using var lower = new SolidBrush(Color.FromArgb(240, 150, 30));
        PointF[] top = { new(17, -4), new(33, 0.5f), new(18, 4) };
        PointF[] bottom = { new(18, 4), new(29, 5), new(17, 9) };
        g.FillPolygon(lower, bottom);
        g.DrawPolygon(pen, bottom);
        g.FillPolygon(upper, top);
        g.DrawPolygon(pen, top);
    }

    /// <summary>Kanat: omuz noktası etrafında sinüsle aşağı-yukarı döner (çırpma).</summary>
    private void DrawWing(Graphics g, Pen pen)
    {
        float angle = Mode == BirdMode.Dead ? -10f : MathF.Sin(_wingPhase) * 38f;
        if (Mode != BirdMode.Dead && Rotation > 55f)
            angle = -20f; // dalışta kanat geriye yatık

        GraphicsState s = g.Save();
        g.TranslateTransform(-5, 2);
        g.RotateTransform(angle);

        var wingRect = new RectangleF(-17, -6, 21, 12);
        using (var wing = new LinearGradientBrush(wingRect, WingColor, BodyLight, LinearGradientMode.Vertical))
            g.FillEllipse(wing, wingRect);
        g.DrawEllipse(pen, wingRect);

        // Kanat üzerindeki tüy çizgileri
        using var feather = new Pen(Color.FromArgb(150, Outline), 1.2f);
        g.DrawLine(feather, -12, -1, -3, -1);
        g.DrawLine(feather, -11, 2, -4, 2.5f);

        g.Restore(s);
    }
}
