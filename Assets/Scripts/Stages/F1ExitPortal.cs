using UnityEngine;

// Portal rúnico 16-bit com acabamento pixel art refinado (brilho pulsante, partículas de runas, point filter).
// A lógica de ativação, colisão e transição permanece 100% idêntica.
public class F1ExitPortal : MonoBehaviour
{
    GroundT1Controller _stage;
    bool _used;
    SpriteRenderer _renderer;
    Sprite[] _frames;
    float _clock;

    // Elementos visuais
    SpriteRenderer _backGlow;
    SpriteRenderer _innerGlow;
    SpriteRenderer _groundGlow;
    Mote[] _motes;

    struct Mote
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public float XOffset;
        public float Y;
        public float Speed;
        public float Phase;
        public float MaxLife;
        public float Life;
        public Color BaseColor;
    }

    static Sprite _pixelGlowSprite;
    static Sprite _pixelDotSprite;

    static Sprite GetGlowSprite()
    {
        if (_pixelGlowSprite != null) return _pixelGlowSprite;
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
                a = Mathf.Round(a * a * 6f) / 6f; // degraus pixel art
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        _pixelGlowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        return _pixelGlowSprite;
    }

    static Sprite GetDotSprite()
    {
        if (_pixelDotSprite != null) return _pixelDotSprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var pixels = new Color32[] { Color.white, Color.white, Color.white, Color.white };
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        _pixelDotSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        return _pixelDotSprite;
    }

    public static void Spawn(GroundT1Controller stage, Vector2 feet)
    {
        var go = new GameObject("PortalFimFaseI");
        go.transform.position = feet;
        var portal = go.AddComponent<F1ExitPortal>();
        portal._stage = stage;
        var texture = Resources.Load<Texture2D>("Effects/portal_f1");
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var r = go.AddComponent<SpriteRenderer>();
            int frameW = texture.height == 64 && texture.width >= 192 ? 48 : texture.width;
            int count = Mathf.Max(1, texture.width / frameW);
            portal._frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                portal._frames[i] = Sprite.Create(texture, new Rect(i * frameW, 0, frameW, texture.height), new Vector2(0.5f, 0f), 16f);
            r.sprite = portal._frames[0];
            r.sortingOrder = 10;
            portal._renderer = r;
        }

        // Acabamento visual: brilho pulsante no fundo (sortingOrder 9)
        var glowGo = new GameObject("PortalBackGlow");
        glowGo.transform.SetParent(go.transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        glowGo.transform.localScale = new Vector3(1.6f, 2.2f, 1f);
        var bgSr = glowGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetGlowSprite();
        bgSr.sortingOrder = 9;
        bgSr.color = new Color(0.25f, 0.72f, 1f, 0.45f);
        portal._backGlow = bgSr;

        // Brilho do chão sob o portal (sortingOrder 8)
        var groundGo = new GameObject("PortalGroundGlow");
        groundGo.transform.SetParent(go.transform, false);
        groundGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        groundGo.transform.localScale = new Vector3(1.8f, 0.5f, 1f);
        var ggSr = groundGo.AddComponent<SpriteRenderer>();
        ggSr.sprite = GetGlowSprite();
        ggSr.sortingOrder = 8;
        ggSr.color = new Color(0.4f, 0.85f, 1f, 0.35f);
        portal._groundGlow = ggSr;

        // Brilho do núcleo interno (sortingOrder 11)
        var innerGo = new GameObject("PortalInnerGlow");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        innerGo.transform.localScale = new Vector3(0.9f, 1.4f, 1f);
        var igSr = innerGo.AddComponent<SpriteRenderer>();
        igSr.sprite = GetGlowSprite();
        igSr.sortingOrder = 11;
        igSr.color = new Color(1f, 0.95f, 0.6f, 0.25f);
        portal._innerGlow = igSr;

        // Partículas pixel art subindo ao redor do portal (sortingOrder 12)
        var dotSpr = GetDotSprite();
        var colors = new[]
        {
            new Color(0.35f, 0.85f, 1f, 0.9f),
            new Color(1f, 0.9f, 0.45f, 0.9f),
            new Color(0.7f, 0.95f, 1f, 0.95f),
            new Color(0.5f, 0.7f, 1f, 0.85f)
        };
        portal._motes = new Mote[8];
        for (int i = 0; i < portal._motes.Length; i++)
        {
            var pGo = new GameObject("Mote_" + i);
            pGo.transform.SetParent(go.transform, false);
            var sr = pGo.AddComponent<SpriteRenderer>();
            sr.sprite = dotSpr;
            sr.sortingOrder = i % 2 == 0 ? 12 : 9;
            float maxLife = 1.4f + (i * 0.25f);
            portal._motes[i] = new Mote
            {
                Transform = pGo.transform,
                Renderer = sr,
                XOffset = ((i % 4) - 1.5f) * 0.42f,
                Y = (i * 0.35f) % 3.0f,
                Speed = 1.1f + (i * 0.15f),
                Phase = i * 1.25f,
                MaxLife = maxLife,
                Life = (i * 0.3f) % maxLife,
                BaseColor = colors[i % colors.Length]
            };
        }

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 3.2f);
        trigger.offset = Vector2.up * 1.6f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;

        // Animação dos frames do portal nativo
        if (_renderer != null && _frames != null && _frames.Length > 1)
        {
            _clock += dt * 8f;
            _renderer.sprite = _frames[(int)_clock % _frames.Length];
            // Pulso sutil de energia rúnica
            float pulse = Mathf.PingPong(time * 2f, 1f);
            _renderer.color = Color.Lerp(Color.white, new Color(0.9f, 0.98f, 1f), pulse);
        }

        // Pulso do brilho de fundo
        if (_backGlow != null)
        {
            float bgAlpha = 0.35f + 0.15f * Mathf.Sin(time * 3f);
            _backGlow.color = new Color(0.25f, 0.72f, 1f, bgAlpha);
            float scaleY = 2.2f + 0.15f * Mathf.Cos(time * 2.5f);
            _backGlow.transform.localScale = new Vector3(1.6f, scaleY, 1f);
        }

        // Pulso do brilho interno
        if (_innerGlow != null)
        {
            float igAlpha = 0.20f + 0.10f * Mathf.Sin(time * 4.5f);
            _innerGlow.color = new Color(1f, 0.95f, 0.6f, igAlpha);
        }

        // Atualização dos motes pixel art subindo
        if (_motes != null)
        {
            const float ppu = 16f;
            for (int i = 0; i < _motes.Length; i++)
            {
                var m = _motes[i];
                m.Life += dt;
                m.Y += m.Speed * dt;
                if (m.Life >= m.MaxLife || m.Y > 3.4f)
                {
                    m.Life = 0f;
                    m.Y = 0.1f + Random.Range(0f, 0.3f);
                    m.XOffset = Random.Range(-0.75f, 0.75f);
                }

                float driftX = m.XOffset + Mathf.Sin(time * 2f + m.Phase) * 0.12f;
                // Alinha na grade de pixels (PPU 16) para estética pixel art
                float snappedX = Mathf.Round(driftX * ppu) / ppu;
                float snappedY = Mathf.Round(m.Y * ppu) / ppu;
                m.Transform.localPosition = new Vector3(snappedX, snappedY, 0f);

                // Fade in e fade out com a vida útil
                float progress = m.Life / m.MaxLife;
                float alpha = progress < 0.2f ? (progress / 0.2f) : (1f - (progress - 0.2f) / 0.8f);
                var c = m.BaseColor;
                c.a *= Mathf.Clamp01(alpha);
                m.Renderer.color = c;

                _motes[i] = m;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || other.GetComponentInParent<PlayerController>() == null) return;
        _used = true;
        if (_stage != null) _stage.OnDemoComplete();
    }
}
