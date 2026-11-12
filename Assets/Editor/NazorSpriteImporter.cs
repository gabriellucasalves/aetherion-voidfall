#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class NazorSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/DevoradorDoVazio/nazor/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
    }
}
#endif
