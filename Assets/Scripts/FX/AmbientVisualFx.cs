using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efeitos visuais de ambiente das Fases 1 e 2 (puramente decorativos, sem colisão e sem dano).
/// </summary>
public static class AmbientFxFactory
{
    public static Sprite[] Slice(Texture2D tex, int cellW, int cellH, Vector2 pivot)
    {
        if (tex == null) return null;
        int cols = tex.width / cellW;
        int rows = tex.height / cellH;
        var list = new List<Sprite>(cols * rows);
        for (int r = rows - 1; r >= 0; r--)
        {
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rect(c * cellW, r * cellH, cellW, cellH);
                list.Add(Sprite.Create(tex, rect, pivot, 32f, 0, SpriteMeshType.FullRect));
            }
        }
        return list.ToArray();
    }
}

public class AmbientSpriteAnimator : MonoBehaviour
{
    Sprite[] _frames;
    float _fps = 10f;
    bool _loop = true;
    bool _destroyOnComplete = false;
    SpriteRenderer _renderer;
    float _timer;
    int _index;

    public void Setup(Sprite[] frames, float fps, bool loop, bool destroyOnComplete = false)
    {
        _frames = frames;
        _fps = fps;
        _loop = loop;
        _destroyOnComplete = destroyOnComplete;
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
        if (_frames != null && _frames.Length > 0)
            _renderer.sprite = _frames[0];
    }

    void Update()
    {
        if (_frames == null || _frames.Length <= 1 || _fps <= 0f) return;
        _timer += Time.deltaTime;
        float frameDur = 1f / _fps;
        if (_timer >= frameDur)
        {
            _timer -= frameDur;
            _index++;
            if (_index >= _frames.Length)
            {
                if (_loop)
                {
                    _index = 0;
                }
                else
                {
                    _index = _frames.Length - 1;
                    if (_destroyOnComplete)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    enabled = false;
                }
            }
            if (_renderer != null) _renderer.sprite = _frames[_index];
        }
    }
}

/// <summary>
/// Pétalas ou motes de Aether flutuando na tela em loop suave.
/// </summary>
public class AmbientFloatingDrift : MonoBehaviour
{
    Vector3 _velocity;
    float _minX, _maxX;
    float _minY, _maxY;
    float _phase;

    public void Setup(Vector3 velocity, float minX, float maxX, float minY, float maxY)
    {
        _velocity = velocity;
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;
        _phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        _phase += Time.deltaTime * 2f;
        Vector3 drift = new Vector3(Mathf.Sin(_phase) * 0.4f, Mathf.Cos(_phase * 0.7f) * 0.2f, 0f);
        transform.position += (_velocity + drift) * Time.deltaTime;

        var pos = transform.position;
        if (pos.x < _minX) pos.x = _maxX;
        else if (pos.x > _maxX) pos.x = _minX;

        if (pos.y < _minY) pos.y = _maxY;
        else if (pos.y > _maxY) pos.y = _minY;

        transform.position = pos;
    }
}

/// <summary>
/// Meteoro caindo no fundo da Fase 2 (diagonal superior-direita para inferior-esquerda).
/// </summary>
public class AmbientMeteorVisual : MonoBehaviour
{
    float _groundY;
    float _speed = 14f;
    Sprite[] _meteorFrames;
    Sprite[] _explosionFrames;
    Sprite[] _smokeFrames;
    Transform _parent;
    float _respawnTimer;
    bool _falling;

    public void Setup(Sprite[] meteorFrames, Sprite[] explosionFrames, Sprite[] smokeFrames, float groundY, Transform parent)
    {
        _meteorFrames = meteorFrames;
        _explosionFrames = explosionFrames;
        _smokeFrames = smokeFrames;
        _groundY = groundY;
        _parent = parent;
        StartFalling();
    }

    void StartFalling()
    {
        _falling = true;
        _speed = Random.Range(12f, 16f);
        float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;
        float startX = camX + Random.Range(4f, 16f);
        float startY = _groundY + Random.Range(8f, 13f);
        transform.position = new Vector3(startX, startY, 0f);
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_falling)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) StartFalling();
            return;
        }

        transform.position += new Vector3(-_speed * 0.85f, -_speed * 0.55f, 0f) * Time.deltaTime;

        if (transform.position.y <= _groundY)
        {
            _falling = false;
            _respawnTimer = Random.Range(1.5f, 3.0f);

            // 1 em cada ~3 meteoros cria impacto visual no solo
            if (Random.value < 0.4f && _explosionFrames != null && _explosionFrames.Length > 0)
            {
                SpawnImpact(transform.position.x, _groundY);
            }
            gameObject.SetActive(false);
        }
    }

    void SpawnImpact(float x, float y)
    {
        var expGo = new GameObject("Ambient_Meteoro_Impacto");
        expGo.transform.SetParent(_parent, false);
        expGo.transform.position = new Vector3(x, y, 0f);
        var sr = expGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -8;
        var anim = expGo.AddComponent<AmbientSpriteAnimator>();
        anim.Setup(_explosionFrames, 14f, loop: false, destroyOnComplete: true);

        if (_smokeFrames != null && _smokeFrames.Length > 0)
        {
            var smokeGo = new GameObject("Ambient_Meteoro_Fumaca");
            smokeGo.transform.SetParent(_parent, false);
            smokeGo.transform.position = new Vector3(x, y, 0f);
            var srSmoke = smokeGo.AddComponent<SpriteRenderer>();
            srSmoke.sortingOrder = -8;
            var animSmoke = smokeGo.AddComponent<AmbientSpriteAnimator>();
            animSmoke.Setup(_smokeFrames, 10f, loop: false, destroyOnComplete: true);
        }
    }
}

