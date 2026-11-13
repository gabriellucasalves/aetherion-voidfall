#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class EpilogueArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Menu/epilogue/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.sRGBTexture = true;

        var webglSettings = importer.GetPlatformTextureSettings("WebGL");
        if (webglSettings.overridden)
        {
            webglSettings.overridden = false;
            importer.SetPlatformTextureSettings(webglSettings);
        }
    }
}
#endif
