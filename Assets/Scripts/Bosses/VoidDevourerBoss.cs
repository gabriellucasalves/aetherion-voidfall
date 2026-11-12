using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoidDevourerBoss : MonoBehaviour
{
    public enum State
    {
        Intro,
        Flying,
        Warning,
        Attacking,
        Descending,
        Transition,
        Defeated
    }

    public enum AttackType
    {
        DoubleGlare,
        RedSweep,
        TentacleEruption,
        SummonMinions,
        DescentExposed
    }

    static Sprite[] _vooFrames;
    static Sprite[] _olhosFrames;
    static Sprite[] _disparoFrames;
    static Sprite[] _descidaFrames;
    static Sprite _danoFlashSprite;
    static Sprite[] _morteFrames;

    static Sprite[] LoadStrip(string name, int fw, int fh, Vector2 pivot)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        int frames = tex.width / fw;
        var arr = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            arr[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), pivot, 32f, 0, SpriteMeshType.FullRect);
        }
        return arr;
    }

    public static void EnsureSprites()
    {
        if (_vooFrames != null) return;
        var pivot = new Vector2(0.5f, 0.5f);
        _vooFrames = LoadStrip("boss_voo_4f", 192, 192, pivot);
        _olhosFrames = LoadStrip("boss_olhos_carregando_3f", 192, 192, pivot);
        _disparoFrames = LoadStrip("boss_disparo_2f", 192, 192, pivot);
        _descidaFrames = LoadStrip("boss_descida_nucleo_3f", 192, 192, pivot);
        _morteFrames = LoadStrip("boss_morte_4f", 192, 192, pivot);

        var flashTex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/boss_dano_flash_1f");
        if (flashTex != null)
        {
            flashTex.filterMode = FilterMode.Point;
            flashTex.wrapMode = TextureWrapMode.Clamp;
            _danoFlashSprite = Sprite.Create(flashTex, new Rect(0, 0, flashTex.width, flashTex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
        }
    }

    public static VoidDevourerBoss Spawn(Vector3 arenaCenter, Transform player, StageData stage, BossHealthBar healthBar, bool isRetry = false)
    {
        EnsureSprites();
        var go = new GameObject("DevoradorDoVazio");
        go.transform.position = arenaCenter + Vector3.up * 4.8f;

        var boss = go.AddComponent<VoidDevourerBoss>();
        boss._target = player;
        boss._stage = stage;
        boss._healthBar = healthBar;
        boss._isRetry = isRetry;
        boss.Build();
        return boss;
    }

    Transform _target;
    StageData _stage;
    BossHealthBar _healthBar;
    public BossHealthBar HealthBar => _healthBar;
    bool _isRetry;

    State _state = State.Intro;
    public State CurrentState => _state;
    int _phase = 1; // Each form has its own complete health pool.
    public int Phase => _phase;

    public HealthSystem Health => _health;
    public EnemyController EnemyCtrl => _enemyController;
    public BossIntroCutscene Cutscene => _cutscene;
    public string CurrentSpeechText => _cutscene != null && _cutscene.SpeechText != null ? _cutscene.SpeechText.text : (_introMainText != null ? _introMainText.text : "");
    public float CurrentBossAlpha => _sr != null ? _sr.color.a : 1f;

    float _stateTimer;
    float _flightBobClock;
    float _nextContactDamageTime;
    float _summonCooldown;
    int _cycleIndex;

    // Supreme damage cap tracking
    float _supremeDamageAccumulator;
    float _supremeCapTimer;

    // Visuals & Physics
    SpriteRenderer _sr;
    Rigidbody2D _body;
    HealthSystem _health;
    EnemyController _enemyController;
    BoxCollider2D _bodyHurtbox;
    BossNucleus _nucleus;
    bool _isChargingLaser;
    bool _isFiringLaser;
    float _attackSubTimer;
    float _flashTimer;
    float _defeatTimer;
    public bool IsDissolving { get; set; }
    public bool IsChargingAttack => _isChargingLaser;
    public bool IsDamageFlashing => _flashTimer > 0f;

    // Intro dramatic speech & emergence
    float _introTotalDuration;
    string _introSpeechText;
    BossIntroCutscene _cutscene;
    GameObject _introSpeechGo;
    Text _introMainText;
    Text _introGlowText;
    CanvasGroup _introCanvasGroup;
    float _introStartY;
    float _introTargetY;

    // Active attack instances to cancel on transition
    List<BossLaser> _activeLasers = new List<BossLaser>();
    List<GroundTentacle> _activeTentacles = new List<GroundTentacle>();
    List<GameObject> _activeAims = new List<GameObject>();

    void Build()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 15;
        if (_vooFrames != null && _vooFrames.Length > 0)
            _sr.sprite = _vooFrames[0];

        _body = gameObject.AddComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.simulated = true;

        _bodyHurtbox = gameObject.AddComponent<BoxCollider2D>();
        _bodyHurtbox.isTrigger = true;
        _bodyHurtbox.size = new Vector2(3.5f, 3.5f);
        _bodyHurtbox.offset = Vector2.zero;

        _enemyController = gameObject.AddComponent<EnemyController>();
        _enemyController.ExternalBrain = true;
        _enemyController.DamageFilter = FilterIncomingDamage;

        var dummyData = ScriptableObject.CreateInstance<EnemyData>();
        dummyData.Id = "devorador_do_vazio";
        dummyData.DisplayName = BossData.DisplayName;
        dummyData.MaxHealth = Mathf.RoundToInt(BossData.MaxHealth);
        dummyData.Size = new Vector2(3.5f, 3.5f);
        dummyData.Color = new Color(0.75f, 0.15f, 0.45f);
        _enemyController.Setup(dummyData, _target, _stage);

        _health = _enemyController.Health;
        _health.Configure(BossData.MaxHealth);
        _health.Died += OnBossDied;
        _health.Changed += OnHealthChanged;

        // Child object for the exposed nucleus
        var nucGo = new GameObject("NucleoExposto");
        nucGo.transform.SetParent(transform, false);
        nucGo.transform.localPosition = new Vector3(-0.3125f, -0.9375f, 0f);
        _nucleus = nucGo.AddComponent<BossNucleus>();
        _nucleus.Setup(this);

        gameObject.AddComponent<BossFormVisual>().Setup(this);
        _healthBar?.SetForm(1);
        StartIntro();
    }

    void OnHealthChanged(HealthSystem h)
    {
        if (_healthBar != null)
            _healthBar.SetHealth(h.Current, h.Max);
        CheckPhaseThresholds();
    }

    void StartIntro()
    {
        GameAudio.Instance?.BeginBoss();
        _state = State.Intro;
        float voiceLength = GameAudio.Instance != null ? GameAudio.Instance.PlayBossVoice(_isRetry ? "BossRetry" : "BossIntro") : 0f;
        _introTotalDuration = Mathf.Max(_isRetry ? BossData.IntroDurationShort : BossData.IntroDurationFull, voiceLength + .8f);
        _stateTimer = _introTotalDuration;

        if (_health != null)
            _health.IsInvulnerable = true;

        _introTargetY = _stage != null ? _stage.GroundTop + 4.8f : 3.0f;
        _introStartY = _introTargetY - 3.0f;

        transform.position = new Vector3(transform.position.x, _introStartY, 0f);
        SetBossAlpha(0f);

        _introSpeechText = _isRetry ? BossDialogue.Retry : BossDialogue.Intro;

        if (_target != null) _target.GetComponent<HealthSystem>().IsInvulnerable = true;
        CreateIntroSpeechUi();

        if (_healthBar != null)
        {
            float fillDelay = Mathf.Max(.3f, _introTotalDuration - 1.5f);
            StartCoroutine(IntroBarRoutine(fillDelay));
        }
    }

    void SetBossAlpha(float a)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    void CreateIntroSpeechUi()
    {
        _cutscene = BossIntroCutscene.Create(_introSpeechText, _isRetry, _introTotalDuration);
        if (_cutscene != null)
            _introMainText = _cutscene.SpeechText;
    }

    IEnumerator IntroBarRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_healthBar != null && _state == State.Intro)
        {
            _healthBar.BeginIntroFill(1.0f);
        }
    }

    void Update()
    {
        if (_state == State.Defeated)
        {
            _defeatTimer += Time.deltaTime;
            UpdateAnimation();
            return;
        }

        UpdateSupremeCapTimer();
        CheckPlayerContact();

        if (_health != null)
        {
            _healthBar?.SetHealth(_health.Current, _health.Max);
            CheckPhaseThresholds();
        }

        switch (_state)
        {
            case State.Intro:
                UpdateIntro();
                break;
            case State.Flying:
                UpdateFlying();
                break;
            case State.Transition:
                UpdateTransition();
                break;
        }

        // Animate sprite row depending on state
        UpdateAnimation();
    }

    void UpdateIntro()
    {
        _stateTimer -= Time.deltaTime;
        float elapsed = _introTotalDuration - _stateTimer;
        float progress = Mathf.Clamp01(elapsed / _introTotalDuration);

        // 1. Interpolação Y com ease-out
        float ease = 1f - Mathf.Pow(1f - progress, 2.5f);
        float currentY = Mathf.Lerp(_introStartY, _introTargetY, ease);
        float bob = Mathf.Sin(Time.time * 3f) * 0.15f * ease;
        transform.position = new Vector3(transform.position.x, currentY + bob, 0f);

        // 2. Fade-in do alfa nos SpriteRenderers do boss (0 a 1)
        float bossAlpha = Mathf.Clamp01(progress / 0.75f);
        SetBossAlpha(bossAlpha);

        // 3. Camera shake leve durante a subida (0.08 - 0.12 u) decaindo
        if (progress < 0.65f)
        {
            float shakeAmp = Mathf.Lerp(0.11f, 0.02f, progress / 0.65f);
            CameraFollow.Shake(0.04f, shakeAmp);
        }

        // 4. Cutscene do boss
        if (_cutscene != null)
        {
            _cutscene.Tick(elapsed);
            if (_cutscene.SkipToEnd)
                _stateTimer = Mathf.Min(_stateTimer, 0.45f);
        }

        if (_stateTimer <= 0f)
        {
            _cutscene?.Close();
            _cutscene = null;
            if (_introSpeechGo != null)
                Destroy(_introSpeechGo);
            SetBossAlpha(1f);
            transform.position = new Vector3(transform.position.x, _introTargetY, 0f);
            if (_health != null)
                _health.IsInvulnerable = false;
            if (_target != null) _target.GetComponent<HealthSystem>().IsInvulnerable = false;
            _state = State.Flying;
            GameAudio.Instance?.BossCombatStarted();
            _stateTimer = 1.0f; // Initial rest before first attack
        }
    }

    void UpdateFlying()
    {
        _stateTimer -= Time.deltaTime * CadenceMultiplier;
        _flightBobClock += Time.deltaTime * 2.2f;

        // Hover smoothly across arena (x = 42 .. 54)
        float targetX = 48f;
        if (_target != null)
        {
            // Position on opposite side or offset from player
            targetX = _target.position.x < 48f ? 52.5f : 43.5f;
        }

        float curX = Mathf.MoveTowards(transform.position.x, targetX, 2.8f * BossData.FormCadence(_phase) * Time.deltaTime);
        float baseY = _stage != null ? _stage.GroundTop + 4.8f : 3.0f;
        float curY = baseY + Mathf.Sin(_flightBobClock) * 0.35f;
        transform.position = new Vector3(curX, curY, 0f);

        // Face towards arena center or player
        if (_target != null)
        {
            float dir = Mathf.Sign(_target.position.x - transform.position.x);
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * (dir >= 0f ? 1f : -1f), transform.localScale.y, 1f);
        }

        if (_stateTimer <= 0f)
        {
            ChooseNextAttack();
        }
    }

    float CadenceMultiplier => BossData.FormCadence(_phase) * ((_enemyController != null && _enemyController.IsSlowed) ? .85f : 1f);
    public float AttackDamageMultiplier => BossData.FormDamage(_phase);
    public NazorPose AnimationPose { get; private set; }
    float _poseStarted;
    public float PoseElapsed => Mathf.Max(0f, Time.time - _poseStarted);
    public int FiredVolleys { get; private set; }
    public int DiveLaunches { get; private set; }
    public void SetAnimationPose(NazorPose pose) { AnimationPose = pose; _poseStarted = Time.time; }

    void ChooseNextAttack()
    {
        _state = State.Attacking;

        if (_phase == 1)
        {
            // Re-aimed volleys, ground control, a crossing dive, then a punish window.
            switch (_cycleIndex % 5)
            {
                case 0: StartCoroutine(AttackDoubleGlare(volleys: 2, warningTime: 0.75f)); break;
                case 1: StartCoroutine(AttackTentacleEruption(count: 3, warningTime: 1.0f)); break;
                case 2: StartCoroutine(AttackDive()); break;
                case 3: StartCoroutine(AttackRedSweep(sweepBack: true, warningTime: 0.85f, dmg: 22f)); break;
                case 4: StartCoroutine(AttackDescentExposed(duration: 2.6f)); break;
            }
            _cycleIndex++;
        }
        else if (_phase == 2)
        {
            // The winged beast replaces eye lasers with jumpable sonic volleys.
            switch (_cycleIndex++ % 4)
            {
                case 0: StartCoroutine(AttackSonicBurst()); break;
                case 1: StartCoroutine(AttackTentacleEruption(4, .95f)); break;
                case 2: StartCoroutine(AttackDive()); break;
                case 3: StartCoroutine(AttackDescentExposed(2.6f)); break;
            }
        }
        else
        {
            // The crowned final form calls down telegraphed eclipse projectiles.
            switch (_cycleIndex++ % 4)
            {
                case 0: StartCoroutine(AttackEclipseRain()); break;
                case 1: StartCoroutine(AttackTentacleEruption(5, .85f, .35f)); break;
                case 2: StartCoroutine(AttackDive()); break;
                case 3: StartCoroutine(AttackDescentExposed(2.2f)); break;
            }
        }
    }

    IEnumerator AttackSonicBurst()
    {
        SetAnimationPose(NazorPose.Roar);
        _isChargingLaser = true;
        GameAudio.Emit(GameAudio.Cue.BossSonicCharge, transform.position);
        for (float t = 0f; t < .95f; t += Time.deltaTime)
        {
            if (Mathf.FloorToInt(t * 10f) != Mathf.FloorToInt((t - Time.deltaTime) * 10f))
                PixelBurst.Spawn(transform.position + Vector3.up * .8f, new Color(.85f, .40f, 1f), 2);
            yield return null;
        }
        _isChargingLaser = false;
        for (int i = 0; i < 2; i++)
        {
            if (_state != State.Attacking) yield break;
            float ground = _stage != null ? _stage.GroundTop : -2f;
            float direction = _target != null && _target.position.x < transform.position.x ? -1f : 1f;
            Vector3 origin = new Vector3(transform.position.x + direction * 1.4f, ground + .85f, 0f);
            SetAnimationPose(NazorPose.Release);
            NazorHazard.Sonic(this, origin, direction, 20f * AttackDamageMultiplier);
            GameAudio.Emit(GameAudio.Cue.BossSonicWave, origin);
            CameraFollow.Shake(.09f, .06f);
            yield return new WaitForSeconds(.65f);
            SetAnimationPose(NazorPose.Roar);
            yield return new WaitForSeconds(.30f);
        }
        FinishAttack(.55f);
    }

    IEnumerator AttackEclipseRain()
    {
        SetAnimationPose(NazorPose.Conjure);
        _isChargingLaser = true;
        for (int volley = 0; volley < 2; volley++)
        {
            if (_state != State.Attacking) yield break;
            float ground = _stage != null ? _stage.GroundTop : -2f;
            // Mark positions once: moving out of the rune always avoids that bolt.
            float target = Mathf.Clamp(_target != null ? _target.position.x : 48f, 40f, 56f);
            foreach (float offset in new[] { -4f, 0f, 4f })
                NazorHazard.Eclipse(this, new Vector3(Mathf.Clamp(target + offset, 38f, 58f), ground, 0f), 24f * AttackDamageMultiplier);
            GameAudio.Emit(GameAudio.Cue.BossVoidMark, transform.position);
            yield return new WaitForSeconds(1.05f);
            SetAnimationPose(NazorPose.Release);
            yield return new WaitForSeconds(.60f);
            SetAnimationPose(NazorPose.Conjure);
        }
        _isChargingLaser = false;
        FinishAttack(.8f);
    }

    IEnumerator AttackDive()
    {
        if (_state != State.Attacking) yield break;
        SetAnimationPose(NazorPose.Windup);
        _isChargingLaser = true;
        float ground = _stage != null ? _stage.GroundTop : -2f;
        Vector3 start = transform.position;
        Vector3 target = new Vector3(Mathf.Clamp(_target != null ? _target.position.x : 48f, 39.5f, 56.5f), ground + 1.6f, 0);
        Vector2 direction = (target - start).normalized;
        var warning = BossLaser.SpawnAimLine(null, start, direction, Vector3.Distance(start, target) + 2f, .85f);
        _activeAims.Add(warning);
        GameAudio.Emit(GameAudio.Cue.BossWarning, transform.position, .9f);
        // The marked dive is locked from the start; it never homes after launch.
        for (float t = 0; t < .85f; t += Time.deltaTime)
        {
            transform.position = start + new Vector3(0f, Mathf.Sin(t / .85f * Mathf.PI) * .45f, 0f);
            yield return null;
        }
        _activeAims.Remove(warning);
        if (warning != null) Destroy(warning);
        _isChargingLaser = false;
        SetAnimationPose(NazorPose.Dive);
        DiveLaunches++;
        GameAudio.Emit(GameAudio.Cue.BossRoar, transform.position, .8f);
        float side = Mathf.Sign(target.x - start.x);
        Vector3 end = new Vector3(Mathf.Clamp(target.x + side * 3f, 38.5f, 57.5f), ground + 1.9f, 0);
        for (float t = 0; t < .70f; t += Time.deltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / .70f);
            transform.position = Vector3.Lerp(start, end, u);
            CheckBiteHit();
            yield return null;
        }
        transform.position = end;
        SetAnimationPose(NazorPose.Recover);
        // A melee-reachable punish window follows every successful/evaded dive.
        _nucleus?.SetActive(true);
        yield return new WaitForSeconds(.75f);
        _nucleus?.SetActive(false);
        Vector3 flight = new Vector3(Mathf.Clamp(end.x, 41f, 55f), ground + 4.8f, 0);
        for (float t = 0; t < .65f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(end, flight, Mathf.SmoothStep(0f, 1f, t / .65f));
            yield return null;
        }
        FinishAttack(.45f);
    }

    bool CanSummon()
    {
        if (Time.time < _summonCooldown) return false;
        int alive = 0;
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (enemy != null && enemy.gameObject != gameObject && enemy.GetComponent<GroundTentacle>() == null)
                alive++;
        }
        return alive < 3;
    }

    IEnumerator AttackDoubleGlare(int volleys, float warningTime)
    {
        for (int v = 0; v < volleys; v++)
        {
            if (_state != State.Attacking) yield break;

            Vector3 eyeL = transform.position + new Vector3(-0.03125f - 0.45f, 0.34375f, 0f);
            Vector3 eyeR = transform.position + new Vector3(-0.03125f + 0.45f, 0.34375f, 0f);
            Vector3 targetPos = _target != null ? _target.position + Vector3.up * 0.4f : transform.position + Vector3.down * 4f;

            Vector2 dirL = (targetPos - eyeL).normalized;
            Vector2 dirR = (targetPos - eyeR).normalized;

            SetAnimationPose(NazorPose.Focus);
            var aimL = BossLaser.SpawnAimLine(transform.parent, eyeL, dirL, 25f, warningTime);
            var aimR = BossLaser.SpawnAimLine(transform.parent, eyeR, dirR, 25f, warningTime);
            _activeAims.Add(aimL);
            _activeAims.Add(aimR);

            GameAudio.Emit(GameAudio.Cue.BossWarning, transform.position, 0.8f);
            _isChargingLaser = true;
            _attackSubTimer = 0f;
            for (float t = 0f; t < warningTime; t += Time.deltaTime)
            {
                _attackSubTimer += Time.deltaTime;
                // Track early, then lock visibly for a fair 0.32 s dodge window.
                if (t < warningTime - .32f && _target != null)
                {
                    targetPos = _target.position + Vector3.up * .4f;
                    dirL = (targetPos - eyeL).normalized;
                    dirR = (targetPos - eyeR).normalized;
                    if (aimL != null) aimL.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirL.y, dirL.x) * Mathf.Rad2Deg);
                    if (aimR != null) aimR.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirR.y, dirR.x) * Mathf.Rad2Deg);
                }
                yield return null;
            }
            _activeAims.Remove(aimL);
            _activeAims.Remove(aimR);

            if (_state != State.Attacking)
            {
                _isChargingLaser = false;
                yield break;
            }

            _isChargingLaser = false;
            _isFiringLaser = true;
            SetAnimationPose(NazorPose.Release);
            FiredVolleys++;
            GameAudio.Emit(GameAudio.Cue.BossBeam, transform.position);
            var beamL = BossLaser.FireDoubleGlareBeam(eyeL, dirL, 16f * AttackDamageMultiplier, 0.5f);
            var beamR = BossLaser.FireDoubleGlareBeam(eyeR, dirR, 16f * AttackDamageMultiplier, 0.5f);
            _activeLasers.Add(beamL);
            _activeLasers.Add(beamR);

            yield return new WaitForSeconds(0.55f);
            _isFiringLaser = false;
            _activeLasers.Remove(beamL);
            _activeLasers.Remove(beamR);

            if (volleys > 1 && v < volleys - 1)
                yield return new WaitForSeconds(0.18f);
        }

        _isChargingLaser = false;
        _isFiringLaser = false;
        FinishAttack(pause: _phase == 3 ? 0.6f : _phase == 2 ? 0.8f : 1.0f);
    }

    IEnumerator AttackRedSweep(bool sweepBack, float warningTime, float dmg)
    {
        if (_state != State.Attacking) yield break;

        float groundY = _stage != null ? _stage.GroundTop : -2.0f;
        float startX = (_target != null && _target.position.x < 48f) ? 58.5f : 37.5f;
        float endX = startX > 48f ? 37.5f : 58.5f;

        GameAudio.Emit(GameAudio.Cue.BossWarning, transform.position, 0.8f);
        // Warning spark on ground
        SetAnimationPose(NazorPose.Focus);
        _isChargingLaser = true;
        _attackSubTimer = 0f;
        for (float t = 0f; t < warningTime; t += Time.deltaTime)
        {
            _attackSubTimer += Time.deltaTime;
            if (Random.value < 0.3f)
                PixelBurst.Spawn(new Vector3(startX, groundY + 0.1f, 0f), new Color(1f, 0.2f, 0.25f), 3);
            yield return null;
        }

        if (_state != State.Attacking)
        {
            _isChargingLaser = false;
            yield break;
        }

        _isChargingLaser = false;
        _isFiringLaser = true;

        // Forward sweep
        SetAnimationPose(NazorPose.Release);
        GameAudio.Emit(GameAudio.Cue.BossBeam, transform.position);
        var sweepBeam = BossLaser.FireRedSweepBeam(transform, new Vector3(startX, groundY, 0f), dmg * AttackDamageMultiplier);
        _activeLasers.Add(sweepBeam);

        float sweepDuration = 1.6f;
        for (float t = 0f; t < sweepDuration; t += Time.deltaTime)
        {
            if (_state != State.Attacking)
            {
                _isFiringLaser = false;
                if (sweepBeam != null) Destroy(sweepBeam.gameObject);
                yield break;
            }
            float curX = Mathf.Lerp(startX, endX, t / sweepDuration);
            sweepBeam.UpdateSweep(transform.position + new Vector3(-0.03125f, 0.34375f, 0f), new Vector3(curX, groundY, 0f));
            yield return null;
        }

        if (sweepBack)
        {
            yield return new WaitForSeconds(0.40f);
            for (float t = 0f; t < sweepDuration; t += Time.deltaTime)
            {
                if (_state != State.Attacking)
                {
                    _isFiringLaser = false;
                    if (sweepBeam != null) Destroy(sweepBeam.gameObject);
                    yield break;
                }
                float curX = Mathf.Lerp(endX, startX, t / sweepDuration);
                sweepBeam.UpdateSweep(transform.position + new Vector3(-0.03125f, 0.34375f, 0f), new Vector3(curX, groundY, 0f));
                yield return null;
            }
        }

        _isFiringLaser = false;
        if (sweepBeam != null)
        {
            _activeLasers.Remove(sweepBeam);
            Destroy(sweepBeam.gameObject);
        }

        FinishAttack(pause: _phase == 3 ? 0.6f : _phase == 2 ? 0.8f : 1.0f);
    }

    IEnumerator AttackTentacleEruption(int count, float warningTime, float waveDelay = 0f)
    {
        SetAnimationPose(NazorPose.Conjure);
        GameAudio.Emit(GameAudio.Cue.BossWarning, transform.position, 0.8f);
        if (_state != State.Attacking) yield break;

        float groundY = _stage != null ? _stage.GroundTop : -2.0f;
        List<float> xs = new List<float>();

        // 1st tentacle is right below hero
        float heroX = _target != null ? _target.position.x : 48f;
        heroX = Mathf.Clamp(heroX, 38f, 58f);
        xs.Add(heroX);

        // Others distributed across arena (at least 2 units apart)
        for (int i = 1; i < count; i++)
        {
            float candidateX = 48f;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                candidateX = Random.Range(37.5f, 58.5f);
                bool valid = true;
                foreach (float x in xs)
                {
                    if (Mathf.Abs(x - candidateX) < 2.0f)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid) break;
            }
            xs.Add(candidateX);
        }

        // Spawn tentacles
        for (int i = 0; i < xs.Count; i++)
        {
            if (_state != State.Attacking) yield break;
            var tentacle = GroundTentacle.Spawn(transform.parent, new Vector3(xs[i], groundY, 0f), this, warningTime);
            _activeTentacles.Add(tentacle);

            if (waveDelay > 0f && i < xs.Count - 1)
                yield return new WaitForSeconds(waveDelay);
        }

        yield return new WaitForSeconds(warningTime + 1.2f + 0.4f);
        _activeTentacles.Clear();

        FinishAttack(pause: _phase == 3 ? 0.6f : _phase == 2 ? 0.8f : 1.0f);
    }

    IEnumerator AttackSummonMinions()
    {
        GameAudio.Emit(GameAudio.Cue.BossRoar, transform.position, 0.8f);
        if (_state != State.Attacking) yield break;

        _summonCooldown = Time.time + (_phase >= 3 ? 10f : 13f);

        // Warning void rifts on floor
        float groundY = _stage != null ? _stage.GroundTop : -2.0f;
        float x1 = 43f;
        float x2 = 53f;

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            PixelBurst.Spawn(new Vector3(x1, groundY + 0.1f, 0f), new Color(0.65f, 0.25f, 0.85f), 2);
            PixelBurst.Spawn(new Vector3(x2, groundY + 0.1f, 0f), new Color(0.65f, 0.25f, 0.85f), 2);
            yield return null;
        }

        if (_state != State.Attacking) yield break;

        // Spawn 2 Cuspidor do Vazio
        SimpleEnemySpawner.SpawnAt(EnemyData.CreateVoidSpitter(), _target, _stage, new Vector2(x1, groundY));
        SimpleEnemySpawner.SpawnAt(EnemyData.CreateVoidSpitter(), _target, _stage, new Vector2(x2, groundY));

        FinishAttack(pause: _phase == 3 ? 0.6f : 0.8f);
    }

    IEnumerator AttackDescentExposed(float duration)
    {
        if (_state != State.Attacking) yield break;

        _state = State.Descending;
        SetAnimationPose(NazorPose.Recover);

        // Descend until nucleus is ~2 u above ground (reachable by melee and runic zone)
        float groundY = _stage != null ? _stage.GroundTop : -2.0f;
        float targetY = groundY + 2.8f; // Nucleus is at localY -0.9, so nucleus worldY ≈ groundY + 1.9f
        float startY = transform.position.y;

        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (_state != State.Descending) yield break;
            float y = Mathf.Lerp(startY, targetY, t / 0.6f);
            transform.position = new Vector3(transform.position.x, y, 0f);
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, targetY, 0f);

        // Enable exposed nucleus hurtbox!
        if (_nucleus != null) _nucleus.SetActive(true);

        // Golden pulsing nucleus window
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (_state != State.Descending)
            {
                if (_nucleus != null) _nucleus.SetActive(false);
                yield break;
            }
            if (Random.value < 0.2f)
                PixelBurst.Spawn(_nucleus.transform.position, new Color(1f, 0.88f, 0.35f), 2);
            yield return null;
        }

        if (_nucleus != null) _nucleus.SetActive(false);

        // 0.6s warning -> Mordida (bite lunge)
        SetAnimationPose(NazorPose.Windup);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (_state != State.Descending) yield break;
            // Shiver / windup
            transform.position += (Vector3)(Random.insideUnitCircle * 0.05f);
            yield return null;
        }

        // Bite lunge forward 3 units
        SetAnimationPose(NazorPose.Dive);
        float dir = _target != null ? Mathf.Sign(_target.position.x - transform.position.x) : 1f;
        Vector3 biteStart = transform.position;
        Vector3 biteEnd = biteStart + new Vector3(dir * 3.2f, -0.4f, 0f);

        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if (_state != State.Descending) yield break;
            transform.position = Vector3.Lerp(biteStart, biteEnd, t / 0.25f);
            CheckBiteHit();
            yield return null;
        }

        // Return to flight
        float returnBaseY = groundY + 4.8f;
        Vector3 returnPos = new Vector3(48f, returnBaseY, 0f);
        Vector3 lungeEnd = transform.position;

        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (_state != State.Descending) yield break;
            transform.position = Vector3.Lerp(lungeEnd, returnPos, t / 0.8f);
            yield return null;
        }

        FinishAttack(pause: _phase == 3 ? 0.6f : _phase == 2 ? 0.8f : 1.0f);
    }

    void CheckBiteHit()
    {
        if (Time.time < _nextContactDamageTime) return;
        var center = transform.position + Vector3.down * 0.4f;
        foreach (var col in Physics2D.OverlapBoxAll(center, new Vector2(2.5f, 2.0f), 0f))
        {
            var player = col.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                _nextContactDamageTime = Time.time + BossData.ContactCooldown;
                EnemyController.DamagePlayer(player.transform, transform.position, 20f * AttackDamageMultiplier);
                PixelBurst.Spawn(player.transform.position + Vector3.up * 0.5f, new Color(1f, 0.2f, 0.3f), 8);
                break;
            }
        }
    }

    void FinishAttack(float pause)
    {
        SetAnimationPose(NazorPose.Recover);
        _isChargingLaser = false;
        _isFiringLaser = false;
        _state = State.Flying;
        _stateTimer = pause;
    }

    public void ForceStateFlying()
    {
        _cutscene?.Close();
        _cutscene = null;
        if (_introSpeechGo != null)
            Destroy(_introSpeechGo);
        SetBossAlpha(1f);
        if (_health != null)
            _health.IsInvulnerable = false;
        transform.position = new Vector3(transform.position.x, _introTargetY > 0f ? _introTargetY : (_stage != null ? _stage.GroundTop + 4.8f : 3.0f), 0f);
        _isChargingLaser = false;
        _isFiringLaser = false;
        _state = State.Flying;
        SetAnimationPose(NazorPose.Hover);
        if (_target != null) _target.GetComponent<HealthSystem>().IsInvulnerable = false;
        if (_healthBar != null && !_healthBar.gameObject.activeSelf) _healthBar.BeginIntroFill(.2f);
        GameAudio.Instance?.BossCombatStarted();
        _stateTimer = 2.0f;
    }

    void OnDestroy()
    {
        CancelActiveAttacks();
        _cutscene?.Close();
        _cutscene = null;
        if (_introSpeechGo != null)
            Destroy(_introSpeechGo);
        if (_health != null)
        {
            _health.Died -= OnBossDied;
            _health.Changed -= OnHealthChanged;
        }
    }

    public void ForwardTentacleDamage(float amount)
    {
        if (_health != null && !_health.IsDead && _state != State.Transition && _state != State.Defeated)
        {
            _flashTimer = 0.06f;
            DamageSystem.Apply(_health, amount);
            _healthBar?.SetHealth(_health.Current, _health.Max);
            CheckPhaseThresholds();
        }
    }

    float FilterIncomingDamage(float amount)
    {
        if (_state == State.Intro || _state == State.Transition || _state == State.Defeated)
            return 0f;

        _flashTimer = 0.06f;

        // Flying body takes 60% damage
        float mult = BossData.FlyingDamageMultiplier;

        // Supreme damage cap check
        if (SpecialController.IsCinematic || _supremeCapTimer > 0f)
        {
            float available = Mathf.Max(0f, BossData.SupremeDamageCap - _supremeDamageAccumulator);
            float scaledAmount = amount * mult;
            if (scaledAmount > available)
            {
                scaledAmount = available;
            }
            _supremeDamageAccumulator += scaledAmount;
            return scaledAmount;
        }

        return amount * mult;
    }

    public void OnNucleusDamaged(float rawAmount)
    {
        if (_state == State.Intro || _state == State.Transition || _state == State.Defeated)
            return;

        _flashTimer = 0.06f;

        // Exposed nucleus takes 150% damage!
        float mult = BossData.ExposedNucleusMultiplier;
        float applied = rawAmount * mult;

        if (SpecialController.IsCinematic || _supremeCapTimer > 0f)
        {
            float available = Mathf.Max(0f, BossData.SupremeDamageCap - _supremeDamageAccumulator);
            if (applied > available) applied = available;
            _supremeDamageAccumulator += applied;
        }

        if (applied > 0f && _health != null)
        {
            DamageSystem.Apply(_health, applied);
            _healthBar?.SetHealth(_health.Current, _health.Max);
            PixelBurst.Spawn(_nucleus.transform.position, new Color(1f, 0.92f, 0.45f), 6);
            CheckPhaseThresholds();
        }
    }

    void UpdateSupremeCapTimer()
    {
        if (SpecialController.IsCinematic)
        {
            _supremeCapTimer = 2.5f;
        }
        else if (_supremeCapTimer > 0f)
        {
            _supremeCapTimer -= Time.deltaTime;
            if (_supremeCapTimer <= 0f)
            {
                _supremeDamageAccumulator = 0f;
            }
        }
    }

    void CheckPlayerContact()
    {
        if (_state == State.Intro || _state == State.Transition || _state == State.Defeated || Time.time < _nextContactDamageTime)
            return;

        foreach (var col in Physics2D.OverlapBoxAll(transform.position, new Vector2(3.2f, 3.2f), 0f))
        {
            var player = col.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                _nextContactDamageTime = Time.time + BossData.ContactCooldown;
                EnemyController.DamagePlayer(player.transform, transform.position, BossData.ContactDamage * AttackDamageMultiplier);
                break;
            }
        }
    }

    void CheckPhaseThresholds()
    {
        if (_health == null || _state == State.Defeated || _state == State.Transition)
            return;

        if (_health.Current <= 0f && _phase < 3)
            TriggerTransition(_phase + 1, _phase == 1 ? BossDialogue.Form2 : BossDialogue.Form3);
    }

    public void CancelActiveAttacks()
    {
        StopAllCoroutines();
        foreach (var hazard in FindObjectsByType<NazorHazard>(FindObjectsSortMode.None))
            if (hazard.Owner == this) Destroy(hazard.gameObject);
        _isChargingLaser = false;
        _isFiringLaser = false;
        foreach (var laser in _activeLasers) if (laser != null) Destroy(laser.gameObject);
        _activeLasers.Clear();
        foreach (var aim in _activeAims) if (aim != null) Destroy(aim);
        _activeAims.Clear();
        foreach (var t in _activeTentacles) if (t != null) Destroy(t.gameObject);
        _activeTentacles.Clear();
        if (_nucleus != null) _nucleus.SetActive(false);
    }

    void TriggerTransition(int targetPhase, string speech)
    {
        // Refill synchronously on health Changed, before HealthSystem emits Died.
        // A huge attack therefore advances exactly one form and never skips it.
        SpecialController.AbortActive();
        _phase = targetPhase;
        _state = State.Transition;
        SetAnimationPose(NazorPose.Transform);
        CancelActiveAttacks();
        _cycleIndex = 0;
        _supremeDamageAccumulator = 0f;
        _supremeCapTimer = 0f;
        _health.IsInvulnerable = true;
        _health.Configure(BossData.FormHealth(_phase));
        _healthBar?.SetForm(_phase);
        _healthBar?.ForceSync();
        GetComponent<BossFormVisual>()?.SetForm(_phase);
        float voiceLength = GameAudio.Instance != null ? GameAudio.Instance.PlayBossVoice(_phase == 2 ? "BossForm2" : "BossForm3") : 0f;
        _introTotalDuration = Mathf.Max(BossData.TransitionDuration, voiceLength + .7f);
        _stateTimer = _introTotalDuration;
        _cutscene = BossIntroCutscene.Create(speech, true, _introTotalDuration, GetComponent<BossFormVisual>().PortraitSprite);
        if (_target != null) _target.GetComponent<HealthSystem>().IsInvulnerable = true;
        SupremeFx.PlayFlash();
        CameraFollow.Shake(.3f, .18f);
        PixelBurst.Spawn(transform.position, new Color(.85f, .3f, 1f), 22);
    }

    void UpdateTransition()
    {
        _stateTimer -= Time.deltaTime;
        _cutscene?.Tick(_introTotalDuration - _stateTimer);
        if (_cutscene != null && _cutscene.SkipToEnd) _stateTimer = Mathf.Min(_stateTimer, .3f);
        if (_stateTimer <= 0f)
        {
            _cutscene?.Close();
            _cutscene = null;
            _health.IsInvulnerable = false;
            if (_target != null) _target.GetComponent<HealthSystem>().IsInvulnerable = false;
            GameAudio.Instance?.BossCombatStarted();
            _state = State.Flying;
            _stateTimer = 1.0f;
        }
    }

    void UpdateAnimation()
    {
        if (_sr == null) return;
        // Forms two/three have their own complete articulated body, including hits.
        if (_phase > 1) { _flashTimer = Mathf.Max(0f, _flashTimer - Time.deltaTime); return; }

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_danoFlashSprite != null)
            {
                _sr.sprite = _danoFlashSprite;
                return;
            }
        }

        switch (_state)
        {
            case State.Intro:
            case State.Flying:
            case State.Transition:
                if (_vooFrames != null && _vooFrames.Length > 0)
                    _sr.sprite = _vooFrames[Mathf.FloorToInt(Time.time * 8f) % _vooFrames.Length];
                break;
            case State.Attacking:
                if (_isChargingLaser && _olhosFrames != null && _olhosFrames.Length > 0)
                {
                    int f = Mathf.Clamp(Mathf.FloorToInt(_attackSubTimer * 6f), 0, _olhosFrames.Length - 1);
                    _sr.sprite = _olhosFrames[f];
                }
                else if (_isFiringLaser && _disparoFrames != null && _disparoFrames.Length > 0)
                {
                    int f = Mathf.FloorToInt(Time.time * 12f) % _disparoFrames.Length;
                    _sr.sprite = _disparoFrames[f];
                }
                else if (_vooFrames != null && _vooFrames.Length > 0)
                {
                    _sr.sprite = _vooFrames[Mathf.FloorToInt(Time.time * 8f) % _vooFrames.Length];
                }
                break;
            case State.Descending:
                if (_descidaFrames != null && _descidaFrames.Length > 0)
                    _sr.sprite = _descidaFrames[Mathf.FloorToInt(Time.time * 6f) % _descidaFrames.Length];
                break;
            case State.Defeated:
                if (_morteFrames != null && _morteFrames.Length > 0)
                {
                    int f = Mathf.Clamp(Mathf.FloorToInt(_defeatTimer * 6f), 0, IsDissolving ? _morteFrames.Length - 1 : 1);
                    _sr.sprite = _morteFrames[f];
                }
                break;
        }
    }

    void OnBossDied(HealthSystem _)
    {
        if (_state == State.Defeated || _phase < 3 || !_health.IsDead) return;
        _state = State.Defeated;
        SetAnimationPose(NazorPose.Dying);
        CancelActiveAttacks();

        var playerController = _target != null ? _target.GetComponent<PlayerController>() : null;
        var stageController = FindFirstObjectByType<GroundT1Controller>();
        BossDefeatCutscene.StartCutscene(this, playerController, stageController);
    }
}

public class BossNucleus : MonoBehaviour
{
    VoidDevourerBoss _boss;
    CircleCollider2D _collider;

    public void Setup(VoidDevourerBoss boss)
    {
        _boss = boss;
        _collider = gameObject.AddComponent<CircleCollider2D>();
        _collider.isTrigger = true;
        _collider.radius = 0.3125f; // 10 px / 32
        _collider.offset = Vector2.zero;
        _collider.enabled = false; // Initially inactive
    }

    public void SetActive(bool active)
    {
        if (_collider != null) _collider.enabled = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Intercept hero weapon hitboxes
        CheckHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Intercept lingering areas like Runic Zone
        CheckHit(other);
    }

    void CheckHit(Collider2D other)
    {
        if (_boss == null || _collider == null || !_collider.enabled) return;

        // If hit by ArcaneBolt or ArcaneZone or Warrior MeleeSlash or Arrow
        var bolt = other.GetComponent<ArcaneBolt>();
        if (bolt != null)
        {
            _boss.OnNucleusDamaged(bolt.Damage);
            Destroy(bolt.gameObject);
            return;
        }

        var arrow = other.GetComponent<Arrow>();
        if (arrow != null)
        {
            _boss.OnNucleusDamaged(arrow.Damage);
            Destroy(arrow.gameObject);
            return;
        }
    }
}
