using UnityEngine;

// Explicit warnings precede every damaging hazard. Both attacks use the normal
// player damage path, so the three hero shields absorb these attacks as well.
public sealed class NazorHazard : MonoBehaviour
{
    public VoidDevourerBoss Owner { get; private set; }
    public bool IsWarning => _eclipse && _age < _warningDuration;
    float _warningDuration = WarningSeconds;
    public const float WarningSeconds = 1.05f;
    public bool IsEclipse => _eclipse;
    bool _eclipse, _released;
    float _age, _damage, _ground, _spawnHeight;
    Vector2 _travelDirection;
    public Vector2 TravelDirection => _travelDirection;
    public Vector3 WarningPosition { get; private set; }
    public float SpawnHeight => _spawnHeight;
    SpriteRenderer _sprite, _warningStem;
    Sprite[] _activeFrames;
    GameObject _groundMarker;
    GameObject _columnWarning;
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

        var frames = NazorSheets.Get("sonic_onda_magenta_48x64_6f");
        if (frames != null && frames.Length > 0)
        {
            hazard._activeFrames = frames;
            hazard._sprite.sprite = frames[0];
            hazard._sprite.color = Color.white;
            hazard.transform.localScale = Vector3.one;
        }
        else
        {
            hazard._sprite.sprite = Ring();
            hazard._sprite.color = new Color(.75f, .35f, 1f, .95f);
            hazard.transform.localScale = new Vector3(.48f, .75f, 1f);
        }
        return hazard;
    }

    public static NazorHazard Eclipse(VoidDevourerBoss owner, Vector3 ground, float damage)
    {
        return Eclipse(owner, ground, damage, ground.y, ground.y + 8f);
    }

    public static NazorHazard Eclipse(VoidDevourerBoss owner, Vector3 marker, float damage, float floor, float sky, float warning = WarningSeconds)
    {
        var hazard = Create(owner, marker + Vector3.up * .25f, damage);
        hazard.WarningPosition = marker;
        hazard._ground = floor;
        hazard._spawnHeight = Mathf.Max(sky, marker.y + 5f);
        hazard._eclipse = true;
        hazard._warningDuration = warning;
        NazorVfx.Mark("eclipse", false);

        hazard._groundMarker = NazorVfx.Play("marcador_chao_dourado_48x12_4f", marker, 0f, 1f, true, warning, 17);
        float colH = hazard._spawnHeight - marker.y;
        hazard._columnWarning = NazorVfx.Column("eclipse_coluna_aviso_40x144_6f", marker, colH, 18, null, warning);

        hazard._sprite.enabled = false;
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
        {
            CleanupVfx();
            Destroy(gameObject);
            return;
        }
        if (Time.timeScale <= 0f) return;
        _age += Time.deltaTime;
        Vector3 previous = transform.position;

        if (_eclipse)
        {
            if (IsWarning)
            {
                // Coluna e marcador pulsam suavemente sem piscar
                float pulse = 0.7f + 0.3f * Mathf.Sin(_age * 8f);
                if (_columnWarning != null)
                {
                    var sr = _columnWarning.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 1f, 1f, pulse);
                }
                return;
            }

            if (!_released)
            {
                _released = true;
                NazorVfx.Mark("eclipse", true);
                CleanupVfx();
                transform.position = new Vector3(transform.position.x, _spawnHeight, 0);
                previous = transform.position;
                _sprite.enabled = true;

                var projFrames = NazorSheets.Get("eclipse_projetil_24x56_4f");
                if (projFrames != null && projFrames.Length > 0)
                {
                    _activeFrames = projFrames;
                    _sprite.sprite = projFrames[0];
                    _sprite.color = Color.white;
                    transform.localScale = Vector3.one;
                }
                else
                {
                    transform.localScale = new Vector3(.6f, 1.3f, 1f);
                    _sprite.sprite = Bolt();
                    _sprite.color = new Color(1f, .60f, .95f);
                }
            }
            transform.position += Vector3.down * (14f * Time.deltaTime);
        }
        else
        {
            transform.position += (Vector3)_travelDirection * (9.5f * Time.deltaTime);
            if (_age > 3.2f) { Destroy(gameObject); return; }
        }

        if (_activeFrames != null && _activeFrames.Length > 0 && _sprite.enabled)
        {
            float fps = NazorSheets.TryGetVfxEntry(_eclipse ? "eclipse_projetil_24x56_4f" : "sonic_onda_magenta_48x64_6f", out var entry) ? entry.Fps : 12f;
            int fIdx = Mathf.FloorToInt(_age * fps) % _activeFrames.Length;
            _sprite.sprite = _activeFrames[fIdx];
        }

        Vector2 hitbox = _eclipse ? new Vector2(.6f, .95f) : new Vector2(.55f, 1.1f);
        Vector2 travel = transform.position - previous;
        foreach (var hit in Physics2D.BoxCastAll(previous, hitbox, transform.eulerAngles.z,
                     travel.normalized, travel.magnitude))
        {
            var player = hit.collider.GetComponentInParent<PlayerController>();
            if (player == null) continue;

            NazorVfx.DamagePlayer(player.transform, transform.position, _damage, _eclipse ? 55 : 45);

            if (_eclipse) NazorVfx.Juice("eclipse");
            if (!_eclipse)
            {
                NazorVfx.Play("sonic_impacto_magenta_48x48_6f", transform.position, 0f, 1f, false, 0.4f, 20);
                PixelBurst.Spawn(transform.position, new Color(0.85f, 0.35f, 1f), 5);
            }
            else
            {
                NazorVfx.Play("eclipse_cratera_80x64_7f", new Vector3(transform.position.x, _ground, 0f), 0f, 1f, false, 0.4f, 17);
                PixelBurst.Spawn(transform.position, _sprite.color, 5);
            }

            Destroy(gameObject);
            return;
        }

        if (_eclipse && transform.position.y <= _ground + .15f)
        {
            var crater = NazorVfx.Play("eclipse_cratera_80x64_7f", new Vector3(transform.position.x, _ground, 0f), duration: .6f, sortingOrder: 17);
            if (crater != null) crater.GetComponent<NazorVfxInstance>().FadeSeconds = .2f;
            NazorVfx.Juice("eclipse");
            PixelBurst.Spawn(transform.position, new Color(.95f, .3f, .7f), 8);
            GameAudio.Emit(GameAudio.Cue.BossVoidImpact, transform.position, .65f);
            Destroy(gameObject);
        }
    }

    void CleanupVfx()
    {
        if (_groundMarker != null) { Destroy(_groundMarker); _groundMarker = null; }
        if (_columnWarning != null) { Destroy(_columnWarning); _columnWarning = null; }
        if (_warningStem != null) { Destroy(_warningStem.gameObject); _warningStem = null; }
    }

    void OnDestroy()
    {
        CleanupVfx();
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
