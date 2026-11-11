using System.Collections.Generic;
using UnityEngine;

// LAYOUT_GAPS, PPU 32: trecho inicial com 2 gaps + plataformas A/B/C e plataformas 16-bit ao longo da fase inteira (v3).
public static class AtmosFrontLevel
{
    public const string FolderV3 = "T1_Hybrid_Atmos/v3/";
    public const float Width = 24f;
    public const float SurfaceRow = 392f;
    public const float GroundSurfacePx = 8f;
    const float Thickness = 0.1875f; // 6 px de colisor

    public readonly struct Platform
    {
        public readonly string Name;
        public readonly float Left, Right, Top;
        public Platform(string name, float left, float right, float top)
        { Name = name; Left = left; Right = right; Top = top; }
    }

    static readonly List<Vector2> _gaps = new List<Vector2>();
    static readonly List<Platform> _platforms = new List<Platform>();
    public static IReadOnlyList<Vector2> Gaps => _gaps;
    public static IReadOnlyList<Platform> Platforms => _platforms;
    public static bool Built { get; private set; }

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
        ("plataforma_musgo_128_a", -9.0f,  2.2f),
        ("plataforma_musgo_96_b",  -4.2f,  3.8f),
        ("plataforma_musgo_160_a",  1.5f,  2.4f),
        ("plataforma_musgo_128_b",  7.2f,  4.1f),
        ("plataforma_musgo_96_a",  12.5f,  2.2f),
        ("plataforma_musgo_160_b", 18.0f,  3.6f),
        ("plataforma_musgo_128_a", 23.5f,  2.3f),
        ("plataforma_musgo_96_b",  28.0f,  3.9f),
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
        var root = new GameObject("P5_FrenteJogavel_T1");
        root.transform.SetParent(parent, false);

        var slick = new PhysicsMaterial2D("AtmosSemAtrito") { friction = 0f, bounciness = 0f };

        // Colisores do chão (mantém exatamente a mesma geometria)
        Ground("Ground_L", X(stage, 0), X(stage, 240), stage.GroundTop, slick, root.transform);
        Ground("IlhaMid", X(stage, 320), X(stage, 460), stage.GroundTop, slick, root.transform);
        Ground("Ground_R", X(stage, 560), stage.HalfWidth, stage.GroundTop, slick, root.transform);

        _gaps.Add(new Vector2(X(stage, 240), X(stage, 320)));
        _gaps.Add(new Vector2(X(stage, 460), X(stage, 560)));

        // Visual do chão por trechos: Grama v3 + Subsolo v3 + Bordas v3
        BuildGroundVisual(stage, X(stage, 0), X(stage, 240), abyssLeft: false, abyssRight: true, root.transform);
        BuildGroundVisual(stage, X(stage, 320), X(stage, 460), abyssLeft: true, abyssRight: true, root.transform);
        BuildGroundVisual(stage, X(stage, 560), stage.HalfWidth, abyssLeft: true, abyssRight: false, root.transform);

        // Abismos escuros com abismo_ceu_64x96 v3 (sem retângulos brancos ou procedurais)
        foreach (var gap in _gaps)
        {
            BuildAbyss(stage, gap.x, gap.y, root.transform);
        }

        // Plataformas flutuantes com arte v3 e colisores na altura exata do MANIFEST
        BuildPlatformV3("plataforma_musgo_96_a",  X(stage, 114), Y(stage, 324), "L0", stage, root.transform, false);
        BuildPlatformV3("plataforma_musgo_96_b",  X(stage, 242), Y(stage, 296), "A",  stage, root.transform, true);
        BuildPlatformV3("plataforma_musgo_128_a", X(stage, 388), Y(stage, 264), "M",  stage, root.transform, false);
        BuildPlatformV3("plataforma_musgo_128_b", X(stage, 528), Y(stage, 284), "B",  stage, root.transform, true);
        BuildPlatformV3("plataforma_musgo_96_a",  X(stage, 664), Y(stage, 240), "C",  stage, root.transform, true);

        BuildExtraPlatforms(stage, root.transform);
        BuildProps(stage, root.transform);
        BuildAmbientFx(stage, root.transform);

        var fall = new GameObject("Atmos_ZonaDeQueda");
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
        var abyss = new GameObject("Abismo_Ceu_T1_" + Mathf.RoundToInt(x0 * 10f));
        abyss.transform.SetParent(parent, false);
        // Topo em GroundTop - 0.125u (4 px abaixo da linha do chão)
        abyss.transform.position = new Vector3(x0, stage.GroundTop - 0.125f, 0f);
        var sr = abyss.AddComponent<SpriteRenderer>();
        var spr = LoadTile("abismo_ceu_64x96", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (spr != null)
        {
            sr.sprite = spr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(x1 - x0, 8.0f);
        }
        sr.sortingOrder = -10;

        // Pedras caindo decorativas nos vãos
        var pSpr = LoadTile("pedra_caindo_" + (Random.Range(0, 4)), new Vector2(0.5f, 0.5f));
        if (pSpr != null)
        {
            var pGo = new GameObject("PedraCaindo_Decor");
            pGo.transform.SetParent(abyss.transform, false);
            pGo.transform.position = new Vector3((x0 + x1) * 0.5f, stage.GroundTop - 1.8f, 0f);
            var pSr = pGo.AddComponent<SpriteRenderer>();
            pSr.sprite = pSpr;
            pSr.sortingOrder = -9;
        }
    }

    static void BuildGroundVisual(StageData stage, float left, float right, bool abyssLeft, bool abyssRight, Transform parent)
    {
        var root = new GameObject("GroundVisual_T1_" + Mathf.RoundToInt(left * 10f));
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
                // Linha caminhável y=8 (8/32u acima da base do topo do PNG); sobrepõe 4 px no vão
                edgeGo.transform.position = new Vector3(curLeft - (4f / 32f), stage.GroundTop + (8f / 32f), 0f);
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
                // Linha caminhável y=7 (7/32u); sobrepõe 4 px no vão
                edgeGo.transform.position = new Vector3(curRight - (48f / 32f) + (4f / 32f), stage.GroundTop + (7f / 32f), 0f);
                var sr = edgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = edgeSpr;
                sr.sortingOrder = 1;
            }
        }

        // Chão de grama: topo em GroundTop + 6 px (linha caminhável y=6)
        var tileSpr = LoadTile("chao_grama_128", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (tileSpr != null && curRight > curLeft)
        {
            var tileGo = new GameObject("ChaoGramaTiled");
            tileGo.transform.SetParent(root.transform, false);
            tileGo.transform.position = new Vector3(curLeft, stage.GroundTop + (6f / 32f), 0f);
            var sr = tileGo.AddComponent<SpriteRenderer>();
            sr.sprite = tileSpr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(curRight - curLeft, 83f / 32f);
            sr.sortingOrder = 0;
        }

        // Subsolo com raízes: começa 77 px abaixo de GroundTop, desce 7u
        var subsoloSpr = LoadTile("chao_subsolo_raizes_128x64", new Vector2(0f, 1f), TextureWrapMode.Repeat);
        if (subsoloSpr != null && right > left)
        {
            var subGo = new GameObject("ChaoSubsoloRaizes");
            subGo.transform.SetParent(root.transform, false);
            subGo.transform.position = new Vector3(left, stage.GroundTop - (77f / 32f), 0f);
            var srSub = subGo.AddComponent<SpriteRenderer>();
            srSub.sprite = subsoloSpr;
            srSub.drawMode = SpriteDrawMode.Tiled;
            srSub.size = new Vector2(right - left, 7.0f);
            srSub.sortingOrder = -1;
        }
    }

    static void BuildPlatformV3(string spriteName, float centerX, float topY, string regName, StageData stage, Transform parent, bool register)
    {
        int h = 62; int yCol = 8; int colPx = 90;
        if (spriteName.Contains("96_a"))      { h = 62;  yCol = 8;  colPx = 90; }
        else if (spriteName.Contains("96_b")) { h = 63;  yCol = 8;  colPx = 90; }
        else if (spriteName.Contains("128_a")){ h = 84;  yCol = 11; colPx = 122; }
        else if (spriteName.Contains("128_b")){ h = 86;  yCol = 12; colPx = 122; }
        else if (spriteName.Contains("160_a")){ h = 105; yCol = 12; colPx = 154; }
        else if (spriteName.Contains("160_b")){ h = 109; yCol = 14; colPx = 154; }

        float pivotY = (h - yCol) / (float)h;
        var spr = LoadPlatform(spriteName, new Vector2(0.5f, pivotY));
        if (spr == null) return;

        float colW = colPx / 32f;
        float colH = 6f / 32f; // 0.1875u

        var go = new GameObject("Plat_T1_" + regName + "_" + spriteName);
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
            _platforms.Add(new Platform(regName, centerX - colW * 0.5f, centerX + colW * 0.5f, topY));
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
        var propsRoot = new GameObject("Props_AtmosT1_v3");
        propsRoot.transform.SetParent(parent, false);

        float baseGround = stage.GroundTop - (3f / 32f);

        // Segmento 1: -36.0f a -28.5f
        BuildSingleProp("placa",          -34.0f, baseGround, propsRoot.transform);
        BuildSingleProp("arbusto_flores", -30.5f, baseGround, propsRoot.transform);

        // Segmento 2: -26.0f a -21.625f
        BuildSingleProp("flores",         -24.8f, baseGround, propsRoot.transform);
        BuildSingleProp("cogumelos",      -22.8f, baseGround, propsRoot.transform);

        // Segmento 3: -18.5f a +36.0f
        BuildSingleProp("pedra_runica",   -16.5f, baseGround, propsRoot.transform);
        BuildSingleProp("lanterna",       -11.0f, baseGround, propsRoot.transform);
        BuildSingleProp("cristais_ciano",  -5.0f, baseGround, propsRoot.transform);
        BuildSingleProp("santuario",        0.0f, baseGround, propsRoot.transform);
        BuildSingleProp("rocha_samambaia",  6.5f, baseGround, propsRoot.transform);
        BuildSingleProp("pilar_vinhas",    13.0f, baseGround, propsRoot.transform);
        BuildSingleProp("arbusto_flores",  19.5f, baseGround, propsRoot.transform);
        BuildSingleProp("flores",          25.0f, baseGround, propsRoot.transform);
        BuildSingleProp("lanterna",        30.5f, baseGround, propsRoot.transform);
        BuildSingleProp("santuario",       34.5f, baseGround, propsRoot.transform);
    }

    static void BuildSingleProp(string name, float x, float baseY, Transform parent)
    {
        var spr = LoadProp(name);
        if (spr == null) return;
        var go = new GameObject("Prop_T1_" + name + "_" + Mathf.RoundToInt(x * 10f));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, baseY, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = -5;
    }

    static void BuildAmbientFx(StageData stage, Transform parent)
    {
        var fxRoot = new GameObject("FX_Ambiente_T1");
        fxRoot.transform.SetParent(parent, false);

        // 1. Pétalas caindo na diagonal (128x32 -> 4 frames 32x32, 8 fps em loop, sorting 15)
        var petalasTex = Resources.Load<Texture2D>(FolderV3 + "fx/fx_petalas_4f_32x32");
        if (petalasTex != null)
        {
            var petalasFrames = AmbientFxFactory.Slice(petalasTex, 32, 32, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 8; i++)
            {
                var pGo = new GameObject("Petala_" + i);
                pGo.transform.SetParent(fxRoot.transform, false);
                float px = -32f + i * 9.5f;
                float py = stage.GroundTop + 1.0f + (i % 4) * 1.5f;
                pGo.transform.position = new Vector3(px, py, 0f);
                var sr = pGo.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 15;
                pGo.AddComponent<AmbientSpriteAnimator>().Setup(petalasFrames, 8f, loop: true);
                pGo.AddComponent<AmbientFloatingDrift>().Setup(
                    new Vector3(-1.2f, -0.6f, 0f),
                    -38f, 38f, stage.GroundTop - 1f, stage.GroundTop + 7f);
            }
        }

        // 2. Motes de Aether ciano flutuando (128x32 -> 4 frames 32x32, 6 fps em loop, sorting -12)
        var motesTex = Resources.Load<Texture2D>(FolderV3 + "fx/fx_motes_aether_4f_32x32");
        if (motesTex != null)
        {
            var motesFrames = AmbientFxFactory.Slice(motesTex, 32, 32, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 6; i++)
            {
                var mGo = new GameObject("MoteAether_" + i);
                mGo.transform.SetParent(fxRoot.transform, false);
                float mx = -28f + i * 11.0f;
                float my = stage.GroundTop + 2.0f + (i % 3) * 1.2f;
                mGo.transform.position = new Vector3(mx, my, 0f);
                var sr = mGo.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -12; // atrás das plataformas
                mGo.AddComponent<AmbientSpriteAnimator>().Setup(motesFrames, 6f, loop: true);
                mGo.AddComponent<AmbientFloatingDrift>().Setup(
                    new Vector3(0.3f, 0.4f, 0f),
                    -36f, 36f, stage.GroundTop, stage.GroundTop + 6.5f);
            }
        }
    }
}
