#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class WorldMapValidation
{
    static double _next;
    static WorldMapValidation() { EditorApplication.update += PollMobileRequest; }
    static void PollMobileRequest()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Library/WorldMapMobileQA.request")) return;
        File.Delete("Library/WorldMapMobileQA.request");
        RunMobile();
    }

    [MenuItem("Aetherion/Validar mapa mobile e retorno das fases")]
    public static void RunMobile()
    {
        SessionState.SetBool("WorldMapMobileQA.Enabled", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }

    public static void Run()
    {
        SessionState.SetBool("WorldMapQA.Enabled", true);
        PolishValidation.Size(1920, 1080);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
