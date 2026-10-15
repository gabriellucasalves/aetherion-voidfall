#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cria e popula automaticamente a Tile Palette 'AetherionPalette'
/// com todos os tiles 16-bit modulares em Assets/Resources/Tilesets.
/// </summary>
[InitializeOnLoad]
public static class AetherionPaletteBuilder
{
    const string PaletteFolder = "Assets/Palettes/AetherionPalette";
    const string TilesFolder = PaletteFolder + "/Tiles";
    const string PalettePrefabPath = PaletteFolder + "/AetherionPalette.prefab";

    [InitializeOnLoadMethod]
    public static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PalettePrefabPath))
            {
                BuildPalette();
            }
        };
    }

    [MenuItem("Aetherion/Gerar Tile Palette (AetherionPalette)")]
    public static void BuildPalette()
    {
        if (!Directory.Exists("Assets/Palettes"))
            Directory.CreateDirectory("Assets/Palettes");
        if (!Directory.Exists(PaletteFolder))
            Directory.CreateDirectory(PaletteFolder);
        if (!Directory.Exists(TilesFolder))
            Directory.CreateDirectory(TilesFolder);

        // Localiza todos os sprites em Assets/Resources/Tilesets
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/Tilesets" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[AetherionPaletteBuilder] Nenhum sprite encontrado em Assets/Resources/Tilesets.");
            return;
        }

        // Monta o GameObject da paleta
        var paletteGo = new GameObject("AetherionPalette");
        var grid = paletteGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var layerGo = new GameObject("Layer1");
        layerGo.transform.SetParent(paletteGo.transform, false);
        var tilemap = layerGo.AddComponent<Tilemap>();
        layerGo.AddComponent<TilemapRenderer>();

        int x = 0;
        int y = 0;
        const int cols = 4;
        int count = 0;

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) continue;

            string tilePath = TilesFolder + "/" + sprite.name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }

            // Pinta o tile no layout da paleta
            tilemap.SetTile(new Vector3Int(x, -y, 0), tile);
            count++;

            x++;
            if (x >= cols)
            {
                x = 0;
                y++;
            }
        }

        // Salva o prefab da Tile Palette
        PrefabUtility.SaveAsPrefabAsset(paletteGo, PalettePrefabPath);
        GameObject.DestroyImmediate(paletteGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#54d6ff><b>[AetherionPalette]</b></color> Sucesso! Tile Palette criada em <i>{PalettePrefabPath}</i> com {count} tiles 16-bit prontos para pintura!");

        // Abre a janela do Tile Palette no Unity e seleciona a nova paleta
        EditorApplication.delayCall += () =>
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            var paletteObj = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePrefabPath);
            if (paletteObj != null)
            {
                Selection.activeObject = paletteObj;
                EditorGUIUtility.PingObject(paletteObj);
            }
        };
    }
}
#endif
