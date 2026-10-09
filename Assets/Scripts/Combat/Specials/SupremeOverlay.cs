using UnityEngine;

public class SupremeOverlay : MonoBehaviour
{
    SpriteRenderer _renderer;
    float _life;
    float _age;
    bool _fading;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite sprite)
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _renderer.sprite = sprite;
    }

    public void PlayFade(float life)
    {
        _life = Mathf.Max(0.05f, life);
        _fading = true;
    }

    void Update()
    {
        if (!_fading || _renderer == null)
            return;

        _age += Time.unscaledDeltaTime;
        var color = _renderer.color;
        color.a = Mathf.Clamp01(1f - _age / _life);
        _renderer.color = color;
        if (_age >= _life)
            Destroy(gameObject);
    }
}
