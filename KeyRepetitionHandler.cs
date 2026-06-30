using SDL3;
using Smash.Input;

public static class KeyRepetitionHandler
{
    private const float FAST_REPEAT_COOLDOWN = 0.04f; 
    private const float BASE_REPEAT_COOLDOWN = 0.15f;
    private const int REMOVE_COOLDOWN = -100000;

    private static Dictionary<SDL.Keycode, float> _cooldowns = new();

    public static bool CanPress(SDL.Keycode key)
    {
        return GetCooldown(key) <= 0;
    }

    public static void Update(double deltaTime)
    {
        for (int i = 0; i < _cooldowns.Count; i++)
        {
            SDL.Keycode key = _cooldowns.ElementAt(i).Key;

            if (_cooldowns[key] == REMOVE_COOLDOWN)
            {
                _cooldowns.Remove(key);
                i--;
                continue;
            }

            _cooldowns[key] -= (float)deltaTime;
            if (_cooldowns[key] <= 0 || !InputHandler.IsKeyDown(key))
            {
                _cooldowns[key] = REMOVE_COOLDOWN;
            }
        }
    }

    public static void Press(SDL.Keycode key)
    {
        if (!_cooldowns.ContainsKey(key))
        {
            _cooldowns.Add(key, BASE_REPEAT_COOLDOWN);
            return;
        }

        _cooldowns[key] = FAST_REPEAT_COOLDOWN;
    }

    private static float GetCooldown(SDL.Keycode key)
    {
        if (!_cooldowns.ContainsKey(key))
        {
            return 0;
        }

        return _cooldowns[key];
    }
}