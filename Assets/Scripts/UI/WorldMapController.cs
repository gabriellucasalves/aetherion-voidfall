using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Playable three-node route over the approved 1280x720 map plate.</summary>
public class WorldMapController : MonoBehaviour
{
    // Pixel coordinates on the map plate; the guide is approximate, so bends follow
    // the visible stone path rather than interpolating directly between the nodes.
    static readonly Vector2[] Route =
    {
        new Vector2(280.5f, 520.5f), new Vector2(309f, 506f),
        new Vector2(349f, 512f), new Vector2(390f, 498f),
        new Vector2(392f, 474f), new Vector2(413f, 457f),
        new Vector2(451f, 462f), new Vector2(480f, 480f),
        new Vector2(522f, 479f), new Vector2(553f, 459f),
        new Vector2(571f, 426f), new Vector2(575f, 383f),
        new Vector2(593f, 353f), new Vector2(630f, 331f),
        new Vector2(660f, 328f), new Vector2(686.5f, 336.5f),
        new Vector2(723f, 327f), new Vector2(755f, 352f),
        new Vector2(796f, 369f), new Vector2(829f, 349f),
        new Vector2(856f, 330f), new Vector2(888f, 315f),
        new Vector2(922f, 294f), new Vector2(950f, 273f),
        new Vector2(974.5f, 268.5f)
    };
    static readonly int[] NodeRouteIndex = { 0, 15, 24 };
    static readonly string[] PhaseNames = { "FASE 1 · SANTUÁRIO", "FASE 2 · RUÍNAS", "FASE 3 · FORTALEZA" };
    const float MapScale = 1.25f; // 1280x720 plate -> 1600x900 in the 1920x1080 canvas.
    const float WalkSpeed = 118f; // plate pixels per second.

    float[] _distances;
    float _position;
    bool _loading;
    RectTransform _heroRect;
    WorldMapHeroAnimator _heroAnimator;
    Text _status;
    Text _hint;
    Image[] _nodeIndicators;
    GameObject _notice;
    Text _noticeText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        if (SceneManager.GetActiveScene().name == GameScenes.WorldMap) EnsureExists();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<WorldMapController>() != null) return;
        new GameObject("WorldMapRuntime").AddComponent<WorldMapController>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        GameManager.EnsureExists();
        SceneTransitionManager.Instance?.ClearFade();
        _distances = new float[Route.Length];
        for (int i = 1; i < Route.Length; i++)
            _distances[i] = _distances[i - 1] + Vector2.Distance(Route[i - 1], Route[i]);
        _position = _distances[NodeRouteIndex[WorldMapProgress.CurrentNode - 1]];
        BuildUi();
        Refresh();
    }

    void Update()
    {
        if (_loading || _notice == null) return;
        if (_notice.activeSelf)
        {
            if (_heroAnimator != null) _heroAnimator.SetWalking(false, 0f);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
                _notice.SetActive(false);
            return;
        }

        float direction = Input.GetAxisRaw("Horizontal");
        bool moved = false;
        if (Mathf.Abs(direction) > 0.01f)
        {
            float old = _position;
            // The locked destination remains visible but cannot be entered.
            float limit = _distances[NodeRouteIndex[Mathf.Min(2, WorldMapProgress.HighestCompleted + 1)]];
            _position = Mathf.Clamp(_position + direction * WalkSpeed * Time.unscaledDeltaTime, 0f, limit);
            moved = Mathf.Abs(_position - old) > 0.001f;
            if (moved) Refresh();
        }
        if (_heroAnimator != null) _heroAnimator.SetWalking(moved, direction);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Confirm();
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
    }

    static Vector2 CanvasPoint(Vector2 platePoint) => new Vector2((platePoint.x - 640f) * MapScale, (360f - platePoint.y) * MapScale);

    Vector2 RoutePoint(float distance)
    {
        for (int i = 1; i < Route.Length; i++)
        {
            if (distance > _distances[i]) continue;
            float segment = _distances[i] - _distances[i - 1];
            return Vector2.Lerp(Route[i - 1], Route[i], segment > 0f ? (distance - _distances[i - 1]) / segment : 0f);
        }
        return Route[Route.Length - 1];
    }

    int NearNode()
    {
        for (int i = 0; i < NodeRouteIndex.Length; i++)
            if (Mathf.Abs(_position - _distances[NodeRouteIndex[i]]) < 45f) return i + 1;
        return 0;
    }

    void BuildUi()
    {
        var canvas = UiKit.CreateCanvas(transform, "WorldMapCanvas");
        var plate = UiKit.Panel(canvas.transform, "MapaIlha", new Vector2(1600f, 900f), Vector2.zero, Color.white);
        plate.sprite = Resources.Load<Sprite>("WorldMap/map_island_bg");
        plate.raycastTarget = false;
        if (plate.sprite == null) Debug.LogError("Mapa: placa limpa não importada.");

        _nodeIndicators = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var icon = UiKit.Panel(canvas.transform, "StatusFase" + (i + 1), new Vector2(60f, 60f), CanvasPoint(Route[NodeRouteIndex[i]]), Color.white);
            icon.raycastTarget = false;
            _nodeIndicators[i] = icon;
        }

        string hero = GameManager.Instance != null ? GameManager.Instance.SelectedHeroId : WorldMapProgress.SelectedHeroId;
        if (string.IsNullOrEmpty(hero)) hero = "guerreiro";
        var avatar = HeroPortrait.Attach(canvas.transform, hero, Vector2.zero, 86f);
        if (avatar != null)
        {
            _heroRect = avatar.rectTransform;
            var portraitAnimator = avatar.GetComponent<UiSpriteAnimator>();
            if (portraitAnimator != null) portraitAnimator.enabled = false;
            _heroAnimator = avatar.gameObject.AddComponent<WorldMapHeroAnimator>();
            _heroAnimator.Setup(avatar, hero);
        }

        var header = UiKit.Panel(canvas.transform, "Topo", new Vector2(870f, 84f), new Vector2(0f, 478f), MenuTheme.Panel);
        UiKit.Label(header.transform, "MAPA DE AETHERION", 31, new Vector2(0f, 15f), MenuTheme.CelestialGold, new Vector2(800f, 48f));
        _status = UiKit.Label(header.transform, "", 19, new Vector2(0f, -25f), MenuTheme.SoftIvory, new Vector2(800f, 32f));

        var footer = UiKit.Panel(canvas.transform, "Controles", new Vector2(1140f, 92f), new Vector2(0f, -475f), MenuTheme.Panel);
        _hint = UiKit.Label(footer.transform, "", 21, new Vector2(0f, 20f), MenuTheme.SoftIvory, new Vector2(1070f, 38f));
        UiKit.Label(footer.transform, "A / D ou ← / →  CAMINHAR     ENTER / ESPAÇO  ENTRAR     ESC  MENU", 17, new Vector2(0f, -22f), MenuTheme.CelestialGold, new Vector2(1070f, 30f));

        _notice = UiKit.Panel(canvas.transform, "Aviso", new Vector2(750f, 260f), Vector2.zero, new Color(0.05f, 0.04f, 0.1f, 0.98f)).gameObject;
        _noticeText = UiKit.Label(_notice.transform, "", 29, new Vector2(0f, 35f), MenuTheme.SoftIvory, new Vector2(700f, 100f));
        UiKit.Button(_notice.transform, "VOLTAR AO MAPA", new Vector2(0f, -72f), new Vector2(320f, 62f), () => _notice.SetActive(false));
        _notice.SetActive(false);
    }

    void Refresh()
    {
        if (_heroRect != null)
            _heroRect.anchoredPosition = CanvasPoint(RoutePoint(_position)) +
                (NearNode() != 0 ? new Vector2(-36f, 18f) : new Vector2(0f, 21f));
        for (int i = 0; i < 3; i++)
        {
            string state = WorldMapProgress.IsCompleted(i + 1) ? "cleared" : WorldMapProgress.IsUnlocked(i + 1) ? "unlocked" : "locked";
            _nodeIndicators[i].sprite = Resources.Load<Sprite>("WorldMap/node_" + state + "_32");
        }
        int node = NearNode();
        _status.text = WorldMapProgress.HighestCompleted == 0 ? "FASE 1 LIBERADA" : "FASE 1 CONCLUÍDA   ·   FASE 2 " + (WorldMapProgress.IsUnlocked(2) ? "LIBERADA" : "BLOQUEADA");
        _hint.text = node == 0 ? "Siga a trilha para o próximo destino" : PhaseNames[node - 1] + "  ·  " +
            (WorldMapProgress.IsCompleted(node) ? "CONCLUÍDA · REVISITAR" : WorldMapProgress.IsUnlocked(node) ? "LIBERADA" : "BLOQUEADA");
    }

    void Confirm()
    {
        int node = NearNode();
        if (node == 0) return;
        if (!WorldMapProgress.IsUnlocked(node))
        {
            ShowNotice("FASE BLOQUEADA");
            return;
        }
        WorldMapProgress.RememberNode(node);
        string scene = node == 1 ? GameScenes.GroundT1 : node == 2 ? "Ground_T2" : "Ground_T3";
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            ShowNotice("FASE " + node + " · EM DESENVOLVIMENTO");
            return;
        }
        _loading = true;
        SceneTransitionManager.Instance.Load(scene);
    }

    void ShowNotice(string message)
    {
        _noticeText.text = message;
        _notice.SetActive(true);
    }
}

/// <summary>Idle and walk from the selected hero's current 7-column, 64px gameplay sheet.</summary>
public class WorldMapHeroAnimator : MonoBehaviour
{
    Image _image;
    Sprite[] _idle;
    Sprite[] _walk;
    bool _walking;
    float _clock;

    public void Setup(Image image, string hero)
    {
        _image = image;
        _idle = hero == "mago" ? HeroPortrait.MagoIdle() : hero == "arqueiro" ? HeroPortrait.ArqueiroIdle() : HeroPortrait.GuerreiroIdle();
        string folder = hero == "mago" ? "Mago" : hero == "arqueiro" ? "Arqueiro" : "Guerreiro";
        var sheet = Resources.Load<Texture2D>(folder + "/sheet");
        if (sheet == null) return;
        sheet.filterMode = FilterMode.Point;
        _walk = new Sprite[6];
        for (int i = 0; i < _walk.Length; i++)
        {
            int cell = i + 4;
            _walk[i] = Sprite.Create(sheet,
                new Rect((cell % 7) * 64, sheet.height - (cell / 7 + 1) * 64, 64, 64),
                new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }
    }

    public void SetWalking(bool walking, float direction)
    {
        if (_walking != walking) _clock = 0f;
        _walking = walking;
        if (Mathf.Abs(direction) > 0.01f)
            transform.localScale = new Vector3(direction < 0f ? -1f : 1f, 1f, 1f);
    }

    void Update()
    {
        var frames = _walking && _walk != null ? _walk : _idle;
        if (_image == null || frames == null || frames.Length == 0) return;
        _clock += Time.unscaledDeltaTime;
        _image.sprite = frames[(int)(_clock * (_walking ? 10f : 3.2f)) % frames.Length];
    }
}
