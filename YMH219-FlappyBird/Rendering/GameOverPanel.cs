using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Oyun bitince açılan panel: skor, en yüksek skor, "YENİ REKOR!" vurgusu,
/// madalya ve Yeniden Başlat / Menü butonları. Parçalar sırayla animasyonla gelir.
/// </summary>
public class GameOverPanel
{
    // Animasyon zaman çizelgesi (saniye)
    private const float TitleDuration = 0.5f;
    private const float PanelStart = 0.25f;
    private const float PanelDuration = 0.5f;
    private const float CountStart = 0.75f;
    private const float CountDuration = 0.6f;
    private const float MedalStart = 1.35f;
    private const float ButtonsStart = 1.0f;

    private static readonly RectangleF PanelRect = new(50, 205, 380, 235);
    private static readonly Color PanelFill = Color.FromArgb(255, 245, 218);
    private static readonly Color PanelBorder = Color.FromArgb(150, 95, 45);
    private static readonly Color LabelColor = Color.FromArgb(185, 120, 60);

    public event Action? RestartRequested;
    public event Action? MenuRequested;

    public UiButton RestartButton { get; }
    public UiButton MenuButton { get; }

    /// <summary>Panel ekranda mı?</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Butonlar göründükten sonra girdi kabul edilir (yanlışlıkla tıklamayı önler).</summary>
    public bool AcceptsInput => IsVisible && _time >= ButtonsStart;

    private float _time;
    private int _score;
    private int _best;
    private bool _newRecord;
    private Medal _medal;
    private bool _assisted; // otopilot kullanıldı mı

    public GameOverPanel()
    {
        RestartButton = new UiButton("YENİDEN BAŞLAT", new RectangleF(58, 470, 210, 56), Color.FromArgb(60, 185, 90)) { FontSize = 20 };
        MenuButton = new UiButton("MENÜ", new RectangleF(282, 470, 140, 56), Color.FromArgb(60, 135, 220)) { FontSize = 22 };
        RestartButton.Clicked += () => RestartRequested?.Invoke();
        MenuButton.Clicked += () => MenuRequested?.Invoke();
    }

    /// <summary>Paneli verilen sonuçlarla açar ve animasyonu baştan başlatır.</summary>
    public void Show(int score, int best, bool newRecord, Medal medal, bool assisted = false)
    {
        _assisted = assisted;
        _score = score;
        _best = best;
        _newRecord = newRecord;
        _medal = medal;
        _time = 0;
        IsVisible = true;
    }

    public void Hide() => IsVisible = false;

    public void Update(float dt)
    {
        if (IsVisible) _time += dt;
    }

    public bool HandleClick(PointF p)
    {
        if (!AcceptsInput) return false;
        return RestartButton.TryClick(p) || MenuButton.TryClick(p);
    }

    public void HandleMouseMove(PointF p)
    {
        RestartButton.IsHovered = AcceptsInput && RestartButton.Contains(p);
        MenuButton.IsHovered = AcceptsInput && MenuButton.Contains(p);
    }

    public void Draw(Graphics g)
    {
        if (!IsVisible) return;

        // Arka planı hafifçe karart
        int dim = (int)(90 * DrawingHelper.Clamp01(_time / 0.4f));
        using (var dimBrush = new SolidBrush(Color.FromArgb(dim, 0, 0, 20)))
            g.FillRectangle(dimBrush, 0, 0, GameSettings.WorldWidth, GameSettings.WorldHeight);

        DrawTitle(g);
        DrawPanel(g);

        // Butonlar ve ipucu yazısı yavaşça belirir
        float buttonAlpha = DrawingHelper.Clamp01((_time - ButtonsStart) / 0.3f);
        RestartButton.Draw(g, buttonAlpha);
        MenuButton.Draw(g, buttonAlpha);
        DrawingHelper.DrawOutlinedText(g, "R / Enter: Yeniden    Esc: Menü", 16f,
            new PointF(GameSettings.WorldWidth / 2f, 556),
            DrawingHelper.WithAlpha(Color.White, buttonAlpha), Color.FromArgb((int)(160 * buttonAlpha), 0, 0, 0), 3.5f, heavy: false);
        if (_assisted)
        {
            DrawingHelper.DrawOutlinedText(g, "Otopilot kullanıldı: skor rekor sayılmaz", 14f,
                new PointF(GameSettings.WorldWidth / 2f, 582),
                DrawingHelper.WithAlpha(Color.FromArgb(150, 255, 200), buttonAlpha), Color.FromArgb((int)(160 * buttonAlpha), 0, 0, 0), 3.5f, heavy: false);
        }
    }

    /// <summary>"OYUN BİTTİ" yazısı yukarıdan sekerek düşer.</summary>
    private void DrawTitle(Graphics g)
    {
        float t = DrawingHelper.EaseOutBack(_time / TitleDuration);
        float y = -60 + (150 - -60) * t;
        DrawingHelper.DrawOutlinedText(g, "OYUN BİTTİ", 56f, new PointF(GameSettings.WorldWidth / 2f, y),
            Color.FromArgb(255, 125, 80), Color.FromArgb(80, 25, 10), 10f);
    }

    /// <summary>Skor kartı aşağıdan kayarak gelir.</summary>
    private void DrawPanel(Graphics g)
    {
        float t = DrawingHelper.EaseOutCubic((_time - PanelStart) / PanelDuration);
        if (t <= 0) return;
        float offsetY = (1 - t) * 520f;

        GraphicsState state = g.Save();
        g.TranslateTransform(0, offsetY);

        RectangleF r = PanelRect;
        using (GraphicsPath shadow = DrawingHelper.RoundedRect(new RectangleF(r.X, r.Y + 6, r.Width, r.Height), 20))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 60, 30, 10)))
            g.FillPath(shadowBrush, shadow);

        using (GraphicsPath path = DrawingHelper.RoundedRect(r, 20))
        {
            using (var fill = new LinearGradientBrush(r, PanelFill, Color.FromArgb(250, 225, 175), LinearGradientMode.Vertical))
                g.FillPath(fill, path);
            using var border = new Pen(PanelBorder, 4f);
            g.DrawPath(border, path);
        }

        // Sol: madalya
        float medalX = r.X + 95;
        DrawLabel(g, "MADALYA", new PointF(medalX, r.Y + 30));
        float medalProgress = (_time - MedalStart) / 0.4f;
        float medalScale = _medal == Medal.None ? 1f : DrawingHelper.EaseOutBack(medalProgress);
        if (_medal == Medal.None || medalProgress > 0)
            MedalRenderer.Draw(g, new PointF(medalX, r.Y + 120), 46f * medalScale, _medal, _time);
        if (_medal != Medal.None && medalProgress > 0.6f)
            DrawLabel(g, MedalRenderer.GetName(_medal), new PointF(medalX, r.Y + 200));
        else if (_medal == Medal.None)
            DrawLabel(g, $"{ScoreManager.BronzeScore} puanda bronz", new PointF(medalX, r.Y + 200), 12f);

        // Sağ: skor (sayarak artar) ve en yüksek skor
        float scoreX = r.Right - 95;
        float countT = DrawingHelper.EaseOutCubic((_time - CountStart) / CountDuration);
        int shown = (int)MathF.Round(_score * countT);
        DrawLabel(g, "SKOR", new PointF(scoreX, r.Y + 30));
        DrawingHelper.DrawOutlinedText(g, shown.ToString(), 46f, new PointF(scoreX, r.Y + 76),
            Color.White, Color.FromArgb(90, 50, 20), 8f);
        DrawLabel(g, "EN İYİ", new PointF(scoreX, r.Y + 130));
        // Rekor kırıldıysa en iyi skor da skorla birlikte sayarak artar
        int bestShown = _newRecord ? shown : _best;
        DrawingHelper.DrawOutlinedText(g, bestShown.ToString(), 36f, new PointF(scoreX, r.Y + 170),
            Color.White, Color.FromArgb(90, 50, 20), 7f);

        if (_newRecord && countT >= 1f)
            DrawNewRecordBadge(g, new PointF(r.Right - 30, r.Y + 2));

        g.Restore(state);
    }

    private static void DrawLabel(Graphics g, string text, PointF at, float size = 16f) =>
        DrawingHelper.DrawOutlinedText(g, text, size, at, LabelColor, Color.Empty, 0f, shadow: false);

    /// <summary>Kartın sağ üst köşesinde nabız gibi atan kırmızı "YENİ REKOR!" etiketi.</summary>
    private void DrawNewRecordBadge(Graphics g, PointF at)
    {
        float pulse = 1f + 0.08f * MathF.Sin(_time * 8f);
        GraphicsState s = g.Save();
        g.TranslateTransform(at.X, at.Y);
        g.RotateTransform(12f);
        g.ScaleTransform(pulse, pulse);

        var badge = new RectangleF(-70, -18, 140, 36);
        using (GraphicsPath path = DrawingHelper.RoundedRect(badge, 10))
        {
            using (var fill = new LinearGradientBrush(badge, Color.FromArgb(255, 90, 80), Color.FromArgb(210, 30, 50), LinearGradientMode.Vertical))
                g.FillPath(fill, path);
            using var pen = new Pen(Color.White, 3f);
            g.DrawPath(pen, path);
        }
        DrawingHelper.DrawOutlinedText(g, "YENİ REKOR!", 19f, new PointF(0, 1), Color.White, Color.FromArgb(120, 10, 20), 4f, shadow: false);
        g.Restore(s);
    }
}
