using FlappyBird.Core;
using FlappyBird.Services;

namespace FlappyBird.Tests;

/// <summary>En yüksek skorun dosyaya yazılıp okunması ve hata durumları.</summary>
public class HighScoreServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ymh219-fb-test-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "highscores.json");

    [Fact]
    public void Skor_ZorlukBazindaKaydedilipOkunur()
    {
        var service = new HighScoreService(FilePath);
        Assert.True(service.TrySetHighScore(Difficulty.Zor, 12));

        var reloaded = new HighScoreService(FilePath);
        Assert.Equal(12, reloaded.GetHighScore(Difficulty.Zor));
        Assert.Equal(0, reloaded.GetHighScore(Difficulty.Kolay));
    }

    [Fact]
    public void DusukSkor_RekoruEzmez()
    {
        var service = new HighScoreService(FilePath);
        service.TrySetHighScore(Difficulty.Normal, 20);
        Assert.False(service.TrySetHighScore(Difficulty.Normal, 5));
        Assert.Equal(20, new HighScoreService(FilePath).GetHighScore(Difficulty.Normal));
    }

    [Fact]
    public void BozukDosya_CokmezVeSifirDoner()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ bu geçerli json değil");

        var service = new HighScoreService(FilePath);
        Assert.Equal(0, service.GetHighScore(Difficulty.Normal));
        Assert.NotNull(service.LastError);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
