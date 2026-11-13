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
    float _age, _damage, _ground, _spawnHeight;
    Vector2 _travelDirection;
    public Vector2 TravelDirection => _travelDirection;
    public Vector3 WarningPosition { get; private set; }
    public float SpawnHeight => _spawnHeight;
    SpriteRenderer _sprite, _warningStem;
    static Sprite _ring, _bolt;

    public static NazorHazard Sonic(VoidDevourerBoss owner, Vector3 origin, float direction, float damage)
    {
        return Sonic(owner, origin, Vector2.right * direction, damage);
    }
    public static NazorHazard Sonic(VoidDevourerBoss owner, Vector3 origin, Vector2 direction, float damage)
    {
        var hazard = Create(owner, origin, damage);
        hazard._travelDirection = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.left;
        hazard.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        hazard._sprite.sprite = Ring();
        hazard._sprite.color = new Color(.75f, .35f, 1f, .95f);
        hazard.transform.localScale = new Vector3(.48f, .75f, 1f);
        return hazard;
    }
    public static NazorHazard Eclipse(VoidDevourerBoss owner, Vector3 ground, float damage)
    {
        return Eclipse(owner, ground, damage, ground.y, ground.y + 8f);
    }
    public static NazorHazard Eclipse(VoidDevourerBoss owner, Vector3 marker, float damage, float floor, float sky)
    {
        var hazard = Create(owner, marker + Vector3.up * .25f, damage);
        hazard.WarningPosition = marker;
        hazard._ground = floor;
        hazard._spawnHeight = Mathf.Max(sky, marker.y + 5f);
        hazard._eclipse = true;
        hazard._sprite.sprite = Ring();
        hazard._sprite.color = new Color(1f, .80f, .25f, .95f);
        hazard.transform.localScale = new Vector3(1.35f, .22f, 1f);
        var stem = new GameObject("AvisoEclipse");
        stem.transform.SetParent(hazard.transform, false);
        float height = hazard._spawnHeight - hazard.transform.position.y;
        stem.transform.localPosition = new Vector3(0f, height * .5f / .22f, 0f);
        stem.transform.localScale = new Vector3(.08f / 1.35f, height * .5f / .22f, 1f);
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
        Vector3 previous = transform.position;
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
                transform.position = new Vector3(transform.position.x, _spawnHeight, 0);
                previous = transform.position; // Never sweep the non-damaging warning up into the sky.
                transform.localScale = new Vector3(.6f, 1.3f, 1f);
                _sprite.sprite = Bolt();
                _sprite.color = new Color(1f, .60f, .95f);
            }
            transform.position += Vector3.down * (14f * Time.deltaTime);
        }
        else
        {
            transform.position += (Vector3)_travelDirection * (9.5f * Time.deltaTime);
            if (_age > 3.2f) { Destroy(gameObject); return; }
        }
        Vector2 hitbox = _eclipse ? new Vector2(.6f, .95f) : new Vector2(.55f, 1.1f);
        Vector2 travel = transform.position - previous;
        // Swept collision also catches a hero on a platform at lower mobile FPS.
        foreach (var hit in Physics2D.BoxCastAll(previous, hitbox, transform.eulerAngles.z,
                     travel.normalized, travel.magnitude))
        {
            var player = hit.collider.GetComponentInParent<PlayerController>();
            if (player == null) continue;
            EnemyController.DamagePlayer(player.transform, transform.position, _damage);
            PixelBurst.Spawn(transform.position, _sprite.color, 5);
            Destroy(gameObject);
            return;
        }
        if (_eclipse && transform.position.y <= _ground + .15f)
        {
            PixelBurst.Spawn(transform.position, new Color(.95f, .3f, .7f), 8);
            GameAudio.Emit(GameAudio.Cue.BossVoidImpact, transform.position, .65f);
            Destroy(gameObject);
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
