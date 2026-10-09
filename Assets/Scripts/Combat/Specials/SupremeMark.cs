using UnityEngine;

public class SupremeMark : MonoBehaviour
{
    SpriteRenderer _renderer;
    float _clock;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        _clock += Time.deltaTime;
        if (_renderer == null)
            return;
        bool on = (int)(_clock * 12f) % 2 == 0;
        var color = _renderer.color;
        color.a = on ? 1f : 0.25f;
        _renderer.color = color;
    }
}
