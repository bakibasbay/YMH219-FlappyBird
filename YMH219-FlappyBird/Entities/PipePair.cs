using FlappyBird.Core;

namespace FlappyBird.Entities;

/// <summary>
/// Aralarında geçiş boşluğu bulunan üst + alt boru çifti.
/// Sağdan sola kayar; ekrandan çıkınca GameManager tarafından yeniden konumlandırılır.
/// </summary>
public class PipePair : GameObject
{
    public Pipe Top { get; } = new(isTop: true);
    public Pipe BottomPipe { get; } = new(isTop: false);

    /// <summary>Boşluğun dikey orta noktası.</summary>
    public float GapCenterY { get; private set; }

    /// <summary>Boşluğun yüksekliği.</summary>
    public float GapSize { get; private set; }

    /// <summary>Sola kayma hızı (px/s). Seviye arttıkça GameManager artırır.</summary>
    public float Speed { get; set; }

    /// <summary>Kuş bu çifti geçip puan aldı mı? (Aynı borudan iki kez puan alınmasın.)</summary>
    public bool Scored { get; set; }

    /// <summary>Boru tamamen ekranın solundan çıktı mı?</summary>
    public bool IsOffScreen => Right + Pipe.CapOverhang < 0;

    public PipePair(float x, float gapCenterY, float gapSize)
    {
        Width = Pipe.PipeWidth;
        Height = GameSettings.WorldHeight;
        Reposition(x, gapCenterY, gapSize);
    }

    /// <summary>Çifti yeni bir X konumuna ve yeni boşluk yüksekliğine taşır.</summary>
    public void Reposition(float x, float gapCenterY, float gapSize)
    {
        X = x;
        GapCenterY = gapCenterY;
        GapSize = gapSize;
        Scored = false;
        Layout();
    }

    /// <summary>Alt boruların konum ve boylarını boşluğa göre hesaplar.</summary>
    private void Layout()
    {
        float gapTop = GapCenterY - GapSize / 2f;
        float gapBottom = GapCenterY + GapSize / 2f;

        // Üst boru ekranın biraz üstünden başlar, boşluğun üst kenarında biter
        Top.X = X;
        Top.Y = -20f;
        Top.Height = gapTop - Top.Y;

        // Alt boru boşluğun alt kenarından zeminin altına kadar uzanır
        BottomPipe.X = X;
        BottomPipe.Y = gapBottom;
        BottomPipe.Height = GameSettings.GroundY + 10f - gapBottom;
    }

    /// <summary>Kuşun çarpışma kutusu borulardan birine değiyor mu?</summary>
    public bool Collides(RectangleF hitbox) =>
        Top.Intersects(hitbox) || BottomPipe.Intersects(hitbox);

    public override void Update(float dt)
    {
        X -= Speed * dt;
        Top.X = X;
        BottomPipe.X = X;
    }

    public override void Draw(Graphics g)
    {
        Top.Draw(g);
        BottomPipe.Draw(g);
    }
}
