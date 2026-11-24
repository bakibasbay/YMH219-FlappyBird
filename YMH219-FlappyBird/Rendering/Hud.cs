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

    public void Update(float dt)
    {
        _time += dt;
        _levelBannerTime = Math.Max(0, _levelBannerTime - dt);
    }

    /// <summary>Yeni oyunda animasyonları sıfırlar.</summary>
    public void Reset()
    {
        _levelBannerTime = 0;
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

    /// <summary>Ekranın üst ortasında büyük, konturlu skor.</summary>
    public void DrawScore(Graphics g, int score)
    {
        DrawingHelper.DrawOutlinedText(g, score.ToString(), 64f,
            new PointF(GameSettings.WorldWidth / 2f, 70), Color.White, OutlineDark, 10f);
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
