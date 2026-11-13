using UnityEngine;
using UnityEngine.UI;

// Four deliberate, readable pages after the final boss. Touch/Space first reveals
// the complete paragraph and then advances; finishing leads to the closing credits.
public sealed class EpilogueController : MonoBehaviour
{
    public static readonly string[] Titles = {
        "O FIM DA ESCURIDÃO", "UM NOVO AMANHECER", "O RENASCIMENTO", "A ESPERANÇA PREVALECE"
    };
    public static readonly string[] Pages = {
        "Após uma longa batalha, a escuridão que ameaçava consumir Aetherion finalmente foi derrotada.\n\nPela primeira vez em muito tempo, o silêncio não trazia medo… mas paz.",
        "Das ruínas de um mundo quase esquecido, a vida começou a florescer novamente.\n\nOs céus recuperaram suas cores, e a esperança voltou a iluminar Aetherion.",
        "Aos poucos, cidades foram reconstruídas, jardins voltaram a florescer e novas histórias começaram a ser escritas.\n\nAetherion não seria mais o mesmo… mas seu povo havia encontrado forças para recomeçar.",
        "E assim, graças à coragem daqueles que se recusaram a desistir, uma nova era de paz teve início.\n\nPois enquanto houver quem lute pela luz…\n\nA esperança jamais desaparecerá."
    };
    public static EpilogueController Instance { get; private set; }
    public int Page => _page;
    public Text Body => _body;
    public Text Heading => _heading;
    public Image PanelImage => _panel;
    public Button ContinueButton => _continue;
    public RectTransform HeroLayer => _heroes;
    Sprite[] _art;
    Text _body, _heading, _counter, _continueText;
    Image _panel, _previous, _background;
    Image[] _pips;
    RectTransform _heroes;
    CanvasGroup _heroGroup;
    Button _continue, _back;
    float _typed, _fade;
    int _page;
    bool _leaving;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.Epilogue) EnsureExists();
    }
    public static void EnsureExists()
    {
        if (FindFirstObjectByType<EpilogueController>() != null) return;
        new GameObject("Epilogue").AddComponent<EpilogueController>();
    }
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Start()
    {
        Time.timeScale = 1f;
        GameManager.EnsureExists();
        gameObject.AddComponent<GameAudio>();
        SceneTransitionManager.Instance?.ClearFade();
        var canvas = UiKit.CreateCanvas(transform, "EpilogueCanvas");
        var stage = MenuKit.Stage(canvas.transform);
        _art = new Sprite[4];
        for (int i = 0; i < _art.Length; i++) _art[i] = MenuKit.Sprite("Menu/epilogue/epilogo_painel_" + (i + 1));

        // Fundo em escala inteira 2x (640x360 nativos) com máscara 480x270 para não vazar
        var bgRoot = MenuKit.Node(stage, "BackgroundRoot", 0, 0, 480, 270);
        bgRoot.gameObject.AddComponent<RectMask2D>();
        _background = MenuKit.Img(bgRoot, _art[0], -80, -45, 640, 360);
        _background.color = new Color(.27f, .22f, .35f, 1f);
        var veil = MenuKit.Img(stage, UiKit.WhiteSprite(), 0, 0, 480, 270);
        veil.color = new Color(.025f, .018f, .045f, .60f);

        // Cabeçalho e indicadores de página
        MenuKit.Label(stage, "EPÍLOGO", MenuKit.TitleFont, 40, MenuKit.Gold, 16, 10, 100, 14);
        _counter = MenuKit.Label(stage, "", MenuKit.BodyFont, 34, MenuKit.Dim, 16, 26, 50, 12);
        _pips = new Image[4];
        for (int i = 0; i < 4; i++) _pips[i] = MenuKit.Img(stage, MenuKit.Sprite("Menu/ui_kit/pip_vazio_7x7"), 68 + i * 11, 29, 7, 7);

        // Moldura nativa (336x196) em volta do painel de 320x180
        MenuKit.Art(stage, "Menu/intro/moldura_ilustracao_336x196", 8, 40);
        _previous = MenuKit.Img(stage, _art[0], 16, 48, 320, 180);
        _panel = MenuKit.Img(stage, _art[0], 16, 48, 320, 180);
        _heroes = MenuKit.Node(stage, "HeroisDoRenascimento", 16, 48, 320, 180);
        _heroes.gameObject.AddComponent<RectMask2D>();
        _heroGroup = _heroes.gameObject.AddComponent<CanvasGroup>();
        foreach (var (id, x) in new[] { ("mago", 149f), ("guerreiro", 193f), ("arqueiro", 239f) })
            HeroPortrait.AttachFeetNative(_heroes, id, x, 164f, 8);

        // Toque na imagem para avançar
        var touch = MenuKit.Img(stage, UiKit.WhiteSprite(), 16, 48, 320, 180, raycast: true);
        touch.color = Color.clear;
        var artButton = touch.gameObject.AddComponent<Button>();
        artButton.transition = Selectable.Transition.None;
        artButton.navigation = new Navigation { mode = Navigation.Mode.None };
        artButton.onClick.AddListener(Advance);

        // Coluna de narrativa e botões à direita (largura nativa 124, margens respeitadas)
        MenuKit.Img(stage, MenuKit.Nine("caixa_dialogo_9s_48x24", 8, 8, 8, 8), 348, 36, 124, 226, sliced: true);
        _heading = MenuKit.Label(stage, "", MenuKit.TitleFont, 34, MenuKit.Gold, 356, 46, 108, 28, TextAnchor.UpperLeft);
        _body = MenuKit.Label(stage, "", MenuKit.BodyFont, 36, MenuKit.Ivory, 356, 78, 108, 140, TextAnchor.UpperLeft, true, .90f);
        _continue = MenuKit.Button(stage, "CONTINUAR", 354, 226, 112, 24, Advance);
        _continueText = _continue.GetComponentInChildren<Text>();

        // Botões auxiliares e atalhos
        _back = MenuKit.Button(stage, "VOLTAR", 16, 242, 70, 20, Back);
        var skip = MenuKit.Button(stage, "PULAR", 412, 8, 60, 20, Finish);
        _continue.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = skip, selectOnDown = _back, selectOnLeft = _back, selectOnRight = skip };
        _back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _continue, selectOnUp = _continue };
        skip.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = _continue, selectOnLeft = _continue };
        MenuKit.Label(stage, "TOQUE NA IMAGEM OU APERTE ESPAÇO", MenuKit.BodyFont, 26, MenuKit.Dim, 96, 248, 240, 10);

        Show(0, true);
        MenuKit.Select(_continue.gameObject);
    }
    void Show(int page, bool instant)
    {
        _previous.sprite = _panel.sprite;
        _page = Mathf.Clamp(page, 0, 3);
        _panel.sprite = _art[_page];
        _background.sprite = _art[_page];
        _heading.text = Titles[_page];
        _counter.text = (_page + 1) + " / 4";
        _typed = 0;
        _body.text = "";
        _fade = instant ? 1f : 0f;
        _panel.color = new Color(1f, 1f, 1f, _fade);
        _heroes.gameObject.SetActive(_page == 3);
        _heroGroup.alpha = _fade;
        _back.interactable = _page > 0;
        if (_continueText != null) _continueText.text = _page == 3 ? "AGRADECIMENTOS" : "CONTINUAR";
        for (int i = 0; i < 4; i++) _pips[i].sprite = MenuKit.Sprite(i == _page ? "Menu/ui_kit/pip_atual_7x7" : "Menu/ui_kit/pip_vazio_7x7");
        GameAudio.Instance?.PlayEpilogue(_page);
    }
    void Update()
    {
        if (_leaving) return;
        _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / .4f);
        _panel.color = new Color(1f, 1f, 1f, Mathf.Floor(_fade * 6f) / 6f);
        _heroGroup.alpha = _panel.color.a;
        _typed = Mathf.Min(Pages[_page].Length, _typed + Time.unscaledDeltaTime * 38f);
        _body.text = Pages[_page].Substring(0, (int)_typed);
        if (Input.GetKeyDown(KeyCode.Space)) Advance();
        if (Input.GetKeyDown(KeyCode.Escape)) Finish();
    }
    public void Advance()
    {
        if (_leaving) return;
        if (_typed < Pages[_page].Length) { _typed = Pages[_page].Length; _body.text = Pages[_page]; return; }
        if (_page == 3) Finish(); else Show(_page + 1, false);
    }
    public void Back() { if (!_leaving && _page > 0) Show(_page - 1, false); }
    public void JumpTo(int page) { Show(page, true); _typed = Pages[_page].Length; _body.text = Pages[_page]; }
    public void Finish()
    {
        if (_leaving) return;
        _leaving = true;
        SceneTransitionManager.Instance.Load(GameScenes.Credits);
    }
}
