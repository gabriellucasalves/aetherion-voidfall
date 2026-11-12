using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public const float BaseSpeed = 5.4f;
    public const float AgilityFactor = 0.045f;
    public const float JumpSpeed = 13.2f;
    const float CoyoteTime = 0.09f;
    const float JumpBuffer = 0.12f;

    CharacterData _hero;
    StageData _stage;
    Rigidbody2D _body;
    HealthSystem _health;
    bool _locked;
    public bool IsLocked => _locked;
    Vector2 _facing = Vector2.right;
    float _coyote;
    float _jumpBuffer;
    bool _jumpHeld;
    float _dashLeft;
    float _dashSpeed;
    Vector2 _dashDir = Vector2.right;
    float _iFrames;

    public CharacterData Hero => _hero;
    public Vector2 Facing => _facing;
    public bool IsDashing => _dashLeft > 0f;
    public bool Grounded => IsGrounded();
    public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;
    public bool WantsMove => Mathf.Abs(ReadHorizontal()) > 0.01f;

    public void Setup(CharacterData hero, StageData stage)
    {
        _hero = hero;
        _stage = stage;
        _body = GetComponent<Rigidbody2D>();
        _health = GetComponent<HealthSystem>();
    }

    public void StartDash(Vector2 direction, float speed, float duration, float iFrames)
    {
        if (direction.sqrMagnitude < 0.01f)
            direction = _facing;
        _dashDir = direction.normalized;
        _facing = new Vector2(Mathf.Sign(_dashDir.x == 0f ? _facing.x : _dashDir.x), 0f);
        _dashSpeed = speed;
        _dashLeft = duration;
        _iFrames = iFrames;
        if (_health != null)
            _health.IsInvulnerable = true;
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked && _body != null)
        {
            _body.linearVelocity = Vector2.zero;
            _dashLeft = 0f;
        }
    }

    public void GrantIFrames(float seconds)
    {
        _iFrames = Mathf.Max(_iFrames, seconds);
        if (_health != null && _iFrames > 0f)
            _health.IsInvulnerable = true;
    }

    public float MoveSpeed
    {
        get
        {
            int agility = _hero != null ? _hero.Agility : 50;
            return BaseSpeed + agility * AgilityFactor;
        }
    }

    void Update()
    {
        if (_locked || _hero == null)
            return;

        if (WantsJumpDown())
            _jumpBuffer = JumpBuffer;

        _jumpHeld = WantsJumpHeld();
        _jumpBuffer -= Time.deltaTime;
        _coyote -= Time.deltaTime;
        if (_iFrames > 0f)
        {
            _iFrames -= Time.deltaTime;
            // O Supremo segura a invulnerabilidade sozinho; não deixa o fim do dash apagá-la.
            if (_health != null && !SpecialController.IsCinematic)
                _health.IsInvulnerable = _iFrames > 0f;
        }
    }

    void FixedUpdate()
    {
        if (_body == null)
            return;

        if (_locked || _hero == null)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        if (_dashLeft > 0f)
        {
            _dashLeft -= Time.fixedDeltaTime;
            var dashVelocity = _body.linearVelocity;
            dashVelocity.x = _dashDir.x * _dashSpeed;
            if (dashVelocity.y < -3f)
                dashVelocity.y = -3f;
            _body.linearVelocity = dashVelocity;
            FaceVisual();
            ClampX();
            return;
        }

        float x = ReadHorizontal();
        if (Mathf.Abs(x) > 0.01f)
            _facing = new Vector2(Mathf.Sign(x), 0f);

        var combat = GetComponent<PlayerCombat>();
        float speed = MoveSpeed;
        if (combat != null && combat.IsBlocking)
            speed *= combat.IsWarrior ? .42f : combat.IsMage ? .72f : .60f;
        else if (combat != null && combat.IsCharging)
            speed *= PlayerCombat.ChargeMoveScale; // 40% da velocidade durante a puxada/carga do Arqueiro

        var velocity = _body.linearVelocity;
        velocity.x = x * speed;

        bool grounded = IsGrounded();
        if (grounded)
            _coyote = CoyoteTime;

        if (_jumpBuffer > 0f && _coyote > 0f)
        {
            velocity.y = (T1AtmosScenery.Built || T2PalaceScenery.Built || T3RuinsScenery.Built) ? T1AtmosScenery.PlayerJumpSpeed : JumpSpeed;
            _jumpBuffer = 0f;
            _coyote = 0f;
        }
        else if (!_jumpHeld && velocity.y > 0f)
        {
            velocity.y *= 0.55f;
        }

        _body.linearVelocity = velocity;
        FaceVisual();
        ClampX();
    }

    public void FaceVisual()
    {
        float sign = _facing.x >= 0f ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
    }

    public void FaceVisual(bool right)
    {
        _facing = right ? Vector2.right : Vector2.left;
        FaceVisual();
    }

    void ClampX()
    {
        if (_stage == null)
            return;

        var position = transform.position;
        float min = -_stage.HalfWidth + 0.8f;
        float max = _stage.HalfWidth - 0.8f;
        if (position.x < min || position.x > max)
        {
            position.x = Mathf.Clamp(position.x, min, max);
            transform.position = position;
            var velocity = _body.linearVelocity;
            if ((position.x <= min && velocity.x < 0f) || (position.x >= max && velocity.x > 0f))
            {
                velocity.x = 0f;
                _body.linearVelocity = velocity;
            }
        }
    }

    bool IsGrounded()
    {
        var box = GetComponent<BoxCollider2D>();
        float bottom = box != null ? box.bounds.min.y : transform.position.y - 0.48f;
        if (_body != null && _body.linearVelocity.y > 0.1f) return false;
        var origin = new Vector2(transform.position.x, bottom + 0.08f);
        var hits = Physics2D.BoxCastAll(origin, new Vector2(0.32f, 0.04f), 0f, Vector2.down, 0.14f);
        foreach (var contact in hits)
        {
            var hit = contact.collider;
            if (hit == null || hit.isTrigger || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.GetComponentInParent<EnemyController>() != null) continue;
            if (hit.bounds.max.y > bottom + 0.11f || contact.normal.y < 0.7f) continue;
            return true;
        }

        return false;
    }

    static float ReadHorizontal()
    {
        float x = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(x) < 0.19f)
            x = GamepadControls.DpadHorizontal;
        if (Mathf.Abs(x) < 0.01f)
            x = MobileControls.Move;
        return x;
    }

    static bool WantsJumpDown()
    {
        return GamepadControls.Down(GamepadControls.Jump)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || MobileControls.ConsumeJumpDown();
    }

    static bool WantsJumpHeld()
    {
        return GamepadControls.Held(GamepadControls.Jump)
            || Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.UpArrow)
            || MobileControls.JumpHeld;
    }
}
