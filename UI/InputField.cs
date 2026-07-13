using Smash.Input;
using Color = System.Drawing.Color;
using SDL3;
using Smash.Graphics;
using System.Numerics;
using Smash;

public class InputField : Button
{
    public const int CARET_HEIGHT = 30;

    private KeybindHandler _keybindHandler = new();

    public InputField(float width, float height, float padding, Color backgroundColor, Color selectedColor, TextElement textElement)
    : base(width, height, padding, backgroundColor, selectedColor, textElement)
    {
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.Backspace, false, true);
        _keybindHandler.RegisterKeybind(SDL.Keycode.Backspace, Action.CtrlBackspace, true, true);
    }

    public bool Update()
    {
        bool needsRedraw = false;

        Action? action = _keybindHandler.Update();
        if (action != null)
        {
            if (PerformAction((Action)action))
                needsRedraw = true;
        }

        if (InputHandler.TextInput != null && InputHandler.TextInput.Length > 0 && _textElement != null)
        {
            _textElement.Text += InputHandler.TextInput;    
            needsRedraw = true;
        }

        return needsRedraw;
    }

    private bool PerformAction(Action action)
    {
        if (_textElement == null)
            throw new Exception("TextElement was null");

        switch (action)
        {
            case Action.Backspace:
                RemoveText(1);
                return true;

            case Action.CtrlBackspace:
                RemoveText(_textElement.Text.Length - GetLastSpace());
                return true;
        }

        return false;
    }

    private void RemoveText(int length)
    {
        if (_textElement == null)
            throw new Exception("TextElement was null");

        if (_textElement.Text.Length >= length)
        {
            _textElement.Text = _textElement.Text.Remove(_textElement.Text.Length - length, length);
        }
    }

    private int GetLastSpace()
    {
        if (_textElement == null)
            throw new Exception("TextElement was null");
        
        if (_textElement.Text.Length > 0)
        {
            for (int i = _textElement.Text.Length - 1; i > 0; i--)
            {
                if (_textElement.Text[i] == ' ')
                    return i;
            }
        }

        return 0;
    }

    public override float Render(Renderer renderer, Vector2 position, UIContext context)
    {
        Rectangle rectangle = new(position + new Vector2(Padding, 0), Width - Padding * 2, Height - Padding);
        
        Color color = Selected ? _selectedColor : _backgroundColor;
        renderer.RenderFilledRectangle(rectangle, color);

        if (_textElement != null)
        {
            _textElement.Render(renderer, position, context);

            Vector2 textPosition = _textElement.GetPosition(position, context);
            
            Rectangle caretRectangle = new(textPosition.X + _textElement.TextWidth + 1, textPosition.Y, 2, CARET_HEIGHT);
            renderer.RenderFilledRectangle(caretRectangle, Color.White);
        }

        return Height - Padding;
    }
}