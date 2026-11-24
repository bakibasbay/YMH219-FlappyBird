using System.Drawing.Drawing2D;
using FlappyBird.Entities;
using FlappyBird.Rendering;
using FlappyBird.Services;

namespace FlappyBird.Core;

/// <summary>
/// Oyunun beyni: oyun döngüsünü (Update/Draw) ve durum makinesini yönetir.
/// Durumlar: Menu → Ready → Playing ⇄ Paused → GameOver → (Ready | Menu)
/// Form yalnızca girdileri buraya iletir ve her karede Update + Draw çağırır.
/// </summary>
public class GameManager : IDisposable
{
    // Kuşun oyundaki sabit yatay konumu ve başlangıç yüksekliği
    public const float BirdStartX = 110f;
    public const float BirdStartY = 320f;

    /// <summary>Ekranda aynı anda dolaşan boru çifti sayısı (föy: en az 2).</summary>
    public const int PipePairCount = 3;

    // Boşluğun ekran kenarlarına ve zemine olan en küçük mesafesi
    private const float GapMargin = 60f;

    /// <summary>Oyunun şu anki durumu.</summary>
    public GameState State { get; private set; } = GameState.Menu;

    /// <summary>Aktif fizik/hız ayarları.</summary>
    public GameSettings Settings { get; private set; } = new();

    /// <summary>Oyuncunun kuşu.</summary>
    public Bird Bird { get; }

    /// <summary>Sağdan sola kayan boru çiftleri.</summary>
    public List<PipePair> Pipes { get; } = new();

    /// <summary>Boruların (ve zeminin) şu anki kayma hızı.</summary>
    public float PipeSpeed { get; private set; }

    /// <summary>Skor, seviye ve en yüksek skor.</summary>
    public ScoreManager Score { get; } = new();

    /// <summary>Oyun başladığından beri geçen toplam süre (animasyonlar için).</summary>
    public float TotalTime { get; private set; }

    /// <summary>Menüden "Çıkış" seçildiğinde form kapanması için tetiklenir.</summary>
    public event EventHandler? ExitRequested;

    private readonly Random _random = new();
    private readonly MenuScreen _menu = new();
    private readonly Hud _hud = new();
    private readonly Background _background = new();
    private readonly ParticleSystem _particles = new();
    private readonly ScreenEffects _effects = new();
    private readonly GameOverPanel _gameOverPanel = new();

    // Çarpışmadan sonra geçen süre (panel kuş düştükten sonra açılır)
    private float _gameOverTimer;

    private readonly HighScoreService _highScores;
    private readonly SoundService _sound;

    /// <summary>Ses açık mı? (M tuşu ile değişir)</summary>
    public bool IsSoundEnabled => _sound.IsEnabled;

    /// <param name="highScores">En yüksek skor servisi (testlerde geçici dosya verilebilir).</param>
    /// <param name="sound">Ses servisi (testlerde sessiz servis verilebilir).</param>
    public GameManager(HighScoreService? highScores = null, SoundService? sound = null)
    {
        _highScores = highScores ?? new HighScoreService();
        _sound = sound ?? new SoundService();
        Bird = new Bird(BirdStartX, BirdStartY);

        for (int i = 0; i < PipePairCount; i++)
            Pipes.Add(new PipePair(0, GameSettings.GroundY / 2f, Settings.PipeGap));

        _menu.StartRequested += StartNewGame;
        _menu.ExitRequested += () => ExitRequested?.Invoke(this, EventArgs.Empty);
        _menu.DifficultySelected += SetDifficulty;
        _gameOverPanel.RestartRequested += StartNewGame;
        _gameOverPanel.MenuRequested += GoToMenu;

        SetDifficulty(Difficulty.Normal);
        GoToMenu();
    }

    /// <summary>
    /// Zorluğu değiştirir: yeni fizik ayarları bir sonraki oyunda kullanılır.
    /// </summary>
    public void SetDifficulty(Difficulty difficulty)
    {
        Settings = GameSettings.Create(difficulty);
        _menu.SetSelectedDifficulty(difficulty);
        _menu.DifficultyDescription = Settings.Description;
        _menu.BestScore = _highScores.GetHighScore(difficulty);
    }

    /// <summary>Menüde sol/sağ ok ile zorluklar arasında dolaşır.</summary>
    private void CycleDifficulty(int direction)
    {
        int count = Enum.GetValues<Difficulty>().Length;
        int next = ((int)Settings.Difficulty + direction + count) % count;
        SetDifficulty((Difficulty)next);
    }

    // ================= Durum geçişleri =================

    /// <summary>Ana menüye döner; kuş menünün ortasında süzülür.</summary>
    public void GoToMenu()
    {
        State = GameState.Menu;
        // Kuş, başlığın alt kenarı (~150) ile menü kartının üstü (300) arasında ortalanır
        Bird.ResetTo(GameSettings.WorldWidth / 2f - Bird.DefaultWidth / 2f, 225f - Bird.DefaultHeight / 2f);
        PipeSpeed = Settings.BasePipeSpeed;
        _particles.Clear();
        _background.SetLevel(1);
        _gameOverPanel.Hide();
    }

    /// <summary>
    /// Yeni oyun: kuş, borular ve skor sıfırlanır, Hazır ekranına geçilir.
    /// </summary>
    public void StartNewGame()
    {
        ApplySettingsToBird();
        Bird.ResetTo(BirdStartX, BirdStartY);
        ResetPipes();
        Score.Reset(_highScores.GetHighScore(Settings.Difficulty));
        _particles.Clear();
        _effects.Reset();
        _hud.Reset();
        _gameOverPanel.Hide();
        _background.SetLevel(1); // gökyüzü gündüze döner
        State = GameState.Ready;
    }

    /// <summary>Oynanıyorsa duraklatır, duraklatılmışsa devam ettirir.</summary>
    public void TogglePause()
    {
        if (State == GameState.Playing)
            State = GameState.Paused;
        else if (State == GameState.Paused)
            State = GameState.Playing;
    }

    /// <summary>Oyunu durdurur ve Game Over durumuna geçer.</summary>
    private void GameOver()
    {
        State = GameState.GameOver;
        _gameOverTimer = 0;
        Bird.Die();

        // Rekor kırıldıysa dosyaya kaydet (hata olursa oyun devam eder)
        _highScores.TrySetHighScore(Settings.Difficulty, Score.Score);

        // Çarpışma efektleri: patlama, ekran sarsıntısı ve beyaz flaş
        _particles.EmitExplosion(Bird.CenterX, Bird.CenterY, Bird.FeatherColor);
        _effects.Shake(0.4f, 10f);
        _effects.Flash();
        _sound.Play(SoundEffect.Hit);
    }

    private void ApplySettingsToBird()
    {
        Bird.Gravity = Settings.Gravity;
        Bird.JumpVelocity = Settings.JumpVelocity;
        Bird.MaxFallSpeed = Settings.MaxFallSpeed;
    }

    // ================= Borular =================

    /// <summary>Boruları ekranın sağına, eşit aralıklarla diz.</summary>
    private void ResetPipes()
    {
        PipeSpeed = Settings.BasePipeSpeed;
        float x = GameSettings.WorldWidth + 80f;
        float gap = GameSettings.GroundY / 2f;
        foreach (PipePair pair in Pipes)
        {
            gap = RandomGapCenter(gap);
            pair.Reposition(x, gap, Settings.PipeGap);
            x += Settings.PipeSpacing;
        }
    }

    /// <summary>
    /// Rastgele bir boşluk yüksekliği üretir. Bir önceki boşluktan
    /// çok uzak olmaması sağlanır, böylece geçilemez dizilim oluşmaz.
    /// </summary>
    private float RandomGapCenter(float previous)
    {
        float half = Settings.PipeGap / 2f;
        float min = GapMargin + half;
        float max = GameSettings.GroundY - GapMargin - half;
        float low = Math.Max(min, previous - Settings.MaxGapShift);
        float high = Math.Min(max, previous + Settings.MaxGapShift);
        return low + (float)_random.NextDouble() * (high - low);
    }

    /// <summary>
    /// Ekrandan çıkan boru çiftini en sağdaki çiftin arkasına taşır
    /// ve yeni rastgele boşluk verir (föy: yeniden konumlandırma).
    /// </summary>
    private void RecyclePipe(PipePair pair)
    {
        PipePair last = Pipes.MaxBy(p => p.X)!;
        pair.Reposition(last.X + Settings.PipeSpacing, RandomGapCenter(last.GapCenterY), Settings.PipeGap);
    }

    // ================= Girdi =================

    /// <summary>Klavye tuşlarını duruma göre işler (Form KeyDown olayından çağrılır).</summary>
    public void HandleKeyDown(Keys key)
    {
        // M her ekranda sesi açar/kapatır
        if (key == Keys.M)
        {
            bool on = _sound.Toggle();
            _hud.ShowToast(on ? "Ses açık" : "Ses kapalı");
            return;
        }

        switch (State)
        {
            case GameState.Menu:
                if (key == Keys.Enter || key == Keys.Space) StartNewGame();
                else if (key == Keys.Escape) ExitRequested?.Invoke(this, EventArgs.Empty);
                else if (key == Keys.Left) CycleDifficulty(-1);
                else if (key == Keys.Right) CycleDifficulty(+1);
                else if (key == Keys.D1) SetDifficulty(Difficulty.Kolay);
                else if (key == Keys.D2) SetDifficulty(Difficulty.Normal);
                else if (key == Keys.D3) SetDifficulty(Difficulty.Zor);
                break;

            case GameState.Ready:
            case GameState.Playing:
                if (key == Keys.Space || key == Keys.Up) Flap();
                else if (key == Keys.P) TogglePause();
                else if (key == Keys.R) StartNewGame();
                else if (key == Keys.Escape) GoToMenu();
                break;

            case GameState.Paused:
                if (key == Keys.P) TogglePause();
                else if (key == Keys.R) StartNewGame();
                else if (key == Keys.Escape) GoToMenu();
                break;

            case GameState.GameOver:
                // R her zaman; Enter/Space yalnızca panel hazır olunca (yanlışlıkla basmayı önler)
                if (key == Keys.R) StartNewGame();
                else if ((key == Keys.Enter || key == Keys.Space) && _gameOverPanel.AcceptsInput) StartNewGame();
                else if (key == Keys.Escape) GoToMenu();
                break;
        }
    }

    /// <summary>Fare tıklamasını işler (dünya koordinatında).</summary>
    public void HandleMouseDown(PointF worldPoint)
    {
        switch (State)
        {
            case GameState.Menu:
                _menu.HandleClick(worldPoint);
                break;
            case GameState.Ready:
            case GameState.Playing:
                Flap();
                break;
            case GameState.GameOver:
                _gameOverPanel.HandleClick(worldPoint);
                break;
        }
    }

    /// <summary>Fare hareketi: butonların üzerine gelme efekti.</summary>
    public void HandleMouseMove(PointF worldPoint)
    {
        if (State == GameState.Menu)
            _menu.HandleMouseMove(worldPoint);
        else if (State == GameState.GameOver)
            _gameOverPanel.HandleMouseMove(worldPoint);
    }

    /// <summary>Zıplama eylemi: Hazır ekranındaysa oyunu başlatır.</summary>
    private void Flap()
    {
        if (State == GameState.Ready)
            State = GameState.Playing;

        if (State == GameState.Playing)
        {
            Bird.Jump();
            _sound.Play(SoundEffect.Jump);
            _particles.EmitJumpDust(Bird.X + 6, Bird.CenterY + 6, Bird.FeatherColor);
        }
    }

    // ================= Döngü =================

    /// <summary>
    /// Bir kare ilerletir. dt: önceki kareden bu yana geçen süre (saniye).
    /// Hareketler dt ile çarpıldığı için FPS düşse de hız sabit kalır.
    /// </summary>
    public void Update(float dt)
    {
        TotalTime += dt;
        _hud.Update(dt);

        // Arka plan: duraklatmada donar, oyun bitince zemin durur (bulutlar süzülmeye devam eder)
        if (State != GameState.Paused)
        {
            float scroll = State == GameState.GameOver ? 0f : PipeSpeed;
            _background.Update(dt, scroll);
            _particles.Update(dt);
            _effects.Update(dt);
        }

        switch (State)
        {
            case GameState.Menu:
                _menu.Update(dt);
                Bird.Update(dt); // menüde süzülür
                break;

            case GameState.Ready:
                Bird.Update(dt); // ilk zıplamayı beklerken süzülür
                break;

            case GameState.Playing:
                Bird.Update(dt);
                UpdatePipes(dt);
                CheckScore();
                CheckCollisions();
                break;

            case GameState.GameOver:
                // Ölen kuş zemine kadar düşmeye devam eder; yere çakılınca toz kalkar
                bool wasInAir = !Bird.IsOnGround;
                Bird.Update(dt);
                if (wasInAir && Bird.IsOnGround)
                {
                    _particles.EmitGroundDust(Bird.CenterX, Bird.Bottom);
                    _effects.Shake(0.15f, 4f);
                }

                // Kuş yere indikten kısa süre sonra (en geç 1 sn) sonuç paneli açılır
                _gameOverTimer += dt;
                if (!_gameOverPanel.IsVisible && (_gameOverTimer > 1f || (Bird.IsOnGround && _gameOverTimer > 0.45f)))
                    _gameOverPanel.Show(Score.Score, Score.HighScore, Score.IsNewRecord, Score.Medal);
                _gameOverPanel.Update(dt);
                break;

            // Paused: hiçbir şey ilerlemez
        }
    }

    /// <summary>Boruları kaydırır, ekrandan çıkanları yeniden konumlandırır.</summary>
    private void UpdatePipes(float dt)
    {
        foreach (PipePair pair in Pipes)
        {
            pair.Speed = PipeSpeed;
            pair.Update(dt);
            if (pair.IsOffScreen)
                RecyclePipe(pair);
        }
    }

    /// <summary>Kuş bir boru çiftinin ortasını geçtiyse +1 puan.</summary>
    private void CheckScore()
    {
        foreach (PipePair pair in Pipes)
        {
            if (!pair.Scored && pair.CenterX < Bird.CenterX)
            {
                pair.Scored = true;
                bool levelUp = Score.AddPoint();
                _particles.EmitScoreSparkle(Bird.CenterX + 10, Bird.Y - 4);
                _hud.TriggerScorePop();
                if (levelUp)
                    OnLevelUp();
                else
                    _sound.Play(SoundEffect.Score);
            }
        }
    }

    /// <summary>
    /// Seviye atlandı: borular hızlanır, gökyüzü bir sonraki gün saatine geçer,
    /// ekranda "SEVİYE n!" yazısı ve konfeti görünür.
    /// </summary>
    private void OnLevelUp()
    {
        PipeSpeed = Settings.GetPipeSpeed(Score.Level);
        _background.SetLevel(Score.Level);
        _hud.ShowLevelUp(Score.Level);
        _particles.EmitConfetti();
        _sound.Play(SoundEffect.LevelUp);
    }

    /// <summary>
    /// Çarpışma kontrolü: kuşun küçültülmüş kutusu (Hitbox) boru dikdörtgenleriyle
    /// kesişiyorsa ya da kuş zemine/tavana değdiyse oyun biter.
    /// </summary>
    private void CheckCollisions()
    {
        RectangleF hitbox = Bird.Hitbox;
        bool hitPipe = Pipes.Any(p => p.Collides(hitbox));
        bool hitGround = Bird.IsOnGround;
        bool hitCeiling = Bird.Y <= 0;

        if (hitPipe || hitGround || hitCeiling)
            GameOver();
    }

    // ================= Çizim =================

    /// <summary>Tüm sahneyi verilen Graphics nesnesine çizer.</summary>
    public void Draw(Graphics g)
    {
        // Sarsıntı: dünya katmanları rastgele küçük bir kaymayla çizilir (arayüz sabit kalır)
        GraphicsState world = g.Save();
        PointF shake = _effects.GetShakeOffset();
        g.TranslateTransform(shake.X, shake.Y);

        // Katman sırası: gökyüzü/parallax → borular → zemin → gece tonu → kuş → parçacıklar → arayüz
        _background.DrawSky(g);

        if (State != GameState.Menu)
        {
            foreach (PipePair pair in Pipes)
                pair.Draw(g);
        }

        _background.DrawGround(g);
        _background.DrawNightTint(g);

        Bird.Draw(g);
        _particles.Draw(g);
        g.Restore(world);

        DrawOverlay(g);
        _hud.DrawToast(g);
        _effects.DrawFlash(g);
    }

    /// <summary>Ses kaynaklarını serbest bırakır (form kapanırken çağrılır).</summary>
    public void Dispose() => _sound.Dispose();

    /// <summary>Duruma göre menü, HUD ve bilgi katmanlarını çizer.</summary>
    private void DrawOverlay(Graphics g)
    {
        if (State == GameState.Menu)
        {
            _menu.Draw(g);
            return;
        }

        // Oyun ekranlarında ortak HUD: seviye, en iyi skor ve büyük skor
        string difficulty = Settings.Difficulty.ToString().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        _hud.DrawTopBar(g, Score.Level, Score.HighScore, difficulty, _sound.IsEnabled);
        if (!_gameOverPanel.IsVisible)
            _hud.DrawScore(g, Score.Score); // panel açıkken skor panelde gösterilir

        switch (State)
        {
            case GameState.Ready:
                _hud.DrawReady(g);
                break;

            case GameState.Playing:
                _hud.DrawLevelBanner(g);
                break;

            case GameState.Paused:
                _hud.DrawPaused(g);
                break;

            case GameState.GameOver:
                _gameOverPanel.Draw(g);
                break;
        }
    }
}
