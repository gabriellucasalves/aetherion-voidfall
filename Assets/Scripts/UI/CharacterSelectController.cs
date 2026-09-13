using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectController : MonoBehaviour
{
    static readonly (string Label, int Max)[] Stats =
    {
        ("VIDA", 150), ("FORÇA", 100), ("PODER", 100), ("DEFESA", 100), ("AGILIDADE", 100)
    };
    const float StatBarWidth = 330f;

    CharacterData[] _heroes;
    CharacterData _selected;
    GameObject _confirmPanel;
    GameObject _donePanel;
    Text _detailTitle;
    Text _detailBody;
    Text _confirmText;
    Image[] _cardBackgrounds;
    Image[] _statFills;
    Text[] _statValues;

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
        root.AddComponent<MenuBackground>();
        root.AddComponent<CharacterSelectController>();
    }

    void Start()
    {
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        _heroes = CharacterCatalog.Playable();
        BuildUi();
        if (_heroes.Length > 0)
            Select(_heroes[0]);
    }

    void BuildUi()
    {
        var canvas = UiKit.CreateCanvas(transform, "SelectCanvas");
        UiKit.Label(canvas.transform, "ESCOLHA SEU HERÓI", 42, new Vector2(0f, 430f), MenuTheme.CelestialGold, new Vector2(1200f, 70f));

        _cardBackgrounds = new Image[_heroes.Length];
        // Espaça Guerreiro / Mago lado a lado sem sobrepor.
        float spacing = 420f;
        float startX = -(_heroes.Length - 1) * spacing * 0.5f;
        for (int i = 0; i < _heroes.Length; i++)
        {
            var pos = new Vector2(startX + i * spacing, 140f);
            _cardBackgrounds[i] = BuildCard(canvas.transform, _heroes[i], pos);
        }

        // painel de detalhes: descrição à esquerda, barras de atributos à direita
        var detail = UiKit.Panel(canvas.transform, "Detalhe", new Vector2(1240f, 280f), new Vector2(0f, -220f), MenuTheme.Panel);
        _detailTitle = UiKit.Label(detail.transform, "", 28, new Vector2(-300f, 100f), MenuTheme.CelestialGold, new Vector2(560f, 40f));
        _detailBody = UiKit.Label(detail.transform, "", 19, new Vector2(-300f, -25f), MenuTheme.SoftIvory, new Vector2(540f, 190f));

        _statFills = new Image[Stats.Length];
        _statValues = new Text[Stats.Length];
        for (int i = 0; i < Stats.Length; i++)
        {
            float y = 95f - i * 44f;
            UiKit.Label(detail.transform, Stats[i].Label, 17, new Vector2(150f, y), MenuTheme.SoftIvory, new Vector2(150f, 30f));
            _statFills[i] = UiKit.Bar(detail.transform, "Barra" + Stats[i].Label,
                new Vector2(410f, y), new Vector2(StatBarWidth, 20f),
                new Color(0f, 0f, 0f, 0.55f), MenuTheme.CelestialGold);
            _statValues[i] = UiKit.Label(detail.transform, "", 16, new Vector2(615f, y), new Color(1f, 1f, 1f, 0.7f), new Vector2(80f, 30f));
        }

        UiKit.Button(canvas.transform, "ESCOLHER", new Vector2(0f, -340f), new Vector2(300f, 76f), OpenConfirm);
        UiKit.Button(canvas.transform, "VOLTAR", new Vector2(-420f, -340f), new Vector2(220f, 76f), () =>
        {
            SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
        });

        _confirmPanel = BuildOverlay(canvas.transform, "Confirmar");
        _confirmText = UiKit.Label(_confirmPanel.transform, "", 26, new Vector2(0f, 70f), MenuTheme.SoftIvory, new Vector2(700f, 80f));
        UiKit.Button(_confirmPanel.transform, "CONFIRMAR", new Vector2(-130f, -80f), new Vector2(220f, 60f), Confirm);
        UiKit.Button(_confirmPanel.transform, "VOLTAR", new Vector2(130f, -80f), new Vector2(200f, 60f), () => _confirmPanel.SetActive(false));
        _confirmPanel.SetActive(false);

        _donePanel = BuildOverlay(canvas.transform, "Pronto");
        UiKit.Label(_donePanel.transform, "Herói confirmado.", 28, new Vector2(0f, 70f), MenuTheme.CelestialGold, new Vector2(700f, 50f));
        UiKit.Label(_donePanel.transform, "O combate na superfície vem na próxima etapa.", 20, new Vector2(0f, 10f), MenuTheme.SoftIvory, new Vector2(700f, 50f));
        UiKit.Button(_donePanel.transform, "MENU", new Vector2(0f, -80f), new Vector2(200f, 60f), () =>
        {
            SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
        });
        _donePanel.SetActive(false);
    }

    Image BuildCard(Transform parent, CharacterData hero, Vector2 position)
    {
        var panel = UiKit.Panel(parent, hero.DisplayName, new Vector2(380f, 420f), position, MenuTheme.Panel);

        var frame = UiKit.Panel(panel.transform, "Vitrine", new Vector2(250f, 250f), new Vector2(0f, 60f), new Color(0.03f, 0.02f, 0.06f, 0.95f));
        UiKit.Panel(frame.transform, "Filete", new Vector2(250f, 4f), new Vector2(0f, -123f), hero.Accent);

        var portrait = HeroPortrait.Attach(frame.transform, hero.Id, new Vector2(0f, 4f), 230f);
        if (portrait == null)
        {
            // herói ainda sem arte: silhueta misteriosa
            UiKit.Label(frame.transform, "?", 84, new Vector2(0f, 10f), new Color(hero.Accent.r, hero.Accent.g, hero.Accent.b, 0.4f), new Vector2(120f, 120f));
            UiKit.Label(frame.transform, "EM BREVE", 14, new Vector2(0f, -70f), new Color(1f, 1f, 1f, 0.45f), new Vector2(180f, 30f));
        }

        UiKit.Label(panel.transform, hero.DisplayName.ToUpper(), 26, new Vector2(0f, -90f), MenuTheme.SoftIvory, new Vector2(280f, 40f));
        UiKit.Label(panel.transform, hero.Role, 16, new Vector2(0f, -130f), new Color(1f, 1f, 1f, 0.7f), new Vector2(280f, 40f));

        var button = panel.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => Select(hero));
        return panel;
    }

    GameObject BuildOverlay(Transform parent, string name)
    {
        var dim = UiKit.Panel(parent, name, new Vector2(1920f, 1080f), Vector2.zero, new Color(0f, 0f, 0f, 0.65f));
        UiKit.Panel(dim.transform, "Caixa", new Vector2(780f, 320f), Vector2.zero, MenuTheme.Panel);
        return dim.gameObject;
    }

    void Select(CharacterData hero)
    {
        _selected = hero;
        _detailTitle.text = hero.DisplayName.ToUpper() + "  ·  " + hero.Role;
        _detailBody.text = hero.Description + "\n\n" + hero.AbilityName + " — " + hero.AbilityDescription;

        int[] values = { hero.MaxHealth, hero.Strength, hero.Power, hero.Defense, hero.Agility };
        for (int i = 0; i < Stats.Length; i++)
        {
            UiKit.SetBar(_statFills[i], values[i] / (float)Stats[i].Max, StatBarWidth);
            _statFills[i].color = hero.Accent;
            _statValues[i].text = values[i].ToString();
        }

        for (int i = 0; i < _heroes.Length; i++)
            _cardBackgrounds[i].color = _heroes[i] == hero ? MenuTheme.ButtonHover : MenuTheme.Panel;
    }

    void OpenConfirm()
    {
        if (_selected == null)
            return;

        _confirmText.text = "Tem certeza que deseja escolher o " + _selected.DisplayName + "?";
        _confirmPanel.SetActive(true);
    }

    void Confirm()
    {
        GameManager.Instance.SelectHero(_selected);
        _confirmPanel.SetActive(false);
        SceneTransitionManager.Instance.Load(GameScenes.GroundT1);
    }
}
