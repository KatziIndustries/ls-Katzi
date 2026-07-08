using Color = System.Drawing.Color;
using Smash.Graphics;
using SDL3;
using System.Numerics;
using Smash;

public class BookmarkHandler
{
    public const int SCROLL_SPEED = 30;

    public static float MaxWidth => App.WindowWidth / 4;
    
    private static Color _backgroundColor = Color.FromArgb(30, 30, 30);

    private const float MIN_OPENED_TIME = 0.02f;
    private const int BUTTON_HEIGHT = 75;

    private float _width = 0;

    public bool Active { get; private set; }
    public bool Closing { get; private set; }

    private float _openedTime;

    private List<Bookmark> _bookmarks = new();
    private int _selectedBookmark = -1;

    private KeybindHandler _keybindHandler = new();

    private List<IUIElement> _uiElements = new();
    private int _selectedButtonIndex = -1;

    private float _previousMaxWidth;

    public BookmarkHandler()
    {
        _previousMaxWidth = MaxWidth;

        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        _uiElements.Add(new TextElement(font, "Boomarks", App.BIG_POINT_SIZE));
        _uiElements.Add(new Separator());
        _uiElements.Add(new Button(MaxWidth, BUTTON_HEIGHT, App.PADDING, _backgroundColor, App.ForegroundColor, new TextElement(font, "+", App.POINT_SIZE)));
        _uiElements.Add(new Separator());

        Button? button = _uiElements[2] as Button;
        button!.Selected = true;
        _selectedButtonIndex = 2;

        AddBoomark(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar, "Home");

        _keybindHandler.RegisterKeybind(SDL.Keycode.D, Action.ToggleBookmarks, true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.J, Action.MoveDown, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.MoveUp, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.E, Action.Enter, false, true);
    }

    public (bool, string?) Update(double deltaTime)
    {
        bool needsRedraw = false;

        if (_previousMaxWidth != MaxWidth)
        {
            ResizeAllUIElements();
            _previousMaxWidth = MaxWidth;
        }

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
        Rectangle rectangle = new(0, 0, _width, App.WindowHeight);
        renderer.RenderFilledRectangle(rectangle, _backgroundColor);

        UIContext uiContext = new()
        {
            TotalWidth = _width,
            TotalHeight = App.WindowHeight
        };

        Vector2 basePosition = new Vector2(_width - MaxWidth, 0);
        foreach (IUIElement uiElement in _uiElements)
        {
            basePosition.Y += uiElement.Render(renderer, basePosition, uiContext);
            basePosition.Y += App.PADDING / 2;
        }


        //Font font = AssetManager.Get<Font>(App.FONT_NAME);


        //Vector2 basePosition = new Vector2(App.PADDING - (MaxWidth - _width));
        //renderer.RenderText(font, App.POINT_SIZE, "Bookmarks", basePosition, Color.White);

        //basePosition.Y += App.POINT_SIZE + App.PADDING;

        //renderer.RenderLine(basePosition with { X = 0}, basePosition with { X = _width }, App.ForegroundColor);

        //basePosition.Y += App.PADDING;

        //Vector2 bookmarkBasePosition = basePosition;
        //for (int i = 0; i < _bookmarks.Count; i++)
        //{
        //    Bookmark bookmark = _bookmarks[i];
        //    Vector2 position = bookmarkBasePosition + new Vector2(0, i * App.ENTRY_SPACING);

        //    if (i == _selectedBookmark)
        //    {
        //        Vector2 textSize = font.MeasureString(bookmark.Name, App.POINT_SIZE);
        //        Rectangle selectionRectangle = new(position - new Vector2(App.PADDING / 2), textSize with { X = _width - App.PADDING * 2 } + new Vector2(App.PADDING));
        //        renderer.RenderFilledRectangle(selectionRectangle, App.ForegroundColor);
        //    }

        //    renderer.RenderText(font, App.POINT_SIZE, bookmark.Name, position, Color.White);
        //}
    }

    public string? PerformAction(Action action)
    {
        switch (action)
        {
            case Action.ToggleBookmarks:
                if (_openedTime > MIN_OPENED_TIME)
                    Close();
                break;

            case Action.MoveDown:  
                MoveDown();
                break;

            case Action.MoveUp:
                MoveUp();
                break;

            case Action.Enter:
                int index = HandleEnter(_selectedBookmark);
                if (index != -1)
                {
                    return _bookmarks[_selectedBookmark].Path;
                }

                break;
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
        _selectedBookmark = -1;
        SelectBookmarkButton(_selectedBookmark);
    }

    private void ResizeAllUIElements()
    {
        foreach (IUIElement uiElement in _uiElements)
        {
            if (uiElement is Button button)
            {
                button.Width = MaxWidth;
            }
        }
    }

    private int HandleEnter(int index)
    {
        if (index == -1)
        {
            AddBoomark(FileManagerScene.CurrentPath, "New bookmark");
            return -1;
        }

        return index;
    }

    private void AddBoomark(string path, string name)
    {
        _bookmarks.Add(new Bookmark()
        {
            Path = path,
            Name = name
        });

        Font font = AssetManager.Get<Font>(App.FONT_NAME);
        _uiElements.Add(new Button(MaxWidth, BUTTON_HEIGHT, App.PADDING, _backgroundColor, App.ForegroundColor, new TextElement(font, name, App.POINT_SIZE)));
    }

    private void MoveUp()
    {
        if (_selectedBookmark > -1)
        {
            _selectedBookmark--;
            SelectBookmarkButton(_selectedBookmark);
        }
    }

    private void MoveDown()
    {
        if (_selectedBookmark < _bookmarks.Count - 1)
        {
            _selectedBookmark++;
            SelectBookmarkButton(_selectedBookmark);
        }
    }

    private void SelectBookmarkButton(int index)
    {
        Button? previousSelectedButton = _uiElements[_selectedButtonIndex] as Button;
        previousSelectedButton!.Selected = false;

        if (index == -1)
        {
            Button? button = _uiElements[2] as Button;
            button!.Selected = true;

            _selectedButtonIndex = 2;
        }
        else
        {
            Button? button = _uiElements[4 + index] as Button;
            button!.Selected = true;

            _selectedButtonIndex = 4 + index;
        }
    }
}