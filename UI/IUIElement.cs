using System.Numerics;
using Smash.Graphics;

public interface IUIElement
{
    public float Render(Renderer renderer, Vector2 position, UIContext uiContext);
}