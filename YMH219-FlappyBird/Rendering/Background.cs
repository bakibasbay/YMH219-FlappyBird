using System.Drawing.Drawing2D;
using FlappyBird.Core;
using FlappyBird.Entities;

namespace FlappyBird.Rendering;

/// <summary>
/// Katmanlı (parallax) arka plan: gökyüzü, yıldızlar, güneş/ay, bulutlar,
/// uzak tepeler, şehir silüeti, çalılar ve kayan zemin.
/// Uzaktaki katman yavaş, yakındaki hızlı kayar; bu derinlik hissi verir.
/// Gün saati (Phase): 0 gündüz → 1 gün batımı → 2 gece → 3 şafak → 4 (=0) gündüz.
/// </summary>
public class Background
{
    // Katmanların zemin hızına göre kayma oranları (parallax katsayıları)
    private const float FarHillsFactor = 0.12f;
    private const float CityFactor = 0.25f;
    private const float BushFactor = 0.5f;

    private const float GrassHeight = 18f;
    private const float StripePeriod = 28f;
    private const float CityPeriod = 640f;
    private const float BushSpacing = 34f;

    // Gün saati anahtar renkleri: gündüz, gün batımı, gece, şafak
    private static readonly Color[] SkyTop =
    {
        Color.FromArgb(66, 160, 235), Color.FromArgb(92, 70, 150),
        Color.FromArgb(12, 16, 44), Color.FromArgb(80, 100, 175)
    };
    private static readonly Color[] SkyBottom =
    {
        Color.FromArgb(190, 232, 255), Color.FromArgb(255, 165, 110),
        Color.FromArgb(44, 56, 108), Color.FromArgb(255, 195, 165)
    };

    /// <summary>Şu anki gün saati (yumuşak geçişli).</summary>
    public float Phase { get; private set; }

    /// <summary>Ulaşılmak istenen gün saati; Phase buna doğru yavaşça kayar.</summary>
    public float TargetPhase { get; set; }

    /// <summary>Gecenin yoğunluğu (0 = gündüz, 1 = tam gece).</summary>
    public float Night { get; private set; }

    /// <summary>Gün batımı / şafak sıcaklığı (0..1).</summary>
    public float Warm { get; private set; }

    private float _time;
    private float _farOffset, _cityOffset, _bushOffset, _groundOffset;

    private readonly List<Cloud> _clouds = new();
    private readonly (float X, float Y, float Size, float Speed, float Phase)[] _stars;
    private readonly (float X, float W, float H, int Seed)[] _buildings;

    public Background()
    {
        // Sabit tohumlu rastgele: şehir ve yıldızlar her açılışta aynı görünür
        var rng = new Random(219);

        _stars = new (float, float, float, float, float)[70];
        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i] = ((float)rng.NextDouble() * GameSettings.WorldWidth,
                         (float)rng.NextDouble() * 430f,
                         0.8f + (float)rng.NextDouble() * 1.6f,
                         1.5f + (float)rng.NextDouble() * 2.5f,
                         (float)rng.NextDouble() * MathF.Tau);
        }

        // Şehir: CityPeriod genişliğinde tekrar eden bina dizisi
        var buildings = new List<(float, float, float, int)>();
        float x = 0;
        while (x < CityPeriod)
        {
            float w = 30 + rng.Next(0, 34);
            float h = 50 + rng.Next(0, 100);
            buildings.Add((x, w, h, rng.Next()));
            x += w + rng.Next(1, 7);
        }
        _buildings = buildings.ToArray();

        // Başlangıçta ekrana dağılmış birkaç bulut
        for (int i = 0; i < 6; i++)
            _clouds.Add(new Cloud(i * 95f + (float)rng.NextDouble() * 40f));

        UpdatePhaseValues();
    }

    /// <summary>Seviyeye göre hedef gün saatini belirler (her seviye yarım adım ilerler).</summary>
    public void SetLevel(int level) => TargetPhase = (level - 1) * 0.5f;

    /// <summary>Katmanları kaydırır ve gün saatini hedefe doğru yaklaştırır.</summary>
    public void Update(float dt, float scrollSpeed)
    {
        _time += dt;
        _farOffset += scrollSpeed * FarHillsFactor * dt;
        _cityOffset += scrollSpeed * CityFactor * dt;
        _bushOffset += scrollSpeed * BushFactor * dt;
        _groundOffset += scrollSpeed * dt;

        foreach (Cloud c in _clouds)
        {
            c.ScrollSpeed = scrollSpeed;
            c.Update(dt);
        }

        // Gün saati geçişi: ileri yavaş, yeni oyunda geri dönüş hızlı
        float rate = TargetPhase < Phase ? 1.5f : 0.35f;
        if (Math.Abs(TargetPhase - Phase) <= rate * dt)
            Phase = TargetPhase;
        else
            Phase += Math.Sign(TargetPhase - Phase) * rate * dt;

        UpdatePhaseValues();
    }

    private void UpdatePhaseValues()
    {
        float p = Wrap(Phase);
        Night = DrawingHelper.Clamp01(1f - Math.Abs(p - 2f));
        Warm = DrawingHelper.Clamp01(1f - Math.Abs(p - 1f)) + DrawingHelper.Clamp01(1f - Math.Abs(p - 3f));
    }

    private static float Wrap(float p) => ((p % 4f) + 4f) % 4f;

    /// <summary>Anahtar renkler arasında gün saatine göre geçiş yapar.</summary>
    private Color PhaseColor(Color[] keys)
    {
        float p = Wrap(Phase);
        int i = (int)p;
        return DrawingHelper.Lerp(keys[i % 4], keys[(i + 1) % 4], p - i);
    }

    /// <summary>Gece/gün batımı etkisini bir katman rengine uygular.</summary>
    private Color Tinted(Color day, Color night, Color warm, float warmAmount)
    {
        Color c = DrawingHelper.Lerp(day, warm, Warm * warmAmount);
        return DrawingHelper.Lerp(c, night, Night);
    }

    // =============== Çizim ===============

    /// <summary>Boruların arkasında kalan tüm katmanları çizer.</summary>
    public void DrawSky(Graphics g)
    {
        // 1) Gökyüzü dikey gradyanı
        // Ekran sarsıntısında kenarlarda boşluk kalmasın diye biraz taşırılarak çizilir
        var skyRect = new RectangleF(-20, -20, GameSettings.WorldWidth + 40, GameSettings.GroundY + 21);
        using (var sky = new LinearGradientBrush(skyRect, PhaseColor(SkyTop), PhaseColor(SkyBottom), LinearGradientMode.Vertical))
            g.FillRectangle(sky, skyRect);

        DrawStars(g);
        DrawSunAndMoon(g);

        // 2) Bulutlar (gün batımında pembemsi, gece koyu)
        Color cloudTint = Tinted(Color.White, Color.FromArgb(70, 80, 120), Color.FromArgb(255, 215, 200), 0.8f);
        foreach (Cloud c in _clouds)
        {
            c.Tint = cloudTint;
            c.Draw(g);
        }

        // 3) Uzak tepeler, şehir, yakın çalılar
        DrawFarHills(g);
        DrawCity(g);
        DrawBushes(g);
    }

    private void DrawStars(Graphics g)
    {
        if (Night <= 0.01f) return;
        foreach (var (x, y, size, speed, phase) in _stars)
        {
            // Her yıldız kendi hızında parıldar
            float twinkle = 0.55f + 0.45f * MathF.Sin(_time * speed + phase);
            int a = (int)(255 * Night * twinkle);
            using var b = new SolidBrush(Color.FromArgb(a, 255, 250, 225));
            g.FillEllipse(b, x - size / 2, y - size / 2, size, size);
            if (size > 2f)
            {
                // Büyük yıldızlara küçük artı şeklinde ışıltı
                using var pen = new Pen(Color.FromArgb(a / 2, 255, 250, 225), 0.8f);
                g.DrawLine(pen, x - size * 2, y, x + size * 2, y);
                g.DrawLine(pen, x, y - size * 2, x, y + size * 2);
            }
        }
    }

    private void DrawSunAndMoon(Graphics g)
    {
        float p = Wrap(Phase);

        // Güneş: gündüz yukarıda, gün batımında alçalıp tepelerin arkasına iner, şafakta yükselir
        float sunY;
        if (p <= 1.6f) sunY = 110f + p / 1.6f * 380f;
        else if (p >= 2.6f) sunY = 490f - (p - 2.6f) / 1.4f * 380f;
        else sunY = 600f;

        if (sunY < 560f)
        {
            Color sunColor = DrawingHelper.Lerp(Color.FromArgb(255, 238, 150), Color.FromArgb(255, 130, 70), Warm);
            var center = new PointF(360f, sunY);
            DrawGlow(g, center, 80f, DrawingHelper.WithAlpha(sunColor, 0.55f));
            using var sun = new SolidBrush(sunColor);
            g.FillEllipse(sun, center.X - 26, center.Y - 26, 52, 52);
        }

        // Ay: yalnızca gece görünür, üzerinde birkaç krater
        if (Night > 0.01f)
        {
            var moon = new PointF(110f, 110f);
            DrawGlow(g, moon, 60f, Color.FromArgb((int)(90 * Night), 220, 230, 255));
            using var body = new SolidBrush(Color.FromArgb((int)(255 * Night), 240, 240, 225));
            g.FillEllipse(body, moon.X - 22, moon.Y - 22, 44, 44);
            using var crater = new SolidBrush(Color.FromArgb((int)(70 * Night), 170, 170, 160));
            g.FillEllipse(crater, moon.X - 10, moon.Y - 8, 10, 10);
            g.FillEllipse(crater, moon.X + 4, moon.Y + 4, 8, 8);
            g.FillEllipse(crater, moon.X - 4, moon.Y + 10, 5, 5);
        }
    }

    /// <summary>Ortadan dışa doğru saydamlaşan yumuşak ışık halesi.</summary>
    private static void DrawGlow(Graphics g, PointF center, float radius, Color color)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = color,
            SurroundColors = new[] { Color.FromArgb(0, color) }
        };
        g.FillPath(brush, path);
    }

    private void DrawFarHills(Graphics g)
    {
        Color color = Tinted(Color.FromArgb(150, 205, 180), Color.FromArgb(30, 44, 78), Color.FromArgb(190, 120, 160), 0.55f);
        using var brush = new SolidBrush(color);
        using var path = new GraphicsPath();

        // İki sinüsün toplamı: periyodik olduğu için kayarken dikiş yeri görünmez
        var points = new List<PointF> { new(0, GameSettings.GroundY) };
        for (float x = 0; x <= GameSettings.WorldWidth + 8; x += 8)
        {
            float wx = x + _farOffset;
            float y = 455f + MathF.Sin(wx * 0.011f) * 28f + MathF.Sin(wx * 0.027f + 1.3f) * 12f;
            points.Add(new PointF(x, y));
        }
        points.Add(new PointF(GameSettings.WorldWidth + 8, GameSettings.GroundY));
        path.AddPolygon(points.ToArray());
        g.FillPath(brush, path);
    }

    private void DrawCity(Graphics g)
    {
        Color color = Tinted(Color.FromArgb(125, 170, 200), Color.FromArgb(24, 30, 58), Color.FromArgb(160, 120, 140), 0.4f);
        Color window = Color.FromArgb((int)(230 * Night), 255, 220, 120);
        using var brush = new SolidBrush(color);
        using var windowBrush = new SolidBrush(window);

        float offset = _cityOffset % CityPeriod;
        // Dizi iki kez çizilir (yan yana): sonsuz kayan şehir
        for (int copy = 0; copy < 2; copy++)
        {
            float baseX = copy * CityPeriod - offset;
            foreach (var (bx, w, h, seed) in _buildings)
            {
                float x = baseX + bx;
                if (x > GameSettings.WorldWidth || x + w < 0) continue;
                float top = GameSettings.GroundY - h;
                g.FillRectangle(brush, x, top, w, h);

                // Bazı binalarda anten
                if (seed % 4 == 0)
                    g.FillRectangle(brush, x + w / 2 - 1.5f, top - 14, 3, 14);

                // Gece ışığı yanan pencereler
                if (Night > 0.05f)
                {
                    int bit = 0;
                    for (float wy = top + 8; wy < GameSettings.GroundY - 10; wy += 14)
                    {
                        for (float wx = x + 6; wx < x + w - 8; wx += 10)
                        {
                            if (((seed >> (bit++ % 30)) & 1) == 1)
                                g.FillRectangle(windowBrush, wx, wy, 4, 6);
                        }
                    }
                }
            }
        }
    }

    private void DrawBushes(Graphics g)
    {
        Color color = Tinted(Color.FromArgb(95, 185, 95), Color.FromArgb(20, 48, 42), Color.FromArgb(150, 160, 80), 0.3f);
        using var brush = new SolidBrush(color);
        using var path = new GraphicsPath(FillMode.Winding);

        int first = (int)MathF.Floor(_bushOffset / BushSpacing) - 1;
        for (int i = first; i < first + GameSettings.WorldWidth / BushSpacing + 3; i++)
        {
            // Her çalının boyu indeksinden türetilir: kayarken şekil değişmez
            float r = 20f + 9f * MathF.Sin(i * 1.7f) + 5f * MathF.Sin(i * 0.63f);
            float x = i * BushSpacing - _bushOffset;
            path.AddEllipse(x - r, GameSettings.GroundY - r * 1.1f, r * 2, r * 2);
        }
        g.FillPath(brush, path);
    }

    /// <summary>Zemini çizer (borulardan sonra çağrılır; boru diplerini örter).</summary>
    public void DrawGround(Graphics g)
    {
        float top = GameSettings.GroundY;
        float w = GameSettings.WorldWidth;

        // Toprak ve kayan çapraz şeritler
        using (var dirt = new SolidBrush(Color.FromArgb(228, 204, 148)))
            g.FillRectangle(dirt, -20, top, w + 40, GameSettings.GroundHeight + 20);

        float offset = _groundOffset % StripePeriod;
        using (var stripe = new SolidBrush(Color.FromArgb(214, 186, 128)))
        {
            for (float x = -offset - StripePeriod; x < w + StripePeriod; x += StripePeriod)
            {
                PointF[] band =
                {
                    new(x, top + GrassHeight), new(x + 12, top + GrassHeight),
                    new(x + 12 - 30, top + GameSettings.GroundHeight), new(x - 30, top + GameSettings.GroundHeight)
                };
                g.FillPolygon(stripe, band);
            }
        }

        // Çimen şeridi
        var grassRect = new RectangleF(-20, top, w + 40, GrassHeight);
        using (var grass = new LinearGradientBrush(grassRect, Color.FromArgb(135, 215, 90), Color.FromArgb(80, 160, 55), LinearGradientMode.Vertical))
            g.FillRectangle(grass, grassRect);

        // Çimen dokusu: zeminle aynı hızda kayan küçük üçgenler (alt koyu, üst açık uçlar)
        using (var blades = new SolidBrush(Color.FromArgb(70, 145, 50)))
        using (var tufts = new SolidBrush(Color.FromArgb(150, 225, 100)))
        {
            float bladeOffset = _groundOffset % 12f;
            for (float x = -bladeOffset - 12; x < w + 12; x += 12)
            {
                PointF[] tri = { new(x, top + GrassHeight), new(x + 6, top + GrassHeight - 7), new(x + 12, top + GrassHeight) };
                g.FillPolygon(blades, tri);
                PointF[] tuft = { new(x + 2, top + 2), new(x + 6, top - 5), new(x + 10, top + 2) };
                g.FillPolygon(tufts, tuft);
            }
        }

        using (var light = new Pen(Color.FromArgb(185, 240, 130), 3f))
            g.DrawLine(light, -20, top + 1.5f, w + 20, top + 1.5f);
        using (var edge = new Pen(Color.FromArgb(90, 70, 40), 2f))
            g.DrawLine(edge, -20, top + GrassHeight + 1, w + 20, top + GrassHeight + 1);
    }

    /// <summary>Gece olunca tüm sahneye hafif lacivert bir ton bindirir.</summary>
    public void DrawNightTint(Graphics g)
    {
        int a = (int)(80 * Night + 25 * Math.Min(Warm, 1f));
        if (a <= 0) return;
        // Gün batımında morumsu, gecede lacivert ton (aradaki geçiş yumuşak)
        Color tint = DrawingHelper.Lerp(Color.FromArgb(120, 40, 60), Color.FromArgb(10, 20, 60),
            Night / Math.Max(0.001f, Night + Warm));
        using var b = new SolidBrush(Color.FromArgb(a, tint));
        g.FillRectangle(b, 0, 0, GameSettings.WorldWidth, GameSettings.WorldHeight);
    }
}
