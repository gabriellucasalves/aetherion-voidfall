using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    GameObject _settingsPanel;
    GameObject _creditsPanel;
    GameObject _soonPanel;
    bool _useImgui;
    string _imguiPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == GameScenes.Pong || sceneName == GameScenes.Boot)
            return;

        if (sceneName == GameScenes.Intro || sceneName == GameScenes.CharacterSelection || sceneName == GameScenes.GroundT1)
            return;

        if (sceneName == GameScenes.MainMenu || sceneName == "Untitled" || string.IsNullOrEmpty(sceneName))
            EnsureExists();
    }

    public static void EnsureExists()
    {
#if UNITY_2023_1_OR_NEWER
        if (FindFirstObjectByType<MainMenuController>() != null)
            return;
#else
        if (FindObjectOfType<MainMenuController>() != null)
            return;
#endif

        var root = new GameObject("MainMenu");
        root.AddComponent<MenuBackground>();
        root.AddComponent<MainMenuController>();
    }

    void Start()
    {
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        try
        {
            EnsureEventSystem();
            BuildUi();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("UI uGUI falhou, usando menu de fallback. " + exception.Message);
            _useImgui = true;
        }
    }

    void OnGUI()
    {
        if (!_useImgui)
            return;

        DrawImguiMenu();
    }

    void BuildUi()
    {
        var canvasObject = new GameObject("MenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // título com sombra dupla pra dar peso
        CreateLabel(canvasObject.transform, "AETHERION: VOIDFALL", 68, new Vector2(4f, 296f), new Color(0f, 0f, 0f, 0.7f));
        CreateLabel(canvasObject.transform, "AETHERION: VOIDFALL", 68, new Vector2(0f, 300f), MenuTheme.CelestialGold);
        CreateLabel(canvasObject.transform, "O Véu caiu. O Vazio avança.", 24, new Vector2(0f, 218f), MenuTheme.SoftIvory);

        // guerreiro animado montando guarda ao lado do menu
        var pedestal = CreatePanel(canvasObject.transform, new Vector2(400f, 470f), new Vector2(-560f, -90f));
        pedestal.GetComponent<Image>().color = new Color(0.07f, 0.05f, 0.11f, 0.75f);
        HeroPortrait.Attach(pedestal.transform, "guerreiro", new Vector2(0f, 40f), 340f);
        CreateLabel(pedestal.transform, "O ÚLTIMO GUERREIRO", 20, new Vector2(0f, -170f), MenuTheme.CelestialGold);
        CreateLabel(pedestal.transform, "ainda de pé nas ruínas", 15, new Vector2(0f, -200f), new Color(1f, 1f, 1f, 0.55f));

        CreateButton(canvasObject.transform, "JOGAR", new Vector2(0f, 40f), OnPlay);
        CreateButton(canvasObject.transform, "CONFIGURAÇÕES", new Vector2(0f, -70f), () => TogglePanel(_settingsPanel));
        CreateButton(canvasObject.transform, "CRÉDITOS", new Vector2(0f, -180f), () => TogglePanel(_creditsPanel));

        CreateLabel(canvasObject.transform, "Protótipo acadêmico · pixel art 16-bit · dark fantasy", 15, new Vector2(0f, -480f), new Color(1f, 1f, 1f, 0.4f));

        _settingsPanel = CreateInfoPanel(canvasObject.transform, "CONFIGURAÇÕES",
            "CONTROLES\n\n" +
            "A / D ou setas — mover      Espaço / W — pular\n" +
            "Mouse esq. ou J — atacar      Mouse dir. ou K — escudo\n" +
            "Shift — dash      ESC — pausa\n\n" +
            "Volume e remapeamento virão nas próximas etapas.");
        _creditsPanel = CreateInfoPanel(canvasObject.transform, "CRÉDITOS",
            "Aetherion: Voidfall\nProtótipo acadêmico de Desenvolvimento de Games.\n\n" +
            "Pixel art autoral (Aseprite + geração procedural)\nAction platformer · Dark fantasy · Terra e espaço");
        _soonPanel = CreateInfoPanel(canvasObject.transform, "PRÓXIMA ETAPA", "Menu funcional.\nA escolha de herói será a Etapa 3.");
    }

    void OnPlay()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.Load(GameScenes.Intro);
    }

    void TogglePanel(GameObject panel)
    {
        bool open = !panel.activeSelf;
        _settingsPanel.SetActive(false);
        _creditsPanel.SetActive(false);
        _soonPanel.SetActive(false);
        panel.SetActive(open);
    }

    GameObject CreateInfoPanel(Transform parent, string title, string body)
    {
        var panel = CreatePanel(parent, new Vector2(820f, 440f), new Vector2(0f, -20f));
        CreateLabel(panel.transform, title, 28, new Vector2(0f, 160f), MenuTheme.CelestialGold);
        CreateLabel(panel.transform, body, 20, new Vector2(0f, 0f), MenuTheme.SoftIvory);
        CreateButton(panel.transform, "FECHAR", new Vector2(0f, -170f), () => panel.SetActive(false));
        panel.SetActive(false);
        return panel;
    }

    static GameObject CreatePanel(Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = MenuTheme.Panel;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }

    static void CreateLabel(Transform parent, string text, int size, Vector2 position, Color color)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = ResolveFont();
        if (label.font == null)
            throw new InvalidOperationException("Nenhuma fonte disponível.");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1400f, 120f);
        rect.anchoredPosition = position;
    }

    static void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = MenuTheme.Button;

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = MenuTheme.ButtonHover;
        colors.pressedColor = MenuTheme.RiftMagenta;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 72f);
        rect.anchoredPosition = position;

        CreateLabel(go.transform, text, 22, Vector2.zero, MenuTheme.SoftIvory);
    }

    static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
#else
        if (FindObjectOfType<EventSystem>() != null)
            return;
#endif

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static Font ResolveFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
            return font;

        return Font.CreateDynamicFontFromOSFont(new[] { "Helvetica Neue", "Helvetica", "Arial" }, 24);
    }

    void DrawImguiMenu()
    {
        var box = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 180f, 440f, 360f);
        GUI.Box(box, "AETHERION: VOIDFALL");

        if (GUI.Button(new Rect(box.x + 70f, box.y + 80f, 300f, 50f), "JOGAR"))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.Load(GameScenes.Intro);
        }
        if (GUI.Button(new Rect(box.x + 70f, box.y + 145f, 300f, 50f), "CONFIGURAÇÕES"))
            _imguiPanel = "settings";
        if (GUI.Button(new Rect(box.x + 70f, box.y + 210f, 300f, 50f), "CRÉDITOS"))
            _imguiPanel = "credits";

        if (string.IsNullOrEmpty(_imguiPanel))
            return;

        string message = _imguiPanel == "credits"
            ? "Protótipo acadêmico. Terra e espaço."
            : _imguiPanel == "settings"
                ? "WASD move. ESC pausa."
                : "Seleção de herói na próxima etapa.";

        GUI.Box(new Rect(box.x, box.y + 270f, 440f, 70f), message);
    }
}
