using UnityEngine;

// F1: distância (osso Aether), salto/garras e tanque com investida. Sem explosão.
public class EnemyController : MonoBehaviour
{
    enum Phase
    {
        Chase,
        Windup,
        Active,
        Recover
    }

    EnemyData _data;
    Transform _target;
    StageData _stage;
    Rigidbody2D _body;
    EnemyTraversal _traversal;
    bool _struckPlayer;
    HealthSystem _health;
    float _touchReady;
    Phase _phase = Phase.Chase;
    float _phaseLeft;
    float _faceDir = 1f;

    float _stunLeft;
    float _slowLeft;
    float _patrolDir = 1f;
    float _patrolClock;
    bool _frozen;
    float _storedDamage;
    float _savedGravity = 1f;
    int _vitalMarks;

    public HealthSystem Health => _health;
    public EnemyData Data => _data;
    public bool IsWindingUp => _phase == Phase.Windup;
    public bool IsCharging => _phase == Phase.Active && _data != null && _data.AttackKind == "charge";
    public bool IsFrozen => _frozen;
    public float StoredDamage => _storedDamage;
    public int VitalMarks => _vitalMarks;
    public bool IsSlowed => _slowLeft > 0f;

    public static event System.Action<EnemyController, float> AnyDamaged;
    public static event System.Action<EnemyController> AnyDied;

    public void Setup(EnemyData data, Transform target, StageData stage)
    {
        _data = data;
        _target = target;
        _stage = stage;
        _body = GetComponent<Rigidbody2D>();
        _traversal = gameObject.AddComponent<EnemyTraversal>();
        _health = gameObject.AddComponent<HealthSystem>();
        _health.Configure(data.MaxHealth);
        _health.Died += OnDied;
    }

    void FixedUpdate()
    {
        if (_target == null || _data == null || _health == null || _health.IsDead || _body == null)
            return;

        if (_frozen)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        if (_slowLeft > 0f)
            _slowLeft -= Time.fixedDeltaTime;

        if (_stunLeft > 0f)
        {
            _stunLeft -= Time.fixedDeltaTime;
            var slide = _body.linearVelocity;
            slide.x = Mathf.MoveTowards(slide.x, 0f, 8f * Time.fixedDeltaTime);
            _body.linearVelocity = slide;
            Clamp();
            Face(_faceDir);
            return;
        }

        float dx = _target.position.x - transform.position.x;
        float dir = dx >= 0f ? 1f : -1f;
        float adx = Mathf.Abs(dx);
        float ady = Mathf.Abs(_target.position.y - transform.position.y);

        switch (_phase)
        {
            case Phase.Chase:
                Chase(dir, adx, ady);
                break;
            case Phase.Windup:
                Halt();
                _phaseLeft -= Time.fixedDeltaTime;
                if (_phaseLeft <= 0f)
                    Strike();
                break;
            case Phase.Active:
                ActiveStrike();
                break;
            case Phase.Recover:
                Halt();
                _phaseLeft -= Time.fixedDeltaTime;
                if (_phaseLeft <= 0f)
                    _phase = Phase.Chase;
                break;
        }

        Clamp();
        Face(_phase == Phase.Chase && _data.AttackKind != "charge" ? dir : _faceDir);
    }

    void Chase(float dir, float adx, float ady)
    {
        if (_data.AttackKind == "charge")
        {
            ChaseGolem(dir, adx, ady);
            return;
        }
        bool climb = _target.position.y - transform.position.y > 1.3f;
        if (T1AtmosScenery.Built && (climb || !_traversal.Grounded))
        {
            _traversal.Move(dir, _data.MoveSpeed, true);
            return;
        }
        if (_data.AttackKind == "bone")
        {
            if (adx >= 3.2f && adx <= _data.AttackRange && ady < 3f && HasLineOfSight())
            {
                BeginWindup(dir);
                return;
            }
            // Ranged recua quando pressionado e avança quando fora do alcance.
            float move = adx < 3.2f && !climb ? -dir : dir;
            _traversal.Move(move, MoveSpeed, climb);
        }
        else
        {
            if (_traversal.Grounded && adx < _data.AttackRange && ady < 1.5f && HasLineOfSight())
            {
                BeginWindup(dir);
                return;
            }
            _traversal.Move(dir, MoveSpeed, climb);
        }
    }

    float MoveSpeed => _data.MoveSpeed * (IsSlowed ? 0.55f : 1f);

    void ChaseGolem(float dir, float adx, float ady)
    {
        // Tanque terrestre: patrulha o apoio atual e só investe num alvo alcançável.
        bool reachable = ady < 1.15f && HasLineOfSight() &&
            EnemyTraversal.SurfaceAt(_target.position.x, _target.position.y + 0.2f, 1.5f, out var targetGround) &&
            EnemyTraversal.SurfaceAt(transform.position.x, transform.position.y + 0.2f, 0.5f, out var ownGround) &&
            Mathf.Abs(targetGround.point.y - ownGround.point.y) < 0.35f &&
            SameSurfacePath(transform.position.x, _target.position.x, ownGround.point.y);
        if (reachable && adx < _data.AttackRange && _traversal.Grounded)
        {
            BeginWindup(dir);
            return;
        }
        float move = reachable ? dir : _patrolDir;
        _patrolClock += Time.fixedDeltaTime;
        if (_patrolClock > 2.4f || !GolemSafe(move))
        {
            _patrolClock = 0f;
            _patrolDir = -move;
            move = _patrolDir;
        }
        if (!GolemSafe(move)) { Halt(); return; }
        _faceDir = move;
        var v = _body.linearVelocity;
        v.x = move * MoveSpeed;
        _body.linearVelocity = v;
    }

    bool GolemSafe(float dir)
    {
        if (!_traversal.SafeAhead(dir, 0.45f)) return false;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * (_data.Size.x * 0.5f), _data.Size.y * 0.5f);
        foreach (var hit in Physics2D.RaycastAll(origin, new Vector2(dir, 0f), 0.45f))
            if (EnemyTraversal.IsSurface(hit.collider)) return false;
        return true;
    }

    static bool SameSurfacePath(float from, float to, float groundY)
    {
        float length = Mathf.Abs(to - from);
        int steps = Mathf.CeilToInt(length / .3f);
        for (int i = 1; i < steps; i++)
        {
            float x = Mathf.Lerp(from, to, i / (float)steps);
            if (!EnemyTraversal.SurfaceAt(x, groundY + .2f, .5f, out var hit) ||
                Mathf.Abs(hit.point.y - groundY) > .35f)
                return false;
        }
        return true;
    }

    bool HasLineOfSight()
    {
        Vector2 from = transform.position + Vector3.up * (_data.Size.y * 0.65f);
        var targetCollider = _target.GetComponent<Collider2D>();
        Vector2 to = targetCollider != null ? targetCollider.bounds.center : _target.position;
        var delta = to - from;
        foreach (var hit in Physics2D.RaycastAll(from, delta.normalized, delta.magnitude))
            if (EnemyTraversal.IsSurface(hit.collider)) return false;
        return true;
    }

    void BeginWindup(float dir)
    {
        _faceDir = dir;
        _phase = Phase.Windup;
        _phaseLeft = _data.AttackWindup;
        _struckPlayer = false;
        Halt();
        if (_data.AttackKind == "charge")
            TrioVfxPlayer.Spawn("golem_carga_tremor", transform.position + Vector3.up * .7f,
                10, 24, 2, new Vector2(.5f, .5f), 8f);
    }

    void Strike()
    {
        if (_data.AttackKind == "bone")
        {
            var mouth = transform.position + new Vector3(_faceDir * 0.55f, _data.Size.y * 0.65f, 0f);
            AetherBone.Launch(mouth, _target, _data.AttackDamage);
            Recover();
            return;
        }
        _phase = Phase.Active;
        _phaseLeft = _data.AttackKind == "charge" ? 0.6f : 0.45f;
        if (_data.AttackKind == "claws")
            _traversal.Launch(_faceDir * 5.5f, 8f);
    }

    void ActiveStrike()
    {
        _phaseLeft -= Time.fixedDeltaTime;
        bool charge = _data.AttackKind == "charge";
        if (_phaseLeft <= 0f || (charge && !GolemSafe(_faceDir)))
        {
            if (charge && _phaseLeft > 0f)
                TrioVfxPlayer.Spawn("golem_parada_borda", transform.position + Vector3.up * .2f,
                    24, 12, 4, new Vector2(.5f, .5f), 12f);
            Recover();
            return;
        }
        if (charge)
        {
            var v = _body.linearVelocity;
            v.x = _faceDir * (IsSlowed ? 4.4f : 8f);
            _body.linearVelocity = v;
        }
        var center = transform.position + new Vector3(_faceDir * (_data.Size.x * 0.5f + 0.25f), _data.Size.y * 0.5f, 0f);
        foreach (var c in Physics2D.OverlapBoxAll(center, new Vector2(1f, _data.Size.y * 0.8f), 0f))
        {
            if (!_struckPlayer && c.GetComponentInParent<PlayerController>() != null)
            {
                _struckPlayer = true;
                _touchReady = Time.time + _data.TouchCooldown;
                HitPlayer(_data.AttackDamage);
            }
        }
    }

    void Recover()
    {
        Halt();
        _phase = Phase.Recover;
        _phaseLeft = Mathf.Max(0.1f, _data.AttackRecover);
    }

    void Halt()
    {
        _traversal.Stop();
    }

    void Clamp()
    {
        if (_stage == null)
            return;

        var position = transform.position;
        position.x = Mathf.Clamp(position.x, -_stage.HalfWidth + 0.6f, _stage.HalfWidth - 0.6f);
        transform.position = position;
    }

    void Face(float dir)
    {
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dir == 0f ? 1f : dir);
        transform.localScale = scale;
    }

    void HitPlayer(float amount)
    {
        DamagePlayer(_target, transform.position, amount);
    }

    // dano no jogador respeitando escudo e defesa — usado pela IA e pelos projéteis
    public static void DamagePlayer(Transform target, Vector3 from, float amount)
    {
        if (target == null)
            return;

        var health = target.GetComponent<HealthSystem>();
        var combat = target.GetComponent<PlayerCombat>();
        var player = target.GetComponent<PlayerController>();
        float incoming = amount;
        if (combat != null)
            combat.TryBlock(from, incoming, out incoming);

        // Campo de Magia do Mago (FASE 2) — mitigação omnidirecional, distinta do escudo.
        if (combat != null && incoming > 0f)
            combat.TryWard(incoming, out incoming);

        if (incoming > 0f)
        {
            int defense = player != null && player.Hero != null ? player.Hero.Defense : 0;
            if (combat != null && combat.IsBlocking)
                defense += 40;
            DamageSystem.Apply(health, incoming, defense);
        }
    }

    // todo dano do jogador nos inimigos passa por aqui pra aplicar a resistência:
    // O Golem de Ardósia recebe 35% a menos; nunca se autodestrói.
    public void ReceiveDamage(float amount)
    {
        if (_health == null || _health.IsDead || amount <= 0f)
            return;

        // Dano durante o tempo parado do Mago entra só quando o gelo solta.
        if (_frozen)
        {
            _storedDamage += amount;
            return;
        }

        float before = _health.Current;
        float multiplier = _data != null ? _data.DamageTaken : 1f;
        DamageSystem.Apply(_health, amount * multiplier);
        if (_health != null && _health.Current < before)
            AnyDamaged?.Invoke(this, amount);
    }

    public void SetFrozen(bool frozen)
    {
        if (_frozen == frozen || _health == null || _health.IsDead)
            return;

        _frozen = frozen;
        if (_body != null)
        {
            if (frozen)
            {
                _savedGravity = _body.gravityScale;
                _body.linearVelocity = Vector2.zero;
                _body.gravityScale = 0f;
            }
            else
            {
                _body.gravityScale = _savedGravity <= 0f ? 1f : _savedGravity;
                _body.linearVelocity = Vector2.zero;
            }
        }

        if (!frozen && _storedDamage > 0f)
        {
            float stored = _storedDamage;
            _storedDamage = 0f;
            ReceiveDamage(stored);
        }
    }

    public void Knockback(Vector2 velocity, float stunSeconds)
    {
        if (_health == null || _health.IsDead)
            return;

        _stunLeft = Mathf.Max(_stunLeft, stunSeconds);
        if (Mathf.Abs(velocity.x) > 0.01f)
            _faceDir = Mathf.Sign(velocity.x);
        if (_body != null)
            _body.linearVelocity = velocity;
    }

    public void ApplySlow(float seconds)
    {
        if (_health == null || _health.IsDead || _data == null || _data.AttackKind == "charge")
            return; // o tanque recebe dano, sem imobilização nem stun.
        _slowLeft = Mathf.Max(_slowLeft, seconds);
    }

    public void AddVitalMark()
    {
        _vitalMarks++;
        SupremeFx.SpawnMark(transform, _vitalMarks);
    }

    public void ClearVitalMarks()
    {
        _vitalMarks = 0;
        var marks = GetComponentsInChildren<SupremeMark>();
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] != null)
                Destroy(marks[i].gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (_frozen || _stunLeft > 0f || _phase != Phase.Chase || _data == null || _target == null || Time.time < _touchReady)
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        HitPlayer(_data.TouchDamage);
        _touchReady = Time.time + _data.TouchCooldown;
    }

    void OnDied(HealthSystem _)
    {
        AnyDied?.Invoke(this);
        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
            stage.RegisterKill();
        // estilhaço de pixels na cor da criatura ao morrer
        PixelBurst.Spawn(transform.position + Vector3.up * 0.8f, _data != null ? _data.Color : Color.gray, 7);
        Destroy(gameObject);
    }
}
