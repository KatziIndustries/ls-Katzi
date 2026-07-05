using Color = System.Drawing.Color;
using System.Numerics;
using Smash.Graphics;
using SDL3;

public class ConfigScene : IScene
{
    private KeybindHandler _keybindHandler = new();

    public ConfigScene()
    {
        _keybindHandler.RegisterKeybind(SDL.Keycode.Comma, Action.ToggleConfig, true, false);
    }

    public bool Update(double deltaTime)
    {
        bool needsRedraw = false;


        Action? action = _keybindHandler.Update();

        if (action != null)
            PerformAction((Action)action);

        return needsRedraw;
    }

    public void Render(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Vector2 basePosition = new Vector2(App.PADDING);

        string headerText = "Default Applications";
        renderer.RenderText(font, App.BIG_POINT_SIZE, headerText, basePosition, Color.White);

        Vector2 headerTextSize = font.MeasureString(headerText, App.BIG_POINT_SIZE);

        basePosition.Y += headerTextSize.Y + App.PADDING;

        renderer.RenderLine(basePosition with { X = 0 }, basePosition with { X = App.WindowWidth}, App.ForegroundColor);

        basePosition.Y += App.PADDING;

        int index = 0;
        foreach (FileType fileType in Enum.GetValues<FileType>())
        {
            if (fileType == FileType.Unknown) 
                continue;

            Vector2 fileTypePosition = basePosition + new Vector2(0, index * App.ENTRY_SPACING);
            Vector2 defaultAppPosition = fileTypePosition + new Vector2(App.WindowWidth / 3, 0);

            FileTypeUtils.DefaultApplications.TryGetValue(fileType, out string? defaultApplication);

            if (defaultApplication == null)
                defaultApplication = "Undefined (defaulting to Built-In)";

            renderer.RenderText(font, App.POINT_SIZE, Enum.GetName(fileType)!, fileTypePosition, Color.White);
            renderer.RenderText(font, App.POINT_SIZE, defaultApplication, defaultAppPosition, Color.White);

            index++; 
        }
    }

    public void PerformAction(Action action)
    {
        if (action == Action.ToggleConfig)
        {
            App.SetScene<FileManagerScene>();
        }
    }
}