using UnityEngine;
using UnityEngine.UI;

// Prólogo em 6 painéis pintados (Resources/Menu/intro/intro_painel_N_320x180) dentro da moldura dourada,
// caixa de diálogo com texto em máquina de escrever, CONTINUAR (pisca) e PULAR. Mock: intro_tela_painel5/6_mock.png.
// Espaço/Enter/clique: completa a página; de novo: avança. ESC: pula.
public class IntroController : MonoBehaviour
{
    public static readonly string[] Pages =
    {
        "Por milhares de anos, o Véu protegeu Aetherion.",
        "Até que algo despertou além das estrelas.",
        "O Véu foi destruído.",
        "E o Vazio atravessou.",
        "Aetherion ainda está de pé — por pouco.\nTemplos ao sol, fontes e jardins: a cidade antes da queda.",
        "Mas ainda existem aqueles dispostos a lutar."
    };

    const float CharsPerSecond = 34f;
    const float PanelFade = 0.35f;

    int _page;
    float _typed;
    float _fade = 1f;
    bool _busy;
    Text _body, _counter;
    Image _panel, _panelPrev, _arrow;
    Image[] _pips;
    Sprite[] _panelSprites;
    Sprite[] _arrowFrames;
    RectTransform _heroLayer;
    Button _continue, _skip;

    public static IntroController Instance { get; private set; }
    public int Page => _page;
    public Text Body => _body;
    public Image PanelImage => _panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != GameScenes.Intro)
            return;
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<IntroController>() != null)
            return;
        var root = new GameObject("Intro");
        root.AddComponent<IntroController>();
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        MenuAtmosBackground.Create(transform, MenuAtmosBackground.Mode.Intro);
        var canvas = UiKit.CreateCanvas(transform, "IntroCanvas");
        var stage = MenuKit.Stage(canvas.transform);

        _panelSprites = new Sprite[Pages.Length];
        for (int i = 0; i < Pages.Length; i++)
            _panelSprites[i] = MenuKit.Sprite("Menu/intro/intro_painel_" + (i + 1) + "_320x180");

        // clique em qualquer lugar da ilustração também avança
        var hit = MenuKit.Img(stage, UiKit.WhiteSprite(), 0, 0, 480, 270, raycast: true);
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.gameObject.AddComponent<Button>().onClick.AddListener(Advance);
        hit.GetComponent<Button>().transition = Selectable.Transition.None;
        hit.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };

        MenuKit.Art(stage, "Menu/intro/moldura_ilustracao_336x196", 72, 6);
        _panelPrev = MenuKit.Img(stage, _panelSprites[0], 80, 14, 320, 180);
        _panel = MenuKit.Img(stage, _panelSprites[0], 80, 14, 320, 180);
        _heroLayer = MenuKit.Node(stage, "HeroisPainel6", 80, 14, 320, 180);
        // painel 6: heróis vivos (idle) nos pés da borda do penhasco; luz e sombras já estão pintadas no painel
        foreach (var (id, x) in new[] { ("mago", 149f), ("arqueiro", 239f), ("guerreiro", 193f) })
            HeroPortrait.AttachFeetNative(_heroLayer, id, x, 158f, 8);
        var mask = _heroLayer.gameObject.AddComponent<RectMask2D>();
        mask.enabled = true;

        MenuKit.Img(stage, MenuKit.Nine("caixa_dialogo_9s_48x24", 8, 8, 8, 8), 72, 206, 336, 58, sliced: true);
        _body = MenuKit.Label(stage, "", MenuKit.BodyFont, 44, MenuKit.Ivory, 86, 211, 306, 34, TextAnchor.UpperLeft, true, 0.92f);

        _continue = MenuKit.Button(stage, "CONTINUAR", 320, 242, 80, 16, Advance);
        _arrowFrames = MenuKit.Strip("Menu/intro/seta_continuar_2f_8x6", 8, 6, 2);
        _arrow = MenuKit.Img(stage, _arrowFrames[0], 308, 247, 8, 6);
        _skip = MenuKit.Button(stage, "PULAR", 414, 8, 60, 16, GoToSelect);

        MenuKit.Label(stage, "PRÓLOGO", MenuKit.TitleFont, 40, MenuKit.Gold, 12, 7, 60, 12);
        _pips = new Image[Pages.Length];
        for (int k = 0; k < Pages.Length; k++)
            _pips[k] = MenuKit.Img(stage, MenuKit.Sprite("Menu/ui_kit/pip_vazio_7x7"), 12 + k * 9, 26, 7, 7);
        _counter = MenuKit.Label(stage, "", MenuKit.BodyFont, 32, MenuKit.Dim, 12, 35, 40, 10);

        var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _skip, selectOnDown = _skip, selectOnLeft = _skip, selectOnRight = _skip };
        _continue.navigation = nav;
        _skip.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _continue, selectOnDown = _continue, selectOnLeft = _continue, selectOnRight = _continue };
        MenuKit.Select(_continue.gameObject);
        ShowPage(0, instant: true);
    }

    void ShowPage(int page, bool instant)
    {
        _panelPrev.sprite = _panel.sprite;
        _page = page;
        _panel.sprite = _panelSprites[page];
        _fade = instant ? 1f : 0f;
        _typed = 0f;
        _body.text = "";
        _heroLayer.gameObject.SetActive(page == Pages.Length - 1);
        for (int k = 0; k < _pips.Length; k++)
            _pips[k].sprite = MenuKit.Sprite(k == page ? "Menu/ui_kit/pip_atual_7x7" : "Menu/ui_kit/pip_vazio_7x7");
        _counter.text = (page + 1) + " / " + Pages.Length;
    }

    void Update()
    {
        if (_busy) return;
        float dt = Time.unscaledDeltaTime;

        // troca de painel: dissolve em 4 passos (pixel art sem alpha "suave" demais)
        if (_fade < 1f)
        {
            _fade = Mathf.Min(1f, _fade + dt / PanelFade);
            _panel.color = new Color(1f, 1f, 1f, Mathf.Floor(_fade * 4f) / 4f);
        }
        else _panel.color = Color.white;

        string page = Pages[_page];
        if (_typed < page.Length)
        {
            _typed = Mathf.Min(page.Length, _typed + CharsPerSecond * dt);
            _body.text = page.Substring(0, (int)_typed);
        }

        // seta do CONTINUAR pisca e só anda quando a página terminou
        bool done = _typed >= page.Length;
        _arrow.sprite = _arrowFrames[(int)(Time.unscaledTime * 3f) % _arrowFrames.Length];
        _arrow.enabled = done || ((int)(Time.unscaledTime * 2f) % 2 == 0);

        if (Input.GetKeyDown(KeyCode.Space))
            Advance();          // Enter já chega pelo botão em foco (Submit)
        if (Input.GetKeyDown(KeyCode.Escape))
            GoToSelect();
    }

    public void Advance()
    {
        if (_busy) return;
        string page = Pages[_page];
        if (_typed < page.Length)
        {
            _typed = page.Length;
            _body.text = page;
            return;
        }
        if (_page + 1 >= Pages.Length)
        {
            GoToSelect();
            return;
        }
        ShowPage(_page + 1, instant: false);
    }

    // usado pelo smoke test para capturar painéis sem esperar a digitação
    public void JumpTo(int page)
    {
        ShowPage(Mathf.Clamp(page, 0, Pages.Length - 1), instant: true);
        _typed = Pages[_page].Length;
        _body.text = Pages[_page];
    }

    void GoToSelect()
    {
        if (_busy) return;
        _busy = true;
        SceneTransitionManager.Instance.Load(GameScenes.CharacterSelection);
    }
}
