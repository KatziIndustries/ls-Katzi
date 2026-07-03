
using SDL3;

public struct Keybind
{
    public required SDL.Keycode Key { get; init; }
    public required bool RequiresCtrl { get; init; }
    public required bool ShouldHold { get; init; }
    public required Action Action { get; init; }
}