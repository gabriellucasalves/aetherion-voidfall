using UnityEngine;
using UnityEngine.UI;

// Fundo pintado em camadas das telas de menu (título, intro, seleção).
// Substitui o MenuBackground procedural nessas três cenas. Canvas próprio atrás do menu (sortingOrder -10),
// escala "envelope" (cobre a tela toda, corta as bordas em telas muito largas/altas).
// Root = área 480x270 em pixels nativos (origem no canto superior esquerdo) para o controller pôr os heróis
// alinhados ao chão do cenário.
public class MenuAtmosBackground : MonoBehaviour
{
    public enum Mode { Titulo, Intro, Selecao }

    const string Dir = "Menu/titulo/";
    public Mode mode = Mode.Titulo;
    public RectTransform Root { get; private set; }
    public static MenuAtmosBackground Current { get; private set; }

    RectTransform _canvasRect;
    RectTransform[] _layers;
    float[] _depth;              // deslocamento máximo (px nativos) por camada no parallax do ponteiro
    RawImage _fogBack, _fogFront;
    Image _riftGlow;
    Vector2 _lastSize;
    Vector2 _parallax;
    Particle[] _particles;

    struct Particle { public Image img; public Vector2 pos; public Vector2 vel; public float life; public bool ember; }

    public static MenuAtmosBackground Create(Transform owner, Mode mode)
    {
        var bg = owner.gameObject.AddComponent<MenuAtmosBackground>();
        bg.mode = mode;
        bg.Build();
        return bg;
    }

    void Build()
    {
        Current = this;
        EnsureCamera();

        var go = new GameObject("FundoMenuCanvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasRect = (RectTransform)go.transform;

        var rootGo = new GameObject("Cenario480x270", typeof(RectTransform));
        rootGo.transform.SetParent(go.transform, false);
        Root = (RectTransform)rootGo.transform;
        Root.anchorMin = Root.anchorMax = Root.pivot = new Vector2(0.5f, 0.5f);
        Root.sizeDelta = new Vector2(1920f, 1080f);

        var ceu = MenuKit.Img(Root, MenuKit.Sprite(Dir + "titulo_ceu_480x270"), -8, -8, 496, 286);
        _riftGlow = MenuKit.Img(ceu.transform, MenuKit.Sprite(Dir + "titulo_fenda_brilho_80x213"), 303 + 8, 11 + 8, 80, 213);
        _fogBack = Fog(Dir + "titulo_nevoa_fundo_480x270");
        var frente = MenuKit.Img(Root, MenuKit.Sprite(Dir + "titulo_frente_480x270"), -8, -8, 496, 286);
        _fogFront = Fog(Dir + "titulo_nevoa_frente_480x270");

        // o céu/frente têm 8 px de sobra em cada lado para o parallax não mostrar borda
        _layers = new[] { ceu.rectTransform, _fogBack.rectTransform, frente.rectTransform, _fogFront.rectTransform };
        _depth = new[] { 1f, 2f, 4f, 6f };

        BuildParticles();

        if (mode != Mode.Titulo)
        {
            // intro e seleção: cenário mais escuro para o conteúdo ler bem
            var dim = MenuKit.Img(Root, UiKit.WhiteSprite(), -20, -20, 520, 310);
            dim.color = mode == Mode.Intro ? new Color(0.03f, 0.01f, 0.05f, 0.55f) : new Color(0.03f, 0.01f, 0.05f, 0.62f);
            if (mode == Mode.Selecao) frente.gameObject.SetActive(false); // seleção usa só o céu (cards ocupam o chão)
        }
        Fit();
    }

    RawImage Fog(string path)
    {
        var tex = Resources.Load<Texture2D>(path);
        var go = new GameObject("Nevoa", typeof(RectTransform));
        go.transform.SetParent(Root, false);
        var raw = go.AddComponent<RawImage>();
        raw.raycastTarget = false;
        if (tex != null) { tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Repeat; raw.texture = tex; }
        else raw.enabled = false;
        MenuKit.Place(raw.rectTransform, -8, -8, 496, 286);
        raw.uvRect = new Rect(0f, 0f, 496f / 480f, 286f / 270f);
        return raw;
    }

    void BuildParticles()
    {
        var ember = MenuKit.Strip(Dir + "particula_brasa_4f_3x3", 3, 3, 4);
        var voidMote = MenuKit.Strip(Dir + "particula_vazio_4f_5x5", 5, 5, 4);
        int count = mode == Mode.Titulo ? 34 : 18;
        _particles = new Particle[count];
        for (int i = 0; i < count; i++)
        {
            bool isEmber = i % 3 != 0;
            var frames = isEmber ? ember : voidMote;
            var img = MenuKit.Img(Root, frames[0], 0, 0, isEmber ? 3 : 5, isEmber ? 3 : 5);
            img.gameObject.AddComponent<MenuSpriteLoop>().Setup(img, frames, isEmber ? 8f : 6f);
            _particles[i] = new Particle { img = img, ember = isEmber };
            Respawn(ref _particles[i], true);
        }
    }

    void Respawn(ref Particle p, bool anywhere)
    {
        if (p.ember)
        {
            p.pos = new Vector2(Random.Range(0f, 480f), anywhere ? Random.Range(60f, 270f) : 272f);
            p.vel = new Vector2(Random.Range(-3f, 3f), -Random.Range(6f, 14f));
            p.life = Random.Range(6f, 14f);
        }
        else
        {
            // poeira do Vazio sai da fenda (x ~ 320..360) e se espalha
            p.pos = new Vector2(Random.Range(318f, 362f), Random.Range(30f, 210f));
            p.vel = new Vector2(Random.Range(-6f, 6f), Random.Range(-3f, 3f));
            p.life = Random.Range(3f, 7f);
        }
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(12, 8, 18, 255);
    }

    void Fit()
    {
        if (_canvasRect == null) return;
        var size = _canvasRect.rect.size;
        if (size == _lastSize) return;
        _lastSize = size;
        float s = Mathf.Max(size.x / 1920f, size.y / 1080f);
        Root.localScale = new Vector3(s, s, 1f);
    }

    void Update()
    {
        Fit();
        float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;

        // parallax pelo ponteiro (desktop); no celular fica parado no centro
        Vector2 target = Vector2.zero;
        if (Input.mousePresent && Screen.width > 0 && Screen.height > 0 && Input.touchCount == 0)
        {
            var m = (Vector2)Input.mousePosition;
            target = new Vector2(m.x / Screen.width - 0.5f, m.y / Screen.height - 0.5f) * 2f;
            target = Vector2.ClampMagnitude(target, 1.4f);
        }
        _parallax = Vector2.Lerp(_parallax, target, 1f - Mathf.Exp(-dt * 3f));
        for (int i = 0; i < _layers.Length; i++)
        {
            var off = new Vector2(-_parallax.x, -_parallax.y * 0.5f) * _depth[i];
            off = new Vector2(Mathf.Round(off.x), Mathf.Round(off.y));           // passos de 1 px de arte
            _layers[i].anchoredPosition = new Vector2((-8f + off.x) * MenuKit.Px, -(-8f - off.y) * MenuKit.Px);
        }

        // névoas andam devagar (4 e 9 px/s), em passos de pixel
        if (_fogBack != null) { var r = _fogBack.uvRect; r.x = Mathf.Floor(t * 4f) / 480f; _fogBack.uvRect = r; }
        if (_fogFront != null) { var r = _fogFront.uvRect; r.x = Mathf.Floor(t * 9f) / 480f; _fogFront.uvRect = r; }

        // a fenda respira
        if (_riftGlow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f) * Mathf.Sin(t * 0.37f + 1f);
            _riftGlow.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * pulse);
        }

        if (_particles == null) return;
        for (int i = 0; i < _particles.Length; i++)
        {
            var p = _particles[i];
            p.pos += p.vel * dt;
            p.life -= dt;
            if (p.life <= 0f || p.pos.y < -6f) Respawn(ref p, false);
            float a = Mathf.Clamp01(p.life / 1.5f);
            p.img.color = new Color(1f, 1f, 1f, a);
            p.img.rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.pos.x) * MenuKit.Px, -Mathf.Round(p.pos.y) * MenuKit.Px);
            _particles[i] = p;
        }
    }

    void OnDestroy() { if (Current == this) Current = null; }
}
