#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BossLifecycleValidation
{
    static double _next;
    static BossLifecycleValidation() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Library/BossLifecycleQA.request")) return;
        File.Delete("Library/BossLifecycleQA.request");
        Run();
    }
    [MenuItem("Aetherion/Validar três formas e vozes do boss")]
    public static void Run()
    {
        Directory.CreateDirectory("/tmp/aetherion-boss-qa");
        SessionState.SetBool("BossLifecycleQA.Enabled", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
