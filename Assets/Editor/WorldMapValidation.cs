#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorldMapValidation
{
    public static void Run()
    {
        SessionState.SetBool("WorldMapQA.Enabled", true);
        PolishValidation.Size(1920, 1080);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
