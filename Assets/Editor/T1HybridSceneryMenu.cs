#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// QA do fundo híbrido da Fase I no Editor.
//   Aetherion > Fundo T1 > Usar fundo híbrido (V3+V2)   liga/desliga (EditorPrefs)
//   Aetherion > Fundo T1 > Jogar Ground_T1 direto        Play começando na fase;
//                                                         a Boot volta a ser a cena inicial ao sair.
[InitializeOnLoad]
public static class T1HybridSceneryMenu
{
    const string ToggleItem = "Aetherion/Fundo T1/Usar fundo híbrido (V3+V2)";
    const string GroundScene = "Assets/Scenes/Ground_T1.unity";
    const string BootScene = "Assets/Scenes/Boot.unity";
    const string RestoreKey = "Aetherion.T1Hybrid.RestoreBoot";

    static T1HybridSceneryMenu()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RestoreKey, false))
                return;
            SessionState.SetBool(RestoreKey, false);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene);
        };
    }

    [MenuItem(ToggleItem, priority = 40)]
    static void Toggle()
    {
        bool next = !T1HybridScenery.Enabled;
        EditorPrefs.SetBool(T1HybridScenery.EditorPrefKey, next);
        Menu.SetChecked(ToggleItem, next);
    }

    [MenuItem(ToggleItem, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(ToggleItem, T1HybridScenery.Enabled);
        return !EditorApplication.isPlaying;
    }

    [MenuItem("Aetherion/Fundo T1/Jogar Ground_T1 direto", priority = 41)]
    static void PlayGround()
    {
        if (EditorApplication.isPlaying)
            return;
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GroundScene);
        if (scene == null)
            return;
        SessionState.SetBool(RestoreKey, true);
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.isPlaying = true;
    }
}
#endif
