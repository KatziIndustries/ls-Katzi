using System.Diagnostics;
using SDL3;
using Smash;
using Smash.Input;

internal static class Program
{
    public const int UPS = 400;
    private static double _targetTime = 1000.0 / UPS;

    private static Stopwatch _stopWatch = new();

    private static void Main(string[] args)
    {
        SmashEngine.Init();

        App application = new App();
        application.Start();
        application.Render();

        InputHandler.StartPollingTextInput();

        while (!application.ApplicationShouldClose())
        {
            _stopWatch.Restart();

            SmashEngine.Update();
            application.Update(SmashEngine.DeltaTime);

            _stopWatch.Stop();

            double elapsed = _stopWatch.Elapsed.TotalMilliseconds;
            int sleepTime = (int)(_targetTime - elapsed);

            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
        }

        application.End();
        SmashEngine.Stop();
    }
}