using Color = System.Drawing.Color;
using System.Numerics;
using Smash.Graphics;
using SDL3;
using Smash.Input;

public class ConfigScene : IScene
{
    public static string DefaultTerminalPath => Path.Combine(App.ConfigDirPath, "DefaultTerminal.katzi");
    public static string DefaultTerminal { get; private set; } = null!;

    private KeybindHandler _keybindHandler = new();

    private float _scroll;
    private float _preferredScroll;

    private float _maxScroll = 0;

    public ConfigScene()
    {
        if (!File.Exists(DefaultTerminalPath))
        {
            using (var fs = File.Create(DefaultTerminalPath)) { }

            if (OperatingSystem.IsLinux())
                File.WriteAllText(DefaultTerminalPath, "kitty");
            else if (OperatingSystem.IsWindows())
                File.WriteAllText(DefaultTerminalPath, "cmd.exe");
        }
        
        DefaultTerminal = File.ReadAllText(DefaultTerminalPath).Trim();

        _keybindHandler.RegisterKeybind(SDL.Keycode.Comma, Action.ToggleConfig, true, false);
    }

    public bool Update(double deltaTime)
    {
        bool needsRedraw = false;

        Action? action = _keybindHandler.Update();

        if (action != null)
            PerformAction((Action)action);

        if (InputHandler.ScrollWheelDelta != 0)
        {
            _preferredScroll -= InputHandler.ScrollWheelDelta * App.SCROLL_SPEED;
            _preferredScroll = Math.Clamp(_preferredScroll, 0, _maxScroll);
        }

        if (_scroll != _preferredScroll)
        {
            _scroll = App.Lerp(_scroll, _preferredScroll, deltaTime, App.SCROLL_ANIM_SPEED, 1);
            needsRedraw = true;
        }
        
        return needsRedraw;
    }

    public void Render(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Vector2 basePosition = new Vector2(App.PADDING);
        basePosition.Y -= _scroll;

        //Default applications
        basePosition = RenderHeader(renderer, basePosition, "Default Applications", font);
        
        FileType[] fileTypes = Enum.GetValues<FileType>();
        for (int i = 0; i < fileTypes.Length; i++)
        {
            FileType fileType = fileTypes[i];

            Vector2 fileTypePosition = basePosition + new Vector2(0, i * App.ENTRY_SPACING);
            Vector2 defaultAppPosition = fileTypePosition + new Vector2(App.WindowWidth / 3, 0);

            FileTypeUtils.DefaultApplications.TryGetValue(fileType, out string? defaultApplication);

            if (defaultApplication == null)
                defaultApplication = "Undefined (defaulting to Built-In probably)";

            renderer.RenderText(font, App.POINT_SIZE, Enum.GetName(fileType)!, fileTypePosition, Color.White);
            renderer.RenderText(font, App.POINT_SIZE, defaultApplication, defaultAppPosition, Color.White);
        }

        basePosition.Y += fileTypes.Length * App.ENTRY_SPACING + App.PADDING * 2;

        //Default Terminal
        basePosition = RenderHeader(renderer, basePosition, "Default Terminal", font);

        renderer.RenderText(font, App.POINT_SIZE, DefaultTerminal, basePosition, Color.White);

        basePosition.Y += App.POINT_SIZE + App.PADDING * 2;
        
        // File extensions
        basePosition = RenderHeader(renderer, basePosition, "File Extensions", font);
    
        float yOffset = 0;
        for (int i = 0; i < FileTypeUtils.ExtensionsFromFileType.Count; i++)
        {
            var kvp = FileTypeUtils.ExtensionsFromFileType.ElementAt(i);
            FileType fileType = kvp.Key;
            List<string> extensions = kvp.Value;

            Vector2 fileTypePosition = Vector2.Round(basePosition + new Vector2(0, i * App.ENTRY_SPACING + yOffset));
            renderer.RenderText(font, App.MEDIUM_POINT_SIZE, Enum.GetName(fileType)!, fileTypePosition, Color.White);

            for (int j = 0; j < extensions.Count; j++)
            {
                Vector2 extensionPosition = fileTypePosition + Vector2.Round(new Vector2(0, (j + 1) * App.ENTRY_SPACING));
                renderer.RenderText(font, App.POINT_SIZE, $"""- "{extensions[j]}" """, extensionPosition, Color.White);

                yOffset += App.ENTRY_SPACING;
            }

            yOffset += App.PADDING;
        }

        basePosition.Y += yOffset;
        basePosition.Y -= App.WindowHeight / 2;

        if (_maxScroll == 0)
            _maxScroll = basePosition.Y;
    }

    private Vector2 RenderHeader(Renderer renderer, Vector2 basePosition, string header, Font font)
    {
        renderer.RenderText(font, App.BIG_POINT_SIZE, header, basePosition, Color.White);

        Vector2 headerTextSize = font.MeasureString(header, App.BIG_POINT_SIZE);

        basePosition.Y += headerTextSize.Y + App.PADDING;

        renderer.RenderLine(basePosition with { X = 0 }, basePosition with { X = App.WindowWidth}, App.ForegroundColor);

        basePosition.Y += App.PADDING;

        return basePosition;
    }

    public void PerformAction(Action action)
    {
        if (action == Action.ToggleConfig)
        {
            App.SetScene<FileManagerScene>();
        }
    }
}