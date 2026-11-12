using UnityEngine;

// Explicit warnings precede every damaging hazard. Both attacks use the normal
// player damage path, so the three hero shields absorb these attacks as well.
public sealed class NazorHazard : MonoBehaviour
{
    public VoidDevourerBoss Owner { get; private set; }
    public bool IsWarning => _eclipse && _age < WarningSeconds;
    public const float WarningSeconds = 1.05f;
    public bool IsEclipse => _eclipse;
    bool _eclipse, _released;
    float _age, _damage, _direction, _ground;
    SpriteRenderer _sprite, _warningStem;
    static Sprite _ring, _bolt;

    public static NazorHazard Sonic(VoidDevourerBoss owner, Vector3 origin, float direction, float damage)
    {
        var hazard = Create(owner, origin, damage);
        hazard._direction = direction;
        hazard._sprite.sprite = Ring();
        hazard._sprite.color = new Color(.75f, .35f, 1f, .95f);
        hazard.transform.localScale = new Vector3(.48f, .75f, 1f);
        return hazard;
    }
    public static NazorHazard Eclipse(VoidDevourerBoss owner, Vector3 ground, float damage)
    {
        var hazard = Create(owner, ground + Vector3.up * .25f, damage);
        hazard._ground = ground.y;
        hazard._eclipse = true;
        hazard._sprite.sprite = Ring();
        hazard._sprite.color = new Color(1f, .80f, .25f, .95f);
        hazard.transform.localScale = new Vector3(1.35f, .22f, 1f);
        var stem = new GameObject("AvisoEclipse");
        stem.transform.SetParent(hazard.transform, false);
        stem.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        stem.transform.localScale = new Vector3(.08f, 3.6f, 1f);
        hazard._warningStem = stem.AddComponent<SpriteRenderer>();
        hazard._warningStem.sprite = Bolt();
        hazard._warningStem.sortingOrder = 19;
        return hazard;
    }
    static NazorHazard Create(VoidDevourerBoss owner, Vector3 position, float damage)
    {
        var go = new GameObject("NazorHazard");
        go.transform.position = position;
        var hazard = go.AddComponent<NazorHazard>();
        hazard.Owner = owner;
        hazard._damage = damage;
        hazard._sprite = go.AddComponent<SpriteRenderer>();
        hazard._sprite.sortingOrder = 18;
        return hazard;
    }
    void Update()
    {
        if (Owner == null || Owner.CurrentState == VoidDevourerBoss.State.Transition || Owner.CurrentState == VoidDevourerBoss.State.Defeated)
        { Destroy(gameObject); return; }
        if (Time.timeScale <= 0f) return;
        _age += Time.deltaTime;
        if (_eclipse)
        {
            if (IsWarning)
            {
                float pulse = Mathf.Floor(_age * 10f) % 2 == 0 ? .6f : 1f;
                _sprite.color = new Color(1f, .80f, .25f, pulse);
                _warningStem.color = new Color(1f, .85f, .35f, pulse * .65f);
                return;
            }
            if (!_released)
            {
                _released = true;
                if (_warningStem != null) Destroy(_warningStem.gameObject);
                transform.position = new Vector3(transform.position.x, _ground + 8f, 0);
                transform.localScale = new Vector3(.6f, 1.3f, 1f);
                _sprite.sprite = Bolt();
                _sprite.color = new Color(1f, .60f, .95f);
            }
            transform.position += Vector3.down * (11f * Time.deltaTime);
            if (transform.position.y <= _ground + .15f)
            {
                PixelBurst.Spawn(transform.position, new Color(.95f, .3f, .7f), 8);
                GameAudio.Emit(GameAudio.Cue.BossVoidImpact, transform.position, .65f);
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            transform.position += Vector3.right * (_direction * 8f * Time.deltaTime);
            if (_age > 3.2f) { Destroy(gameObject); return; }
        }
        Vector2 hitbox = _eclipse ? new Vector2(.6f, .95f) : new Vector2(.35f, 1.1f);
        foreach (var collider in Physics2D.OverlapBoxAll(transform.position, hitbox, 0f))
        {
            var player = collider.GetComponentInParent<PlayerController>();
            if (player == null) continue;
            EnemyController.DamagePlayer(player.transform, transform.position, _damage);
            PixelBurst.Spawn(transform.position, _sprite.color, 5);
            Destroy(gameObject);
            break;
        }
    }
    static Sprite Ring()
    {
        if (_ring != null) return _ring;
        return _ring = PixelSprite(false);
    }
    static Sprite Bolt()
    {
        if (_bolt != null) return _bolt;
        return _bolt = PixelSprite(true);
    }
    static Sprite PixelSprite(bool bolt)
    {
        const int n = 32;
        var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = x - 15.5f, dy = y - 15.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            bool visible = bolt ? Mathf.Abs(dx) + Mathf.Abs(dy) * .44f < 7f : radius >= 12f && radius <= 15f;
            pixels[y * n + x] = visible ? new Color32(255, 235, 255, 255) : new Color32(0, 0, 0, 0);
        }
        texture.SetPixels32(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 16f, 0, SpriteMeshType.FullRect);
    }
}
