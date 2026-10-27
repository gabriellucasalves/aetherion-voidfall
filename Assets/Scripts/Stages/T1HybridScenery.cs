using UnityEngine;

// Fundo híbrido da Fase I: céu Gate B.1 V3 + layout do conceito V2
// (guarda-sol, salgueiro, ruínas, água Aether, Cidadela Celeste ao fundo).
// Substitui só o céu, os planos distantes, a decoração frontal e o chão VISUAL do
// GoldenAgeScenery; plataformas, colisores, câmera e gameplay continuam iguais.
// Arte em Resources/T1_Hybrid (contrato T1: PPU 16, Point, pivô na base).
public static class T1HybridScenery
{
    public const bool DefaultEnabled = true;
    public const string EditorPrefKey = "Aetherion.T1Hybrid.Enabled";
    const string Folder = "T1_Hybrid/";
    const float Ppu = PixelArt.Ppu;

    // Raízes criadas pelo GoldenAgeScenery que o híbrido troca.
    static readonly string[] ReplacedRoots = { "CeuDourado", "ChaoVisual", "CenarioFrente" };

    // Superfície de caminhada do ground.png fica 9 px abaixo do topo do tile.
    const float GroundSurfacePx = 9f;

    // Nuvens independentes do céu (opcionais): arquivo, X do centro, Y da base, paralaxe.
    static readonly (string file, float x, float y, float factor)[] Clouds =
    {
        ("clouds_a", -8f, 3.7f, 0.92f),
        ("clouds_c", -1.5f, 5.3f, 0.90f),
        ("clouds_d", 4f, 6f, 0.88f),
        ("clouds_b", 8.5f, 3.1f, 0.90f),
    };

    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EditorPrefKey, DefaultEnabled);
#else
            string url = Application.absoluteURL ?? "";
            if (url.Contains("fundo=classico"))
                return false;
            if (url.Contains("fundo=hibrido"))
                return true;
            return DefaultEnabled;
#endif
        }
    }

    // Chamado depois do GoldenAgeScenery.Build. Sem arte híbrida, mantém o clássico.
    public static bool Apply(StageData stage)
    {
        var sky = Load("sky");
        var far = Load("far");
        var city = Load("city");
        var ground = Load("ground");
        if (sky == null || far == null || city == null || ground == null)
        {
            Debug.LogWarning("Fundo híbrido: faltam PNGs em Resources/T1_Hybrid — mantendo o fundo clássico.");
            return false;
        }

        foreach (var name in ReplacedRoots)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.Destroy(go);
        }

        var root = new GameObject("FundoHibrido_V3V2");
        float restCamY = stage.GroundTop + PixelPresentation.OrthoSize * 0.55f;

        // Céu: acompanha quase toda a câmera (sempre cobre o topo da tela).
        var skyGo = Place("Ceu", sky, Vector3.zero, -50, root.transform);
        skyGo.AddComponent<T1Backdrop>().Setup(0.95f, 0.95f, -3.0f - 0.95f * restCamY);

        foreach (var cloud in Clouds)
        {
            var sprite = Load(cloud.file);
            if (sprite == null)
                continue;
            Place("Nuvem_" + cloud.file, sprite, new Vector3(cloud.x, cloud.y, 0f), -48, root.transform)
                .AddComponent<T1HybridLayer>().Setup(cloud.factor, cloud.factor, restCamY);
        }

        Place("MontanhasCidadela", far, new Vector3(0f, -1.9f, 0f), -36, root.transform)
            .AddComponent<T1HybridLayer>().Setup(0.9f, 0.12f, restCamY);
        Place("FlorestaRuinas", city, new Vector3(0f, -3.7f, 0f), -30, root.transform)
            .AddComponent<T1HybridLayer>().Setup(0.8f, 0.1f, restCamY);

        BuildGround(stage, ground, root.transform);
        return true;
    }

    // Faixa de grama/terra tileável: cópias alternadas espelhadas, sem emenda visível.
    static void BuildGround(StageData stage, Sprite tile, Transform parent)
    {
        float tileW = tile.rect.width / Ppu;
        float bottomY = stage.GroundTop + GroundSurfacePx / Ppu - tile.rect.height / Ppu;
        float span = stage.HalfWidth * 2f + 16f;
        int count = Mathf.CeilToInt(span / tileW);
        float startX = -count * tileW * 0.5f + tileW * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var go = Place("ChaoHibrido_" + i, tile, new Vector3(startX + i * tileW, bottomY, 0f), -6, parent);
            go.GetComponent<SpriteRenderer>().flipX = i % 2 == 1;
        }
    }

    static Sprite Load(string name)
    {
        var texture = Resources.Load<Texture2D>(Folder + name);
        if (texture == null)
            return null;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0f), Ppu, 0, SpriteMeshType.FullRect);
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

// Paralaxe com referência na câmera em repouso: na altura padrão a camada fica
// exatamente onde foi composta; X e Y têm fatores próprios.
public class T1HybridLayer : MonoBehaviour
{
    float _factorX;
    float _factorY;
    float _restCamY;
    Vector3 _origin;

    public void Setup(float factorX, float factorY, float restCamY)
    {
        _factorX = factorX;
        _factorY = factorY;
        _restCamY = restCamY;
        _origin = transform.position;
        LateUpdate();
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var c = camera.transform.position;
        transform.position = new Vector3(
            _origin.x + c.x * _factorX,
            _origin.y + (c.y - _restCamY) * _factorY,
            _origin.z);
    }
}
