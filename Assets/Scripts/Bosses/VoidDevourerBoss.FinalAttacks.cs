using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Continuation of the existing boss: same health, voices, intro and defeat path.
public partial class VoidDevourerBoss
{
    readonly List<GameObject> _transientVfx = new List<GameObject>();
    GameObject _enrageAura, _riftTint;
    readonly List<float> _riftSafeCenters = new List<float>();
    public IReadOnlyList<float> RiftSafeCenters => _riftSafeCenters;
    public float RiftSafeWidth { get; private set; }
    public bool RiftActive { get; private set; }
    public float RiftWallX { get; private set; }
    public Vector3 LastDiveTarget { get; private set; }
    public static readonly int[][] CollapseMasks = {
        new[] {1,0,0,0,1,1,0,0,1,1},
        new[] {1,1,0,0,0,1,1,1,0,0},
        new[] {1,0,0,1,1,1,0,0,0,1}
    };
    public static readonly int[][] CollapseShardLanes = { new int[0], new[] {4,8}, new[] {2,6} };
    public static float CollapseLaneX(int lane) => 37.5f + 2.1f * (lane + .5f);
    public float Telegraph(float duration) => _isEnraged && duration > .75f ? Mathf.Max(.6f, duration / 1.25f) : duration;
    Vector3 VfxPoint(Vector2 offset) => transform.TransformPoint(offset);
    GameObject TrackVfx(GameObject go) { if (go != null) _transientVfx.Add(go); return go; }
    void ClearTransientVfx()
    {
        foreach (var go in _transientVfx) if (go != null) Destroy(go);
        _transientVfx.Clear();
        if (_riftTint != null) { Destroy(_riftTint); _riftTint = null; }
    }
    public float SurfaceAt(float x)
    {
        float floor = _stage != null ? _stage.GroundTop : -2f;
        float feet = _target != null ? _target.position.y - .6f : floor;
        foreach (var platform in T3RuinsFrontLevel.Platforms)
            if (x >= platform.Left && x <= platform.Right && platform.Top <= feet + .6f)
                floor = Mathf.Max(floor, platform.Top);
        return floor;
    }
    void ReactToDamage(HealthSystem health)
    {
        if (_state == State.Defeated || _state == State.Transition || _state == State.Intro) return;
        _flashTimer = 2f / 12f;
        Vector2 impact = _target != null ? (Vector2)(_target.position + Vector3.up * .5f) : (Vector2)transform.position;
        if (_bodyHurtbox != null) impact = _bodyHurtbox.ClosestPoint(impact);
        NazorVfx.Play("faisca_acerto_32x32_4f", impact, sortingOrder: 23);
    }
    IEnumerator AttackPalaceCollapse()
    {
        if (_state != State.Attacking) yield break;
        SetAnimationPose(NazorPose.Conjure);
        float ground = _stage != null ? _stage.GroundTop : -2f;
        for (int wave = 0; wave < 3; wave++)
        {
            if (_state != State.Attacking) yield break;
            float warning = 1.1f - wave * .1f;
            CollapseWaves++;
            var root = TrackVfx(new GameObject("NazorDesabamentoOnda" + (wave + 1)));
            for (int lane = 0; lane < 10; lane++)
            {
                if (CollapseMasks[wave][lane] == 0) continue;
                float x = CollapseLaneX(lane);
                NazorFallingDebris.Spawn(this, root.transform, x, SurfaceAt(x), ground + 10f, warning, false);
            }
            // Lane numbers in the specification are zero-based: one shard per gap.
            foreach (int lane in CollapseShardLanes[wave])
            {
                float x = CollapseLaneX(lane);
                NazorFallingDebris.Spawn(this, root.transform, x, SurfaceAt(x), ground + 10f, .8f, true);
            }
            GameAudio.Emit(GameAudio.Cue.BossVoidMark, transform.position);
            yield return new WaitForSeconds(warning + .45f);
            if (_state != State.Attacking) yield break;
            NazorVfx.Juice("desabamento");
            GameAudio.Emit(GameAudio.Cue.KnightSlam, transform.position, .9f);
            if (wave < 2) yield return new WaitForSeconds(.35f);
        }
        FinishAttack(.8f);
    }
    IEnumerator AttackGlareDiveCombo()
    {
        if (_state != State.Attacking) yield break;
        ComboLaunches++;
        yield return AttackDoubleGlareCore(1, .75f, false);
        if (_state != State.Attacking) yield break;
        yield return new WaitForSeconds(.35f);
        // Dive samples the NEW hero position, after the beam and its dodge.
        yield return AttackDiveCore(false);
        if (_state == State.Attacking) FinishAttack(.8f);
    }
    public static float ClawPosition(int index, float first, float retarget)
    {
        float sign = first < 48f ? 1f : -1f;
        if (index == 0) return Mathf.Clamp(first, 37.5f, 58.5f);
        if (index == 3) return Mathf.Clamp(retarget, 37.5f, 58.5f);
        float x = first + (index == 1 ? 3.8f : -3.8f) * sign;
        if (x < 37.5f || x > 58.5f) x = first + 7.6f * sign;
        return Mathf.Clamp(x, 37.5f, 58.5f);
    }
    IEnumerator AttackVoidClaws()
    {
        if (_state != State.Attacking) yield break;
        SetAnimationPose(NazorPose.Conjure);
        float first = Mathf.Clamp(_target != null ? _target.position.x : 48f, 37.5f, 58.5f);
        float interval = _isEnraged ? .44f : .55f;
        for (int i = 0; i < 4; i++)
        {
            if (_state != State.Attacking) yield break;
            float current = _target != null ? _target.position.x : first;
            float x = ClawPosition(i, first, current);
            var root = TrackVfx(new GameObject("NazorGarra" + (i + 1)));
            NazorClawStrike.Spawn(this, root, new Vector3(x, SurfaceAt(x), 0));
            GameAudio.Emit(GameAudio.Cue.BossWarning, transform.position, .9f);
            if (i < 3) yield return new WaitForSeconds(interval);
        }
        // Every claw keeps its own .9 s warning, even during enrage.
        yield return new WaitForSeconds(.9f + .25f + .4f);
        FinishAttack(.6f);
    }
    public void NotifyClawStrike() { ClawStrikes++; }
    public static float[] PlanRift(float heroX, bool enraged)
    {
        heroX = Mathf.Clamp(heroX, 37.5f, 58.5f);
        float first = Mathf.Clamp(heroX + (heroX < 48f ? 4.2f : -4.2f), 39f, 57f);
        var safe = new List<float> { first };
        if (!enraged)
            foreach (float candidate in new[] {39f, 57f})
                if (Mathf.Abs(candidate - heroX) >= 8f && Mathf.Abs(candidate - first) >= 6f)
                { safe.Add(candidate); break; }
        return safe.ToArray();
    }
    public bool InsideRiftSafe(float x)
    {
        foreach (float center in _riftSafeCenters) if (Mathf.Abs(x - center) <= RiftSafeWidth * .5f) return true;
        return false;
    }
    IEnumerator AttackStellarRift()
    {
        if (_state != State.Attacking) yield break;
        SetAnimationPose(NazorPose.Roar);
        NazorVfx.Mark("fenda", false);
        TrackVfx(NazorVfx.Play("fenda_abertura_96x96_8f", transform.position, scale: 2f, duration: 1f, sortingOrder: 13));
        SupremeFx.BeginTint(new Color(8f/255f, 6f/255f, 40f/255f, .46f));
        _riftTint = GameObject.Find("TintaSupremo");
        GameAudio.Emit(GameAudio.Cue.BossRoar, transform.position, .9f);
        GameAudio.Emit(GameAudio.Cue.BossSonicCharge, transform.position);
        yield return new WaitForSeconds(.2f);
        if (_state != State.Attacking) yield break;
        _riftSafeCenters.Clear();
        _riftSafeCenters.AddRange(PlanRift(_target != null ? _target.position.x : 48f, _isEnraged));
        RiftSafeWidth = _isEnraged ? 3.4f : 3f;
        float ground = _stage != null ? _stage.GroundTop : -2f;
        float height = Camera.main != null ? Camera.main.orthographicSize * 2f + 2f : 16f;
        float bottom = Camera.main != null ? Mathf.Min(ground, Camera.main.transform.position.y - Camera.main.orthographicSize - 1f) : ground;
        foreach (float x in _riftSafeCenters)
        {
            var safe = TrackVfx(NazorVfx.Column("fenda_ponto_seguro_64x144_6f", new Vector3(x, bottom, 0), height));
            if (safe != null) { var sr = safe.GetComponent<SpriteRenderer>(); sr.size = new Vector2(RiftSafeWidth, height); }
        }
        GameAudio.Emit(GameAudio.Cue.BossVoidMark, transform.position);
        yield return new WaitForSeconds(Telegraph(1.6f) - .2f);
        for (int sweep = 0; sweep < 2; sweep++)
        {
            if (_state != State.Attacking) yield break;
            float start = sweep == 0 ? 37.5f : 58.5f, end = sweep == 0 ? 58.5f : 37.5f;
            var wall = TrackVfx(NazorVfx.Column("fenda_varredura_48x224_6f", new Vector3(start, bottom, 0), height, 20));
            if (wall != null) wall.GetComponent<SpriteRenderer>().flipX = sweep == 1;
            RiftSweeps++;
            RiftActive = true;
            NazorVfx.Mark("fenda", true);
            NazorVfx.Juice("fenda");
            GameAudio.Emit(GameAudio.Cue.BossSonicWave, transform.position);
            bool hit = false;
            float previous = start;
            for (float time = 0f; time < .9f; time += Time.deltaTime)
            {
                if (_state != State.Attacking) yield break;
                RiftWallX = Mathf.Lerp(start, end, time / .9f);
                if (wall != null) wall.transform.position = new Vector3(RiftWallX, bottom, 0);
                float hero = _target != null ? _target.position.x : -100f;
                bool crossing = hero >= Mathf.Min(previous, RiftWallX) - .6f && hero <= Mathf.Max(previous, RiftWallX) + .6f;
                if (!hit && crossing && !InsideRiftSafe(hero))
                { hit = true; NazorVfx.DamagePlayer(_target, new Vector3(RiftWallX, ground, 0), 30f * AttackDamageMultiplier, 70); }
                previous = RiftWallX;
                yield return null;
            }
            // Include the final segment even on a long mobile frame.
            float finalHero = _target != null ? _target.position.x : -100f;
            if (!hit && finalHero >= Mathf.Min(previous,end)-.6f && finalHero <= Mathf.Max(previous,end)+.6f && !InsideRiftSafe(finalHero))
                NazorVfx.DamagePlayer(_target, new Vector3(end,ground,0), 30f * AttackDamageMultiplier,70);
            RiftActive = false;
            if (wall != null) Destroy(wall);
            if (sweep == 0) yield return new WaitForSeconds(.5f);
        }
        GameAudio.Emit(GameAudio.Cue.BossVoidImpact, transform.position, .7f);
        yield return new WaitForSeconds(.6f);
        ClearTransientVfx();
        FinishAttack(1.2f);
    }
    IEnumerator EnterEnrage()
    {
        if (_isEnraged || _state != State.Flying || _phase != 3) yield break;
        _isEnraged = true;
        EnrageEntries++;
        _enrageCadence = 1.25f;
        _state = State.Warning;
        SetAnimationPose(NazorPose.Roar);
        _enrageAura = NazorVfx.Play("aura_enrage_192x224_8f", transform.position, loop: true, sortingOrder: _sr.sortingOrder - 1, parent: transform);
        var aura = _enrageAura != null ? _enrageAura.GetComponent<SpriteRenderer>() : null;
        if (aura != null) aura.color = new Color(1,1,1,0);
        NazorVfx.Juice("enrage");
        HitStop.Do(120);
        GameAudio.Emit(GameAudio.Cue.BossRoar, transform.position);
        for (float time = 0f; time < .9f; time += Time.deltaTime)
        {
            if (aura != null) aura.color = new Color(1,1,1,.9f * Mathf.Clamp01(time / .6f));
            if (time >= .15f && !_enrageSecondFlash)
            {
                _enrageSecondFlash = true;
                SupremeFx.PlayFlash(new Color32(130,225,255,255), .30f, .12f);
                GameAudio.Emit(GameAudio.Cue.BossSonicWave, transform.position, .8f);
            }
            yield return null;
        }
        if (aura != null) aura.color = new Color(1,1,1,.9f);
        FinishAttack(.6f);
    }
    bool _enrageSecondFlash;
}
