using FlappyBird.Core;
using FlappyBird.Services;
using Xunit.Abstractions;

namespace FlappyBird.Tests;

/// <summary>
/// Oyun yöneticisinin durum makinesi ve oyun döngüsü testleri.
/// Pencere açılmadan Update() elle çağrılarak simüle edilir.
/// </summary>
public class GameManagerTests : IDisposable
{
    private const float Dt = 1f / 60f;
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ymh219-fb-gm-" + Guid.NewGuid().ToString("N"));
    private readonly ITestOutputHelper _output;

    public GameManagerTests(ITestOutputHelper output) => _output = output;

    private GameManager CreateGame() =>
        new(new HighScoreService(Path.Combine(_dir, "hs.json")), new SoundService(enabled: false));

    private static void Run(GameManager game, float seconds)
    {
        for (int i = 0; i < seconds / Dt; i++) game.Update(Dt);
    }

    [Fact]
    public void Akis_MenuHazirOynaniyor()
    {
        using var game = CreateGame();
        Assert.Equal(GameState.Menu, game.State);
        game.HandleKeyDown(Keys.Enter);
        Assert.Equal(GameState.Ready, game.State);
        game.HandleKeyDown(Keys.Space);
        Assert.Equal(GameState.Playing, game.State);
    }

    [Fact]
    public void GirdiYoksa_KusDuserVeOyunBiter()
    {
        using var game = CreateGame();
        game.StartNewGame();
        game.HandleKeyDown(Keys.Space);
        Run(game, 3f);
        Assert.Equal(GameState.GameOver, game.State);
    }

    [Fact]
    public void Duraklatma_HareketiDondurur()
    {
        using var game = CreateGame();
        game.StartNewGame();
        game.HandleKeyDown(Keys.Space);
        game.HandleKeyDown(Keys.P);
        Assert.Equal(GameState.Paused, game.State);

        float y = game.Bird.Y, x = game.Pipes[0].X;
        Run(game, 1f);
        Assert.Equal(y, game.Bird.Y);
        Assert.Equal(x, game.Pipes[0].X);

        game.HandleKeyDown(Keys.P);
        Assert.Equal(GameState.Playing, game.State);
    }

    [Fact]
    public void R_YenidenBaslatir_Esc_MenuyeDoner()
    {
        using var game = CreateGame();
        game.StartNewGame();
        game.HandleKeyDown(Keys.Space);
        Run(game, 3f); // oyun biter
        game.HandleKeyDown(Keys.R);
        Assert.Equal(GameState.Ready, game.State);
        Assert.Equal(0, game.Score.Score);
        game.HandleKeyDown(Keys.Escape);
        Assert.Equal(GameState.Menu, game.State);
    }

    [Fact]
    public void Borular_EkrandanCikincaSagdaYenidenKonumlanir()
    {
        using var game = CreateGame();
        game.HandleKeyDown(Keys.F2); // otopilot: kuş uzun süre hayatta kalsın
        game.StartNewGame();

        var seenGaps = new HashSet<float>();
        for (int i = 0; i < 20 * 60 && game.State != GameState.GameOver; i++)
        {
            game.Update(Dt);
            Assert.Equal(GameManager.PipePairCount, game.Pipes.Count);
            foreach (var p in game.Pipes)
            {
                seenGaps.Add(p.GapCenterY);
                Assert.True(p.Right + 10 > 0, "Ekrandan çıkan boru geride kalmamalı");
                Assert.True(p.GapCenterY - p.GapSize / 2 > 0 && p.GapCenterY + p.GapSize / 2 < GameSettings.GroundY);
            }
        }
        // Yeniden konumlanan borular farklı (rastgele) boşluk yükseklikleri almalı
        Assert.True(seenGaps.Count > GameManager.PipePairCount);
    }

    [Theory]
    [InlineData(Difficulty.Kolay)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Zor)]
    public void Otopilot_BorulardanGecerekSkorKazanir(Difficulty difficulty)
    {
        using var game = CreateGame();
        game.SetDifficulty(difficulty);
        game.HandleKeyDown(Keys.F2);

        var scores = new List<int>();
        for (int run = 0; run < 8; run++)
        {
            game.StartNewGame();
            for (int i = 0; i < 90 * 60 && game.State != GameState.GameOver; i++)
                game.Update(Dt);
            scores.Add(game.Score.Score);
        }
        _output.WriteLine($"{difficulty}: {string.Join(", ", scores)}");
        // Otopilot kusursuz değildir; ortalamada en az 2 seviye (10 boru) geçmesi beklenir
        Assert.True(scores.Average() >= 10, $"Ortalama skor çok düşük: {scores.Average()}");
        Assert.All(scores, s => Assert.True(s >= 1, "Her oyunda en az bir boru geçilmeli"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
