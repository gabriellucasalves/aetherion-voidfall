using UnityEngine;
using UnityEngine.UI;

// História de abertura: cada página aparece com efeito de máquina de escrever.
// Espaço/Enter avança (ou completa a página se ainda está digitando).
public class IntroController : MonoBehaviour
{
    static readonly string[] Pages =
    {
        "Por milhares de anos, o Véu protegeu Aetherion.",
        "Até que algo despertou além das estrelas.",
        "O Véu foi destruído.",
        "E o Vazio atravessou.",
        "Aetherion está caindo.\nSuas torres viraram ruínas. Seus mortos não descansam.",
        "Mas ainda existem aqueles dispostos a lutar."
    };

    const float CharsPerSecond = 34f;

    int _page;
    float _typed;
    Text _body;
    Text _dots;
    Text _hint;
    bool _busy;

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
        root.AddComponent<MenuBackground>();
        root.AddComponent<IntroController>();
    }

    void Start()
    {
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        var canvas = UiKit.CreateCanvas(transform, "IntroCanvas");
        UiKit.Label(canvas.transform, "AETHERION: VOIDFALL", 30, new Vector2(0f, 380f), MenuTheme.CelestialGold, new Vector2(1400f, 60f));

        // moldura do texto: painel escuro com filete dourado em cima e embaixo
        var frame = UiKit.Panel(canvas.transform, "Moldura", new Vector2(1080f, 300f), new Vector2(0f, 20f), MenuTheme.Panel);
        UiKit.Panel(frame.transform, "FileteTopo", new Vector2(1080f, 3f), new Vector2(0f, 150f), new Color(0.93f, 0.78f, 0.38f, 0.5f));
        UiKit.Panel(frame.transform, "FileteBase", new Vector2(1080f, 3f), new Vector2(0f, -150f), new Color(0.93f, 0.78f, 0.38f, 0.5f));
        _body = UiKit.Label(frame.transform, "", 34, Vector2.zero, MenuTheme.SoftIvory, new Vector2(960f, 260f));

        _dots = UiKit.Label(canvas.transform, "", 26, new Vector2(0f, -200f), new Color(1f, 1f, 1f, 0.8f), new Vector2(600f, 40f));
        _hint = UiKit.Label(canvas.transform, "Toque para continuar", 18, new Vector2(0f, -240f), new Color(1f, 1f, 1f, 0.55f), new Vector2(800f, 40f));
        UiKit.Button(canvas.transform, "CONTINUAR", new Vector2(-140f, -300f), new Vector2(260f, 72f), Advance);
        UiKit.Button(canvas.transform, "PULAR", new Vector2(140f, -300f), new Vector2(220f, 72f), GoToSelect);

        RefreshDots();
    }

    void Update()
    {
        if (_busy)
            return;

        // digita a página atual aos poucos
        string page = Pages[_page];
        if (_typed < page.Length)
        {
            _typed = Mathf.Min(page.Length, _typed + CharsPerSecond * Time.deltaTime);
            _body.text = page.Substring(0, (int)_typed);
        }

        // dica pulsa devagar
        if (_hint != null)
        {
            var color = _hint.color;
            color.a = 0.35f + 0.3f * Mathf.Abs(Mathf.Sin(Time.time * 2f));
            _hint.color = color;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            Advance();
    }

    void Advance()
    {
        // primeiro clique completa a página; o próximo avança
        string page = Pages[_page];
        if (_typed < page.Length)
        {
            _typed = page.Length;
            _body.text = page;
            return;
        }

        _page++;
        if (_page >= Pages.Length)
        {
            GoToSelect();
            return;
        }

        _typed = 0f;
        _body.text = "";
        RefreshDots();
    }

    void RefreshDots()
    {
        if (_dots == null)
            return;

        var text = "";
        for (int i = 0; i < Pages.Length; i++)
            text += i == _page ? "●  " : "○  ";
        _dots.text = text.TrimEnd();
    }

    void GoToSelect()
    {
        if (_busy)
            return;

        _busy = true;
        SceneTransitionManager.Instance.Load(GameScenes.CharacterSelection);
    }
}
