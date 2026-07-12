using Color = System.Drawing.Color;
using System.Numerics;
using Smash.Graphics;
using Smash;

public class Button : IUIElement
{
    public float Width;
    public float Height;
    public float Padding;

    public bool Selected = false;

    public string? Text
    {
        get => _textElement?.Text;
        set => _textElement?.Text = value!;
    }

    protected private Color _backgroundColor { get; init; }
    protected private Color _selectedColor { get; init; }

    protected private TextElement? _textElement { get; init; }

    public Button(float width, float height, float padding, Color backgroundColor, Color selectedColor, TextElement? textElement = null)
    {
        Width = width;
        Height = height;
        Padding = padding;
        _backgroundColor = backgroundColor;
        _selectedColor = selectedColor;
        _textElement = textElement;
    }

    public Button(InputField inputField)
    {
        Width = inputField.Width;
        Height = inputField.Height;
        Padding = inputField.Padding;
        _backgroundColor = inputField._backgroundColor;
        _selectedColor = inputField._selectedColor;
        _textElement = inputField._textElement;
    }

    public virtual float Render(Renderer renderer, Vector2 position, UIContext context)
    {
        Rectangle rectangle = new(position + new Vector2(Padding, 0), Width - Padding * 2, Height - Padding);
        
        Color color = Selected ? _selectedColor : _backgroundColor;
        renderer.RenderFilledRectangle(rectangle, color);

        if (_textElement != null)
        {
            _textElement.Render(renderer, position, context);
        }

        return Height - Padding;
    }
}