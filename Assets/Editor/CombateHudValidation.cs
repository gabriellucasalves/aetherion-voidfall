#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CombateHudValidation
{
    [MenuItem("Aetherion/Validar Combate HUD e Fase 3")]
    public static void Run()
    {
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, true);
        SessionState.SetBool("CombateHudQA.Enabled", true);
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
