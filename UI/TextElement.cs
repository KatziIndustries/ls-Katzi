using System.Drawing;
using System.Numerics;
using Smash.Graphics;

public class TextElement : IUIElement
{
    public string Text;

    public Vector2 TextSize => _font.MeasureString(Text, _pointSize);

    private readonly Font _font;
    private readonly int _pointSize;
    private readonly Alignment _alignment;

    public TextElement(Font font, string text, int pointSize, Alignment alignment = Alignment.Center)
    {
        _font = font;
        Text = text;
        _pointSize = pointSize;
        _alignment = alignment;
    }

    public float Render(Renderer renderer, Vector2 position, UIContext context)
    {
        Vector2 textSize = _font.MeasureString(Text, _pointSize);
        Vector2 textPosition = GetPosition(position, context);

        renderer.RenderText(_font, _pointSize, Text, textPosition, Color.White);
        return textSize.Y * 1.5f;
    }

    public Vector2 GetPosition(Vector2 basePosition, UIContext context)
    {
        Vector2 textSize = _font.MeasureString(Text, _pointSize);
        Vector2 textPosition;

        switch (_alignment)
        {
            case Alignment.Center:
                textPosition = Vector2.Round(basePosition + new Vector2(context.TotalWidth / 2, 0) + new Vector2(-textSize.X, textSize.Y) / 2);
                break;

            case Alignment.Left:
                textPosition = Vector2.Round(basePosition + new Vector2(textSize.Y / 2));
                break;

            default:
                textPosition = Vector2.Round(basePosition + new Vector2(context.TotalWidth / 2, 0) + new Vector2(-textSize.X, textSize.Y) / 2);
                break;
        }
        return textPosition;
    }
}