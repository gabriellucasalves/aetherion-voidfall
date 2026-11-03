using System.Collections.Generic;
using UnityEngine;

// Frente jogável da Fase I no fundo híbrido: chão em segmentos com gaps mortais,
// ilhas flutuantes one-way em duas alturas e zona de queda abaixo dos gaps.
// Arte: Resources/T1_Hybrid (kit Tools/Aseprite/plataformas_frente, PPU 16, Point).
// Colisores medidos nos pixels da arte:
//   ground*: superfície na linha 8; beirada sólida a 2 px do gap.
//   plat_*:  superfície na linha 4; topo sólido de x=2 até w-3.
public static class T1FrontLevel
{
    const string Folder = "T1_Hybrid/";
    const float Ppu = PixelArt.Ppu;
    const int GroundSurfacePx = 8;
    const int EdgeInsetPx = 2;
    const int EdgeWidthPx = 24;
    const int PlatSurfacePx = 4;
    const int PlatInsetPx = 2;
    const int PitTuckPx = 4;
    const float PlatThickness = 0.38f;
    const float GroundDepth = 3.4f;
    // Câmera mostra ~3 u abaixo do GroundTop: a zona fica fora da tela.
    const float FallZoneDepth = 4.5f;
    // Segmentos que passam deste X continuam fora da tela, sem beirada.
    const float OpenEdgeX = 36f;
    const float SpawnMargin = 1.2f;

    // Chão em unidades (1 u = tile de 16 px). Os vãos entre segmentos são gaps mortais:
    // 2 u logo após o início seguro (ensina o pulo) e 3 u nos demais.
    // Praça de combate contínua entre -4 e 13 (ondas de inimigos).
    public static readonly (float x0, float x1)[] Segments =
    {
        (-40f, -26f), (-24f, -17f), (-14f, -7f), (-4f, 13f), (16f, 24f), (27f, 40f),
    };

    // Ilhas: altura = topo de caminhada acima do GroundTop (2,0 e 3,75 u; pulo alcança ~2,7 u).
    public static readonly (string sprite, float x, float height)[] Plats =
    {
        ("plat_s", -15.5f, 2.0f), ("plat_m", -11f, 3.75f), ("plat_m", -5.5f, 2.0f),
        ("plat_s", 14.5f, 2.0f), ("plat_m", 19f, 3.75f), ("plat_s", 25.5f, 2.0f), ("plat_l", 31f, 3.75f),
    };

    static readonly List<Vector2> Gaps = new List<Vector2>();

    public static bool Built { get; private set; }

    public static void Reset()
    {
        Built = false;
        Gaps.Clear();
    }

    public static bool Build(StageData stage, Transform parent)
    {
        Reset();
        var ground = Load("ground");
        var edgeL = Load("ground_edge_l");
        var edgeR = Load("ground_edge_r");
        var pit = Load("pit");
        var plats = new Dictionary<string, Texture2D>();
        foreach (var plat in Plats)
            if (!plats.ContainsKey(plat.sprite))
                plats[plat.sprite] = Load(plat.sprite);
        if (ground == null || edgeL == null || edgeR == null || pit == null || plats.ContainsValue(null))
        {
            Debug.LogWarning("Frente T1: faltam PNGs do kit em Resources/T1_Hybrid — mantendo chão contínuo.");
            return false;
        }

        // Plataformas clássicas: no híbrido ficavam como colisores invisíveis, com os pilares brancos da ponte.
        var classic = GameObject.Find("Plataformas");
        if (classic != null)
            Object.Destroy(classic);

        var root = new GameObject("FrenteJogavel");
        root.transform.SetParent(parent, false);
        float groundBottom = stage.GroundTop + GroundSurfacePx / Ppu - ground.height / Ppu;
        var slick = new PhysicsMaterial2D("ChaoSemAtrito") { friction = 0f, bounciness = 0f };

        for (int i = 0; i + 1 < Segments.Length; i++)
        {
            float g0 = Segments[i].x1;
            float g1 = Segments[i + 1].x0;
            Gaps.Add(new Vector2(g0, g1));
            float tuck = PitTuckPx / Ppu;
            Strip("Abismo_" + i, pit, g0 - tuck, g1 + tuck, groundBottom, -7, root.transform);
        }

        for (int i = 0; i < Segments.Length; i++)
            BuildSegment(i, Segments[i].x0, Segments[i].x1, stage, ground, edgeL, edgeR, groundBottom, slick, root.transform);

        foreach (var plat in Plats)
            BuildPlat(plat.sprite, plats[plat.sprite], plat.x, plat.height, stage, root.transform);

        BuildFallZone(stage, root.transform);
        Built = true;
        return true;
    }

    // Inimigos não nascem em cima de um gap.
    public static float SnapToGround(float x)
    {
        if (!Built)
            return x;
        foreach (var gap in Gaps)
        {
            if (x <= gap.x - SpawnMargin || x >= gap.y + SpawnMargin)
                continue;
            return x - gap.x < gap.y - x ? gap.x - SpawnMargin : gap.y + SpawnMargin;
        }
        return x;
    }

    static void BuildSegment(int index, float x0, float x1, StageData stage, Texture2D ground,
        Texture2D edgeL, Texture2D edgeR, float groundBottom, PhysicsMaterial2D slick, Transform parent)
    {
        bool openL = x0 <= -OpenEdgeX;
        bool openR = x1 >= OpenEdgeX;
        float edgeW = EdgeWidthPx / Ppu;
        // Beiradas cobrem 1 u + 8 px de sobreposição sobre o miolo.
        Strip("Chao_" + index, ground, openL ? x0 : x0 + 1f, openR ? x1 : x1 - 1f, groundBottom, -6, parent);
        if (!openL)
            Place("BeiradaEsq_" + index, Full(edgeL), new Vector3(x0 + edgeW * 0.5f, groundBottom, 0f), -5, parent);
        if (!openR)
            Place("BeiradaDir_" + index, Full(edgeR), new Vector3(x1 - edgeW * 0.5f, groundBottom, 0f), -5, parent);

        float c0 = openL ? x0 : x0 + EdgeInsetPx / Ppu;
        float c1 = openR ? x1 : x1 - EdgeInsetPx / Ppu;
        var go = new GameObject("ColisorChao_" + index);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((c0 + c1) * 0.5f, stage.GroundTop - GroundDepth * 0.5f, 0f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(c1 - c0, GroundDepth);
        collider.sharedMaterial = slick;
    }

    static void BuildPlat(string name, Texture2D texture, float x, float height, StageData stage, Transform parent)
    {
        float top = stage.GroundTop + height;
        float spriteBottom = top + PlatSurfacePx / Ppu - texture.height / Ppu;
        var go = Place("Ilha_" + name, Full(texture), new Vector3(x, spriteBottom, 0f), -4, parent);

        var solid = new GameObject("Colisor");
        solid.transform.SetParent(go.transform, false);
        solid.transform.position = new Vector3(x, top - PlatThickness * 0.5f, 0f);
        var collider = solid.AddComponent<BoxCollider2D>();
        collider.size = new Vector2((texture.width - 2 * PlatInsetPx) / Ppu, PlatThickness);
        collider.usedByEffector = true;
        var effector = solid.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;
    }

    static void BuildFallZone(StageData stage, Transform parent)
    {
        var go = new GameObject("ZonaDeQueda");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, stage.GroundTop - FallZoneDepth, 0f);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(stage.HalfWidth * 2f + 16f, 1f);
        go.AddComponent<T1FallZone>();
    }

    // Faixa horizontal com cópias do tile; a última é recortada no pixel exato.
    static void Strip(string name, Texture2D texture, float x0, float x1, float bottom, int order, Transform parent)
    {
        int totalPx = Mathf.RoundToInt((x1 - x0) * Ppu);
        int done = 0;
        int part = 0;
        while (done < totalPx)
        {
            int w = Mathf.Min(texture.width, totalPx - done);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, w, texture.height), new Vector2(0.5f, 0f), Ppu, 0,
                SpriteMeshType.FullRect);
            float cx = x0 + (done + w * 0.5f) / Ppu;
            Place(name + "_" + part, sprite, new Vector3(cx, bottom, 0f), order, parent);
            done += w;
            part++;
        }
    }

    static Texture2D Load(string name)
    {
        var texture = Resources.Load<Texture2D>(Folder + name);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    static Sprite Full(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), Ppu, 0,
            SpriteMeshType.FullRect);
    }

    static GameObject Place(string name, Sprite sprite, Vector3 position, int order, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return go;
    }
}

// Abaixo dos gaps: queda é morte (mesmo com i-frames/escudo). Jogador cai no painel "VOCÊ CAIU".
public class T1FallZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var body = other.attachedRigidbody;
        var target = body != null ? body.gameObject : other.gameObject;
        var health = target.GetComponent<HealthSystem>();
        if (health != null)
            health.Kill();
    }
}
