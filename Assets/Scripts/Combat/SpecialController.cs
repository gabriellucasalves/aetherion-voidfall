using System.Collections;
using UnityEngine;

// Especial Supremo (tecla E / botão SUPREMO). Não substitui o especial de L/Q.
public class SpecialController : MonoBehaviour
{
    public static bool IsCinematic { get; private set; }

    static SpecialController _instance;

    SpecialData _data;
    CharacterData _hero;
    PlayerController _player;
    PlayerCombat _combat;
    HealthSystem _health;
    ResonanceMeter _meter;
    float _cooldownLeft;
    bool _finished = true;
    bool _completed;

    public float CooldownLeft => Mathf.Max(0f, _cooldownLeft);
    public SpecialData Data => _data;
    public ResonanceMeter Meter => _meter;

    public bool Ready =>
        !IsCinematic
        && _cooldownLeft <= 0f
        && _meter != null
        && _meter.IsFull
        && (_health == null || !_health.IsDead);

    public static void AbortActive()
    {
        if (_instance != null)
            _instance.Abort();
    }

    public void Setup(CharacterData hero)
    {
        _hero = hero;
        _data = SpecialData.ForHero(hero);
        _player = GetComponent<PlayerController>();
        _combat = GetComponent<PlayerCombat>();
        _health = GetComponent<HealthSystem>();
        _meter = GetComponent<ResonanceMeter>();
        if (_meter == null)
            _meter = gameObject.AddComponent<ResonanceMeter>();
        _meter.Configure(hero);
    }

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        if (!_finished)
        {
            StopAllCoroutines();
            Finish();
        }
    }

    public void Abort()
    {
        if (_finished)
            return;
        StopAllCoroutines();
        Finish();
    }

    void Update()
    {
        if (_cooldownLeft > 0f)
            _cooldownLeft -= Time.deltaTime;

        // Pausa (timeScale 0) e o próprio cut-in não podem disparar nem guardar o toque.
        if (IsCinematic || Time.timeScale <= 0f)
        {
            if (MobileControls.IsVisible)
                MobileControls.ConsumeSupremeDown();
            return;
        }

        if (!Ready)
            return;

        if (WantsSupreme() && _meter.TrySpend())
            StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        _finished = false;
        _completed = false;
        IsCinematic = true;

        try
        {
            if (_player != null)
                _player.SetLocked(true);
            if (_combat != null)
                _combat.SetLocked(true);
            if (_health != null)
                _health.IsInvulnerable = true;

            SupremeFx.PlayFlash();

            float hitStop = _data != null ? _data.HitStop : 0.08f;
            float slow = _data != null ? _data.WorldSlowScale : 0.05f;
            CinematicTime.Begin(0f);
            yield return new WaitForSecondsRealtime(hitStop);

            if (_data != null)
                SupremeFx.BeginTint(_data.Tint);
            if (_hero != null && _hero.Id == "arqueiro")
                SupremeFx.BeginSkyStars();

            CinematicTime.Set(slow);
            if (_data != null)
                yield return CutInPanel.Play(_data, _hero);

            const float ease = 0.15f;
            for (float t = 0f; t < ease; t += Time.unscaledDeltaTime)
            {
                CinematicTime.Set(Mathf.Lerp(slow, 1f, t / ease));
                yield return null;
            }

            CinematicTime.End();
            SupremeFx.ClearAtmosphere();

            // Mago age durante o tempo parado. Guerreiro fica enraizado.
            // Arqueiro é solto dentro do HunterStars para o salto.
            if (_hero != null && _hero.Id == "mago")
            {
                if (_player != null)
                    _player.SetLocked(false);
                if (_combat != null)
                    _combat.SetLocked(false);
            }

            if (_hero != null && _hero.Id == "mago")
                yield return VoidTimeStop.Run(transform, _hero);
            else if (_hero != null && _hero.Id == "arqueiro")
                yield return HunterStars.Run(transform, _hero, _player, _combat);
            else
                yield return WarriorBarrage.Run(transform, _hero, _player);

            _completed = _health == null || !_health.IsDead;
        }
        finally
        {
            Finish();
        }
    }

    void Finish()
    {
        if (_finished)
            return;
        _finished = true;

        CinematicTime.End();
        CutInPanel.Cancel();
        SupremeFx.ClearAtmosphere();
        VoidTimeStop.ReleaseAll();
        SupremeFx.ClearMarks();

        bool dead = _health != null && _health.IsDead;
        if (_health != null)
            _health.IsInvulnerable = false;

        if (!dead)
        {
            if (_player != null)
                _player.SetLocked(false);
            if (_combat != null)
                _combat.SetLocked(false);
            if (_completed && _hero != null && _hero.Id == "arqueiro" && _player != null)
                _player.GrantIFrames(1f);
        }

        if (_data != null)
            _cooldownLeft = _data.MinCooldown;
        IsCinematic = false;
        _completed = false;
    }

    static bool WantsSupreme()
    {
        if (MobileControls.IsVisible && MobileControls.ConsumeSupremeDown())
            return true;
        return Input.GetKeyDown(KeyCode.E);
    }
}
