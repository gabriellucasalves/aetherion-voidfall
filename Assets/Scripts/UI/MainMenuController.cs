using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de título "AETHERION: APOCALIPSE" no kit pixel art novo (mock titulo_menu_mock.png).
// Fundo em camadas com parallax (MenuAtmosBackground), logo, botões com cursor, trio de heróis com pés no chão.
public class MainMenuController : MonoBehaviour
{
    public const string GameTitle = "AETHERION: APOCALIPSE";

    Canvas _canvas;
    RectTransform _stage;
    GameObject _settingsPanel;
    GameObject _creditsPanel;
    readonly List<Button> _buttons = new List<Button>();
    Button _settingsButton, _creditsButton;
    Image _logo;
    float _t;
    bool _useImgui;
    string _imguiPanel;

    // expostos para o smoke test
    public static MainMenuController Instance { get; private set; }
    public GameObject SettingsPanel => _settingsPanel;
    public GameObject CreditsPanel => _creditsPanel;
    public IList<Button> Buttons => _buttons;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == GameScenes.Pong || sceneName == GameScenes.Boot)
            return;
        if (sceneName == GameScenes.Intro || sceneName == GameScenes.CharacterSelection || sceneName == GameScenes.GroundT1 || sceneName == GameScenes.WorldMap)
            return;
        if (sceneName == GameScenes.MainMenu || sceneName == "Untitled" || string.IsNullOrEmpty(sceneName))
            EnsureExists();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<MainMenuController>() != null)
            return;
        var root = new GameObject("MainMenu");
        root.AddComponent<MainMenuController>();
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        GameManager.EnsureExists();
        gameObject.AddComponent<GameAudio>().PlayMenu();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        try
        {
            var bg = MenuAtmosBackground.Create(transform, MenuAtmosBackground.Mode.Titulo);
            BuildHeroes(bg.Root);
            BuildUi();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("UI uGUI falhou, usando menu de fallback. " + exception);
            _useImgui = true;
        }
    }

    // trio no chão do cenário (pés em y=224 nativo). Ordem de desenho: mago, arqueiro, guerreiro (na frente)
    void BuildHeroes(RectTransform root)
    {
        var shadow = MenuKit.Sprite("Menu/ui_kit/sombra_heroi_40x8");
        foreach (var (id, x) in new[] { ("mago", 83f), ("arqueiro", 187f), ("guerreiro", 133f) })
        {
            var sh = MenuKit.Img(root, shadow, x - 20f, 222f, 40, 8);
            sh.color = new Color(1f, 1f, 1f, 0.85f);
            HeroPortrait.AttachFeetNative(root, id, x, 224f, 8);
        }
    }

    void BuildUi()
    {
        _canvas = UiKit.CreateCanvas(transform, "MenuCanvas");
        _stage = MenuKit.Stage(_canvas.transform);

        _logo = MenuKit.Art(_stage, "Menu/titulo/logo_aetherion_301x58", 89, 8);
        MenuKit.Art(_stage, "Menu/titulo/subtitulo_apocalipse_130x19", 175, 62);
        MenuKit.Label(_stage, "O Véu caiu. O Vazio avança.", MenuKit.BodyFont, 40, MenuKit.Ivory, 120, 86, 240, 14, TextAnchor.MiddleCenter);

        var items = new List<(string, UnityEngine.Events.UnityAction)>
        {
            ("JOGAR", OnPlay)
        };
        if (WorldMapProgress.HighestCompleted > 0)
            items.Add(("MAPA", () => SceneTransitionManager.Instance.Load(GameScenes.WorldMap)));
        items.Add(("CONFIGURAÇÕES", () => Open(_settingsPanel)));
        items.Add(("CRÉDITOS", () => Open(_creditsPanel)));

        for (int i = 0; i < items.Count; i++)
        {
            var b = MenuKit.Button(_stage, items[i].Item1, 194, 104 + i * 21, 92, 16, items[i].Item2);
            _buttons.Add(b);
            if (items[i].Item1 == "CONFIGURAÇÕES") _settingsButton = b;
            if (items[i].Item1 == "CRÉDITOS") _creditsButton = b;
        }
        ChainVertical(_buttons);

        MenuKit.Label(_stage, "Protótipo acadêmico  ·  pixel art 16-bit  ·  dark fantasy", MenuKit.BodyFont, 28, MenuKit.Dim, 100, 257, 280, 10, TextAnchor.MiddleCenter);
        MenuKit.Label(_stage, "ENTER  confirmar     SETAS  navegar", MenuKit.BodyFont, 28, MenuKit.Dim, 330, 257, 142, 10, TextAnchor.MiddleRight);

        _settingsPanel = BuildSettings(_canvas.transform, () => Close(_settingsPanel, _settingsButton));
        _creditsPanel = BuildCredits(_canvas.transform, () => Close(_creditsPanel, _creditsButton));
        MenuKit.Select(_buttons[0].gameObject);
    }

    public static void ChainVertical(IList<Button> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = list[(i - 1 + list.Count) % list.Count];
            nav.selectOnDown = list[(i + 1) % list.Count];
            list[i].navigation = nav;
        }
    }

    // painel CONFIGURAÇÕES (mock menu_config_mock.png) — reaproveitável pela pausa se quiser
    public static GameObject BuildSettings(Transform canvas, UnityEngine.Events.UnityAction onClose)
    {
        var go = MenuKit.Modal(canvas, "PainelConfiguracoes", "CONFIGURAÇÕES", 300, 252, out var box);
        MenuKit.Label(box, "CONTROLES", MenuKit.TitleFont, 40, MenuKit.Gold, 16, 30, 120, 12);
        MenuKit.Label(box, "TECLADO", MenuKit.BodyFont, 28, MenuKit.Dim, 180, 30, 104, 12, TextAnchor.MiddleRight);
        var rows = new (string, string[])[]
        {
            ("Mover", new[] { "A", "D", "/", "SETAS" }),
            ("Pular", new[] { "ESPAÇO", "/", "W" }),
            ("Atacar", new[] { "J", "/", "MOUSE ESQ." }),
            ("Escudo / Campo", new[] { "K", "S", "/", "MOUSE DIR." }),
            ("Especial", new[] { "L", "Q" }),
            ("Supremo", new[] { "E" }),
            ("Dash", new[] { "SHIFT" }),
            ("Pausa", new[] { "ESC" }),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float y = 46 + i * 17;
            if (i % 2 == 0)
            {
                var stripe = MenuKit.Img(box, UiKit.WhiteSprite(), 12, y - 2, 276, 16);
                stripe.color = new Color(1f, 1f, 1f, 0.04f);
            }
            MenuKit.Label(box, rows[i].Item1, MenuKit.BodyFont, 36, MenuKit.Ivory, 18, y, 92, 12);
            float cx = 112;
            foreach (var k in rows[i].Item2)
            {
                if (k == "/") { MenuKit.Label(box, "ou", MenuKit.BodyFont, 28, MenuKit.Dim, cx + 2, y, 10, 12); cx += 11; continue; }
                cx += MenuKit.Keycap(box, k, cx, y, rows[i].Item1 == "Supremo") + 3;
            }
        }
        MenuKit.Label(box, "No celular: botões na tela (ataque, escudo, especial, SUPREMO).", MenuKit.BodyFont, 30, MenuKit.Dim, 12, 183, 276, 10, TextAnchor.MiddleCenter);
        var music = AudioVolumeControls.Add(box, "MÚSICA", 196, 300, true);
        var effects = AudioVolumeControls.Add(box, "EFEITOS", 215, 300, false);
        var close = MenuKit.Button(box, "FECHAR", 108, 234, 84, 16, onClose);
        AudioVolumeControls.Link(close, music, effects, close);
        go.SetActive(false);
        go.AddComponent<MenuModalFocus>().First = close;
        return go;
    }

    static GameObject BuildCredits(Transform canvas, UnityEngine.Events.UnityAction onClose)
    {
        var go = MenuKit.Modal(canvas, "PainelCreditos", "CRÉDITOS", 220, 176, out var box);
        var logo = MenuKit.Art(box, "Menu/titulo/logo_aetherion_301x58", 0, 0);
        MenuKit.Place(logo.rectTransform, 110 - 75, 30, 150, 29);  // logo pela metade (escala 0,5 → 2 u/px)
        var lines = new (string, Color, int, Font, float)[]
        {
            ("Aetherion: Apocalipse", MenuKit.Gold, 40, MenuKit.TitleFont, 12),
            ("Protótipo acadêmico de Desenvolvimento de Games.", MenuKit.Ivory, 34, MenuKit.BodyFont, 20),
            ("Pixel art autoral (Aseprite + geração procedural)", MenuKit.Dim, 30, MenuKit.BodyFont, 18),
            ("Action platformer · Dark fantasy", MenuKit.Ivory, 34, MenuKit.BodyFont, 10),
            ("Fontes: Jersey 10, VT323, Silkscreen (OFL)", MenuKit.Dim, 28, MenuKit.BodyFont, 10),
        };
        float y = 62;
        foreach (var l in lines)
        {
            MenuKit.Label(box, l.Item1, l.Item4, l.Item3, l.Item2, 10, y, 200, l.Item5, TextAnchor.MiddleCenter);
            y += l.Item5 + 3;
        }
        var close = MenuKit.Button(box, "FECHAR", 68, 152, 84, 16, onClose);
        go.SetActive(false);
        go.AddComponent<MenuModalFocus>().First = close;
        return go;
    }

    void Open(GameObject panel)
    {
        _settingsPanel.SetActive(false);
        _creditsPanel.SetActive(false);
        panel.SetActive(true);
    }

    void Close(GameObject panel, Button back)
    {
        panel.SetActive(false);
        if (back != null) MenuKit.Select(back.gameObject);
    }

    void OnPlay()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.Load(GameScenes.Intro);
    }

    void Update()
    {
        if (_useImgui) return;
        _t += Time.unscaledDeltaTime;
        // logo "respira" 1 px a cada ~2 s
        if (_logo != null)
        {
            float bob = Mathf.Round(Mathf.Sin(_t * 1.4f) * 0.6f);
            _logo.rectTransform.anchoredPosition = new Vector2(89 * MenuKit.Px, -(8 + bob) * MenuKit.Px);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_settingsPanel != null && _settingsPanel.activeSelf) Close(_settingsPanel, _settingsButton);
            else if (_creditsPanel != null && _creditsPanel.activeSelf) Close(_creditsPanel, _creditsButton);
        }
        // nada selecionado (clique no vazio) → teclado volta ao primeiro botão
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null
            && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (_settingsPanel.activeSelf || _creditsPanel.activeSelf) return;
            MenuKit.Select(_buttons[0].gameObject);
        }
    }

    void OnGUI()
    {
        if (!_useImgui) return;
        var box = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 180f, 440f, 360f);
        GUI.Box(box, GameTitle);
        if (GUI.Button(new Rect(box.x + 70f, box.y + 80f, 300f, 50f), "JOGAR") && SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.Load(GameScenes.Intro);
        if (GUI.Button(new Rect(box.x + 70f, box.y + 145f, 300f, 50f), "CONFIGURAÇÕES")) _imguiPanel = "settings";
        if (GUI.Button(new Rect(box.x + 70f, box.y + 210f, 300f, 50f), "CRÉDITOS")) _imguiPanel = "credits";
        if (string.IsNullOrEmpty(_imguiPanel)) return;
        string message = _imguiPanel == "credits" ? "Aetherion: Apocalipse — protótipo acadêmico." : "A/D move, Espaço pula, J ataca, L/Q especial, E supremo, ESC pausa.";
        GUI.Box(new Rect(box.x, box.y + 270f, 440f, 70f), message);
    }
}

// ao abrir um modal, foca o primeiro botão (teclado/controle continuam funcionando)
public class MenuModalFocus : MonoBehaviour
{
    public Selectable First;
    void OnEnable() { if (First != null) MenuKit.Select(First.gameObject); }
}
