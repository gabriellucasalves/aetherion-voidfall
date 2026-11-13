using System.Collections;
using UnityEngine;

// A single unscaled clock owns hitstop. Cutscenes and an explicit pause keep
// ownership of their own time scale; repeated hits extend, never stack freezes.
public static class HitStop
{
    static HitStopRunner _runner;
    static float _savedScale = 1f, _endsAt;
    public static bool IsActive { get; private set; }
    public static int LastMilliseconds { get; private set; }
    public static void Do(int ms)
    {
        if (ms <= 0 || CinematicTime.IsActive || (GameAudio.Instance != null && GameAudio.Instance.IsPaused)) return;
        if (!IsActive && Time.timeScale <= .0001f) return;
        LastMilliseconds = ms;
        if (IsActive) { _endsAt = Mathf.Max(_endsAt, Time.unscaledTime + ms / 1000f); return; }
        if (_runner == null)
        {
            var go = new GameObject("HitStopRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitStopRunner>();
        }
        _savedScale = Time.timeScale;
        _endsAt = Time.unscaledTime + ms / 1000f;
        IsActive = true;
        Time.timeScale = 0f;
        _runner.StartCoroutine(Routine());
    }
    static IEnumerator Routine()
    {
        while (IsActive && Time.unscaledTime < _endsAt && !CinematicTime.IsActive)
            yield return null;
        ForceRestore();
    }
    public static void ForceRestore()
    {
        if (!IsActive) return;
        IsActive = false;
        if (_runner != null) _runner.StopAllCoroutines();
        if (!CinematicTime.IsActive && (GameAudio.Instance == null || !GameAudio.Instance.IsPaused))
            Time.timeScale = _savedScale > .0001f ? _savedScale : 1f;
    }
}

public sealed class HitStopRunner : MonoBehaviour
{
    void OnDisable() { HitStop.ForceRestore(); }
    void OnDestroy() { HitStop.ForceRestore(); }
}
