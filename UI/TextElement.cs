using System.Drawing;
using System.Numerics;
using Smash.Graphics;

public class TextElement : IUIElement
{
    public string Text;

    private readonly Font _font;
    private readonly int _pointSize;

    public TextElement(Font font, string text, int pointSize)
    {
        _font = font;
        Text = text;
        _pointSize = pointSize;
    }

    public float Render(Renderer renderer, Vector2 position, UIContext context)
    {
        Vector2 textSize = _font.MeasureString(Text, _pointSize);
        Vector2 middlePosition = Vector2.Round(position + new Vector2(context.TotalWidth / 2, 0) + new Vector2(-textSize.X, textSize.Y) / 2);

        renderer.RenderText(_font, _pointSize, Text, middlePosition, Color.White);
        return textSize.Y * 1.5f;
    }
}