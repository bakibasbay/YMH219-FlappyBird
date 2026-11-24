using System.Drawing.Drawing2D;

namespace FlappyBird.Core;

/// <summary>
/// Oyunun beyni: oyun döngüsünü (Update/Draw) ve oyun durumunu yönetir.
/// Form yalnızca girdileri buraya iletir ve her karede Update + Draw çağırır.
/// </summary>
public class GameManager
{
    /// <summary>Oyunun şu anki durumu.</summary>
    public GameState State { get; private set; } = GameState.Ready;

    /// <summary>Aktif fizik/hız ayarları.</summary>
    public GameSettings Settings { get; private set; } = new();

    /// <summary>Oyun başladığından beri geçen toplam süre (animasyonlar için).</summary>
    public float TotalTime { get; private set; }

    /// <summary>
    /// Bir kare ilerletir. dt: önceki kareden bu yana geçen süre (saniye).
    /// Hareketler dt ile çarpıldığı için FPS düşse de hız sabit kalır.
    /// </summary>
    public void Update(float dt)
    {
        TotalTime += dt;
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

        // Zemin
        using var ground = new SolidBrush(Color.FromArgb(222, 200, 140));
        g.FillRectangle(ground, 0, GameSettings.GroundY, GameSettings.WorldWidth, GameSettings.GroundHeight);
    }
}
