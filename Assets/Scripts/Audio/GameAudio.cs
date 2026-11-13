using System;
using UnityEngine;

// Scene-owned audio: leaving a menu or stage destroys its voices and music together.
// Music and effects never depend on timeScale; only an explicit pause suspends them.
public sealed class GameAudio : MonoBehaviour
{
    public enum Cue
    {
        Sword, Bow, Magic, WarriorSpecial, ArcherSpecial, MageSpecial,
        SupremeWarrior, SupremeArcher, SupremeMage, Claws, Bone, Slam,
        Charge, Spit, Ember, Hit, Hurt, Block, BossWarning, BossBeam,
        BossRoar, Defeat, Victory,
        BowDraw, KnightCharge, KnightSlam, EmberDive, Tentacle,
        SelectWarrior, SelectMage, SelectArcher,
        WarriorCrescent, WarriorFinal, ArcherStar, MageTimeBreak,
        WarriorHurt, MageHurt, ArcherHurt, SkeletonHit, BeastHit, GolemHit,
        SentinelHit, EmberHit, KnightHit, SpitterHit, BossHit,
        WarriorRelease, MageRelease, ArcherRelease, MageNova, ArcherNova,
        ProloguePeace, PrologueOmen, PrologueRupture, PrologueVoid, PrologueMemory, PrologueResolve,
        WarriorCallout, MageCallout, ArcherCallout,
        WarriorImpactCallout, MageImpactCallout, ArcherImpactCallout,
        WarriorChannelLoop, MageChannelLoop, ArcherChannelLoop, MageGuard, ArcherGuard,
        BossSonicCharge, BossSonicWave, BossVoidMark, BossVoidImpact
    }

    public static GameAudio Instance { get; private set; }
    const int VoiceCount = 16;
    public const float SupremeMusicScale = .20f;

    // User recordings live separately from generated effects, so rebuilding SFX
    // cannot overwrite the full performance supplied for each hero.
    public static string CuePath(Cue cue)
    {
        bool recording = cue == Cue.WarriorCallout || cue == Cue.MageCallout || cue == Cue.ArcherCallout;
        return (recording ? "Audio/Voices/" : "Audio/Sfx/") + cue;
    }
    AudioSource _accent;
    AudioSource _callout;
    AudioSource _bossVoice;
    float _bossVoiceGain;
    AudioSource _channel;
    float _channelGain;
    float _accentGain;
    float _calloutGain;
    bool _supremeActive;
    public static string PrologueTrack(int page)
    {
        string[] themes = { "calm", "omen", "destruction", "void", "memory", "resolve" };
        return "Audio/Ambience/prologue_" + themes[Mathf.Clamp(page, 0, themes.Length - 1)];
    }
    const float FadeSeconds = 1.6f;
    readonly AudioSource[] _music = new AudioSource[2];
    readonly AudioSource[] _voices = new AudioSource[VoiceCount];
    readonly float[] _voiceGains = new float[VoiceCount];
    readonly AudioClip[] _clips = new AudioClip[Enum.GetValues(typeof(Cue)).Length];
    readonly float[] _lastPlayed = new float[Enum.GetValues(typeof(Cue)).Length];
    readonly float[] _fadeFrom = new float[2];
    int _activeMusic;
    float _fade = 1f;
    bool _stopping;
    bool _paused;
    bool _ended;
    bool _duck;
    float _duckGain = 1f;
    float _musicVolume;
    float _effectsVolume;

    public bool IsPaused => _paused;
    public AudioClip CurrentTrack => _music[_activeMusic].clip;
    public float MusicVolume => _musicVolume;
    public float EffectsVolume => _effectsVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Audio.Music", 0.65f));
        _effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Audio.Effects", 0.8f));
        for (int i = 0; i < 2; i++)
        {
            _music[i] = NewSource("Music " + i);
            _music[i].loop = true;
            _music[i].priority = 32;
        }
        for (int i = 0; i < VoiceCount; i++)
            _voices[i] = NewSource("Effect " + i);
        _accent = NewSource("Effect Cinematic");
        _accent.priority = 16;
        _callout = NewSource("Effect Callout");
        _callout.priority = 8;
        _bossVoice = NewSource("Effect Boss Voice");
        _bossVoice.priority = 4;
        _channel = NewSource("Effect Channel");
        _channel.priority = 24;
        for (int i = 0; i < _clips.Length; i++)
        {
            _clips[i] = Resources.Load<AudioClip>(CuePath((Cue)i));
            _lastPlayed[i] = float.NegativeInfinity;
            if (_clips[i] == null) Debug.LogWarning("Missing audio cue: " + (Cue)i);
        }
    }

    AudioSource NewSource(string label)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static string StageTrack(int phase)
    {
        return "Audio/Music/stage_" + Mathf.Clamp(phase, 1, 3);
    }

    public void PlayMenu()
    {
        _ended = false;
        _duck = false;
        SetPaused(false);
        PlayMusic("Audio/Music/menu_infernal_loop");
    }

    public void PlayPrologue(int page)
    {
        _ended = false;
        _duck = false;
        SetPaused(false);
        PlayMusic(PrologueTrack(page));
        var cues = new[] { Cue.ProloguePeace, Cue.PrologueOmen, Cue.PrologueRupture,
            Cue.PrologueVoid, Cue.PrologueMemory, Cue.PrologueResolve };
        PlayAccent(cues[Mathf.Clamp(page, 0, cues.Length - 1)], .75f);
    }

    public void PlayEpilogue(int page)
    {
        _ended = false;
        _duck = false;
        SetPaused(false);
        string theme = page == 0 ? "calm" : page == 1 ? "memory" : "resolve";
        PlayMusic("Audio/Ambience/prologue_" + theme);
        PlayAccent(page == 0 ? Cue.ProloguePeace : page == 1 ? Cue.PrologueMemory : Cue.PrologueResolve, .35f);
    }

    public void PlayCredits()
    {
        PlayEpilogue(3);
        PlayAccent(Cue.Victory, .55f);
    }

    void PlayAccent(Cue cue, float gain)
    {
        _accent.Stop();
        _accent.clip = _clips[(int)cue];
        _accent.pitch = 1f;
        _accentGain = gain;
        _accent.volume = gain * _effectsVolume;
        _accent.Play();
    }

    public void BeginSupreme(string heroId)
    {
        _supremeActive = true;
        _callout.Stop();
        _channel.Stop();
        _channel.clip = _clips[(int)(heroId == "mago" ? Cue.MageChannelLoop
            : heroId == "arqueiro" ? Cue.ArcherChannelLoop : Cue.WarriorChannelLoop)];
        _channel.loop = true;
        _channel.pitch = 1f;
        _channelGain = .18f;
        _channel.volume = _channelGain * _effectsVolume;
        _channel.Play();
        // Make room immediately for the flash and channeling; never change pitch with timeScale.
        _duckGain = SupremeMusicScale;
        PlayAccent(heroId == "mago" ? Cue.SupremeMage : heroId == "arqueiro" ? Cue.SupremeArcher : Cue.SupremeWarrior, .85f);
    }

    public float PlaySupremeCallout(string heroId)
    {
        if (!_supremeActive) return 0f;
        _callout.Stop();
        _callout.clip = _clips[(int)(heroId == "mago" ? Cue.MageCallout
            : heroId == "arqueiro" ? Cue.ArcherCallout : Cue.WarriorCallout)];
        _callout.pitch = 1f;
        _calloutGain = .90f;
        // The spoken attack name takes the foreground during the portrait beat.
        _accentGain = .32f;
        _accent.volume = _accentGain * _effectsVolume;
        _callout.volume = _calloutGain * _effectsVolume;
        _callout.Play();
        return _callout.clip != null ? _callout.clip.length : 0f;
    }

    public void ReleaseSupreme(string heroId)
    {
        _channel.Stop();
        _channel.loop = false;
        PlayAccent(heroId == "mago" ? Cue.MageRelease : heroId == "arqueiro" ? Cue.ArcherRelease : Cue.WarriorRelease, .95f);
    }

    public void EndSupreme(bool interrupted)
    {
        _supremeActive = false;
        _accent.Stop();
        _channel.Stop();
        _channel.loop = false;
        // Preserve the complete supplied recording, including its final syllable.
        // Abort/death/scene exit still cancel the voice immediately.
        if (!interrupted) return;
        _callout.Stop();
        foreach (var voice in _voices)
        {
            if (voice.clip == null) continue;
            string name = voice.clip.name;
            if (name == "WarriorCrescent" || name == "WarriorFinal" || name == "ArcherStar"
                || name == "MageTimeBreak" || name == "MageNova" || name == "ArcherNova") voice.Stop();
        }
    }

    public void PlayStage(int phase)
    {
        _ended = false;
        _duck = false;
        SetPaused(false);
        PlayMusic(StageTrack(phase));
    }

    public float PlayBossVoice(string recording)
    {
        _bossVoice.Stop();
        _bossVoice.clip = Resources.Load<AudioClip>("Audio/Voices/" + recording);
        _bossVoice.pitch = 1f;
        _bossVoiceGain = .95f;
        _bossVoice.volume = _bossVoiceGain * _effectsVolume;
        if (_bossVoice.clip == null) { Debug.LogError("Missing boss recording: " + recording); return 0f; }
        _bossVoice.Play();
        return _bossVoice.clip.length;
    }

    public void StopBossVoice() { _bossVoice.Stop(); }

    public void BeginBoss()
    {
        _ended = false;
        _duck = true; // Intro dialogue remains clear; released at combat start.
        SetPaused(false);
        // A retry restarts the encounter cue even if the same track was fading out.
        PlayMusic("Audio/Music/boss_placeholder", true);
        Play(Cue.BossRoar, transform.position, 0.85f, true);
    }

    public void BossCombatStarted() { _duck = false; StopBossVoice(); }

    void PlayMusic(string path, bool restart = false)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogError("Missing soundtrack: " + path);
            return;
        }
        if (!restart && !_stopping && CurrentTrack == clip) return;
        // Continue from current gains even if a second transition interrupts a fade.
        for (int i = 0; i < 2; i++) _fadeFrom[i] = _music[i].volume;
        _activeMusic = 1 - _activeMusic;
        var next = _music[_activeMusic];
        next.Stop();
        next.clip = clip;
        next.volume = 0f;
        _fadeFrom[_activeMusic] = 0f;
        next.pitch = 1f;
        next.Play();
        _stopping = false;
        _fade = 0f;
    }

    void Update()
    {
        if (_paused) return;
        _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / FadeSeconds);
        float targetDuck = (_supremeActive || _callout.isPlaying || _bossVoice.isPlaying) ? SupremeMusicScale : _duck ? .40f : 1f;
        _duckGain = Mathf.MoveTowards(_duckGain, targetDuck, Time.unscaledDeltaTime * 2.5f);
        for (int i = 0; i < 2; i++)
        {
            float target = !_stopping && i == _activeMusic ? _musicVolume * _duckGain : 0f;
            _music[i].volume = Mathf.Lerp(_fadeFrom[i], target, Mathf.SmoothStep(0f, 1f, _fade));
            if (_fade >= 1f && (i != _activeMusic || _stopping) && _music[i].isPlaying)
                _music[i].Stop();
        }
    }

    public void SetPaused(bool paused)
    {
        if (_paused == paused) return;
        _paused = paused;
        foreach (var source in _music)
            if (paused) source.Pause(); else source.UnPause();
        foreach (var source in _voices)
            if (paused) source.Pause(); else source.UnPause();
        if (paused) { _accent.Pause(); _callout.Pause(); _channel.Pause(); _bossVoice.Pause(); }
        else { _accent.UnPause(); _callout.UnPause(); _channel.UnPause(); _bossVoice.UnPause(); }
    }

    public void Finish(bool victory)
    {
        _ended = true;
        StopBossVoice();
        SetPaused(false);
        foreach (var voice in _voices) voice.Stop();
        _accent.Stop();
        _callout.Stop();
        _channel.Stop();
        _channel.loop = false;
        _supremeActive = false;
        for (int i = 0; i < 2; i++) _fadeFrom[i] = _music[i].volume;
        _fade = 0f;
        _stopping = true;
        Play(victory ? Cue.Victory : Cue.Defeat, transform.position, 0.9f, true);
    }

    public static void SaveVolumes(float music, float effects)
    {
        music = Mathf.Clamp01(music);
        effects = Mathf.Clamp01(effects);
        if (Instance != null)
        {
            Instance._musicVolume = music;
            Instance._effectsVolume = effects;
            Instance._accent.volume = Instance._accentGain * effects;
            Instance._callout.volume = Instance._calloutGain * effects;
            Instance._bossVoice.volume = Instance._bossVoiceGain * effects;
            Instance._channel.volume = Instance._channelGain * effects;
            for (int i = 0; i < VoiceCount; i++)
                Instance._voices[i].volume = Instance._voiceGains[i] * effects;
        }
        PlayerPrefs.SetFloat("Audio.Music", music);
        PlayerPrefs.SetFloat("Audio.Effects", effects);
        PlayerPrefs.Save();
    }

    // One reserved voice belongs to the player's bow. Release/cancel cannot stop
    // a recycled enemy voice, and pausing/muting still uses the common controls.
    public static void BeginBowDraw()
    {
        var audio = Instance;
        if (audio == null || audio._paused || audio._ended) return;
        int slot = VoiceCount - 1;
        var voice = audio._voices[slot];
        voice.Stop();
        voice.clip = audio._clips[(int)Cue.BowDraw];
        voice.pitch = 1f;
        voice.panStereo = 0f;
        audio._voiceGains[slot] = .5f;
        voice.volume = .5f * audio._effectsVolume;
        voice.Play();
    }

    public static void StopBowDraw()
    {
        if (Instance != null) Instance._voices[VoiceCount - 1].Stop();
    }

    public static void SelectHero(string id)
    {
        var cue = id == "mago" ? Cue.SelectMage : id == "arqueiro" ? Cue.SelectArcher : Cue.SelectWarrior;
        Emit(cue, Camera.main != null ? Camera.main.transform.position : Vector3.zero, .85f);
    }

    public static void EmitHit(HealthSystem target)
    {
        var player = target.GetComponent<PlayerController>();
        Cue cue;
        if (player != null)
        {
            string id = player.Hero != null ? player.Hero.Id : "guerreiro";
            cue = id == "mago" ? Cue.MageHurt : id == "arqueiro" ? Cue.ArcherHurt : Cue.WarriorHurt;
        }
        else
        {
            var enemy = target.GetComponent<EnemyController>();
            string id = enemy != null && enemy.Data != null ? enemy.Data.Id : "";
            switch (id)
            {
                case "esqueleto_alien": cue = Cue.SkeletonHit; break;
                case "bicho": cue = Cue.BeastHit; break;
                case "golem_ardosia": cue = Cue.GolemHit; break;
                case "sentinela_escombro": cue = Cue.SentinelHit; break;
                case "arauto_brasa": cue = Cue.EmberHit; break;
                case "cavaleiro_corrompido": cue = Cue.KnightHit; break;
                case "cuspidor_vazio": cue = Cue.SpitterHit; break;
                default: cue = target.GetComponent<VoidDevourerBoss>() != null ? Cue.BossHit : Cue.Hit; break;
            }
        }
        // Hit feedback sits below the weapon/creature sound instead of masking it.
        Emit(cue, target.transform.position, player != null ? .65f : .35f);
    }

    public static void Emit(Cue cue, Vector3 position, float gain = 1f)
    {
        if (Instance != null) Instance.Play(cue, position, gain, false);
    }

    void Play(Cue cue, Vector3 position, float gain, bool force)
    {
        if (!force && (_paused || _ended)) return;
        int index = (int)cue;
        if (_supremeActive && (cue == Cue.Bone || cue == Cue.Claws || cue == Cue.Charge
            || cue == Cue.Slam || cue == Cue.KnightCharge || cue == Cue.KnightSlam || cue == Cue.Spit
            || cue == Cue.Ember || cue == Cue.EmberDive || cue == Cue.Hit)) gain *= .35f;
        if (_clips[index] == null || (!force && Time.unscaledTime - _lastPlayed[index] < 0.055f)) return;
        _lastPlayed[index] = Time.unscaledTime;
        // Screen-relative attenuation prevents off-screen enemies overwhelming combat.
        float attenuation = 1f;
        float pan = 0f;
        if (!force && Camera.main != null)
        {
            float dx = position.x - Camera.main.transform.position.x;
            attenuation = Mathf.Clamp01(1f - Mathf.Max(0f, Mathf.Abs(dx) - 7f) / 18f);
            pan = Mathf.Clamp(dx / 18f, -0.55f, 0.55f);
        }
        if (attenuation <= 0f) return;
        int voiceIndex = -1;
        for (int i = 0; i < VoiceCount - 1; i++)
            if (!_voices[i].isPlaying) { voiceIndex = i; break; }
        if (voiceIndex < 0) return; // Bounded polyphony, never allocate per impact.
        AudioSource voice = _voices[voiceIndex];
        voice.clip = _clips[index];
        voice.panStereo = pan;
        voice.pitch = force ? 1f : UnityEngine.Random.Range(0.95f, 1.05f);
        _voiceGains[voiceIndex] = Mathf.Clamp01(gain * attenuation * 0.65f);
        voice.volume = _voiceGains[voiceIndex] * _effectsVolume;
        voice.Play();
    }
}
