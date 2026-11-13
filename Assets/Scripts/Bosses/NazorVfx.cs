using UnityEngine;

public static class NazorVfx
{
    public static event System.Action<string, float, float> ShakePlayed;
    public static event System.Action<string, bool, float> AttackMarked;
    public static void Mark(string attack, bool active) { AttackMarked?.Invoke(attack, active, Time.time); }
    public static event System.Action<float, int> PlayerHit;
    public static bool DamagePlayer(Transform player, Vector3 from, float damage, int hitstopMs)
    {
        if (player == null) return false;
        var health = player.GetComponent<HealthSystem>();
        var shield = player.GetComponent<ShieldSystem>();
        float before = (health != null ? health.Current : 0f) + (shield != null ? shield.Current : 0f);
        EnemyController.DamagePlayer(player, from, damage);
        float after = (health != null ? health.Current : 0f) + (shield != null ? shield.Current : 0f);
        if (before - after < .0001f) return false;
        Play("faisca_acerto_32x32_4f", player.position + Vector3.up * .5f, duration: .3f, sortingOrder: 25);
        PixelBurst.Spawn(player.position + Vector3.up * .5f, new Color(1f, .5f, .7f), 5);
        PlayerHit?.Invoke(damage, hitstopMs);
        HitStop.Do(hitstopMs);
        return true;
    }
    public static void Juice(string attack)
    {
        float seconds = .1f, magnitude = .04f, alpha = 0f, flash = .1f;
        Color color = Color.white;
        switch (attack)
        {
            case "olhar": color = new Color32(255,60,70,255); alpha = .14f; flash = .09f; break;
            case "varredura": seconds = 1.6f; magnitude = .035f; color = new Color32(255,40,60,255); alpha = .12f; break;
            case "tentaculo": seconds = .14f; magnitude = .06f; break;
            case "mergulho": seconds = .22f; magnitude = .12f; alpha = .22f; break;
            case "nucleo": seconds = .12f; magnitude = .06f; color = new Color32(255,214,120,255); alpha = .1f; flash = .08f; break;
            case "sonica": seconds = .09f; magnitude = .06f; color = new Color32(225,70,185,255); alpha = .12f; flash = .08f; break;
            case "eclipse": seconds = .12f; magnitude = .08f; color = new Color32(255,214,120,255); alpha = .12f; break;
            case "desabamento": seconds = .2f; magnitude = .1f; color = new Color32(225,70,185,255); alpha = .1f; flash = .08f; break;
            case "combo": color = new Color32(225,70,185,255); alpha = .14f; flash = .09f; break;
            case "fenda": seconds = .45f; magnitude = .07f; color = new Color32(130,225,255,255); alpha = .3f; flash = .12f; break;
            case "garras": seconds = .16f; magnitude = .09f; color = new Color32(130,225,255,255); alpha = .1f; flash = .08f; break;
            case "enrage": seconds = .45f; magnitude = .2f; color = new Color32(235,50,70,255); alpha = .35f; flash = .12f; break;
        }
        CameraFollow.Shake(seconds, magnitude);
        ShakePlayed?.Invoke(attack, seconds, magnitude);
        if (alpha > 0f) SupremeFx.PlayFlash(color, alpha, flash);
    }

    public static GameObject Play(string sheet, Vector3 pos, float rotZ = 0f, float scale = 1f, bool loop = false, float duration = 0f, int sortingOrder = 19, Transform parent = null)
    {
        var frames = NazorSheets.Get(sheet);
        if (frames == null || frames.Length == 0) return null;

        float fps = 12f;
        if (NazorSheets.TryGetVfxEntry(sheet, out var entry))
        {
            fps = entry.Fps > 0 ? entry.Fps : 12f;
        }

        var go = new GameObject("Vfx_" + sheet);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = sortingOrder;

        var inst = go.AddComponent<NazorVfxInstance>();
        inst.Setup(frames, fps, loop, duration);
        return go;
    }

    public static GameObject Tiled(string sheet, Vector3 origin, float angleDeg, float lengthU, int sortingOrder = 18, Transform parent = null, float duration = 0f)
    {
        var frames = NazorSheets.Get(sheet);
        if (frames == null || frames.Length == 0) return null;

        float fps = 12f;
        float cellH = frames[0].rect.height;
        if (NazorSheets.TryGetVfxEntry(sheet, out var entry))
        {
            fps = entry.Fps > 0 ? entry.Fps : 12f;
            if (entry.CelulaH > 0) cellH = entry.CelulaH;
        }

        var go = new GameObject("VfxTiled_" + sheet);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = origin;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(lengthU, cellH / NazorSheets.Ppu);
        sr.sortingOrder = sortingOrder;

        var inst = go.AddComponent<NazorVfxInstance>();
        inst.Setup(frames, fps, true, duration);
        return go;
    }

    public static GameObject Column(string sheet, Vector3 baseWorld, float heightU, int sortingOrder = 17, Transform parent = null, float duration = 0f)
    {
        var frames = NazorSheets.Get(sheet);
        if (frames == null || frames.Length == 0) return null;

        float fps = 12f;
        float cellW = frames[0].rect.width;
        if (NazorSheets.TryGetVfxEntry(sheet, out var entry))
        {
            fps = entry.Fps > 0 ? entry.Fps : 12f;
            if (entry.CelulaW > 0) cellW = entry.CelulaW;
        }

        var go = new GameObject("VfxCol_" + sheet);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = baseWorld;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(cellW / NazorSheets.Ppu, heightU);
        sr.sortingOrder = sortingOrder;

        var inst = go.AddComponent<NazorVfxInstance>();
        inst.Setup(frames, fps, true, duration);
        return go;
    }
}

public class NazorVfxInstance : MonoBehaviour
{
    Sprite[] _frames;
    float _fps = 12f;
    bool _loop = false;
    float _duration = 0f;
    float _timer = 0f;
    SpriteRenderer _sr;
    public float FadeSeconds;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(Sprite[] frames, float fps, bool loop, float duration)
    {
        _frames = frames;
        _fps = fps > 0 ? fps : 12f;
        _loop = loop;
        _duration = duration;
        _timer = 0f;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && _frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    public void SwitchSheet(string sheet, bool loop = true, float fps = -1f)
    {
        var newFrames = NazorSheets.Get(sheet);
        if (newFrames == null || newFrames.Length == 0) return;
        _frames = newFrames;
        _timer = 0f;
        _loop = loop;
        if (fps > 0) _fps = fps;
        else if (NazorSheets.TryGetVfxEntry(sheet, out var entry) && entry.Fps > 0) _fps = entry.Fps;
        if (_sr != null)
            _sr.sprite = _frames[0];
    }

    void Update()
    {
        if (_frames == null || _frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        _timer += Time.deltaTime;
        if (_duration > 0f && _timer >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        if (FadeSeconds > 0f && _duration > 0f && _sr != null)
        { var color = _sr.color; color.a = Mathf.Clamp01((_duration - _timer) / FadeSeconds); _sr.color = color; }
        int frameIndex = Mathf.FloorToInt(_timer * _fps);
        if (!_loop && frameIndex >= _frames.Length && _duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (_loop) frameIndex %= _frames.Length;
        else frameIndex = Mathf.Clamp(frameIndex, 0, _frames.Length - 1);

        if (_sr != null)
            _sr.sprite = _frames[frameIndex];
    }
}

