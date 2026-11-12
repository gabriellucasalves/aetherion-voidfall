using UnityEngine;

// WebGL uses the browser's standard gamepad layout for Xbox and DualSense.
// Keep the existing Input Manager and menu navigation; share the same device
// with gameplay, even while the touch controls are visible.
public static class GamepadControls
{
    public const int Jump = 0;       // A / Cross
    public const int Dash = 1;       // B / Circle
    public const int Attack = 2;     // X / Square (hold to charge the Archer)
    public const int Special = 3;    // Y / Triangle
    public const int Block = 4;      // LB / L1 (hold)
    public const int Supreme = 5;    // RB / R1
    public const int Pause = 9;      // Menu / Options (WebGL standard mapping)

    public static bool Held(int button) => Input.GetKey(Key(button));
    public static bool Down(int button) => Input.GetKeyDown(Key(button));
    public static bool Up(int button) => Input.GetKeyUp(Key(button));

    public static float DpadHorizontal
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return (Held(15) ? 1f : 0f) - (Held(14) ? 1f : 0f);
#else
            return Input.GetAxisRaw(IsPlayStation() ? "PadDpadPS" : "PadDpadXbox");
#endif
        }
    }

    static KeyCode Key(int button)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // Native legacy HID exposes Sony face buttons as Square/Cross/Circle/
        // Triangle; Windows XInput exposes Xbox as A/B/X/Y and Menu at index 7.
        bool sony = IsPlayStation();
        if (sony && button <= Attack)
            button = button == Jump ? 1 : button == Dash ? 2 : 0;
        else if (!sony && button == Pause)
            button = 7;
#endif
        return (KeyCode)((int)KeyCode.JoystickButton0 + button);
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    static int _namesFrame = -1;
    static bool _sony;
    static bool IsPlayStation()
    {
        if (_namesFrame == Time.frameCount) return _sony;
        _namesFrame = Time.frameCount;
        _sony = false;
        foreach (string name in Input.GetJoystickNames())
        {
            string id = (name ?? "").ToLowerInvariant();
            if (id.Contains("xbox") || id.Contains("xinput")) continue;
            if (id.Contains("dualsense") || id.Contains("dualshock")
                || id.Contains("wireless controller") || id.Contains("054c"))
            {
                _sony = true;
                break;
            }
        }
        return _sony;
    }
#endif
}
