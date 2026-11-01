static class Program
{
    // WebView2 は、画面を動かすスレッドが STA である必要がある。[STAThread] が要る
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
