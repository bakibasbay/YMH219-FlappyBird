using System.Media;

namespace FlappyBird.Services;

/// <summary>Oyunda çalınan ses efektleri.</summary>
public enum SoundEffect
{
    Jump,
    Score,
    Hit,
    LevelUp
}

/// <summary>
/// Ses efektlerini çalar. Harici ses dosyası kullanılmaz: kısa WAV sesleri
/// program açılırken matematiksel olarak (sinüs, kare dalga, gürültü) üretilir.
/// Ses aygıtı yoksa veya çalma başarısız olursa oyun çökmez, sessiz devam eder.
/// </summary>
public sealed class SoundService : IDisposable
{
    private const int SampleRate = 22050;

    /// <summary>Kullanıcı sesi açtı mı? (M tuşu)</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Sesler başarıyla hazırlandı mı?</summary>
    public bool IsAvailable { get; private set; }

    private readonly Dictionary<SoundEffect, SoundPlayer> _players = new();
    private readonly List<MemoryStream> _streams = new();

    public SoundService(bool enabled = true)
    {
        IsEnabled = enabled;
        try
        {
            Register(SoundEffect.Jump, CreateJump());
            Register(SoundEffect.Score, CreateScore());
            Register(SoundEffect.Hit, CreateHit());
            Register(SoundEffect.LevelUp, CreateLevelUp());
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            IsAvailable = false; // ses hazırlanamadı: oyun sessiz çalışır
        }
    }

    private void Register(SoundEffect effect, float[] samples)
    {
        var stream = new MemoryStream(CreateWav(samples));
        var player = new SoundPlayer(stream);
        player.Load(); // WAV başlığını şimdi doğrula, oyun sırasında takılma olmasın
        _streams.Add(stream);
        _players[effect] = player;
    }

    /// <summary>Sesi aç/kapa. Yeni durumu döndürür.</summary>
    public bool Toggle()
    {
        IsEnabled = !IsEnabled;
        return IsEnabled;
    }

    /// <summary>Efekti eşzamansız (oyunu bekletmeden) çalar.</summary>
    public void Play(SoundEffect effect)
    {
        if (!IsEnabled || !IsAvailable) return;
        try
        {
            if (_players.TryGetValue(effect, out SoundPlayer? player))
                player.Play();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            // Ses çalınamadıysa oyunu durdurmaya değmez
        }
    }

    public void Dispose()
    {
        foreach (SoundPlayer p in _players.Values) p.Dispose();
        foreach (MemoryStream s in _streams) s.Dispose();
        _players.Clear();
        _streams.Clear();
    }

    // ================= Ses üretimi =================

    /// <summary>Zıplama: yukarı kayan kısa "vıp" sesi (500 → 950 Hz).</summary>
    private static float[] CreateJump()
    {
        float duration = 0.11f;
        return Generate(duration, (t, phase) =>
        {
            float wave = 0.6f * MathF.Sin(phase) + 0.4f * MathF.Sign(MathF.Sin(phase)) * 0.5f;
            return wave * Envelope(t, duration, 0.005f) * 0.35f;
        }, t => 500f + 450f * (t / duration));
    }

    /// <summary>Skor: iki notalı parlak "di-ding".</summary>
    private static float[] CreateScore()
    {
        float[] a = Generate(0.07f, (t, ph) => (MathF.Sin(ph) + 0.3f * MathF.Sin(2 * ph)) * Envelope(t, 0.07f, 0.003f) * 0.3f, _ => 988f);
        float[] b = Generate(0.18f, (t, ph) => (MathF.Sin(ph) + 0.3f * MathF.Sin(2 * ph)) * MathF.Exp(-t * 14f) * 0.32f, _ => 1480f);
        return a.Concat(b).ToArray();
    }

    /// <summary>Çarpma: gürültü patlaması + alçalan kalın "güm".</summary>
    private static float[] CreateHit()
    {
        var rng = new Random(7);
        float smooth = 0;
        return Generate(0.32f, (t, ph) =>
        {
            // Basit alçak geçiren filtre: gürültüyü yumuşatıp "tok" yapar
            smooth += ((float)rng.NextDouble() * 2 - 1 - smooth) * 0.35f;
            float noise = smooth * MathF.Exp(-t * 16f) * 0.7f;
            float thud = MathF.Sin(ph) * MathF.Exp(-t * 9f) * 0.6f;
            return Math.Clamp(noise + thud, -1f, 1f) * 0.6f;
        }, t => 130f - 70f * t);
    }

    /// <summary>Seviye atlama: yükselen dört notalı arpej (Do-Mi-Sol-Do).</summary>
    private static float[] CreateLevelUp()
    {
        float[] notes = { 523f, 659f, 784f, 1047f };
        var result = new List<float>();
        for (int i = 0; i < notes.Length; i++)
        {
            float d = i == notes.Length - 1 ? 0.22f : 0.08f;
            float f = notes[i];
            // Üçgen dalga: yumuşak, "oyun" sesi
            result.AddRange(Generate(d, (t, ph) =>
            {
                float tri = 2f / MathF.PI * MathF.Asin(MathF.Sin(ph));
                return tri * Envelope(t, d, 0.004f) * 0.32f;
            }, _ => f));
        }
        return result.ToArray();
    }

    /// <summary>
    /// Örnekleri üretir. frequency(t) ile anlık frekans verilir; faz biriktirilerek
    /// hesaplandığı için frekans değişirken ses "tıkırdamaz".
    /// </summary>
    private static float[] Generate(float duration, Func<float, float, float> sample, Func<float, float> frequency)
    {
        int count = (int)(duration * SampleRate);
        var data = new float[count];
        float phase = 0;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            phase += MathF.Tau * frequency(t) / SampleRate;
            data[i] = sample(t, phase);
        }
        return data;
    }

    /// <summary>Hızlı yükselen, sona doğru sönen ses zarfı (tık sesini önler).</summary>
    private static float Envelope(float t, float duration, float attack)
    {
        if (t < attack) return t / attack;
        float k = 1f - (t - attack) / (duration - attack);
        return Math.Max(0, k * k);
    }

    /// <summary>Örnekleri 16 bit mono PCM WAV dosya baytlarına çevirir.</summary>
    private static byte[] CreateWav(float[] samples)
    {
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            int dataSize = samples.Length * 2;
            w.Write("RIFF".ToCharArray());
            w.Write(36 + dataSize);
            w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray());
            w.Write(16);                 // fmt bölüm boyutu
            w.Write((short)1);           // PCM
            w.Write((short)1);           // mono
            w.Write(SampleRate);
            w.Write(SampleRate * 2);     // bayt/saniye
            w.Write((short)2);           // blok hizası
            w.Write((short)16);          // bit derinliği
            w.Write("data".ToCharArray());
            w.Write(dataSize);
            foreach (float s in samples)
                w.Write((short)(Math.Clamp(s, -1f, 1f) * short.MaxValue));
        }
        return ms.ToArray();
    }
}
