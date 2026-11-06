using UnityEngine;

// Integra o pack intacto, com PPU próprio. Não muda o contrato PPU 16 dos personagens.
public static class T1AtmosScenery
{
    public const string Folder = "T1_Hybrid_Atmos/";
    public const string EditorPrefKey = "Aetherion.AtmosV1.Enabled";
    public const bool DefaultEnabled = true;
    public const bool AllowLegacyFallback = false;
    public const float Ppu = 32f;
    public const float GroundBelowCenter = 5.5f;
    public const float PlayerJumpSpeed = 15.6f;
    public static bool Built { get; private set; }
    static GameObject _root;
    static Material Grade(bool far, Texture texture, AtmosV1Root owner)
    {
        var material = new Material(Resources.Load<Shader>("AtmosDepth"));
        material.mainTexture = texture;
        material.SetFloat("_Saturation", far ? .65f : .92f);
        material.SetFloat("_Contrast", far ? .96f : 1.03f);
        owner.Own(material);
        return material;
    }

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
        _root = null;
        AtmosFrontLevel.Reset();
    }
    public static void ConfigureStage(StageData stage)
    {
        // Mantém 72 u; apenas a origem vertical é alinhada à grade nativa do pack.
        stage.GroundTop = AtmosV1Parallax.Snap(stage.GroundTop);
    }
    public static Sprite Load(string name)
    {
        var texture = Resources.Load<Texture2D>(Folder + name);
        if (texture == null) return null;
        // O contrato também é aplicado aos sprites criados em runtime, sem alterar os pixels.
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = name == "ground" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
    }
    public static bool Build(StageData stage)
    {
        Reset();
        var ground = Resources.Load<Texture2D>(Folder + "ground");
        if (ground == null) { Debug.LogError("Atmos V1: ground.png ausente"); return false; }
        var names = new[] { "layer_p1", "layer_p2", "overlay_p2b_fog", "layer_p3", "overlay_p3b_veil", "layer_p4", "layer_p5" };
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            sprites[i] = Load(names[i]);
            if (sprites[i] == null || sprites[i].rect.width != 768 || sprites[i].rect.height != 432)
            {
                Debug.LogError("Atmos V1: camada ausente ou dimensões inválidas: " + names[i]);
                return false;
            }
        }
        var root = new GameObject("Cenario_AtmosV1");
        var owner = root.AddComponent<AtmosV1Root>();
        var reference = new Vector2(-stage.HalfWidth + 12f, stage.GroundTop + GroundBelowCenter);
        float[] factors = { 0.08f, 0.22f, 0.22f, 0.40f, 0.40f, 0.62f };
        int[] orders = { -50, -40, -35, -30, -25, -20 };
        for (int i = 0; i < factors.Length; i++)
        {
            var group = new GameObject(names[i]);
            group.transform.SetParent(root.transform, false);
            var grade = i == 0 || i == 1 || i == 3 || i == 5 ? Grade(i < 2, sprites[i].texture, owner) : null;
            for (int copy = -1; copy <= 1; copy++)
            {
                var tile = new GameObject("Copia_" + copy);
                tile.transform.SetParent(group.transform, false);
                tile.transform.localPosition = new Vector3(copy * 24f, 0, 0);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = orders[i];
                if (i == 0 || i == 1)
                {
                    renderer.sharedMaterial = grade;
                    renderer.color = i == 0 ? new Color(.77f,.82f,.92f) : new Color(.76f,.82f,.94f);
                }
                else if (i == 3 || i == 5) renderer.sharedMaterial = grade;
                // Fog e veil conservam o alpha original e ficam entre seus planos.

            }
            group.AddComponent<AtmosV1Parallax>().Setup(factors[i], reference, i == 0);
        }
        // Dois gaps no início; a extensão restante não acrescenta abismos.
        AtmosFrontLevel.Build(stage, sprites[6], root.transform);
        _root = root;
        Built = true;
        return true;
    }
    public static void ClearIfOwner(GameObject owner)
    {
        if (_root == owner) Reset();
    }
}
