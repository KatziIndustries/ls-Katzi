using Smash.Graphics;

public abstract class Scene : IScene
{
    public abstract bool Update(double deltaTime);
    public abstract void Render(Renderer renderer);
    public abstract void PerformAction(Action action);
}