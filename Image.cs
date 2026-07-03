using System.Numerics;
using Smash.Graphics;

public struct Image
{
    public readonly Texture2D Texture { get; init; }
    public readonly float Zoom { get; init; }
    public readonly Vector2 Offset { get; init; }
}