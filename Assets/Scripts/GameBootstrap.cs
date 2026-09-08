using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (SceneManager.GetActiveScene().name != "Pong")
            return;

#if UNITY_2023_1_OR_NEWER
        if (Object.FindFirstObjectByType<PongGame>() != null)
            return;
#else
        if (Object.FindObjectOfType<PongGame>() != null)
            return;
#endif

        var root = new GameObject("PongGame");
        root.AddComponent<PongGame>();
    }
}
