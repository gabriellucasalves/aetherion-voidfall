using UnityEngine;

/// <summary>
/// Pós-imagem curta do dash do Arqueiro — fade-out e destroy.
/// </summary>
public class DashGhost : MonoBehaviour
{
    float _life = 0.16f;
    float _age;
    SpriteRenderer _renderer;
    Color _start;

    public void Begin(float life)
    {
        _life = Mathf.Max(0.05f, life);
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _start = _renderer.color;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_renderer != null)
        {
            var c = _start;
            c.a = _start.a * (1f - _age / _life);
            _renderer.color = c;
        }

        if (_age >= _life)
            Destroy(gameObject);
    }
}
