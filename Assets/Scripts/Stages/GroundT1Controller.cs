using UnityEngine;
using UnityEngine.UI;

public class GroundT1Controller : MonoBehaviour
{
    // Fallback de debug (FASE 5: seleção já libera o Mago).
    // Caminho principal: CharacterCatalog.UnlockMagoInSelection.
    public const bool ForceMagoForTesting = false;

    StageData _stage;
    public StageData Stage => _stage;
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
    Image _supremeGlow;
    Image _supremeParticles;
    Image _supremeBanner;
    Sprite _specialNormalSprite;
    Sprite _specialReadySprite;
    Sprite[] _supremeFullFrames;
    Sprite[] _glowFrames;
    Sprite[] _particleFrames;
    Sprite[] _bannerKeyFrames;
    Sprite[] _bannerTouchFrames;
    Sprite _supremeNormalSprite;
    SpecialController _special;
    GameObject _shieldTrack;
    Image _specialFill;
    Text _specialText;
    PlayerCombat _combat;
    static readonly Color ResonanceGold = new Color(1f, 0.839f, 0.251f); // #FFD640
    bool _showWard;
    Text _waveBanner;
    float _waveBannerLeft;
    const float BarWidth = StageHud.BarWidth;
    bool _paused;
    bool _dead;
    bool _demoDone;
    int _kills;

    bool _inBossArena;
    public bool InBossArena => _inBossArena;
    public Text WaveBanner => _waveBanner;
    float _savedResonance;
    VoidDevourerBoss _boss;
    Transform _canvasTransform;
    Button _retryButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != GameScenes.GroundT1 && scene != GameScenes.GroundT2 && scene != GameScenes.GroundT3)
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
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isT3 = activeScene == GameScenes.GroundT3 || StageData.ActivePhase == 3;
        bool isT2 = activeScene == GameScenes.GroundT2 || StageData.ActivePhase == 2;
        if (isT3)
        {
            _stage = StageData.CreateT3();
            if (T3RuinsScenery.Enabled) T3RuinsScenery.ConfigureStage(_stage);
        }
        else if (isT2)
        {
            _stage = StageData.CreateT2();
            if (T2PalaceScenery.Enabled) T2PalaceScenery.ConfigureStage(_stage);
        }
        else
        {
            _stage = StageData.CreateT1();
            if (T1AtmosScenery.Enabled) T1AtmosScenery.ConfigureStage(_stage);
        }
        var hero = ResolveHero();
        BuildStage();
        _player = SpawnHero(hero);
        _combat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        BindCamera();
        // Fase diurna: sem chuva nesta fase
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

        if (_stage != null && _stage.Id == "t3" && !_inBossArena && _player != null && _player.transform.position.x >= 37.5f)
        {
            EnterBossArena();
        }

        if (_dead || _demoDone)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // Barra de carregamento do especial (todos os herois) tica com o tempo.
        RefreshSpecialBar();
        RefreshResonanceBar();
        // Com a Ressonância cheia, o "PRONTO!" pulsa todo frame.
        bool resonanceLive = _special != null && (_special.Ready || _special.CooldownLeft > 0f);
        if (_showWard || resonanceLive)
            RefreshHud();
    }

    public void AnnounceWave(int wave, string enemyName)
    {
        RefreshHud();
        if (_waveBanner == null || _inBossArena)
            return;

        int total = _spawner != null ? _spawner.TotalWaves : SimpleEnemySpawner.DemoWaveCount;
        if (_stage != null && _stage.Id == "t3")
        {
            _waveBanner.text = "Onda " + wave + " — " + enemyName;
        }
        else
        {
            _waveBanner.text = "ONDA " + wave + "/" + total + "   ·   " + enemyName.ToUpper();
        }
        _waveBanner.gameObject.SetActive(true);
        _waveBannerLeft = 2.4f;
    }

    public void OpenExitPortal()
    {
        if (_dead || _demoDone || FindFirstObjectByType<F1ExitPortal>() != null) return;
        bool isT2 = _stage != null && _stage.Id == "t2";
        F1ExitPortal.Spawn(this, new Vector2(32.5f, _stage.GroundTop));
        if (_player != null)
        {
            float px = _player.transform.position.x;
            float nearX = Mathf.Clamp(px + 4.5f, -33f, 29f);
            // Evita abrir em cima dos gaps
            if (!isT2 && nearX > -29.2f && nearX < -25.2f) nearX = -24.2f;
            if (nearX > -22.5f && nearX < -17.6f) nearX = -16.5f;
            if (Mathf.Abs(nearX - 32.5f) > 3f)
                F1ExitPortal.Spawn(this, new Vector2(nearX, _stage.GroundTop));
        }
        if (_waveBanner != null)
        {
            _waveBanner.text = isT2
                ? "PORTAL REAL ABERTO — ATRAVESSE PARA CONCLUIR A FASE II"
                : "PORTAL ANCESTRAL ABERTO — ATRAVESSE PARA CONCLUIR A FASE I";
            _waveBanner.gameObject.SetActive(true);
            _waveBannerLeft = 8f;
        }
    }

    public void OnDemoComplete()
    {
        if (_dead || _demoDone)
            return;

        _demoDone = true;
        bool isT2 = _stage != null && _stage.Id == "t2";
        WorldMapProgress.Complete(isT2 ? 2 : 1);
        StageData.ActivePhase = 1;
        SpecialController.AbortActive();
        CinematicTime.End();
        Time.timeScale = 1f;
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
        SceneTransitionManager.Instance.Load(GameScenes.WorldMap);
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
        T1FrontLevel.Reset();
        T1AtmosScenery.Reset();
        T2PalaceFrontLevel.Reset();
        T2PalaceScenery.Reset();
        T3RuinsFrontLevel.Reset();
        T3RuinsScenery.Reset();
        if (_stage != null && _stage.Id == "t3")
        {
            T3RuinsScenery.Build(_stage);
            return;
        }
        if (_stage != null && _stage.Id == "t2")
        {
            T2PalaceScenery.Build(_stage);
            return;
        }
        if (T1AtmosScenery.Enabled)
        {
            if (T1AtmosScenery.Build(_stage)) return;
            if (!T1AtmosScenery.AllowLegacyFallback) return;
        }
        GoldenAgeScenery.Build(_stage);
        if (T1HybridScenery.Enabled)
            T1HybridScenery.Apply(_stage);
    }

    void BuildPlayfield()
    {
        // Fundo híbrido com o kit da frente: chão em segmentos já montado pelo T1FrontLevel / T2PalaceFrontLevel / T3RuinsFrontLevel.
        if (T1AtmosScenery.Built || T1FrontLevel.Built || T2PalaceScenery.Built || T2PalaceFrontLevel.Built || T3RuinsScenery.Built || T3RuinsFrontLevel.Built)
            return;
        if (T1AtmosScenery.Enabled && !T1AtmosScenery.AllowLegacyFallback)
            return;

        float groundHeight = 3.4f;
        float groundY = _stage.GroundTop - groundHeight * 0.5f;
        Solid("Chao", new Vector3(0f, groundY, 0f), new Vector2(_stage.HalfWidth * 2f + 6f, groundHeight), _stage.GroundColor, -8);
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
        float camX = (_stage != null && _stage.Id == "t3") ? -55f : -31f;
        camera.transform.position = new Vector3(camX, 0.6f, -10f);
        if (camera.GetComponent<CameraFollow>() == null)
            camera.gameObject.AddComponent<CameraFollow>();
    }

    PlayerController SpawnHero(CharacterData hero)
    {
        var go = new GameObject(hero != null ? hero.DisplayName : "Heroi");
        float spawnX = (_stage != null && _stage.Id == "t3") ? -55f : -31f;
        go.transform.position = new Vector3(spawnX, _stage.GroundTop + 0.7f, 0f);

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
        if (T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built)
            foreach (var renderer in go.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sortingOrder == 12 || renderer.sortingOrder == 11) renderer.sortingOrder -= 2;
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
        _canvasTransform = canvas.transform;
        var hud = StageHud.Build(canvas.transform, hero, _stage.DisplayName);
        _hudName = hud.Name;
        _lifeFill = hud.Life; _lifeText = hud.LifeValue;
        _shieldFill = hud.Defense; _shieldText = hud.DefenseValue;
        _shieldLabel = hud.DefenseLabel;
        _shieldTrack = hud.Defense != null ? hud.Defense.transform.parent.gameObject : null;
        _specialFill = hud.Special; _specialText = hud.SpecialValue;
        _resonanceFill = hud.Supreme;
        _resonanceText = hud.SupremeValueText;
        _supremeGlow = hud.SupremeGlow;
        _supremeParticles = hud.SupremeParticles;
        _supremeBanner = hud.SupremeBanner;
        _specialNormalSprite = hud.SpecialNormal;
        _specialReadySprite = hud.SpecialReadySprite;
        _supremeFullFrames = hud.SupremeFullFrames;
        _glowFrames = hud.GlowFrames;
        _particleFrames = hud.ParticleFrames;
        _bannerKeyFrames = hud.BannerKeyFrames;
        _bannerTouchFrames = hud.BannerTouchFrames;
        if (_resonanceFill != null) _supremeNormalSprite = _resonanceFill.sprite;
        _showWard = hero != null && hero.Id == "mago";

        _special = _player != null ? _player.GetComponent<SpecialController>() : null;
        RefreshHud();
        StageHud.Footer(canvas.transform, HintFor(hero));
        MobileControls.Attach(transform);
        MobileControls.SetDefenseIcon(_showWard);
        RefreshHud();

        _waveBanner = UiKit.Label(canvas.transform, "", 32, new Vector2(0f, 230f), MenuTheme.CelestialGold, new Vector2(1000f, 48f));
        _waveBanner.gameObject.SetActive(false);

        BuildPhaseTitleCard(canvas.transform);

        _pausePanel = MenuPanels.Pause(canvas.transform, TogglePause, BackToMenu,
            WorldMapProgress.HighestCompleted > 0 ? (UnityEngine.Events.UnityAction)BackToMap : null);
        _defeatPanel = MenuPanels.Defeat(canvas.transform, RetryBoss, BackToMenu,
            WorldMapProgress.HighestCompleted > 0 ? (UnityEngine.Events.UnityAction)BackToMap : null, out _retryButton);
        MenuPanels.SetRetryVisible(_defeatPanel, _retryButton, false);
        _demoCompletePanel = MenuPanels.DemoComplete(canvas.transform, BackToMenu);
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
            _hudName.text = name + "   ·   " + _kills + " ABATES" + wave;
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

    // Barra dedicada: enche conforme o especial L/Q recarrega (para os 3 heróis).
    void RefreshSpecialBar()
    {
        if (_specialFill == null || _combat == null)
            return;

        float left = _combat.SpecialCooldownLeft;
        float max = _combat.SpecialCooldownMax;
        float ready = max > 0f ? 1f - Mathf.Clamp01(left / max) : 1f;
        UiKit.SetBar(_specialFill, ready, BarWidth);
        _specialFill.color = Color.white;

        if (_combat.SpecialReady)
        {
            if (_specialReadySprite != null) _specialFill.sprite = _specialReadySprite;
            if (_specialText != null) _specialText.text = "PRONTO (L/Q)";
        }
        else
        {
            if (_specialNormalSprite != null) _specialFill.sprite = _specialNormalSprite;
            if (_specialText != null) _specialText.text = left.ToString("0.0") + "s";
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

    static readonly int[] GlowPingPong = { 0, 1, 2, 3, 2, 1 };

    void RefreshResonanceBar()
    {
        if (_player == null || _resonanceFill == null)
            return;
        var meter = _player.GetComponent<ResonanceMeter>();
        float resonance = meter != null ? meter.Normalized : 0f;
        UiKit.SetBar(_resonanceFill, resonance, BarWidth);
        _resonanceFill.color = Color.white;

        bool cinematic = SpecialController.IsCinematic || (_boss != null && _boss.Cutscene != null);
        bool supremeReady = _special != null && _special.Ready;

        if (cinematic)
        {
            if (_supremeGlow != null) _supremeGlow.gameObject.SetActive(false);
            if (_supremeParticles != null) _supremeParticles.gameObject.SetActive(false);
            if (_supremeBanner != null) _supremeBanner.gameObject.SetActive(false);
            if (_resonanceText != null) _resonanceText.gameObject.SetActive(true);
            if (_supremeNormalSprite != null) _resonanceFill.sprite = _supremeNormalSprite;
            MobileControls.SetSupremeVisible(false);
            return;
        }

        if (supremeReady)
        {
            if (_supremeFullFrames != null && _supremeFullFrames.Length > 0)
                _resonanceFill.sprite = _supremeFullFrames[(int)(Time.unscaledTime * 10f) % _supremeFullFrames.Length];

            if (_supremeGlow != null)
            {
                _supremeGlow.gameObject.SetActive(true);
                if (_glowFrames != null && _glowFrames.Length > 0)
                {
                    int gIdx = GlowPingPong[(int)(Time.unscaledTime * 8f) % GlowPingPong.Length];
                    if (gIdx < _glowFrames.Length) _supremeGlow.sprite = _glowFrames[gIdx];
                }
            }

            if (_supremeParticles != null)
            {
                _supremeParticles.gameObject.SetActive(true);
                if (_particleFrames != null && _particleFrames.Length > 0)
                    _supremeParticles.sprite = _particleFrames[(int)(Time.unscaledTime * 12f) % _particleFrames.Length];
            }

            if (_supremeBanner != null)
            {
                _supremeBanner.gameObject.SetActive(true);
                var bFrames = MobileControls.IsVisible ? _bannerTouchFrames : _bannerKeyFrames;
                if (bFrames != null && bFrames.Length > 0)
                    _supremeBanner.sprite = bFrames[(int)(Time.unscaledTime / 0.35f) % bFrames.Length];
            }

            if (_resonanceText != null)
                _resonanceText.gameObject.SetActive(false);
        }
        else
        {
            if (_supremeNormalSprite != null)
                _resonanceFill.sprite = _supremeNormalSprite;
            if (_supremeGlow != null)
                _supremeGlow.gameObject.SetActive(false);
            if (_supremeParticles != null)
                _supremeParticles.gameObject.SetActive(false);
            if (_supremeBanner != null)
                _supremeBanner.gameObject.SetActive(false);

            if (_resonanceText != null)
            {
                _resonanceText.gameObject.SetActive(true);
                _resonanceText.color = MenuTheme.SoftIvory;
                if (meter != null && meter.IsFull && _special != null && _special.CooldownLeft > 0.05f)
                {
                    _resonanceText.text = "SUPREMO " + _special.CooldownLeft.ToString("0.0") + "s";
                }
                else
                {
                    float current = meter != null ? meter.Current : 0f;
                    _resonanceText.text = Mathf.CeilToInt(current) + " / 100";
                }
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
        {
            MenuPanels.SetRetryVisible(_defeatPanel, _retryButton, _inBossArena && _stage != null && _stage.Id == "t3");
            _defeatPanel.SetActive(true);
        }
    }

    void EnterBossArena()
    {
        _inBossArena = true;
        var gate = FindFirstObjectByType<RuinsArenaGate>();
        if (gate != null) gate.LockArena();

        var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.LockX(36f, 60f, _stage.GroundTop + 5.5f);

        var meter = _player != null ? _player.GetComponent<ResonanceMeter>() : null;
        _savedResonance = meter != null ? meter.Current : 0f;

        if (_spawner != null) _spawner.SetLocked(true);

        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (enemy != null && enemy.gameObject != gameObject)
            {
                PixelBurst.Spawn(enemy.transform.position + Vector3.up * 0.5f, new Color(0.65f, 0.25f, 0.85f), 6);
                Destroy(enemy.gameObject);
            }
        }

        if (_waveBanner != null)
        {
            _waveBanner.gameObject.SetActive(false);
            _waveBannerLeft = 0f;
        }

        var canvasParent = _canvasTransform != null ? _canvasTransform : FindFirstObjectByType<Canvas>()?.transform;
        if (canvasParent != null)
        {
            var bar = BossHealthBar.Create(canvasParent);
            _boss = VoidDevourerBoss.Spawn(new Vector3(48f, _stage.GroundTop, 0f), _player.transform, _stage, bar, isRetry: false);
        }
    }

    void RetryBoss()
    {
        _dead = false;
        Time.timeScale = 1f;

        if (_defeatPanel != null)
            _defeatPanel.SetActive(false);

        if (_player != null)
        {
            _player.SetLocked(false);
            _player.transform.position = new Vector3(38.5f, _stage.GroundTop + 0.7f, 0f);
            var rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            var health = _player.GetComponent<HealthSystem>();
            if (health != null) health.Configure(_player.Hero != null ? _player.Hero.MaxHealth : 100);

            var combat = _player.GetComponent<PlayerCombat>();
            if (combat != null) combat.SetLocked(false);

            var meter = _player.GetComponent<ResonanceMeter>();
            if (meter != null) meter.ForceSet(_savedResonance);
        }

        if (_boss != null)
        {
            if (_boss.HealthBar != null) Destroy(_boss.HealthBar.gameObject);
            Destroy(_boss.gameObject);
        }
        foreach (var b in FindObjectsByType<VoidDevourerBoss>(FindObjectsSortMode.None))
        {
            if (b.HealthBar != null) Destroy(b.HealthBar.gameObject);
            Destroy(b.gameObject);
        }

        var retryCanvasParent = _canvasTransform != null ? _canvasTransform : FindFirstObjectByType<Canvas>()?.transform;
        if (retryCanvasParent != null)
        {
            var bar = BossHealthBar.Create(retryCanvasParent);
            _boss = VoidDevourerBoss.Spawn(new Vector3(48f, _stage.GroundTop, 0f), _player.transform, _stage, bar, isRetry: true);
        }

        RefreshHud();
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

    void BackToMap()
    {
        SpecialController.AbortActive();
        CinematicTime.End();
        Time.timeScale = 1f;
        Physics2D.gravity = new Vector2(0f, -9.81f);
        SceneTransitionManager.Instance.Load(GameScenes.WorldMap);
    }

    static string HintFor(CharacterData hero)
    {
        if (MobileControls.ShouldShow() || MobileControls.IsVisible)
        {
            if (hero != null && hero.Id == "mago")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: pulso rúnico / campo / zona rúnica   |   SUPREMO quando a barra encher";
            if (hero != null && hero.Id == "guerreiro")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: corte / escudo / Feixe Infernal   |   ESPECIAL quando a barra encher";
            if (hero != null && hero.Id == "arqueiro")
                return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: tiro carregado / flecha perfurante / dash   |   SUPREMO quando a barra encher";
            return "Esquerda: arrasta para andar  ·  para cima pula   |   Direita: ataque / escudo";
        }
        if (hero != null && hero.Id == "mago")
            return "A/D andar   ·   ESPAÇO pular   ·   clique / J Pulso Rúnico   ·   S/K/direito campo   ·   L/Q Zona Rúnica   ·   E supremo   ·   ESC pausa";
        if (hero != null && hero.Id == "arqueiro")
            return "A/D andar   ·   ESPAÇO pular   ·   segurar J / clique tiro carregado   ·   SHIFT dash   ·   L/Q Flecha Perfurante   ·   E supremo   ·   ESC pausa";
        return "A/D andar   ·   ESPAÇO pular   ·   clique / J corta   ·   L/Q/E Feixe Infernal (barra cheia)   ·   S / K / direito bloqueia   ·   ESC pausa";
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
        // Colisor do chão sólido mantido; visual de tijolo escuro escondido
        var go = new GameObject(name);
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    void BuildPhaseTitleCard(Transform canvas)
    {
        var card = new GameObject("PhaseTitleCard");
        card.transform.SetParent(canvas, false);

        var cg = card.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var panel = UiKit.Panel(card.transform, "FundoCard", new Vector2(760f, 220f), new Vector2(0f, 20f), new Color(0.04f, 0.06f, 0.10f, 0.76f));
        panel.raycastTarget = false;

        var trimTop = UiKit.Panel(panel.transform, "TrimTop", new Vector2(760f, 3f), new Vector2(0f, 110f), new Color(0.93f, 0.78f, 0.38f, 0.85f));
        trimTop.raycastTarget = false;

        var trimBottom = UiKit.Panel(panel.transform, "TrimBottom", new Vector2(760f, 3f), new Vector2(0f, -110f), new Color(0.93f, 0.78f, 0.38f, 0.85f));
        trimBottom.raycastTarget = false;

        string indexText = _stage != null && !string.IsNullOrEmpty(_stage.PhaseIndex) ? _stage.PhaseIndex : "FASE I";
        string titleText = _stage != null && !string.IsNullOrEmpty(_stage.PhaseTitle) ? _stage.PhaseTitle : "AETHERION";
        string subText = _stage != null && !string.IsNullOrEmpty(_stage.PhaseSubtitle) ? _stage.PhaseSubtitle : "ANTES DA QUEDA";

        var lblIndex = UiKit.Label(panel.transform, indexText.ToUpper(), 20, new Vector2(0f, 58f), MenuTheme.CelestialGold, new Vector2(700f, 32f));
        lblIndex.raycastTarget = false;

        var lblTitle = UiKit.Label(panel.transform, titleText.ToUpper(), 44, new Vector2(0f, 8f), MenuTheme.SoftIvory, new Vector2(700f, 54f));
        lblTitle.raycastTarget = false;

        var lblSub = UiKit.Label(panel.transform, subText.ToUpper(), 22, new Vector2(0f, -48f), MenuTheme.CelestialGold, new Vector2(700f, 34f));
        lblSub.raycastTarget = false;

        card.AddComponent<PhaseTitleCardFader>();
    }
}

public class PhaseTitleCardFader : MonoBehaviour
{
    CanvasGroup _group;
    float _timer;
    const float HoldDuration = 3.8f;
    const float TotalDuration = 5.0f;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group != null)
        {
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= TotalDuration)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (_timer > HoldDuration && _group != null)
        {
            float t = (_timer - HoldDuration) / (TotalDuration - HoldDuration);
            _group.alpha = Mathf.Lerp(1f, 0f, t);
        }
    }
}
