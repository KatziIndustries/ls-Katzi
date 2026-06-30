using System.Net.Sockets;
using System.Numerics;
using SDL3;
using Smash;
using Smash.Graphics;
using Smash.Input;
using Color = System.Drawing.Color;

public class App : Application 
{
    public static readonly Color BackgroundColor = Color.FromArgb(25, 25, 25);
    public Color PathColor => _selectedEntry == -1 ? Color.FromArgb(50, 50, 50) : Color.FromArgb(28, 28, 28);

    public const string FONT_NAME = "Rubik-Regular";
    public const int POINT_SIZE = 25;
    
    private const int ENTRY_SPACING = 45;
    private const int PADDING = 20;

    private const int SCROLL_SPEED = 30;
    private const int SCROLL_ANIM_SPEED = 40;

    private const int PATH_HEIGHT = 50;

    private Window _window;
    private Renderer _renderer;

    private bool needsRedraw = true;

    private string _currentPath;
    private string[] _systemEntries = [];

    private float _scroll;
    private float _preferredScroll;

    private Vector2 _lastWindowBounds;

    private bool _showHiddenFiles = false;

    private bool _pathPermissionDenied = false;

    private int _selectedEntry = -1;

    public App() 
    {
        CreateWindowAndRenderer("ls-Katzi", 800, 600, out _window, out _renderer);
        _window.SetWindowResizable(true);

        AssetManager.SetAssetRootDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
        AssetManager.LoadFont(FONT_NAME + ".ttf");

        _renderer.SetVSyncEnabled(true);

        _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar;
        RefreshSystemEntries();
    }

    public override void Update(double deltaTime) 
    {
        needsRedraw = false;

        if (InputHandler.ScrollWheelDelta != 0)
        {
            _preferredScroll += InputHandler.ScrollWheelDelta * SCROLL_SPEED;
            _preferredScroll = Math.Clamp(_preferredScroll, Math.Min(GetMinScroll(), 0), 0);
        }

        if (_scroll != _preferredScroll)
        {
            _scroll = MathHelper.Lerp(_scroll, _preferredScroll, SCROLL_ANIM_SPEED * (float)deltaTime);
            needsRedraw = true;

            if (Math.Abs(_scroll - _preferredScroll) < 1)
                _scroll = _preferredScroll;
        }
        
        if (_lastWindowBounds != _window.Bounds)
        {
            needsRedraw = true;
            _lastWindowBounds = _window.Bounds;
        }

        if (InputHandler.TextInput != null && InputHandler.TextInput != string.Empty && _selectedEntry == -1)
        {
            UpdatePath(_currentPath + InputHandler.TextInput);
            needsRedraw = true;

            RefreshSystemEntries();
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.Backspace) && _currentPath.Length > 0)
        {
            if (KeyRepetitionHandler.CanPress(SDL.Keycode.Backspace))
            {
                KeyRepetitionHandler.Press(SDL.Keycode.Backspace);

                if (InputHandler.IsKeyDown(SDL.Keycode.LCtrl))
                {
                    if (_currentPath[_currentPath.Length - 1] == Path.DirectorySeparatorChar)
                    {
                        UpdatePath(_currentPath = _currentPath.Remove(_currentPath.Length - 1, 1));
                    }

                    int index = _currentPath.LastIndexOf(Path.DirectorySeparatorChar);
                    UpdatePath(_currentPath.Substring(0, index + 1));
                }
                else
                {
                    UpdatePath(_currentPath.Remove(_currentPath.Length - 1, 1));
                }

                needsRedraw = true;
                RefreshSystemEntries();
            }
        }

        if ((InputHandler.IsKeyPressed(SDL.Keycode.Return) || InputHandler.IsKeyPressed(SDL.Keycode.Escape)) && _systemEntries.Length > 0 && _selectedEntry == -1)
        {
            _selectedEntry = 0;
            needsRedraw = true;
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.K))
        {
            if (InputHandler.IsKeyDown(SDL.Keycode.LCtrl))
            {
                _selectedEntry = -1;
                needsRedraw = true;
            }
            else if (KeyRepetitionHandler.CanPress(SDL.Keycode.K) && _selectedEntry > 0)
            {
                KeyRepetitionHandler.Press(SDL.Keycode.K);
                _selectedEntry--;
                needsRedraw = true;
            }
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.J) && KeyRepetitionHandler.CanPress(SDL.Keycode.J) && _selectedEntry < _systemEntries.Length - 1)
        {
            KeyRepetitionHandler.Press(SDL.Keycode.J);
            _selectedEntry++;
            needsRedraw = true;
        }

        if (InputHandler.IsKeyPressed(SDL.Keycode.E))
        {
            if (InputHandler.IsKeyDown(SDL.Keycode.LCtrl) && _selectedEntry == -1)
            {
                if (_systemEntries.Length > 0)
                {
                    if (Directory.Exists(_systemEntries[0]))
                    {
                        UpdatePath(_systemEntries[0] + Path.DirectorySeparatorChar);
                        RefreshSystemEntries();
                        ClampSelectedEntry();
                        needsRedraw = true;
                    }
                }
            }
            else if (_selectedEntry != -1)
            {
                if (Directory.Exists(_systemEntries[_selectedEntry]))
                {
                    UpdatePath(_systemEntries[_selectedEntry] + Path.DirectorySeparatorChar);
                    RefreshSystemEntries();
                    ClampSelectedEntry();
                    needsRedraw = true;
                }
            }
        }

        if (InputHandler.IsKeyPressed(SDL.Keycode.B))
        {
            if ((InputHandler.IsKeyDown(SDL.Keycode.LCtrl) && _selectedEntry == -1) || _selectedEntry != -1)
            {
                string parentDir = GetParentDirectory();
                UpdatePath(parentDir + (parentDir == "/" ? "" : Path.DirectorySeparatorChar));
                RefreshSystemEntries();
                ClampSelectedEntry();
                needsRedraw = true;
            }
        }

        KeyRepetitionHandler.Update(deltaTime);

        if (needsRedraw)
        {
            Render();
            needsRedraw = false;
        }
    }

    public override void Render() 
    {
        _renderer.Clear(BackgroundColor);

        Font font = AssetManager.Get<Font>(FONT_NAME);

        Rectangle pathRectangle = new(0, 0, _window.Width, PATH_HEIGHT);
        _renderer.RenderFilledRectangle(pathRectangle, PathColor);

        int lineColor = PathColor.R + 20; 
        _renderer.RenderLine(pathRectangle.Position + new Vector2(0, pathRectangle.Height), pathRectangle.Position + pathRectangle.Bounds, Color.FromArgb(lineColor, lineColor, lineColor));

        Vector2 pathTextPosition = new(PATH_HEIGHT / 2);
        _renderer.RenderText(font, POINT_SIZE, _currentPath, pathTextPosition - new Vector2(0, font.MeasureString(_currentPath, POINT_SIZE).Y / 2), Color.White);

        Vector2 entriesStartPosition = new Vector2(PADDING) + new Vector2(0, _scroll + PATH_HEIGHT);

        Rectangle clipRect = new Rectangle(0, PATH_HEIGHT, _window.Width, _window.Height - PATH_HEIGHT);
        SDL.SetRenderClipRect(_renderer.Handle, clipRect.ToSDLRect());
        
        if (!_pathPermissionDenied)
        {
            for (int i = 0; i < _systemEntries.Length; i++)
            {
                Vector2 position = entriesStartPosition + new Vector2(0, i * ENTRY_SPACING);

                if (i == _selectedEntry)
                {
                    Vector2 hitboxStartPos = position;
                    hitboxStartPos.X = 0;
                    hitboxStartPos.Y -= ENTRY_SPACING / 2 - font.MeasureString(_systemEntries[i], POINT_SIZE).Y / 2;

                    Rectangle rect = new Rectangle(hitboxStartPos, _window.Width, ENTRY_SPACING);
                    _renderer.RenderFilledRectangle(rect, Color.FromArgb(50, 50, 50));
                }

                bool isDirectory = Directory.Exists(_systemEntries[i]);
                _renderer.RenderText(font, POINT_SIZE, Path.GetFileName(_systemEntries[i]), position, isDirectory ? Color.RoyalBlue : Color.White);
            }
        }
        else
        {
            _renderer.RenderText(font, POINT_SIZE, "Can't access this directory (Permission denied)", entriesStartPosition, Color.Red);
        }

        SDL.SetRenderClipRect(_renderer.Handle, IntPtr.Zero);

        _renderer.RenderPresent();
    }

    private void UpdatePath(string newPath)
    {
        _currentPath = newPath;
        if (_currentPath == string.Empty)
        {
            string? root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            if (root == null) return;
            _currentPath = root;
        }
    }

    private bool RefreshSystemEntries()
    {
        string[] newEntries;

        string? path = Path.GetDirectoryName(_currentPath);
        if (_currentPath == "/") path = "/";

        string filter = Path.GetFileName(_currentPath);

        if (path == null || !Directory.Exists(path))
        {
            if (_systemEntries.Length > 0)
            {
                _systemEntries = [];
                _selectedEntry = -1;
                return true;
            }
            else return false;
        }

        try
        {
            if (_showHiddenFiles)
                newEntries = Directory.GetFileSystemEntries(path, "*" + filter + "*");
            else
                newEntries = Directory.GetFileSystemEntries(path, "*" + filter + "*").Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) != FileAttributes.Hidden).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            _systemEntries = [];
            _pathPermissionDenied = true;
            _selectedEntry = -1;
            return true; 
        }

        _pathPermissionDenied = false;

        if (newEntries == _systemEntries)
        {
            return false;
        }
        else
        {
            _systemEntries = newEntries;
            return true;
        }
    }

    private float GetMinScroll()
    {
        return -PADDING * 2 - (_systemEntries.Length * ENTRY_SPACING) + (_window.Height - PATH_HEIGHT);
    }

    private string GetParentDirectory()
    {
        string dirName = Path.GetDirectoryName(_currentPath) ?? "/";
        string parentDirName = (Directory.GetParent(dirName) ?? new DirectoryInfo("/")).FullName;
        return parentDirName;
    }

    private void ClampSelectedEntry()
    {
        if (_systemEntries.Length > 0) _selectedEntry = 0;
        else _selectedEntry = -1;
    }

    public override void End() 
    {
        _window.Dispose();
        _renderer.Dispose();
        AssetManager.Dispose();
    }
}