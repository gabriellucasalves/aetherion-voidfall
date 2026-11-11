using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossLaser : MonoBehaviour
{
    static Sprite[] _muzzleFrames;
    static Sprite _segmentSprite;
    static Sprite[] _impactFrames;
    static Sprite _aimSprite;

    public static void EnsureSprites()
    {
        if (_segmentSprite != null) return;
        _muzzleFrames = LoadStrip("laser_muzzle_3f_24", 24, 24, new Vector2(0.5f, 0.5f));
        _segmentSprite = LoadSingleSprite("laser_segmento_16x8", new Vector2(0f, 0.5f), TextureWrapMode.Repeat);
        _impactFrames = LoadStrip("laser_impacto_3f_24", 24, 24, new Vector2(0.5f, 1f / 24f));
        _aimSprite = LoadSingleSprite("laser_mira_16x1", new Vector2(0f, 0.5f), TextureWrapMode.Repeat);
    }

    static Sprite[] LoadStrip(string name, int fw, int fh, Vector2 pivot)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/" + name);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        int frames = tex.width / fw;
        var arr = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            arr[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), pivot, 32f, 0, SpriteMeshType.FullRect);
        }
        return arr;
    }

    static Sprite LoadSingleSprite(string name, Vector2 pivot, TextureWrapMode wrap)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/v2/" + name);
        if (tex == null) return UiKit.WhiteSprite();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = wrap;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 32f, 0, SpriteMeshType.FullRect);
    }

    // Aim Line Indicator
    public static GameObject SpawnAimLine(Transform parent, Vector3 origin, Vector2 direction, float length, float duration)
    {
        EnsureSprites();
        var go = new GameObject("BossLaserAim");
        go.transform.SetParent(parent, false);
        go.transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _aimSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(length, 1f / 32f);
        sr.sortingOrder = 18;
        sr.color = Color.white;

        go.AddComponent<LaserAimBlinker>().Setup(duration);
        return go;
    }

    // Olhar Duplo Beam
    public static BossLaser FireDoubleGlareBeam(Vector3 origin, Vector2 direction, float damage, float duration = 0.5f)
    {
        EnsureSprites();
        var go = new GameObject("BossDoubleGlareBeam");
        go.transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var laser = go.AddComponent<BossLaser>();
        laser._origin = origin;
        laser._direction = direction.normalized;
        laser._damage = damage;
        laser._duration = duration;
        laser._isSweep = false;
        laser.BuildBeamVisual(26f);
        return laser;
    }

    // Varredura Rubra Beam
    public static BossLaser FireRedSweepBeam(Transform bossEye, Vector3 groundTarget, float damage)
    {
        EnsureSprites();
        var go = new GameObject("BossRedSweepBeam");
        var laser = go.AddComponent<BossLaser>();
        laser._bossEye = bossEye;
        laser._groundPos = groundTarget;
        laser._damage = damage;
        laser._isSweep = true;
        laser.BuildSweepVisual();
        return laser;
    }

    Vector3 _origin;
    Vector2 _direction;
    float _damage;
    float _duration;
    bool _isSweep;
    Transform _bossEye;
    Vector3 _groundPos;
    float _clock;
    bool _hasHitPlayer;
    float _nextSweepHitTime;

    SpriteRenderer _muzzleSr;
    SpriteRenderer _segmentSr;
    SpriteRenderer _impactSr;
    BoxCollider2D _collider;

    void BuildBeamVisual(float length)
    {
        var muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(transform, false);
        _muzzleSr = muzzleGo.AddComponent<SpriteRenderer>();
        if (_muzzleFrames != null && _muzzleFrames.Length > 0)
            _muzzleSr.sprite = _muzzleFrames[0];
        _muzzleSr.sortingOrder = 20;

        var segmentGo = new GameObject("Beam");
        segmentGo.transform.SetParent(transform, false);
        _segmentSr = segmentGo.AddComponent<SpriteRenderer>();
        _segmentSr.sprite = _segmentSprite;
        _segmentSr.sortingOrder = 19;
        _segmentSr.drawMode = SpriteDrawMode.Tiled;
        _segmentSr.size = new Vector2(length, 8f / 32f);

        var colGo = new GameObject("Collider");
        colGo.transform.SetParent(transform, false);
        _collider = colGo.AddComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _collider.size = new Vector2(length, 0.45f);
        _collider.offset = new Vector2(length * 0.5f, 0f);

        var impactGo = new GameObject("Impact");
        impactGo.transform.SetParent(transform, false);
        impactGo.transform.localPosition = new Vector3(length, 0f, 0f);
        _impactSr = impactGo.AddComponent<SpriteRenderer>();
        if (_impactFrames != null && _impactFrames.Length > 0)
            _impactSr.sprite = _impactFrames[0];
        _impactSr.sortingOrder = 20;
    }

    void BuildSweepVisual()
    {
        var muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(transform, false);
        _muzzleSr = muzzleGo.AddComponent<SpriteRenderer>();
        if (_muzzleFrames != null && _muzzleFrames.Length > 0)
            _muzzleSr.sprite = _muzzleFrames[0];
        _muzzleSr.sortingOrder = 20;

        var segmentGo = new GameObject("Beam");
        segmentGo.transform.SetParent(transform, false);
        _segmentSr = segmentGo.AddComponent<SpriteRenderer>();
        _segmentSr.sprite = _segmentSprite;
        _segmentSr.sortingOrder = 19;
        _segmentSr.drawMode = SpriteDrawMode.Tiled;

        var impactGo = new GameObject("Impact");
        impactGo.transform.SetParent(transform, false);
        _impactSr = impactGo.AddComponent<SpriteRenderer>();
        if (_impactFrames != null && _impactFrames.Length > 0)
            _impactSr.sprite = _impactFrames[0];
        _impactSr.sortingOrder = 20;

        _collider = gameObject.AddComponent<BoxCollider2D>();
        _collider.isTrigger = true;
    }

    public void UpdateSweep(Vector3 eyePos, Vector3 groundPos)
    {
        _origin = eyePos;
        _groundPos = groundPos;
        transform.position = eyePos;

        Vector2 diff = groundPos - eyePos;
        float dist = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (_segmentSr != null)
        {
            _segmentSr.size = new Vector2(dist, 8f / 32f);
        }
        if (_impactSr != null)
        {
            _impactSr.transform.localPosition = new Vector3(dist, 0f, 0f);
        }
        if (_collider != null)
        {
            _collider.size = new Vector2(dist, 0.55f);
            _collider.offset = new Vector2(dist * 0.5f, 0f);
        }

        // Fagulhas vermelhas no ponto de impacto no chão
        if (Random.value < 0.4f)
        {
            PixelBurst.Spawn(groundPos, new Color(1f, 0.2f, 0.25f), 3);
        }

        CheckPlayerHit(isSweep: true);
    }

    void Update()
    {
        _clock += Time.deltaTime;
        if (_muzzleSr != null && _muzzleFrames != null && _muzzleFrames.Length > 0)
            _muzzleSr.sprite = _muzzleFrames[Mathf.FloorToInt(_clock * 15f) % _muzzleFrames.Length];
        if (_impactSr != null && _impactFrames != null && _impactFrames.Length > 0)
            _impactSr.sprite = _impactFrames[Mathf.FloorToInt(_clock * 15f) % _impactFrames.Length];

        if (!_isSweep)
        {
            CheckPlayerHit(isSweep: false);
            if (_clock >= _duration)
            {
                Destroy(gameObject);
            }
        }
    }

    void CheckPlayerHit(bool isSweep)
    {
        if (isSweep)
        {
            if (Time.time < _nextSweepHitTime) return;
        }
        else
        {
            if (_hasHitPlayer) return;
        }

        if (_collider == null) return;

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        var hits = new List<Collider2D>();
        if (_collider.Overlap(filter, hits) > 0)
        {
            foreach (var hit in hits)
            {
                var player = hit.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    var health = player.GetComponent<HealthSystem>();
                    if (health != null && !health.IsDead)
                    {
                        if (isSweep)
                        {
                            _nextSweepHitTime = Time.time + 0.50f;
                        }
                        else
                        {
                            _hasHitPlayer = true;
                        }

                        EnemyController.DamagePlayer(player.transform, transform.position, _damage);
                        PixelBurst.Spawn(player.transform.position + Vector3.up * 0.5f, new Color(1f, 0.25f, 0.35f), 6);
                        break;
                    }
                }
            }
        }
    }
}

public class LaserAimBlinker : MonoBehaviour
{
    float _duration;
    float _clock;
    SpriteRenderer _sr;

    public void Setup(float duration)
    {
        _duration = duration;
        _sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        _clock += Time.deltaTime;
        if (_sr != null)
        {
            bool on = (Mathf.FloorToInt(_clock * 20f) % 2) == 0;
            _sr.enabled = on;
        }
        if (_clock >= _duration)
        {
            Destroy(gameObject);
        }
    }
}

