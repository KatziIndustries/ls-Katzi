using Color = System.Drawing.Color;
using Smash.Graphics;
using System.Numerics;
using SDL3;
using Smash;
using Smash.Input;
using System.Diagnostics;

public class FileManagerScene : Scene
{
    public Color PathColor => _selectedEntry == -1 ? Color.FromArgb(50, 50, 50) : Color.FromArgb(28, 28, 28);

    private ImageHandler _imageHandler = new();
    private KeybindHandler _keybindHandler = new();

    private string _currentPath;
    private string[] _systemEntries = [];

    private int _selectedEntry = -1;

    private float _scroll;
    private float _preferredScroll;

    private bool _showHiddenFiles = false;
    private bool _pathPermissionDenied = false;


    public FileManagerScene()
    {
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
        _keybindHandler.RegisterKeybind(SDL.Keycode.Comma, Action.ToggleConfig , true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.H, Action.ToggleShowHiddenFiles , true, false);

        _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar;
        RefreshSystemEntries();
    }

    public override bool Update(double deltaTime)
    {
        bool needsRedraw = false;

        if (InputHandler.ScrollWheelDelta != 0)
        {
            _preferredScroll += InputHandler.ScrollWheelDelta * App.SCROLL_SPEED;
            _preferredScroll = Math.Clamp(_preferredScroll, Math.Min(GetMinScroll(_systemEntries.Length), 0), 0);
        }

        if (_scroll != _preferredScroll)
        {
            _scroll = App.Lerp(_scroll, _preferredScroll, deltaTime, App.SCROLL_ANIM_SPEED, 1);
            needsRedraw = true;
        }

        if (_imageHandler.Update(deltaTime))
            needsRedraw = true;


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

        return needsRedraw;
    }

    public override void Render(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        bool shouldDeleteImage = false;

        if (File.Exists(_currentPath) && _imageHandler.Image != null)
        {
            RenderFile(renderer);
            shouldDeleteImage = false;
        }
        else
        {
            RenderDirectory(renderer);
            shouldDeleteImage = true;
        }

        Color pathColor = _selectedEntry == -1 ? Color.FromArgb(50, 50, 50) : Color.FromArgb(28, 28, 28);

        Rectangle pathRectangle = new(0, 0, App.WindowWidth, App.PATH_HEIGHT);
        renderer.RenderFilledRectangle(pathRectangle, pathColor);

        int lineColor = pathColor.R + 20; 
        renderer.RenderLine(pathRectangle.Position + new Vector2(0, pathRectangle.Height), pathRectangle.Position + pathRectangle.Bounds, Color.FromArgb(lineColor, lineColor, lineColor));

        Vector2 pathTextPosition = new(App.PATH_HEIGHT / 2);
        renderer.RenderText(font, App.POINT_SIZE, _currentPath, pathTextPosition - new Vector2(0, font.MeasureString(_currentPath, App.POINT_SIZE).Y / 2), Color.White);

        if (shouldDeleteImage)
            _imageHandler.DisposeImage();
    }

    private void RenderFile(Renderer renderer)
    {
        Image image = (Image)_imageHandler.Image!;

        Vector2 position = new Vector2(App.WindowWidth / 2, App.WindowHeight / 2) - image.Texture.Bounds * image.Zoom / 2;
        renderer.RenderTexture(image.Texture, position - image.Offset, Color.White, image.Zoom);
    }

    private void RenderDirectory(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Vector2 entriesStartPosition = new Vector2(App.PADDING) + new Vector2(0, _scroll + App.PATH_HEIGHT);

        Rectangle clipRect = new Rectangle(0, App.PATH_HEIGHT, App.WindowWidth, App.WindowHeight - App.PATH_HEIGHT);
        SDL.SetRenderClipRect(renderer.Handle, clipRect.ToSDLRect());
        
        if (!_pathPermissionDenied)
        {
            for (int i = 0; i < _systemEntries.Length; i++)
            {
                Vector2 position = Vector2.Round(entriesStartPosition + new Vector2(0, i * App.ENTRY_SPACING));
                if (position.Y < 0) continue;
                if (position.Y > App.WindowHeight) break;

                if (i == _selectedEntry)
                {
                    Vector2 hitboxStartPos = position;
                    hitboxStartPos.X = 0;
                    hitboxStartPos.Y -= App.ENTRY_SPACING / 2 - font.MeasureString(_systemEntries[i], App.POINT_SIZE).Y / 2;

                    Rectangle rect = new Rectangle(hitboxStartPos, App.WindowWidth, App.ENTRY_SPACING);
                    renderer.RenderFilledRectangle(rect, Color.FromArgb(50, 50, 50));
                }

                bool isDirectory = Directory.Exists(_systemEntries[i]);
                renderer.RenderText(font, App.POINT_SIZE, Path.GetFileName(_systemEntries[i]), position, isDirectory ? Color.RoyalBlue : Color.White);
            }
        }
        else
        {
            renderer.RenderText(font, App.POINT_SIZE, "Can't access this directory (Permission denied)", entriesStartPosition, Color.Red);
        }

        SDL.SetRenderClipRect(renderer.Handle, IntPtr.Zero);
    }

    public override void PerformAction(Action action)
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

        if (action == Action.ToggleConfig)
        {
            App.SetScene(Scenes.Config);
        }

        if (action == Action.ToggleShowHiddenFiles)
        {
            _showHiddenFiles = !_showHiddenFiles;
            RefreshSystemEntries();
            _selectedEntry = 0;
        }
    }

    private string GetParentDirectory()
    {
        if (File.Exists(_currentPath))
            return Path.GetDirectoryName(_currentPath)!;

        string dirName = Path.GetDirectoryName(_currentPath) ?? "/";
        return (Directory.GetParent(dirName) ?? new DirectoryInfo("/")).FullName;
    }

    private void CenteriseScroll(bool immediate = false)
    {
        float value = Math.Clamp(-(App.ENTRY_SPACING * _selectedEntry) + App.WindowHeight / 2, Math.Min(-(App.ENTRY_SPACING * _systemEntries.Length) + App.WindowHeight - App.PATH_HEIGHT - App.PADDING, 0), 0);

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
        FileType fileType = FileTypeUtils.FromExtension(extension, entry);

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

        ProcessStartInfo processStartInfo;

        if (fileType == FileType.Executable)
        {
            processStartInfo = new()
            {
                FileName = entry,
                UseShellExecute = true
            };
        }
        else
        {
            processStartInfo = new()
            {
                FileName = defaultApp,
                Arguments = entry,
                UseShellExecute = true
            };
        }

        Process.Start(processStartInfo);
    }

    private void OpenFileTypeBuiltIn(string entry, FileType fileType)
    {
        if (fileType == FileType.Unknown)
            return;

        if (fileType == FileType.Image)
        {
            nint textureHandle = App.LoadTexture(entry);

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

        if (fileType == FileType.Executable)
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = entry,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
    }

    private int FindEntryIndex(string entry)
    {
        return _systemEntries.IndexOf(entry);
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
            _selectedEntry = 0;
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


    private float GetMinScroll(int numSysEntries)
    {
        return -App.PADDING * 2 - (numSysEntries * App.ENTRY_SPACING) + (App.WindowHeight - App.PATH_HEIGHT);
    }
}