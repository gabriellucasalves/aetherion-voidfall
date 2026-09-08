using UnityEngine;

// Fundo compartilhado das telas de menu: céu do Vazio com lua corrompida em
// pixel art, fissura dentada pulsando, estrelas que piscam e a silhueta da
// cidade de Aetherion destruída no horizonte.
public class MenuBackground : MonoBehaviour
{
    const int StarCount = 56;

    Transform[] _stars;
    SpriteRenderer[] _starRenderers;
    float[] _speeds;
    float[] _twinkle;
    SpriteRenderer _rift;
    SpriteRenderer _riftGlow;

    void Start()
    {
        ConfigureCamera();
        CreateMoon();
        CreateRift();
        CreateCitySilhouette();
        CreateStars();
    }

    void Update()
    {
        // estrelas deslizam devagar e piscam fora de sincronia
        if (_stars != null)
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                star.position += Vector3.left * _speeds[i] * Time.deltaTime;
                if (star.position.x < -10f)
                    star.position = new Vector3(10f, star.position.y, 0f);

                float alpha = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * _twinkle[i] + i));
                var color = _starRenderers[i].color;
                color.a = alpha;
                _starRenderers[i].color = color;
            }
        }

        // a fissura respira — energia do Vazio nunca fica parada
        if (_rift != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.7f);
            _rift.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * pulse);
            if (_riftGlow != null)
                _riftGlow.color = new Color(1f, 1f, 1f, 0.16f + 0.14f * pulse);
        }
    }

    static void ConfigureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = MenuTheme.VoidBlack;
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    void CreateMoon()
    {
        var moon = new GameObject("LuaCorrompida");
        moon.transform.position = new Vector3(-5.6f, 2.1f, 0f);
        var renderer = moon.AddComponent<SpriteRenderer>();
        renderer.sprite = MoonSprite();
        renderer.sortingOrder = -20;

        // halo suave atrás da lua
        var halo = new GameObject("HaloLua");
        halo.transform.position = moon.transform.position;
        halo.transform.localScale = Vector3.one * 1.45f;
        var haloRenderer = halo.AddComponent<SpriteRenderer>();
        haloRenderer.sprite = renderer.sprite;
        haloRenderer.color = new Color(0.5f, 0.3f, 0.62f, 0.18f);
        haloRenderer.sortingOrder = -21;
    }

    void CreateRift()
    {
        var rift = new GameObject("Fissura");
        rift.transform.position = new Vector3(4.4f, 0.9f, 0f);
        rift.transform.rotation = Quaternion.Euler(0f, 0f, 14f);
        _rift = rift.AddComponent<SpriteRenderer>();
        _rift.sprite = RiftSprite();
        _rift.sortingOrder = -15;

        var glow = new GameObject("BrilhoFissura");
        glow.transform.position = rift.transform.position;
        glow.transform.rotation = rift.transform.rotation;
        glow.transform.localScale = new Vector3(3.2f, 1.05f, 1f);
        _riftGlow = glow.AddComponent<SpriteRenderer>();
        _riftGlow.sprite = _rift.sprite;
        _riftGlow.sortingOrder = -16;
    }

    void CreateCitySilhouette()
    {
        // duas camadas de torres arruinadas no horizonte
        Layer("CidadeFundo", -3.6f, 22f, 34, new Color(0.1f, 0.07f, 0.16f), -13);
        Layer("CidadeFrente", -4.1f, 24f, 26, new Color(0.055f, 0.04f, 0.1f), -12);
    }

    void Layer(string name, float baseY, float width, int towers, Color color, int order)
    {
        var seed = Random.state;
        Random.InitState(name.GetHashCode());
        for (int i = 0; i < towers; i++)
        {
            float x = -width * 0.5f + (width / towers) * (i + Random.Range(0.1f, 0.9f));
            float w = Random.Range(0.5f, 1.5f);
            float h = Random.Range(0.7f, 2.6f);
            var tower = PixelQuad(name + "_" + i, new Vector3(x, baseY + h * 0.5f, 0f), new Vector2(w, h), color);
            tower.GetComponent<SpriteRenderer>().sortingOrder = order;

            // topo quebrado: um bloco menor deslocado
            if (Random.value > 0.4f)
            {
                var stub = PixelQuad(name + "_topo_" + i,
                    new Vector3(x + Random.Range(-w, w) * 0.25f, baseY + h + 0.12f, 0f),
                    new Vector2(w * Random.Range(0.3f, 0.55f), 0.3f), color);
                stub.GetComponent<SpriteRenderer>().sortingOrder = order;
            }
        }
        Random.state = seed;
    }

    void CreateStars()
    {
        _stars = new Transform[StarCount];
        _starRenderers = new SpriteRenderer[StarCount];
        _speeds = new float[StarCount];
        _twinkle = new float[StarCount];

        for (int i = 0; i < StarCount; i++)
        {
            float x = Random.Range(-9.5f, 9.5f);
            float y = Random.Range(-2.6f, 4.8f);
            float size = Random.Range(0.04f, 0.11f);
            var star = PixelQuad($"Estrela_{i}", new Vector3(x, y, 0f), new Vector2(size, size), MenuTheme.SoftIvory);
            _starRenderers[i] = star.GetComponent<SpriteRenderer>();
            _starRenderers[i].sortingOrder = -18;
            _stars[i] = star.transform;
            _speeds[i] = Random.Range(0.1f, 0.6f);
            _twinkle[i] = Random.Range(0.6f, 2.4f);
        }
    }

    // lua em pixel art: círculo com sombra no canto, crateras e borda iluminada
    static Sprite MoonSprite()
    {
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        var baseColor = new Color32(64, 44, 92, 255);
        var lit = new Color32(96, 70, 130, 255);
        var dark = new Color32(40, 27, 62, 255);
        var rim = new Color32(140, 108, 176, 255);

        Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        float radius = size * 0.5f - 1f;

        // crateras fixas (x, y, raio)
        var craters = new[] { new Vector3(15f, 30f, 4f), new Vector3(30f, 16f, 5f), new Vector3(34f, 33f, 3f), new Vector3(20f, 12f, 2f) };

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), center);
            if (dist > radius)
            {
                pixels[y * size + x] = new Color32(0, 0, 0, 0);
                continue;
            }

            Color32 color = baseColor;
            // luz vinda do canto superior esquerdo
            if (x + y > size * 1.15f)
                color = dark;
            else if (x + y < size * 0.72f)
                color = lit;
            // borda iluminada no lado da luz
            if (dist > radius - 1.6f && x + y < size)
                color = rim;

            foreach (var crater in craters)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(crater.x, crater.y)) < crater.z)
                    color = dark;
            }

            pixels[y * size + x] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 12f, 0, SpriteMeshType.FullRect);
    }

    // fissura do Vazio: rachadura vertical dentada com núcleo claro
    static Sprite RiftSprite()
    {
        const int width = 18;
        const int height = 110;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        var core = new Color32(240, 130, 210, 255);
        var edge = new Color32(150, 48, 120, 255);

        var seed = Random.state;
        Random.InitState(7331);
        float wander = width * 0.5f;
        for (int y = 0; y < height; y++)
        {
            wander += Random.Range(-1.4f, 1.4f);
            wander = Mathf.Clamp(wander, 3f, width - 4f);
            // afina nas pontas
            float taper = Mathf.Clamp01(Mathf.Min(y, height - 1 - y) / (height * 0.22f));
            int half = Mathf.Max(0, Mathf.RoundToInt(2.4f * taper));
            int cx = Mathf.RoundToInt(wander);
            for (int x = cx - half - 1; x <= cx + half + 1; x++)
            {
                if (x < 0 || x >= width)
                    continue;
                bool isCore = Mathf.Abs(x - cx) <= half;
                pixels[y * width + x] = isCore ? core : edge;
            }
        }
        Random.state = seed;

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 14f, 0, SpriteMeshType.FullRect);
    }

    static GameObject PixelQuad(string name, Vector3 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite();
        renderer.color = color;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static Sprite WhiteSprite()
    {
        var texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }
}
