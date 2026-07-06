using Color = System.Drawing.Color;
using Smash.Graphics;
using Smash;
using SDL3;
using System.Numerics;

public class BookmarkHandler
{
    public const int SCROLL_SPEED = 30;

    public static float MaxWidth => App.WindowWidth / 4;
    
    private static Color _backgroundColor = Color.FromArgb(30, 30, 30);

    private const float MIN_OPENED_TIME = 0.01f;

    private float _width = 0;

    public bool Active { get; private set; }
    public bool Closing { get; private set; }

    private float _openedTime;

    private List<Bookmark> _bookmarks = new();
    private int _selectedBookmark = 0;

    private KeybindHandler _keybindHandler = new();

    public BookmarkHandler()
    {
        for (int i = 0; i < 10; i++)
        {
            _bookmarks.Add(new Bookmark()
            {
                Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar,
                Name = "Home"
            });
        }

        _keybindHandler.RegisterKeybind(SDL.Keycode.D, Action.ToggleBookmarks, true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.J, Action.MoveDown, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.MoveUp, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.E, Action.Enter, false, true);
    }

    public (bool, string?) Update(double deltaTime)
    {
        bool needsRedraw = false;

        if (Closing)
        {
            _width = App.Lerp(_width, 0, deltaTime, SCROLL_SPEED, 1);

            if (_width == 0)
            {
                Active = false;
                Closing = false;
            }
            else
            {
                needsRedraw = true;
            }
        }
        else
        {
            _width = App.Lerp(_width, MaxWidth, deltaTime, SCROLL_SPEED, 1);
            _openedTime += (float)deltaTime;

            if (_width != MaxWidth)
                needsRedraw = true;
        }

        string? path = null;

        Action? action = _keybindHandler.Update();
        if (action != null)
            path = PerformAction((Action)action);

        return (needsRedraw, path);
    }

    public void Render(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Rectangle rectangle = new(0, 0, _width, App.WindowHeight);
        renderer.RenderFilledRectangle(rectangle, _backgroundColor);

        Vector2 basePosition = new Vector2(App.PADDING - (MaxWidth - _width));
        renderer.RenderText(font, App.POINT_SIZE, "Bookmarks", basePosition, Color.White);

        basePosition.Y += App.POINT_SIZE + App.PADDING;

        renderer.RenderLine(basePosition with { X = 0}, basePosition with { X = _width }, App.ForegroundColor);

        basePosition.Y += App.PADDING;

        Vector2 bookmarkBasePosition = basePosition;
        for (int i = 0; i < _bookmarks.Count; i++)
        {
            Bookmark bookmark = _bookmarks[i];
            Vector2 position = bookmarkBasePosition + new Vector2(0, i * App.ENTRY_SPACING);

            if (i == _selectedBookmark)
            {
                Vector2 textSize = font.MeasureString(bookmark.Name, App.POINT_SIZE);
                Rectangle selectionRectangle = new(position - new Vector2(App.PADDING / 2), textSize with { X = _width - App.PADDING * 2 } + new Vector2(App.PADDING));
                renderer.RenderFilledRectangle(selectionRectangle, App.ForegroundColor);
            }

            renderer.RenderText(font, App.POINT_SIZE, bookmark.Name, position, Color.White);
        }
    }

    public string? PerformAction(Action action)
    {
        switch (action)
        {
            case Action.ToggleBookmarks:
                if (_openedTime > MIN_OPENED_TIME)
                {
                    Close();
                }
                break;

            case Action.MoveDown:  
                if (_selectedBookmark < _bookmarks.Count - 1)
                    _selectedBookmark++;
                break;

            case Action.MoveUp:
                if (_selectedBookmark > 0)
                    _selectedBookmark--;
                break;

            case Action.Enter:
                return _bookmarks[_selectedBookmark].Path;
        }

        return null;
    }

    public void Toggle()
    {
        if (Closing)
        {
            Open();
            return;
        }
    
        if (Active && !Closing)
        {
            Close();
            return;
        }
        
        if (!Active)
        {
            Open();
            return;
        }
    }

    public void Close()
    {
        Active = false;
        Closing = true;
    }

    public void Open()
    {
        Active = true;
        Closing = false;
        _openedTime = 0;
        _selectedBookmark = 0;
    }
}