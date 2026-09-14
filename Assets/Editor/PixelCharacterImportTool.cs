#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica settings de import que o projeto realmente usa nos heróis
/// (Point, sem compressão, sem mipmaps, PPU 15, pivot pé 3/64, Readable).
/// Menu: Tools &gt; Pixel Art &gt; Import Tool
/// </summary>
public class PixelCharacterImportTool : EditorWindow
{
    Vector2 _scroll;
    readonly List<Texture2D> _textures = new List<Texture2D>();
    readonly List<string> _previewLines = new List<string>();
    bool _includePivotAndPpu = true;
    bool _forceReadable = true;

    [MenuItem("Tools/Pixel Art/Import Tool")]
    public static void Open()
    {
        var win = GetWindow<PixelCharacterImportTool>("Pixel Import");
        win.minSize = new Vector2(420, 320);
        win.RefreshSelection();
    }

    void OnSelectionChange() => RefreshSelection();

    void OnFocus() => RefreshSelection();

    void RefreshSelection()
    {
        _textures.Clear();
        _previewLines.Clear();

        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                continue;

            _textures.Add(tex);
            _previewLines.Add(BuildDiff(path));
        }

        Repaint();
    }

    static string BuildDiff(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return path + "\n  ❌ não é TextureImporter";

        var sb = new StringBuilder();
        sb.AppendLine(path);

        void Line(string label, string current, string target, bool ok)
        {
            sb.Append("  ");
            sb.Append(ok ? "✅ " : "⚠ ");
            sb.Append(label);
            sb.Append(": ");
            sb.Append(current);
            if (!ok)
            {
                sb.Append(" → ");
                sb.Append(target);
            }
            sb.AppendLine();
        }

        Line("Type", importer.textureType.ToString(), "Sprite",
            importer.textureType == TextureImporterType.Sprite);
        Line("Sprite Mode", importer.spriteImportMode.ToString(), "Single",
            importer.spriteImportMode == SpriteImportMode.Single);
        Line("Filter", importer.filterMode.ToString(), "Point",
            importer.filterMode == FilterMode.Point);
        Line("Mipmaps", importer.mipmapEnabled ? "On" : "Off", "Off",
            !importer.mipmapEnabled);
        Line("Readable", importer.isReadable ? "On" : "Off", "On",
            importer.isReadable);

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        Line("PPU", settings.spritePixelsPerUnit.ToString("0.##"),
            CharacterPixelArtStandards.RuntimePixelsPerUnit.ToString("0.##"),
            Mathf.Approximately(settings.spritePixelsPerUnit, CharacterPixelArtStandards.RuntimePixelsPerUnit));

        var pivot = settings.spriteAlignment == (int)SpriteAlignment.Custom
            ? settings.spritePivot
            : AlignmentToPivot((SpriteAlignment)settings.spriteAlignment);
        var targetPivot = new Vector2(0.5f, CharacterPixelArtStandards.FootPivotY);
        bool pivotOk = Mathf.Abs(pivot.x - targetPivot.x) < 0.001f
                       && Mathf.Abs(pivot.y - targetPivot.y) < 0.001f;
        Line("Pivot", $"({pivot.x:0.####}, {pivot.y:0.####})",
            $"({targetPivot.x:0.####}, {targetPivot.y:0.####})", pivotOk);

        var platform = importer.GetDefaultPlatformTextureSettings();
        Line("Compression", platform.textureCompression.ToString(), "Uncompressed",
            platform.textureCompression == TextureImporterCompression.Uncompressed);

        return sb.ToString();
    }

    static Vector2 AlignmentToPivot(SpriteAlignment alignment)
    {
        switch (alignment)
        {
            case SpriteAlignment.Center: return new Vector2(0.5f, 0.5f);
            case SpriteAlignment.TopLeft: return new Vector2(0f, 1f);
            case SpriteAlignment.TopCenter: return new Vector2(0.5f, 1f);
            case SpriteAlignment.TopRight: return new Vector2(1f, 1f);
            case SpriteAlignment.LeftCenter: return new Vector2(0f, 0.5f);
            case SpriteAlignment.RightCenter: return new Vector2(1f, 0.5f);
            case SpriteAlignment.BottomLeft: return new Vector2(0f, 0f);
            case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
            case SpriteAlignment.BottomRight: return new Vector2(1f, 0f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Pixel Character Import Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Aplica o contrato medido no audit / usado pelos *Visuals:\n" +
            "Sprite · Point · Compression None · Mipmaps Off · Readable On · " +
            $"PPU {CharacterPixelArtStandards.RuntimePixelsPerUnit} · " +
            $"Pivot (0.5, {CharacterPixelArtStandards.FootPivotY:0.####}).\n" +
            "Não reescreve pixels — só TextureImporter.",
            MessageType.Info);

        _includePivotAndPpu = EditorGUILayout.ToggleLeft(
            "Atualizar PPU + Pivot para o contrato de runtime", _includePivotAndPpu);
        _forceReadable = EditorGUILayout.ToggleLeft(
            "Forçar Read/Write (necessário para Sprite.Create nos Visuals)", _forceReadable);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Selecionados: {_textures.Count}", EditorStyles.miniBoldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_previewLines.Count == 0)
            EditorGUILayout.LabelField("Selecione Texture2D / sprites no Project.");
        else
            foreach (var block in _previewLines)
                EditorGUILayout.TextArea(block, GUILayout.MinHeight(80));
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(_textures.Count == 0))
        {
            if (GUILayout.Button("Aplicar settings (com Undo)", GUILayout.Height(32)))
                ApplyAll();
        }

        if (GUILayout.Button("Recarregar seleção"))
            RefreshSelection();
    }

    void ApplyAll()
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Pixel Character Import Settings");

        int changed = 0;
        foreach (var tex in _textures)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            Undo.RecordObject(importer, "Pixel Import " + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            if (_forceReadable)
                importer.isReadable = true;

            if (_includePivotAndPpu)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spritePixelsPerUnit = CharacterPixelArtStandards.RuntimePixelsPerUnit;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, CharacterPixelArtStandards.FootPivotY);
                settings.spriteMode = (int)SpriteImportMode.Single;
                importer.SetTextureSettings(settings);
            }

            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            importer.SetPlatformTextureSettings(platform);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            changed++;
        }

        Undo.CollapseUndoOperations(group);
        AssetDatabase.Refresh();
        RefreshSelection();
        EditorUtility.DisplayDialog(
            "Pixel Import",
            $"Settings aplicados em {changed} textura(s).\nUndo: Edit > Undo.",
            "OK");
    }
}
#endif
