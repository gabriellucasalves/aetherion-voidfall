using UnityEngine;

public class PixelBurst : MonoBehaviour
{
    Vector2 _velocity;
    float _life;
    float _age;
    SpriteRenderer _renderer;

    public static void Spawn(Vector3 position, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Pixel");
            go.transform.position = position;
            var burst = go.AddComponent<PixelBurst>();
            burst.Kick(color);
        }
    }

    void Kick(Color color)
    {
        _velocity = Random.insideUnitCircle * 3.4f;
        _life = Random.Range(0.12f, 0.22f);
        _renderer = gameObject.AddComponent<SpriteRenderer>();
        var texture = Texture2D.whiteTexture;
        _renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        _renderer.color = color;
        _renderer.sortingOrder = 22;
        transform.localScale = new Vector3(0.12f, 0.12f, 1f);
    }

    void Update()
    {
        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _age += Time.deltaTime;
        if (_renderer != null)
        {
            var color = _renderer.color;
            color.a = 1f - _age / _life;
            _renderer.color = color;
        }

        if (_age >= _life)
            Destroy(gameObject);
    }
}
