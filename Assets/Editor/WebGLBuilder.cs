#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    [MenuItem("Aetherion/📦 Gerar Build WebGL para Vercel")]
    public static void Build()
    {
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.runInBackground = true;

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "webgl",
            target = BuildTarget.WebGL,
            options = BuildOptions.CompressWithLz4HC
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("WebGL build falhou: " + report.summary.result);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        Debug.Log("<color=#54d6ff><b>[WebGL]</b></color> Build concluído com sucesso em webgl/ (" + report.summary.totalSize + " bytes)!");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }
}
#endif
