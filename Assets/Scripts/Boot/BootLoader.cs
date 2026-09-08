using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == GameScenes.Pong || sceneName == GameScenes.MainMenu)
            return;

        bool untitled = string.IsNullOrEmpty(sceneName) || sceneName == "Untitled";
        if (sceneName != GameScenes.Boot && !untitled)
            return;

#if UNITY_2023_1_OR_NEWER
        if (Object.FindFirstObjectByType<BootLoader>() != null)
            return;
#else
        if (Object.FindObjectOfType<BootLoader>() != null)
            return;
#endif

        var root = new GameObject("BootLoader");
        root.AddComponent<BootLoader>();
    }

    void Start()
    {
        GameManager.EnsureExists();
        SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
    }
}
