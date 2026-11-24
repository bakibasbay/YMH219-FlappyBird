namespace FlappyBird;

static class Program
{
    /// <summary>
    /// Uygulamanın giriş noktası.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Yüksek DPI, görsel stiller gibi uygulama ayarlarını yükler.
        ApplicationConfiguration.Initialize();

        // Ana formu açar; form kapanınca uygulama sona erer.
        Application.Run(new Form1());
    }
}
