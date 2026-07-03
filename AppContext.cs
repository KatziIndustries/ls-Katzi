using Smash.Graphics;

public struct AppContext
{
    public required string[] SystemEntries;
    public required int SelectedEntry;
    public required string CurrentPath;
    public required float Scroll;
    public required bool PathPermissionDenied;
    public required Texture2D? ImageTexture;
    public required float ImageZoom;
}