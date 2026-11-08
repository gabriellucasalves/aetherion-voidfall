#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CombateHudValidation
{
    public static void Run()
    {
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, true);
        SessionState.SetBool("CombateHudQA.Enabled", true);
        PolishValidation.Size(768, 432);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
