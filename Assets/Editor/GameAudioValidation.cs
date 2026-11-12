#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GameAudioValidation
{
    static double _nextPoll;
    static GameAudioValidation() { EditorApplication.update += PollLocalRequest; }

    // Local, one-shot QA/build requests for the already-open editor.
    static void PollLocalRequest()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Library/GameAudioQA.request"))
        {
            File.Delete("Library/GameAudioQA.request");
            Run();
        }
        else if (File.Exists("Library/GameAudioBuild.request"))
        {
            File.Delete("Library/GameAudioBuild.request");
            WebGLBuilder.Build();
        }
    }

    [MenuItem("Aetherion/Validar áudio das fases e combate")]
    public static void Run()
    {
        Directory.CreateDirectory("/tmp/aetherion-audio-qa");
        SessionState.SetBool("GameAudioQA.Enabled", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
