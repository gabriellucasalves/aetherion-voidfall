#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class F1Validation
{
    public static void Run()
    {
        SessionState.SetBool("F1Smoke.Enabled", true);
        SessionState.SetBool("F1Smoke.OldAtmos", T1AtmosScenery.Enabled);
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, false);
        SessionState.SetBool("F1Smoke.OldHybrid", T1HybridScenery.Enabled);
        EditorPrefs.SetBool(T1HybridScenery.EditorPrefKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
