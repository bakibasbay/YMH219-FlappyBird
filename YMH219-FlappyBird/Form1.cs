using System.Diagnostics;
using FlappyBird.Core;
using FlappyBird.Rendering;

namespace FlappyBird;

/// <summary>
/// Oyunun ana penceresi. Oyun mantığı burada değil, GameManager sınıfındadır.
/// Form yalnızca zamanlayıcıyı çalıştırır ve klavye/fare olaylarını iletir.
/// </summary>
public partial class Form1 : Form
{
    private readonly GameManager _game;
    private readonly GamePanel _panel;
    private readonly System.Windows.Forms.Timer _timer;

    // Delta time ölçümü için kronometre
    private readonly Stopwatch _clock = new();
    private double _lastTime;

    public Form1()
    {
        InitializeComponent();

        _game = new GameManager();

        // Çizim panelini oluştur ve formu doldur
        _panel = new GamePanel { Dock = DockStyle.Fill, Game = _game };
        Controls.Add(_panel);

        // Pencere boyutunu DPI'ya göre ayarla, ekrana sığdır
        FitWindowToScreen();

        // ~60 FPS oyun döngüsü (16 ms)
        _timer = new System.Windows.Forms.Timer(components) { Interval = 16 };
        _timer.Tick += Timer_Tick;

        // Girdi olayları: klavye form üzerinden (KeyPreview), fare panel üzerinden
        KeyDown += Form1_KeyDown;
        _panel.MouseDown += Panel_MouseDown;
        _panel.MouseMove += (_, e) => _game.HandleMouseMove(_panel.ToWorld(e.Location));

        // Menüden Çıkış seçilince pencereyi kapat
        _game.ExitRequested += (_, _) => Close();

        // Kapanırken zamanlayıcıyı durdur ve ses kaynaklarını bırak
        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _game.Dispose();
        };

        Shown += (_, _) =>
        {
            _panel.Focus();
            _clock.Start();
            _timer.Start();
        };
    }

    /// <summary>
    /// Pencereyi 480x720 oranında, ekran ölçeğine göre büyütür ama ekrandan taşırmaz.
    /// </summary>
    private void FitWindowToScreen()
    {
        float dpiScale = DeviceDpi / 96f;
        Rectangle area = Screen.FromControl(this).WorkingArea;
        float maxScale = (area.Height - 60) / GameSettings.WorldHeight;
        float scale = Math.Min(dpiScale, maxScale);
        ClientSize = new Size((int)(GameSettings.WorldWidth * scale), (int)(GameSettings.WorldHeight * scale));
    }

    /// <summary>
    /// Her tikte geçen süreyi (dt) hesaplar, oyunu günceller ve yeniden çizdirir.
    /// </summary>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)(now - _lastTime);
        _lastTime = now;

        // Pencere sürüklenirken vb. oluşan büyük sıçramaları sınırla
        dt = Math.Clamp(dt, 0f, 0.05f);

        _game.Update(dt);
        _panel.Invalidate();
    }

    /// <summary>Klavye tuşunu oyun yöneticisine iletir.</summary>
    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        _game.HandleKeyDown(e.KeyCode);
        e.Handled = true;
        e.SuppressKeyPress = true; // Windows "ding" sesini engeller
    }

    /// <summary>Fare tıklamasını dünya koordinatına çevirip iletir.</summary>
    private void Panel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _game.HandleMouseDown(_panel.ToWorld(e.Location));
    }
}
