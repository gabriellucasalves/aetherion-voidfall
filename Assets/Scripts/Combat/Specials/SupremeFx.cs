using UnityEngine;

// Tinta de tela, flash, estrelas do céu, marcas vitais e textos flutuantes do Supremo.
// Sprites gerados em código: Point, sem mipmap. Corpo usa PPU 15.
public static class SupremeFx
{
    const float Ppu = 15f;

    static Sprite _dot;
    static Sprite _star;
    static Sprite _cross;
    static SupremeOverlay _tint;
    static SupremeOverlay _flash;
    static readonly System.Collections.Generic.List<SupremeOverlay> Stars = new System.Collections.Generic.List<SupremeOverlay>();

    public static void PlayFlash()
    {
        if (_flash != null)
            Object.Destroy(_flash.gameObject);

        var cam = Camera.main;
        _flash = SpawnQuad("FlashSupremo", cam != null ? cam.transform : null, Color.white, 40);
        if (_flash != null)
            _flash.PlayFade(0.16f);
    }

    public static void BeginTint(Color color)
    {
        ClearTint();
        var cam = Camera.main;
        _tint = SpawnQuad("TintaSupremo", cam != null ? cam.transform : null, color, 9);
    }

    public static void BeginSkyStars()
    {
        ClearStars();
        var cam = Camera.main;
        if (cam == null)
            return;

        float height = cam.orthographicSize * 2f;
        float width = height * Mathf.Max(0.5f, cam.aspect);
        Vector2[] spots =
        {
            new Vector2(-0.32f, 0.34f),
            new Vector2(-0.12f, 0.40f),
            new Vector2(0.06f, 0.30f),
            new Vector2(0.22f, 0.38f),
            new Vector2(-0.24f, 0.18f),
            new Vector2(0.02f, 0.16f),
            new Vector2(0.28f, 0.20f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            bool gold = i == spots.Length - 1;
            var color = gold ? new Color(1f, 0.84f, 0.25f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
            var star = SpawnQuad("EstrelaCeu", cam.transform, color, -47);
            if (star == null)
                continue;
            star.SetSprite(StarSprite());
            star.transform.localPosition = new Vector3(spots[i].x * width * 0.42f, spots[i].y * height * 0.42f, 10f);
            float s = gold ? 0.55f : 0.38f;
            star.transform.localScale = new Vector3(s, s, 1f);
            Stars.Add(star);
        }
    }

    public static void ClearAtmosphere()
    {
        ClearTint();
        ClearStars();
        if (_flash != null)
        {
            Object.Destroy(_flash.gameObject);
            _flash = null;
        }
    }

    public static void Popup(Vector3 world, string text, Color color)
    {
        var go = new GameObject("GritoMundo");
        go.transform.position = world;
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = UiKit.ResolveFont();
        mesh.fontSize = 42;
        mesh.characterSize = 0.065f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 28;
            if (mesh.font != null && mesh.font.material != null)
                renderer.sharedMaterial = mesh.font.material;
        }

        var popup = go.AddComponent<SupremePopup>();
        popup.Kick(color);
    }

    public static void SpawnMark(Transform enemy, int index)
    {
        if (enemy == null)
            return;

        var go = new GameObject("MarcaVital");
        go.transform.SetParent(enemy, false);
        float ox = ((index - 1) % 3 - 1) * 0.18f;
        go.transform.localPosition = new Vector3(ox, 0.85f, 0f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CrossSprite();
        renderer.sortingOrder = 23;
        renderer.color = new Color(1f, 0.84f, 0.25f, 1f);
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        go.AddComponent<SupremeMark>();
    }

    public static void ClearMarks()
    {
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].ClearVitalMarks();
        }
    }

    public static void Link(Vector3 a, Vector3 b, Color color)
    {
        Vector3 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.2f)
            return;

        int dots = Mathf.Clamp(Mathf.RoundToInt(length / 0.55f), 2, 5);
        for (int i = 0; i < dots; i++)
        {
            float t = dots == 1 ? 0.5f : i / (float)(dots - 1);
            var go = new GameObject("Elo");
            go.transform.position = Vector3.Lerp(a, b, t) + Vector3.up * 0.55f;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = DotSprite();
            renderer.color = color;
            renderer.sortingOrder = 21;
            go.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            Object.Destroy(go, 0.45f);
        }
    }

    static void ClearTint()
    {
        if (_tint != null)
        {
            Object.Destroy(_tint.gameObject);
            _tint = null;
        }
    }

    static void ClearStars()
    {
        for (int i = 0; i < Stars.Count; i++)
        {
            if (Stars[i] != null)
                Object.Destroy(Stars[i].gameObject);
        }
        Stars.Clear();
    }

    static SupremeOverlay SpawnQuad(string name, Transform parent, Color color, int order)
    {
        var cam = Camera.main;
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = DotSprite();
        renderer.color = color;
        renderer.sortingOrder = order;

        float height = cam != null ? cam.orthographicSize * 2f : 10.7f;
        float width = cam != null ? height * Mathf.Max(0.5f, cam.aspect) : 19f;
        // DotSprite tem 1 unidade (PPU = tamanho). Cobre a câmera.
        go.transform.localScale = new Vector3(width, height, 1f);
        go.transform.localPosition = new Vector3(0f, 0f, 10f);
        return go.AddComponent<SupremeOverlay>();
    }

    public static Sprite DotSprite()
    {
        if (_dot != null)
            return _dot;
        _dot = MakeSprite(4, 4, (x, y) => new Color32(255, 255, 255, 255), 4f, new Vector2(0.5f, 0.5f));
        return _dot;
    }

    static Sprite StarSprite()
    {
        if (_star != null)
            return _star;
        string[] art =
        {
            "......w......",
            "......w......",
            "......w......",
            "...wwwwwww...",
            ".....wyw.....",
            ".....wyw.....",
            "....w.w.w....",
            "...w..w..w...",
            "..w...w...w..",
            "......w......",
        };
        _star = ArtSprite(art, ch => ch == 'w'
            ? new Color32(255, 255, 255, 255)
            : ch == 'y'
                ? new Color32(255, 230, 140, 255)
                : new Color32(0, 0, 0, 0));
        return _star;
    }

    static Sprite CrossSprite()
    {
        if (_cross != null)
            return _cross;
        string[] art =
        {
            "..y....y..",
            ".yy....yy.",
            "..y.yy.y..",
            "...yyyy...",
            "..yyyyyy..",
            "..yyyyyy..",
            "...yyyy...",
            "..y.yy.y..",
            ".yy....yy.",
            "..y....y..",
        };
        _cross = ArtSprite(art, ch => ch == 'y'
            ? new Color32(255, 214, 64, 255)
            : new Color32(0, 0, 0, 0));
        return _cross;
    }

    public static Sprite ArtSprite(string[] art, System.Func<char, Color32> paint)
    {
        int h = art.Length;
        int w = art[0].Length;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = art[h - 1 - y];
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = paint(x < row.Length ? row[x] : '.');
        }

        return MakeSprite(w, h, pixels, Ppu, new Vector2(0.5f, 0.5f));
    }

    public static Sprite MakeSprite(int width, int height, System.Func<int, int, Color32> paint, float ppu, Vector2 pivot)
    {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            pixels[y * width + x] = paint(x, y);
        return MakeSprite(width, height, pixels, ppu, pivot);
    }

    public static Sprite MakeSprite(int width, int height, Color32[] pixels, float ppu, Vector2 pivot)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, ppu, 0, SpriteMeshType.FullRect);
    }
}
