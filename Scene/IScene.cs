using Smash.Graphics;

public interface IScene
{
    public bool Update(double deltaTime);
    public void Render(Renderer renderer);
    public void PerformAction(Action action);
}