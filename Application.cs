using System.Numerics;
using Smash;
using Smash.Graphics;
using Color = System.Drawing.Color;

public class App : Application 
{
    public static readonly Color BackgroundColor = Color.FromArgb(25, 25, 25);
    public static readonly Color ForegroundColor = Color.FromArgb(50, 50, 50);

    public const string FONT_NAME = "Rubik-Regular";
    public const int BIG_POINT_SIZE = 34;
    public const int POINT_SIZE = 25;
    
    public const int ENTRY_SPACING = 45;
    public const int PADDING = 20;

    public const int SCROLL_SPEED = 30;
    public const int SCROLL_ANIM_SPEED = 40;

    public const int PATH_HEIGHT = 50;

    public static float WindowWidth => _window.Width;
    public static float WindowHeight => _window.Height;
    public static Vector2 WindowBounds => _window.Bounds;

    public static string AssetDirPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
    public static string ConfigDirPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ls-Katzi");

    private static Window _window = null!;
    private static Renderer _renderer = null!;

    private Vector2 _lastWindowBounds;

    private static SceneManager _sceneManager = new();

    public App() 
    {
        CreateWindowAndRenderer("ls-Katzi", 800, 600, out _window, out _renderer);
        _window.SetWindowResizable(true);

        AssetManager.SetAssetRootDirectory(AssetDirPath);
        AssetManager.LoadFont(FONT_NAME + ".ttf");

        _renderer.SetVSyncEnabled(true);

        if (!Directory.Exists(ConfigDirPath))
            Directory.CreateDirectory(ConfigDirPath);

        FileTypeUtils.InitFileTypes();
    }

    public override void Update(double deltaTime) 
    {
        bool needsRedraw = false;

        if (_lastWindowBounds != _window.Bounds)
        {
            _lastWindowBounds = _window.Bounds;
            needsRedraw = true;
        }

        if (_sceneManager.Update(deltaTime))
            needsRedraw = true;

        KeyRepetitionHandler.Update(deltaTime);

        if (needsRedraw || _sceneManager.NeedsRedraw)
        {
            Render();
        }
    }

    public override void Render() 
    {
        _renderer.Clear(BackgroundColor);

        _sceneManager.Render(_renderer);

        _renderer.RenderPresent();
    }

    
    public static float Lerp(float start, float end, double deltaTime, float speed, float errorCorrection)
    {
        start = MathHelper.Lerp(start, end, speed * (float)deltaTime);

        if (Math.Abs(end - start) < errorCorrection)
            start = end;

        return start;
    }

    public static nint LoadTexture(string entry)
    {
        return SDL3.Image.LoadTexture(_renderer.Handle, entry);
    }

    public static void SetScene(Scenes scene)
    {
        _sceneManager.SetScene(scene);
    }

    public override void End() 
    {
        _window.Dispose();
        _renderer.Dispose();
        AssetManager.Dispose();
    }
}