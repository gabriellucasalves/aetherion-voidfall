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
            if (_health != null)
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
            speed *= 0.42f;

        var velocity = _body.linearVelocity;
        velocity.x = x * speed;

        bool grounded = IsGrounded();
        if (grounded)
            _coyote = CoyoteTime;

        if (_jumpBuffer > 0f && _coyote > 0f)
        {
            velocity.y = JumpSpeed;
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

    void FaceVisual()
    {
        float sign = _facing.x >= 0f ? 1f : -1f;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
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
        var origin = new Vector2(transform.position.x, bottom + 0.04f);
        var hits = Physics2D.OverlapBoxAll(origin, new Vector2(0.32f, 0.16f), 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.isTrigger || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            if (hit.GetComponent<EnemyController>() != null)
                continue;
            return true;
        }

        return false;
    }

    static float ReadHorizontal()
    {
        float x = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Mathf.Abs(x) < 0.01f)
            x = MobileControls.Move;
        return x;
    }

    static bool WantsJumpDown()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || MobileControls.ConsumeJumpDown();
    }

    static bool WantsJumpHeld()
    {
        return Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.UpArrow)
            || MobileControls.JumpHeld;
    }
}
