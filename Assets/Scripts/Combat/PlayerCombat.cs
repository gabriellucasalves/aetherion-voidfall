using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    AbilityData _ability;
    CharacterData _hero;
    PlayerController _player;
    HealthSystem _health;
    ShieldSystem _shield;
    MagicWard _ward;
    Transform _shieldVisual;
    Vector3 _shieldRest;
    float _cooldownLeft;
    float _dashCooldown;
    float _specialCooldownLeft;
    float _specialAnimLeft;
    float _attackLeft;
    bool _locked;
    bool _blocking;

    // Pulso Rúnico: telegraph à frente, dano em área e lentidão curta.
    public const float MageSpecialCooldown = 6.5f;
    public const float MageSpecialRadius = 3.4f;
    public const float MageSpecialDamageScale = 0.95f;

    // Feixe Infernal: L/Q/E consomem a mesma barra ESPECIAL; Corte permanece independente.
    public const float WarriorSpecialCooldown = 6f;


    // Especial do Arqueiro (Rajada em leque) — CD próprio, espelha o padrão do Mago.
    public const float ArcherSpecialCooldown = 6.5f;

    public bool IsBlocking => CanBlock && _blocking && !_locked;
    public bool IsWarding => IsMage && _ward != null && _ward.IsActive && !_locked;
    public bool IsAttacking => _attackLeft > 0f;
    public float SpecialCooldownLeft => Mathf.Max(0f, _specialCooldownLeft);
    public float SpecialCooldownMax =>
        IsArcher ? ArcherSpecialCooldown : (IsMage ? MageSpecialCooldown : WarriorSpecialCooldown);
    public bool SpecialReady => _specialCooldownLeft <= 0f && (!IsWarrior ||
        (GetComponent<ResonanceMeter>() != null && GetComponent<ResonanceMeter>().IsFull));
    public bool IsCastingSpecial => _specialAnimLeft > 0f;
    public bool IsSpecialAttacking => _specialAnimLeft > 0f;
    public ShieldSystem Shield => _shield;
    public MagicWard Ward => _ward;
    public bool IsWarrior => _hero != null && _hero.Id == "guerreiro";
    public bool IsMage => _hero != null && _hero.Id == "mago";
    public bool IsArcher => _hero != null && _hero.Id == "arqueiro";
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
            if (IsArcher)
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

        if (IsMage)
        {
            _ward = GetComponent<MagicWard>();
            if (_ward == null)
                _ward = gameObject.AddComponent<MagicWard>();
            _ward.Configure();
        }
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked)
        {
            _blocking = false;
            if (_ward != null)
                _ward.SetHolding(false);
        }
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
            if (leftover <= 0.01f)
                GetComponent<ResonanceMeter>()?.Add(ResonanceMeter.PerPerfectBlock);
        }
        return blocked;
    }

    /// <summary>Campo de Magia do Mago — mitigação omnidirecional (não é escudo físico).</summary>
    public bool TryWard(float rawDamage, out float leftover)
    {
        leftover = rawDamage;
        if (!IsWarding || _ward == null || rawDamage <= 0f)
            return false;

        leftover = _ward.Mitigate(rawDamage);
        return leftover < rawDamage;
    }

    void Update()
    {
        if (_locked || _ability == null || _hero == null)
        {
            // Descarta toques mobile de especial/dash enquanto travado (pausa/morte).
            if (MobileControls.IsVisible)
            {
                MobileControls.ConsumeSpecialDown();
                MobileControls.ConsumeDashPressed();
            }
            return;
        }

        _cooldownLeft -= Time.deltaTime;
        _dashCooldown -= Time.deltaTime;
        _specialCooldownLeft -= Time.deltaTime;
        if (_specialAnimLeft > 0f)
            _specialAnimLeft -= Time.deltaTime;
        if (_attackLeft > 0f)
            _attackLeft -= Time.deltaTime;

        if (IsWarrior)
        {
            _blocking = WantsBlock();
            PoseShield();
            if (_blocking)
            {
                _attackLeft = 0f; // levantar o escudo cancela o corte
                _specialAnimLeft = 0f;
                return;
            }

            // Especial tem CD próprio — não fica bloqueado pelo CD do Corte.
            if (_specialCooldownLeft <= 0f && WantsSpecial())
                TryFireWarriorSpecial();

            if (WantsAttack() && _cooldownLeft <= 0f)
                FireWarrior();
            return;
        }

        _blocking = false;

        if (IsArcher && WantsDash() && _dashCooldown <= 0f && _player != null)
            Dash();

        if (IsArcher && _specialCooldownLeft <= 0f && WantsSpecial())
            FireArcherSpecial();

        if (IsMage)
        {
            // Campo de Magia: mesmo input de block do Guerreiro (S/K/direito / mobile Block).
            if (_ward != null)
                _ward.SetHolding(WantsBlock());

            // Especial tem CD próprio — não fica bloqueado pelo CD do Orbe básico.
            // CD primeiro: não consome o toque/tecla se ainda estiver em cooldown.
            if (_specialCooldownLeft <= 0f && WantsSpecial())
                FireMageSpecial();

            // Tiro básico: só clique/J (ou hold) — sem auto-fire por proximidade.
            // Hold do campo cancela o orbe (canalizar defesa).
            if (!IsWarding && WantsAttack() && _cooldownLeft <= 0f)
                FireMage();
            return;
        }

        if (IsArcher && WantsAttack() && _cooldownLeft <= 0f)
            FireArcher();
    }

    void FireWarrior()
    {
        var go = new GameObject("Corte");
        go.AddComponent<MeleeSlash>().Swing(transform, _ability, DamageForShot());
        Vector3 swingFx = transform.position + (Vector3)_player.Facing * 1.0f + Vector3.up * 0.35f;
        PixelBurst.Spawn(swingFx, _ability.Color, 6);
        // 4 frames de ataque a ~12 fps (~0.33s) + hold curto no último frame
        _attackLeft = 0.40f;
        ArmCooldown();
    }

    public bool TryFireWarriorSpecial()
    {
        var meter = GetComponent<ResonanceMeter>();
        if (!IsWarrior || _locked || IsBlocking || _specialCooldownLeft > 0f ||
            Time.timeScale <= 0f || (_health != null && _health.IsDead) || meter == null || !meter.TrySpend())
            return false;
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        InfernalBeam.Launch(transform, facing, _hero.Strength * InfernalBeam.DamageScale);
        _attackLeft = InfernalBeam.Startup + InfernalBeam.ActiveDuration;
        _specialAnimLeft = _attackLeft;
        _specialCooldownLeft = WarriorSpecialCooldown;
        return true;
    }

    void FireMage()
    {
        // Orbe Arcano: tiro reto na facing (sem lock / sem homing). Clique/J ou hold.
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.right;
        facing.Normalize();

        ArcaneBolt.Launch(transform, facing, _ability, DamageForShot());
        // 4 frames de cast @ ~12 fps (~0.33s) + hold curto no último frame — sync MagoVisual 13–16.
        _attackLeft = 0.52f;
        ArmCooldown();
    }

    void FireMageSpecial()
    {
        // Centro fixado na direção do personagem no início do cast.
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        Vector3 center = transform.position + (Vector3)(facing.normalized * 2.3f) + Vector3.up * 0.55f;

        float damage = _hero.Power * MageSpecialDamageScale;
        var color = _ability != null ? _ability.Color : new Color(0.45f, 0.72f, 1f);
        ArcaneNova.Detonate(center, MageSpecialRadius, damage, color, true, transform);

        _attackLeft = 0.45f; // cast visual (MagoVisual)
        // Clip special 17–20 (~11 fps) + hold: cobre wind-up (0.18s) e pico da explosão VFX.
        _specialAnimLeft = 0.75f; // cut-in (~0.71s) + pico da explosao
        _specialCooldownLeft = MageSpecialCooldown; // CD próprio ~6.5s — independente do Orbe básico
        PixelBurst.Spawn(transform.position + Vector3.up * 0.8f, color, 6);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.55f, new Color(0.7f, 0.45f, 1f), 4);
    }

    void FireArcher()
    {
        // Flecha visual (Arrow): 1 tiro reto na facing. Spawn com delay no frame de soltar.
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        FireArrow(facing, Vector3.zero, DamageForShot(), _ability.Color);
        // Clip shoot 13×2→14→15 @ ~12 fps (~0.33s) + hold no recover — sync Arrow.BasicMuzzleDelay.
        _attackLeft = 0.55f;
        ArmCooldown();
    }

    void FireArcherSpecial()
    {
        // Flecha Perfurante: tiro reto, atravessa até três inimigos e para no terreno.
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.right;
        facing.Normalize();

        Color tint = new Color(0.55f, 0.95f, 0.62f);
        Arrow.Launch(transform, facing, _ability, DamageForShot() * 1.35f,
            Vector3.zero, tint, Arrow.SpecialMuzzleDelay, 3);
        Vector3 muzzle = transform.position + (Vector3)facing * .65f + Vector3.up * .4f;
        TrioVfxPlayer.Spawn("perfurante_mira", muzzle, 40, 3, 2,
            new Vector2(0f, 1f / 3f), 4f, 1f, facing.x);
        TrioVfxPlayer.Spawn("perfurante_carga", muzzle, 12, 12, 4,
            new Vector2(.5f, .5f), 8f);

        // VFX de wind-up (draw) — o PixelBurst de muzzle de cada Arrow dispara no soltar.
        PixelBurst.Spawn(transform.position + (Vector3)facing * 0.45f + Vector3.up * 0.35f, tint, 10);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.55f, new Color(0.7f, 1f, 0.75f), 4);

        // Clip special {13,13,14,18} @ ~12 fps (~0.33s) + hold no leque.
        _attackLeft = 0.45f;
        _specialAnimLeft = 0.70f; // cobre o cut-in (~0.71s)
        _specialCooldownLeft = ArcherSpecialCooldown;
    }

    void Dash()
    {
        // Esquiva do arqueiro: impulso curto na facing com i-frames breves (PlayerController).
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        float agility = Mathf.Max(40, _hero.Agility);
        float speed = 18f + agility * 0.06f;
        float duration = 0.16f;
        float iFrames = 0.24f;
        _player.StartDash(facing, speed, duration, iFrames);
        _dashCooldown = 0.82f * (70f / agility);
        PixelBurst.Spawn(transform.position, new Color(0.95f, 0.72f, 0.28f), 5);
    }

    public void RechargeDash()
    {
        _dashCooldown = 0f;
    }

    void FireArrow(Vector2 direction, Vector3 localOffset, float damage, Color color, float muzzleDelay = -1f)
    {
        // Arrow: sprite ponta+haste+pena, rotação na direção, PixelBurst no impacto.
        Arrow.Launch(transform, direction, _ability, damage, localOffset, color, muzzleDelay);
    }

    float DamageForShot()
    {
        // Mago: Power * DamageScale (kit de dano à distância). Guerreiro continua em Strength.
        if (IsMage)
            return _hero.Power * _ability.DamageScale;
        if (IsArcher)
            return (_hero.Strength * 0.55f + _hero.Power * 0.45f) * _ability.DamageScale;
        return _hero.Strength * _ability.DamageScale;
    }

    void ArmCooldown()
    {
        float attackSpeed = Mathf.Max(20, _hero.AttackSpeed);
        float cooldown = _ability.Cooldown * (40f / attackSpeed);
        if (IsArcher)
            cooldown *= 80f / Mathf.Max(40, _hero.Agility);
        _cooldownLeft = cooldown;
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

    static bool WantsSpecial()
    {
        // Teclado: L ou Q. Mobile: botão de especial (ConsumeSpecialDown).
        // E fica reservado ao Especial Supremo (SpecialController).
        if (MobileControls.IsVisible)
            return MobileControls.ConsumeSpecialDown();
        return Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Q);
    }

    static bool WantsDash()
    {
        if (MobileControls.IsVisible && MobileControls.ConsumeDashPressed())
            return true;
        return Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.RightShift);
    }
}
