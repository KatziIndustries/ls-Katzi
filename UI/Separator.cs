using System.Numerics;
using Smash.Graphics;

public class Separator : IUIElement
{
    public float Render(Renderer renderer, Vector2 position, UIContext uiContext)
    {
        renderer.RenderLine(position, position + new Vector2(uiContext.TotalWidth, 0), App.ForegroundColor);
        return 3;
    }
}