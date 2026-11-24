namespace FlappyBird;

partial class Form1
{
    /// <summary>
    /// Tasarımcı tarafından kullanılan bileşen kapsayıcısı.
    /// </summary>
    private System.ComponentModel.IContainer components = null!;

    /// <summary>
    /// Kullanılan kaynakları serbest bırakır.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        //
        // Form1
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(480, 720);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        KeyPreview = true;
        MaximizeBox = false;
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Kanat Çırp — YMH219 Flappy Bird";
        ResumeLayout(false);
    }

    #endregion
}
