using System.Collections.Generic;
using UnityEngine;

// Integra o pack 16-bit em PPU 32 Point. Não muda o contrato PPU 16 dos personagens.
public static class T1AtmosScenery
{
    public const string Folder = "T1_Hybrid_Atmos/";
    public const string FolderV3 = "T1_Hybrid_Atmos/v3/";
    public const float GroundBelowCenter = 5.5f;
    public const string EditorPrefKey = "Aetherion.AtmosV1.Enabled";
    public const bool DefaultEnabled = true;
    public const bool AllowLegacyFallback = false;
    public const float Ppu = 32f;
    public const float PlayerJumpSpeed = 15.6f;
    public static bool Built { get; private set; }
    public static bool HasV3Layers { get; private set; }
    public static bool NoProceduralSprites { get; private set; }
    static GameObject _root;

    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EditorPrefKey, DefaultEnabled);
#else
            var url = Application.absoluteURL ?? "";
            if (url.Contains("fundo=classico") || url.Contains("fundo=hibrido")) return false;
            if (url.Contains("fundo=atmos")) return true;
            return DefaultEnabled;
#endif
        }
    }
    public static void Reset()
    {
        Built = false;
        HasV3Layers = false;
        NoProceduralSprites = false;
        _root = null;
        AtmosFrontLevel.Reset();
    }
    public static void ConfigureStage(StageData stage)
    {
        stage.GroundTop = AtmosV1Parallax.Snap(stage.GroundTop);
    }
    public static Sprite LoadV3Layer(string name)
    {
        var texture = Resources.Load<Texture2D>(FolderV3 + "camadas/" + name);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        var pivot = new Vector2(0.5f, 60f / 432f);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            pivot, Ppu, 0, SpriteMeshType.FullRect);
    }
    public static bool Build(StageData stage)
    {
        Reset();
        var layerNames = new[]
        {
            "P1_ceu_dia_native",
            "P2_castelo_montanhas_longe_native",
            "P3_montanhas_bosque_native",
            "P4_arvores_ruinas_native",
            "P4b_nevoa_clara_native",
            "P6_folhagem_frente_native"
        };
        float[] factors = { 0.08f, 0.22f, 0.40f, 0.62f, 0.62f, 1.25f };
        int[] orders = { -50, -40, -30, -20, -15, 20 };

        var sprites = new Sprite[layerNames.Length];
        for (int i = 0; i < layerNames.Length; i++)
        {
            sprites[i] = LoadV3Layer(layerNames[i]);
            if (sprites[i] == null)
            {
                Debug.LogError("T1 Atmos v3: camada ausente: " + layerNames[i]);
                return false;
            }
        }
        HasV3Layers = true;

        var root = new GameObject("Cenario_AtmosV1_v3");
        root.AddComponent<AtmosV1Root>();
        var reference = new Vector2(-stage.HalfWidth + 12f, stage.GroundTop);

        for (int i = 0; i < factors.Length; i++)
        {
            var group = new GameObject(layerNames[i]);
            group.transform.SetParent(root.transform, false);
            for (int copy = -3; copy <= 4; copy++)
            {
                var tile = new GameObject("Copia_" + copy);
                tile.transform.SetParent(group.transform, false);
                tile.transform.localPosition = new Vector3(copy * 24f, 0f, 0f);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = orders[i];
            }
            group.AddComponent<AtmosV1Parallax>().Setup(factors[i], reference, i == 0);
        }

        AtmosFrontLevel.Build(stage, root.transform);

        bool allClean = true;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite != null && (sr.sprite.texture == Texture2D.whiteTexture || sr.sprite.rect.width <= 2))
            {
                allClean = false;
                break;
            }
        }
        NoProceduralSprites = allClean;

        _root = root;
        Built = true;
        return true;
    }
    public static void ClearIfOwner(GameObject owner)
    {
        if (_root == owner) Reset();
    }
}

[DefaultExecutionOrder(100)]
public class AtmosSingleCitadel : MonoBehaviour
{
    float _factor;
    Vector2 _reference;
    public void Setup(float factor, Vector2 reference)
    {
        _factor = factor;
        _reference = reference;
        Apply();
    }
    void LateUpdate() => Apply();
    void Apply()
    {
        if (Camera.main == null) return;
        var cam = Camera.main.transform.position;
        float x = _reference.x + (cam.x - _reference.x) * (1f - _factor);
        float y = _reference.y + (cam.y - _reference.y) * (1f - _factor);
        transform.position = AtmosV1Parallax.Snap(new Vector3(x, y, 0f));
    }
}

// =============================================================================
// FASE 2 — "O PALÁCIO EM QUEDA" (T2_Palace_Atmos)
// Mantém exatamente o mesmo pipeline PPU 32 Point, câmera e paralaxe da Fase 1.
// =============================================================================
public static class T2PalaceScenery
{
    public const string Folder = "T2_Palace_Atmos/";
    public const string FolderV3 = "T2_Palace_Atmos/v3/";
    public const float GroundBelowCenter = 5.5f;
    public const float Ppu = 32f;
    public const bool Enabled = true;
    public static bool Built { get; private set; }
    public static bool HasV3Layers { get; private set; }
    public static bool NoProceduralSprites { get; private set; }
    static GameObject _root;

    public static void Reset()
    {
        Built = false;
        HasV3Layers = false;
        NoProceduralSprites = false;
        _root = null;
        T2PalaceFrontLevel.Reset();
    }

    public static void ClearIfOwner(GameObject owner)
    {
        if (_root == owner) Reset();
    }

    public static void ConfigureStage(StageData stage)
    {
        stage.GroundTop = AtmosV1Parallax.Snap(stage.GroundTop);
    }

    public static Sprite LoadV3Layer(string name)
    {
        var texture = Resources.Load<Texture2D>(FolderV3 + "camadas/" + name);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        var pivot = new Vector2(0.5f, 60f / 432f);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            pivot, Ppu, 0, SpriteMeshType.FullRect);
    }

    public static bool Build(StageData stage)
    {
        Reset();
        var layerNames = new[]
        {
            "P1_ceu_caos_native",
            "P2_palacio_desabando_native",
            "P2b_nevoa_distante_native",
            "P3_salao_ruinas_native",
            "P3b_nevoa_media_native",
            "P4_estatuas_destruidas_native",
            "P4b_nevoa_fumaca_native",
            "P6_silhuetas_frente_native"
        };
        float[] factors = { 0.08f, 0.22f, 0.30f, 0.40f, 0.50f, 0.62f, 0.62f, 1.25f };
        int[] orders = { -50, -40, -35, -30, -25, -20, -15, 20 };

        var sprites = new Sprite[layerNames.Length];
        for (int i = 0; i < layerNames.Length; i++)
        {
            sprites[i] = LoadV3Layer(layerNames[i]);
            if (sprites[i] == null)
            {
                Debug.LogError("T2 Palace v3: camada ausente: " + layerNames[i]);
                return false;
            }
        }
        HasV3Layers = true;

        var root = new GameObject("Cenario_PalacioT2_v3");
        root.AddComponent<AtmosV1Root>();
        var reference = new Vector2(-stage.HalfWidth + 12f, stage.GroundTop);

        for (int i = 0; i < factors.Length; i++)
        {
            var group = new GameObject(layerNames[i]);
            group.transform.SetParent(root.transform, false);
            for (int copy = -3; copy <= 4; copy++)
            {
                var tile = new GameObject("Copia_" + copy);
                tile.transform.SetParent(group.transform, false);
                tile.transform.localPosition = new Vector3(copy * 24f, 0f, 0f);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = orders[i];
            }
            group.AddComponent<AtmosV1Parallax>().Setup(factors[i], reference, i == 0);
        }

        // Estátua Colossal que desaba
        var statueGo = new GameObject("EstatuaColossal_QuedaEvento");
        statueGo.transform.SetParent(root.transform, false);
        statueGo.AddComponent<PalaceCollapsingStatue>().Setup(new Vector3(4.5f, stage.GroundTop, 0f));

        // Chuva cíclica de meteoros
        var meteorGo = new GameObject("ChuvaDeMeteoros_T2");
        meteorGo.transform.SetParent(root.transform, false);
        meteorGo.AddComponent<PalaceMeteorShower>().Setup(stage);

        T2PalaceFrontLevel.Build(stage, root.transform);

        bool allClean = true;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite != null && (sr.sprite.texture == Texture2D.whiteTexture || sr.sprite.rect.width <= 2))
            {
                allClean = false;
                break;
            }
        }
        NoProceduralSprites = allClean;

        _root = root;
        Built = true;
        return true;
    }
}

public static class T2PalaceFrontLevel
{
    public const string FolderV3 = "T2_Palace_Atmos/v3/";
    public const float SurfaceRow = 392f;
    public const float GroundSurfacePx = 8f;
    const float Thickness = 0.1875f;
    public static bool Built { get; private set; }

    static readonly List<Vector2> _gaps = new List<Vector2>();
    static readonly List<AtmosFrontLevel.Platform> _platforms = new List<AtmosFrontLevel.Platform>();

    public static IReadOnlyList<Vector2> Gaps => _gaps;
    public static IReadOnlyList<AtmosFrontLevel.Platform> Platforms => _platforms;

    public static void Reset()
    {
        Built = false;
        _gaps.Clear();
        _platforms.Clear();
    }

    public static float X(StageData stage, float pixelX) => -stage.HalfWidth + pixelX / 32f;
    public static float Y(StageData stage, float pixelY) => stage.GroundTop + (SurfaceRow - pixelY) / 32f;

    static readonly (string sprite, float centerX, float heightAboveGround)[] ExtraPlatforms =
    {
        ("plataforma_palacio_128_a", -9.0f,  2.2f),
        ("plataforma_palacio_96_b",  -4.2f,  3.8f),
        ("plataforma_palacio_160_a",  1.5f,  2.4f),
        ("plataforma_palacio_128_b",  7.2f,  4.0f),
        ("plataforma_palacio_96_a",  12.5f,  2.2f),
        ("plataforma_palacio_160_b", 18.0f,  3.6f),
        ("plataforma_palacio_128_a", 23.5f,  2.3f),
        ("plataforma_palacio_96_b",  28.0f,  3.8f),
    };

    static Sprite LoadTile(string name, Vector2 pivot, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var tex = Resources.Load<Texture2D>(FolderV3 + "tiles/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = wrap;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
    }

    static Sprite LoadProp(string name)
    {
        var tex = Resources.Load<Texture2D>(FolderV3 + "props/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
    }

    static Sprite LoadPlatform(string name, Vector2 pivot)
    {
        var tex = Resources.Load<Texture2D>(FolderV3 + "plataformas/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
    }

    public static void Build(StageData stage, Transform parent)
    {
        Reset();
        var root = new GameObject("P5_FrenteJogavel_T2");
        root.transform.SetParent(parent, false);

        var slick = new PhysicsMaterial2D("PalacioSemAtrito") { friction = 0f, bounciness = 0f };

        // Colisores do chão (pixel 0..472 e 544..HalfWidth)
        Ground("Ground_Palacio_L", X(stage, 0f), X(stage, 472f), stage.GroundTop, slick, root.transform);
        Ground("Ground_Palacio_R", X(stage, 544f), stage.HalfWidth, stage.GroundTop, slick, root.transform);

        var gap = new Vector2(X(stage, 472f), X(stage, 544f));
        _gaps.Add(gap);

        // Visual do chão por trechos: Palácio v3 + Subsolo v3 + Bordas v3
        BuildGroundVisual(stage, X(stage, 0f), X(stage, 472f), abyssLeft: false, abyssRight: true, root.transform);
        BuildGroundVisual(stage, X(stage, 544f), stage.HalfWidth, abyssLeft: true, abyssRight: false, root.transform);

        // Abismo escuro com abismo_profundo_64x96 v3 (sem retângulos pretos procedurais)
        BuildAbyss(stage, gap.x, gap.y, root.transform);

        // Plataformas flutuantes com arte v3 e colisores conforme o MANIFEST
        BuildPlatformV3("plataforma_palacio_96_a",  X(stage, 96),  Y(stage, 326), "L0", stage, root.transform, false);
        BuildPlatformV3("plataforma_palacio_96_b",  X(stage, 240), Y(stage, 294), "A",  stage, root.transform, true);
        BuildPlatformV3("plataforma_palacio_128_a", X(stage, 376), Y(stage, 258), "M",  stage, root.transform, false);
        BuildPlatformV3("plataforma_palacio_128_b", X(stage, 502), Y(stage, 290), "B",  stage, root.transform, true);
        BuildPlatformV3("plataforma_palacio_96_a",  X(stage, 684), Y(stage, 250), "C",  stage, root.transform, true);

        BuildExtraPlatforms(stage, root.transform);
        BuildProps(stage, root.transform);
        BuildPalaceHazards(stage, root.transform);
        BuildAmbientFx(stage, root.transform);

        var fall = new GameObject("Palacio_ZonaDeQueda");
        fall.transform.SetParent(root.transform, false);
        fall.transform.position = new Vector3(0f, stage.GroundTop - 4.5f, 0f);
        var trigger = fall.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(stage.HalfWidth * 2f + 16f, 1f);
        fall.AddComponent<T1FallZone>();

        Built = true;
    }

    static void Ground(string name, float left, float right, float top, PhysicsMaterial2D material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 2f, 0f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(right - left, 4f);
        collider.sharedMaterial = material;
    }

    static void BuildAbyss(StageData stage, float x0, float x1, Transform parent)
    {
        var abyss = new GameObject("Abismo_Profundo_T2_" + Mathf.RoundToInt(x0 * 10f));
        abyss.transform.SetParent(parent, false);
        abyss.transform.position = new Vector3(x0, stage.GroundTop - 0.125f, 0f);
        var sr = abyss.AddComponent<SpriteRenderer>();
        var spr = LoadTile("abismo_profundo_64x96", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (spr != null)
        {
            sr.sprite = spr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(x1 - x0, 8.0f);
        }
        sr.sortingOrder = -10;

        var pSpr = LoadTile("pedra_caindo_" + Random.Range(0, 4), new Vector2(0.5f, 0.5f));
        if (pSpr != null)
        {
            var pGo = new GameObject("PedraCaindo_Decor");
            pGo.transform.SetParent(abyss.transform, false);
            pGo.transform.position = new Vector3((x0 + x1) * 0.5f, stage.GroundTop - 2.0f, 0f);
            var pSr = pGo.AddComponent<SpriteRenderer>();
            pSr.sprite = pSpr;
            pSr.sortingOrder = -9;
        }
    }

    static void BuildGroundVisual(StageData stage, float left, float right, bool abyssLeft, bool abyssRight, Transform parent)
    {
        var root = new GameObject("GroundVisual_T2_" + Mathf.RoundToInt(left * 10f));
        root.transform.SetParent(parent, false);

        float curLeft = left;
        float curRight = right;

        if (abyssLeft)
        {
            var edgeSpr = LoadTile("chao_borda_abismo_esq", new Vector2(0f, 1f));
            if (edgeSpr != null)
            {
                var edgeGo = new GameObject("BordaAbismoEsq");
                edgeGo.transform.SetParent(root.transform, false);
                edgeGo.transform.position = new Vector3(curLeft - (4f / 32f), stage.GroundTop, 0f);
                var sr = edgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = edgeSpr;
                sr.sortingOrder = 1;
            }
        }

        if (abyssRight)
        {
            var edgeSpr = LoadTile("chao_borda_abismo_dir", new Vector2(0f, 1f));
            if (edgeSpr != null)
            {
                var edgeGo = new GameObject("BordaAbismoDir");
                edgeGo.transform.SetParent(root.transform, false);
                edgeGo.transform.position = new Vector3(curRight - (48f / 32f) + (4f / 32f), stage.GroundTop, 0f);
                var sr = edgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = edgeSpr;
                sr.sortingOrder = 1;
            }
        }

        var tileSpr = LoadTile("chao_palacio_128", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (tileSpr != null && curRight > curLeft)
        {
            var tileGo = new GameObject("ChaoPalacioTiled");
            tileGo.transform.SetParent(root.transform, false);
            tileGo.transform.position = new Vector3(curLeft, stage.GroundTop, 0f);
            var sr = tileGo.AddComponent<SpriteRenderer>();
            sr.sprite = tileSpr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(curRight - curLeft, 75f / 32f);
            sr.sortingOrder = 0;
        }

        var subsoloSpr = LoadTile("chao_subsolo_128x64", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (subsoloSpr != null && right > left)
        {
            var subGo = new GameObject("ChaoSubsoloPalacio");
            subGo.transform.SetParent(root.transform, false);
            subGo.transform.position = new Vector3(left, stage.GroundTop - (75f / 32f), 0f);
            var srSub = subGo.AddComponent<SpriteRenderer>();
            srSub.sprite = subsoloSpr;
            srSub.drawMode = SpriteDrawMode.Tiled;
            srSub.size = new Vector2(right - left, 7.0f);
            srSub.sortingOrder = -1;
        }
    }

    static void BuildPlatformV3(string spriteName, float centerX, float topY, string regName, StageData stage, Transform parent, bool register)
    {
        int h = 54; int yCol = 1; int colPx = 92;
        if (spriteName.Contains("96_a"))      { h = 54; yCol = 1; colPx = 92; }
        else if (spriteName.Contains("96_b")) { h = 56; yCol = 2; colPx = 92; }
        else if (spriteName.Contains("128_a")){ h = 73; yCol = 2; colPx = 124; }
        else if (spriteName.Contains("128_b")){ h = 75; yCol = 2; colPx = 124; }
        else if (spriteName.Contains("160_a")){ h = 95; yCol = 3; colPx = 156; }
        else if (spriteName.Contains("160_b")){ h = 95; yCol = 3; colPx = 156; }

        float pivotY = (h - yCol) / (float)h;
        var spr = LoadPlatform(spriteName, new Vector2(0.5f, pivotY));
        if (spr == null) return;

        float colW = colPx / 32f;
        float colH = 6f / 32f;

        var go = new GameObject("Plat_T2_" + regName + "_" + spriteName);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, topY, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 1;

        var colGo = new GameObject("Colisor");
        colGo.transform.SetParent(go.transform, false);
        colGo.transform.position = new Vector3(centerX, topY - colH * 0.5f, 0f);
        var collider = colGo.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(colW, colH);
        collider.usedByEffector = true;
        var effector = colGo.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;

        if (register)
            _platforms.Add(new AtmosFrontLevel.Platform(regName, centerX - colW * 0.5f, centerX + colW * 0.5f, topY));
    }

    static void BuildExtraPlatforms(StageData stage, Transform parent)
    {
        for (int i = 0; i < ExtraPlatforms.Length; i++)
        {
            var item = ExtraPlatforms[i];
            float top = stage.GroundTop + item.heightAboveGround;
            BuildPlatformV3(item.sprite, item.centerX, top, "Extra_" + i, stage, parent, register: false);
        }
    }

    static void BuildProps(StageData stage, Transform parent)
    {
        var propsRoot = new GameObject("Props_PalacioT2_v3");
        propsRoot.transform.SetParent(parent, false);

        float baseGround = stage.GroundTop - (3f / 32f);

        // Segmento 1: -36.0f a -21.25f
        BuildSingleProp("balaustrada",            -34.5f, baseGround, propsRoot.transform);
        BuildSingleProp("braseiro",               -30.0f, baseGround, propsRoot.transform);
        BuildSingleProp("entulho",                -26.0f, baseGround, propsRoot.transform);
        BuildSingleProp("candelabro",             -23.0f, baseGround, propsRoot.transform);

        // Segmento 2: -19.0f a +36.0f
        BuildSingleProp("estandarte_caido",       -17.0f, baseGround, propsRoot.transform);
        BuildSingleProp("urna_quebrada",          -12.0f, baseGround, propsRoot.transform);
        BuildSingleProp("estatua_cabeca_coroada",  -5.0f, baseGround, propsRoot.transform);
        BuildSingleProp("cristais_ciano",           1.0f, baseGround, propsRoot.transform);
        BuildSingleProp("braseiro",                 8.0f, baseGround, propsRoot.transform);
        BuildSingleProp("trono_tombado",           15.0f, baseGround, propsRoot.transform);
        BuildSingleProp("tesouro",                 22.0f, baseGround, propsRoot.transform);
        BuildSingleProp("balaustrada",             28.0f, baseGround, propsRoot.transform);
        BuildSingleProp("entulho",                 33.0f, baseGround, propsRoot.transform);
    }

    static void BuildSingleProp(string name, float x, float baseY, Transform parent)
    {
        var spr = LoadProp(name);
        if (spr == null) return;
        var go = new GameObject("Prop_T2_" + name + "_" + Mathf.RoundToInt(x * 10f));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, baseY, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = -5;
    }

    static void BuildAmbientFx(StageData stage, Transform parent)
    {
        var fxRoot = new GameObject("FX_Ambiente_T2");
        fxRoot.transform.SetParent(parent, false);

        var meteoroTex = Resources.Load<Texture2D>(FolderV3 + "fx/fx_meteoro_queda_8f_48x48");
        var impactoTex = Resources.Load<Texture2D>(FolderV3 + "fx/fx_impacto_explosao_6f_64x64");
        var fumacaTex  = Resources.Load<Texture2D>(FolderV3 + "fx/fx_fumaca_6f_32x32");

        Sprite[] meteoroFrames = null;
        Sprite[] impactoFrames = null;
        Sprite[] fumacaFrames  = null;

        if (meteoroTex != null)
            meteoroFrames = AmbientFxFactory.Slice(meteoroTex, 48, 48, new Vector2(0.27f, 0.27f));
        if (impactoTex != null)
            impactoFrames = AmbientFxFactory.Slice(impactoTex, 64, 64, new Vector2(0.5f, 0.06f));
        if (fumacaTex != null)
            fumacaFrames = AmbientFxFactory.Slice(fumacaTex, 32, 32, new Vector2(0.5f, 0f));

        if (meteoroFrames != null)
        {
            for (int i = 0; i < 2; i++)
            {
                var metGo = new GameObject("MeteoroFundo_" + i);
                metGo.transform.SetParent(fxRoot.transform, false);
                var sr = metGo.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -32;
                metGo.AddComponent<AmbientSpriteAnimator>().Setup(meteoroFrames, 14f, loop: true);
                metGo.AddComponent<AmbientMeteorVisual>().Setup(
                    meteoroFrames, impactoFrames, fumacaFrames, stage.GroundTop, fxRoot.transform);
            }
        }

        var brasasTex = Resources.Load<Texture2D>(FolderV3 + "fx/fx_brasas_4f_32x32");
        if (brasasTex != null)
        {
            var brasasFrames = AmbientFxFactory.Slice(brasasTex, 32, 32, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 8; i++)
            {
                var bGo = new GameObject("Brasa_" + i);
                bGo.transform.SetParent(fxRoot.transform, false);
                float bx = -30f + i * 8.5f;
                float by = stage.GroundTop + 0.5f + (i % 3) * 1.0f;
                bGo.transform.position = new Vector3(bx, by, 0f);
                var sr = bGo.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 15;
                bGo.AddComponent<AmbientSpriteAnimator>().Setup(brasasFrames, 8f, loop: true);
                bGo.AddComponent<AmbientFloatingDrift>().Setup(
                    new Vector3(0.2f, 0.8f, 0f),
                    -36f, 36f, stage.GroundTop, stage.GroundTop + 5.5f);
            }
        }
    }

    static void BuildPalaceHazards(StageData stage, Transform parent)
    {
        var hazardsRoot = new GameObject("Perigos_Palacio_T2");
        hazardsRoot.transform.SetParent(parent, false);

        // 1. Fendas com Fogo e Brasa no chão (dano contínuo leve com cooldown)
        float[] fireXs = { -27.9f, -14.5f, -2.5f, 10.5f, 21.5f };
        foreach (float fx in fireXs)
            PalaceFireHazard.Spawn(hazardsRoot.transform, new Vector3(fx, stage.GroundTop, 0f), 1.85f);

        // 2. Espinhos de Ouro e Ferro do Palácio (alguns fixos, outros em ciclo com aviso antes de subir)
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(-22.2f, stage.GroundTop, 0f), 1.4f, cycling: true);
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(-6.2f,  stage.GroundTop, 0f), 1.4f, cycling: false);
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(5.2f,   stage.GroundTop, 0f), 1.5f, cycling: true);
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(15.8f,  stage.GroundTop, 0f), 1.4f, cycling: true);

        // 3. Escombros caindo do teto com sombra/marca de aviso no chão antes da queda
        float[] debrisXs = { -17.5f, 0.5f, 13.0f, 25.0f };
        for (int i = 0; i < debrisXs.Length; i++)
            PalaceFallingDebris.Spawn(hazardsRoot.transform, new Vector3(debrisXs[i], stage.GroundTop, 0f), i * 1.1f);
        Built = true;
    }
}

/// <summary>
/// Perigo de chão 1 da Fase 2: Fenda com Fogo e Brasa no chão (labaredas 16-bit animadas + dano periódico leve).
/// </summary>
public class PalaceFireHazard : MonoBehaviour
{
    static Sprite[] _flameFrames;
    SpriteRenderer _sr;
    float _clock;
    float _nextDamageTime;
    float _sparkClock;

    public static PalaceFireHazard Spawn(Transform parent, Vector3 position, float width)
    {
        var go = new GameObject("FendaFogoBrasa");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var hazard = go.AddComponent<PalaceFireHazard>();
        var frames = GetFlameFrames();
        hazard._sr = go.AddComponent<SpriteRenderer>();
        hazard._sr.sprite = frames[0];
        hazard._sr.sortingOrder = 5;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 0.55f);
        col.offset = new Vector2(0f, 0.25f);
        return hazard;
    }

    void Update()
    {
        _clock += Time.deltaTime;
        var frames = GetFlameFrames();
        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[Mathf.FloorToInt(_clock * 8f) % frames.Length];

        _sparkClock += Time.deltaTime;
        if (_sparkClock >= 0.45f)
        {
            _sparkClock = 0f;
            PixelBurst.Spawn(transform.position + new Vector3(Random.Range(-0.7f, 0.7f), 0.25f, 0f),
                new Color(1f, 0.72f, 0.25f), 2);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < _nextDamageTime) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        var health = player.GetComponent<HealthSystem>();
        if (health == null || health.IsDead) return;

        _nextDamageTime = Time.time + 0.65f;
        EnemyController.DamagePlayer(player.transform, transform.position, 9f);
        PixelBurst.Spawn(player.transform.position + Vector3.up * 0.3f, new Color(1f, 0.58f, 0.22f), 5);
    }

    static Sprite[] GetFlameFrames()
    {
        if (_flameFrames != null) return _flameFrames;
        const int w = 32, h = 16;
        _flameFrames = new Sprite[3];
        Color32 outline = new Color32(18, 16, 32, 255);
        Color32 white   = new Color32(255, 253, 240, 255);
        Color32 yellow  = new Color32(255, 238, 132, 255);
        Color32 orange  = new Color32(245, 148, 62, 255);
        Color32 red     = new Color32(196, 68, 56, 255);
        Color32 deep    = new Color32(34, 26, 56, 255);

        for (int f = 0; f < 3; f++)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            // Base de brasa viva (y = 0..3)
            for (int y = 0; y < 4; y++)
            for (int x = 1; x < w - 1; x++)
            {
                if (y == 0 || x == 1 || x == w - 2) px[y * w + x] = outline;
                else if (y == 1) px[y * w + x] = ((x + f) & 1) == 0 ? white : yellow;
                else px[y * w + x] = y == 2 ? orange : deep;
            }
            // Labaredas esculpidas (y = 3..14)
            for (int i = 0; i < 5; i++)
            {
                int cx = 4 + i * 6;
                int th = 8 + ((i + f) % 4);
                for (int dy = 0; dy < th; dy++)
                {
                    int y = 3 + dy;
                    if (y >= h - 1) break;
                    int sway = Mathf.RoundToInt(Mathf.Sin(dy * 0.6f + f * 2.1f + i) * 1.1f);
                    int hw = Mathf.Max(0, Mathf.RoundToInt((1f - dy / (float)th) * 2.4f));
                    for (int dx = -hw - 1; dx <= hw + 1; dx++)
                    {
                        int xx = Mathf.Clamp(cx + sway + dx, 0, w - 1);
                        if (dx == -hw - 1 || dx == hw + 1 || dy == th - 1)
                            px[y * w + xx] = outline;
                        else if (dy <= 1 && Mathf.Abs(dx) <= 1)
                            px[y * w + xx] = white;
                        else if (dy < th * 0.45f)
                            px[y * w + xx] = yellow;
                        else if (dy < th * 0.78f)
                            px[y * w + xx] = orange;
                        else
                            px[y * w + xx] = red;
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            _flameFrames[f] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 2f / 16f), 16f);
        }
        return _flameFrames;
    }
}

/// <summary>
/// Perigo de chão 2 da Fase 2: Espinhos de Ouro e Ferro do Palácio (fixos ou em ciclo com aviso antes de subir).
/// </summary>
public class PalaceSpikeHazard : MonoBehaviour
{
    static Sprite _warnSprite;
    static Sprite _extSprite;

    bool _cycling;
    float _clock;
    float _nextHitTime;
    SpriteRenderer _sr;

    public bool IsExtended { get; private set; } = true;

    public static PalaceSpikeHazard Spawn(Transform parent, Vector3 position, float width, bool cycling)
    {
        var go = new GameObject(cycling ? "EspinhosPalacio_Ciclo" : "EspinhosPalacio_Fixo");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var hazard = go.AddComponent<PalaceSpikeHazard>();
        hazard._cycling = cycling;
        hazard.IsExtended = !cycling;

        EnsureSprites();
        hazard._sr = go.AddComponent<SpriteRenderer>();
        hazard._sr.sprite = hazard.IsExtended ? _extSprite : _warnSprite;
        hazard._sr.sortingOrder = 5;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 0.65f);
        col.offset = new Vector2(0f, 0.32f);
        return hazard;
    }

    void Update()
    {
        if (!_cycling) return;
        _clock += Time.deltaTime;
        // Ciclo de 2.8s: 0.9s recolhido/aviso faiscante -> 1.9s ativo estendido
        float phase = _clock % 2.8f;
        bool ext = phase >= 0.9f;
        if (ext != IsExtended)
        {
            IsExtended = ext;
            if (_sr != null) _sr.sprite = IsExtended ? _extSprite : _warnSprite;
            if (IsExtended)
                PixelBurst.Spawn(transform.position + Vector3.up * 0.25f, new Color(1f, 0.85f, 0.45f), 4);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsExtended || Time.time < _nextHitTime) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        var health = player.GetComponent<HealthSystem>();
        if (health == null || health.IsDead) return;

        _nextHitTime = Time.time + 0.75f;
        EnemyController.DamagePlayer(player.transform, transform.position, 12f);
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            float pushX = player.transform.position.x >= transform.position.x ? 3.5f : -3.5f;
            body.linearVelocity = new Vector2(pushX, 7.5f);
        }
        PixelBurst.Spawn(player.transform.position + Vector3.up * 0.25f, new Color(1f, 0.88f, 0.45f), 6);
    }

    static void EnsureSprites()
    {
        if (_extSprite != null && _warnSprite != null) return;
        _warnSprite = BuildSpikeSprite(extended: false);
        _extSprite = BuildSpikeSprite(extended: true);
    }

    static Sprite BuildSpikeSprite(bool extended)
    {
        const int w = 26, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        Color32 outline = new Color32(18, 16, 32, 255);
        Color32 goldB   = new Color32(255, 244, 156, 255);
        Color32 goldL   = new Color32(248, 206, 92, 255);
        Color32 goldM   = new Color32(216, 156, 54, 255);
        Color32 ironL   = new Color32(164, 168, 192, 255);
        Color32 ironM   = new Color32(108, 110, 138, 255);
        Color32 ironD   = new Color32(62, 62, 86, 255);
        Color32 orange  = new Color32(245, 148, 62, 255);

        for (int y = 0; y < 3; y++)
        for (int x = 1; x < w - 1; x++)
        {
            if (y == 0 || x == 1 || x == w - 2) px[y * w + x] = outline;
            else px[y * w + x] = y == 2 ? goldM : ironD;
        }

        int sh = extended ? 11 : 4;
        for (int sx = 4; sx <= w - 5; sx += 6)
        {
            for (int dy = 0; dy < sh; dy++)
            {
                int y = 3 + dy;
                if (y >= h) break;
                int hw = Mathf.Max(0, (sh - 1 - dy) / (extended ? 3 : 2));
                for (int dx = -hw - 1; dx <= hw + 1; dx++)
                {
                    int xx = Mathf.Clamp(sx + dx, 0, w - 1);
                    if (dx == -hw - 1 || dx == hw + 1 || dy == sh - 1)
                        px[y * w + xx] = outline;
                    else if (!extended)
                        px[y * w + xx] = dy >= 2 ? goldB : orange;
                    else if (dx < 0)
                        px[y * w + xx] = goldB;
                    else if (dx == 0)
                        px[y * w + xx] = dy < 5 ? goldL : ironL;
                    else
                        px[y * w + xx] = ironM;
                }
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f / 16f), 16f);
    }
}

/// <summary>
/// Perigo de chão 3 da Fase 2: Escombro caindo do teto com sombra/marca de aviso no chão antes da queda.
/// </summary>
public class PalaceFallingDebris : MonoBehaviour
{
    static Sprite _warningSprite;
    static Sprite _blockSprite;

    Vector3 _groundPos;
    float _timer;
    int _state; // 0 = cooldown, 1 = aviso no chão (0.85s), 2 = bloco caindo
    float _fallY;
    SpriteRenderer _warningSr;
    SpriteRenderer _blockSr;

    public static PalaceFallingDebris Spawn(Transform parent, Vector3 groundPos, float initialDelay)
    {
        var go = new GameObject("EscombroCaindo_T2");
        go.transform.SetParent(parent, false);
        go.transform.position = groundPos;

        var d = go.AddComponent<PalaceFallingDebris>();
        d._groundPos = groundPos;
        d._timer = 1.6f + initialDelay;
        d.BuildVisuals();
        return d;
    }

    void BuildVisuals()
    {
        EnsureSprites();
        var warnGo = new GameObject("MarcaAvisoChao");
        warnGo.transform.SetParent(transform, false);
        warnGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        _warningSr = warnGo.AddComponent<SpriteRenderer>();
        _warningSr.sprite = _warningSprite;
        _warningSr.sortingOrder = 6;
        _warningSr.enabled = false;

        var blockGo = new GameObject("BlocoMarmore");
        blockGo.transform.SetParent(transform, false);
        _blockSr = blockGo.AddComponent<SpriteRenderer>();
        _blockSr.sprite = _blockSprite;
        _blockSr.sortingOrder = 14;
        _blockSr.enabled = false;
    }

    void Update()
    {
        if (_state == 0)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _state = 1;
                _timer = 0.85f;
                _warningSr.enabled = true;
            }
        }
        else if (_state == 1)
        {
            _timer -= Time.deltaTime;
            bool blink = Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
            _warningSr.color = blink ? Color.white : new Color(1f, 0.55f, 0.25f, 0.85f);
            if (_timer <= 0f)
            {
                _state = 2;
                _fallY = 6.2f;
                _blockSr.enabled = true;
                _blockSr.transform.localPosition = new Vector3(0f, _fallY, 0f);
            }
        }
        else if (_state == 2)
        {
            _fallY -= 14.5f * Time.deltaTime;
            _blockSr.transform.localPosition = new Vector3(0f, Mathf.Max(0.2f, _fallY), 0f);
            if (_fallY <= 0.25f)
            {
                // Impacto no piso: partículas de mármore e ouro + dano se o herói estiver embaixo
                _warningSr.enabled = false;
                _blockSr.enabled = false;
                _state = 0;
                _timer = 3.2f;

                PixelBurst.Spawn(_groundPos + Vector3.up * 0.2f, new Color(0.92f, 0.88f, 0.96f), 6);
                PixelBurst.Spawn(_groundPos + Vector3.up * 0.25f, new Color(1f, 0.82f, 0.38f), 4);

                foreach (var c in Physics2D.OverlapBoxAll(_groundPos + Vector3.up * 0.5f, new Vector2(1.1f, 1.1f), 0f))
                {
                    var player = c.GetComponentInParent<PlayerController>();
                    if (player != null)
                    {
                        EnemyController.DamagePlayer(player.transform, _groundPos, 13f);
                        break;
                    }
                }
            }
        }
    }

    static void EnsureSprites()
    {
        if (_warningSprite != null && _blockSprite != null) return;

        // Marca de aviso no chão (22x6)
        var wTex = new Texture2D(22, 6, TextureFormat.RGBA32, false);
        var wPx = new Color32[22 * 6];
        for (int y = 0; y < 6; y++)
        for (int x = 0; x < 22; x++)
        {
            float d = Mathf.Abs(x - 10.5f) / 10.5f;
            if (y <= 2 && d < 0.95f)
                wPx[y * 22 + x] = y == 1 ? new Color32(255, 238, 132, 255) : new Color32(245, 148, 62, 255);
            else if (y > 2 && (x == 4 || x == 11 || x == 17))
                wPx[y * 22 + x] = new Color32(255, 238, 132, 255);
        }
        wTex.SetPixels32(wPx);
        wTex.Apply();
        wTex.filterMode = FilterMode.Point;
        _warningSprite = Sprite.Create(wTex, new Rect(0, 0, 22, 6), new Vector2(0.5f, 0.2f), 16f);

        // Bloco de mármore e ouro (16x14)
        var bTex = new Texture2D(16, 14, TextureFormat.RGBA32, false);
        var bPx = new Color32[16 * 14];
        Color32 outline = new Color32(18, 16, 32, 255);
        Color32 marbleL = new Color32(188, 174, 216, 255);
        Color32 marbleS = new Color32(92, 78, 126, 255);
        Color32 goldL   = new Color32(248, 206, 92, 255);
        for (int y = 0; y < 14; y++)
        for (int x = 1; x < 15; x++)
        {
            if (y == 0 || y == 13 || x == 1 || x == 14) bPx[y * 16 + x] = outline;
            else if (y >= 8 && y <= 10) bPx[y * 16 + x] = goldL;
            else bPx[y * 16 + x] = x < 8 ? marbleL : marbleS;
        }
        bTex.SetPixels32(bPx);
        bTex.Apply();
        bTex.filterMode = FilterMode.Point;
        _blockSprite = Sprite.Create(bTex, new Rect(0, 0, 16, 14), new Vector2(0.5f, 0.5f), 16f);
    }
}

/// <summary>
/// Evento animado de cenário da Fase 2: Estátua Colossal que racha, solta pedaços e tomba quando o jogador passa por ela.
/// </summary>
public class PalaceCollapsingStatue : MonoBehaviour
{
    SpriteRenderer _sr;
    Sprite[] _frames;
    bool _triggered;
    float _animClock;
    int _frameIndex;

    public void Setup(Vector3 position)
    {
        transform.position = position;
        var tex = Resources.Load<Texture2D>(T2PalaceScenery.Folder + "estatua_queda");
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        int fw = tex.width / 5;
        int fh = tex.height;
        _frames = new Sprite[5];
        for (int i = 0; i < 5; i++)
            _frames[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0f), 32f);

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = _frames[0];
        _sr.sortingOrder = -18;
    }

    void Update()
    {
        if (_frames == null || _sr == null) return;
        if (!_triggered)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null && player.transform.position.x >= transform.position.x - 4.5f)
            {
                _triggered = true;
                PixelBurst.Spawn(transform.position + Vector3.up * 2.2f, new Color(1f, 0.72f, 0.32f), 10);
                PixelBurst.Spawn(transform.position + Vector3.up * 1.0f, new Color(0.75f, 0.68f, 0.88f), 12);
            }
            return;
        }

        if (_frameIndex < _frames.Length - 1)
        {
            _animClock += Time.deltaTime;
            if (_animClock >= 0.16f)
            {
                _animClock = 0f;
                _frameIndex++;
                _sr.sprite = _frames[_frameIndex];
                PixelBurst.Spawn(transform.position + Vector3.up * 0.4f, new Color(0.65f, 0.58f, 0.78f), 8);
            }
        }
    }
}

/// <summary>
/// Chuva cíclica de meteoros 16-bit cruzando o céu ao fundo da Fase 2 e soltando clarão/poeira ao atingir o palácio.
/// </summary>
public class PalaceMeteorShower : MonoBehaviour
{
    static Sprite _meteorSprite;
    StageData _stage;
    float _spawnClock;

    public void Setup(StageData stage)
    {
        _stage = stage;
    }

    void Update()
    {
        if (_stage == null || Camera.main == null) return;
        _spawnClock -= Time.deltaTime;
        if (_spawnClock > 0f) return;
        _spawnClock = 1.85f;

        Vector3 cam = Camera.main.transform.position;
        Vector3 start = new Vector3(cam.x + Random.Range(-6f, 10f), cam.y + 6.2f, 0f);
        var go = new GameObject("MeteoroFundo16Bit");
        go.transform.SetParent(transform, false);
        go.transform.position = start;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetMeteorSprite();
        sr.sortingOrder = -44;
        go.AddComponent<FallingBackgroundMeteor>();
    }

    static Sprite GetMeteorSprite()
    {
        if (_meteorSprite != null) return _meteorSprite;
        const int w = 24, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        Color32 outline = new Color32(46, 34, 68, 255);
        Color32 white   = new Color32(255, 253, 240, 255);
        Color32 yellow  = new Color32(255, 238, 132, 255);
        Color32 orange  = new Color32(245, 148, 62, 255);
        Color32 red     = new Color32(196, 68, 56, 255);

        // Cabeça incandescente (inferior esquerda) + rastro escalonado (superior direita)
        for (int i = 0; i < 6; i++)
        {
            int cx = 5 + i * 3;
            int cy = 4 + i * 2;
            int r = Mathf.Max(1, 3 - i / 2);
            for (int dy = -r - 1; dy <= r + 1; dy++)
            for (int dx = -r - 1; dx <= r + 1; dx++)
            {
                int xx = Mathf.Clamp(cx + dx, 0, w - 1);
                int yy = Mathf.Clamp(cy + dy, 0, h - 1);
                int d2 = dx * dx + dy * dy;
                if (d2 > (r + 1) * (r + 1)) continue;
                if (d2 >= r * r) px[yy * w + xx] = outline;
                else if (i == 0 && d2 <= 1) px[yy * w + xx] = white;
                else if (i <= 1) px[yy * w + xx] = yellow;
                else if (i <= 3) px[yy * w + xx] = orange;
                else px[yy * w + xx] = red;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _meteorSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        return _meteorSprite;
    }
}

public class FallingBackgroundMeteor : MonoBehaviour
{
    float _life = 1.35f;
    void Update()
    {
        transform.position += new Vector3(-6.5f, -4.8f, 0f) * Time.deltaTime;
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            PixelBurst.Spawn(transform.position, new Color(1f, 0.72f, 0.28f), 5);
            Destroy(gameObject);
        }
    }
}

// =============================================================================
// FASE 3 — "AETHERION EM RUÍNAS" (T3_Ruins_Atmos)
// =============================================================================
public static class T3RuinsScenery
{
    public const string Folder = "T3_Ruins_Atmos/";
    public const string FolderV3 = "T3_Ruins_Atmos/v3/";
    public const float GroundBelowCenter = 5.5f;
    public const float Ppu = 32f;
    public const bool Enabled = true;
    public static bool Built { get; private set; }
    public static bool HasV3Layers { get; private set; }
    public static bool NoProceduralSprites { get; private set; }
    static GameObject _root;

    public static void Reset()
    {
        Built = false;
        HasV3Layers = false;
        NoProceduralSprites = false;
        _root = null;
        T3RuinsFrontLevel.Reset();
    }

    public static void ClearIfOwner(GameObject owner)
    {
        if (_root == owner) Reset();
    }

    public static void ConfigureStage(StageData stage)
    {
        stage.GroundTop = AtmosV1Parallax.Snap(stage.GroundTop);
    }

    public static Sprite LoadV3Layer(string name)
    {
        var texture = Resources.Load<Texture2D>(FolderV3 + name);
        if (texture == null) return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        // Pivô na linha y=372px (de cima para baixo) num frame de 432px de altura:
        // 432 - 372 = 60px do fundo -> pivô y = 60 / 432 = 0.1388889f
        var pivot = new Vector2(0.5f, 60f / 432f);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            pivot, Ppu, 0, SpriteMeshType.FullRect);
    }

    public static bool Build(StageData stage)
    {
        Reset();

        // 7 camadas oficiais v3 com névoas adicionais:
        // P1_ceu_fenda (opaca, 0.08, -50)
        // P2_cidadela (0.22, -40)
        // P2b_nevoa_fenda (0.30, -35)
        // P3_salao (0.40, -30)
        // P3b_nevoa_media (0.50, -25)
        // P4_tentaculos_estatuas (0.62, -20)
        // P4b_nevoa_chao (semitransparente, 0.62, -15) <-- Névoa que destaca os inimigos dos tentáculos
        var layerNames = new[]
        {
            "P1_ceu_fenda_native",
            "P2_cidadela_native",
            "P2b_nevoa_fenda_native",
            "P3_salao_native",
            "P3b_nevoa_media_native",
            "P4_tentaculos_estatuas_native",
            "P4b_nevoa_chao_native"
        };
        float[] factors = { 0.08f, 0.22f, 0.30f, 0.40f, 0.50f, 0.62f, 0.62f };
        int[] orders = { -50, -40, -35, -30, -25, -20, -15 };

        var sprites = new Sprite[layerNames.Length];
        for (int i = 0; i < layerNames.Length; i++)
        {
            sprites[i] = LoadV3Layer(layerNames[i]);
            if (sprites[i] == null)
            {
                Debug.LogError("T3 Ruins v3: camada ausente: " + layerNames[i]);
                return false;
            }
        }
        HasV3Layers = true;

        var root = new GameObject("Cenario_RuinasT3_v3");
        root.AddComponent<AtmosV1Root>();

        // Ancoragem vertical: pivô na linha y=372px fica exatamente em stage.GroundTop
        var reference = new Vector2(-stage.HalfWidth + 12f, stage.GroundTop);

        for (int i = 0; i < factors.Length; i++)
        {
            var group = new GameObject(layerNames[i]);
            group.transform.SetParent(root.transform, false);

            // Loop de cópias contínuas (-3 a +4) cobrindo todo o range de x da fase e da câmera
            for (int copy = -3; copy <= 4; copy++)
            {
                var tile = new GameObject("Copia_" + copy);
                tile.transform.SetParent(group.transform, false);
                tile.transform.localPosition = new Vector3(copy * 24f, 0f, 0f);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = orders[i];
            }
            group.AddComponent<AtmosV1Parallax>().Setup(factors[i], reference, false);
        }

        T3RuinsFrontLevel.Build(stage, root.transform);
        _root = root;

        // Validação de que nenhum SpriteRenderer da Fase 3 usa whiteTexture ou procedurais
        bool allClean = true;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite != null && (sr.sprite.texture == Texture2D.whiteTexture || sr.sprite.rect.width <= 2))
            {
                allClean = false;
                break;
            }
        }
        NoProceduralSprites = allClean;

        Built = true;
        return true;
    }
}

public static class T3RuinsFrontLevel
{
    const float Thickness = 0.375f;
    public static bool Built { get; private set; }

    static readonly List<Vector2> _gaps = new List<Vector2>();
    static readonly List<AtmosFrontLevel.Platform> _platforms = new List<AtmosFrontLevel.Platform>();

    public static IReadOnlyList<Vector2> Gaps => _gaps;
    public static IReadOnlyList<AtmosFrontLevel.Platform> Platforms => _platforms;

    public static void Reset()
    {
        Built = false;
        _gaps.Clear();
        _platforms.Clear();
    }

    static Sprite LoadV2(string name, Vector2 pivot, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var tex = Resources.Load<Texture2D>("T3_Ruins_Atmos/v2/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = wrap;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
    }

    static Sprite LoadV3Tile(string name, Vector2 pivot, TextureWrapMode wrap = TextureWrapMode.Repeat)
    {
        var tex = Resources.Load<Texture2D>("T3_Ruins_Atmos/v3/tiles/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = wrap;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
    }

    static Sprite LoadV3Prop(string name)
    {
        var tex = Resources.Load<Texture2D>("T3_Ruins_Atmos/v3/props/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        // Pivô base-centro (0.5, 0) com o pé no GroundTop
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
    }

    public static void Build(StageData stage, Transform parent)
    {
        Reset();
        var root = new GameObject("P5_FrenteJogavel_T3");
        root.transform.SetParent(parent, false);

        var slick = new PhysicsMaterial2D("RuinasSemAtrito") { friction = 0f, bounciness = 0f };

        // Trecho Caminho (x = -60 .. +36):
        // Segmento 1: x = -60f .. -38f
        // Gap 1: x = -38f .. -34f (4u)
        // Segmento 2: x = -34f .. -8f
        // Gap 2: x = -8f .. -4f (4u)
        // Segmento 3: x = -4f .. +36f
        // Arena do Chefe: x = +36f .. +60f (piso contínuo de 24u)
        Ground("Ground_T3_Seg1", -60f, -38f, stage.GroundTop, slick, root.transform);
        Ground("Ground_T3_Seg2", -34f, -8f, stage.GroundTop, slick, root.transform);
        Ground("Ground_T3_Seg3", -4f, 36f, stage.GroundTop, slick, root.transform);
        Ground("Ground_T3_Arena", 36f, 60f, stage.GroundTop, slick, root.transform);

        // Visual do chão ao longo de todo o mapa (-60 a +60) com o kit v2 + subsolo v3
        BuildGroundVisualV2(stage, -60f, -38f, abyssLeft: false, abyssRight: true, root.transform);
        BuildGroundVisualV2(stage, -34f, -8f, abyssLeft: true, abyssRight: true, root.transform);
        BuildGroundVisualV2(stage, -4f, 60f, abyssLeft: true, abyssRight: true, root.transform);

        // Abismos escuros sob os gaps (com abismo_vazio_64x128 v3 substituindo o retângulo preto)
        BuildAbyssGap(stage, -38f, -34f, root.transform);
        BuildAbyssGap(stage, -8f, -4f, root.transform);
        // Abismo após o fim do chão da arena (60f a 68f)
        BuildAbyssGap(stage, 60f, 68f, root.transform);

        // Plataformas flutuantes pelo caminho (v2: 96, 128, 160) + Degraus (capitel e estátua)
        BuildV2Platform("plataforma_flutuante_96x48",  -50.0f, stage.GroundTop + 2.2f, root.transform);
        BuildV2Platform("plataforma_flutuante_128x48", -44.0f, stage.GroundTop + 3.8f, root.transform);
        BuildV2Platform("plataforma_flutuante_160x48", -36.0f, stage.GroundTop + 2.2f, root.transform); // Sobre Gap 1
        BuildSteppingStone("capitel_coluna",          -26.0f, stage.GroundTop, root.transform);
        BuildV2Platform("plataforma_flutuante_128x48", -18.0f, stage.GroundTop + 3.6f, root.transform);
        BuildV2Platform("plataforma_flutuante_160x48",  -6.0f, stage.GroundTop + 2.3f, root.transform); // Sobre Gap 2
        BuildSteppingStone("estatua_rei_tombada",       4.0f, stage.GroundTop, root.transform);
        BuildV2Platform("plataforma_flutuante_96x48",   14.0f, stage.GroundTop + 3.6f, root.transform);
        BuildV2Platform("plataforma_flutuante_128x48",  24.0f, stage.GroundTop + 2.3f, root.transform);

        // Arena do Chefe (x = 36 .. 60) - 3 plataformas flutuantes (128, 160, 128) nas alturas 2.2u, 3.4u, 2.2u
        BuildV2Platform("plataforma_flutuante_128x48", 42.5f, stage.GroundTop + 2.2f, root.transform);
        BuildV2Platform("plataforma_flutuante_160x48", 48.0f, stage.GroundTop + 3.4f, root.transform);
        BuildV2Platform("plataforma_flutuante_128x48", 53.5f, stage.GroundTop + 2.2f, root.transform);

        // Pilares decorativos da arena (capitel de coluna com pé no chão)
        BuildDecorativePillar(new Vector3(38.2f, stage.GroundTop, 0f), root.transform);
        BuildDecorativePillar(new Vector3(57.8f, stage.GroundTop, 0f), root.transform);

        // Props decorativos do chão (v3: entulho, cristal, caixotes, urnas, ossos, tambor)
        BuildAllProps(stage, root.transform);

        // Perigos do caminho (fogo, espinhos, escombros caindo)
        BuildRuinsHazards(stage, root.transform);

        // Portão e Paredes da Arena
        BuildArenaGates(stage, root.transform);

        // Fall zone abaixo do chão
        var fall = new GameObject("Ruinas_ZonaDeQueda");
        fall.transform.SetParent(root.transform, false);
        fall.transform.position = new Vector3(0f, stage.GroundTop - 4.5f, 0f);
        var trigger = fall.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(stage.HalfWidth * 2f + 16f, 1f);
        fall.AddComponent<T1FallZone>();

        Built = true;
    }

    static void Ground(string name, float left, float right, float top, PhysicsMaterial2D material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 2f, 0f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(right - left, 4f);
        collider.sharedMaterial = material;
    }

    static void BuildAbyssGap(StageData stage, float x0, float x1, Transform parent)
    {
        _gaps.Add(new Vector2(x0, x1));
        var abyss = new GameObject("Abismo_Vazio_T3_" + Mathf.RoundToInt(x0));
        abyss.transform.SetParent(parent, false);
        // Topo em GroundTop - 0.25u (8px abaixo da superfície), descendo 8u até a base da câmera
        abyss.transform.position = new Vector3(x0, stage.GroundTop - 0.25f, 0f);
        var sr = abyss.AddComponent<SpriteRenderer>();
        var spr = LoadV3Tile("abismo_vazio_64x128", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (spr != null)
        {
            sr.sprite = spr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(x1 - x0, 8.0f);
        }
        sr.sortingOrder = -10;
    }

    static void BuildGroundVisualV2(StageData stage, float left, float right, bool abyssLeft, bool abyssRight, Transform parent)
    {
        var root = new GameObject("GroundVisual_T3_" + Mathf.RoundToInt(left * 10f));
        root.transform.SetParent(parent, false);

        float curLeft = left;
        float curRight = right;

        if (abyssLeft)
        {
            var edgeSpr = LoadV2("chao_borda_abismo_esq_24x48", new Vector2(0f, 1f));
            if (edgeSpr != null)
            {
                var edgeGo = new GameObject("BordaAbismoEsq");
                edgeGo.transform.SetParent(root.transform, false);
                edgeGo.transform.position = new Vector3(curLeft, stage.GroundTop, 0f);
                var sr = edgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = edgeSpr;
                sr.sortingOrder = 0;
            }
            curLeft += 0.75f;
        }
        else
        {
            var tipSpr = LoadV2("chao_ponta_esq_16x32", new Vector2(0f, 1f));
            if (tipSpr != null)
            {
                var tipGo = new GameObject("PontaEsq");
                tipGo.transform.SetParent(root.transform, false);
                tipGo.transform.position = new Vector3(curLeft, stage.GroundTop, 0f);
                var sr = tipGo.AddComponent<SpriteRenderer>();
                sr.sprite = tipSpr;
                sr.sortingOrder = 0;
            }
            curLeft += 0.5f;
        }

        if (abyssRight)
        {
            var edgeSpr = LoadV2("chao_borda_abismo_dir_24x48", new Vector2(0f, 1f));
            if (edgeSpr != null)
            {
                var edgeGo = new GameObject("BordaAbismoDir");
                edgeGo.transform.SetParent(root.transform, false);
                edgeGo.transform.position = new Vector3(curRight - 0.75f, stage.GroundTop, 0f);
                var sr = edgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = edgeSpr;
                sr.sortingOrder = 0;
            }
            curRight -= 0.75f;
        }
        else
        {
            var tipSpr = LoadV2("chao_ponta_dir_16x32", new Vector2(0f, 1f));
            if (tipSpr != null)
            {
                var tipGo = new GameObject("PontaDir");
                tipGo.transform.SetParent(root.transform, false);
                tipGo.transform.position = new Vector3(curRight - 0.5f, stage.GroundTop, 0f);
                var sr = tipGo.AddComponent<SpriteRenderer>();
                sr.sprite = tipSpr;
                sr.sortingOrder = 0;
            }
            curRight -= 0.5f;
        }

        var tileSpr = LoadV2("chao_tile_64x32", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (tileSpr != null && curRight > curLeft)
        {
            var tileGo = new GameObject("ChaoTiled");
            tileGo.transform.SetParent(root.transform, false);
            tileGo.transform.position = new Vector3(curLeft, stage.GroundTop, 0f);
            var sr = tileGo.AddComponent<SpriteRenderer>();
            sr.sprite = tileSpr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(curRight - curLeft, 1.0f);
            sr.sortingOrder = 0;
        }

        // Subsolo: chao_subsolo_64x48 preenche embaixo do chao_tile até a base da câmera (7u)
        // Substitui a faixa vermelha lisa. Sorting -1 (igual ao chão menos 1). Sem colisor.
        var subsoloSpr = LoadV3Tile("chao_subsolo_64x48", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (subsoloSpr != null && right > left)
        {
            var subGo = new GameObject("ChaoSubsolo");
            subGo.transform.SetParent(root.transform, false);
            subGo.transform.position = new Vector3(left, stage.GroundTop - 1.0f, 0f);
            var srSub = subGo.AddComponent<SpriteRenderer>();
            srSub.sprite = subsoloSpr;
            srSub.drawMode = SpriteDrawMode.Tiled;
            srSub.size = new Vector2(right - left, 7.0f);
            srSub.sortingOrder = -1;
        }
    }

    static void BuildProp(string name, float x, float groundTop, Transform parent)
    {
        var spr = LoadV3Prop(name);
        if (spr == null) return;
        var go = new GameObject("Prop_T3_" + name + "_" + Mathf.RoundToInt(x * 10f));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, groundTop, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = -5;
    }

    static void BuildAllProps(StageData stage, Transform parent)
    {
        var propsRoot = new GameObject("Props_RuinasT3_v3");
        propsRoot.transform.SetParent(parent, false);

        // Segmento 1: x = -60 a -38 (abismo em -38)
        BuildProp("entulho_a",      -58.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("urna_rachada",   -52.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("cristal_a",      -46.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("tambor_coluna",  -41.5f, stage.GroundTop, propsRoot.transform);

        // Segmento 2: x = -34 a -8 (abismos em -38..-34 e -8..-4)
        BuildProp("caixotes_barril",-31.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("ossos_detritos", -28.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("entulho_b",      -22.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("cristal_b",      -19.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("pilar_quebrado", -15.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("urna_rachada",   -11.0f, stage.GroundTop, propsRoot.transform);

        // Segmento 3: x = -4 a +36 (abismo em -8..-4, portão em 36.4)
        BuildProp("tambor_coluna",   -1.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("entulho_a",        2.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("caixotes_barril",  7.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("cristal_c",       11.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("ossos_detritos",  16.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("pilar_quebrado",  20.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("entulho_b",       25.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("urna_rachada",    29.5f, stage.GroundTop, propsRoot.transform);
        BuildProp("tambor_coluna",   33.5f, stage.GroundTop, propsRoot.transform);

        // Arena: x = 36 a 60 (portão em 36.4, parede em 59.6)
        BuildProp("entulho_a",       40.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("ossos_detritos",  45.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("cristal_a",       51.0f, stage.GroundTop, propsRoot.transform);
        BuildProp("urna_rachada",    56.0f, stage.GroundTop, propsRoot.transform);
    }

    static void BuildV2Platform(string name, float centerX, float topY, Transform parent)
    {
        var spr = LoadV2(name, new Vector2(0.5f, 1f));
        if (spr == null) return;
        float w = spr.rect.width / 32f;
        float colW = w - (4f / 32f);

        var go = new GameObject("Plat_T3_" + name + "_" + Mathf.RoundToInt(centerX));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, topY, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 1;

        var colGo = new GameObject("Colisor");
        colGo.transform.SetParent(go.transform, false);
        colGo.transform.position = new Vector3(centerX, topY - 0.125f, 0f);
        var collider = colGo.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(colW, 0.25f);
        collider.usedByEffector = true;
        var effector = colGo.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;

        _platforms.Add(new AtmosFrontLevel.Platform(name, centerX - colW * 0.5f, centerX + colW * 0.5f, topY));
    }

    static void BuildSteppingStone(string name, float x, float groundTop, Transform parent)
    {
        var spr = LoadV2(name, new Vector2(0.5f, 0f));
        if (spr == null) return;
        var go = new GameObject("Degrau_T3_" + name + "_" + Mathf.RoundToInt(x));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, groundTop, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 1;

        float topY = groundTop + (name == "capitel_coluna" ? (53f / 32f) : (31f / 32f));
        float colW = name == "capitel_coluna" ? (72f / 32f) : (64f / 32f);
        var colGo = new GameObject("Colisor");
        colGo.transform.SetParent(go.transform, false);
        colGo.transform.position = new Vector3(x, topY - 0.125f, 0f);
        var collider = colGo.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(colW, 0.25f);
        collider.usedByEffector = true;
        var effector = colGo.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;

        _platforms.Add(new AtmosFrontLevel.Platform(name, x - colW * 0.5f, x + colW * 0.5f, topY));
    }

    static void BuildDecorativePillar(Vector3 position, Transform parent)
    {
        var spr = LoadV2("capitel_coluna", new Vector2(0.5f, 0f));
        if (spr == null) return;
        var go = new GameObject("PilarDecorativo");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 2;
    }

    static void BuildRuinsHazards(StageData stage, Transform parent)
    {
        var hazardsRoot = new GameObject("Perigos_Ruinas_T3");
        hazardsRoot.transform.SetParent(parent, false);

        // Fendas com fogo/vazio
        float[] fireXs = { -48.0f, -22.0f, 8.0f, 22.0f };
        foreach (float fx in fireXs)
            PalaceFireHazard.Spawn(hazardsRoot.transform, new Vector3(fx, stage.GroundTop, 0f), 1.8f);

        // Espinhos de obsidiana
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(-42.0f, stage.GroundTop, 0f), 1.4f, cycling: true);
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3(-14.0f, stage.GroundTop, 0f), 1.4f, cycling: false);
        PalaceSpikeHazard.Spawn(hazardsRoot.transform, new Vector3( 18.0f, stage.GroundTop, 0f), 1.5f, cycling: true);

        // Escombros caindo
        float[] debrisXs = { -28.0f, -1.0f, 12.0f, 28.0f };
        for (int i = 0; i < debrisXs.Length; i++)
            PalaceFallingDebris.Spawn(hazardsRoot.transform, new Vector3(debrisXs[i], stage.GroundTop, 0f), i * 0.9f);
    }

    static void BuildArenaGates(StageData stage, Transform parent)
    {
        var gateGo = new GameObject("ArenaGate_T3");
        gateGo.transform.SetParent(parent, false);
        gateGo.transform.position = new Vector3(36.4f, stage.GroundTop, 0f);

        // Solid collider for gate when closed (120x100 px = 3.75u x 3.125u, centered at base)
        var colL = gateGo.AddComponent<BoxCollider2D>();
        colL.size = new Vector2(120f / 32f, 100f / 32f);
        colL.offset = new Vector2(0f, 50f / 32f);
        colL.enabled = false;

        // Right wall of arena at x = 59.6f (invisible camera/arena boundary)
        var wallR = new GameObject("ArenaWallRight");
        wallR.transform.SetParent(gateGo.transform, false);
        wallR.transform.position = new Vector3(59.6f, stage.GroundTop + 4f, 0f);
        var colR = wallR.AddComponent<BoxCollider2D>();
        colR.size = new Vector2(0.6f, 14f);
        colR.enabled = false;

        var gate = gateGo.AddComponent<RuinsArenaGate>();
        gate.Setup(colL, colR, stage);
    }
}

public class RuinsArenaGate : MonoBehaviour
{
    BoxCollider2D _colL;
    BoxCollider2D _colR;
    StageData _stage;
    bool _locked;
    SpriteRenderer _gateVisual;
    Sprite _frameClosed;
    Sprite _frameOpen;

    public void Setup(BoxCollider2D colL, BoxCollider2D colR, StageData stage)
    {
        _colL = colL;
        _colR = colR;
        _stage = stage;

        var tex = Resources.Load<Texture2D>("T3_Ruins_Atmos/v2/portao_arena_2f_164x100");
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            int cellW = 164;
            int cellH = 100;
            var pivot = new Vector2(0.5f, 0f);
            _frameClosed = Sprite.Create(tex, new Rect(0, 0, cellW, cellH), pivot, 32f, 0, SpriteMeshType.FullRect);
            _frameOpen = Sprite.Create(tex, new Rect(cellW, 0, cellW, cellH), pivot, 32f, 0, SpriteMeshType.FullRect);
        }

        var visGo = new GameObject("GateVisual");
        visGo.transform.SetParent(transform, false);
        visGo.transform.localPosition = Vector3.zero;
        visGo.transform.localScale = Vector3.one;
        _gateVisual = visGo.AddComponent<SpriteRenderer>();
        _gateVisual.sprite = _frameOpen;
        _gateVisual.sortingOrder = 9;
    }

    public void LockArena()
    {
        if (_locked) return;
        _locked = true;
        if (_colL != null) _colL.enabled = true;
        if (_colR != null) _colR.enabled = true;
        if (_gateVisual != null) _gateVisual.sprite = _frameClosed;
        PixelBurst.Spawn(transform.position + Vector3.up * 1.5f, new Color(0.75f, 0.25f, 0.95f), 15);
    }

    public void UnlockArena()
    {
        _locked = false;
        if (_colL != null) _colL.enabled = false;
        if (_colR != null) _colR.enabled = false;
        if (_gateVisual != null) _gateVisual.sprite = _frameOpen;
    }
}

