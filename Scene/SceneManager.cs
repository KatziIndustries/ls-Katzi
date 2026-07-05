using Smash.Graphics;

public class SceneManager
{
    public bool NeedsRedraw { get; private set; } = false;

    private List<IScene> _scenes = new()
    {
        new FileManagerScene(),
        new ConfigScene()
    };

    private IScene? _currentScene;

    public SceneManager()
    {
        SetScene<FileManagerScene>();
    }

    public bool Update(double deltaTime)
    {
        if (_currentScene == null)
            return false;

        return _currentScene.Update(deltaTime);
    }

    public void Render(Renderer renderer)
    {
        if (_currentScene == null)
            return;

        _currentScene.Render(renderer);
    }

    public void SetScene<T>() where T : IScene
    {
        IScene? scene = _scenes.FirstOrDefault(f => f.GetType() == typeof(T));

        _currentScene = scene;
        NeedsRedraw = true;
    }
}