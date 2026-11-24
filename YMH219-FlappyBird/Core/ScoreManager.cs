namespace FlappyBird.Core;

/// <summary>
/// Skor, seviye ve en yüksek skor bilgilerini tutar.
/// Her PointsPerLevel puanda bir seviye atlanır.
/// </summary>
public class ScoreManager
{
    /// <summary>Kaç puanda bir seviye atlanacağı.</summary>
    public const int PointsPerLevel = 5;

    /// <summary>Bu oyundaki skor (geçilen boru sayısı).</summary>
    public int Score { get; private set; }

    /// <summary>Şu anki seviye (1'den başlar).</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Seçili zorluktaki en yüksek skor.</summary>
    public int HighScore { get; private set; }

    /// <summary>Bu oyunda en yüksek skor geçildi mi?</summary>
    public bool IsNewRecord { get; private set; }

    /// <summary>Yeni oyun için skoru sıfırlar; en yüksek skoru dışarıdan alır.</summary>
    public void Reset(int highScore)
    {
        Score = 0;
        Level = 1;
        HighScore = highScore;
        IsNewRecord = false;
    }

    /// <summary>
    /// Skoru 1 artırır. Seviye atlandıysa true döner.
    /// </summary>
    public bool AddPoint()
    {
        Score++;

        // Rekor kırıldıysa en yüksek skoru anında güncelle
        if (Score > HighScore)
        {
            HighScore = Score;
            IsNewRecord = true;
        }

        int newLevel = Score / PointsPerLevel + 1;
        if (newLevel > Level)
        {
            Level = newLevel;
            return true;
        }
        return false;
    }
}
