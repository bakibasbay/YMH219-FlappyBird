using System.Text.Json;
using FlappyBird.Core;

namespace FlappyBird.Services;

/// <summary>
/// En yüksek skorları her zorluk için ayrı ayrı JSON dosyasında saklar.
/// Dosya bozuk/erişilemez olsa bile oyun çökmez; skorlar 0 kabul edilir.
/// Varsayılan konum: %AppData%\YMH219-FlappyBird\highscores.json
/// </summary>
public class HighScoreService
{
    /// <summary>Skorların kaydedildiği dosyanın tam yolu.</summary>
    public string FilePath { get; }

    /// <summary>Son dosya işleminde oluşan hata (yoksa null).</summary>
    public string? LastError { get; private set; }

    // Anahtar: zorluk adı ("Kolay", "Normal", "Zor"), değer: en yüksek skor
    private Dictionary<string, int> _scores = new();

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YMH219-FlappyBird", "highscores.json");

    public HighScoreService() : this(DefaultFilePath) { }

    public HighScoreService(string filePath)
    {
        FilePath = filePath;
        Load();
    }

    /// <summary>Verilen zorluktaki en yüksek skoru döndürür (kayıt yoksa 0).</summary>
    public int GetHighScore(Difficulty difficulty) =>
        _scores.TryGetValue(difficulty.ToString(), out int score) ? score : 0;

    /// <summary>
    /// Skor mevcut rekordan büyükse kaydeder ve true döner.
    /// </summary>
    public bool TrySetHighScore(Difficulty difficulty, int score)
    {
        if (score <= GetHighScore(difficulty)) return false;
        _scores[difficulty.ToString()] = score;
        Save();
        return true;
    }

    /// <summary>Dosyadan skorları okur. Hata olursa boş tabloyla devam eder.</summary>
    private void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            if (data == null) return;

            // Negatif ya da anlamsız değerleri ayıkla
            _scores = data.Where(kv => kv.Value >= 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            LastError = "En yüksek skor dosyası okunamadı: " + ex.Message;
            _scores = new Dictionary<string, int>();
        }
    }

    /// <summary>Skorları dosyaya yazar. Başarılıysa true döner.</summary>
    public bool Save()
    {
        try
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            LastError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LastError = "En yüksek skor kaydedilemedi: " + ex.Message;
            return false;
        }
    }
}
