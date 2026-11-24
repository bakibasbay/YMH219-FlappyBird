using FlappyBird.Core;
using FlappyBird.Entities;

namespace FlappyBird.Tests;

/// <summary>Skor, seviye, madalya ve zorluk ayarları testleri.</summary>
public class CoreTests
{
    [Fact]
    public void AddPoint_SkoruBirArtirir()
    {
        var score = new ScoreManager();
        score.Reset(0);
        score.AddPoint();
        Assert.Equal(1, score.Score);
    }

    [Fact]
    public void Her5PuandaSeviyeAtlanir()
    {
        var score = new ScoreManager();
        score.Reset(0);
        var levelUps = new List<int>();
        for (int i = 1; i <= 12; i++)
            if (score.AddPoint()) levelUps.Add(i);

        Assert.Equal(new[] { 5, 10 }, levelUps);
        Assert.Equal(3, score.Level);
    }

    [Fact]
    public void RekorGecilinceEnYuksekSkorGuncellenir()
    {
        var score = new ScoreManager();
        score.Reset(2);
        score.AddPoint();
        score.AddPoint();
        Assert.False(score.IsNewRecord);
        score.AddPoint();
        Assert.True(score.IsNewRecord);
        Assert.Equal(3, score.HighScore);
    }

    [Fact]
    public void OtopilotluOyundaRekorSayilmaz()
    {
        var score = new ScoreManager();
        score.Reset(1);
        score.MarkAssisted();
        for (int i = 0; i < 5; i++) score.AddPoint();
        Assert.False(score.IsNewRecord);
        Assert.Equal(1, score.HighScore);
    }

    [Theory]
    [InlineData(0, Medal.None)]
    [InlineData(9, Medal.None)]
    [InlineData(10, Medal.Bronze)]
    [InlineData(19, Medal.Bronze)]
    [InlineData(20, Medal.Silver)]
    [InlineData(30, Medal.Gold)]
    [InlineData(99, Medal.Gold)]
    public void MadalyaEsikleriDogru(int score, Medal expected) =>
        Assert.Equal(expected, ScoreManager.GetMedal(score));

    [Fact]
    public void ZorlukSeviyeleriFarkliAyarlarKullanir()
    {
        var easy = GameSettings.Create(Difficulty.Kolay);
        var normal = GameSettings.Create(Difficulty.Normal);
        var hard = GameSettings.Create(Difficulty.Zor);

        Assert.True(easy.PipeGap > normal.PipeGap && normal.PipeGap > hard.PipeGap);
        Assert.True(easy.BasePipeSpeed < normal.BasePipeSpeed && normal.BasePipeSpeed < hard.BasePipeSpeed);
        Assert.True(easy.Gravity < normal.Gravity && normal.Gravity < hard.Gravity);
    }

    [Fact]
    public void BoruHiziSeviyeyleArtarVeSinirdaDurur()
    {
        var s = GameSettings.Create(Difficulty.Normal);
        Assert.Equal(s.BasePipeSpeed, s.GetPipeSpeed(1));
        Assert.True(s.GetPipeSpeed(2) > s.GetPipeSpeed(1));
        Assert.Equal(s.BasePipeSpeed * GameSettings.MaxSpeedMultiplier, s.GetPipeSpeed(100), 3);
    }

    [Theory]
    [InlineData(Difficulty.Kolay)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Zor)]
    public void UcBoruCiftiEkraniKesintisizDoldurur(Difficulty d)
    {
        // Yeniden konumlanan boru ekranın sağ dışında doğmalı (aniden belirmemeli)
        var s = GameSettings.Create(d);
        Assert.True(GameManager.PipePairCount * s.PipeSpacing >= GameSettings.WorldWidth + Pipe.PipeWidth + 2 * Pipe.CapOverhang);
    }
}
