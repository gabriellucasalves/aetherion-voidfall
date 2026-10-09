using UnityEngine;

// Time.timeScale do cut-in. A pausa fica bloqueada enquanto IsActive,
// e o valor de jogo (1) é sempre restaurado — inclusive se o herói morrer no meio.
public static class CinematicTime
{
    const float DefaultFixedDelta = 0.02f;

    static float _savedScale = 1f;

    public static bool IsActive { get; private set; }

    public static void Begin(float worldScale)
    {
        if (IsActive)
        {
            Set(worldScale);
            return;
        }

        IsActive = true;
        // 0 = pausa; valores muito baixos são slow-mo vazado, não o ritmo do jogo.
        _savedScale = Time.timeScale < 0.5f ? 1f : Time.timeScale;
        Set(worldScale);
    }

    public static void Set(float scale)
    {
        Time.timeScale = Mathf.Clamp(scale, 0f, 1f);
        // Física acompanha o slow-mo sem passo zerado.
        Time.fixedDeltaTime = DefaultFixedDelta * Mathf.Max(0.05f, Time.timeScale);
    }

    public static void End()
    {
        if (!IsActive)
            return;

        Time.timeScale = _savedScale;
        Time.fixedDeltaTime = DefaultFixedDelta * Mathf.Max(0.05f, _savedScale);
        IsActive = false;
    }
}
