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
        _background = MenuKit.Img(stage, _art[0], -4, -4, 488, 278);
        _background.color = new Color(.27f, .22f, .35f, 1f);
        var veil = MenuKit.Img(stage, UiKit.WhiteSprite(), -4, -4, 488, 278);
        veil.color = new Color(.025f, .018f, .045f, .60f);
        MenuKit.Label(stage, "EPÍLOGO", MenuKit.TitleFont, 44, MenuKit.Gold, 14, 10, 100, 14);
        _counter = MenuKit.Label(stage, "", MenuKit.BodyFont, 36, MenuKit.Dim, 14, 26, 55, 12);
        _pips = new Image[4];
        for (int i = 0; i < 4; i++) _pips[i] = MenuKit.Img(stage, MenuKit.Sprite("Menu/ui_kit/pip_vazio_7x7"), 72 + i * 11, 29, 7, 7);
        MenuKit.Img(stage, MenuKit.Sprite("Menu/intro/moldura_ilustracao_336x196", new Vector4(8, 8, 8, 8)), 8, 52, 288, 169, sliced: true);
        _previous = MenuKit.Img(stage, _art[0], 16, 60, 272, 153);
        _panel = MenuKit.Img(stage, _art[0], 16, 60, 272, 153);
        _heroes = MenuKit.Node(stage, "HeroisDoRenascimento", 16, 60, 272, 153);
        _heroes.gameObject.AddComponent<RectMask2D>();
        _heroGroup = _heroes.gameObject.AddComponent<CanvasGroup>();
        foreach (var (id, x) in new[] { ("mago", 126f), ("guerreiro", 164f), ("arqueiro", 203f) })
            HeroPortrait.AttachFeetNative(_heroes, id, x, 134f, 8);
        var touch = MenuKit.Img(stage, UiKit.WhiteSprite(), 16, 60, 272, 153, raycast: true);
        touch.color = Color.clear;
        var artButton = touch.gameObject.AddComponent<Button>();
        artButton.transition = Selectable.Transition.None;
        artButton.navigation = new Navigation { mode = Navigation.Mode.None };
        artButton.onClick.AddListener(Advance);
        MenuKit.Img(stage, MenuKit.Nine("caixa_dialogo_9s_48x24", 8, 8, 8, 8), 304, 36, 168, 212, sliced: true);
        _heading = MenuKit.Label(stage, "", MenuKit.TitleFont, 40, MenuKit.Gold, 314, 46, 148, 28, TextAnchor.UpperLeft);
        _body = MenuKit.Label(stage, "", MenuKit.BodyFont, 44, MenuKit.Ivory, 314, 80, 148, 137, TextAnchor.UpperLeft, true, .94f);
        _continue = MenuKit.Button(stage, "CONTINUAR", 318, 220, 140, 24, Advance);
        _continueText = _continue.GetComponentInChildren<Text>();
        _back = MenuKit.Button(stage, "VOLTAR", 16, 232, 82, 20, Back);
        var skip = MenuKit.Button(stage, "PULAR", 410, 8, 60, 20, Finish);
        _continue.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = skip, selectOnDown = _back, selectOnLeft = _back, selectOnRight = skip };
        _back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _continue, selectOnUp = _continue };
        skip.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = _continue, selectOnLeft = _continue };
        MenuKit.Label(stage, "TOQUE NA IMAGEM OU APERTE ESPAÇO", MenuKit.BodyFont, 28, MenuKit.Dim, 16, 256, 280, 9);
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
