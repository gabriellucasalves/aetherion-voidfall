using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuspidorDoVazio : MonoBehaviour
{
    const float Ppu = PixelArt.Ppu; // 32
    static readonly Vector2 PivotPx = new Vector2(30f / 64f, 3f / 64f); // (30,61) do topo-esq

    public enum State
    {
        Idle,
        Walk,
        Flee,
        Swell,
        Spit,
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

    Sprite[] _idleFrames;
    Sprite[] _swellFrames;
    Sprite[] _spitFrames;
    Sprite[] _hurtFrames;
    Sprite[] _deathFrames;

    float _spitCooldown = 2.6f;

    public State CurrentState => _state;
    public bool IsSwelling => _state == State.Swell;
    public bool IsSpitting => _state == State.Spit;
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
        string p = "Enemies/CuspidorDoVazio/";
        _idleFrames  = SliceStrip(p + "cuspidor_idle_6f_v2", 64, 64, PivotPx);
        _swellFrames = SliceStrip(p + "cuspidor_swell_3f_v2", 64, 64, PivotPx);
        _spitFrames  = SliceStrip(p + "cuspidor_spit_3f_v2", 64, 64, PivotPx);
        _hurtFrames  = SliceStrip(p + "cuspidor_hurt_2f_v2", 64, 64, PivotPx);
        _deathFrames = SliceStrip(p + "cuspidor_death_5f_v2", 64, 64, PivotPx);
    }

    void OnDamaged(HealthSystem hs)
    {
        if (_state == State.Dead) return;
        VoidImpactFx.Spawn(transform.position + Vector3.up * 0.45f);
        if (_state != State.Swell && _state != State.Spit)
        {
            _state = State.Hurt;
            _stateTimer = 0.15f;
        }
    }

    void OnDied(HealthSystem hs)
    {
        _state = State.Dead;
        _stateTimer = 0.55f;
        VoidImpactFx.Spawn(transform.position + Vector3.up * 0.45f);
        if (_body != null) _body.linearVelocity = Vector2.zero;
        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;
    }

    void Update()
    {
        if (_state == State.Dead)
        {
            AnimateStrip(_deathFrames, 10f, loop: false);
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

        _faceDir = dx >= 0f ? 1f : -1f;
        Face(_faceDir);

        if (_spitCooldown > 0f)
            _spitCooldown -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:
            case State.Walk:
            case State.Flee:
                if (_spitCooldown <= 0f)
                {
                    StartSwell();
                    break;
                }

                // Distanciamento: mantém 6 a 8 u; foge devagar se < 2.5 u
                if (dist < 2.5f)
                {
                    _state = State.Flee;
                    if (_body != null)
                        _body.linearVelocity = new Vector2(-_faceDir * 1.5f, _body.linearVelocity.y);
                    AnimateStrip(_idleFrames, 8f, loop: true);
                }
                else if (dist > 8.0f)
                {
                    _state = State.Walk;
                    if (_body != null)
                        _body.linearVelocity = new Vector2(_faceDir * 2.0f, _body.linearVelocity.y);
                    AnimateStrip(_idleFrames, 8f, loop: true);
                }
                else
                {
                    _state = State.Idle;
                    if (_body != null)
                        _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                    AnimateStrip(_idleFrames, 6f, loop: true);
                }
                break;

            case State.Swell:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                // Incha por 0.45 s (aviso)
                AnimateStrip(_swellFrames, 3f / 0.45f, loop: false);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartSpit();
                }
                break;

            case State.Spit:
                if (_body != null)
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                AnimateStrip(_spitFrames, 12f, loop: false);
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    _spitCooldown = 2.6f;
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

    public void ForceSpit()
    {
        StartSwell();
    }

    void StartSwell()
    {
        _state = State.Swell;
        _stateTimer = 0.45f;
        _animTimer = 0f;
        _animIndex = 0;
    }

    void StartSpit()
    {
        _state = State.Spit;
        _stateTimer = 0.25f;
        _animTimer = 0f;
        _animIndex = 0;

        LaunchGlob(_target != null ? _target.position : transform.position + new Vector3(_faceDir * 6f, 0f, 0f));
    }

    public VoidAcidGlob LaunchGlob(Vector3 targetPos)
    {
        // Boca: (+12, -14) px a partir do pivô
        Vector3 mouth = transform.position + new Vector3(_faceDir * (12f / 32f), 14f / 32f, 0f);
        return VoidAcidGlob.Launch(mouth, targetPos, 14f, _target);
    }

    public VoidAcidPool SpawnAcidPool(Vector3 position)
    {
        return VoidAcidPool.Spawn(position, _target);
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

public class VoidAcidGlob : MonoBehaviour
{
    static Sprite[] _frames;

    public static void EnsureSprites()
    {
        if (_frames != null && _frames.Length == 3) return;
        var tex = Resources.Load<Texture2D>("Enemies/CuspidorDoVazio/cuspidor_glob_acido_3f_16");
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        _frames = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            _frames[i] = Sprite.Create(tex, new Rect(i * 16, 0, 16, 16), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        }
    }

    public static VoidAcidGlob Launch(Vector3 from, Vector3 target, float damage, Transform playerTarget)
    {
        EnsureSprites();
        var go = new GameObject("VoidAcidGlob");
        go.transform.position = from;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;

        var glob = go.AddComponent<VoidAcidGlob>();
        glob.Setup(from, target, damage, playerTarget);
        return glob;
    }

    Vector3 _velocity;
    float _gravity = 14f;
    float _damage = 14f;
    Transform _playerTarget;
    SpriteRenderer _sr;
    float _animTimer;
    int _animIndex;
    float _life = 3.0f;
    bool _impacted;

    public void Setup(Vector3 from, Vector3 target, float damage, Transform playerTarget)
    {
        _damage = damage;
        _playerTarget = playerTarget;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 10;
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];

        // Lança em arco parabólico em direção ao alvo
        float dx = target.x - from.x;
        float dy = target.y - from.y;
        float flightTime = Mathf.Clamp(Mathf.Abs(dx) / 7.5f, 0.45f, 1.2f);
        float vx = dx / flightTime;
        float vy = (dy + 0.5f * _gravity * flightTime * flightTime) / flightTime;
        _velocity = new Vector3(vx, vy, 0f);
    }

    void Update()
    {
        if (_impacted) return;

        _velocity.y -= _gravity * Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

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

        // Teste de colisão com o jogador
        if (_playerTarget != null)
        {
            if (Vector2.Distance(transform.position, _playerTarget.position + Vector3.up * 0.4f) < 0.55f)
            {
                HitPlayer();
                return;
            }
        }

        // Teste de impacto com o solo
        var hit = Physics2D.OverlapCircle(transform.position, 0.2f);
        if (hit != null && !hit.isTrigger && hit.GetComponentInParent<EnemyController>() == null)
        {
            Impact(transform.position);
            return;
        }

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Impact(transform.position);
        }
    }

    void HitPlayer()
    {
        _impacted = true;
        if (_playerTarget != null)
        {
            EnemyController.DamagePlayer(_playerTarget, transform.position, _damage);
        }
        Impact(transform.position);
    }

    void Impact(Vector3 pos)
    {
        _impacted = true;
        VoidImpactFx.Spawn(pos);
        VoidAcidPool.Spawn(pos, _playerTarget);
        Destroy(gameObject);
    }
}

public class VoidAcidPool : MonoBehaviour
{
    static Sprite[] _frames;

    public static void EnsureSprites()
    {
        if (_frames != null && _frames.Length == 4) return;
        var tex = Resources.Load<Texture2D>("Enemies/CuspidorDoVazio/cuspidor_poca_acido_4f_48x16");
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        _frames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            _frames[i] = Sprite.Create(tex, new Rect(i * 48, 0, 48, 16), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
        }
    }

    public static VoidAcidPool Spawn(Vector3 pos, Transform target)
    {
        EnsureSprites();
        var go = new GameObject("VoidAcidPool");
        // Ajusta pé no chão usando raycast ou nível
        var hit = Physics2D.Raycast(pos + Vector3.up * 0.5f, Vector2.down, 2.0f);
        float groundY = hit.collider != null && !hit.collider.isTrigger ? hit.point.y : pos.y;
        go.transform.position = new Vector3(pos.x, groundY, 0f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Trigger ≈ 42x6 px
        col.size = new Vector2(42f / 32f, 6f / 32f);
        col.offset = new Vector2(0f, 3f / 32f);

        var pool = go.AddComponent<VoidAcidPool>();
        pool.Setup(target);
        return pool;
    }

    Transform _target;
    SpriteRenderer _sr;
    float _duration = 3.0f; // poça dura 3 s
    float _tickTimer;
    float _animTimer;
    int _animIndex;

    public float RemainingDuration => _duration;

    public void Setup(Transform target)
    {
        _target = target;
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 8;
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    void Update()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (_frames != null && _frames.Length > 0)
        {
            _animTimer += Time.deltaTime;
            if (_animTimer >= 1f / 6f)
            {
                _animTimer -= 1f / 6f;
                _animIndex = (_animIndex + 1) % _frames.Length;
                _sr.sprite = _frames[_animIndex];
            }
        }

        // Causa 6 a cada 0.5 s em quem estiver nela
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= 0.5f)
        {
            _tickTimer -= 0.5f;
            DamageTick();
        }
    }

    public void DamageTick()
    {
        var target = _target != null ? _target : FindFirstObjectByType<PlayerController>()?.transform;
        if (target != null)
        {
            float dx = Mathf.Abs(target.position.x - transform.position.x);
            float dy = target.position.y - transform.position.y;
            if (dx < (21f / 32f) && dy >= -0.2f && dy < 0.6f)
            {
                EnemyController.DamagePlayer(target, transform.position, 6f);
                VoidImpactFx.Spawn(target.position + Vector3.up * 0.2f, 0.75f);
            }
        }
    }
}

