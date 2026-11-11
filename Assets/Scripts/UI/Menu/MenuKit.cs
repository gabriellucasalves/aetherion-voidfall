using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Kit de UI pixel art das telas de menu (título, intro, seleção, configurações, pausa, derrota, fim de demo).
// Arte em Resources/Menu/... desenhada em pixels nativos; na tela 1 pixel de arte = 4 unidades UI (ref. 1920x1080).
// Layout em "pixels nativos" 480x270 com origem no canto SUPERIOR ESQUERDO (igual aos mocks aprovados).
public static class MenuKit
{
    public const int Px = 4;               // unidades UI por pixel de arte
    public const float SpritePpu = 100f / Px; // com referencePixelsPerUnit 100 do Canvas → 1 px = 4 u

    public static readonly Color Gold = new Color32(248, 226, 156, 255);
    public static readonly Color GoldDark = new Color32(212, 154, 42, 255);
    public static readonly Color Ivory = new Color32(246, 238, 222, 255);
    public static readonly Color Dim = new Color32(176, 164, 190, 255);
    public static readonly Color Ink = new Color32(12, 8, 18, 255);
    public static readonly Color ShadowCol = new Color32(12, 8, 18, 230);

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite[]> StripCache = new Dictionary<string, Sprite[]>();
    static Font _title, _body, _label, _labelBold;

    // ---------------- fontes (OFL) ----------------
    public static Font TitleFont { get { return _title != null ? _title : (_title = LoadFont("Menu/fontes/Jersey10-Regular")); } }
    public static Font BodyFont { get { return _body != null ? _body : (_body = LoadFont("Menu/fontes/VT323-Regular")); } }
    public static Font LabelFont { get { return _label != null ? _label : (_label = LoadFont("Menu/fontes/Silkscreen-Regular")); } }
    public static Font LabelBoldFont { get { return _labelBold != null ? _labelBold : (_labelBold = LoadFont("Menu/fontes/Silkscreen-Bold")); } }

    static Font LoadFont(string path)
    {
        var f = Resources.Load<Font>(path);
        return f != null ? f : UiKit.ResolveFont();
    }

    // ---------------- sprites ----------------
    // border = (esquerda, baixo, direita, cima) em pixels de arte, como no Sprite Editor.
    public static Sprite Sprite(string path, Vector4 border = default(Vector4), bool repeat = false)
    {
        string key = path + border + repeat;
        if (Cache.TryGetValue(key, out var s) && s != null) return s;
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) { Debug.LogWarning("[MenuKit] arte faltando: Resources/" + path); return UiKit.WhiteSprite(); }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpritePpu, 0, SpriteMeshType.FullRect, border);
        s.name = path.Substring(path.LastIndexOf('/') + 1);
        Cache[key] = s;
        return s;
    }

    public static Sprite Nine(string name, int l, int b, int r, int t) { return Sprite("Menu/ui_kit/" + name, new Vector4(l, b, r, t)); }

    public static Sprite[] Strip(string path, int frameW, int frameH, int count)
    {
        string key = path + frameW + "x" + frameH + "x" + count;
        if (StripCache.TryGetValue(key, out var arr) && arr != null) return arr;
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) { Debug.LogWarning("[MenuKit] arte faltando: Resources/" + path); return new[] { UiKit.WhiteSprite() }; }
        tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
        arr = new Sprite[count];
        for (int i = 0; i < count; i++)
            arr[i] = UnityEngine.Sprite.Create(tex, new Rect(i * frameW, 0, frameW, frameH), new Vector2(0.5f, 0.5f), SpritePpu, 0, SpriteMeshType.FullRect);
        StripCache[key] = arr;
        return arr;
    }

    // ---------------- palco 1920x1080 centralizado (cabe em qualquer proporção) ----------------
    public static RectTransform Stage(Transform canvas, string name = "Palco")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f, 1080f);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<MenuStageFit>();
        return rt;
    }

    // posiciona um RectTransform em pixels nativos (x,y = canto superior esquerdo, dentro do pai)
    public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x * Px, -y * Px);
        rt.sizeDelta = new Vector2(w * Px, h * Px);
        return rt;
    }

    public static RectTransform Node(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return Place((RectTransform)go.transform, x, y, w, h);
    }

    public static Image Img(Transform parent, Sprite sprite, float x, float y, float w, float h, bool sliced = false, bool tiled = false, bool raycast = false)
    {
        var go = new GameObject(sprite != null ? sprite.name : "Img", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = tiled ? Image.Type.Tiled : (sliced ? Image.Type.Sliced : Image.Type.Simple);
        img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = raycast;
        Place(img.rectTransform, x, y, w, h);
        return img;
    }

    // imagem no tamanho natural da arte (w,h vêm da textura)
    public static Image Art(Transform parent, string path, float x, float y)
    {
        var s = Sprite(path);
        return Img(parent, s, x, y, s.rect.width, s.rect.height);
    }

    // ---------------- texto ----------------
    // caixa em pixels nativos; tamanho de fonte em unidades UI (múltiplos: Jersey 40/60, VT323 28-48, Silkscreen 16/24)
    public static Text Label(Transform parent, string text, Font font, int size, Color color, float x, float y, float w, float h,
        TextAnchor anchor = TextAnchor.MiddleLeft, bool shadow = true, float lineSpacing = 1f)
    {
        var go = new GameObject("T_" + (text.Length > 18 ? text.Substring(0, 18) : text), typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color; t.text = text; t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;   // nunca vaza da caixa
        t.lineSpacing = lineSpacing;
        t.raycastTarget = false;
        t.supportRichText = false;
        Place(t.rectTransform, x, y, w, h);
        if (shadow)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = ShadowCol; sh.effectDistance = new Vector2(2f, -2f);
        }
        return t;
    }

    // true se o texto cabe na caixa (usado pelo smoke test "sem texto vazando")
    public static bool Fits(Text t)
    {
        if (t == null || string.IsNullOrEmpty(t.text)) return true;
        var gen = t.cachedTextGeneratorForLayout;
        var settings = t.GetGenerationSettings(t.rectTransform.rect.size);
        float h = gen.GetPreferredHeight(t.text, settings) / t.pixelsPerUnit;
        bool fitsH = h <= t.rectTransform.rect.height + 1f;
        bool fitsW = true;
        if (t.text.IndexOf('\n') < 0 && t.horizontalOverflow == HorizontalWrapMode.Wrap)
        {
            var s2 = t.GetGenerationSettings(new Vector2(99999f, t.rectTransform.rect.height));
            float w = gen.GetPreferredWidth(t.text, s2) / t.pixelsPerUnit;
            // linha única que deveria caber numa linha (caixas de 1 linha: altura < 2 linhas)
            if (t.rectTransform.rect.height < t.fontSize * 2f) fitsW = w <= t.rectTransform.rect.width + 1f;
        }
        return fitsH && fitsW;
    }

    // ---------------- botões ----------------
    public static Button Button(Transform parent, string text, float x, float y, float w, float h, UnityAction onClick, int fontSize = 40)
    {
        var img = Img(parent, Nine("botao_normal_9s_48x16", 6, 5, 6, 5), x, y, w, h, sliced: true, raycast: true);
        img.gameObject.name = "Botao_" + text;
        var btn = img.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        var st = new SpriteState
        {
            highlightedSprite = Nine("botao_hover_9s_48x16", 6, 5, 6, 5),
            selectedSprite = Nine("botao_hover_9s_48x16", 6, 5, 6, 5),
            pressedSprite = Nine("botao_pressed_9s_48x16", 6, 5, 6, 5),
            disabledSprite = Nine("botao_disabled_9s_48x16", 6, 5, 6, 5)
        };
        btn.spriteState = st;
        if (onClick != null) btn.onClick.AddListener(onClick);
        var label = Label(img.transform, text, TitleFont, fontSize, Ivory, 7, 0, w - 14, h, TextAnchor.MiddleCenter);
        img.gameObject.AddComponent<MenuButtonFx>().Setup(btn, label);
        return btn;
    }

    // ---------------- painéis ----------------
    public static Image Panel(Transform parent, float x, float y, float w, float h, string title = null, bool ornament = true)
    {
        var p = Img(parent, Nine("moldura_painel_9s_32x32", 10, 10, 10, 10), x, y, w, h, sliced: true, raycast: true);
        p.gameObject.name = "Painel_" + (title ?? "");
        if (ornament) Art(p.transform, "Menu/ui_kit/ornamento_topo_40x11", w / 2f - 20f, -6f);
        if (!string.IsNullOrEmpty(title))
        {
            float pw = title.Length * 7 + 40;
            var plate = Img(p.transform, Nine("placa_titulo_9s_24x15", 6, 6, 6, 6), w / 2f - pw / 2f, 8, pw, 17, sliced: true);
            Label(plate.transform, title, TitleFont, 40, Gold, 6, 0, pw - 12, 17, TextAnchor.MiddleCenter);
        }
        return p;
    }

    // overlay modal: escurece a tela inteira e centraliza um painel; retorna o painel (filhos em pixels nativos)
    public static GameObject Modal(Transform canvas, string name, string title, float w, float h, out RectTransform box, float dim = 0.68f)
    {
        var dimImg = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        dimImg.transform.SetParent(canvas, false);
        dimImg.sprite = UiKit.WhiteSprite();
        dimImg.color = new Color(0.02f, 0.01f, 0.04f, dim);
        dimImg.raycastTarget = true;
        var drt = dimImg.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
        var stage = Stage(dimImg.transform, "PalcoModal");
        var p = Panel(stage, 240f - w / 2f, 135f - h / 2f, w, h, title);
        box = p.rectTransform;
        return dimImg.gameObject;
    }

    // botão empilhado dentro de um painel modal (linha 0,1,2… a partir de yTop)
    public static Button ModalButton(RectTransform box, string text, float yTop, int row, UnityAction onClick, float w = 88f, float h = 16f, float gap = 4f)
    {
        float bw = box.sizeDelta.x / Px;
        return Button(box, text, bw / 2f - w / 2f, yTop + row * (h + gap), w, h, onClick);
    }

    // tecla desenhada (A, D, L/Q, E, ESPAÇO…) — retorna a largura usada em pixels nativos
    public static float Keycap(Transform parent, string key, float x, float y, bool gold = false)
    {
        var font = LabelBoldFont; int size = 24;
        float w = Mathf.Max(13f, Mathf.Ceil(EstimateWidth(key, font, size) / Px) + 8f);
        var img = Img(parent, Nine(gold ? "tecla_ouro_9s_12x12" : "tecla_9s_12x12", 4, 4, 4, 4), x, y, w, 12, sliced: true);
        Label(img.transform, key, font, size, Ink, 0, -0.5f, w, 11, TextAnchor.MiddleCenter, shadow: false);
        return w;
    }

    public static float EstimateWidth(string s, Font font, int size)
    {
        font.RequestCharactersInTexture(s, size, FontStyle.Normal);
        float w = 0f;
        foreach (char c in s)
            if (font.GetCharacterInfo(c, out var info, size, FontStyle.Normal)) w += info.advance;
        return w;
    }

    public static void Select(GameObject go)
    {
        if (EventSystem.current != null && go != null) EventSystem.current.SetSelectedGameObject(go);
    }
}

// mantém o palco 1920x1080 inteiro visível (escala uniforme) em 16:9, 19,5:9 (celular), 4:3…
public class MenuStageFit : MonoBehaviour
{
    RectTransform _rt, _parent;
    Vector2 _last;
    void Awake() { _rt = (RectTransform)transform; _parent = transform.parent as RectTransform; }
    void LateUpdate()
    {
        if (_parent == null) return;
        var size = _parent.rect.size;
        if (size == _last) return;
        _last = size;
        float s = Mathf.Min(size.x / 1920f, size.y / 1080f);
        _rt.localScale = new Vector3(s, s, 1f);
    }
}

// foco unificado mouse/teclado/toque: setas-cursor piscando nas laterais e texto dourado
public class MenuButtonFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    Button _btn; Text _label; Image _left, _right; Sprite[] _arrow; bool _focus; float _t;
    public static System.Action OnClickSfx;

    public void Setup(Button btn, Text label)
    {
        _btn = btn; _label = label;
        _arrow = MenuKit.Strip("Menu/ui_kit/seta_cursor_2f_8x9", 8, 9, 2);
        var rt = (RectTransform)transform;
        float w = rt.sizeDelta.x / MenuKit.Px, h = rt.sizeDelta.y / MenuKit.Px;
        _left = MenuKit.Img(transform, _arrow[0], -11f, (h - 9f) / 2f, 8, 9);
        _right = MenuKit.Img(transform, _arrow[0], w + 3f, (h - 9f) / 2f, 8, 9);
        _right.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        _right.rectTransform.pivot = new Vector2(0.5f, 1f);
        _right.rectTransform.anchoredPosition += new Vector2(4f * MenuKit.Px, 0f);
        SetFocus(false);
    }

    void SetFocus(bool on)
    {
        _focus = on;
        if (_left) _left.enabled = on;
        if (_right) _right.enabled = on;
        if (_label) _label.color = on && _btn.interactable ? MenuKit.Gold : MenuKit.Ivory;
    }

    public void OnSelect(BaseEventData e) { SetFocus(true); }
    public void OnDeselect(BaseEventData e) { SetFocus(false); }
    public void OnPointerEnter(PointerEventData e) { if (_btn.interactable) MenuKit.Select(gameObject); }
    public void OnSubmit(BaseEventData e) { OnClickSfx?.Invoke(); }
    public void OnPointerClick(PointerEventData e) { OnClickSfx?.Invoke(); }

    void OnDisable() { SetFocus(false); }

    void Update()
    {
        if (!_focus || _arrow == null) return;
        _t += Time.unscaledDeltaTime;
        var s = _arrow[(int)(_t * 4f) % 2];
        _left.sprite = s; _right.sprite = s;
    }
}

// cicla sprites numa Image (partículas, setas, pips…), tempo sem escala (funciona na pausa)
public class MenuSpriteLoop : MonoBehaviour
{
    Image _img; Sprite[] _frames; float _fps, _t;
    public void Setup(Image img, Sprite[] frames, float fps) { _img = img; _frames = frames; _fps = fps; _t = Random.Range(0f, 5f); }
    void Update()
    {
        if (_img == null || _frames == null || _frames.Length == 0) return;
        _t += Time.unscaledDeltaTime;
        _img.sprite = _frames[(int)(_t * _fps) % _frames.Length];
    }
}
