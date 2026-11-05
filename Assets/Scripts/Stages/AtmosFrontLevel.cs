using System.Collections.Generic;
using UnityEngine;

// LAYOUT_GAPS, PPU 32: trecho aprovado de 24 u no início de uma fase de 72 u.
// Decisão da Direção: somente dois gaps; o Ground R continua sólido até o fim.
public static class AtmosFrontLevel
{
    public const float Width = 24f;
    public const float SurfaceRow = 392f;
    public const float GroundSurfacePx = 8f;
    const float Thickness = 0.375f;
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
    public static void Reset() { _gaps.Clear(); _platforms.Clear(); }
    public static float X(StageData stage, float pixelX) => -stage.HalfWidth + pixelX / 32f;
    public static float Y(StageData stage, float pixelY) => stage.GroundTop + (SurfaceRow - pixelY) / 32f;

    public static void Build(StageData stage, Sprite playfield, Transform parent)
    {
        Reset();
        var root = new GameObject("P5_FrenteJogavel");
        root.transform.SetParent(parent, false);
        var visual = new GameObject("TrechoOriginal_P5");
        visual.transform.SetParent(root.transform, false);
        visual.transform.position = new Vector3(X(stage, 384f), Y(stage, 216f), 0f);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = playfield;
        renderer.sortingOrder = 0;

        var slick = new PhysicsMaterial2D("AtmosSemAtrito") { friction = 0f, bounciness = 0f };
        Ground("Ground_L", X(stage, 0), X(stage, 240), stage.GroundTop, slick, root.transform);
        Ground("IlhaMid", X(stage, 320), X(stage, 460), stage.GroundTop, slick, root.transform);
        Ground("Ground_R", X(stage, 560), stage.HalfWidth, stage.GroundTop, slick, root.transform);
        _gaps.Add(new Vector2(X(stage, 240), X(stage, 320)));
        _gaps.Add(new Vector2(X(stage, 460), X(stage, 560)));
        BuildPlatform("A", 200, 280, 300, stage, root.transform);
        BuildPlatform("B", 480, 580, 270, stage, root.transform);
        BuildPlatform("C", 620, 700, 240, stage, root.transform);
        ExtendGround(stage, root.transform);

        var fall = new GameObject("Atmos_ZonaDeQueda");
        fall.transform.SetParent(root.transform, false);
        fall.transform.position = new Vector3(0f, stage.GroundTop - 4.5f, 0f);
        var trigger = fall.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(stage.HalfWidth * 2f + 16f, 1f);
        fall.AddComponent<T1FallZone>();
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
    static void BuildPlatform(string name, int left, int right, int surface, StageData stage, Transform parent)
    {
        float x0 = X(stage, left), x1 = X(stage, right), top = Y(stage, surface);
        _platforms.Add(new Platform(name, x0, x1, top));
        var go = new GameObject("Plat_" + name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, top - Thickness * 0.5f, 0f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(x1 - x0, Thickness);
        collider.usedByEffector = true;
        var effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;
    }
    static void ExtendGround(StageData stage, Transform parent)
    {
        float left = X(stage, 768f), right = stage.HalfWidth;
        if (right <= left) return;
        var texture = Resources.Load<Texture2D>(T1AtmosScenery.Folder + "ground");
        int total = Mathf.RoundToInt((right - left) * 32f);
        float bottom = stage.GroundTop + GroundSurfacePx / 32f - texture.height / 32f;
        int done = 0;
        while (done < total)
        {
            int width = Mathf.Min(texture.width, total - done);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, texture.height), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
            var go = new GameObject("Ground_Extensao_" + done);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(left + (done + width * 0.5f) / 32f, bottom, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;
            done += width;
        }
    }
}
