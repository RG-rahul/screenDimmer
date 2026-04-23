using ScreenDimmer;

static class Program
{
    [STAThread]
    static void Main()
    {
        using Mutex mutex = new Mutex(true, "ScreenDimmer_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ScreenDimmerApplicationContext());
    }
}