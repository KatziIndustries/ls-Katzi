using Smash.Graphics;

public interface IScene
{
    public SceneRenderResult Render(Renderer renderer, AppContext context);
}