using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class CreditsController : MonoBehaviour
{
    public static CreditsController Instance { get; private set; }
    public ScrollRect Scroll => _scroll;
    public IReadOnlyList<Text> CreditTexts => _texts;
    public bool AutoScrolling => _auto;
    ScrollRect _scroll;
    Text _toggleText;
    bool _auto = true;
    readonly List<Text> _texts = new List<Text>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.Credits) EnsureExists();
    }
    public static void EnsureExists()
    {
        if (FindFirstObjectByType<CreditsController>() == null) new GameObject("ClosingCredits").AddComponent<CreditsController>();
    }
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Start()
    {
        Time.timeScale = 1f;
        GameManager.EnsureExists();
        gameObject.AddComponent<GameAudio>();
        GameAudio.Instance.PlayCredits();
        SceneTransitionManager.Instance?.ClearFade();
        var canvas = UiKit.CreateCanvas(transform, "ClosingCreditsCanvas");
        var stage = MenuKit.Stage(canvas.transform);
        var art = MenuKit.Img(stage, MenuKit.Sprite("Menu/epilogue/epilogo_painel_4"), -4, -4, 488, 278);
        art.color = new Color(.34f, .28f, .38f, 1f);
        var shade = MenuKit.Img(stage, UiKit.WhiteSprite(), -4, -4, 488, 278);
        shade.color = new Color(.025f, .02f, .045f, .74f);
        MenuKit.Label(stage, CreditsContent.Title, MenuKit.TitleFont, 52, MenuKit.Gold, 36, 7, 408, 18, TextAnchor.MiddleCenter);
        MenuKit.Label(stage, "AGRADECIMENTOS E EQUIPE", MenuKit.BodyFont, 30, MenuKit.Dim, 36, 26, 408, 10, TextAnchor.MiddleCenter);
        MenuKit.Img(stage, MenuKit.Nine("caixa_dialogo_9s_48x24", 8, 8, 8, 8), 36, 40, 408, 200, sliced: true);
        var viewport = MenuKit.Node(stage, "LeituraCreditos", 46, 48, 388, 184);
        var hit = viewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 36f;
        _scroll.decelerationRate = .08f;
        viewport.gameObject.AddComponent<CreditsReadingGesture>().Owner = this;
        var content = MenuKit.Node(viewport, "TextoEncerramento", 0, 0, 388, 1200);
        _scroll.content = content;
        float y = 6f;
        foreach (var block in CreditsContent.Blocks)
        {
            bool heading = block.Tone == CreditsContent.Style.Heading;
            bool final = block.Tone == CreditsContent.Style.Final;
            if (heading) y += 12f;
            int fontSize = final ? 76 : heading ? 52 : 44;
            Color color = heading || final || block.Tone == CreditsContent.Style.Emphasis ? MenuKit.Gold : MenuKit.Ivory;
            var text = MenuKit.Label(content, block.Text, heading || final ? MenuKit.TitleFont : MenuKit.BodyFont,
                fontSize, color, 12, y, 364, 24, TextAnchor.UpperCenter, true, 1.02f);
            if (block.Tone == CreditsContent.Style.Signature) text.fontStyle = FontStyle.Italic;
            if (block.Tone == CreditsContent.Style.Emphasis) text.fontStyle = FontStyle.Bold;
            float height = Mathf.Ceil(text.preferredHeight / MenuKit.Px) + 3f;
            MenuKit.Place(text.rectTransform, 12, y, 364, height);
            _texts.Add(text);
            y += height + (block.Tone == CreditsContent.Style.Name ? 3f : 12f);
        }
        content.sizeDelta = new Vector2(388f * MenuKit.Px, (y + 18f) * MenuKit.Px);
        var map = MenuKit.Button(stage, "MAPA", 36, 245, 96, 20, GoToMap);
        var toggle = MenuKit.Button(stage, "PAUSAR ROLAGEM", 152, 245, 176, 20, ToggleScroll);
        _toggleText = toggle.GetComponentInChildren<Text>();
        var menu = MenuKit.Button(stage, "MENU", 348, 245, 96, 20, GoToMenu);
        map.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = toggle };
        toggle.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = map, selectOnRight = menu };
        menu.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = toggle };
        Canvas.ForceUpdateCanvases();
        _scroll.verticalNormalizedPosition = 1f;
        MenuKit.Select(toggle.gameObject);
    }
    void Update()
    {
        if (_scroll == null) return;
        float input = Input.GetKey(KeyCode.DownArrow) ? 1f : Input.GetKey(KeyCode.UpArrow) ? -1f : 0f;
        if (input != 0f) { PauseAuto(); Move(input * 80f * Time.unscaledDeltaTime); }
        if (Input.GetKeyDown(KeyCode.End)) ScrollToEnd();
        if (Input.GetKeyDown(KeyCode.Home)) { PauseAuto(); _scroll.verticalNormalizedPosition = 1f; }
        if (Input.GetKeyDown(KeyCode.Space)) ToggleScroll();
        if (Input.GetKeyDown(KeyCode.Escape)) GoToMenu();
        if (_auto) Move(6f * Time.unscaledDeltaTime);
    }
    void Move(float nativePixels)
    {
        float max = Mathf.Max(0f, _scroll.content.rect.height - _scroll.viewport.rect.height);
        Vector2 position = _scroll.content.anchoredPosition;
        position.y = Mathf.Clamp(position.y + nativePixels * MenuKit.Px, 0f, max);
        _scroll.content.anchoredPosition = position;
        if (position.y >= max && max > 0f) { _auto = false; if (_toggleText != null) _toggleText.text = "RELER"; }
    }
    public void PauseAuto() { _auto = false; if (_toggleText != null) _toggleText.text = "CONTINUAR ROLAGEM"; }
    public void ToggleScroll()
    {
        if (!_auto && _scroll.verticalNormalizedPosition <= .005f) _scroll.verticalNormalizedPosition = 1f;
        _auto = !_auto;
        _scroll.StopMovement();
        if (_toggleText != null) _toggleText.text = _auto ? "PAUSAR ROLAGEM" : "CONTINUAR ROLAGEM";
    }
    public void ScrollToEnd() { PauseAuto(); _scroll.StopMovement(); _scroll.verticalNormalizedPosition = 0f; }
    public void GoToMap() { SceneTransitionManager.Instance.Load(GameScenes.WorldMap); }
    public void GoToMenu() { SceneTransitionManager.Instance.Load(GameScenes.MainMenu); }
}

public sealed class CreditsReadingGesture : MonoBehaviour, IBeginDragHandler, IScrollHandler
{
    public CreditsController Owner;
    public void OnBeginDrag(PointerEventData data) { Owner?.PauseAuto(); }
    public void OnScroll(PointerEventData data) { Owner?.PauseAuto(); }
}
