using Smash.Graphics;

public class SceneManager
{
    public bool NeedsRedraw { get; private set; } = false;

    private Dictionary<Scenes, Scene> _scenes = new();

    private Scenes _currentScene = Scenes.FileManager;

    public bool Update(double deltaTime)
    {
        _scenes.TryGetValue(_currentScene, out Scene? cachedScene);

        if (cachedScene == null)
        {
            Scene scene = CreateScene(_currentScene);
            _scenes.Add(_currentScene, scene);

            return scene.Update(deltaTime);
        }
        else
        {
            return cachedScene.Update(deltaTime);
        }
    }

    public void Render(Renderer renderer)
    {
        _scenes.TryGetValue(_currentScene, out Scene? cachedScene);

        if (cachedScene == null)
        {
            Scene scene = CreateScene(_currentScene);
            _scenes.Add(_currentScene, scene);

            scene.Render(renderer);
        }
        else
        {
            cachedScene.Render(renderer);
        }
    }

    public void SetScene(Scenes scene)
    {
        _currentScene = scene;
        NeedsRedraw = true;
    }

    private Scene CreateScene(Scenes scene)
    {
        switch (scene)
        {
            case Scenes.FileManager:
                return new FileManagerScene();

            case Scenes.Config:
                return new ConfigScene();
        }

        return null!;
    }
}