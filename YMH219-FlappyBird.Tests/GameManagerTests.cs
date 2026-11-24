using FlappyBird.Core;
using FlappyBird.Entities;
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

    [Fact]
    public void Borular_EkrandanCikincaSagdaRastgeleBoslukla_YenidenKonumlanir()
    {
        using var game = CreateGame();
        game.StartNewGame();
        game.HandleKeyDown(Keys.Space);

        var spacing = game.Settings.PipeSpacing;
        var previousX = game.Pipes.Select(p => p.X).ToArray();
        var seenGaps = new HashSet<float>(game.Pipes.Select(p => p.GapCenterY));
        int recycled = 0;

        for (int frame = 0; frame < 15 * 60; frame++)
        {
            // Kuşu doğrudan sabitle: her karede önündeki boşluğun ortasına koy (oyun bitmesin)
            float hitLeft = game.Bird.Hitbox.Left;
            var ahead = game.Pipes.Where(p => p.Right + Pipe.CapOverhang > hitLeft).OrderBy(p => p.X).First();
            game.Bird.Y = ahead.GapCenterY - game.Bird.Height / 2f;

            game.Update(Dt);
            Assert.Equal(GameState.Playing, game.State);
            Assert.Equal(GameManager.PipePairCount, game.Pipes.Count);

            for (int i = 0; i < game.Pipes.Count; i++)
            {
                PipePair p = game.Pipes[i];
                if (p.X > previousX[i])
                {
                    // X arttıysa bu çift yeniden konumlandı: ekranın sağ dışında,
                    // en sağdaki diğer çiftin bir aralık arkasında doğmalı
                    recycled++;
                    float otherMax = game.Pipes.Where(o => o != p).Max(o => o.X);
                    Assert.True(p.X - Pipe.CapOverhang > GameSettings.WorldWidth, "Yeni boru ekranın dışında doğmalı");
                    // Aynı karede listede sonra gelen çiftler bir adım daha kayabilir: 1 karelik tolerans
                    Assert.Equal(otherMax + spacing, p.X, game.PipeSpeed * Dt + 0.5f);
                    Assert.False(p.Scored);
                    Assert.InRange(p.GapCenterY - p.GapSize / 2f, 0f, GameSettings.GroundY);
                    Assert.InRange(p.GapCenterY + p.GapSize / 2f, 0f, GameSettings.GroundY);
                    seenGaps.Add(p.GapCenterY);
                }
                // Ekrandan çıkan boru solda birikmemeli
                Assert.True(p.Right + Pipe.CapOverhang > -game.PipeSpeed * Dt - 1f);
                previousX[i] = p.X;
            }
        }

        Assert.True(recycled >= 3, $"En az 3 yeniden konumlanma beklenirdi, olan: {recycled}");
        // Yeniden konumlanan borular rastgele, farklı boşluk yükseklikleri almalı
        Assert.True(seenGaps.Count > GameManager.PipePairCount);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
