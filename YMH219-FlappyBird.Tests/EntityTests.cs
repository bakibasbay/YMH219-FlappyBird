using FlappyBird.Core;
using FlappyBird.Entities;

namespace FlappyBird.Tests;

/// <summary>Kuş fiziği ve boru çarpışma testleri.</summary>
public class EntityTests
{
    private const float Dt = 1f / 60f;

    [Fact]
    public void Ziplama_YukariHizVerir()
    {
        var bird = new Bird(100, 300);
        bird.Jump();
        Assert.Equal(BirdMode.Flying, bird.Mode);
        Assert.True(bird.VelocityY < 0);
    }

    [Fact]
    public void YerCekimi_KusuAsagiCeker()
    {
        var bird = new Bird(100, 300);
        bird.Jump();
        for (int i = 0; i < 60; i++) bird.Update(Dt);
        Assert.True(bird.Y > 300, "1 saniye sonra kuş başlangıçtan aşağıda olmalı");
        Assert.True(bird.VelocityY > 0);
    }

    [Fact]
    public void Kus_ZemindeDurur()
    {
        var bird = new Bird(100, 300);
        bird.Jump();
        for (int i = 0; i < 600; i++) bird.Update(Dt);
        Assert.True(bird.IsOnGround);
        Assert.Equal(GameSettings.GroundY, bird.Bottom, 2);
    }

    [Fact]
    public void Hitbox_GovdedenKucukVeIcinde()
    {
        var bird = new Bird(100, 300);
        RectangleF body = bird.Bounds, hit = bird.Hitbox;
        Assert.True(hit.Width < body.Width && hit.Height < body.Height);
        Assert.True(body.Contains(hit));
    }

    [Fact]
    public void BoruCifti_BosluktaCarpmaz_BoruyaDegerseCarpar()
    {
        var pair = new PipePair(200, gapCenterY: 300, gapSize: 170);

        var inGap = new RectangleF(210, 290, 30, 20);
        var inTopPipe = new RectangleF(210, 150, 30, 20);
        var inBottomPipe = new RectangleF(210, 450, 30, 20);

        Assert.False(pair.Collides(inGap));
        Assert.True(pair.Collides(inTopPipe));
        Assert.True(pair.Collides(inBottomPipe));
    }

    [Fact]
    public void BoruCifti_SolaKayarVeEkrandanCikar()
    {
        var pair = new PipePair(10, 300, 170) { Speed = 200 };
        Assert.False(pair.IsOffScreen);
        for (int i = 0; i < 60; i++) pair.Update(Dt);
        Assert.True(pair.X < 10);
        Assert.True(pair.IsOffScreen);
    }

    [Fact]
    public void Parcacik_OmruBitinceAktifligiKalkar()
    {
        Particle p = new StarParticle(0, 0, 10, 10, 0.2f, 10, Color.Gold);
        for (int i = 0; i < 20; i++) p.Update(Dt);
        Assert.False(p.IsActive);
    }
}
