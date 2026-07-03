using System.Numerics;
using SDL3;
using Smash.Graphics;
using Smash.Input;

public class ImageHandler
{
    public const float ZOOM_SPEED = 0.15f;
    public const float ZOOM_ANIM_SPEED = 35f;

    public Image? Image => CreateImage();

    private Texture2D? _texture = null;
    private Vector2 _offset;

    private float _zoom = 1;
    private float _preferredZoom = 1;

    private Vector2? _holdStartPosition = null;

    public bool Update(double deltaTime)
    {
        bool needsRedraw = false;

        {
            if (InputHandler.ScrollWheelDelta != 0)
                _preferredZoom += InputHandler.ScrollWheelDelta * ZOOM_SPEED;

            if (InputHandler.IsKeyPressed(SDL.Keycode.Plus))
                _preferredZoom += ZOOM_SPEED * 2;

            if (InputHandler.IsKeyPressed(SDL.Keycode.Minus))
                _preferredZoom -= ZOOM_SPEED * 2;

            _preferredZoom = Math.Max(_preferredZoom, 0.1f);
        }

        if (_zoom != _preferredZoom)
        {
            _zoom = App.Lerp(_zoom, _preferredZoom, deltaTime, ZOOM_ANIM_SPEED, 0.01f);
            needsRedraw = true;
        }

        if (InputHandler.IsLeftMouseDown())
        {
            if (_holdStartPosition == null)
            {
                _holdStartPosition = InputHandler.MousePosition - (App.WindowBounds / 2) + _offset;
            }
            else
            {
                _offset += (Vector2)_holdStartPosition - (InputHandler.MousePosition - (App.WindowBounds / 2) + _offset);
                _holdStartPosition = InputHandler.MousePosition - (App.WindowBounds / 2) + _offset;
            }

            needsRedraw = true;
        }
        else _holdStartPosition = null;

        return needsRedraw;
    }

    public void DisposeImage()
    {
        if (_texture == null)
            return;

        _texture.Dispose();
        _texture = null;
        _offset = Vector2.Zero;
        _zoom = 1;
        _preferredZoom = 1;
    }

    public void SetImage(Texture2D texture)
    {
        _texture = texture;
    }

    private Image? CreateImage()
    {
        if (_texture == null) 
            return null;

        return new Image()
        {
            Texture = _texture,
            Offset = _offset,
            Zoom = _zoom
        };
    }
}