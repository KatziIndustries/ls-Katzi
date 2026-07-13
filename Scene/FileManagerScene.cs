using Color = System.Drawing.Color;
using Smash.Graphics;
using System.Numerics;
using SDL3;
using Smash;
using Smash.Input;
using System.Diagnostics;

public class FileManagerScene : IScene
{
    public static string CurrentPath => _currentPath;
    public Color PathColor => _selectedEntry == -1 ? Color.FromArgb(50, 50, 50) : Color.FromArgb(28, 28, 28);

    private ImageHandler _imageHandler = new();
    private KeybindHandler _keybindHandler = new();

    private static string _currentPath = null!;

    private InputField _pathField;
    private Separator _pathSeparator;

    private string[] _systemEntries = [];
    private int _selectedEntry = -1;

    private Button[] _entryButtons = [];

    private float _scroll;
    private float _preferredScroll;

    private bool _showHiddenFiles = false;
    private bool _pathPermissionDenied = false;

    private BookmarkHandler _bookmarkHandler = new();

    private float _lastWindowWidth;

    private bool _renaming = false;

    public FileManagerScene()
    {
        _keybindHandler.RegisterKeybind(SDL.Keycode.E, Action.Enter, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.B, Action.EnterParentDirectory, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Tab, Action.ForceEnterDirectory, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Escape, Action.LeaveSearchBar, false, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.EnterSearchBar, true, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.K, Action.MoveUp, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.J, Action.MoveDown, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.Backspace, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.CtrlBackspace, true, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Comma, Action.ToggleConfig , true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.H, Action.ToggleShowHiddenFiles , true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.D, Action.ToggleBookmarks , true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.T, Action.OpenTerminal , true, false);
        _keybindHandler.RegisterKeybind(SDL.Keycode.N, Action.Rename , true, false);

        if (App.InitialDirectory != null)
        {
            _currentPath = App.InitialDirectory;
            if (File.Exists(_currentPath))
                OpenFile(_currentPath);
        }
        else
        {
            _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar;
        }

        Font font = AssetManager.Get<Font>(App.FONT_NAME);
        _pathField = new InputField(App.WindowWidth, App.PATH_HEIGHT, 0, App.BackgroundColor, App.ForegroundColor, new TextElement(font, _currentPath, App.POINT_SIZE, Alignment.Left));
        _pathSeparator = new Separator();

        _lastWindowWidth = App.WindowWidth;

        RefreshSystemEntries();

        SelectEntry(0);
    }

    public bool Update(double deltaTime)
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

        if (!_bookmarkHandler.Active || _bookmarkHandler.Closing)
        {
            if (_imageHandler.Update(deltaTime))
                needsRedraw = true;

            if (InputHandler.TextInput != null && InputHandler.TextInput != string.Empty)
            {
                if (_selectedEntry == -1)
                {
                    UpdatePath(_currentPath + InputHandler.TextInput);
                    needsRedraw = true;

                    RefreshSystemEntries();
                }
                else if (_renaming)
                {
                    ((InputField)_entryButtons[_selectedEntry]).Text += InputHandler.TextInput;
                }
            }

            Action? action = _keybindHandler.Update();
            if (action != null)
            {
                if (!_renaming)
                {
                    PerformAction((Action)action);
                    needsRedraw = true;
                }
                else
                {
                    action = (Action)action;
                    if (action == Action.Backspace)
                        Backspace();

                    if (action == Action.CtrlBackspace)
                        Backspace(true);
                }
            }
        }

        if (_bookmarkHandler.Active || _bookmarkHandler.Closing)
        {
            (bool bookmarkNeedsRedraw, string? path) = _bookmarkHandler.Update(deltaTime);
            
            if (path != null)
            {
                UpdatePath(path);
                SelectEntry(0);
                _bookmarkHandler.Close();
            }

            if (bookmarkNeedsRedraw)
                needsRedraw = true;
        }

        if (_lastWindowWidth != App.WindowWidth)
        {
            _lastWindowWidth = App.WindowWidth;

            _pathField.Width = App.WindowWidth;
            _pathField.Text = _currentPath;

            ResizeUI();
        }

        if (InputHandler.IsKeyPressed(SDL.Keycode.Return) && _renaming)
        {
            FinishRename();
        }

        return needsRedraw;
    }

    public void Render(Renderer renderer)
    {
        if (File.Exists(_currentPath) && _imageHandler.Image != null)
        {
            Image image = (Image)_imageHandler.Image!;

            Vector2 position = new Vector2(App.WindowWidth / 2, App.WindowHeight / 2) - image.Texture.Bounds * image.Zoom / 2;
            renderer.RenderTexture(image.Texture, position - image.Offset, Color.White, image.Zoom);
        }
        else
        {
            RenderDirectory(renderer);
            _imageHandler.DisposeImage();
        }

        UIContext context = new()
        {
            TotalWidth = App.WindowWidth,
            TotalHeight = App.PATH_HEIGHT - App.PADDING,
        };

        _pathField.Render(renderer, Vector2.Zero, context);
        _pathSeparator.Render(renderer, new Vector2(0, App.PATH_HEIGHT), context);

        if (_bookmarkHandler.Active || _bookmarkHandler.Closing)
        {
            if (!_bookmarkHandler.Closing)
            {
                Rectangle overlayRectangle = new(0, 0, App.WindowWidth, App.WindowHeight);
                renderer.RenderFilledRectangle(overlayRectangle, Color.FromArgb(125, 0, 0, 0));
            }

            _bookmarkHandler.Render(renderer);
        }
    }

    private void RenderDirectory(Renderer renderer)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Vector2 entriesStartPosition = new Vector2(0, App.PADDING) + new Vector2(0, _scroll + App.PATH_HEIGHT);

        UIContext context = new()
        {
            TotalWidth = App.WindowWidth,
            TotalHeight = App.ENTRY_SPACING
        };

        Rectangle clipRect = new Rectangle(0, App.PATH_HEIGHT, App.WindowWidth, App.WindowHeight - App.PATH_HEIGHT);
        SDL.SetRenderClipRect(renderer.Handle, clipRect.ToSDLRect());
        
        if (!_pathPermissionDenied)
        {
            foreach (Button button in _entryButtons)
            {
                entriesStartPosition.Y += button.Render(renderer, entriesStartPosition, context);

                if (entriesStartPosition.Y > App.WindowHeight)
                    break;
            }
        }
        else
        {
            renderer.RenderText(font, App.POINT_SIZE, "Can't access this directory (Permission denied)", entriesStartPosition, Color.Red);
        }

        SDL.SetRenderClipRect(renderer.Handle, IntPtr.Zero);
    }

    public void PerformAction(Action action)
    {
        switch (action)
        {
            case Action.Enter:
                if (_selectedEntry != -1 && _selectedEntry < _systemEntries.Length)
                    OpenEntry(_selectedEntry);
                break;

            case Action.ForceEnterDirectory:
                if (_systemEntries.Length > 0 && _selectedEntry == -1)
                    OpenEntry(0);
                break;

            case Action.LeaveSearchBar:
                if (_systemEntries.Length > 0 && _selectedEntry == -1)
                    SelectEntry(0);
                break;

            case Action.EnterSearchBar:
                SelectEntry(-1);
                break;

            case Action.MoveUp:
                MoveUp();
                break;
            
            case Action.MoveDown:
                MoveDown();
                break;

            case Action.Backspace:
                Backspace();
                break;

            case Action.CtrlBackspace:
                Backspace(true);
                break;

            case Action.ToggleConfig:
                App.SetScene<ConfigScene>();
                break;

            case Action.ToggleShowHiddenFiles:
                _showHiddenFiles = !_showHiddenFiles;
                RefreshSystemEntries();
                SelectEntry(0);
                break;

            case Action.ToggleBookmarks:
                _bookmarkHandler.Toggle();
                break;

            case Action.EnterParentDirectory:
                if (_selectedEntry == -1)
                    break;

                OpenParentDirectory();
                break;

            case Action.OpenTerminal:   
                Utils.OpenTerminal(_currentPath);
                break;

            case Action.Rename:
                Rename();
                break;
        }
    }

    private void Rename()
    {
        if (_selectedEntry == -1)
            return;

        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Button button = _entryButtons[_selectedEntry];
        
        bool directory = Directory.Exists(_systemEntries[_selectedEntry]);
        string text = button.Text!;

        if (directory)
            text = text.Remove(text.Length - 1, 1);

        TextElement textElement = new TextElement(font, text, App.POINT_SIZE, Alignment.Left) { XPadding = App.PADDING };
        InputField inputField = new(button.Width, button.Height, button.Padding, App.BackgroundColor, App.ForegroundColor, textElement);

        _entryButtons[_selectedEntry] = inputField;
        _renaming = true;
    }

    private void FinishRename()
    {
        string entry = _systemEntries[_selectedEntry];
        if (File.Exists(entry))
        {
            string directory = Path.GetDirectoryName(entry)!;
            string name = ((InputField)_entryButtons[_selectedEntry]).Text!;

            try
            {
                File.Move(entry, Path.Combine(directory, name));
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Couldn't rename file (No permission)");
            }
            catch (IOException) { }
        }
        else if (Directory.Exists(entry))
        {
            string dir = Path.GetDirectoryName(entry)!;
            string name = ((InputField)_entryButtons[_selectedEntry]).Text!;

            try
            {
                Directory.Move(entry, Path.Combine(dir, name));
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Couldn't rename directory (No permission)");
            }
            catch (IOException) { }
        }

        RefreshSystemEntries();
        SelectEntry(_selectedEntry);
        _renaming = false;
    }

    private void MoveUp()
    {
        if (_renaming)
            return;

        if (_selectedEntry > 0)
        {
            SelectEntry(_selectedEntry - 1);
            CenterScroll();
        }
    }

    private void MoveDown()
    {
        if (_renaming)
            return;

        if (_systemEntries.Length > _selectedEntry + 1)
        {
            SelectEntry(_selectedEntry + 1);
            CenterScroll();
        }
    }

    private void Backspace(bool ctrl = false)
    {
        if(_renaming)
        {
            InputField inputField = (InputField)_entryButtons[_selectedEntry];

            if (inputField.Text != null && inputField.Text.Length > 0)
            {
                inputField.Text = inputField.Text!.Remove(inputField.Text.Length - 1, 1);
            }

            return;
        }

        if (!ctrl)
            UpdatePath(_currentPath.Remove(_currentPath.Length - 1, 1));
        else
            UpdatePath(Utils.JumpBack(_currentPath));
    }

    private void SelectEntry(int index)
    {
        if (index > _systemEntries.Length - 1)
            return;

        ToggleButtonByIndex(_selectedEntry, false);
        ToggleButtonByIndex(index, true);
            
        _selectedEntry = index;
    }

    private void ToggleButtonByIndex(int index, bool select)
    {
        if (index == -1)
        {
            _pathField.Selected = select;
            return;
        }

        if (index > _entryButtons.Length - 1)
            return;

        _entryButtons[index].Selected = select; 
    }

    private void CenterScroll(bool immediate = false)
    {
        float value = Math.Clamp(-(App.ENTRY_SPACING * _selectedEntry) + App.WindowHeight / 2, Math.Min(-(App.ENTRY_SPACING * _systemEntries.Length) + App.WindowHeight - App.PATH_HEIGHT - App.PADDING * 2, 0), 0);

        _preferredScroll = value;

        if (immediate)
            _scroll = value;
    }

    private void OpenParentDirectory()
    {
        string parentDir = Utils.GetParentDirectory(_currentPath);
        string oldPath = _currentPath;
        UpdatePath(parentDir + (parentDir == "/" ? "" : Path.DirectorySeparatorChar));

        if (Directory.Exists(oldPath)) oldPath = oldPath.Remove(oldPath.Length - 1, 1);
        int entryIndex = _systemEntries.IndexOf(oldPath);

        if (entryIndex != -1)
            SelectEntry(entryIndex);
        else
            SelectEntry(0);

        CenterScroll(true);
        return;
    }

    private void OpenEntry(int entryIndex)
    {
        if (entryIndex > _systemEntries.Length - 1)
            return;

        string entry = _systemEntries[entryIndex];

        if (Directory.Exists(entry))
        {
            UpdatePath(entry + Path.DirectorySeparatorChar);

            if (_selectedEntry != -1) 
                SelectEntry(0);

            return;
        }
        else if (File.Exists(entry))
        {
            OpenFile(entry);
        }
    }

    private void OpenFile(string path)
    {
        string extension = Path.GetExtension(path);

        FileType fileType = FileTypeUtils.FromExtension(extension);

        if (fileType == FileType.Unknown)
        {
            Console.WriteLine($"""Unknown file type "{extension}" """);
            return;
        }

        FileTypeUtils.DefaultApplications.TryGetValue(fileType, out string? defaultApp);
        if (defaultApp == null) 
            return;

        if (defaultApp == "Built-In")
        {
            OpenFileBuiltIn(path, fileType);
            return;
        }

        ProcessStartInfo processStartInfo;

        if (fileType == FileType.Executable)
        {
            processStartInfo = new()
            {
                FileName = path,
                UseShellExecute = true
            };
        }
        else
        {
            processStartInfo = new()
            {
                FileName = defaultApp,
                Arguments = path,
                UseShellExecute = true
            };
        }

        Process.Start(processStartInfo);
    }

    private void OpenFileBuiltIn(string entry, FileType fileType)
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
                    SelectEntry(0);

                Texture2D texture = new Texture2D(textureHandle, "");
                SDL.SetTextureScaleMode(texture.Handle, SDL.ScaleMode.Linear);
                
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
            _pathField.Text = _currentPath;
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
                SetSystemEntries([]);
                SelectEntry(-1);
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
            _pathPermissionDenied = true;
            SetSystemEntries([]);
            SelectEntry(0);
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
            SetSystemEntries(newEntries);
            return true;
        }
    }

    private void SetSystemEntries(string[] entries)
    {
        _systemEntries = entries;
        
        _entryButtons = new Button[entries.Length];

        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        for (int i = 0; i < entries.Length; i++)
        {
            bool isDirectory = Directory.Exists(entries[i]);
            string name = Path.GetFileName(entries[i]) + (isDirectory ? Path.DirectorySeparatorChar : "");

            TextElement textElement = new(font, name, App.POINT_SIZE, Alignment.Left)
            {
                TextColor = isDirectory ? Color.RoyalBlue : Color.White,
                XPadding = App.PADDING
            };
            
            _entryButtons[i] = new Button(App.WindowWidth, App.ENTRY_SPACING, 0, App.BackgroundColor, App.ForegroundColor, textElement);
        }
    }

    private float GetMinScroll(int numSysEntries)
    {
        return -App.PADDING * 2 - (numSysEntries * App.ENTRY_SPACING) + (App.WindowHeight - App.PATH_HEIGHT);
    }

    private void ResizeUI()
    {
        foreach (Button button in _entryButtons)
        {
            button.Width = App.WindowWidth;
        }
    }
}