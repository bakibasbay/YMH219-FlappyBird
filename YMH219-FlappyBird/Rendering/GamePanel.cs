using System.Drawing.Drawing2D;
using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Oyunun çizildiği özel kontrol. Çift tamponlama (double buffer) sayesinde titreme olmaz.
/// Oyun dünyası sabit 480x720 mantıksal boyuttadır; panel boyutuna göre ölçeklenir.
/// </summary>
public class GamePanel : Control
{
    /// <summary>Çizimi yapılacak oyun yöneticisi.</summary>
    public GameManager? Game { get; set; }

    /// <summary>Dünya koordinatından ekran pikseline ölçek katsayısı.</summary>
    public float ViewScale { get; private set; } = 1f;

    // Dünyanın panel içinde ortalanması için kaydırma miktarı (letterbox)
    private PointF _viewOffset;

    public GamePanel()
    {
        // Titremeyi önleyen ayarlar: tüm çizimi biz yaparız ve bellekte tamponlanır
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
        BackColor = Color.Black;
    }

    /// <summary>
    /// Ok tuşları ve Space normalde form gezintisi için kullanılır;
    /// burada bunları oyun girdisi olarak kabul ediyoruz.
    /// </summary>
    protected override bool IsInputKey(Keys keyData)
    {
        switch (keyData & Keys.KeyCode)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
            case Keys.Space:
                return true;
        }
        return base.IsInputKey(keyData);
    }

    /// <summary>Ekrandaki bir pikseli oyun dünyası koordinatına çevirir.</summary>
    public PointF ToWorld(Point p) =>
        new((p.X - _viewOffset.X) / ViewScale, (p.Y - _viewOffset.Y) / ViewScale);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Color.Black);
        if (Game == null) return;

        // Dünyayı en-boy oranını koruyarak panele sığdır
        ViewScale = Math.Min(Width / GameSettings.WorldWidth, Height / GameSettings.WorldHeight);
        _viewOffset = new PointF(
            (Width - GameSettings.WorldWidth * ViewScale) / 2f,
            (Height - GameSettings.WorldHeight * ViewScale) / 2f);

        g.TranslateTransform(_viewOffset.X, _viewOffset.Y);
        g.ScaleTransform(ViewScale, ViewScale);
        g.SetClip(new RectangleF(0, 0, GameSettings.WorldWidth, GameSettings.WorldHeight));

        // Kenarları yumuşak çizim
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        Game.Draw(g);
    }
}
