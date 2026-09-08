using UnityEngine;

public class Ball : MonoBehaviour
{
    public float StartSpeed = 7.5f;
    public float SpeedGain = 0.45f;
    public float MaxSpeed = 16f;

    Rigidbody2D _body;
    float _speed;
    Vector2 _direction = Vector2.right;

    public Vector2 Velocity => _direction * _speed;

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            _body = gameObject.AddComponent<Rigidbody2D>();

        _body.bodyType = RigidbodyType2D.Dynamic;
        _body.gravityScale = 0f;
        _body.linearDamping = 0f;
        _body.angularDamping = 0f;
        _body.freezeRotation = true;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _speed = StartSpeed;
        ApplyVelocity();
    }

    public void Serve(int towardPlayer)
    {
        _speed = StartSpeed;
        float x = towardPlayer == 2 ? 1f : -1f;
        float y = Random.Range(-0.55f, 0.55f);
        _direction = new Vector2(x, y).normalized;
        transform.position = Vector3.zero;
        ApplyVelocity();
    }

    public void Stop()
    {
        _speed = 0f;
        _direction = Vector2.zero;
        transform.position = Vector3.zero;
        ApplyVelocity();
    }

    void FixedUpdate()
    {
        ApplyVelocity();
    }

    void ApplyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        _body.linearVelocity = _direction * _speed;
#else
        _body.velocity = _direction * _speed;
#endif
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var paddle = collision.collider.GetComponent<Paddle>();
        if (paddle != null)
        {
            BounceFromPaddle(paddle);
            ApplyVelocity();
            if (PongGame.Instance != null)
                PongGame.Instance.Audio.PlayPaddle();
            return;
        }

        if (collision.collider.GetComponent<WallMarker>() != null)
        {
            _direction = new Vector2(_direction.x, -_direction.y).normalized;
            ApplyVelocity();
            if (PongGame.Instance != null)
                PongGame.Instance.Audio.PlayWall();
        }
    }

    void BounceFromPaddle(Paddle paddle)
    {
        float half = paddle.transform.localScale.y * 0.5f;
        float offset = (transform.position.y - paddle.transform.position.y) / Mathf.Max(0.1f, half);
        offset = Mathf.Clamp(offset, -1f, 1f);

        float x = paddle.PlayerSide == Paddle.Side.Left ? 1f : -1f;
        _direction = new Vector2(x, offset).normalized;
        _speed = Mathf.Min(MaxSpeed, _speed + SpeedGain);
    }
}
