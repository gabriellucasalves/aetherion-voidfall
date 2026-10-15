#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cria, popula e pinta automaticamente todo o cenário 16-bit na cena Ground_T1.
/// </summary>
[InitializeOnLoad]
public static class AetherionPaletteBuilder
{
    const string PaletteFolder = "Assets/Palettes/AetherionPalette";
    const string TilesFolder = PaletteFolder + "/Tiles";
    const string PalettePrefabPath = PaletteFolder + "/AetherionPalette.prefab";
    const string ScenePath = "Assets/Scenes/Ground_T1.unity";

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

    static Tile GetTile(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Tile>($"{TilesFolder}/{name}.asset");
    }

    [MenuItem("Aetherion/▶ Abrir e Testar Fase I no Play Mode")]
    public static void OpenAndPlay()
    {
        PaintCompleteScenery();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Aetherion/Pintar e Montar Cenário Completo na Cena (Ground_T1)")]
    public static void PaintCompleteScenery()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        BuildPalette();

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

        var chaoTilemap = chaoGo.GetComponent<Tilemap>();
        chaoTilemap.ClearAllTiles();

        var tTopL = GetTile("ground_top_left");
        var tTopM = GetTile("ground_top_mid");
        var tTopR = GetTile("ground_top_right");
        var tMid = GetTile("ground_mid_center");
        var tBot = GetTile("ground_bot_center");

        int minX = -36;
        int maxX = 36;
        int groundY = -3;

        for (int x = minX; x <= maxX; x++)
        {
            var topTile = (x == minX && tTopL != null) ? tTopL : ((x == maxX && tTopR != null) ? tTopR : tTopM);
            chaoTilemap.SetTile(new Vector3Int(x, groundY, 0), topTile);

            if (tMid != null)
            {
                chaoTilemap.SetTile(new Vector3Int(x, groundY - 1, 0), tMid);
                chaoTilemap.SetTile(new Vector3Int(x, groundY - 2, 0), tMid);
            }
            if (tBot != null)
            {
                chaoTilemap.SetTile(new Vector3Int(x, groundY - 3, 0), tBot);
            }
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

        var platTilemap = platGo.GetComponent<Tilemap>();
        platTilemap.ClearAllTiles();

        var tRune = GetTile("rune_block_tile");
        var tStair = GetTile("stair_step_tile");
        var tPlat = GetTile("platform_thin_mid");
        var tColCap = GetTile("column_cap_tile");
        var tColShaft = GetTile("column_shaft_tile");
        var tColBase = GetTile("column_base_tile");

        // Escadaria Oeste (5 degraus)
        if (tStair != null)
        {
            for (int i = 0; i < 5; i++)
            {
                platTilemap.SetTile(new Vector3Int(-29 + i, groundY + 1 + i, 0), tStair);
            }
        }

        // Ponte de 4 Blocos Rúnicos de Mármore
        if (tRune != null)
        {
            for (int x = -21; x <= -18; x++)
            {
                platTilemap.SetTile(new Vector3Int(x, groundY + 5, 0), tRune);
            }
        }

        // Colunas de sustentação sob a ponte
        int bridgePillarLeft = -21;
        int bridgePillarRight = -18;
        if (tColCap != null)
        {
            platTilemap.SetTile(new Vector3Int(bridgePillarLeft, groundY + 4, 0), tColCap);
            platTilemap.SetTile(new Vector3Int(bridgePillarRight, groundY + 4, 0), tColCap);
        }
        if (tColShaft != null)
        {
            for (int y = groundY + 1; y <= groundY + 3; y++)
            {
                platTilemap.SetTile(new Vector3Int(bridgePillarLeft, y, 0), tColShaft);
                platTilemap.SetTile(new Vector3Int(bridgePillarRight, y, 0), tColShaft);
            }
        }
        if (tColBase != null)
        {
            platTilemap.SetTile(new Vector3Int(bridgePillarLeft, groundY, 0), tColBase);
            platTilemap.SetTile(new Vector3Int(bridgePillarRight, groundY, 0), tColBase);
        }

        // Ilhas flutuantes (plataformas suspensas)
        if (tPlat != null)
        {
            // Ilha Alta em x = -13
            for (int x = -15; x <= -11; x++)
                platTilemap.SetTile(new Vector3Int(x, groundY + 6, 0), tPlat);

            // Ilha Baixa em x = -6
            for (int x = -8; x <= -4; x++)
                platTilemap.SetTile(new Vector3Int(x, groundY + 4, 0), tPlat);

            // Terraço e Ilha Final Leste
            for (int x = 18; x <= 22; x++)
                platTilemap.SetTile(new Vector3Int(x, groundY + 5, 0), tPlat);
            for (int x = 24; x <= 27; x++)
                platTilemap.SetTile(new Vector3Int(x, groundY + 6, 0), tPlat);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = chaoGo;
        Debug.Log("<color=#54d6ff><b>[Aetherion]</b></color> Cenário 16-bit completamente pintado e salvo na cena Ground_T1!");
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

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/Tilesets" });
        if (guids.Length == 0) return;

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

            tilemap.SetTile(new Vector3Int(x, -y, 0), tile);

            x++;
            if (x >= cols)
            {
                x = 0;
                y++;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(paletteGo, PalettePrefabPath);
        GameObject.DestroyImmediate(paletteGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
