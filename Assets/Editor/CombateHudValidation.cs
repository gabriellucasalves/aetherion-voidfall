#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class CombateHudValidation
{
    static double _next;
    static CombateHudValidation() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Library/CombateHudQA.request")) return;
        File.Delete("Library/CombateHudQA.request");
        Run();
    }

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
