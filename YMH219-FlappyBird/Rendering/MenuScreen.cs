using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Ana menü ekranı: animasyonlu oyun adı, Başla ve Çıkış butonları.
/// Butonlara tıklanınca olaylar (event) ile GameManager'a haber verir.
/// </summary>
public class MenuScreen
{
    private const string Title = "KANAT ÇIRP";
    private const float TitleSize = 62f;

    /// <summary>Başla butonuna basıldığında.</summary>
    public event Action? StartRequested;

    /// <summary>Çıkış butonuna basıldığında.</summary>
    public event Action? ExitRequested;

    public UiButton StartButton { get; }
    public UiButton ExitButton { get; }

    private float _time;

    // Başlıktaki her harfin genişliği (ilk çizimde bir kez hesaplanır)
    private float[]? _letterWidths;

    public MenuScreen()
    {
        StartButton = new UiButton("BAŞLA", new RectangleF(120, 478, 240, 62), Color.FromArgb(60, 185, 90)) { FontSize = 30 };
        ExitButton = new UiButton("ÇIKIŞ", new RectangleF(160, 556, 160, 46), Color.FromArgb(225, 85, 75)) { FontSize = 22 };
        StartButton.Clicked += () => StartRequested?.Invoke();
        ExitButton.Clicked += () => ExitRequested?.Invoke();
    }

    /// <summary>Menüdeki tüm butonlar (fare kontrolü için).</summary>
    public IEnumerable<UiButton> Buttons
    {
        get
        {
            yield return StartButton;
            yield return ExitButton;
        }
    }

    public void Update(float dt) => _time += dt;

    /// <summary>Tıklanan noktadaki butonu çalıştırır.</summary>
    public bool HandleClick(PointF p) => Buttons.Any(b => b.TryClick(p));

    /// <summary>Fare üzerindeki butonu vurgular.</summary>
    public void HandleMouseMove(PointF p)
    {
        foreach (UiButton b in Buttons)
            b.IsHovered = b.Contains(p);
    }

    public void Draw(Graphics g)
    {
        DrawTitle(g);

        DrawingHelper.DrawOutlinedText(g, "Flappy tarzı bir YMH219 oyunu", 17f,
            new PointF(GameSettings.WorldWidth / 2f, 182), Color.White, Color.FromArgb(160, 40, 60, 90), 4f, heavy: false);

        // Butonların arkasındaki yarı saydam kart
        var card = new RectangleF(36, 300, GameSettings.WorldWidth - 72, 316);
        using (GraphicsPath cardPath = DrawingHelper.RoundedRect(card, 22))
        using (var cardBrush = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
        using (var cardPen = new Pen(Color.FromArgb(140, 255, 255, 255), 2f))
        {
            g.FillPath(cardBrush, cardPath);
            g.DrawPath(cardPen, cardPath);
        }

        foreach (UiButton b in Buttons)
            b.Draw(g);

        // Zemin üzerinde kontrol ipuçları
        Color hint = Color.White;
        Color hintOutline = Color.FromArgb(170, 70, 50, 20);
        DrawingHelper.DrawOutlinedText(g, "SPACE / ↑ / Tık: Zıpla    P: Duraklat    R: Yeniden", 15f,
            new PointF(GameSettings.WorldWidth / 2f, GameSettings.GroundY + 42), hint, hintOutline, 3.5f, heavy: false);
        DrawingHelper.DrawOutlinedText(g, "M: Ses aç/kapa    Esc: Menü / Çıkış", 15f,
            new PointF(GameSettings.WorldWidth / 2f, GameSettings.GroundY + 66), hint, hintOutline, 3.5f, heavy: false);
    }

    /// <summary>
    /// Oyun adını harf harf, dalgalanan ve hafifçe sallanan şekilde çizer.
    /// </summary>
    private void DrawTitle(Graphics g)
    {
        _letterWidths ??= MeasureLetters();

        const float spacing = 2f;
        float total = _letterWidths.Sum() + spacing * (Title.Length - 1);
        float x = (GameSettings.WorldWidth - total) / 2f;
        float baseY = 118f;

        for (int i = 0; i < Title.Length; i++)
        {
            float w = _letterWidths[i];
            char c = Title[i];
            if (c != ' ')
            {
                // Her harf farklı fazda sinüs dalgasıyla yukarı-aşağı oynar
                float bob = MathF.Sin(_time * 3.2f + i * 0.55f) * 7f;
                float tilt = MathF.Sin(_time * 2.1f + i * 0.8f) * 5f;

                GraphicsState state = g.Save();
                g.TranslateTransform(x + w / 2f, baseY + bob);
                g.RotateTransform(tilt);

                using GraphicsPath path = DrawingHelper.CreateTextPath(c.ToString(), TitleSize, PointF.Empty);
                var gradRect = new RectangleF(-w, -TitleSize / 2f, w * 2, TitleSize);
                using var fill = new LinearGradientBrush(gradRect, Color.FromArgb(255, 240, 120),
                    Color.FromArgb(255, 140, 40), LinearGradientMode.Vertical);
                DrawingHelper.DrawOutlinedPath(g, path, fill, Color.FromArgb(110, 45, 20), 9f);
                g.Restore(state);
            }
            x += w + spacing;
        }
    }

    /// <summary>Her harfin piksel genişliğini yazı yolundan ölçer.</summary>
    private static float[] MeasureLetters()
    {
        var widths = new float[Title.Length];
        for (int i = 0; i < Title.Length; i++)
        {
            if (Title[i] == ' ')
            {
                widths[i] = TitleSize * 0.28f;
                continue;
            }
            using GraphicsPath p = DrawingHelper.CreateTextPath(Title[i].ToString(), TitleSize, PointF.Empty);
            widths[i] = p.GetBounds().Width;
        }
        return widths;
    }
}
