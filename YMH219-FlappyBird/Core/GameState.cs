namespace FlappyBird.Core;

/// <summary>
/// Oyunun durum makinesindeki durumlar.
/// </summary>
public enum GameState
{
    /// <summary>Ana menü: zorluk seçimi, Başla / Çıkış.</summary>
    Menu,
    /// <summary>Hazır ekranı: kuş süzülür, ilk zıplamayı bekler.</summary>
    Ready,
    /// <summary>Oyun oynanıyor.</summary>
    Playing,
    /// <summary>Oyun duraklatıldı (P).</summary>
    Paused,
    /// <summary>Kuş çarptı, Game Over paneli gösterilir.</summary>
    GameOver
}
