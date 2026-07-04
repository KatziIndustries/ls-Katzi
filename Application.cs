using System.Diagnostics;
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

    private bool needsRedraw = true;

    private string _currentPath;
    private string[] _systemEntries = [];

    private float _scroll;
    private float _preferredScroll;

    private Vector2 _lastWindowBounds;

    private bool _showHiddenFiles = false;

    private bool _pathPermissionDenied = false;

    private int _selectedEntry = -1;

    private KeybindHandler _keybindHandler = new();
    private SceneRenderer _sceneRenderer;
    private ImageHandler _imageHandler = new();

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

        _keybindHandler.RegisterKeybind(SDL.Keycode.E, Action.EnterDirectory, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.E, Action.ForceEnterDirectory, true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.B, Action.EnterParentDirectory, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.B, Action.ForceEnterParentDirectory, true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Tab, Action.ForceEnterDirectory, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Escape, Action.LeaveSearchBar, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.EnterSearchBar, true, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.MoveUp, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.J, Action.MoveDown, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.Backspace, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.CtrlBackspace, true, true);

        Font font = AssetManager.Get<Font>(FONT_NAME);
        _sceneRenderer = new(font);

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
            _scroll = Lerp(_scroll, _preferredScroll, deltaTime, SCROLL_ANIM_SPEED, 1);
            needsRedraw = true;
        }

        if (_lastWindowBounds != _window.Bounds)
        {
            _lastWindowBounds = _window.Bounds;
            needsRedraw = true;
        }

        if (InputHandler.TextInput != null && InputHandler.TextInput != string.Empty && _selectedEntry == -1)
        {
            UpdatePath(_currentPath + InputHandler.TextInput);
            needsRedraw = true;

            RefreshSystemEntries();
        }

        Action? action = _keybindHandler.Update();
        if (action != null)
        {
            PerformAction((Action)action);
            needsRedraw = true;
        }

        if (_imageHandler.Update(deltaTime))
            needsRedraw = true;

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

        AppContext context = new()
        {
            CurrentPath = _currentPath,
            PathPermissionDenied = _pathPermissionDenied,
            Scroll = _scroll,
            SelectedEntry = _selectedEntry,
            SystemEntries = _systemEntries,
            Image = _imageHandler.Image
        };

        bool deleteImageTexture = _sceneRenderer.Render(_renderer, context);

        if (deleteImageTexture)
        {
            _imageHandler.DisposeImage();
        }

        _renderer.RenderPresent();
    }

    private void UpdatePath(string newPath)
    {
        string oldPath = _currentPath;
        _currentPath = newPath;

        if (_currentPath == string.Empty)
        {
            string? root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            if (root == null) return;
            _currentPath = root;
        }

        if (oldPath != _currentPath)
        {
            RefreshSystemEntries();
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
                _scroll = 0;
                _preferredScroll = 0;
                _systemEntries = [];
                _selectedEntry = -1;
                return true;
            }
            else return false;
        }

        try
        {
            if (_showHiddenFiles)
                newEntries = Directory.GetFileSystemEntries(path, "*" + filter + "*").OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
            else
                newEntries = Directory.GetFileSystemEntries(path, "*" + filter + "*").Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) != FileAttributes.Hidden).OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            _scroll = 0;
            _preferredScroll = 0;
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
            _scroll = 0;
            _preferredScroll = 0;
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
        if (File.Exists(_currentPath))
            return Path.GetDirectoryName(_currentPath)!;

        string dirName = Path.GetDirectoryName(_currentPath) ?? "/";
        return (Directory.GetParent(dirName) ?? new DirectoryInfo("/")).FullName;
    }

    private void ClampSelectedEntry()
    {
        if (_systemEntries.Length > 0) 
            _selectedEntry = 0;
        else 
            _selectedEntry = -1;
    }

    private void PerformAction(Action action)
    {
        if (action == Action.EnterDirectory)
        {
            if (_selectedEntry != -1 && _selectedEntry < _systemEntries.Length)
            {
                OpenFile(_selectedEntry);
            }

            return;
        }

        if (action == Action.ForceEnterDirectory)
        {
            if (_systemEntries.Length > 0 && _selectedEntry == -1)
            {
                OpenFile(0);
            }

            return;
        }

        if ((action == Action.EnterParentDirectory && _selectedEntry != -1) || (action == Action.ForceEnterParentDirectory && _selectedEntry == -1))
        {
            string parentDir = GetParentDirectory();
            string oldPath = _currentPath;
            UpdatePath(parentDir + (parentDir == "/" ? "" : Path.DirectorySeparatorChar));

            if (Directory.Exists(oldPath)) oldPath = oldPath.Remove(oldPath.Length - 1, 1);
            int entryIndex = FindEntryIndex(oldPath);

            if (entryIndex != -1)
            {
                _selectedEntry = entryIndex;
            }
            else
            {
                _selectedEntry = 0;
            }

            CenteriseScroll(true);
            return;
        }

        if (action == Action.LeaveSearchBar)
        {
            if (_systemEntries.Length > 0 && _selectedEntry == -1)
            {
                _selectedEntry = 0;
            }

            return;
        }

        if (action == Action.EnterSearchBar)
        {
            _selectedEntry = -1;
            return;
        }

        if (action == Action.MoveUp)
        {
            if (_selectedEntry > 0)
            {
                _selectedEntry--;
                CenteriseScroll();
            }

            return;
        }

        if (action == Action.MoveDown)
        {
            if (_systemEntries.Length > _selectedEntry + 1)
            {
                _selectedEntry++;
                CenteriseScroll();
            }

            return;
        }

        if (action == Action.CtrlBackspace)
        {
            if (_currentPath[_currentPath.Length - 1] == Path.DirectorySeparatorChar)
            {
                UpdatePath(_currentPath = _currentPath.Remove(_currentPath.Length - 1, 1));
            }

            int index = _currentPath.LastIndexOf(Path.DirectorySeparatorChar);
            UpdatePath(_currentPath.Substring(0, index + 1));
            return;
        }

        if (action == Action.Backspace)
        {
            UpdatePath(_currentPath.Remove(_currentPath.Length - 1, 1));
            return;
        }
    }

    private void CenteriseScroll(bool immediate = false)
    {
        float value = Math.Clamp(-(ENTRY_SPACING * _selectedEntry) + WindowHeight / 2, Math.Min(-(ENTRY_SPACING * _systemEntries.Length) + WindowHeight - PATH_HEIGHT - PADDING, 0), 0);

        _preferredScroll = value;

        if (immediate)
            _scroll = value;
    }

    private void OpenFile(int entryIndex)
    {
        string entry = _systemEntries[entryIndex];
        if (Directory.Exists(entry))
        {
            UpdatePath(entry + Path.DirectorySeparatorChar);
            if (_selectedEntry != -1) 
                _selectedEntry = 0;
            return;
        }
        else if (File.Exists(entry))
        {
            string extension = Path.GetExtension(entry);
            OpenFileType(entry, extension);
        }
    }

    private void OpenFileType(string entry, string extension)
    {
        FileType fileType = FileTypeUtils.FromExtension(extension);

        if (fileType == FileType.Unknown)
        {
            Console.WriteLine($"""Unknown file type "{extension}" """);
            return;
        }

        FileTypeUtils.DefaultApplications.TryGetValue(fileType, out string? defaultApp);
        if (defaultApp == null) return;

        if (defaultApp == "Built-In")
        {
            OpenFileTypeBuiltIn(entry, fileType);
            return;
        }

        ProcessStartInfo processStartInfo = new()
        {
            FileName = defaultApp,
            Arguments = entry,
            UseShellExecute = true
        };

        Process.Start(processStartInfo);
    }

    private void OpenFileTypeBuiltIn(string entry, FileType fileType)
    {
        if (fileType == FileType.Unknown)
            return;

        if (fileType == FileType.Image)
        {
            nint textureHandle = SDL3.Image.LoadTexture(_renderer.Handle, entry);

            if (textureHandle != 0)
            {
                UpdatePath(entry);

                if (_selectedEntry == -1) 
                    _selectedEntry = 0;

                Texture2D texture = new Texture2D(textureHandle, "");
                SDL.SetTextureScaleMode(texture.Handle, SDL.ScaleMode.Nearest);
                
                _imageHandler.SetImage(texture);
                return; 
            }

            return;
        }
    }

    private int FindEntryIndex(string entry)
    {
        return _systemEntries.IndexOf(entry);
    }
    
    public static float Lerp(float start, float end, double deltaTime, float speed, float errorCorrection)
    {
        start = MathHelper.Lerp(start, end, speed * (float)deltaTime);

        if (Math.Abs(end - start) < errorCorrection)
            start = end;

        return start;
    }

    public override void End() 
    {
        _window.Dispose();
        _renderer.Dispose();
        AssetManager.Dispose();
    }
}