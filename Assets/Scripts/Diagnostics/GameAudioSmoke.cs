#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameAudioSmoke : MonoBehaviour
{
    readonly List<string> _results = new List<string>();
    int _failures;
    float _savedMusic, _savedEffects;
    string _hero;
    int _activePhase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("GameAudioQA.Enabled", false)) return;
        SessionState.SetBool("GameAudioQA.Enabled", false);
        var root = new GameObject("GameAudioSmoke");
        DontDestroyOnLoad(root);
        root.AddComponent<GameAudioSmoke>();
    }

    void Check(bool result, string message)
    {
        _results.Add((result ? "PASS " : "FAIL ") + message);
        if (!result) _failures++;
        Debug.Log("[Audio QA] " + _results.Last());
    }

    static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);
    }

    static AudioSource[] Effects(GameAudio audio)
    {
        return audio.GetComponentsInChildren<AudioSource>().Where(s => s.name.StartsWith("Effect ")).ToArray();
    }

    IEnumerator Start()
    {
        _savedMusic = PlayerPrefs.GetFloat("Audio.Music", .65f);
        _savedEffects = PlayerPrefs.GetFloat("Audio.Effects", .8f);
        _hero = GameManager.EnsureExists().SelectedHeroId;
        _activePhase = StageData.ActivePhase;
        var run = Run();
        while (true)
        {
            object current;
            try
            {
                if (!run.MoveNext()) break;
                current = run.Current;
            }
            catch (Exception e)
            {
                Check(false, e.ToString());
                break;
            }
            yield return current;
        }
        typeof(MobileControls).GetField("<BlockHeld>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        GameAudio.SaveVolumes(_savedMusic, _savedEffects);
        StageData.ActivePhase = _activePhase;
        if (!string.IsNullOrEmpty(_hero)) GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(_hero));
        Time.timeScale = 1f;
        File.WriteAllLines("/tmp/aetherion-audio-qa/result.txt", _results.Concat(new[] { "FAILURES=" + _failures }));
        Debug.Log("[Audio QA] Finished, failures=" + _failures);
        EditorApplication.isPlaying = false;
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.5f);
        foreach (GameAudio.Cue cue in Enum.GetValues(typeof(GameAudio.Cue)))
        {
            var clip = Resources.Load<AudioClip>(GameAudio.CuePath(cue));
            Check(clip != null && clip.length > .05f && clip.samples > 0, "SFX " + cue + " carregado");
        }
        Check(JapaneseText.Font != null, "Fonte japonesa está incluída no jogo");
        foreach (string id in new[] { "guerreiro", "mago", "arqueiro" })
        {
            var data = SpecialData.ForHero(CharacterCatalog.ById(id));
            string expectedName = id == "guerreiro" ? SpecialJapanese.WarriorName : id == "mago" ? SpecialJapanese.MageName : SpecialJapanese.ArcherName;
            string expectedChannel = id == "guerreiro" ? SpecialJapanese.WarriorChannel : id == "mago" ? SpecialJapanese.MageChannel : SpecialJapanese.ArcherChannel;
            string expectedImpact = id == "guerreiro" ? SpecialJapanese.WarriorImpact : id == "mago" ? SpecialJapanese.MageImpact : SpecialJapanese.ArcherImpact;
            Check(data.DisplayName == expectedName && data.Callout == expectedChannel && data.FinalShout == expectedImpact,
                "Nome, canalização e impacto usam o texto japonês de " + id);
            Check((data.DisplayName + data.Callout + data.FinalShout).Where(c => c != '\n').All(c => JapaneseText.Font.HasCharacter(c)),
                "Fonte contém todos os caracteres de " + id);
            Check(HeroSkillInfo.For(id).Skills[2].Name == data.DisplayName, "Seleção exibe nome japonês de " + id);
            Destroy(data);
        }
        SceneTransitionManager.Instance.Load(GameScenes.Intro);
        yield return new WaitForSecondsRealtime(.5f);
        var intro = FindFirstObjectByType<IntroController>();
        Check(GameAudio.Instance != null && GameAudio.Instance.CurrentTrack.name == "prologue_calm", "Prólogo começa calmo");
        var accent = Effects(GameAudio.Instance).First(v => v.name == "Effect Cinematic");
        float accentTime = accent.time;
        intro.Advance();
        Check(accent.time >= accentTime, "Completar a digitação não reinicia efeito do painel");
        var storyCues = new[] { "ProloguePeace", "PrologueOmen", "PrologueRupture", "PrologueVoid", "PrologueMemory", "PrologueResolve" };
        for (int page = 0; page < IntroController.Pages.Length; page++)
        {
            intro.JumpTo(page);
            yield return new WaitForSecondsRealtime(.12f);
            Check(GameAudio.Instance.CurrentTrack.name == GameAudio.PrologueTrack(page).Split('/').Last(), "Ambiência acompanha painel " + (page + 1));
            Check(accent.isPlaying && accent.clip.name == storyCues[page], "Efeito narrativo exclusivo " + storyCues[page]);
        }
        Call(intro, "GoToSelect");
        yield return new WaitForSecondsRealtime(.5f);
        Check(GameAudio.Instance != null && GameAudio.Instance.CurrentTrack == null, "Pular prólogo encerra ambiências e explosões");
        SceneTransitionManager.Instance.Load(GameScenes.CharacterSelection);
        yield return new WaitForSecondsRealtime(.5f);
        var selection = FindFirstObjectByType<CharacterSelectController>();
        var selectionAudio = GameAudio.Instance;
        Check(selectionAudio != null && selectionAudio.CurrentTrack == null,
            "Seleção tem efeitos próprios sem iniciar música");
        selection.Select((CharacterData)null);
        foreach (string hero in new[] { "guerreiro", "mago", "arqueiro" })
        {
            selection.Select(CharacterCatalog.ById(hero));
            string cue = hero == "guerreiro" ? "SelectWarrior" : hero == "mago" ? "SelectMage" : "SelectArcher";
            Check(Effects(selectionAudio).Any(v => v.isPlaying && v.clip.name == cue), "Selecionar " + hero + " toca " + cue);
            yield return new WaitForSecondsRealtime(.1f);
        }
        for (int phase = 1; phase <= 3; phase++)
        {
            StageData.ActivePhase = phase;
            GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(phase == 1 ? "guerreiro" : phase == 2 ? "mago" : "arqueiro"));
            SceneTransitionManager.Instance.Load(phase == 1 ? GameScenes.GroundT1 : phase == 2 ? GameScenes.GroundT2 : GameScenes.GroundT3);
            yield return new WaitForSecondsRealtime(.5f);
            var audio = GameAudio.Instance;
            Check(audio != null && FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length == 1, "Fase " + phase + " possui um único sistema de áudio");
            Check(audio.CurrentTrack != null && audio.CurrentTrack.name == "stage_" + phase, "Fase " + phase + " toca a faixa correta");
            Check(audio.GetComponentsInChildren<AudioSource>().Count(s => s.loop) == 2, "Música em loop e duas vozes para transição");
            var player = FindFirstObjectByType<PlayerController>();
            var health = player.GetComponent<HealthSystem>();
            health.IsInvulnerable = true;
            FindFirstObjectByType<SimpleEnemySpawner>()?.SetLocked(true);
            var combat = player.GetComponent<PlayerCombat>();
            if (phase == 1)
            {
                var blockInput = typeof(MobileControls).GetField("<BlockHeld>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
                blockInput.SetValue(null, true);
                yield return new WaitForSecondsRealtime(.25f);
                var sprite = GameObject.Find("GuerreiroPixel").GetComponent<SpriteRenderer>();
                Check(combat.IsBlocking && sprite.sprite.texture == Resources.Load<Texture2D>("TrioAnimations/guerreiro/ataque"),
                    "Escudo mantém sprite do guerreiro atual");
                var pivot = sprite.sprite.pivot;
                yield return new WaitForSecondsRealtime(.25f);
                Check(sprite.sprite.texture != Resources.Load<Texture2D>("Guerreiro/sheet") && sprite.sprite.pivot == pivot,
                    "Todos os quadros de bloqueio preservam modelo e alinhamento atuais");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot("/tmp/aetherion-audio-qa/warrior-current-block.png");
                yield return null;
                blockInput.SetValue(null, false);
                yield return new WaitForSecondsRealtime(.1f);
                Check(!combat.IsBlocking && sprite.sprite.texture == Resources.Load<Texture2D>("TrioAnimations/guerreiro/idle"),
                    "Soltar escudo volta ao idle atual");
            }
            if (phase == 1) Call(combat, "FireWarrior");
            else if (phase == 2) Call(combat, "FireMage");
            else
            {
                combat.BeginArcherCharge();
                Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == "BowDraw"), "Arqueiro toca puxada antes do disparo");
                combat.CancelArcherCharge();
                Check(!Effects(audio).Any(v => v.isPlaying && v.clip.name == "BowDraw"), "Cancelar carga interrompe puxada sem disparar");
                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(.85f);
                combat.ReleaseArcherCharge();
                Check(!Effects(audio).Any(v => v.isPlaying && v.clip.name == "BowDraw"), "Soltar o arco encerra a puxada");
            }
            Check(Effects(audio).Any(s => s.isPlaying && s.clip.name == (phase == 1 ? "Sword" : phase == 2 ? "Magic" : "Bow")), "Ataque do herói dispara SFX na fase " + phase);
            yield return new WaitForSecondsRealtime(.1f);
            if (phase == 1) combat.TryFireWarriorSpecial();
            else Call(combat, phase == 2 ? "FireMageSpecial" : "FireArcherSpecial");
            Check(Effects(audio).Any(s => s.isPlaying && s.clip.name == (phase == 1 ? "WarriorSpecial" : phase == 2 ? "MageSpecial" : "ArcherSpecial")), "Especial dispara SFX na fase " + phase);
            var supreme = player.GetComponent<SpecialController>();
            var stageForTarget = FindFirstObjectByType<GroundT1Controller>();
            var targetData = EnemyData.CreateSlateGolem();
            var supremeTarget = SimpleEnemySpawner.SpawnAt(targetData, player.transform, stageForTarget.Stage,
                player.transform.position + Vector3.right * 3f, player.GetComponent<Collider2D>());
            supremeTarget.ExternalBrain = true;
            supremeTarget.Health.Configure(20000f);
            supremeTarget.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            supreme.StartCoroutine((IEnumerator)Call(supreme, "Run"));
            string supremeCue = phase == 1 ? "SupremeWarrior" : phase == 2 ? "SupremeMage" : "SupremeArcher";
            Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == supremeCue), "Supremo exclusivo dispara " + supremeCue);
            var heard = new HashSet<string>();
            bool checkedJapaneseLayout = false;
            bool checkedRecordedVoice = false;
            float quietestMusic = float.MaxValue;
            float deadline = Time.unscaledTime + 18f;
            while (SpecialController.IsCinematic && Time.unscaledTime < deadline)
            {
                foreach (var voice in Effects(audio))
                    if (voice.isPlaying && voice.clip != null) heard.Add(voice.clip.name);
                var musicSource = audio.GetComponentsInChildren<AudioSource>().First(v => v.clip == audio.CurrentTrack);
                quietestMusic = Mathf.Min(quietestMusic, musicSource.volume);
                var recordedVoice = Effects(audio).First(v => v.name == "Effect Callout");
                if (!checkedRecordedVoice && recordedVoice.isPlaying)
                {
                    string voicePath = "Audio/Voices/" + (phase == 1 ? "WarriorCallout" : phase == 2 ? "MageCallout" : "ArcherCallout");
                    Check(recordedVoice.clip == Resources.Load<AudioClip>(voicePath) && recordedVoice.clip.length > 5f && recordedVoice.pitch == 1f,
                        "Supremo usa gravação completa fornecida sem alterar tom na fase " + phase);
                    audio.SetPaused(true);
                    float pausedAt = recordedVoice.time;
                    yield return new WaitForSecondsRealtime(.12f);
                    Check(!recordedVoice.isPlaying && Mathf.Abs(recordedVoice.time - pausedAt) < .06f,
                        "Pausa suspende a nova gravação na fase " + phase);
                    audio.SetPaused(false);
                    yield return null;
                    Check(recordedVoice.isPlaying, "Retomar continua a voz do supremo na fase " + phase);
                    checkedRecordedVoice = true;
                }
                if (!checkedJapaneseLayout && CutInPanel.Current != null)
                {
                    var texts = CutInPanel.Current.GetComponentsInChildren<Text>(true);
                    var chant = texts.FirstOrDefault(t => t.text == supreme.Data.Callout);
                    if (chant != null && chant.gameObject.activeInHierarchy)
                    {
                        Check(chant.font == JapaneseText.Font && chant.preferredHeight <= chant.rectTransform.rect.height + 1f
                            && chant.preferredWidth <= chant.rectTransform.rect.width + 1f,
                            "Canalização japonesa cabe no painel de " + player.Hero.Id);
                        checkedJapaneseLayout = true;
                    }
                }
                yield return null;
            }
            // The archer emits its final burst on the same frame the coroutine finishes.
            foreach (var voice in Effects(audio))
                if (voice.isPlaying && voice.clip != null) heard.Add(voice.clip.name);
            Check(!SpecialController.IsCinematic, "Supremo completo encerra normalmente na fase " + phase);
            string callout = phase == 1 ? "WarriorCallout" : phase == 2 ? "MageCallout" : "ArcherCallout";
            string release = phase == 1 ? "WarriorRelease" : phase == 2 ? "MageRelease" : "ArcherRelease";
            string finale = phase == 1 ? "WarriorFinal" : phase == 2 ? "MageNova" : "ArcherNova";
            Check(heard.Contains(callout), "Fala japonesa sincronizada dispara " + callout);
            string impactVoice = phase == 1 ? "WarriorImpactCallout" : phase == 2 ? "MageImpactCallout" : "ArcherImpactCallout";
            Check(!heard.Contains(impactVoice), "Gravação fornecida não é cortada nem sobreposta pela voz antiga na fase " + phase);
            Check(checkedRecordedVoice && quietestMusic <= audio.MusicVolume * (GameAudio.SupremeMusicScale + .01f),
                "Trilha abaixa automaticamente durante supremo na fase " + phase);
            yield return new WaitForSecondsRealtime(.5f);
            var restoredMusic = audio.GetComponentsInChildren<AudioSource>().First(v => v.clip == audio.CurrentTrack);
            Check(Mathf.Abs(restoredMusic.volume - audio.MusicVolume) < .01f,
                "Trilha retorna ao volume configurado depois do supremo na fase " + phase);
            Check(checkedJapaneseLayout, "Painel japonês foi apresentado para " + player.Hero.Id);
            Check(heard.Contains(release), "Canalização libera poder com " + release);
            Check(heard.Contains(finale), "Final do supremo tem impacto " + finale);
            Destroy(supremeTarget.gameObject);
            Destroy(targetData);
            supreme.StartCoroutine((IEnumerator)Call(supreme, "Run"));
            yield return new WaitForSecondsRealtime(.75f);
            supreme.Abort();
            Check(!Effects(audio).Any(v => v.isPlaying && (v.clip.name == callout || v.clip.name == supremeCue || v.clip.name == release)),
                "Interromper supremo encerra fala e canalização");
            health.IsInvulnerable = false;
            health.TakeDamage(1f);
            string hurtCue = phase == 1 ? "WarriorHurt" : phase == 2 ? "MageHurt" : "ArcherHurt";
            Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == hurtCue), "Dano recebido usa material do herói " + hurtCue);
            health.IsInvulnerable = true;
            yield return new WaitForSecondsRealtime(2f);
            var stage = FindFirstObjectByType<GroundT1Controller>();
            stage.RequestPause();
            Check(audio.IsPaused && !audio.GetComponentsInChildren<AudioSource>().Any(s => s.isPlaying), "Pausa suspende música e efeitos");
            var sliders = stage.GetComponentsInChildren<Slider>(true);
            Check(sliders.Length >= 2, "Pausa tem controles independentes de volume");
            if (phase == 1)
            {
                ScreenCapture.CaptureScreenshot("/tmp/aetherion-audio-qa/pause.png");
                yield return null;
                sliders[0].value = .25f;
                sliders[1].value = 0f;
                Check(Mathf.Approximately(audio.MusicVolume, .25f) && Mathf.Approximately(audio.EffectsVolume, 0f), "Volumes salvam e aplicam independentemente");
                GameAudio.SaveVolumes(_savedMusic, _savedEffects);
            }
            Call(stage, "TogglePause");
            Check(!audio.IsPaused && audio.GetComponentsInChildren<AudioSource>().Any(s => s.loop && s.isPlaying), "Continuar retoma a faixa");
            if (phase == 1)
            {
                foreach (var data in new[] { EnemyData.CreateAlienSkeleton(), EnemyData.CreateBeast(),
                    EnemyData.CreateSlateGolem(), EnemyData.CreateRubbleSentinel(), EnemyData.CreateEmberHerald(),
                    EnemyData.CreateCorruptedKnight(), EnemyData.CreateVoidSpitter() })
                {
                    foreach (var voice in Effects(audio)) voice.Stop();
                    var enemy = SimpleEnemySpawner.SpawnAt(data, player.transform, stage.Stage,
                        player.transform.position + Vector3.right * 4f, player.GetComponent<Collider2D>());
                    enemy.ExternalBrain = true;
                    string cue;
                    if (data.Id == "cavaleiro_corrompido")
                    {
                        enemy.GetComponent<CavaleiroCorrompido>().ForceCharge();
                        Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == "KnightCharge"), "Investida do cavaleiro toca som");
                        enemy.GetComponent<CavaleiroCorrompido>().ForceSlam();
                        cue = "KnightSlam";
                    }
                    else if (data.Id == "cuspidor_vazio")
                    {
                        enemy.GetComponent<CuspidorDoVazio>().ForceSpit();
                        yield return new WaitForSecondsRealtime(.5f); // Swell windup precedes the projectile release.
                        cue = "Spit";
                    }
                    else
                    {
                        Call(enemy, "Strike");
                        cue = data.AttackKind == "bone" ? "Bone" : data.AttackKind == "claws" ? "Claws"
                            : data.AttackKind == "charge" ? "Charge" : data.AttackKind == "burrow_slam" ? "Slam" : "EmberDive";
                    }
                    Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == cue), "Ataque de " + data.DisplayName + " toca " + cue);
                    if (data.AttackKind == "ember_flyer")
                    {
                        Call(enemy, "Strike");
                        Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == "Ember"), "Arauto distingue bola de brasa do mergulho");
                    }
                    enemy.Health.TakeDamage(1f);
                    string hitCue = data.Id == "esqueleto_alien" ? "SkeletonHit" : data.Id == "bicho" ? "BeastHit"
                        : data.Id == "golem_ardosia" ? "GolemHit" : data.Id == "sentinela_escombro" ? "SentinelHit"
                        : data.Id == "arauto_brasa" ? "EmberHit" : data.Id == "cavaleiro_corrompido" ? "KnightHit" : "SpitterHit";
                    Check(Effects(audio).Any(v => v.isPlaying && v.clip.name == hitCue), "Impacto exclusivo de " + data.DisplayName);
                    Destroy(enemy.gameObject);
                    Destroy(data);
                    yield return new WaitForSecondsRealtime(.1f);
                }
                GameAudio.Emit(GameAudio.Cue.SupremeWarrior, player.transform.position);
                GameAudio.SaveVolumes(_savedMusic, 0f);
                Check(Effects(audio).All(v => v.volume == 0f), "Silenciar efeitos também silencia vozes em execução");
                GameAudio.SaveVolumes(_savedMusic, _savedEffects);
            }
            if (phase == 3)
            {
                Call(stage, "EnterBossArena");
                yield return new WaitForSecondsRealtime(.1f);
                Check(audio.CurrentTrack.name == "boss_placeholder", "Arena troca trilha da fase pela trilha exclusiva do boss");
                Check(Effects(audio).Any(s => s.isPlaying && s.clip.name == "BossRoar"), "Entrada do boss dispara rugido");
                yield return new WaitForSecondsRealtime(1.8f);
                Check(audio.GetComponentsInChildren<AudioSource>().Count(s => s.loop && s.isPlaying) == 1, "Transição encerra a faixa anterior sem sobreposição permanente");
                Call(stage, "OnPlayerDied", health);
                Check(Effects(audio).Any(s => s.isPlaying && s.clip.name == "Defeat"), "Derrota toca mesmo com timeScale zero");
                yield return new WaitForSecondsRealtime(1.8f);
                Check(!audio.GetComponentsInChildren<AudioSource>().Any(s => s.loop && s.isPlaying), "Derrota encerra trilha de combate");
                Call(stage, "RetryBoss");
                yield return new WaitForSecondsRealtime(.1f);
                Check(!audio.IsPaused && audio.CurrentTrack.name == "boss_placeholder" && audio.GetComponentsInChildren<AudioSource>().Any(s => s.loop && s.isPlaying), "Tentar novamente reinicia a música do boss");
                var boss = FindFirstObjectByType<VoidDevourerBoss>();
                Call(boss, "OnBossDied", health);
                Check(!Effects(audio).Any(s => s.isPlaying && s.clip.name == "Victory"), "Primeira forma viva não dispara vitória antecipada");
            }
        }
        SceneTransitionManager.Instance.Load(GameScenes.MainMenu);
        yield return new WaitForSecondsRealtime(.5f);
        var menuAudio = GameAudio.Instance;
        Check(menuAudio != null && menuAudio.CurrentTrack != null && menuAudio.CurrentTrack.name == "menu_infernal_loop",
            "Voltar ao menu toca Infernal Loop");
        Check(FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length == 1 && !Effects(menuAudio).Any(s => s.isPlaying),
            "Voltar ao menu elimina música e efeitos da fase sem duplicar áudio");
        yield return new WaitForSecondsRealtime(1.8f);
        Check(menuAudio.GetComponentsInChildren<AudioSource>().Count(s => s.loop && s.isPlaying) == 1,
            "Infernal Loop toca em loop na tela inicial");
        GameAudio.SaveVolumes(0f, _savedEffects);
        yield return null;
        Check(menuAudio.GetComponentsInChildren<AudioSource>().Where(s => s.loop).All(s => s.volume == 0f),
            "Volume de música também controla a trilha do menu");
        GameAudio.SaveVolumes(_savedMusic, _savedEffects);
        var settings = FindFirstObjectByType<MainMenuController>().SettingsPanel;
        settings.SetActive(true);
        yield return null;
        Check(settings.GetComponentsInChildren<Slider>().Length == 2, "Configurações do menu oferecem música e efeitos");
        ScreenCapture.CaptureScreenshot("/tmp/aetherion-audio-qa/settings.png");
        yield return null;
    }
}
#endif
