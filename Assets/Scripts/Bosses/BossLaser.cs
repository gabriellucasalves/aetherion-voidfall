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
        _muzzleFrames = NazorSheets.Get("laser_boca_vermelho_32x32_4f");
        if (_muzzleFrames == null || _muzzleFrames.Length == 0)
            _muzzleFrames = LoadStrip("laser_muzzle_3f_24", 24, 24, new Vector2(0.5f, 0.5f));

        var segFrames = NazorSheets.Get("laser_olhar_vermelho_16x14_4f");
        if (segFrames != null && segFrames.Length > 0)
            _segmentSprite = segFrames[0];
        else
            _segmentSprite = LoadSingleSprite("laser_segmento_16x8", new Vector2(0f, 0.5f), TextureWrapMode.Repeat);

        _impactFrames = NazorSheets.Get("laser_impacto_vermelho_48x48_6f");
        if (_impactFrames == null || _impactFrames.Length == 0)
            _impactFrames = LoadStrip("laser_impacto_3f_24", 24, 24, new Vector2(0.5f, 1f / 24f));

        var aimFrames = NazorSheets.Get("aviso_mira_vermelho_16x8_4f");
        if (aimFrames != null && aimFrames.Length > 0)
            _aimSprite = aimFrames[0];
        else
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
    public static GameObject SpawnAimLine(Transform parent, Vector3 origin, Vector2 direction, float length, float duration, string color = "vermelho")
    {
        EnsureSprites();
        var go = new GameObject("BossLaserAim");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(length, 8f / 32f);
        sr.sortingOrder = 18;
        sr.color = Color.white;

        var blinker = go.AddComponent<LaserAimBlinker>();
        blinker.Setup(duration, length, color);
        return go;
    }

    // Olhar Duplo Beam
    public static BossLaser FireDoubleGlareBeam(Vector3 origin, Vector2 direction, float damage, float duration = 0.55f, int hitstop = 45, bool playJuice = true, string juice = "olhar")
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
        laser._hitstop = hitstop;
        laser._isSweep = false;
        laser.BuildBeamVisual(26f);

        if (playJuice) NazorVfx.Juice(juice);

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

        NazorVfx.Juice("varredura");

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
    int _hitstop = 45;
    bool _hasHitPlayer;
    float _nextSweepHitTime;
    float _impactSpawnTimer;

    SpriteRenderer _muzzleSr;
    SpriteRenderer _segmentSr;
    SpriteRenderer _impactSr;
    BoxCollider2D _collider;
    Sprite[] _activeSegmentFrames;

    void BuildBeamVisual(float length)
    {
        var muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(transform, false);
        _muzzleSr = muzzleGo.AddComponent<SpriteRenderer>();
        _muzzleSr.sortingOrder = 20;

        _activeSegmentFrames = NazorSheets.Get("laser_olhar_vermelho_16x14_4f");
        var segmentGo = new GameObject("Beam");
        segmentGo.transform.SetParent(transform, false);
        _segmentSr = segmentGo.AddComponent<SpriteRenderer>();
        _segmentSr.sprite = (_activeSegmentFrames != null && _activeSegmentFrames.Length > 0) ? _activeSegmentFrames[0] : _segmentSprite;
        _segmentSr.sortingOrder = 19;
        _segmentSr.drawMode = SpriteDrawMode.Tiled;
        _segmentSr.size = new Vector2(length, 14f / 32f);

        var colGo = new GameObject("Collider");
        colGo.transform.SetParent(transform, false);
        _collider = colGo.AddComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _collider.size = new Vector2(length, 0.3f);
        _collider.offset = new Vector2(length * 0.5f, 0f);

        var impactGo = new GameObject("Impact");
        impactGo.transform.SetParent(transform, false);
        impactGo.transform.localPosition = new Vector3(length, 0f, 0f);
        _impactSr = impactGo.AddComponent<SpriteRenderer>();
        _impactSr.sortingOrder = 20;
    }

    void BuildSweepVisual()
    {
        var muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(transform, false);
        _muzzleSr = muzzleGo.AddComponent<SpriteRenderer>();
        _muzzleSr.sortingOrder = 20;
        muzzleGo.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        _activeSegmentFrames = NazorSheets.Get("laser_varredura_vermelho_16x18_4f");
        var segmentGo = new GameObject("Beam");
        segmentGo.transform.SetParent(transform, false);
        _segmentSr = segmentGo.AddComponent<SpriteRenderer>();
        _segmentSr.sprite = (_activeSegmentFrames != null && _activeSegmentFrames.Length > 0) ? _activeSegmentFrames[0] : _segmentSprite;
        _segmentSr.sortingOrder = 19;
        _segmentSr.drawMode = SpriteDrawMode.Tiled;

        var impactGo = new GameObject("Impact");
        impactGo.transform.SetParent(transform, false);
        _impactSr = impactGo.AddComponent<SpriteRenderer>();
        _impactSr.sortingOrder = 20;
        impactGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

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
            _segmentSr.size = new Vector2(dist, 18f / 32f);
        }
        if (_impactSr != null)
        {
            _impactSr.transform.localPosition = new Vector3(dist, 0f, 0f);
        }
        if (_collider != null)
        {
            _collider.size = new Vector2(dist, 0.45f);
            _collider.offset = new Vector2(dist * 0.5f, 0f);
        }

        _impactSpawnTimer += Time.deltaTime;
        if (_impactSpawnTimer >= 0.1f)
        {
            _impactSpawnTimer = 0f;
            NazorVfx.Play("laser_impacto_vermelho_48x48_6f", groundPos, 0f, 1.2f, false, 0.4f, 20);
        }

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
            _muzzleSr.sprite = _muzzleFrames[Mathf.FloorToInt(_clock * 20f) % _muzzleFrames.Length];

        if (_segmentSr != null && _activeSegmentFrames != null && _activeSegmentFrames.Length > 0)
            _segmentSr.sprite = _activeSegmentFrames[Mathf.FloorToInt(_clock * 20f) % _activeSegmentFrames.Length];

        if (_impactSr != null && _impactFrames != null && _impactFrames.Length > 0)
            _impactSr.sprite = _impactFrames[Mathf.FloorToInt(_clock * 18f) % _impactFrames.Length];

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
                            _nextSweepHitTime = Time.time + BossData.ContactCooldown;
                        }
                        else
                        {
                            _hasHitPlayer = true;
                        }

                        NazorVfx.DamagePlayer(player.transform, transform.position, _damage, isSweep ? 50 : _hitstop);
                        NazorVfx.Play("laser_impacto_vermelho_48x48_6f", player.transform.position);
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
    float _length;
    string _color = "vermelho";
    SpriteRenderer _sr;
    Sprite[] _activeFrames;
    Sprite[] _lockedFrames;
    bool _isLocked;

    public void Setup(float duration, float length = 24f, string color = "vermelho")
    {
        _duration = duration;
        _length = length;
        _color = string.IsNullOrEmpty(color) ? "vermelho" : color;
        _sr = GetComponent<SpriteRenderer>();

        _activeFrames = NazorSheets.Get($"aviso_mira_{_color}_16x8_4f");
        if (_activeFrames == null || _activeFrames.Length == 0)
            _activeFrames = NazorSheets.Get("aviso_mira_vermelho_16x8_4f");

        _lockedFrames = NazorSheets.Get($"aviso_mira_travada_{_color}_16x8_2f");
        if (_lockedFrames == null || _lockedFrames.Length == 0)
            _lockedFrames = NazorSheets.Get("aviso_mira_travada_vermelho_16x8_2f");

        if (_sr != null && _activeFrames != null && _activeFrames.Length > 0)
        {
            _sr.sprite = _activeFrames[0];
            _sr.size = new Vector2(_length, 8f / 32f);
        }
    }

    void Update()
    {
        _clock += Time.deltaTime;
        float remaining = _duration - _clock;

        if (remaining <= 0.32f && !_isLocked)
        {
            _isLocked = true;
        }

        var frames = _isLocked ? _lockedFrames : _activeFrames;
        float fps = _isLocked ? 16f : 12f;

        if (_sr != null && frames != null && frames.Length > 0)
        {
            int idx = Mathf.FloorToInt(_clock * fps) % frames.Length;
            _sr.sprite = frames[idx];
            _sr.size = new Vector2(_length, 8f / 32f);
        }

        if (_clock >= _duration)
        {
            Destroy(gameObject);
        }
    }
}
