using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Oyun sırasında ekranın üstünde görünen bilgiler (HUD):
/// skor, Hazır ekranı yazıları ve duraklatma katmanı.
/// </summary>
public class Hud
{
    private static readonly Color OutlineDark = Color.FromArgb(60, 40, 30);

    private const float LevelBannerDuration = 1.8f;

    private float _time;
    private float _levelBannerTime; // "SEVİYE n!" yazısının kalan gösterim süresi
    private int _bannerLevel;

    private const float PopDuration = 0.3f;
    private float _popTime; // skor "pop" animasyonunun kalan süresi

    public void Update(float dt)
    {
        _time += dt;
        _levelBannerTime = Math.Max(0, _levelBannerTime - dt);
        _popTime = Math.Max(0, _popTime - dt);
        _toastTime = Math.Max(0, _toastTime - dt);
    }

    /// <summary>Yeni oyunda animasyonları sıfırlar.</summary>
    public void Reset()
    {
        _levelBannerTime = 0;
        _popTime = 0;
    }

    /// <summary>Skor arttığında skor yazısını kısa süre büyütür.</summary>
    public void TriggerScorePop() => _popTime = PopDuration;

    /// <summary>
    /// Üst köşelerdeki bilgi kapsülleri: solda seviye ve zorluk, sağda en yüksek skor.
    /// </summary>
    public void DrawTopBar(Graphics g, int level, int highScore, string difficulty, bool soundOn)
    {
        DrawPill(g, new RectangleF(12, 12, 112, 32), $"SEVİYE {level}", Color.FromArgb(60, 135, 220));
        DrawingHelper.DrawOutlinedText(g, difficulty, 13f, new PointF(68, 56),
            Color.White, Color.FromArgb(150, 20, 30, 60), 3f, heavy: false, shadow: false);

        var bestRect = new RectangleF(GameSettings.WorldWidth - 124, 12, 112, 32);
        DrawPill(g, bestRect, $"EN İYİ {highScore}", Color.FromArgb(235, 160, 40));

        if (!soundOn)
            DrawMutedIcon(g, new PointF(GameSettings.WorldWidth - 30, 62));
    }

    /// <summary>Ses kapalıyken üstü çizili hoparlör simgesi.</summary>
    private static void DrawMutedIcon(Graphics g, PointF c)
    {
        PointF[] speaker =
        {
            new(c.X - 10, c.Y - 4), new(c.X - 5, c.Y - 4), new(c.X + 1, c.Y - 10),
            new(c.X + 1, c.Y + 10), new(c.X - 5, c.Y + 4), new(c.X - 10, c.Y + 4)
        };
        using (var fill = new SolidBrush(Color.White))
        using (var edge = new Pen(Color.FromArgb(160, 20, 30, 60), 2f) { LineJoin = LineJoin.Round })
        {
            g.DrawPolygon(edge, speaker);
            g.FillPolygon(fill, speaker);
        }
        using var cross = new Pen(Color.FromArgb(235, 70, 60), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(cross, c.X + 5, c.Y - 5, c.X + 13, c.Y + 5);
        g.DrawLine(cross, c.X + 13, c.Y - 5, c.X + 5, c.Y + 5);
    }

    /// <summary>Otopilot açıkken skorun altında yanıp sönen küçük etiket.</summary>
    public void DrawAutopilotBadge(Graphics g)
    {
        float alpha = 0.55f + 0.45f * MathF.Sin(_time * 4f);
        DrawingHelper.DrawOutlinedText(g, "OTOPİLOT (F2)", 14f, new PointF(GameSettings.WorldWidth / 2f, 146),
            DrawingHelper.WithAlpha(Color.FromArgb(150, 255, 200), alpha), DrawingHelper.WithAlpha(Color.FromArgb(20, 60, 40), alpha), 3.5f, shadow: false);
    }

    private string _toastText = "";
    private float _toastTime;

    /// <summary>Ekranın üstünde kısa süreli bilgi mesajı (ör. "Ses kapalı").</summary>
    public void ShowToast(string text)
    {
        _toastText = text;
        _toastTime = 1.3f;
    }

    public void DrawToast(Graphics g)
    {
        if (_toastTime <= 0) return;
        float alpha = Math.Min(1f, _toastTime / 0.3f);
        // Üst ortadaki boş alan: seviye/en iyi kapsüllerinin arası (butonlarla çakışmaz)
        var r = new RectangleF(GameSettings.WorldWidth / 2f - 80, 12, 160, 32);
        using (GraphicsPath path = DrawingHelper.RoundedRect(r, 16))
        using (var fill = new SolidBrush(Color.FromArgb((int)(200 * alpha), 20, 25, 45)))
            g.FillPath(fill, path);
        DrawingHelper.DrawOutlinedText(g, _toastText, 17f, new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f + 1),
            DrawingHelper.WithAlpha(Color.White, alpha), Color.Empty, 0f, heavy: false, shadow: false);
    }

    /// <summary>Yarı saydam, yuvarlak uçlu bilgi kapsülü.</summary>
    private static void DrawPill(Graphics g, RectangleF r, string text, Color color)
    {
        using GraphicsPath path = DrawingHelper.RoundedRect(r, r.Height / 2f);
        using (var fill = new SolidBrush(Color.FromArgb(190, color)))
            g.FillPath(fill, path);
        using (var pen = new Pen(Color.FromArgb(220, 255, 255, 255), 2f))
            g.DrawPath(pen, path);
        DrawingHelper.DrawOutlinedText(g, text, 15f, new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f + 1),
            Color.White, DrawingHelper.Darken(color, 0.55f), 3.5f, shadow: false);
    }

    /// <summary>Ekranın ortasında kısa süreli "SEVİYE n!" yazısı başlatır.</summary>
    public void ShowLevelUp(int level)
    {
        _bannerLevel = level;
        _levelBannerTime = LevelBannerDuration;
    }

    /// <summary>Seviye yazısı: yaylı büyüyerek gelir, sonunda saydamlaşarak kaybolur.</summary>
    public void DrawLevelBanner(Graphics g)
    {
        if (_levelBannerTime <= 0) return;

        float elapsed = LevelBannerDuration - _levelBannerTime;
        float scale = 0.3f + 0.7f * DrawingHelper.EaseOutBack(elapsed / 0.4f);
        float alpha = _levelBannerTime < 0.4f ? _levelBannerTime / 0.4f : 1f;

        GraphicsState state = g.Save();
        g.TranslateTransform(GameSettings.WorldWidth / 2f, 235);
        g.ScaleTransform(scale, scale);
        DrawingHelper.DrawOutlinedText(g, $"SEVİYE {_bannerLevel}!", 54f, PointF.Empty,
            DrawingHelper.WithAlpha(Color.FromArgb(120, 235, 255), alpha),
            DrawingHelper.WithAlpha(Color.FromArgb(20, 40, 90), alpha), 10f);
        DrawingHelper.DrawOutlinedText(g, "Hız arttı!", 22f, new PointF(0, 48),
            DrawingHelper.WithAlpha(Color.White, alpha),
            DrawingHelper.WithAlpha(Color.FromArgb(20, 40, 90), alpha), 5f, heavy: false);
        g.Restore(state);
    }

    /// <summary>
    /// Ekranın üst ortasında büyük, konturlu skor. Skor alınınca önce
    /// büyüyüp altın rengine döner, sonra hızla normale iner ("pop").
    /// </summary>
    public void DrawScore(Graphics g, int score)
    {
        float k = _popTime / PopDuration;           // 1 → 0
        float scale = 1f + 0.5f * k * k;            // 1.5 → 1.0
        Color fill = DrawingHelper.Lerp(Color.White, Color.FromArgb(255, 220, 70), k);

        GraphicsState state = g.Save();
        g.TranslateTransform(GameSettings.WorldWidth / 2f, 96);
        g.ScaleTransform(scale, scale);
        DrawingHelper.DrawOutlinedText(g, score.ToString(), 64f, PointF.Empty, fill, OutlineDark, 10f);
        g.Restore(state);
    }

    /// <summary>Hazır ekranı: başlık ve "SPACE / tıkla" ipucu.</summary>
    public void DrawReady(Graphics g)
    {
        float cx = GameSettings.WorldWidth / 2f;

        // Başlık hafifçe nefes alır gibi büyüyüp küçülür
        float pulse = 1f + MathF.Sin(_time * 3f) * 0.04f;
        GraphicsState state = g.Save();
        g.TranslateTransform(cx, 190);
        g.ScaleTransform(pulse, pulse);
        DrawingHelper.DrawOutlinedText(g, "HAZIR OL!", 54f, PointF.Empty,
            Color.FromArgb(255, 225, 80), Color.FromArgb(120, 50, 10), 10f);
        g.Restore(state);

        // Dokunma ikonu: genişleyerek kaybolan halka
        var tap = new PointF(cx + 70, 335);
        float ringT = (_time * 1.2f) % 1f;
        float ringR = 10 + ringT * 26;
        using (var ringPen = new Pen(Color.FromArgb((int)(200 * (1 - ringT)), 255, 255, 255), 3f))
            g.DrawEllipse(ringPen, tap.X - ringR, tap.Y - ringR, ringR * 2, ringR * 2);
        using (var dot = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
            g.FillEllipse(dot, tap.X - 9, tap.Y - 9, 18, 18);

        // Yanıp sönen ipucu yazısı
        float blink = 0.6f + 0.4f * MathF.Sin(_time * 5f);
        DrawingHelper.DrawOutlinedText(g, "Başlamak için SPACE / tıkla", 24f, new PointF(cx, 450),
            DrawingHelper.WithAlpha(Color.White, blink), DrawingHelper.WithAlpha(OutlineDark, blink), 5f, heavy: false);
        DrawingHelper.DrawOutlinedText(g, "P: Duraklat    R: Yeniden    Esc: Menü", 16f, new PointF(cx, 490),
            Color.FromArgb(230, 255, 255, 255), Color.FromArgb(150, 40, 40, 60), 3.5f, heavy: false);
    }

    /// <summary>Duraklatma: ekranı karartır ve bilgi yazar.</summary>
    public void DrawPaused(Graphics g)
    {
        using (var dim = new SolidBrush(Color.FromArgb(140, 10, 15, 35)))
            g.FillRectangle(dim, 0, 0, GameSettings.WorldWidth, GameSettings.WorldHeight);

        float cx = GameSettings.WorldWidth / 2f;
        DrawingHelper.DrawOutlinedText(g, "DURAKLATILDI", 46f, new PointF(cx, 300),
            Color.White, OutlineDark, 9f);
        float blink = 0.65f + 0.35f * MathF.Sin(_time * 4f);
        DrawingHelper.DrawOutlinedText(g, "P: Devam et    R: Yeniden    Esc: Menü", 18f, new PointF(cx, 360),
            DrawingHelper.WithAlpha(Color.White, blink), Color.FromArgb(160, 0, 0, 0), 4f, heavy: false);
    }
}
