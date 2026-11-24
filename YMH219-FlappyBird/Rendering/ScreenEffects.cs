using FlappyBird.Core;

namespace FlappyBird.Rendering;

/// <summary>
/// Ekran efektleri: çarpışmada kısa ekran sarsıntısı ve beyaz flaş.
/// </summary>
public class ScreenEffects
{
    private float _shakeTime;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private float _flash; // 0..1 arası flaş yoğunluğu
    private readonly Random _rng = new();

    /// <summary>Ekranı belirtilen süre ve şiddetle sarsar.</summary>
    public void Shake(float duration, float magnitude)
    {
        // Daha güçlü bir sarsıntı varsa onu ezme
        if (magnitude * duration < _shakeMagnitude * _shakeTime) return;
        _shakeDuration = _shakeTime = duration;
        _shakeMagnitude = magnitude;
    }

    /// <summary>Ekranı bir anlığına beyaza boyar, sonra hızla söner.</summary>
    public void Flash(float strength = 1f) => _flash = Math.Max(_flash, strength);

    public void Reset()
    {
        _shakeTime = 0;
        _flash = 0;
    }

    public void Update(float dt)
    {
        _shakeTime = Math.Max(0, _shakeTime - dt);
        _flash = Math.Max(0, _flash - dt * 3.5f);
    }

    /// <summary>
    /// Bu karedeki sarsıntı kayması. Süre azaldıkça şiddet de azalır.
    /// </summary>
    public PointF GetShakeOffset()
    {
        if (_shakeTime <= 0) return PointF.Empty;
        float power = _shakeMagnitude * (_shakeTime / _shakeDuration);
        return new PointF(
            (float)(_rng.NextDouble() * 2 - 1) * power,
            (float)(_rng.NextDouble() * 2 - 1) * power);
    }

    public void DrawFlash(Graphics g)
    {
        if (_flash <= 0) return;
        using var b = new SolidBrush(Color.FromArgb((int)(230 * _flash), 255, 255, 255));
        g.FillRectangle(b, -20, -20, GameSettings.WorldWidth + 40, GameSettings.WorldHeight + 40);
    }
}
