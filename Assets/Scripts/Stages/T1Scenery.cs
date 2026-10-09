using UnityEngine;

// Cenário 16-bit dark fantasy de Aetherion, montado em camadas modulares:
//   1. céu (T1Backdrop ~0.95)  2. fundo distante (0.9)  3. cidade em ruínas (0.8)
//   4. props próximos (0.4)    5. plano jogável (calçada + plataformas de ruína)
// As camadas são PNGs pixel art gerados por Assets/Art/T1/build_t1.py.
public static class T1Scenery
{
    const float Ppu = PixelArt.Ppu;

    public static Color PlatformColor => new Color(0.14f, 0.13f, 0.21f);
    public static Color PlatformEdge => new Color(0.30f, 0.30f, 0.44f);
    public static Color WoodColor => new Color(0.12f, 0.1f, 0.08f);

    public static void Build(StageData stage)
    {
        BuildLayered(stage);
    }

    static bool BuildLayered(StageData stage)
    {
        var sky = LoadSprite("sky");
        var far = LoadSprite("far");
        var city = LoadSprite("city");
        var pavement = LoadSprite("pavement");
        if (sky == null || far == null || city == null || pavement == null)
        {
            Debug.LogWarning("Camadas do T1 não encontradas em Resources/T1 — rode Assets/Art/T1/build_t1.py.");
            return false;
        }

        // camada 1 — céu noturno (lua, estrelas, nuvens, horizonte corrompido)
        var skyGo = Place("Ceu", sky, Vector3.zero, -50);
        skyGo.AddComponent<T1Backdrop>().Setup(0.95f, 0.95f, -5.8f);
        Glow(skyGo.transform, new Vector3((350f - 240f) / Ppu, 142f / Ppu, 0f),
            new Color(0.78f, 0.86f, 1f), 3.6f, 3.6f, 0.06f, 0.04f, 0.3f, -49);

        // camada 2 — silhueta dos templos e das torres-deus
        Place("FundoLonge", far, new Vector3(0f, -1.6f, 0f), -36)
            .AddComponent<ParallaxLayer>().Setup(0.9f);
        DriftMist("BrumaLonge", new Vector3(0f, 2.4f, 0f), 0.85f, -34, 0.16f, 2.6f, 0.045f);

        // camada 3 — templos, estátuas e portão circular
        var cityGo = Place("Cidade", city, new Vector3(0f, -3.0f, 0f), -30);
        cityGo.AddComponent<ParallaxLayer>().Setup(0.8f);
        CityGlow(cityGo.transform, 79, 33, 1.8f, 1.6f, 0.08f, 0.06f, 1.4f);  // têmpora da cabeça enterrada
        CityGlow(cityGo.transform, 470, 22, 2.0f, 1.6f, 0.12f, 0.10f, 2.2f);  // cristal do portão

        var near = LoadSprite("near");
        if (near != null)
        {
            Place("VeunePerto", near, new Vector3(0f, stage.GroundTop + 0.15f, 0f), 20)
                .AddComponent<ParallaxLayer>().Setup(0.25f);
        }
        DriftMist("BrumaMeio", new Vector3(4f, -0.6f, 0f), 0.7f, -12, 0.14f, 3.2f, 0.06f);

        // camada 4 — elementos próximos
        BuildProps(stage);

        // camada 5 — calçada do plano jogável (sobre o bloco de colisão)
        int tiles = Mathf.CeilToInt(stage.HalfWidth * 2f / 32f) + 2;
        int first = -tiles / 2;
        for (int i = 0; i < tiles; i++)
            Place("Calcada" + i, pavement, new Vector3((first + i) * 32f, stage.GroundTop - 2.65f, 0f), -4);

        // camada 5b — decoração fixa do plano jogável (estilo SNES, sem parallax)
        BuildGameplayDecor(stage);
        BuildKitWalk(stage);

        DriftMist("BrumaFrente", new Vector3(-4f, -2.3f, 0f), 0.88f, 25, 0.10f, 4f, 0.08f);
        return true;
    }

    static void BuildProps(StageData stage)
    {
        float y = stage.GroundTop - 0.35f;
        NearProp("tree", -26f, y, -7);
        NearProp("statue", -17f, y, -6);
        NearProp("rubble", -9f, y, -6);
        var lampA = NearProp("lamp", -2.5f, y, -6);
        NearProp("column", 5f, y, -7);
        NearProp("tree", 13f, y, -7);
        NearProp("rubble", 20f, y, -6);
        var lampB = NearProp("lamp", 27f, y, -6);
        var fireColor = new Color(1f, 0.6f, 0.2f);
        if (lampA != null)
            Glow(lampA.transform, new Vector3(0f, 3.25f, 0f), fireColor, 1.1f, 1.1f, 0.14f, 0.12f, 3.4f, -5);
        if (lampB != null)
            Glow(lampB.transform, new Vector3(0f, 3.25f, 0f), fireColor, 1.1f, 1.1f, 0.14f, 0.12f, 3.4f, -5);
    }

    // Objetos fixos no plano jogável: ficam parados no mundo (parallax 1.0),
    // atrás do herói mas na frente da calçada — integram a plataforma ao cenário.
    static void BuildGameplayDecor(StageData stage)
    {
        float y = stage.GroundTop - 0.12f;

        WorldProp("ruin_wall", -28f, y, -3);
        WorldProp("stones", -24f, y, -1);
        WorldProp("statue_warrior", -20.5f, y, -2);
        WorldProp("fallen", -14f, y, -3);
        WorldProp("bush", -11.2f, y, -1);
        OrbGlow(WorldProp("statue_mage", -6.5f, y, -2));
        WorldProp("stones", -0.8f, y, -1);
        WorldProp("bush", 3.1f, y, -1);
        WorldProp("fallen", 6.4f, y, -3);
        WorldProp("bush", 10.8f, y, -1);
        WorldProp("statue_warrior", 15.8f, y, -2);
        WorldProp("stones", 19.6f, y, -1);
        WorldProp("ruin_wall", 25.5f, y, -3);
        OrbGlow(WorldProp("statue_mage", 29.5f, y, -2));
        WorldProp("bush", 32f, y, -1);

        // primeiro plano: silhuetas na frente do herói, mais escuras e maiores,
        // com parallax invertido leve (passam mais rápido que a fase = mais perto)
        ForeProp("fore_thorns", -22f, stage.GroundTop - 0.6f);
        ForeProp("fore_thorns", -3.5f, stage.GroundTop - 0.65f);
        ForeProp("fore_thorns", 16.5f, stage.GroundTop - 0.6f);
        ForeProp("fore_thorns", 27.5f, stage.GroundTop - 0.65f);
        ForeProp("stones", -18f, stage.GroundTop - 0.55f);
        ForeProp("bush", -8f, stage.GroundTop - 0.5f);
        ForeProp("stones", 1.5f, stage.GroundTop - 0.55f);
        ForeProp("bush", 12f, stage.GroundTop - 0.5f);
        ForeProp("stones", 22f, stage.GroundTop - 0.55f);
        ForeProp("bush", 31f, stage.GroundTop - 0.5f);
    }

    // Luz roxa pulsando no orbe caído da estátua de mago
    static void OrbGlow(GameObject statue)
    {
        if (statue == null)
            return;
        // orbe está no px (9, 15) do sprite 44x88 (pivô bottom-center)
        Glow(statue.transform, new Vector3((9f - 22f) / Ppu, 15f / Ppu, 0f),
            new Color(0.72f, 0.45f, 1f), 0.7f, 0.7f, 0.14f, 0.12f, 2.0f, -1);
    }

    static GameObject WorldProp(string name, float x, float y, int order)
    {
        var sprite = LoadSprite(name);
        if (sprite == null)
            return null;
        return Place(name, sprite, new Vector3(x, y, 0f), order);
    }

    static void ForeProp(string name, float x, float y)
    {
        var go = WorldProp(name, x, y, 30);
        if (go == null)
            return;
        go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.color = new Color(0.4f, 0.42f, 0.55f); // silhueta escura e fria
        go.AddComponent<ParallaxLayer>().Setup(-0.12f);
    }

    static GameObject NearProp(string name, float x, float y, int order)
    {
        var sprite = LoadSprite(name);
        if (sprite == null)
            return null;
        var go = Place(name, sprite, new Vector3(x, y, 0f), order);
        go.AddComponent<ParallaxLayer>().Setup(0.4f);
        return go;
    }

    // Plataforma de ruína pré-desenhada (bordas irregulares, musgo, vinhas).
    // Só visual — o colisor é criado pelo GroundT1Controller.
    public static void RuinPlatform(Vector3 center, float width, float thickness, int order)
    {
        var texture = Resources.Load<Texture2D>("T1/plat" + Mathf.RoundToInt(width * 100f));
        if (texture == null)
        {
            TiledBlock("PlataformaRuina", center, new Vector2(width, thickness), order);
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        // corpo de pedra ocupa [10, 10+6] px; pivô no centro do corpo
        float pivotY = (10f + thickness * Ppu * 0.5f) / texture.height;
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, pivotY),
            Ppu,
            0,
            SpriteMeshType.FullRect);

        var go = new GameObject("PlataformaRuina");
        go.transform.position = center;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
    }

    // ---------- blocos tileáveis (corpo do chão / fallback) ----------

    static Sprite _brickSprite;

    public static GameObject TiledBlock(string name, Vector3 position, Vector2 size, int order)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BrickTile();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        renderer.sortingOrder = order;
        return go;
    }

    static Sprite BrickTile()
    {
        if (_brickSprite != null)
            return _brickSprite;

        const int s = 16;
        var texture = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var pixels = new Color[s * s];
        var mortar = new Color(0.045f, 0.05f, 0.085f);
        var baseA = new Color(0.13f, 0.12f, 0.20f);
        var baseB = new Color(0.11f, 0.105f, 0.18f);
        var lit = new Color(0.20f, 0.19f, 0.30f);
        var moss = new Color(0.10f, 0.17f, 0.12f);

        for (int y = 0; y < s; y++)
        {
            int row = y / 8;
            int offset = row % 2 == 0 ? 0 : 4;
            for (int x = 0; x < s; x++)
            {
                int bx = (x + offset) % 8;
                bool mortarLine = y % 8 == 7 || bx == 7;
                int brickId = (x + offset) / 8 + row * 3;
                Color c = mortarLine ? mortar : (brickId % 2 == 0 ? baseA : baseB);
                if (!mortarLine && y % 8 == 6)
                    c = lit;
                if (!mortarLine && (x * 7 + y * 13) % 29 == 0)
                    c = moss;
                pixels[y * s + x] = c;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        _brickSprite = Sprite.Create(
            texture, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        return _brickSprite;
    }

    // ---------- neblina em deriva ----------

    static void DriftMist(string name, Vector3 position, float parallax, int order, float alpha, float amplitude, float speed)
    {
        int w = 512;
        int h = 40;
        var pixels = PixelArt.Clear(w, h);
        for (int y = 0; y < h; y++)
        {
            float fade = 1f - Mathf.Abs(y - h * 0.5f) / (h * 0.5f);
            for (int x = 0; x < w; x++)
            {
                float edge = Mathf.Clamp01(Mathf.Min(x, w - 1 - x) / 90f);
                float wave = 0.75f + 0.25f * Mathf.Sin(x * 0.045f + y * 0.3f);
                float a = fade * edge * wave * alpha;
                PixelArt.Plot(pixels, w, h, x, y, new Color(0.55f, 0.6f, 0.75f, a));
            }
        }

        var go = PixelArt.Place(name, PixelArt.Make(w, h, pixels), position, order, 0f);
        go.AddComponent<T1Mist>().Setup(parallax, amplitude, speed);
    }

    // ---------- luz pulsante ----------

    static Sprite _glowSprite;

    static void CityGlow(Transform city, int pxX, int pxY, float sizeX, float sizeY, float baseAlpha, float amplitude, float speed)
    {
        Glow(city, new Vector3((pxX - 320f) / Ppu, pxY / Ppu, 0f),
            new Color(1f, 0.5f, 0.16f), sizeX, sizeY, baseAlpha, amplitude, speed, -29);
    }

    static void Glow(
        Transform parent, Vector3 localPosition, Color color,
        float sizeX, float sizeY, float baseAlpha, float amplitude, float speed, int order)
    {
        var go = new GameObject("Brilho");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(sizeX, sizeY, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GlowSprite();
        renderer.sortingOrder = order;
        go.AddComponent<T1Glow>().Setup(renderer, color, baseAlpha, amplitude, speed);
    }

    static Sprite GlowSprite()
    {
        if (_glowSprite != null)
            return _glowSprite;

        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Round(a * a * 6f) / 6f; // degraus = luz pixelada
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        _glowSprite = Sprite.Create(
            texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        return _glowSprite;
    }

    // ---------- utilidades ----------

    static readonly System.Collections.Generic.Dictionary<string, Sprite> _sprites =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    static Sprite LoadSprite(string name)
    {
        if (_sprites.TryGetValue(name, out var cached) && cached != null)
            return cached;

        var texture = Resources.Load<Texture2D>("T1/" + name);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0f),
            Ppu,
            0,
            SpriteMeshType.FullRect);
        _sprites[name] = sprite;
        return sprite;
    }

    // ENV-003: caminhada de descoberta, da esquerda (spawn) para a direita.
    static void BuildKitWalk(StageData stage)
    {
        float g = stage.GroundTop;

        // ruína pequena
        Kit("ruina_baixa", -26f, g, 1);
        Kit("ruina_alta", -22.5f, g, 1);
        Kit("pedra_2", -20.2f, g, 2);
        Kit("musgo_faixa", -24f, g, 2);
        Kit("raiz_b", -21f, g, 2);

        // fragmentos da estátua
        Kit("pedra_1", -2f, g, 2);
        Kit("pedra_4", 0.4f, g, 2);
        Kit("fragmento_anel", 2.2f, g, 3);
        Kit("fragmento_olho", 3.6f, g, 3);
        Kit("divindade_mao", 5.2f, g, 3);
        Kit("musgo_tufo", 1.2f, g, 2);
        Kit("raiz_a", 6.5f, g, 2);

        // cabeça enterrada
        Kit("divindade_pedestal", 28f, g, 2);
        Kit("divindade_cabeca", 31.2f, g, 4);
        Kit("piso_simbolo", 26f, g, -2);
        Kit("musgo_borda", 33.5f, g, 2);
        Kit("planta_haste", 24.5f, g, 3);
        Kit("pedra_3", 34.8f, g, 2);

        // primeiro monumento
        Kit("coluna_quebrada", 50f, g, 2);
        Kit("parede_simbolo", 54f, g, 1);
        Kit("coluna_lisa", 58f, g, 2);
        Kit("altar_pequeno", 52.5f, g, 3);
        Kit("simbolo_anel", 56.5f, g, 3);
        Kit("planta_folha", 48.5f, g, 3);

        // templo
        Kit("parede_rachada", 74f, g, 1);
        Kit("arco_inteiro", 78.5f, g, 2);
        Kit("coluna_inteira", 83f, g, 3);
        Kit("degrau", 76f, g, -1);
        Kit("musgo_tufo", 81f, g, 3);
        Kit("fungo", 73f, g, 3);
        Kit("raiz_c", 85f, g, 2);

        // grande divindade quebrada
        Kit("divindade_quebrada", 110f, g, 3);
        Kit("divindade_braco", 115.5f, g, 4);
        Kit("fragmento_anel", 107f, g, 4);
        Kit("musgo_faixa", 112f, g, 2);
        Kit("planta_cristal", 117.5f, g, 3);

        var portal = Kit("porta_selo", 140f, g, 3);
        Kit("coluna_inteira", 136.2f, g, 2);
        Kit("coluna_lisa", 143.8f, g, 2);
        if (portal != null)
        {
            Glow(portal.transform, new Vector3(0f, 1.3f, 0f),
                new Color(0.35f, 0.9f, 0.82f), 1.4f, 1.6f, 0.16f, 0.12f, 1.8f, 2);
        }
        Kit("cristal_baixo", 144f, g, 4);
    }

    static GameObject Kit(string name, float x, float y, int order)
    {
        var sprite = LoadKit(name);
        if (sprite == null)
            return null;
        return Place("Kit_" + name, sprite, new Vector3(x, y, 0f), order);
    }

    static Sprite LoadKit(string name)
    {
        string key = "kit:" + name;
        if (_sprites.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var texture = Resources.Load<Texture2D>("Kit/" + name);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0f),
            Ppu,
            0,
            SpriteMeshType.FullRect);
        _sprites[key] = sprite;
        return sprite;
    }

    static GameObject Place(string name, Sprite sprite, Vector3 position, int order)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return go;
    }
}

// Acompanha a câmera com fator de parallax no X (e leve no Y),
// mantendo o fundo sempre cobrindo a visão.
public class T1Backdrop : MonoBehaviour
{
    float _factorX;
    float _factorY;
    float _yOffset;

    public void Setup(float factorX, float factorY, float yOffset)
    {
        _factorX = factorX;
        _factorY = factorY;
        _yOffset = yOffset;
        Follow();
    }

    void LateUpdate()
    {
        Follow();
    }

    void Follow()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var p = camera.transform.position;
        transform.position = new Vector3(p.x * _factorX, p.y * _factorY + _yOffset, 5f);
    }
}

// Neblina: acompanha a câmera com parallax e deriva de um lado pro outro.
public class T1Mist : MonoBehaviour
{
    float _parallax;
    float _amplitude;
    float _speed;
    float _seed;
    Vector3 _origin;

    public void Setup(float parallax, float amplitude, float speed)
    {
        _parallax = parallax;
        _amplitude = amplitude;
        _speed = speed;
        _seed = Random.Range(0f, 10f);
        _origin = transform.position;
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var c = camera.transform.position;
        float drift = Mathf.Sin(Time.time * _speed + _seed) * _amplitude;
        transform.position = new Vector3(
            _origin.x + c.x * _parallax + drift,
            _origin.y + c.y * _parallax * 0.18f,
            0f);
    }
}

// Pulso suave de luz (ruído Perlin) para lua, fogueiras e tochas.
public class T1Glow : MonoBehaviour
{
    SpriteRenderer _renderer;
    Color _color;
    float _base;
    float _amplitude;
    float _speed;
    float _seed;

    public void Setup(SpriteRenderer renderer, Color color, float baseAlpha, float amplitude, float speed)
    {
        _renderer = renderer;
        _color = color;
        _base = baseAlpha;
        _amplitude = amplitude;
        _speed = speed;
        _seed = Random.Range(0f, 100f);
        Apply(0.5f);
    }

    void LateUpdate()
    {
        if (_renderer == null)
            return;

        float noise = Mathf.PerlinNoise(_seed, Time.time * _speed);
        Apply(noise);
    }

    void Apply(float t)
    {
        var c = _color;
        c.a = _base + _amplitude * t;
        _renderer.color = c;
    }
}
