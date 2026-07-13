using System.Drawing;
using System.Numerics;
using Smash.Graphics;

public class TextElement : IUIElement
{
    public string Text;

    public float TextWidth => _font.MeasureString(Text, _pointSize).X;
    public float TextHeight => _textHeight;
    public int PointSize => _pointSize;

    public float XPadding { get; init; }

    public Color TextColor { get; init; } = Color.White;


    private readonly Font _font;
    private readonly int _pointSize;
    private readonly Alignment _alignment;
    private readonly float _textHeight;

    public TextElement(Font font, string text, int pointSize, Alignment alignment = Alignment.Center)
    {
        _font = font;
        Text = text;
        _pointSize = pointSize;
        _alignment = alignment;
        _textHeight = _font.MeasureString("|", _pointSize).Y;
    }

    public float Render(Renderer renderer, Vector2 position, UIContext context)
    {
        Vector2 textSize = _font.MeasureString(Text, _pointSize);
        Vector2 textPosition = GetPosition(position, context);

        renderer.RenderText(_font, _pointSize, Text, textPosition, TextColor);
        return textSize.Y * 1.5f;
    }

    public Vector2 GetPosition(Vector2 basePosition, UIContext context)
    {
        Vector2 textPosition;

        switch (_alignment)
        {
            case Alignment.Center:
                textPosition = Vector2.Round(basePosition + new Vector2(context.TotalWidth / 2, 0) + new Vector2(-TextWidth, TextHeight) / 2);
                break;

            case Alignment.Left:
                textPosition = Vector2.Round(basePosition + new Vector2(TextHeight / 2, TextHeight - context.TotalHeight / 2));
                break;

            default:
                textPosition = Vector2.Round(basePosition + new Vector2(context.TotalWidth / 2, 0) + new Vector2(-TextWidth, TextHeight) / 2);
                break;
        }

        textPosition.X += XPadding;

        return textPosition;
    }
}