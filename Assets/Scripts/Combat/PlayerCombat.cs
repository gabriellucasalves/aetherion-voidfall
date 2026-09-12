using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    AbilityData _ability;
    CharacterData _hero;
    PlayerController _player;
    HealthSystem _health;
    ShieldSystem _shield;
    Transform _shieldVisual;
    Vector3 _shieldRest;
    float _cooldownLeft;
    float _dashCooldown;
    float _attackLeft;
    bool _locked;
    bool _blocking;

    public bool IsBlocking => CanBlock && _blocking && !_locked;
    public bool IsAttacking => _attackLeft > 0f;
    public ShieldSystem Shield => _shield;
    public bool IsWarrior => _hero != null && _hero.Id == "guerreiro";
    public bool IsMage => _hero != null && _hero.Id == "mago";
    public bool IsAngel => _hero != null && _hero.Id == "anjo";
    public bool CanBlock => IsWarrior;
    public AbilityData Ability => _ability;

    public float AttackRange
    {
        get
        {
            if (_ability == null || _hero == null)
                return 1.7f;
            float range = _ability.Range;
            if (IsMage)
                range += _hero.Power * 0.028f;
            if (IsAngel)
                range += _hero.Agility * 0.012f;
            return range;
        }
    }

    public void Setup(CharacterData hero, AbilityData ability)
    {
        _hero = hero;
        _ability = ability;
        _player = GetComponent<PlayerController>();
        _health = GetComponent<HealthSystem>();

        if (IsWarrior)
        {
            _shield = GetComponent<ShieldSystem>();
            if (_shield == null)
                _shield = gameObject.AddComponent<ShieldSystem>();
            _shield.Configure(20f + hero.Defense);
            _shieldVisual = transform.Find("Escudo");
            if (_shieldVisual != null)
                _shieldRest = _shieldVisual.localPosition;
        }
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked)
            _blocking = false;
    }

    public bool TryBlock(Vector3 attackerPosition, float rawDamage, out float leftover)
    {
        leftover = rawDamage;
        if (!IsBlocking || _shield == null || rawDamage <= 0f)
            return false;

        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        float side = attackerPosition.x - transform.position.x;
        if (Mathf.Abs(side) > 0.05f && Mathf.Sign(side) != Mathf.Sign(facing.x))
            return false;

        leftover = _shield.Absorb(rawDamage);
        bool blocked = leftover < rawDamage;
        if (blocked)
        {
            Vector3 impact = transform.position + new Vector3(facing.x * 0.7f, 0.9f, 0f);
            PixelBurst.Spawn(impact, new Color(0.45f, 0.72f, 1f), 5);
        }
        return blocked;
    }

    void Update()
    {
        if (_locked || _ability == null || _hero == null)
            return;

        _cooldownLeft -= Time.deltaTime;
        _dashCooldown -= Time.deltaTime;
        if (_attackLeft > 0f)
            _attackLeft -= Time.deltaTime;

        if (IsWarrior)
        {
            _blocking = WantsBlock();
            PoseShield();
            if (_blocking)
            {
                _attackLeft = 0f; // levantar o escudo cancela o corte
                return;
            }
            if (WantsAttack() && _cooldownLeft <= 0f)
                FireWarrior();
            return;
        }

        _blocking = false;

        if (IsAngel && WantsDash() && _dashCooldown <= 0f && _player != null)
            Dash();

        if (IsMage)
        {
            if (_cooldownLeft > 0f)
                return;
            if (WantsAttack() || HasTargetInRange())
                FireMage();
            return;
        }

        if (IsAngel && WantsAttack() && _cooldownLeft <= 0f)
            FireAngel();
    }

    void FireWarrior()
    {
        var go = new GameObject("Corte");
        go.AddComponent<MeleeSlash>().Swing(transform, _ability, DamageForShot());
        PixelBurst.Spawn(transform.position + (Vector3)_player.Facing * 0.8f, _ability.Color, 3);
        _attackLeft = 0.42f;
        ArmCooldown();
    }

    void FireMage()
    {
        var target = FindNearest(transform, transform.position, AttackRange);
        Vector2 direction = _player != null ? _player.Facing : Vector2.right;
        Transform lockOn = null;
        if (target != null)
        {
            direction = target.transform.position - transform.position;
            lockOn = target.transform;
        }
        else if (!WantsAttack())
        {
            return;
        }

        SpawnOrb(direction, lockOn);
        PixelBurst.Spawn(transform.position + (Vector3)direction.normalized * 0.5f, _ability.Color, 3);
        _attackLeft = 0.36f; // alimenta MagoVisual cast
        ArmCooldown();
    }

    void FireAngel()
    {
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        FireFeather(Rotate(facing, 11f), new Vector3(0f, 0.22f, 0f));
        FireFeather(facing, Vector3.zero);
        FireFeather(Rotate(facing, -11f), new Vector3(0f, -0.2f, 0f));
        PixelBurst.Spawn(transform.position + (Vector3)facing * 0.55f, _ability.Color, 4);
        ArmCooldown();
    }

    void Dash()
    {
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        float agility = Mathf.Max(40, _hero.Agility);
        float speed = 18f + agility * 0.06f;
        float duration = 0.16f;
        float iFrames = 0.24f;
        _player.StartDash(facing, speed, duration, iFrames);
        _dashCooldown = 0.82f * (70f / agility);
        PixelBurst.Spawn(transform.position, new Color(1f, 0.9f, 0.45f), 6);
        if (_health != null)
            _health.IsInvulnerable = true;
    }

    void SpawnOrb(Vector2 direction, Transform target)
    {
        var go = MakeShot("Orbe", _ability.ProjectileSize, _ability.Color);
        go.AddComponent<HomingOrb>().Launch(direction, _ability, DamageForShot(), target);
    }

    void FireFeather(Vector2 direction, Vector3 localOffset)
    {
        var go = MakeShot("Pena", _ability.ProjectileSize, _ability.Color);
        go.transform.position += localOffset;
        go.AddComponent<Projectile>().Launch(direction, _ability, DamageForShot());
    }

    GameObject MakeShot(string name, Vector2 size, Color color)
    {
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        var go = new GameObject(name);
        go.transform.position = transform.position + (Vector3)facing.normalized * 0.55f;
        var renderer = go.AddComponent<SpriteRenderer>();
        var texture = Texture2D.whiteTexture;
        renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        renderer.color = color;
        renderer.sortingOrder = 20;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var owner = GetComponent<Collider2D>();
        if (owner != null)
            Physics2D.IgnoreCollision(collider, owner, true);
        return go;
    }

    float DamageForShot()
    {
        if (IsMage)
            return _hero.Power * _ability.DamageScale;
        if (IsAngel)
            return (_hero.Strength * 0.55f + _hero.Power * 0.45f) * _ability.DamageScale;
        return _hero.Strength * _ability.DamageScale;
    }

    void ArmCooldown()
    {
        float attackSpeed = Mathf.Max(20, _hero.AttackSpeed);
        float cooldown = _ability.Cooldown * (40f / attackSpeed);
        if (IsAngel)
            cooldown *= 80f / Mathf.Max(40, _hero.Agility);
        _cooldownLeft = cooldown;
    }

    bool HasTargetInRange()
    {
        return FindNearest(transform, transform.position, AttackRange) != null;
    }

    public static EnemyController FindNearest(Transform from, Vector2 origin, float range)
    {
        if (from != null)
            origin = from.position;

        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController best = null;
        float bestDistance = range;
        foreach (var enemy in enemies)
        {
            if (enemy.Health == null || enemy.Health.IsDead)
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }

        return best;
    }

    void PoseShield()
    {
        if (_shieldVisual == null)
            return;

        _shieldVisual.localPosition = _blocking
            ? _shieldRest + new Vector3(0.22f, 0.08f, 0f)
            : _shieldRest;
        _shieldVisual.localScale = _blocking
            ? new Vector3(0.38f, 0.92f, 1f)
            : new Vector3(0.28f, 0.7f, 1f);
    }

    static Vector2 Rotate(Vector2 value, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    static bool WantsAttack()
    {
        if (MobileControls.IsVisible)
            return MobileControls.AttackHeld;
        return Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
    }

    static bool WantsBlock()
    {
        if (MobileControls.IsVisible)
            return MobileControls.BlockHeld;
        return Input.GetMouseButton(1)
            || Input.GetKey(KeyCode.K)
            || Input.GetKey(KeyCode.S);
    }

    static bool WantsDash()
    {
        return Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.RightShift);
    }
}
