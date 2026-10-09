using UnityEngine;
using UnityEngine.UI;

public class GroundT1Controller : MonoBehaviour
{
    // Fallback de debug (FASE 5: seleção já libera o Mago).
    // Caminho principal: CharacterCatalog.UnlockMagoInSelection.
    public const bool ForceMagoForTesting = false;

    StageData _stage;
    PlayerController _player;
    SimpleEnemySpawner _spawner;
    GameObject _pausePanel;
    GameObject _defeatPanel;
    GameObject _demoCompletePanel;
    Text _hudName;
    Text _lifeText;
    Text _shieldText;
    Text _shieldLabel;
    Image _lifeFill;
    Image _shieldFill;
    Image _resonanceFill;
    Text _resonanceText;
    SpecialController _special;
    GameObject _shieldTrack;
    Image _specialFill;
    Text _specialText;
    PlayerCombat _combat;
    static readonly Color ResonanceGold = new Color(1f, 0.839f, 0.251f); // #FFD640
    bool _showWard;
    Text _waveBanner;
    float _waveBannerLeft;
    const float BarWidth = 360f;
    bool _paused;
    bool _dead;
    bool _demoDone;
    int _kills;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != GameScenes.GroundT1)
            return;

        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<GroundT1Controller>() != null)
            return;

        var root = new GameObject("GroundT1");
        root.AddComponent<GroundT1Controller>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        GameManager.EnsureExists();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ClearFade();

        Physics2D.gravity = new Vector2(0f, -32f);
        _stage = StageData.CreateT1();
        var hero = ResolveHero();
        BuildStage();
        _player = SpawnHero(hero);
        _combat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        BindCamera();
        var rain = gameObject.AddComponent<RainField>();
        rain.Setup(Camera.main != null ? Camera.main.transform : _player.transform);
        BuildHud(hero);
        _spawner = gameObject.AddComponent<SimpleEnemySpawner>();
        _spawner.Setup(_player.transform, _stage);
    }

    void Update()
    {
        if (_waveBannerLeft > 0f)
        {
            _waveBannerLeft -= Time.deltaTime;
            if (_waveBannerLeft <= 0f && _waveBanner != null)
                _waveBanner.gameObject.SetActive(false);
        }

        if (_dead || _demoDone)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // Barra de carregamento do especial (todos os herois) tica com o tempo.
        RefreshSpecialBar();
        // Com a Ressonância cheia, o "PRONTO!" pulsa todo frame.
        bool resonanceLive = _special != null && (_special.Ready || _special.CooldownLeft > 0f);
        if (_showWard || resonanceLive)
            RefreshHud();
    }

    public void AnnounceWave(int wave, string enemyName)
    {
        RefreshHud();
        if (_waveBanner == null)
            return;

        int total = _spawner != null ? _spawner.TotalWaves : SimpleEnemySpawner.DemoWaveCount;
        _waveBanner.text = "ONDA " + wave + "/" + total + "   ·   " + enemyName.ToUpper();
        _waveBanner.gameObject.SetActive(true);
        _waveBannerLeft = 2.4f;
    }

    public void OnDemoComplete()
    {
        if (_dead || _demoDone)
            return;

        _demoDone = true;
        SpecialController.AbortActive();
        CinematicTime.End();
        Time.timeScale = 0f;
        if (_player != null)
        {
            _player.SetLocked(true);
            var combat = _player.GetComponent<PlayerCombat>();
            if (combat != null)
                combat.SetLocked(true);
        }
        if (_spawner != null)
            _spawner.SetLocked(true);
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
        if (_demoCompletePanel != null)
            _demoCompletePanel.SetActive(true);
    }

    CharacterData ResolveHero()
    {
        if (ForceMagoForTesting)
        {
            var mago = CharacterCatalog.ById("mago");
            if (mago != null)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.SelectHero(mago);
                return mago;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.SelectedHero != null)
            return GameManager.Instance.SelectedHero;

        var all = CharacterCatalog.Playable();
        var fallback = all.Length > 0 ? all[0] : null;
        if (GameManager.Instance != null)
            GameManager.Instance.SelectHero(fallback);
        return fallback;
    }

    void BuildStage()
    {
        ConfigureCamera();
        BuildBackdrop();
        BuildPlayfield();
    }

    void BuildBackdrop()
    {
        T1Scenery.Build(_stage);
    }

    void BuildPlayfield()
    {
        float groundHeight = 3.4f;
        float groundY = _stage.GroundTop - groundHeight * 0.5f;
        Solid("Chao", new Vector3(0f, groundY, 0f), new Vector2(_stage.HalfWidth * 2f + 6f, groundHeight), _stage.GroundColor, -8);

        Platform(-16.5f, 2.05f, 5.4f);
        Platform(-3.4f, 3.4f, 4.2f);
        Stair(8.2f, 5, 1.15f, 0.52f);
        Platform(22.5f, 3.9f, 4.2f);
    }

    void Platform(float x, float heightFromGround, float width)
    {
        const float thickness = 0.38f;
        float top = _stage.GroundTop + heightFromGround;
        var center = new Vector3(x, top - thickness * 0.5f, 0f);

        // colisor one-way: dá pra atravessar por baixo e pousar em cima
        var go = new GameObject("Plataforma");
        go.transform.position = center;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, thickness);
        collider.usedByEffector = true;
        var effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 165f;

        // visual de ruína (bordas irregulares, musgo, vinhas) baked em pixel art
        T1Scenery.RuinPlatform(center, width, thickness, 2);
    }

    void Stair(float startX, int steps, float stepWidth, float stepRise)
    {
        for (int i = 0; i < steps; i++)
            Platform(startX + i * (stepWidth * 0.85f), 0.7f + i * stepRise, stepWidth);
    }

    void ConfigureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = PixelPresentation.OrthoSize;
        if (camera.GetComponent<PixelPresentation>() == null)
            camera.gameObject.AddComponent<PixelPresentation>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = _stage.SkyColor;
        camera.transform.position = new Vector3(-20f, 0.6f, -10f);
        if (camera.GetComponent<CameraFollow>() == null)
            camera.gameObject.AddComponent<CameraFollow>();
    }

    PlayerController SpawnHero(CharacterData hero)
    {
        var go = new GameObject(hero != null ? hero.DisplayName : "Heroi");
        go.transform.position = new Vector3(-22f, _stage.GroundTop + 0.7f, 0f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 1f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = new Vector2(0.58f, 1.15f);
        collider.offset = new Vector2(0f, 0.08f);

        var health = go.AddComponent<HealthSystem>();
        health.Configure(hero != null ? hero.MaxHealth : 100);
        health.Changed += _ => RefreshHud();
        health.Died += OnPlayerDied;

        var player = go.AddComponent<PlayerController>();
        player.Setup(hero, _stage);

        var combat = go.AddComponent<PlayerCombat>();
        combat.Setup(hero, AbilityData.ForHero(hero));
        go.AddComponent<SpecialController>().Setup(hero);
        var meter = go.GetComponent<ResonanceMeter>();
        if (meter != null)
            meter.Changed += _ => RefreshHud();
        var shield = go.GetComponent<ShieldSystem>();
        if (shield != null)
            shield.Changed += _ => RefreshHud();

        var ward = go.GetComponent<MagicWard>();
        if (ward != null)
            ward.Changed += _ => RefreshHud();

        HeroAppearance.Build(go.transform, hero);
        return player;
    }

    void BindCamera()
    {
        var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null)
            follow.Configure(_player.transform, _stage);
    }

    void BuildHud(CharacterData hero)
    {
        var canvas = UiKit.CreateCanvas(transform, "HudT1");
        UiKit.Label(canvas.transform, _stage.DisplayName, 22, new Vector2(0f, 470f), MenuTheme.CelestialGold, new Vector2(800f, 40f));
        _hudName = UiKit.Label(canvas.transform, "", 22, new Vector2(-620f, 430f), MenuTheme.SoftIvory, new Vector2(820f, 36f));
        _hudName.alignment = TextAnchor.MiddleLeft;

        UiKit.Label(canvas.transform, "VIDA", 14, new Vector2(-820f, 392f), new Color(1f, 0.45f, 0.38f), new Vector2(80f, 22f)).alignment = TextAnchor.MiddleLeft;
        _lifeFill = UiKit.Bar(canvas.transform, "BarraVida", new Vector2(-560f, 392f), new Vector2(BarWidth, 18f), new Color(0.12f, 0.05f, 0.06f, 0.85f), new Color(0.78f, 0.18f, 0.2f));
        _lifeText = UiKit.Label(canvas.transform, "", 14, new Vector2(-300f, 392f), MenuTheme.SoftIvory, new Vector2(140f, 22f));
        _lifeText.alignment = TextAnchor.MiddleLeft;

        _shieldLabel = UiKit.Label(canvas.transform, "ESCUDO", 14, new Vector2(-820f, 364f), new Color(0.7f, 0.82f, 0.95f), new Vector2(90f, 22f));
        _shieldLabel.alignment = TextAnchor.MiddleLeft;
        _shieldFill = UiKit.Bar(canvas.transform, "BarraEscudo", new Vector2(-560f, 364f), new Vector2(BarWidth, 16f), new Color(0.07f, 0.1f, 0.16f, 0.85f), new Color(0.55f, 0.72f, 0.92f));
        _shieldTrack = _shieldFill.transform.parent.gameObject;
        _shieldText = UiKit.Label(canvas.transform, "", 14, new Vector2(-300f, 364f), MenuTheme.SoftIvory, new Vector2(140f, 22f));
        _shieldText.alignment = TextAnchor.MiddleLeft;

        // Barra de carregamento do ESPECIAL - dedicada, para os 3 herois.
        var specialLabel = UiKit.Label(canvas.transform, "ESPECIAL", 14, new Vector2(-820f, 336f), MenuTheme.CelestialGold, new Vector2(110f, 22f));
        specialLabel.alignment = TextAnchor.MiddleLeft;
        _specialFill = UiKit.Bar(canvas.transform, "BarraEspecial", new Vector2(-560f, 336f), new Vector2(BarWidth, 16f), new Color(0.08f, 0.06f, 0.12f, 0.85f), SpecialColor(hero));
        _specialText = UiKit.Label(canvas.transform, "", 14, new Vector2(-300f, 336f), MenuTheme.SoftIvory, new Vector2(160f, 22f));
        _specialText.alignment = TextAnchor.MiddleLeft;

        bool showShield = hero != null && hero.Id == "guerreiro";
        // Arqueiro: sem barra de escudo (o especial ganhou barra dedicada acima).
        _showWard = hero != null && hero.Id == "mago";
        if (_shieldLabel != null)
        {
            _shieldLabel.gameObject.SetActive(showShield || _showWard);
            if (_showWard)
            {
                _shieldLabel.text = "CAMPO";
                _shieldLabel.color = new Color(0.55f, 0.78f, 1f);
            }
        }
        if (_shieldTrack != null)
            _shieldTrack.SetActive(showShield || _showWard);
        if (_shieldText != null)
            _shieldText.gameObject.SetActive(showShield || _showWard);
        if (_shieldFill != null && _showWard)
            _shieldFill.color = new Color(0.45f, 0.7f, 1f);

        UiKit.Label(canvas.transform, "RESSONÂNCIA", 14, new Vector2(-870f, 308f), ResonanceGold, new Vector2(160f, 22f)).alignment = TextAnchor.MiddleLeft;
        _resonanceFill = UiKit.Bar(
            canvas.transform,
            "BarraRessonancia",
            new Vector2(-560f, 308f),
            new Vector2(BarWidth, 12f),
            new Color(0.12f, 0.1f, 0.03f, 0.85f),
            ResonanceGold);
        _resonanceText = UiKit.Label(canvas.transform, "", 14, new Vector2(-250f, 308f), MenuTheme.SoftIvory, new Vector2(180f, 22f));
        _resonanceText.alignment = TextAnchor.MiddleLeft;

        _special = _player != null ? _player.GetComponent<SpecialController>() : null;
        RefreshHud();
        UiKit.Label(canvas.transform, HintFor(hero), 16, new Vector2(0f, -480f), new Color(1f, 1f, 1f, 0.55f), new Vector2(1700f, 30f));
        MobileControls.Attach(transform);
        MobileControls.SetDefenseIcon(_showWard);
        RefreshHud();

        _waveBanner = UiKit.Label(canvas.transform, "", 32, new Vector2(0f, 290f), MenuTheme.CelestialGold, new Vector2(1000f, 48f));
        _waveBanner.gameObject.SetActive(false);

        _pausePanel = UiKit.Panel(canvas.transform, "Pausa", new Vector2(1920f, 1080f), Vector2.zero, new Color(0f, 0f, 0f, 0.62f)).gameObject;
        UiKit.Panel(_pausePanel.transform, "Caixa", new Vector2(520f, 280f), Vector2.zero, MenuTheme.Panel);
        UiKit.Label(_pausePanel.transform, "PAUSA", 36, new Vector2(0f, 70f), MenuTheme.CelestialGold, new Vector2(400f, 50f));
        UiKit.Button(_pausePanel.transform, "CONTINUAR", new Vector2(0f, -10f), new Vector2(240f, 58f), TogglePause);
        UiKit.Button(_pausePanel.transform, "MENU", new Vector2(0f, -85f), new Vector2(240f, 58f), BackToMenu);
        _pausePanel.SetActive(false);

        _defeatPanel = UiKit.Panel(canvas.transform, "Derrota", new Vector2(1920f, 1080f), Vector2.zero, new Color(0f, 0f, 0f, 0.72f)).gameObject;
        UiKit.Panel(_defeatPanel.transform, "Caixa", new Vector2(560f, 260f), Vector2.zero, MenuTheme.Panel);
        UiKit.Label(_defeatPanel.transform, "VOCÊ CAIU", 36, new Vector2(0f, 50f), MenuTheme.CelestialGold, new Vector2(500f, 50f));
        UiKit.Button(_defeatPanel.transform, "MENU", new Vector2(0f, -60f), new Vector2(240f, 58f), BackToMenu);
        _defeatPanel.SetActive(false);

        _demoCompletePanel = UiKit.Panel(canvas.transform, "DemoFim", new Vector2(1920f, 1080f), Vector2.zero, new Color(0f, 0f, 0f, 0.78f)).gameObject;
        UiKit.Panel(_demoCompletePanel.transform, "Caixa", new Vector2(720f, 360f), Vector2.zero, MenuTheme.Panel);
        UiKit.Label(_demoCompletePanel.transform, "PARABÉNS!", 40, new Vector2(0f, 110f), MenuTheme.CelestialGold, new Vector2(640f, 52f));
        UiKit.Label(
            _demoCompletePanel.transform,
            "Você concluiu a demo de Aetherion: Voidfall.\nObrigado por jogar — novas fases e ajustes vêm a caminho.",
            20,
            new Vector2(0f, 20f),
            MenuTheme.SoftIvory,
            new Vector2(640f, 90f));
        UiKit.Button(_demoCompletePanel.transform, "MENU", new Vector2(0f, -100f), new Vector2(240f, 58f), BackToMenu);
        _demoCompletePanel.SetActive(false);
    }

    public void RegisterKill()
    {
        _kills++;
        RefreshHud();
    }

    void RefreshHud()
    {
        if (_player == null)
            return;

        var hero = _player.Hero;
        var health = _player.GetComponent<HealthSystem>();
        var shield = _player.GetComponent<ShieldSystem>();
        string name = hero != null ? hero.DisplayName.ToUpper() : "HERÓI";
        string kit = hero != null ? hero.AbilityName.ToUpper() : "";
        if (_hudName != null)
        {
            string wave = "";
            if (_spawner != null && _spawner.Wave > 0)
                wave = "   ·   ONDA " + _spawner.Wave + "/" + _spawner.TotalWaves;
            _hudName.text = name + "   ·   " + kit + "   ·   ☠ " + _kills + wave;
        }

        float life = health != null ? health.Current : 0f;
        float lifeMax = health != null ? health.Max : 1f;
        UiKit.SetBar(_lifeFill, life / lifeMax, BarWidth);
        if (_lifeText != null)
            _lifeText.text = Mathf.CeilToInt(life) + " / " + Mathf.CeilToInt(lifeMax);

        if (shield != null && !_showWard)
        {
            UiKit.SetBar(_shieldFill, shield.Current / shield.Max, BarWidth);
            if (_shieldText != null)
                _shieldText.text = Mathf.CeilToInt(shield.Current) + " / " + Mathf.CeilToInt(shield.Max);
        }

        if (_showWard)
        {
            var ward = _player.GetComponent<MagicWard>();
            float n = ward != null ? ward.Normalized : 0f;
            UiKit.SetBar(_shieldFill, n, BarWidth);
            if (_shieldText != null)
            {
                if (ward != null && ward.IsActive)
                    _shieldText.text = "ATIVO";
                else
                    _shieldText.text = ward != null
                        ? Mathf.CeilToInt(ward.Charge) + " / " + Mathf.CeilToInt(ward.Max)
                        : "0";
            }
        }

        RefreshSpecialBar();
        RefreshResonanceBar();
    }

    // Barra dedicada: enche conforme o especial recarrega; pulsa quando pronto.
    void RefreshSpecialBar()
    {
        if (_specialFill == null || _combat == null)
            return;

        float left = _combat.SpecialCooldownLeft;
        float max = _combat.SpecialCooldownMax;
        float ready = max > 0f ? 1f - Mathf.Clamp01(left / max) : 1f;
        UiKit.SetBar(_specialFill, ready, BarWidth);

        Color color = SpecialColor(_player != null ? _player.Hero : null);
        if (left <= 0.05f)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            _specialFill.color = Color.Lerp(color, Color.white, 1f - pulse);
            if (_specialText != null)
                _specialText.text = "PRONTO (L/Q)";
        }
        else
        {
            _specialFill.color = color;
            if (_specialText != null)
                _specialText.text = left.ToString("0.0") + "s";
        }
    }

    static Color SpecialColor(CharacterData hero)
    {
        if (hero != null && hero.Id == "mago")
            return new Color(0.72f, 0.45f, 1f);
        if (hero != null && hero.Id == "arqueiro")
            return new Color(0.45f, 0.88f, 0.58f);
        return new Color(1f, 0.62f, 0.25f);
    }

    void RefreshResonanceBar()
    {
        if (_player == null || _resonanceFill == null)
            return;
        var meter = _player.GetComponent<ResonanceMeter>();
        float resonance = meter != null ? meter.Normalized : 0f;
        UiKit.SetBar(_resonanceFill, resonance, BarWidth);
        bool supremeReady = _special != null && _special.Ready;
        if (_resonanceText != null)
        {
            if (supremeReady)
            {
                _resonanceText.text = "PRONTO! [E]";
                var pulse = ResonanceGold;
                pulse.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
                _resonanceText.color = pulse;
            }
            else if (meter != null && meter.IsFull && _special != null && _special.CooldownLeft > 0.05f)
            {
                _resonanceText.color = ResonanceGold;
                _resonanceText.text = _special.CooldownLeft.ToString("0.0") + "s";
            }
            else
            {
                _resonanceText.color = MenuTheme.SoftIvory;
                float current = meter != null ? meter.Current : 0f;
                _resonanceText.text = Mathf.CeilToInt(current) + " / 100";
            }
        }

        MobileControls.SetSupremeVisible(supremeReady);
    }

    void OnPlayerDied(HealthSystem _)
    {
        if (_dead)
            return;

        _dead = true;
        SpecialController.AbortActive();
        CinematicTime.End();
        Time.timeScale = 0f;
        MobileControls.SetSupremeVisible(false);
        if (_player != null)
            _player.SetLocked(true);
        var combat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        if (combat != null)
            combat.SetLocked(true);
        if (_spawner != null)
            _spawner.SetLocked(true);
        if (_defeatPanel != null)
            _defeatPanel.SetActive(true);
    }

    public void RequestPause()
    {
        if (_dead || _demoDone || CinematicTime.IsActive || SpecialController.IsCinematic)
            return;
        if (!_paused)
            TogglePause();
    }

    void TogglePause()
    {
        if (_dead || _demoDone || CinematicTime.IsActive || SpecialController.IsCinematic)
            return;

        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
        if (_player != null)
        {
            _player.SetLocked(_paused);
            var combat = _player.GetComponent<PlayerCombat>();
            if (combat != null)
                combat.SetLocked(_paused);
        }
        if (_spawner != null)
            _spawner.SetLocked(_paused);
        if (_pausePanel != null)
            _pausePanel.SetActive(_paused);
    }

    void BackToMenu()
    {
        SpecialController.AbortActive();
        CinematicTime.End();
        Time.timeScale = 1f;
        Physics2D.gravity = new Vector2(0f, -9.81f);
        SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
    }

    static string HintFor(CharacterData hero)
    {
        if (MobileControls.ShouldShow() || MobileControls.IsVisible)
        {
            if (hero != null && hero.Id == "mago")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: orbe / campo / especial   |   SUPREMO quando a barra encher";
            if (hero != null && hero.Id == "guerreiro")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: corte / escudo / especial   |   SUPREMO quando a barra encher";
            if (hero != null && hero.Id == "arqueiro")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: flecha / especial / dash   |   SUPREMO quando a barra encher";
            return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: ataque / escudo";
        }
        if (hero != null && hero.Id == "mago")
            return "A/D andar   ·   ESPAÇO pular   ·   clique / J orbe reto   ·   S/K/direito campo   ·   L/Q Núcleo Arcano   ·   E supremo   ·   ESC pausa";
        if (hero != null && hero.Id == "arqueiro")
            return "A/D andar   ·   ESPAÇO pular   ·   clique / J flecha   ·   SHIFT dash   ·   L/Q rajada   ·   E supremo   ·   ESC pausa";
        return "A/D andar   ·   ESPAÇO pular   ·   clique / J corta   ·   L/Q onda   ·   E supremo   ·   S / K / direito bloqueia   ·   ESC pausa";
    }

    static GameObject CreateQuad(string name, Vector3 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        var texture = Texture2D.whiteTexture;
        renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        renderer.color = color;
        renderer.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static void Decor(string name, Vector3 position, Vector2 size, Color color, int order)
    {
        CreateQuad(name, position, size, color, order);
    }

    static void Solid(string name, Vector3 position, Vector2 size, Color color, int order)
    {
        // blocos de tijolo de pedra na paleta do fundo (cor mantida só como fallback)
        var go = T1Scenery.TiledBlock(name, position, size, order);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
    }
}
