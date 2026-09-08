using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pad mobile: joystick à esquerda (andar + pular para cima) e, à direita,
// ícone de espada (atacar) e de escudo (defender). No WebGL o Unity não
// marca o aparelho como "mobile", então a detecção vai pelo navegador.
public class MobileControls : MonoBehaviour
{
    public static float Move { get; private set; }
    public static bool AttackHeld { get; private set; }
    public static bool BlockHeld { get; private set; }
    public static bool IsVisible { get; private set; }
    public static bool JumpHeld => _jumpButton || _stickJump;

    static bool _jumpButton;
    static bool _stickJump;
    static bool _jumpPressed;
    static bool _forcedOn;
    static Sprite _circle;
    static Sprite _sword;
    static Sprite _shield;
    static Sprite _jumpArrow;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern int AetherionIsMobile();
#endif

    public static bool ConsumeJumpDown()
    {
        if (!_jumpPressed)
            return false;
        _jumpPressed = false;
        return true;
    }

    public static bool ShouldShow()
    {
        if (_forcedOn)
            return true;
        if (Application.isMobilePlatform)
            return true;

        string os = (SystemInfo.operatingSystem + " " + SystemInfo.deviceModel).ToLowerInvariant();
        if (os.Contains("android") || os.Contains("iphone") || os.Contains("ipad") || os.Contains("ios"))
            return true;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            if (AetherionIsMobile() != 0)
                return true;
        }
        catch (System.Exception)
        {
        }
#endif
        return false;
    }

    public static void Attach(Transform owner)
    {
        if (owner == null)
            return;
        if (owner.GetComponentInChildren<MobileControls>(true) != null)
            return;

        UiKit.EnsureEventSystem();

        var canvasGo = new GameObject("MobileCanvas");
        canvasGo.transform.SetParent(owner, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f; // prioriza altura — botões ficam grandes no celular
        canvasGo.AddComponent<GraphicRaycaster>();

        var pad = canvasGo.AddComponent<MobileControls>();
        pad.Build(canvasGo.transform);
        pad.ApplyVisibility(ShouldShow());
    }

    void Start()
    {
        Input.multiTouchEnabled = true;
    }

    void Update()
    {
        // primeiro toque no WebGL liga o pad mesmo se o user-agent falhar
        if (!IsVisible && Input.touchCount > 0)
        {
            _forcedOn = true;
            ApplyVisibility(true);
        }
    }

    void OnDisable()
    {
        Move = 0f;
        _jumpButton = false;
        _stickJump = false;
        AttackHeld = false;
        BlockHeld = false;
        IsVisible = false;
    }

    void ApplyVisibility(bool on)
    {
        IsVisible = on;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(on);
    }

    void Build(Transform parent)
    {
        BuildStick(parent);
        BuildJump(parent);

        var sword = IconButton(parent, "Espada", SwordSprite(), new Vector2(1f, 0f), new Vector2(-168f, 248f), 168f,
            new Color(0.18f, 0.1f, 0.22f, 0.82f));
        Hold(sword, () => AttackHeld = true, () => AttackHeld = false);

        var shield = IconButton(parent, "Escudo", ShieldSprite(), new Vector2(1f, 0f), new Vector2(-168f, 78f), 148f,
            new Color(0.08f, 0.14f, 0.28f, 0.82f));
        Hold(shield, () => BlockHeld = true, () => BlockHeld = false);
    }

    void BuildStick(Transform parent)
    {
        var baseImg = Circle(parent, "Joystick", new Vector2(0f, 0f), new Vector2(210f, 210f), 240f,
            new Color(1f, 1f, 1f, 0.16f));
        var knob = Circle(baseImg.transform, "Knob", new Vector2(0.5f, 0.5f), Vector2.zero, 108f,
            new Color(0.93f, 0.78f, 0.38f, 0.95f));
        var stick = baseImg.gameObject.AddComponent<VirtualStick>();
        stick.Setup(knob.rectTransform, 92f);
    }

    void BuildJump(Transform parent)
    {
        var jump = IconButton(parent, "Pulo", JumpSprite(), new Vector2(0f, 0f), new Vector2(430f, 92f), 110f,
            new Color(0.16f, 0.12f, 0.22f, 0.8f));
        Hold(jump,
            () => { _jumpButton = true; _jumpPressed = true; },
            () => _jumpButton = false);
    }

    static Image IconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, float size, Color bg)
    {
        var image = Circle(parent, name, anchor, position, size, bg);
        var iconGo = new GameObject("Icone");
        iconGo.transform.SetParent(image.transform, false);
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        var rect = iconGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size * 0.62f, size * 0.62f);
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    static Image Circle(Transform parent, string name, Vector2 anchor, Vector2 position, float diameter, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = CircleSprite();
        image.color = color;
        image.raycastTarget = true;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(diameter, diameter);
        rect.anchoredPosition = position;
        return image;
    }

    static void Hold(Image image, System.Action down, System.Action up)
    {
        var hold = image.gameObject.AddComponent<HoldPad>();
        hold.Setup(down, up);
    }

    static Sprite CircleSprite()
    {
        if (_circle != null)
            return _circle;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float r = size * 0.5f - 1.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            if (d > r)
                pixels[y * size + x] = new Color32(0, 0, 0, 0);
            else if (d > r - 3f)
                pixels[y * size + x] = new Color32(255, 255, 255, 255);
            else
                pixels[y * size + x] = new Color32(255, 255, 255, 220);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        _circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _circle;
    }

    static Sprite SwordSprite()
    {
        if (_sword != null)
            return _sword;
        string[] art =
        {
            "......w.......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            ".....wyw......",
            "....kwwwk.....",
            "....kkkkk.....",
            "......u.......",
            "......u.......",
            "......u.......",
            ".....uuu......",
            "......u.......",
        };
        _sword = PixelSprite(art, ch => ch switch
        {
            'w' => new Color32(230, 230, 236, 255),
            'y' => new Color32(255, 220, 90, 255),
            'k' => new Color32(168, 122, 48, 255),
            'u' => new Color32(92, 70, 48, 255),
            _ => new Color32(0, 0, 0, 0),
        });
        return _sword;
    }

    static Sprite ShieldSprite()
    {
        if (_shield != null)
            return _shield;
        string[] art =
        {
            "..kkkkkkkkkk..",
            ".kbbbbbbbbbbk.",
            "kbbwwwwwwwwbbk",
            "kbwwbbbbbbwwbk",
            "kbwbbwwwwbbwbk",
            "kbwbbwyywbbwbk",
            "kbwbbwwwwbbwbk",
            "kbwwbbbbbbwwbk",
            ".kbwwwwwwwwbk.",
            ".kbbwwwwwwbbk.",
            "..kbbbbbbbbk..",
            "...kbbbbbbk...",
            "....kbbbbk....",
            ".....kkkk.....",
        };
        _shield = PixelSprite(art, ch => ch switch
        {
            'w' => new Color32(210, 230, 255, 255),
            'b' => new Color32(70, 120, 210, 255),
            'y' => new Color32(255, 214, 70, 255),
            'k' => new Color32(24, 36, 70, 255),
            _ => new Color32(0, 0, 0, 0),
        });
        return _shield;
    }

    static Sprite JumpSprite()
    {
        if (_jumpArrow != null)
            return _jumpArrow;
        string[] art =
        {
            "......w.......",
            ".....www......",
            "....wwwww.....",
            "...wwwwwww....",
            "..www.w.www...",
            "......w.......",
            "......w.......",
            "......w.......",
            "......w.......",
            "......w.......",
        };
        _jumpArrow = PixelSprite(art, ch => ch == 'w'
            ? new Color32(245, 236, 210, 255)
            : new Color32(0, 0, 0, 0));
        return _jumpArrow;
    }

    static Sprite PixelSprite(string[] art, System.Func<char, Color32> paint)
    {
        int height = art.Length;
        int width = art[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = art[height - 1 - y];
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = paint(x < row.Length ? row[x] : '.');
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), Mathf.Max(width, height));
    }

    class HoldPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        System.Action _down;
        System.Action _up;
        int _pointer = -1;

        public void Setup(System.Action down, System.Action up)
        {
            _down = down;
            _up = up;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointer = eventData.pointerId;
            _down?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId && _pointer != -1)
                return;
            _pointer = -1;
            _up?.Invoke();
        }
    }

    class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _knob;
        float _range;

        public void Setup(RectTransform knob, float range)
        {
            _knob = knob;
            _range = range;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (_knob == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, eventData.position, eventData.pressEventCamera, out var local);
            local = Vector2.ClampMagnitude(local, _range);
            _knob.anchoredPosition = local;
            Move = Mathf.Abs(local.x) < 14f ? 0f : Mathf.Clamp(local.x / _range, -1f, 1f);
            bool jump = local.y > _range * 0.48f;
            if (jump && !_stickJump)
                _jumpPressed = true;
            _stickJump = jump;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
            Move = 0f;
            _stickJump = false;
        }
    }
}
