using SDL3;
using Smash.Input;

public class KeybindHandler
{
    private List<Keybind> _keybinds = new();

    public void RegisterKeybind(SDL.Keycode key, Action action, bool requiresCtrl, bool shouldHold)
    {
        _keybinds.Add(new Keybind()
        {
            Key = key,
            RequiresCtrl = requiresCtrl,
            ShouldHold = shouldHold,
            Action = action
        });
    }

    public Action? Update()
    {
        foreach (Keybind keybind in _keybinds)
        {
            if (keybind.RequiresCtrl && !InputHandler.IsKeyDown(SDL.Keycode.LCtrl)) 
                continue;

            if (!keybind.RequiresCtrl && InputHandler.IsKeyDown(SDL.Keycode.LCtrl))
                continue;

            if (keybind.ShouldHold)
            {
                if (!KeyRepetitionHandler.CanPress(keybind.Key)) 
                    continue;

                if (InputHandler.IsKeyDown(keybind.Key))
                {
                    KeyRepetitionHandler.Press(keybind.Key);
                    return keybind.Action;
                }
            }
            else
            {
                if (InputHandler.IsKeyPressed(keybind.Key))
                {
                    return keybind.Action;
                }
            }
        }

        return null;
    }
}