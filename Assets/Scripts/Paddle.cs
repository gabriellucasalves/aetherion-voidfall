using UnityEngine;

public class Paddle : MonoBehaviour
{
    public enum Side
    {
        Left,
        Right
    }

    public Side PlayerSide = Side.Left;
    public bool UseAi;
    public float Speed = 9f;
    public float AiReaction = 0.85f;

    Rigidbody2D _body;
    float _limit;

    public void Setup(Side side, bool useAi, float moveLimit)
    {
        PlayerSide = side;
        UseAi = useAi;
        _limit = moveLimit;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        if (_body == null)
            _body = gameObject.AddComponent<Rigidbody2D>();

        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void FixedUpdate()
    {
        if (PongGame.Instance == null)
            return;

        float input = UseAi ? ReadAi() : ReadPlayer();
        float nextY = transform.position.y + input * Speed * Time.fixedDeltaTime;
        nextY = Mathf.Clamp(nextY, -_limit, _limit);
        _body.MovePosition(new Vector2(transform.position.x, nextY));
    }

    float ReadPlayer()
    {
        if (PlayerSide == Side.Left)
        {
            float value = 0f;
            if (Input.GetKey(KeyCode.W)) value += 1f;
            if (Input.GetKey(KeyCode.S)) value -= 1f;
            return value;
        }

        float arrows = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) arrows += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) arrows -= 1f;
        return arrows;
    }

    float ReadAi()
    {
        var ball = PongGame.Instance.ActiveBall;
        if (ball == null)
            return 0f;

        bool comingThisWay = PlayerSide == Side.Right
            ? ball.Velocity.x > 0f
            : ball.Velocity.x < 0f;

        if (!comingThisWay)
            return 0f;

        float delta = ball.transform.position.y - transform.position.y;
        if (Mathf.Abs(delta) < 0.12f)
            return 0f;

        return Mathf.Sign(delta) * AiReaction;
    }
}
