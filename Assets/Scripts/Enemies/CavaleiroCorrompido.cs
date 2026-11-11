using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CavaleiroCorrompido : MonoBehaviour
{
    const float Ppu = PixelArt.Ppu; // 32
    static readonly Vector2 PivotPx = new Vector2(30f / 64f, 3f / 64f); // (30,61) a partir do topo-esq

    public enum State
    {
        Idle,
        Walk,
        RaisingShield,
        Charging,
        SlamWindup,
        Slam,
        VulnerableWindow,
        Hurt,
        Dead
    }

    State _state = State.Idle;
    float _stateTimer;
    float _faceDir = 1f;
    float _animTimer;
    int _animIndex;

    EnemyController _controller;
    HealthSystem _health;
    Rigidbody2D _body;
    Transform _target;
    SpriteRenderer _sr;

    // Sprite strips
    Sprite[] _idleFrames;
    Sprite[] _walkFrames;
    Sprite[] _chargeFrames;
    Sprite[] _windupFrames;
    Sprite[] _slamFrames;
    Sprite[] _hurtFrames;
    Sprite[] _deathFrames;

    bool _struckPlayerDuringCharge;
    float _attackCooldown;

    public State CurrentState => _state;
    public bool IsCharging => _state == State.Charging || _state == State.RaisingShield;
    public bool IsShieldUp => IsCharging;
    public bool IsVulnerableWindow => _state == State.VulnerableWindow;
    public bool IsSlamming => _state == State.Slam;
    public SpriteRenderer Renderer => _sr;
    public bool UsesV2Sheets => _idleFrames != null && _idleFrames.Length > 0 && _idleFrames[0].texture.name.EndsWith("_v2");

    void Awake()
    {
        _controller = GetComponent<EnemyController>();
        _health = GetComponent<HealthSystem>();
        _body = GetComponent<Rigidbody2D>();

        LoadSprites();

        var visGo = new GameObject("Visual");
        visGo.transform.SetParent(transform, false);
        visGo.transform.localPosition = Vector3.zero;
        visGo.transform.localScale = Vector3.one;
        _sr = visGo.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 9;
        if (_idleFrames != null && _idleFrames.Length > 0)
            _sr.sprite = _idleFrames[0];

        if (_controller != null)
        {
            _controller.ExternalBrain = true;
            _controller.DamageFilter = FilterDamage;
            if (_controller.Target != null)
            {
                _target = _controller.Target;
                _faceDir = _target.position.x >= transform.position.x ? 1f : -1f;
            }
        }

        if (_health != null)
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }
    }

    void Start()
    {
        if (_controller != null && _controller.Target != null)
            _target = _controller.Target;
        else
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p != null) _target = p.transform;
        }
    }

    static Sprite[] SliceStrip(string path, int cellW, int cellH, Vector2 pivot)
    {
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        int count = tex.width / cellW;
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(tex, new Rect(i * cellW, 0, cellW, cellH), pivot, Ppu, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    void LoadSprites()
    {
        string p = "Enemies/CavaleiroCorrompido/";
        _idleFrames   = SliceStrip(p + "cavaleiro_idle_4f_v2", 64, 64, PivotPx);
        _walkFrames   = SliceStrip(p + "cavaleiro_walk_6f_v2", 64, 64, PivotPx);
        _chargeFrames = SliceStrip(p + "cavaleiro_charge_4f_v2", 64, 64, PivotPx);
        _windupFrames = SliceStrip(p + "cavaleiro_slam_windup_3f_v2", 64, 64, PivotPx);
        _slamFrames   = SliceStrip(p + "cavaleiro_slam_3f_v2", 64, 64, PivotPx);
        _hurtFrames   = SliceStrip(p + "cavaleiro_hurt_2f_v2", 64, 64, PivotPx);
        _deathFrames  = SliceStrip(p + "cavaleiro_death_5f_v2", 64, 64, PivotPx);
    }

    public float FilterDamage(float amount)
    {
        if (_state == State.Dead) return 0f;

        // Escudo frontal bloqueia 70% durante o raise/charge (só golpes vindos da frente)
        if (IsShieldUp)
        {
            var target = _target != null ? _target : FindFirstObjectByType<PlayerController>()?.transform;
            if (target != null)
            {
                bool fromFront = (target.position.x - transform.position.x) * _faceDir > 0f;
                if (fromFront)
                    return amount * 0.30f; // reduz 70%, recebe 30%
            }
        }

        // Por trás ou durante o golpe / janela vulnerável: leva 100%
        return amount;
    }

    void OnDamaged(HealthSystem hs)
    {
        if (_state == State.Dead) return;
        VoidImpactFx.Spawn(transform.position + Vector3.up * 0.6f);
        if (_state != State.Charging && _state != State.Slam && _state != State.SlamWindup)
        {
            _state = State.Hurt;
            _stateTimer = 0.16f;
        }
    }

    void OnDied(HealthSystem hs)
    {
        _state = State.Dead;
        _stateTimer = 0.65f;
        VoidImpactFx.Spawn(transform.position + Vector3.up * 0.6f);
        if (_body != null) _body.linearVelocity = Vector2.zero;
        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;
    }

    void Update()
    {
        if (_state == State.Dead)
        {
            AnimateStrip(_deathFrames, 8f, loop: false);
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                Destroy(gameObject);
            return;
        }

        if (_target == null)
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p != null) _target = p.transform;
            if (_target == null) return;
        }

        float dx = _target.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:
            case State.Walk:
                _faceDir = dx >= 0f ? 1f : -1f;
                Face(_faceDir);
                if (dist > 5.2f)
                {
                    _state = State.Walk;
                    if (_body != null)
                        _body.linearVelocity = new Vector2(_faceDir * 2.2f, _body.linearVelocity.y);
                    AnimateStrip(_walkFrames, 10f, loop: true);
                }
                else
                {
                    if (_attackCooldown <= 0f)
                    {
                        StartCharge();
                    }
                    else
                    {
                        _state = State.Idle;
                        if (_body != null)
                            _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                        AnimateStrip(_idleFrames, 6f, loop: true);
                    }
                }
                break;

            case State.Charging:
                Face(_faceDir);
                if (_body != null)
                    _body.linearVelocity = new Vector2(_faceDir * 6.0f, _body.linearVelocity.y);
                AnimateStrip(_chargeFrames, 12f, loop: true);

                // Touch damage: investe a 6 u/s por 0.8s, causa 18 se encostar
                if (!_struckPlayerDuringCharge && dist < 1.4f)
                {
                    _struckPlayerDuringCharge = true;
                    EnemyController.DamagePlayer(_target, transform.position, 18f);
                }

                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartSlamWindup();
                }
                break;

            case State.SlamWindup:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                // 3 quadros em 0.5 s
                AnimateStrip(_windupFrames, 3f / 0.5f, loop: false);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    ExecuteSlam();
                }
                break;

            case State.Slam:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                AnimateStrip(_slamFrames, 12f, loop: false);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartVulnerableWindow();
                }
                break;

            case State.VulnerableWindow:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                AnimateStrip(_idleFrames, 6f, loop: true);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    _attackCooldown = 1.6f;
                    _state = State.Idle;
                }
                break;

            case State.Hurt:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                AnimateStrip(_hurtFrames, 10f, loop: false);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                    _state = State.Idle;
                break;
        }
    }

    public void ForceCharge()
    {
        StartCharge();
    }

    public void ForceSlam()
    {
        ExecuteSlam();
    }

    void StartCharge()
    {
        _state = State.Charging;
        _stateTimer = 0.8f;
        _struckPlayerDuringCharge = false;
        _animTimer = 0f;
        _animIndex = 0;
        if (_target == null && _controller != null && _controller.Target != null)
            _target = _controller.Target;
        if (_target == null)
            _target = FindFirstObjectByType<PlayerController>()?.transform;
        if (_target != null)
            _faceDir = _target.position.x >= transform.position.x ? 1f : -1f;
        Face(_faceDir);
    }

    void StartSlamWindup()
    {
        _state = State.SlamWindup;
        _stateTimer = 0.5f;
        _animTimer = 0f;
        _animIndex = 0;
    }

    void ExecuteSlam()
    {
        _state = State.Slam;
        _stateTimer = 0.35f;
        _animTimer = 0f;
        _animIndex = 0;

        // Golpe por cima: dano 22 perto
        if (_target != null)
        {
            float dx = _target.position.x - transform.position.x;
            if (Mathf.Abs(dx) <= 1.6f && Mathf.Abs(_target.position.y - transform.position.y) <= 1.8f)
            {
                EnemyController.DamagePlayer(_target, transform.position, 22f);
            }
        }

        // Onda de choque: corre 4 u pelo chão causando 12
        SpawnShockwave();
    }

    public CavaleiroShockwave SpawnShockwave()
    {
        Vector3 origin = transform.position + new Vector3(_faceDir * (22f / 32f), 0f, 0f);
        return CavaleiroShockwave.Spawn(origin, _faceDir, 12f, _target);
    }

    void StartVulnerableWindow()
    {
        _state = State.VulnerableWindow;
        _stateTimer = 1.0f; // 1.0s de recuperação vulnerável sem escudo
        _animTimer = 0f;
        _animIndex = 0;
    }

    void Face(float dir)
    {
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(dir);
        transform.localScale = s;
    }

    void AnimateStrip(Sprite[] frames, float fps, bool loop)
    {
        if (frames == null || frames.Length == 0 || _sr == null) return;
        _animTimer += Time.deltaTime;
        float frameDur = 1f / fps;
        if (_animTimer >= frameDur)
        {
            _animTimer -= frameDur;
            _animIndex++;
            if (_animIndex >= frames.Length)
            {
                if (loop) _animIndex = 0;
                else _animIndex = frames.Length - 1;
            }
        }
        _animIndex = Mathf.Clamp(_animIndex, 0, frames.Length - 1);
        _sr.sprite = frames[_animIndex];
    }
}

public class CavaleiroShockwave : MonoBehaviour
{
    static Sprite[] _frames;

    public static void EnsureSprites()
    {
        if (_frames != null && _frames.Length == 4) return;
        var tex = Resources.Load<Texture2D>("Enemies/CavaleiroCorrompido/cavaleiro_onda_choque_4f_32x24");
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        _frames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            _frames[i] = Sprite.Create(tex, new Rect(i * 32, 0, 32, 24), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
        }
    }

    public static CavaleiroShockwave Spawn(Vector3 origin, float dir, float damage, Transform target)
    {
        EnsureSprites();
        var go = new GameObject("CavaleiroShockwave");
        go.transform.position = origin;
        go.transform.localScale = new Vector3(Mathf.Sign(dir), 1f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.0f, 0.75f);
        col.offset = new Vector2(0f, 0.375f);

        var wave = go.AddComponent<CavaleiroShockwave>();
        wave.Setup(dir, damage, target);
        return wave;
    }

    float _dir = 1f;
    float _damage = 12f;
    Transform _target;
    SpriteRenderer _sr;
    float _startX;
    float _travelLimit = 4.0f; // corre 4u pelo chão
    float _speed = 4.5f;
    float _animTimer;
    int _animIndex;
    bool _hitPlayer;

    public void Setup(float dir, float damage, Transform target)
    {
        _dir = Mathf.Sign(dir);
        _damage = damage;
        _target = target;
        _startX = transform.position.x;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 9;
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    void Update()
    {
        transform.position += new Vector3(_dir * _speed * Time.deltaTime, 0f, 0f);

        if (_frames != null && _frames.Length > 0)
        {
            _animTimer += Time.deltaTime;
            if (_animTimer >= 1f / 12f)
            {
                _animTimer -= 1f / 12f;
                _animIndex = (_animIndex + 1) % _frames.Length;
                _sr.sprite = _frames[_animIndex];
            }
        }

        if (!_hitPlayer && _target != null)
        {
            float dx = Mathf.Abs(_target.position.x - transform.position.x);
            float dy = _target.position.y - transform.position.y;
            // Só atinge se estiver no chão / pulo baixo (altura < 0.8u)
            if (dx < 0.7f && dy >= -0.2f && dy < 0.85f)
            {
                _hitPlayer = true;
                EnemyController.DamagePlayer(_target, transform.position, _damage);
                VoidImpactFx.Spawn(transform.position + Vector3.up * 0.3f);
            }
        }

        if (Mathf.Abs(transform.position.x - _startX) >= _travelLimit)
        {
            Destroy(gameObject);
        }
    }
}

