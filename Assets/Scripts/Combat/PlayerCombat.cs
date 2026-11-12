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
    public bool IsLocked => _locked;
    bool _blocking;

    // Estado da nova mecânica de Tiro Carregado do Arqueiro Arcano (Puxar -> Carregar -> Soltar -> Recuo)
    bool _isCharging;
    float _chargeTime;
    bool _prevMobileAttackHeld;
    bool _damagedHooked;
    int _lastReleasedTier = 2;

    // Zona Rúnica no chão (especial L/Q do Mago): raio 2.8, duração 3.5s, tick de 16 (Power * 0.16) + lentidão.
    public const float MageSpecialCooldown = 6.5f;
    public const float MageSpecialRadius = ArcaneZone.DefaultRadius;
    public const float MageSpecialDamageScale = ArcaneZone.TickDamageScale;

    // Feixe Infernal: L/Q/E consomem a mesma barra ESPECIAL; Corte permanece independente.
    public const float WarriorSpecialCooldown = 6f;

    // Especial do Arqueiro (Flecha Perfurante) — CD próprio, espelha o padrão do Mago.
    public const float ArcherSpecialCooldown = 6.5f;

    // Constantes do Tiro Carregado do Arqueiro Arcano
    public const float ChargeMoveScale = 0.4f;
    public const float ArcherChargeMidThreshold = 0.25f;
    public const float ArcherChargeMaxThreshold = 0.80f;
    public const float ArcherReleaseCooldown = 0.30f;
    public const float ArcherRecoilDuration = 0.22f;

    public bool IsGuardHeld => CanBlock && _blocking && !_locked && (_health == null || !_health.IsDead);
    public bool IsBlocking => IsGuardHeld && _shield != null && !_shield.IsBroken;
    public bool IsWarding => IsMage && IsBlocking;
    public bool IsCharging => IsArcher && _isCharging && !_locked;
    public float ChargeTime => _isCharging ? _chargeTime : 0f;
    public int ChargeSegmentCount => !_isCharging ? 0 : (_chargeTime >= ArcherChargeMaxThreshold ? 3 : (_chargeTime >= ArcherChargeMidThreshold ? 2 : 1));
    public int LastReleasedTier => _lastReleasedTier;
    public bool IsAttacking => _attackLeft > 0f;
    public float SpecialCooldownLeft => Mathf.Max(0f, _specialCooldownLeft);
    public float SpecialCooldownMax =>
        IsArcher ? ArcherSpecialCooldown : (IsMage ? MageSpecialCooldown : WarriorSpecialCooldown);
    public bool SpecialReady => _specialCooldownLeft <= 0f;
    public bool IsCastingSpecial => _specialAnimLeft > 0f;
    public bool IsSpecialAttacking => _specialAnimLeft > 0f;
    public ShieldSystem Shield => _shield;
    public MagicWard Ward => _ward;
    public bool IsWarrior => _hero != null && _hero.Id == "guerreiro";
    public bool IsMage => _hero != null && _hero.Id == "mago";
    public bool IsArcher => _hero != null && _hero.Id == "arqueiro";
    public bool CanBlock => _hero != null;
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
        HookDamageCancel();

        _shield = GetComponent<ShieldSystem>();
        if (_shield == null) _shield = gameObject.AddComponent<ShieldSystem>();
        _shield.Configure(IsWarrior ? 20f + hero.Defense : IsMage ? 100f : 75f);
        _shieldVisual = transform.Find("Escudo");
        if (_shieldVisual != null) _shieldRest = _shieldVisual.localPosition;

        if (IsMage)
        {
            _ward = GetComponent<MagicWard>();
            if (_ward == null)
                _ward = gameObject.AddComponent<MagicWard>();
            _ward.Configure();
        }
    }

    void HookDamageCancel()
    {
        if (_damagedHooked || _health == null)
            return;
        _health.Damaged += OnOwnerDamaged;
        _damagedHooked = true;
    }

    void OnDisable() { CancelArcherCharge(); }

    void OnDestroy()
    {
        if (_damagedHooked && _health != null)
            _health.Damaged -= OnOwnerDamaged;
    }

    void OnOwnerDamaged(HealthSystem _)
    {
        // Tomar dano no meio da carga cancela a puxada SEM disparar nenhuma flecha.
        if (IsArcher && _isCharging)
            CancelArcherCharge();
    }

    public void CancelArcherCharge()
    {
        if (_isCharging) GameAudio.StopBowDraw();
        _isCharging = false;
        _chargeTime = 0f;
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked)
        {
            _blocking = false;
            CancelArcherCharge();
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
        leftover = _shield.Absorb(rawDamage);
        bool blocked = leftover < rawDamage;
        if (blocked)
        {
            GameAudio.Emit(IsMage ? GameAudio.Cue.MageGuard : IsArcher ? GameAudio.Cue.ArcherGuard : GameAudio.Cue.Block, transform.position);
            Vector3 impact = transform.position + new Vector3(facing.x * 0.7f, 0.9f, 0f);
            PixelBurst.Spawn(impact, new Color(0.45f, 0.72f, 1f), 5);
            if (IsWarrior && leftover <= 0.01f)
                GetComponent<ResonanceMeter>()?.Add(ResonanceMeter.PerPerfectBlock);
        }
        return blocked;
    }

    /// <summary>Compatibility entry point; the ward now absorbs shield-first, without health leakage.</summary>
    public bool TryWard(float rawDamage, out float leftover)
    {
        leftover = rawDamage;
        if (!IsWarding || _ward == null || rawDamage <= 0f)
            return false;

        return TryBlock(transform.position, rawDamage, out leftover);
    }

    void Update()
    {
        HookDamageCancel();

        bool mobileHeld = MobileControls.IsVisible && MobileControls.AttackHeld;
        bool mobileDown = mobileHeld && !_prevMobileAttackHeld;
        bool mobileUp = !mobileHeld && _prevMobileAttackHeld;
        _prevMobileAttackHeld = mobileHeld;

        if (_locked || _ability == null || _hero == null || (_health != null && _health.IsDead))
        {
            if (_isCharging)
                CancelArcherCharge();
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

        _blocking = WantsBlock();
        if (_ward != null) _ward.SetHolding(IsWarding);
        PoseShield();
        if (IsGuardHeld)
        {
            if (MobileControls.IsVisible)
            {
                MobileControls.ConsumeSpecialDown();
                MobileControls.ConsumeDashPressed();
                MobileControls.ConsumeSupremeDown();
            }
            CancelArcherCharge();
            _attackLeft = 0f;
            _specialAnimLeft = 0f;
            return;
        }

        if (IsWarrior)
        {
            // Especial tem CD próprio — não fica bloqueado pelo CD do Corte.
            if (_specialCooldownLeft <= 0f && WantsSpecial())
                TryFireWarriorSpecial();

            if (WantsAttack() && _cooldownLeft <= 0f)
                FireWarrior();
            return;
        }

        if (IsMage)
        {
            // Especial tem CD próprio — não fica bloqueado pelo CD do Orbe básico.
            if (_specialCooldownLeft <= 0f && WantsSpecial())
                FireMageSpecial();

            // Tiro básico: só clique/J (ou hold) — sem auto-fire por proximidade.
            if (!IsWarding && WantsAttack() && _cooldownLeft <= 0f)
                FireMage();
            return;
        }

        if (IsArcher)
        {
            UpdateArcherCombat(mobileDown, mobileHeld, mobileUp);
        }
    }

    void UpdateArcherCombat(bool mobileDown, bool mobileHeld, bool mobileUp)
    {
        // Dash (Shift) cancela carga ativa sem disparar.
        if (WantsDash() && _dashCooldown <= 0f && _player != null)
        {
            CancelArcherCharge();
            Dash();
            return;
        }

        // Especial (L/Q) cancela carga ativa sem disparar o básico e lança 1 Flecha Perfurante.
        if (_specialCooldownLeft <= 0f && WantsSpecial())
        {
            CancelArcherCharge();
            FireArcherSpecial();
            return;
        }

        if (_isCharging)
        {
            // Se entrou em dash ou especial, cancela sem disparar.
            if ((_player != null && _player.IsDashing) || _specialAnimLeft > 0f)
            {
                CancelArcherCharge();
                return;
            }

            bool released = WantsArcherAttackUp(mobileUp) || !WantsArcherAttackHeld(mobileHeld);
            if (released)
            {
                ReleaseArcherCharge();
            }
            else
            {
                _chargeTime += Time.deltaTime;
            }
            return;
        }

        // Fora de carga: só inicia uma nova puxada na borda de descida (GetMouseButtonDown/GetKeyDown/toque inicial)
        // e nunca durante o recuo (_attackLeft > 0), cooldown (_cooldownLeft > 0), especial ou dash.
        bool canStartCharge = _cooldownLeft <= 0f
            && _attackLeft <= 0f
            && _specialAnimLeft <= 0f
            && (_player == null || !_player.IsDashing);

        if (canStartCharge && WantsArcherAttackDown(mobileDown))
        {
            BeginArcherCharge();
        }
    }

    public bool BeginArcherCharge()
    {
        if (!IsArcher || _locked || IsGuardHeld || (_health != null && _health.IsDead))
            return false;
        if (_cooldownLeft > 0f || _attackLeft > 0f || _specialAnimLeft > 0f || (_player != null && _player.IsDashing))
            return false;
        _isCharging = true;
        _chargeTime = 0f;
        GameAudio.BeginBowDraw();
        return true;
    }

    public void AdvanceArcherCharge(float seconds)
    {
        if (_isCharging && seconds > 0f)
            _chargeTime += seconds;
    }

    void FireArcher()
    {
        // Mantido apenas para compatibilidade de reflexão em testes legados: simula puxada média + soltura de 1 flecha
        _isCharging = true;
        _chargeTime = 0.45f;
        ReleaseArcherCharge();
    }

    public void ReleaseArcherCharge()
    {
        if (!_isCharging)
            return;

        GameAudio.StopBowDraw();
        float held = _chargeTime;
        _isCharging = false;
        _chargeTime = 0f;

        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.right;
        facing.Normalize();

        float baseDamage = DamageForShot();
        int tier;
        float damage;
        float speed;
        float lifetime;
        Color fxColor;

        if (held < ArcherChargeMidThreshold)
        {
            // Nível 1 (< 0.25s): 40% do dano base, mais lenta, alcance curto e leve queda no fim
            tier = 1;
            damage = baseDamage * 0.40f;
            speed = 14.5f;
            lifetime = 0.28f;
            fxColor = new Color(0.42f, 0.97f, 1f);
        }
        else if (held < ArcherChargeMaxThreshold)
        {
            // Nível 2 (0.25s a 0.80s): dano interpola continuamente de 40% a 100%, haste dourada/cristal
            tier = 2;
            float t = Mathf.InverseLerp(ArcherChargeMidThreshold, ArcherChargeMaxThreshold, held);
            damage = baseDamage * Mathf.Lerp(0.40f, 1.00f, t);
            speed = Mathf.Lerp(18.5f, 23.0f, t);
            lifetime = Mathf.Lerp(0.38f, 0.50f, t);
            fxColor = new Color(0.55f, 0.95f, 1f);
        }
        else
        {
            // Nível 3 (>= 0.80s): 100% do dano base, mais rápida e longa, rastro mágico e impacto estelar
            tier = 3;
            damage = baseDamage;
            speed = 27.5f;
            lifetime = 0.60f;
            fxColor = new Color(1f, 0.95f, 0.62f);
        }

        // Nasce EXATAMENTE UMA flecha no frame de soltar (muzzleDelay = 0f).
        Arrow.Launch(
            transform,
            facing,
            _ability,
            damage,
            Vector3.zero,
            fxColor,
            muzzleDelay: 0f,
            pierce: 1,
            chargeTier: tier,
            speedOverride: speed,
            lifetimeOverride: lifetime);

        GameAudio.Emit(GameAudio.Cue.Bow, transform.position, tier == 3 ? 1f : 0.75f);
        _lastReleasedTier = tier;
        _attackLeft = ArcherRecoilDuration;     // Janela de animação Soltar -> Recuo (~0.22s)
        _cooldownLeft = ArcherReleaseCooldown;  // Cooldown fixo de 0.30s após o disparo
    }

    void FireWarrior()
    {
        GameAudio.Emit(GameAudio.Cue.Sword, transform.position);
        var go = new GameObject("Corte");
        go.AddComponent<MeleeSlash>().Swing(transform, _ability, DamageForShot());
        Vector3 swingFx = transform.position + (Vector3)_player.Facing * 1.0f + Vector3.up * 0.35f;
        PixelBurst.Spawn(swingFx, _ability.Color, 6);
        _attackLeft = 0.40f;
        ArmCooldown();
    }

    public bool TryFireWarriorSpecial()
    {
        if (!IsWarrior || _locked || IsBlocking || _specialCooldownLeft > 0f ||
            Time.timeScale <= 0f || (_health != null && _health.IsDead))
            return false;
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        GameAudio.Emit(GameAudio.Cue.WarriorSpecial, transform.position);
        InfernalBeam.Launch(transform, facing, _hero.Strength * InfernalBeam.DamageScale);
        _attackLeft = InfernalBeam.Startup + InfernalBeam.ActiveDuration;
        _specialAnimLeft = _attackLeft;
        _specialCooldownLeft = WarriorSpecialCooldown;
        return true;
    }

    void FireMage()
    {
        GameAudio.Emit(GameAudio.Cue.Magic, transform.position);
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.right;
        facing.Normalize();

        ArcaneBolt.Launch(transform, facing, _ability, DamageForShot());
        _attackLeft = 0.52f;
        ArmCooldown();
    }

    void FireMageSpecial()
    {
        GameAudio.Emit(GameAudio.Cue.MageSpecial, transform.position);
        Vector2 facing = _player != null ? _player.Facing : Vector2.right;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.right;
        Vector3 center = transform.position + (Vector3)(facing.normalized * 2.3f) + Vector3.up * 0.55f;

        float tickDamage = _hero.Power * MageSpecialDamageScale;
        var color = _ability != null ? _ability.Color : new Color(0.45f, 0.72f, 1f);
        ArcaneZone.Spawn(center, MageSpecialRadius, tickDamage, color, transform, ArcaneZone.DefaultDuration);

        _attackLeft = 0.45f;
        _specialAnimLeft = 0.75f;
        _specialCooldownLeft = MageSpecialCooldown;
        PixelBurst.Spawn(transform.position + Vector3.up * 0.8f, color, 6);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.55f, new Color(0.7f, 0.45f, 1f), 4);
    }

    void FireArcherSpecial()
    {
        GameAudio.Emit(GameAudio.Cue.ArcherSpecial, transform.position);
        // Flecha Perfurante: 1 único projétil reto, atravessa até três inimigos e para no terreno.
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

        PixelBurst.Spawn(transform.position + (Vector3)facing * 0.45f + Vector3.up * 0.35f, tint, 10);
        PixelBurst.Spawn(transform.position + Vector3.up * 0.55f, new Color(0.7f, 1f, 0.75f), 4);

        _attackLeft = 0.45f;
        _specialAnimLeft = 0.70f;
        _specialCooldownLeft = ArcherSpecialCooldown;
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
        PixelBurst.Spawn(transform.position, new Color(0.95f, 0.72f, 0.28f), 5);
    }

    public void RechargeDash()
    {
        _dashCooldown = 0f;
    }

    float DamageForShot()
    {
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

    static bool WantsAttack()
    {
        return MobileControls.AttackHeld || GamepadControls.Held(GamepadControls.Attack)
            || (!MobileControls.IsVisible && Input.GetMouseButton(0)) || Input.GetKey(KeyCode.J);
    }

    static bool WantsArcherAttackDown(bool mobileDown)
    {
        return mobileDown || GamepadControls.Down(GamepadControls.Attack)
            || (!MobileControls.IsVisible && Input.GetMouseButtonDown(0)) || Input.GetKeyDown(KeyCode.J);
    }

    static bool WantsArcherAttackHeld(bool mobileHeld)
    {
        return WantsAttack();
    }

    static bool WantsArcherAttackUp(bool mobileUp)
    {
        // Release only after ALL input sources have let go of the charged shot.
        return !WantsAttack() && (mobileUp || GamepadControls.Up(GamepadControls.Attack)
            || (!MobileControls.IsVisible && Input.GetMouseButtonUp(0)) || Input.GetKeyUp(KeyCode.J));
    }

    static bool WantsBlock()
    {
        return MobileControls.BlockHeld || GamepadControls.Held(GamepadControls.Block)
            || (!MobileControls.IsVisible && Input.GetMouseButton(1))
            || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.S);
    }

    static bool WantsSpecial()
    {
        bool touch = MobileControls.ConsumeSpecialDown();
        return touch || GamepadControls.Down(GamepadControls.Special)
            || Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Q);
    }

    static bool WantsDash()
    {
        bool touch = MobileControls.ConsumeDashPressed();
        return touch || GamepadControls.Down(GamepadControls.Dash)
            || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }
}
