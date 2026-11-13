#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class NazorAtaquesValidation
{
    static double _next;
    static NazorAtaquesValidation() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Library/NazorAtaquesQA.request")) return;
        File.Delete("Library/NazorAtaquesQA.request");
        Run();
    }
    [MenuItem("Aetherion/Validar ataques e formas do Nazor")]
    public static void Run()
    {
        Directory.CreateDirectory("/tmp/aetherion-combate-hud-local");
        PolishValidation.Size(768, 432);
        SessionState.SetBool("NazorAtaquesQA.Enabled", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
