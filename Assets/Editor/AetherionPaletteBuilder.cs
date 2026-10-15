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

    [MenuItem("Aetherion/Configurar Tilemaps na Cena (Ground_T1)")]
    public static void SetupSceneTilemaps()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Ground_T1.unity")
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        }

        var gridGo = GameObject.Find("Grid_Cenario");
        if (gridGo == null)
        {
            gridGo = new GameObject("Grid_Cenario");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
        }

        // 1. Tilemap do Chão Sólido
        var chaoGo = GameObject.Find("Tilemap_Chao");
        if (chaoGo == null)
        {
            chaoGo = new GameObject("Tilemap_Chao");
            chaoGo.transform.SetParent(gridGo.transform, false);
            chaoGo.AddComponent<Tilemap>();
            var tr = chaoGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -6;

            var col = chaoGo.AddComponent<TilemapCollider2D>();
            col.usedByComposite = true;
            var rb = chaoGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            chaoGo.AddComponent<CompositeCollider2D>();
        }

        // 2. Tilemap das Plataformas One-Way
        var platGo = GameObject.Find("Tilemap_Plataformas");
        if (platGo == null)
        {
            platGo = new GameObject("Tilemap_Plataformas");
            platGo.transform.SetParent(gridGo.transform, false);
            platGo.AddComponent<Tilemap>();
            var tr = platGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = 6;

            var col = platGo.AddComponent<TilemapCollider2D>();
            col.usedByComposite = true;
            var rb = platGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var comp = platGo.AddComponent<CompositeCollider2D>();
            comp.usedByEffector = true;
            var eff = platGo.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 165f;
        }

        Selection.activeGameObject = chaoGo;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("<color=#54d6ff><b>[Aetherion]</b></color> Grid e Tilemaps configurados com colisores em Ground_T1!");
    }
}
#endif
