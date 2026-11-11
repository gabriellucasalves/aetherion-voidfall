using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Seleção de herói no kit novo (mocks selecao_<herói>_mock.png).
// Abre SEM herói escolhido; clique/toque/teclado num card mostra atributos + BÁSICO / ESPECIAL / SUPREMO;
// ESCOLHER <NOME> abre a confirmação (mesmo fluxo de antes: GameManager.SelectHero → Ground_T1).
public class CharacterSelectController : MonoBehaviour
{
    // VIDA vai até 160 (Guerreiro tem 160 e não pode estourar a barra)
    public static readonly (string Label, int Max)[] Stats =
    {
        ("VIDA", 160), ("FORÇA", 100), ("PODER", 100), ("DEFESA", 100), ("AGILIDADE", 100)
    };

    const float CardW = 68f, CardH = 124f, CardY = 30f, BarInner = 108f;

    CharacterData[] _heroes;
    CharacterData _selected;
    readonly List<Card> _cards = new List<Card>();
    GameObject _confirmPanel;
    Text _confirmText;
    Button _chooseButton, _backButton, _confirmYes;
    Text _chooseLabel;

    // painel de detalhe
    GameObject _detailRoot, _emptyHint;
    Text _name, _role, _desc;
    Image[] _statFills;
    Text[] _statValues;
    readonly List<SkillRow> _rows = new List<SkillRow>();

    public static CharacterSelectController Instance { get; private set; }
    public CharacterData Selected => _selected;

    class Card
    {
        public CharacterData Hero;
        public RectTransform Root;
        public Image Frame, Glow, Portrait;
        public Text Name, Tag;
        public Button Button;
        public string Id;
    }

    class SkillRow
    {
        public Image Icon, TagImg, Key;
        public Text TagText, KeyText, Extra, Name, Desc;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != GameScenes.CharacterSelection)
            return;
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<CharacterSelectController>() != null)
            return;
        var root = new GameObject("CharacterSelect");
        root.AddComponent<CharacterSelectController>();
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        _heroes = CharacterCatalog.Playable();
        MenuAtmosBackground.Create(transform, MenuAtmosBackground.Mode.Selecao);
        BuildUi();
        Select((CharacterData)null);
        if (_cards.Count > 0) MenuKit.Select(_cards[0].Button.gameObject);
    }

    void BuildUi()
    {
        var canvas = UiKit.CreateCanvas(transform, "SelectCanvas");
        var stage = MenuKit.Stage(canvas.transform);

        var plate = MenuKit.Img(stage, MenuKit.Nine("placa_titulo_9s_24x15", 6, 6, 6, 6), 145, 5, 190, 19, sliced: true);
        MenuKit.Label(plate.transform, "ESCOLHA SEU HERÓI", MenuKit.TitleFont, 60, MenuKit.Gold, 8, 0, 174, 19, TextAnchor.MiddleCenter);

        // cards (3 cabem em 8..220; com menos heróis ficam centrados na mesma faixa)
        int n = Mathf.Min(3, _heroes.Length);
        float startX = 8f + (212f - (n * CardW + (n - 1) * 4f)) / 2f;
        for (int i = 0; i < n; i++)
            _cards.Add(BuildCard(stage, _heroes[i], startX + i * (CardW + 4f)));

        // ATRIBUTOS
        MenuKit.Panel(stage, 8, 158, 212, 86, null, ornament: false);
        MenuKit.Label(stage, "ATRIBUTOS", MenuKit.TitleFont, 40, MenuKit.Gold, 18, 161, 120, 12);
        _statFills = new Image[Stats.Length];
        _statValues = new Text[Stats.Length];
        for (int k = 0; k < Stats.Length; k++)
        {
            float y = 175 + k * 13;
            MenuKit.Label(stage, Stats[k].Label, MenuKit.BodyFont, 32, MenuKit.Ivory, 18, y - 1, 54, 10);
            MenuKit.Img(stage, MenuKit.Nine("barra_atributo_moldura_9s_16x8", 3, 3, 3, 3), 74, y, 112, 8, sliced: true);
            _statFills[k] = MenuKit.Img(stage, null, 76, y + 2, 0, 4, tiled: true);
            _statValues[k] = MenuKit.Label(stage, "", MenuKit.BodyFont, 32, MenuKit.Gold, 192, y - 1, 24, 10);
        }

        // painel direito: nome, papel, descrição e as 3 habilidades
        MenuKit.Panel(stage, 224, 28, 248, 216);
        _emptyHint = MenuKit.Label(stage, "Clique ou toque num herói\npara ver as habilidades.", MenuKit.BodyFont, 40, MenuKit.Dim, 236, 110, 224, 40, TextAnchor.MiddleCenter).gameObject;
        _detailRoot = MenuKit.Node(stage, "Detalhe", 0, 0, 480, 270).gameObject;
        var d = _detailRoot.transform;
        _name = MenuKit.Label(d, "", MenuKit.TitleFont, 60, MenuKit.Gold, 236, 39, 120, 17);
        _role = MenuKit.Label(d, "", MenuKit.BodyFont, 32, MenuKit.Dim, 322, 43, 140, 10, TextAnchor.MiddleRight);
        _desc = MenuKit.Label(d, "", MenuKit.BodyFont, 32, MenuKit.Ivory, 236, 56, 226, 18, TextAnchor.UpperLeft, true, 0.88f);
        MenuKit.Img(d, MenuKit.Sprite("Menu/ui_kit/divisor_tile_16x5", default, true), 234, 79, 228, 5, tiled: true);
        MenuKit.Art(d, "Menu/ui_kit/divisor_centro_15x7", 341, 78);
        for (int k = 0; k < 3; k++)
        {
            float y = 88 + k * 50;
            var r = new SkillRow();
            r.Icon = MenuKit.Img(d, null, 236, y + 2, 24, 24);
            r.TagImg = MenuKit.Img(d, null, 266, y, 44, 10, sliced: true);
            r.TagText = MenuKit.Label(r.TagImg.transform, "", MenuKit.LabelBoldFont, 24, MenuKit.Ink, 2, 0, 40, 10, TextAnchor.MiddleCenter, shadow: false);
            r.Key = MenuKit.Img(d, null, 313, y - 1, 14, 11, sliced: true);
            r.KeyText = MenuKit.Label(r.Key.transform, "", MenuKit.LabelBoldFont, 24, MenuKit.Ink, 0, 0, 14, 10, TextAnchor.MiddleCenter, shadow: false);
            r.Extra = MenuKit.Label(d, "", MenuKit.BodyFont, 28, MenuKit.Dim, 331, y - 1, 130, 10);
            r.Name = MenuKit.Label(d, "", MenuKit.TitleFont, 40, MenuKit.Ivory, 266, y + 11, 200, 12);
            r.Name.resizeTextForBestFit = true; r.Name.resizeTextMinSize = 28; r.Name.resizeTextMaxSize = 40;
            r.Desc = MenuKit.Label(d, "", MenuKit.BodyFont, 32, MenuKit.Ivory, 266, y + 22, 198, 17, TextAnchor.UpperLeft, true, 0.88f);
            r.Desc.resizeTextForBestFit = true; r.Desc.resizeTextMinSize = 26; r.Desc.resizeTextMaxSize = 32;
            _rows.Add(r);
        }

        _backButton = MenuKit.Button(stage, "VOLTAR", 8, 248, 84, 18, () => SceneTransitionManager.Instance.Load(GameScenes.MainMenu));
        _chooseButton = MenuKit.Button(stage, "ESCOLHER", 352, 248, 120, 18, OpenConfirm);
        _chooseLabel = _chooseButton.GetComponentInChildren<Text>();
        MenuKit.Label(stage, "Clique ou toque num herói para ver as habilidades", MenuKit.BodyFont, 28, MenuKit.Dim, 100, 257, 250, 10, TextAnchor.MiddleCenter);

        WireNavigation();

        _confirmPanel = MenuKit.Modal(canvas.transform, "Confirmar", "CONFIRMAR", 220, 92, out var box);
        _confirmText = MenuKit.Label(box, "", MenuKit.BodyFont, 40, MenuKit.Ivory, 12, 30, 196, 30, TextAnchor.MiddleCenter);
        _confirmYes = MenuKit.Button(box, "CONFIRMAR", 16, 66, 90, 16, Confirm);
        var no = MenuKit.Button(box, "VOLTAR", 114, 66, 90, 16, CloseConfirm);
        _confirmYes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no, selectOnLeft = no };
        no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _confirmYes, selectOnLeft = _confirmYes };
        _confirmPanel.AddComponent<MenuModalFocus>().First = _confirmYes;
        _confirmPanel.SetActive(false);
    }

    Card BuildCard(RectTransform stage, CharacterData hero, float x)
    {
        var c = new Card { Hero = hero, Id = hero.Id };
        c.Root = MenuKit.Node(stage, "Card_" + hero.Id, x, CardY, CardW, CardH);
        c.Glow = MenuKit.Img(c.Root, MenuKit.Sprite("Menu/ui_kit/brilho_pedestal_" + hero.Id + "_80x20"), CardW / 2f - 40f, CardH - 44f, 80, 20);
        c.Frame = MenuKit.Img(c.Root, MenuKit.Nine("card_" + hero.Id + "_normal_9s_48x48", 12, 12, 12, 12), 0, 0, CardW, CardH, sliced: true, raycast: true);
        MenuKit.Art(c.Root, "Menu/ui_kit/pedestal_" + hero.Id + "_64x14", 2, 82);
        c.Portrait = HeroPortrait.AttachFeetNative(c.Root, hero.Id, 35, 88, 8);
        if (c.Portrait == null)
            MenuKit.Label(c.Root, "?", MenuKit.TitleFont, 60, MenuKit.Dim, 0, 40, CardW, 30, TextAnchor.MiddleCenter);
        var info = HeroSkillInfo.For(hero.Id);
        c.Name = MenuKit.Label(c.Root, info.Name, MenuKit.TitleFont, 40, MenuKit.Dim, 5, 94, CardW - 10, 12, TextAnchor.MiddleCenter);
        c.Tag = MenuKit.Label(c.Root, info.RoleTag, MenuKit.BodyFont, 32, MenuKit.Dim, 5, 105, CardW - 10, 10, TextAnchor.MiddleCenter);

        c.Button = c.Frame.gameObject.AddComponent<Button>();
        c.Button.transition = Selectable.Transition.None;
        c.Button.onClick.AddListener(() => Select(hero));
        c.Frame.gameObject.AddComponent<HeroCardFocus>().Setup(() => { if (_selected != null || Input.anyKeyDown) Select(hero); });
        return c;
    }

    void WireNavigation()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].Button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = _cards[(i - 1 + _cards.Count) % _cards.Count].Button,
                selectOnRight = _cards[(i + 1) % _cards.Count].Button,
                selectOnDown = _chooseButton,
                selectOnUp = _chooseButton
            };
        }
        var mid = _cards.Count > 0 ? _cards[Mathf.Min(1, _cards.Count - 1)].Button : null;
        _chooseButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = mid, selectOnLeft = _backButton, selectOnRight = _backButton };
        _backButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _cards.Count > 0 ? _cards[0].Button : null, selectOnLeft = _chooseButton, selectOnRight = _chooseButton };
    }

    public void SelectById(string heroId)
    {
        foreach (var c in _cards) if (c.Id == heroId) { Select(c.Hero); return; }
    }

    public void Select(CharacterData hero)
    {
        _selected = hero;
        bool has = hero != null;
        _detailRoot.SetActive(has);
        _emptyHint.SetActive(!has);
        _chooseButton.interactable = has;
        _chooseLabel.text = has ? "ESCOLHER " + HeroSkillInfo.For(hero.Id).Name : "ESCOLHER";

        foreach (var c in _cards)
        {
            bool on = c.Hero == hero;
            c.Root.anchoredPosition = new Vector2(c.Root.anchoredPosition.x, -(on ? CardY - 3f : CardY) * MenuKit.Px);
            c.Frame.sprite = MenuKit.Nine("card_" + c.Id + (on ? "_selecionado" : "_normal") + "_9s_48x48", 12, 12, 12, 12);
            c.Glow.enabled = on;
            if (c.Portrait != null) c.Portrait.color = on || !has ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            c.Name.color = on ? MenuKit.Gold : MenuKit.Dim;
            c.Tag.color = on ? MenuKit.Ivory : new Color32(120, 110, 130, 255);
        }

        int[] values = has ? new[] { hero.MaxHealth, hero.Strength, hero.Power, hero.Defense, hero.Agility } : new int[Stats.Length];
        var fill = has ? MenuKit.Sprite("Menu/ui_kit/barra_atributo_fill_" + hero.Id + "_tile_4x4", default, true) : null;
        for (int k = 0; k < Stats.Length; k++)
        {
            float ratio = Mathf.Clamp01(values[k] / (float)Stats[k].Max);
            _statFills[k].sprite = fill;
            _statFills[k].enabled = has && ratio > 0f;
            _statFills[k].rectTransform.sizeDelta = new Vector2(Mathf.Round(BarInner * ratio) * MenuKit.Px, 4 * MenuKit.Px);
            _statValues[k].text = has ? values[k].ToString() : "—";
        }
        if (!has) return;

        var info = HeroSkillInfo.For(hero.Id);
        _name.text = info.Name;
        _name.color = info.NameColor;
        _role.text = info.RoleLine;
        _desc.text = info.Description;
        string[] tagSprite = { "etiqueta_basico_9s_10x9", "etiqueta_especial_9s_10x9", "etiqueta_supremo_9s_10x9" };
        for (int k = 0; k < 3; k++)
        {
            var s = info.Skills[k];
            var r = _rows[k];
            r.Icon.sprite = MenuKit.Sprite("Menu/selecao/" + s.Icon);
            r.TagImg.sprite = MenuKit.Nine(tagSprite[k], 3, 3, 3, 3);
            r.TagText.text = s.Tag;
            bool supremo = k == 2;
            float kw = s.Key.Length == 1 ? 14f : 28f;
            r.Key.sprite = MenuKit.Nine(supremo ? "tecla_ouro_9s_12x12" : "tecla_9s_12x12", 4, 4, 4, 4);
            r.Key.rectTransform.sizeDelta = new Vector2(kw * MenuKit.Px, 11 * MenuKit.Px);
            r.KeyText.rectTransform.sizeDelta = new Vector2(kw * MenuKit.Px, 10 * MenuKit.Px);
            r.KeyText.text = s.Key;
            r.Extra.text = s.Extra;
            r.Extra.rectTransform.anchoredPosition = new Vector2((313 + kw + 4) * MenuKit.Px, r.Extra.rectTransform.anchoredPosition.y);
            Color tipo = k == 0 ? (Color)new Color32(176, 172, 188, 255) : k == 1 ? info.EspecialColor : HeroSkillInfo.SupremoColor;
            r.Extra.color = tipo;
            r.Name.text = s.Name;
            r.Name.color = k == 0 ? MenuKit.Ivory : tipo;
            r.Desc.text = s.Desc;
        }
    }

    // smoke test: todos os textos do painel cabem nas caixas?
    public List<string> OverflowingTexts()
    {
        var bad = new List<string>();
        foreach (var t in _detailRoot.GetComponentsInChildren<Text>(true))
            if (!t.resizeTextForBestFit && !MenuKit.Fits(t)) bad.Add(t.text);
        foreach (var r in _rows)
        {
            // best fit: confere se chegou no tamanho mínimo e ainda assim estourou
            foreach (var t in new[] { r.Name, r.Desc })
            {
                int saved = t.fontSize; t.resizeTextForBestFit = false; t.fontSize = t.resizeTextMinSize; bool ok = MenuKit.Fits(t); t.fontSize = saved; t.resizeTextForBestFit = true;
                if (!ok) bad.Add(t.text);
            }
        }
        foreach (var c in _cards) { if (!MenuKit.Fits(c.Name)) bad.Add(c.Name.text); if (!MenuKit.Fits(c.Tag)) bad.Add(c.Tag.text); }
        if (!MenuKit.Fits(_chooseLabel)) bad.Add(_chooseLabel.text);
        return bad;
    }

    void Update()
    {
        if (_confirmPanel != null && _confirmPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseConfirm();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
        // atalhos 1/2/3
        for (int i = 0; i < _cards.Count; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Select(_cards[i].Hero); MenuKit.Select(_cards[i].Button.gameObject); }
    }

    void OpenConfirm()
    {
        if (_selected == null) return;
        _confirmText.text = "Tem certeza que deseja escolher o " + _selected.DisplayName + "?";
        _confirmPanel.SetActive(true);
    }

    void CloseConfirm()
    {
        _confirmPanel.SetActive(false);
        MenuKit.Select(_chooseButton.gameObject);
    }

    void Confirm()
    {
        GameManager.Instance.SelectHero(_selected);
        _confirmPanel.SetActive(false);
        SceneTransitionManager.Instance.Load(GameScenes.GroundT1);
    }
}

// foco de teclado num card já mostra o herói (setas ← → trocam e atualizam o painel)
public class HeroCardFocus : MonoBehaviour, UnityEngine.EventSystems.ISelectHandler
{
    System.Action _onKeyboardFocus;
    public void Setup(System.Action onKeyboardFocus) { _onKeyboardFocus = onKeyboardFocus; }
    public void OnSelect(UnityEngine.EventSystems.BaseEventData e)
    {
        if (e is UnityEngine.EventSystems.PointerEventData) return; // mouse/toque seleciona pelo clique
        _onKeyboardFocus?.Invoke();
    }
}
