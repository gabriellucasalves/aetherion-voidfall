#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PongProjectSetup
{
    const string ScenePath = "Assets/Scenes/Pong.unity";

    [MenuItem("Aetherion/Abrir Pong legado")]
    static void OpenScene()
    {
        EditorSceneManager.OpenScene(ScenePath);
    }
}
#endif
