using Color = System.Drawing.Color;
using Smash.Graphics;
using System.Numerics;
using SDL3;
using Smash;

public class FileManagerScene : IScene
{
    public SceneRenderResult Render(Renderer renderer, AppContext context)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        bool shouldDeleteImage = false;

        if (File.Exists(context.CurrentPath) && context.Image != null)
        {
            RenderFile(renderer, context);
            shouldDeleteImage = false;
        }
        else
        {
            RenderDirectory(renderer, context);
            shouldDeleteImage = true;
        }

        Color pathColor = context.SelectedEntry == -1 ? Color.FromArgb(50, 50, 50) : Color.FromArgb(28, 28, 28);

        Rectangle pathRectangle = new(0, 0, App.WindowWidth, App.PATH_HEIGHT);
        renderer.RenderFilledRectangle(pathRectangle, pathColor);

        int lineColor = pathColor.R + 20; 
        renderer.RenderLine(pathRectangle.Position + new Vector2(0, pathRectangle.Height), pathRectangle.Position + pathRectangle.Bounds, Color.FromArgb(lineColor, lineColor, lineColor));

        Vector2 pathTextPosition = new(App.PATH_HEIGHT / 2);
        renderer.RenderText(font, App.POINT_SIZE, context.CurrentPath, pathTextPosition - new Vector2(0, font.MeasureString(context.CurrentPath, App.POINT_SIZE).Y / 2), Color.White);

        return new()
        {
            DeleteImage = shouldDeleteImage
        };
    }

    private void RenderDirectory(Renderer renderer, AppContext context)
    {
        Font font = AssetManager.Get<Font>(App.FONT_NAME);

        Vector2 entriesStartPosition = new Vector2(App.PADDING) + new Vector2(0, context.Scroll + App.PATH_HEIGHT);

        Rectangle clipRect = new Rectangle(0, App.PATH_HEIGHT, App.WindowWidth, App.WindowHeight - App.PATH_HEIGHT);
        SDL.SetRenderClipRect(renderer.Handle, clipRect.ToSDLRect());
        
        if (!context.PathPermissionDenied)
        {
            for (int i = 0; i < context.SystemEntries.Length; i++)
            {
                Vector2 position = Vector2.Round(entriesStartPosition + new Vector2(0, i * App.ENTRY_SPACING));
                if (position.Y < 0) continue;
                if (position.Y > App.WindowHeight) break;

                if (i == context.SelectedEntry)
                {
                    Vector2 hitboxStartPos = position;
                    hitboxStartPos.X = 0;
                    hitboxStartPos.Y -= App.ENTRY_SPACING / 2 - font.MeasureString(context.SystemEntries[i], App.POINT_SIZE).Y / 2;

                    Rectangle rect = new Rectangle(hitboxStartPos, App.WindowWidth, App.ENTRY_SPACING);
                    renderer.RenderFilledRectangle(rect, Color.FromArgb(50, 50, 50));
                }

                bool isDirectory = Directory.Exists(context.SystemEntries[i]);
                renderer.RenderText(font, App.POINT_SIZE, Path.GetFileName(context.SystemEntries[i]), position, isDirectory ? Color.RoyalBlue : Color.White);
            }
        }
        else
        {
            renderer.RenderText(font, App.POINT_SIZE, "Can't access this directory (Permission denied)", entriesStartPosition, Color.Red);
        }

        SDL.SetRenderClipRect(renderer.Handle, IntPtr.Zero);
    }

    private void RenderFile(Renderer renderer, AppContext context)
    {
        Image image = (Image)context.Image!;

        Vector2 position = new Vector2(App.WindowWidth / 2, App.WindowHeight / 2) - image.Texture.Bounds * image.Zoom / 2;
        renderer.RenderTexture(image.Texture, position - image.Offset, Color.White, image.Zoom);
    }
}