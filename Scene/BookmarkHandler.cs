using Color = System.Drawing.Color;
using Smash.Graphics;
using Smash;
using Smash.Input;
using SDL3;

public class BookmarkHandler
{
    public const int SCROLL_SPEED = 30;

    public static float MaxWidth => App.WindowWidth / 4;

    private const float MIN_OPENED_TIME = 0.01f;

    private float _width = 0;

    public bool Active { get; private set; }
    public bool Closing { get; private set; }

    private float _openedTime;

    public bool Update(double deltaTime)
    {
        bool needsRedraw = false;

        if (Closing)
        {
            _width = App.Lerp(_width, 0, deltaTime, SCROLL_SPEED, 1);
            if (_width == 0)
            {
                Active = false;
            }
        }
        else
        {
            _width = App.Lerp(_width, MaxWidth, deltaTime, SCROLL_SPEED, 1);

            if (_openedTime > MIN_OPENED_TIME)
            {
                if (InputHandler.IsKeyDown(SDL.Keycode.LCtrl))
                {
                    if (InputHandler.IsKeyPressed(SDL.Keycode.D))
                    {
                        Toggle();
                    }
                }
            }

            _openedTime += (float)deltaTime;
        }


        return needsRedraw;
    }

    public void Render(Renderer renderer)
    {
        Rectangle rectangle = new(0, 0, _width, App.WindowHeight);
        renderer.RenderFilledRectangle(rectangle, App.BackgroundColor);
    }

    public void Toggle()
    {
        if (Closing)
        {
            Closing = false;
            Active = true;
            return;
        }
    
        if (Active && !Closing)
        {
            Closing = true;
            _openedTime = 0;
            return;
        }
        
        if (!Active)
        {
            Active = true;
            Closing = false;
            _openedTime = 0;
        }
    }
}