using FlappyBird.Core;
using FlappyBird.Services;

namespace FlappyBird.Tests;

/// <summary>
/// Oyun yöneticisinin durum makinesi ve oyun döngüsü testleri.
/// Pencere açılmadan Update() elle çağrılarak simüle edilir.
/// </summary>
public class GameManagerTests : IDisposable
{
    private const float Dt = 1f / 60f;
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ymh219-fb-gm-" + Guid.NewGuid().ToString("N"));

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

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
