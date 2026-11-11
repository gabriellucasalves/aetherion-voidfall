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

    // Fase 2 — Sentinela de Escombro (burrow_slam) e Arauto de Brasa (ember_flyer)
    int _burrowStage; // 1 = escondido/rachando o chão (0.5s), 2 = emergindo (0.3s), 0 = na superfície
    float _burrowTimer;
    bool _submergeAfterRecover;
    bool _emberNextDive;
    bool _isDiving;
    bool _isShootingEmber;
    Vector2 _diveVelocity;

    public HealthSystem Health => _health;
    public EnemyData Data => _data;
    public bool IsWindingUp => _phase == Phase.Windup;
    public bool IsAttackingActive => _phase == Phase.Active;
    public bool IsCharging => _phase == Phase.Active && _data != null && _data.AttackKind == "charge";
    public int BurrowStage => _burrowStage;
    public bool IsDiving => _isDiving;
    public bool IsShootingEmber => _isShootingEmber;
    public bool IsFrozen => _frozen;
    public float StoredDamage => _storedDamage;
    public int VitalMarks => _vitalMarks;
    public bool IsSlowed => _slowLeft > 0f;
    public float StunLeft => _stunLeft;
    public bool ExternalBrain;
    public System.Func<float, float> DamageFilter;
    public Transform Target => _target;

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

        if (_data != null && _data.AttackKind == "burrow_slam")
        {
            _burrowStage = 1;
            _burrowTimer = 0.52f;
        }
        else if (_data != null && _data.AttackKind == "ember_flyer" && _body != null)
        {
            _body.gravityScale = 0f;
            _savedGravity = 0f;
        }
    }

    void FixedUpdate()
    {
        if (_target == null || _data == null || _health == null || _health.IsDead || _body == null)
            return;

        if (_slowLeft > 0f)
            _slowLeft -= Time.fixedDeltaTime;

        if (_stunLeft > 0f)
            _stunLeft -= Time.fixedDeltaTime;

        if (ExternalBrain)
            return;

        if (_frozen)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        if (_stunLeft > 0f)
        {
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
                {
                    if (_submergeAfterRecover && _data.AttackKind == "burrow_slam")
                    {
                        _submergeAfterRecover = false;
                        SubmergeNearTarget();
                    }
                    else
                    {
                        _phase = Phase.Chase;
                    }
                }
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
        if (_data.AttackKind == "burrow_slam")
        {
            ChaseBurrowSentinel(dir, adx, ady);
            return;
        }
        if (_data.AttackKind == "ember_flyer")
        {
            ChaseEmberHerald(dir, adx, ady);
            return;
        }
        bool climb = _target.position.y - transform.position.y > 1.3f;
        if ((T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) && (climb || !_traversal.Grounded))
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

    void ChaseBurrowSentinel(float dir, float adx, float ady)
    {
        // Fase 1 do Sentinela: escondido sob o piso (aviso de ~0.5s com poeira e fendas ciano)
        if (_burrowStage == 1)
        {
            Halt();
            _burrowTimer -= Time.fixedDeltaTime;
            if (_burrowTimer <= 0f)
            {
                _burrowStage = 2;
                _burrowTimer = 0.28f;
                PixelBurst.Spawn(transform.position + Vector3.up * 0.2f, new Color(0.42f, 0.96f, 1f), 6);
                PixelBurst.Spawn(transform.position + Vector3.up * 0.15f, new Color(0.75f, 0.68f, 0.86f), 5);
            }
            return;
        }
        // Fase 2 do Sentinela: emergindo da terra
        if (_burrowStage == 2)
        {
            Halt();
            _burrowTimer -= Time.fixedDeltaTime;
            if (_burrowTimer <= 0f)
                _burrowStage = 0;
            return;
        }

        // Na superfície: vulnerável, aproxima-se e anuncia o soco + onda de choque de pedra
        if (_traversal.Grounded && adx <= _data.AttackRange && ady < 1.7f && HasLineOfSight())
        {
            BeginWindup(dir);
            return;
        }
        bool climb = _target.position.y - transform.position.y > 1.3f;
        _traversal.Move(dir, MoveSpeed, climb);
    }

    void SubmergeNearTarget()
    {
        _burrowStage = 1;
        _burrowTimer = 0.52f;
        _phase = Phase.Chase;
        PixelBurst.Spawn(transform.position + Vector3.up * 0.15f, new Color(0.55f, 0.48f, 0.68f), 6);
        if (_target != null && _stage != null)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float candidateX = Mathf.Clamp(_target.position.x + side * Random.Range(2.2f, 4.2f),
                -_stage.HalfWidth + 1.5f, _stage.HalfWidth - 1.5f);
            if (EnemyTraversal.TrySpawnPoint(candidateX, _stage, _data.Size, out var feet))
                transform.position = new Vector3(feet.x, feet.y, 0f);
        }
    }

    void ChaseEmberHerald(float dir, float adx, float ady)
    {
        if (_body != null) _body.gravityScale = 0f;
        float desiredY = (_stage != null ? _stage.GroundTop : -2.35f) + 1.85f;
        if (_target != null)
            desiredY = Mathf.Max(desiredY, _target.position.y + 1.35f);

        float vy = Mathf.Clamp((desiredY - transform.position.y) * 3.2f, -3.2f, 3.2f);
        float vx = 0f;
        if (adx > 4.8f) vx = dir * MoveSpeed;
        else if (adx < 2.4f) vx = -dir * MoveSpeed * 0.8f;

        _body.linearVelocity = new Vector2(vx, vy);

        if (adx <= _data.AttackRange && ady < 3.6f)
            BeginWindup(dir);
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
        if (_data.AttackKind == "burrow_slam")
        {
            _phase = Phase.Active;
            _phaseLeft = 0.42f;
            _submergeAfterRecover = true;
            SentinelaShockwave.Spawn(
                transform.position + new Vector3(_faceDir * 0.95f, 0f, 0f),
                _faceDir,
                _data.AttackDamage,
                _target);
            return;
        }
        if (_data.AttackKind == "ember_flyer")
        {
            _emberNextDive = !_emberNextDive;
            if (!_emberNextDive)
            {
                _phase = Phase.Active;
                _phaseLeft = 0.36f;
                _isShootingEmber = true;
                _isDiving = false;
                var origin = transform.position + new Vector3(_faceDir * 0.55f, 0.52f, 0f);
                EmberFireball.Launch(origin, _target, _data.AttackDamage);
            }
            else
            {
                _phase = Phase.Active;
                _phaseLeft = 0.48f;
                _isDiving = true;
                _isShootingEmber = false;
                _diveVelocity = new Vector2(_faceDir * 6.8f, -3.4f);
            }
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
        if (_data.AttackKind == "ember_flyer")
        {
            if (_phaseLeft <= 0f)
            {
                _isDiving = false;
                _isShootingEmber = false;
                Recover();
                return;
            }
            if (_isDiving)
            {
                _body.linearVelocity = _diveVelocity * (IsSlowed ? 0.65f : 1f);
                var diveCenter = transform.position + new Vector3(_faceDir * 0.45f, _data.Size.y * 0.45f, 0f);
                foreach (var c in Physics2D.OverlapBoxAll(diveCenter, new Vector2(1.1f, _data.Size.y * 0.9f), 0f))
                {
                    if (!_struckPlayer && c.GetComponentInParent<PlayerController>() != null)
                    {
                        _struckPlayer = true;
                        _touchReady = Time.time + _data.TouchCooldown;
                        HitPlayer(_data.AttackDamage);
                    }
                }
            }
            else
            {
                _body.linearVelocity = Vector2.zero;
            }
            return;
        }

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
        if (ExternalBrain || _stage == null)
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

        if (DamageFilter != null)
        {
            amount = DamageFilter(amount);
            if (amount <= 0f)
                return;
        }

        // Sentinela de Escombro submersa (antes de emergir) não recebe dano;
        // fica vulnerável assim que começa a emergir ou está fora da terra.
        if (_data != null && _data.AttackKind == "burrow_slam" && _burrowStage == 1)
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

        if (ExternalBrain)
        {
            _stunLeft = Mathf.Max(_stunLeft, Mathf.Min(1.0f, stunSeconds));
            return;
        }

        _stunLeft = Mathf.Max(_stunLeft, stunSeconds);
        if (Mathf.Abs(velocity.x) > 0.01f)
            _faceDir = Mathf.Sign(velocity.x);
        if (_body != null)
            _body.linearVelocity = velocity;
    }

    public void ApplySlow(float seconds)
    {
        if (_health == null || _health.IsDead)
            return;
        if (!ExternalBrain && _data != null && _data.AttackKind == "charge")
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
        if (ExternalBrain || _frozen || _stunLeft > 0f || _phase != Phase.Chase || _data == null || _target == null || Time.time < _touchReady)
            return;
        if (_data.AttackKind == "burrow_slam" && _burrowStage == 1)
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
        if (ExternalBrain)
        {
            AnyDied?.Invoke(this);
            return;
        }

        AnyDied?.Invoke(this);
        var stage = FindFirstObjectByType<GroundT1Controller>();
        if (stage != null)
            stage.RegisterKill();
        // estilhaço de pixels na cor da criatura ao morrer
        PixelBurst.Spawn(transform.position + Vector3.up * 0.8f, _data != null ? _data.Color : Color.gray, 7);
        Destroy(gameObject);
    }
}

public class SentinelaShockwave : MonoBehaviour
{
    static Sprite _waveSprite;
    float _dir;
    float _damage;
    Transform _target;
    float _timer;
    bool _hitPlayer;

    public static SentinelaShockwave Spawn(Vector3 position, float dir, float damage, Transform target)
    {
        EnsureSprite();
        var go = new GameObject("SentinelaShockwave");
        go.transform.position = position;
        go.transform.localScale = new Vector3(dir >= 0f ? 1f : -1f, 1f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _waveSprite;
        sr.sortingOrder = 11;
        var wave = go.AddComponent<SentinelaShockwave>();
        wave._dir = dir >= 0f ? 1f : -1f;
        wave._damage = damage;
        wave._target = target;
        return wave;
    }

    static void EnsureSprite()
    {
        if (_waveSprite != null) return;
        var tex = resourcesLoadShockwave();
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            _waveSprite = Sprite.Create(tex, new Rect(8 * 64, 0, 64, 64), new Vector2(0.5f, 7f / 64f), 16f, 0, SpriteMeshType.FullRect);
        }
    }

    static Texture2D resourcesLoadShockwave() => Resources.Load<Texture2D>("Inimigos/sentinela_escombro");

    void Update()
    {
        _timer += Time.deltaTime;
        transform.position += new Vector3(_dir * 3.8f * Time.deltaTime, 0f, 0f);
        if (!_hitPlayer)
        {
            foreach (var c in Physics2D.OverlapBoxAll(transform.position + Vector3.up * 0.45f, new Vector2(1.45f, 0.9f), 0f))
            {
                var p = c.GetComponentInParent<PlayerController>();
                if (p != null)
                {
                    _hitPlayer = true;
                    EnemyController.DamagePlayer(p.transform, transform.position, _damage);
                    break;
                }
            }
        }
        if (_timer >= 0.42f)
            Destroy(gameObject);
    }
}

public class EmberFireball : MonoBehaviour
{
    static Sprite _ballSprite;
    Vector2 _velocity;
    float _damage;
    float _life = 2.6f;

    public static EmberFireball Launch(Vector3 from, Transform target, float damage)
    {
        EnsureSprite();
        var go = new GameObject("EmberFireball");
        go.transform.position = from;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _ballSprite;
        sr.sortingOrder = 12;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;

        Vector2 dest = target != null ? (Vector2)target.position + Vector2.up * 0.45f : (Vector2)from + new Vector2(4f, -2f);
        Vector2 dir = (dest - (Vector2)from).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = new Vector2(1f, -0.4f).normalized;

        var fb = go.AddComponent<EmberFireball>();
        fb._velocity = dir * 6.8f;
        fb._damage = damage;
        return fb;
    }

    static void EnsureSprite()
    {
        if (_ballSprite != null) return;
        var tex = Resources.Load<Texture2D>("Inimigos/arauto_brasa");
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            _ballSprite = Sprite.Create(tex, new Rect(7 * 64, 0, 64, 64), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        }
    }

    void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(_velocity * Time.deltaTime);
        if (transform.position.y <= -1.9f)
        {
            PixelBurst.Spawn(transform.position, new Color(1f, 0.48f, 0.16f), 6);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p != null)
        {
            EnemyController.DamagePlayer(p.transform, transform.position, _damage);
            PixelBurst.Spawn(transform.position, new Color(1f, 0.48f, 0.16f), 6);
            Destroy(gameObject);
        }
    }
}
