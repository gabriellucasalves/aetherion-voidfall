#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class HeroShieldValidation
{
    static double _next;
    static HeroShieldValidation() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Library/HeroShieldQA.request")) return;
        File.Delete("Library/HeroShieldQA.request"); Run();
    }
    [MenuItem("Aetherion/Validar escudos dos três heróis")]
    public static void Run()
    {
        SessionState.SetBool("HeroShieldQA.Enabled", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
