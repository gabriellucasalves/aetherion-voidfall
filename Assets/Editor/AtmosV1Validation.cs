#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AtmosV1Validation
{
    [MenuItem("Aetherion/Atmos V1/Abrir início em Play Mode")]
    public static void OpenStartPreview()
    {
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }

    public static void Run()
    {
        SessionState.SetBool("AtmosQA.Enabled", true);
        SessionState.SetBool("AtmosQA.OldAtmos", T1AtmosScenery.Enabled);
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
    public static void BuildWebGL()
    {
        // Deixa o Editor no modo entregue; a build usa o pipeline existente do Feixe.
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, true);
        WebGLBuilder.Build();
    }
    const string TogglePath = "Aetherion/Atmos V1/Usar pack Atmos V1";
    [MenuItem(TogglePath)]
    static void Toggle() => EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, !T1AtmosScenery.Enabled);
    [MenuItem(TogglePath, true)]
    static bool Validate()
    {
        Menu.SetChecked(TogglePath, T1AtmosScenery.Enabled);
        return !EditorApplication.isPlaying;
    }
}
#endif
