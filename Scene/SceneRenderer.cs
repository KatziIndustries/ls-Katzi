using Smash.Graphics;

public class SceneRenderer
{
    private Dictionary<Scene, IScene> _scenes = new()
    {
        { Scene.FileManager, new FileManagerScene() },
        { Scene.Config, new ConfigScene() },
    };

    public SceneRenderResult Render(Renderer renderer, AppContext context, Scene scene)
    {
        return _scenes[scene].Render(renderer, context);
    }
}