#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AetherionProjectSetup
{
    const string BootPath = "Assets/Scenes/Boot.unity";

    static readonly string[] RequiredFolders =
    {
        "Assets/Art",
        "Assets/Animations",
        "Assets/Audio",
        "Assets/Materials",
        "Assets/Prefabs",
        "Assets/Scenes",
        "Assets/Scripts",
        "Assets/Scripts/Core",
        "Assets/Scripts/Boot",
        "Assets/Scripts/Player",
        "Assets/Scripts/Enemies",
        "Assets/Scripts/Combat",
        "Assets/Scripts/Abilities",
        "Assets/Scripts/Upgrades",
        "Assets/Scripts/Waves",
        "Assets/Scripts/UI",
        "Assets/Scripts/Ships",
        "Assets/Scripts/Managers",
        "Assets/ScriptableObjects",
        "Assets/Scripts/Stages",
        "Assets/UI"
    };

    static AetherionProjectSetup()
    {
        EditorApplication.delayCall += EnsureFoundation;
    }

    static void EnsureFoundation()
    {
        if (Application.isPlaying)
            return;

        foreach (var folder in RequiredFolders)
            EnsureFolder(folder);

        var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath);
        if (boot != null)
            EditorSceneManager.playModeStartScene = boot;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BootPath, true),
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Intro.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CharacterSelection.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Ground_T1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Pong.unity", false)
        };
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = path.Substring(0, path.LastIndexOf('/'));
        var name = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    [MenuItem("Aetherion/Abrir Boot")]
    static void OpenBoot()
    {
        EditorSceneManager.OpenScene(BootPath);
    }
}
#endif
