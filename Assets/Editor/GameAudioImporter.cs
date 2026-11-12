#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class GameAudioImporter : AssetPostprocessor
{
    public override uint GetVersion() { return 2; }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/Music/") || assetPath.Contains("/Ambience/");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = 0.65f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = !music;
        importer.loadInBackground = false;
    }
}
#endif
