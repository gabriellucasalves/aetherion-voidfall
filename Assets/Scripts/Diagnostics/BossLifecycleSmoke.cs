#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossLifecycleSmoke : MonoBehaviour
{
    readonly List<string> _results = new List<string>();
    int _failures;
    readonly List<string> _runtimeErrors = new List<string>();
    void RecordError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _runtimeErrors.Add(message);
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object o, string method) => o.GetType().GetMethod(method, Private | BindingFlags.Public).Invoke(o, null);
    void Check(bool ok, string message)
    {
        _results.Add((ok ? "PASS " : "FAIL ") + message);
        if (!ok) _failures++;
        Debug.Log("[Boss QA] " + _results.Last());
    }
    static AudioSource Voice => GameAudio.Instance.GetComponentsInChildren<AudioSource>().First(s => s.name == "Effect Boss Voice");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("BossLifecycleQA.Enabled", false)) return;
        SessionState.SetBool("BossLifecycleQA.Enabled", false);
        var root = new GameObject("BossLifecycleQA");
        DontDestroyOnLoad(root);
        root.AddComponent<BossLifecycleSmoke>();
    }
    IEnumerator Start()
    {
        Application.logMessageReceived += RecordError;
        string hero = GameManager.EnsureExists().SelectedHeroId;
        int phase = StageData.ActivePhase;
        // Restore progress after exercising the real victory-save path.
        var fields = new[] { "aetherion.worldmap.v1.completed", "aetherion.worldmap.v1.game_complete", "aetherion.worldmap.v1.node" };
        var values = fields.Select(k => PlayerPrefs.GetInt(k, -1)).ToArray();
        PlayerPrefs.SetInt("aetherion.worldmap.v1.completed", 2);
        var run = Run();
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception e) { Check(false, e.ToString()); break; }
            yield return current;
        }
        for (int i = 0; i < fields.Length; i++)
            if (values[i] < 0) PlayerPrefs.DeleteKey(fields[i]); else PlayerPrefs.SetInt(fields[i], values[i]);
        PlayerPrefs.Save();
        if (!string.IsNullOrEmpty(hero)) GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(hero));
        StageData.ActivePhase = phase;
        Time.timeScale = 1f;
        Application.logMessageReceived -= RecordError;
        Check(_runtimeErrors.Count == 0, "Execução sem erros ou exceções: " + string.Join("; ", _runtimeErrors));
        File.WriteAllLines("/tmp/aetherion-boss-qa/results.txt", _results.Concat(new[] { "FAILURES=" + _failures }));
        EditorApplication.isPlaying = false;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("/tmp/aetherion-boss-qa/" + name + ".png");
        yield return null;
    }
    IEnumerator CheckArticulation(VoidDevourerBoss boss, int form)
    {
        typeof(VoidDevourerBoss).GetField("_stateTimer", Private).SetValue(boss, 99f);
        yield return new WaitForSecondsRealtime(.08f);
        var visual = boss.GetComponent<BossFormVisual>();
        var rig = visual.Rig;
        Check(rig != null && rig.VertexCount > 1000 && rig.Renderer.enabled && boss.GetComponent<SpriteRenderer>().forceRenderingOff,
            "Forma " + form + " renderiza articulação própria sem cartão estático duplicado");
        Check(rig.Renderer.sharedMaterial.mainTexture == boss.GetComponent<SpriteRenderer>().sprite.texture,
            "Forma " + form + " usa a textura original intacta na articulação");
        Vector2 wing = new Vector2(.10f, .80f), tail = new Vector2(.34f, .06f), chest = new Vector2(.5f, .5f);
        Check(Vector3.Distance(rig.EvaluatePoint(wing, .1f, NazorPose.Hover, 1f), rig.EvaluatePoint(wing, .5f, NazorPose.Hover, 1f)) > .06f,
            "Asas da forma " + form + " batem independentemente do corpo");
        Check(Vector3.Distance(rig.EvaluatePoint(tail, .1f, NazorPose.Hover, 1f), rig.EvaluatePoint(tail, .6f, NazorPose.Hover, 1f)) > .03f,
            "Tentáculos/manto da forma " + form + " têm ondulação própria");
        Check(Vector3.Distance(rig.EvaluatePoint(chest, .1f, NazorPose.Hover, 1f), rig.EvaluatePoint(chest, .5f, NazorPose.Hover, 1f)) < .02f,
            "Centro da forma " + form + " fica estável enquanto extremidades se movem");
        Check(Vector3.Distance(rig.EvaluatePoint(wing, .5f, NazorPose.Windup, .5f), rig.EvaluatePoint(wing, .5f, NazorPose.Release, .05f)) > .20f,
            "Forma " + form + " tem poses distintas de preparação e disparo");
        boss.EnemyCtrl.SetFrozen(true);
        float pausedClock = rig.Clock;
        yield return new WaitForSecondsRealtime(.15f);
        Check(Mathf.Approximately(rig.Clock, pausedClock), "Congelamento suspende articulações da forma " + form);
        boss.EnemyCtrl.SetFrozen(false);
        for (int frame = 0; frame < 12; frame++)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/aetherion-boss-qa/motion-form" + form + "-" + frame.ToString("D2") + ".png");
            yield return new WaitForSecondsRealtime(.07f);
        }
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.3f);
        foreach (string key in new[] { "BossIntro", "BossRetry", "BossForm2", "BossForm3", "BossDefeat" })
        {
            var clip = Resources.Load<AudioClip>("Audio/Voices/" + key);
            Check(clip != null && clip.length > 2f, key + " gravação original carregada");
        }
        string japanese = BossDialogue.Intro + BossDialogue.Retry + BossDialogue.Form2 + BossDialogue.Form3 + BossDialogue.Defeat;
        Check(japanese.Where(c => c != '\n').All(c => JapaneseText.Font.HasCharacter(c)), "Fonte inclui todos os caracteres japoneses do boss");
        GameManager.EnsureExists().SelectHero(CharacterCatalog.ById("guerreiro"));
        StageData.ActivePhase = 3;
        SceneTransitionManager.Instance.Load(GameScenes.GroundT3);
        yield return new WaitForSecondsRealtime(.5f);
        var stage = FindFirstObjectByType<GroundT1Controller>();
        var player = FindFirstObjectByType<PlayerController>();
        player.transform.position = new Vector3(39f, stage.Stage.GroundTop + .7f, 0);
        yield return new WaitForSecondsRealtime(.15f);
        var boss = FindFirstObjectByType<VoidDevourerBoss>();
        Check(boss != null && boss.CurrentState == VoidDevourerBoss.State.Intro, "Entrar na arena inicia primeiro encontro");
        Check(Voice.isPlaying && Voice.clip.name == "BossIntro" && Voice.pitch == 1f, "Entrada toca a gravação longa sem mudar tom");
        boss.Cutscene.RequestSkip();
        Check(boss.CurrentSpeechText == BossDialogue.IntroPt && !boss.Cutscene.SkipToEnd, "Primeiro toque revela legenda portuguesa completa sem cortar a voz");
        yield return new WaitForSecondsRealtime(.6f);
        Check(Voice.isPlaying && GameAudio.Instance.GetComponentsInChildren<AudioSource>().First(v => v.clip == GameAudio.Instance.CurrentTrack).volume <= GameAudio.Instance.MusicVolume * .21f,
            "Trilha abaixa enquanto o boss fala");
        GameAudio.Instance.SetPaused(true);
        float voiceTime = Voice.time;
        yield return new WaitForSecondsRealtime(.12f);
        Check(!Voice.isPlaying && Mathf.Abs(Voice.time - voiceTime) < .06f, "Pausa preserva posição da voz do boss");
        GameAudio.Instance.SetPaused(false);
        yield return null;
        Check(Voice.isPlaying, "Retomar continua gravação do boss");
        Check(boss.Cutscene.SpeechText.preferredHeight <= boss.Cutscene.SpeechText.rectTransform.rect.height, "Legenda de entrada cabe no painel ampliada");
        yield return Capture("intro-portuguese");
        yield return new WaitForSecondsRealtime(Voice.clip.length + .9f);
        Check(boss.CurrentState != VoidDevourerBoss.State.Intro && !Voice.isPlaying, "Combate aguarda entrada completa");
        Check(Mathf.Abs(GameAudio.Instance.GetComponentsInChildren<AudioSource>().First(v => v.clip == GameAudio.Instance.CurrentTrack).volume - GameAudio.Instance.MusicVolume) < .01f,
            "Trilha retorna ao volume configurado no combate");
        boss.CancelActiveAttacks(); boss.ForceStateFlying();
        Check(boss.Phase == 1 && boss.Health.Max == 1000f, "Primeira forma tem barra própria de 1000");
        Check(boss.HealthBar.VisibleBars == 3 && BossData.DisplayName == "NAZOR, O DEVORADOR", "Primeira forma exibe nome Nazor e três barras");
        yield return Capture("form-1");
        yield return CheckArticulation(boss, 1);
        int fired = boss.FiredVolleys;
        typeof(VoidDevourerBoss).GetField("_cycleIndex", Private).SetValue(boss, 0);
        Call(boss, "ChooseNextAttack");
        yield return new WaitForSecondsRealtime(.2f);
        Check(boss.AnimationPose == NazorPose.Focus && FindObjectsByType<BossLaser>(FindObjectsSortMode.None).Length == 0,
            "Primeira forma prepara a mira antes de disparar");
        yield return new WaitForSecondsRealtime(2.8f);
        Check(boss.FiredVolleys - fired == 2, "Primeira forma dispara duas salvas com nova mira");
        boss.CancelActiveAttacks(); boss.ForceStateFlying();
        typeof(VoidDevourerBoss).GetField("_cycleIndex", Private).SetValue(boss, 2);
        int dives = boss.DiveLaunches;
        player.GetComponent<HealthSystem>().IsInvulnerable = true;
        Vector3 flightPosition = boss.transform.position;
        Call(boss, "ChooseNextAttack");
        yield return new WaitForSecondsRealtime(.25f);
        Check(boss.AnimationPose == NazorPose.Windup && boss.DiveLaunches == dives, "Investida avisa antes de avançar");
        yield return new WaitForSecondsRealtime(.85f);
        Check(boss.DiveLaunches == dives + 1 && boss.AnimationPose == NazorPose.Dive && Vector3.Distance(flightPosition, boss.transform.position) > 1f,
            "Boss avança fisicamente pela arena com pose de investida");
        yield return new WaitForSecondsRealtime(.65f);
        Check(boss.AnimationPose == NazorPose.Recover && boss.transform.position.y < stage.Stage.GroundTop + 2.2f,
            "Após investida há janela de contra-ataque perto do chão");
        boss.CancelActiveAttacks(); boss.ForceStateFlying();
        typeof(VoidDevourerBoss).GetField("_stateTimer", Private).SetValue(boss, 99f);
        boss.Health.TakeDamage(500f);
        Check(boss.Phase == 1 && boss.Health.Current == 500f, "Forma não troca apenas ao cruzar antigas porcentagens");
        for (int form = 2; form <= 3; form++)
        {
            boss.EnemyCtrl.ReceiveDamage(100000f);
            Check(boss.Phase == form && boss.CurrentState == VoidDevourerBoss.State.Transition && !boss.Health.IsDead,
                "Golpe letal avança somente para forma " + form);
            Check(boss.Health.Max == BossData.FormHealth(form) && boss.Health.Current == boss.Health.Max,
                "Forma " + form + " recupera barra completa maior que anterior");
            Check(boss.Cutscene.SpeechText.text == "" && boss.Cutscene.SpeechText.font == MenuKit.BodyFont,
                "Transição " + form + " usa legenda portuguesa legível");
            Check(Voice.isPlaying && Voice.clip.name == "BossForm" + form, "Transição toca gravação da forma " + form);
            boss.Cutscene.RequestSkip();
            Check(boss.CurrentSpeechText == (form == 2 ? BossDialogue.Form2Pt : BossDialogue.Form3Pt), "Fala exata da forma " + form);
            Check(!player.GetComponent<SpecialController>().Ready && player.GetComponent<PlayerCombat>().IsLocked, "Diálogo bloqueia supremo e ataques");
            float hp = boss.Health.Current;
            boss.EnemyCtrl.ReceiveDamage(100000f); boss.OnNucleusDamaged(100000f); boss.ForwardTentacleDamage(100000f);
            Check(boss.Health.Current == hp && !BossDefeatCutscene.IsPlaying, "Invulnerabilidade na transformação impede pular formas");
            var visual = boss.GetComponent<BossFormVisual>();
            Check(visual.FormSprite != null && visual.FormSprite.name == "NazorForma" + form
                && visual.FormSprite.texture == Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/nazor/form" + form), "Forma " + form + " usa a arte fornecida pelo usuário");
            Check(boss.Cutscene.GetComponentsInChildren<Image>(true).Any(image => image.sprite != null && image.sprite.texture == visual.FormSprite.texture), "Retrato de diálogo corresponde à nova forma " + form);
            Check(boss.HealthBar.RemainingBars == 4 - form && boss.HealthBar.VisibleBars == 4 - form, "HUD remove barra vencida: restam " + (4 - form));
            yield return Capture("transition-" + form);
            yield return new WaitForSecondsRealtime(Voice.clip.length + .9f);
            Check(boss.CurrentState != VoidDevourerBoss.State.Transition && !boss.Health.IsInvulnerable, "Forma " + form + " libera combate após voz completa");
            boss.CancelActiveAttacks(); boss.ForceStateFlying();
            Check(boss.AttackDamageMultiplier > BossData.FormDamage(form - 1) && BossData.FormCadence(form) > BossData.FormCadence(form - 1),
                "Forma " + form + " tem ataques mais fortes e rápidos");
            Check(boss.HealthBar.GetComponentInChildren<Text>().text.Contains("FORMA " + form + "/3"), "HUD identifica a forma " + form);
            yield return Capture("form-" + form);
            yield return CheckArticulation(boss, form);
            Call(boss, "ChooseNextAttack");
            if (form == 2)
            {
                yield return new WaitForSecondsRealtime(.2f);
                Check(boss.IsChargingAttack && FindObjectsByType<NazorHazard>(FindObjectsSortMode.None).Length == 0, "Segunda forma avisa antes da onda sônica");
                yield return new WaitForSecondsRealtime(.85f);
                var wave = FindFirstObjectByType<NazorHazard>();
                Check(wave != null && !wave.IsEclipse, "Segunda forma dispara onda sônica em vez de laser");
                if (wave != null)
                {
                    Vector3 position = wave.transform.position;
                    yield return new WaitForSecondsRealtime(.1f);
                    Check(wave != null && Vector3.Distance(position, wave.transform.position) > .4f, "Onda sônica se desloca pela arena");
                }
                yield return Capture("sonic-wave");
            }
            else
            {
                yield return new WaitForSecondsRealtime(.2f);
                var runes = FindObjectsByType<NazorHazard>(FindObjectsSortMode.None);
                Check(runes.Length == 3 && runes.All(h => h.IsEclipse && h.IsWarning), "Terceira forma marca três posições antes de atacar");
                float before = player.GetComponent<HealthSystem>().Current;
                yield return new WaitForSecondsRealtime(.25f);
                Check(player.GetComponent<HealthSystem>().Current == before, "Marcações do eclipse não causam dano antes do disparo");
                yield return Capture("eclipse-warning");
                yield return new WaitForSecondsRealtime(.65f);
                Check(FindObjectsByType<NazorHazard>(FindObjectsSortMode.None).Any(h => h.IsEclipse && !h.IsWarning), "Após aviso, eclipse dispara projéteis descendentes");
            }
            boss.CancelActiveAttacks(); boss.ForceStateFlying();
            yield return null;
            Check(FindObjectsByType<NazorHazard>(FindObjectsSortMode.None).Length == 0, "Interromper ataque limpa novos perigos da forma " + form);
            if (form == 2)
            {
                var combat = player.GetComponent<PlayerCombat>();
                var shield = combat.Shield;
                var health = player.GetComponent<HealthSystem>();
                var held = typeof(MobileControls).GetField("<BlockHeld>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
                held.SetValue(null, true);
                shield.Refill();
                yield return new WaitForSecondsRealtime(.06f);
                float guardedHealth = health.Current;
                NazorHazard.Sonic(boss, player.transform.position + Vector3.up * .5f, 1f, 20f);
                yield return new WaitForSecondsRealtime(.12f);
                Check(health.Current == guardedHealth && shield.Current < shield.Max, "Escudo absorve onda sônica sem perder vida");
                held.SetValue(null, false);
                shield.Refill();
                boss.CancelActiveAttacks(); boss.ForceStateFlying();
            }
        }
        boss.EnemyCtrl.ReceiveDamage(100000f);
        Check(boss.Phase == 3 && boss.CurrentState == VoidDevourerBoss.State.Defeated && BossDefeatCutscene.IsPlaying, "Somente terceira forma inicia derrota definitiva");
        yield return new WaitForSecondsRealtime(1f);
        Check(Voice.isPlaying && Voice.clip.name == "BossDefeat", "Derrota toca gravação completa correta");
        Check(FindObjectsByType<BossLaser>(FindObjectsSortMode.None).Length == 0 && FindObjectsByType<GroundTentacle>(FindObjectsSortMode.None).Length == 0,
            "Derrota limpa ataques e tentáculos perigosos");
        yield return new WaitForSecondsRealtime(12.3f);
        var defeat = FindFirstObjectByType<BossDefeatCutscene>();
        Check(defeat.DialogueText.text == BossDialogue.DefeatPt, "Derrota apresenta as quatro legendas portuguesas completas");
        Check(defeat.DialogueText.preferredHeight <= defeat.DialogueText.rectTransform.rect.height, "Texto final cabe no painel");
        yield return Capture("defeat-portuguese");
        yield return new WaitForSecondsRealtime(2.5f);
        Check(WorldMapProgress.GameComplete && WorldMapProgress.IsCompleted(3), "Vitória salva conclusão do jogo e da terceira fase");
        Check(!BossDefeatCutscene.IsPlaying && FindFirstObjectByType<VoidDevourerBoss>() == null && EpilogueController.Instance != null, "Derrota conduz ao epílogo após a fala final completa");
        Check(GameObject.Find("CosmicHood") == null && GameObject.Find("TeleportBeam") == null, "Sem quarta forma, teleporte ou gancho espacial");
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.Epilogue, "Epílogo carregou sua cena registrada no build");
        var ending = EpilogueController.Instance;
        Check(ending.Page == 0, "Epílogo começa na primeira tela");
        ending.Advance();
        Check(ending.Page == 0 && ending.Body.text == EpilogueController.Pages[0], "Primeiro toque revela o texto sem pular a página");
        var textures = new HashSet<Texture>();
        for (int page = 0; page < 4; page++)
        {
            ending.JumpTo(page);
            Canvas.ForceUpdateCanvases();
            Check(ending.Heading.text == EpilogueController.Titles[page] && ending.Body.text == EpilogueController.Pages[page], "Tela " + (page + 1) + " contém texto integral aprovado");
            Check(ending.PanelImage.sprite != null && textures.Add(ending.PanelImage.sprite.texture), "Tela " + (page + 1) + " tem ilustração própria");
            Check(ending.Body.preferredHeight <= ending.Body.rectTransform.rect.height && ending.Heading.preferredHeight <= ending.Heading.rectTransform.rect.height, "Título e narrativa cabem na tela " + (page + 1));
            Check(ending.HeroLayer.gameObject.activeSelf == (page == 3), "Heróis nativos aparecem no panorama final: " + (page + 1));
            yield return Capture("epilogue-" + (page + 1));
        }
        ending.Back();
        Check(ending.Page == 2, "Voltar permite reler a tela anterior");
        ending.JumpTo(3);
        ending.Advance();
        yield return new WaitForSecondsRealtime(.4f);
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.Credits, "Créditos carregaram sua cena registrada no build");
        var credits = CreditsController.Instance;
        Check(credits != null, "Última tela leva aos agradecimentos");
        Check(GameAudio.Instance.GetComponentsInChildren<AudioSource>().Any(s => s.isPlaying && s.clip != null && s.clip.name == "Victory"), "Fanfarra celebra os créditos sem interromper voz final");
        Check(credits.CreditTexts.Count == CreditsContent.Blocks.Length && credits.CreditTexts.Select(t => t.text).SequenceEqual(CreditsContent.Blocks.Select(b => b.Text)), "Créditos apresentam todos os blocos na ordem, sem abreviar");
        foreach (string name in new[] { "Bianca Xavier de Oliveira", "Gabriel Lucas Alves da Silva", "Isabela Rosa dos Santos Gontijo", "Matheus Nascimento Cardoso", "Samer Osama Mohammad Taleeb", "Thiago Costa Renovato", "Wesley Thiago Matias Xavier", "Professor Weverson Garcia Medeiros" })
            Check(credits.CreditTexts.Any(t => t.text == name), "Créditos preservam " + name);
        Check(credits.CreditTexts.All(t => t.preferredHeight <= t.rectTransform.rect.height), "Todos os textos dos créditos cabem em suas caixas");
        Check(credits.Scroll.content.rect.height > credits.Scroll.viewport.rect.height, "Créditos têm área de leitura rolável");
        yield return Capture("credits-start");
        float scrollStart = credits.Scroll.content.anchoredPosition.y;
        yield return new WaitForSecondsRealtime(.3f);
        Check(credits.AutoScrolling && credits.Scroll.content.anchoredPosition.y > scrollStart, "Rolagem automática avança os agradecimentos");
        UnityEngine.EventSystems.ExecuteEvents.Execute(credits.Scroll.gameObject,
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        Check(!credits.AutoScrolling, "Arrastar pausa rolagem automática para leitura manual");
        credits.Scroll.verticalNormalizedPosition = .55f;
        yield return Capture("credits-team");
        credits.ScrollToEnd();
        yield return null;
        Check(credits.Scroll.verticalNormalizedPosition < .01f && credits.CreditTexts.Last().text == "FIM", "Leitura chega à mensagem final e ao FIM");
        yield return Capture("credits-end");
        credits.GoToMap();
        yield return new WaitForSecondsRealtime(.3f);
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.WorldMap && WorldMapProgress.GameComplete, "Retorno ao mapa preserva conclusão");
        SceneTransitionManager.Instance.Load(GameScenes.Epilogue);
        yield return new WaitForSecondsRealtime(.3f);
        EpilogueController.Instance.Finish();
        yield return new WaitForSecondsRealtime(.3f);
        Check(CreditsController.Instance != null, "Pular epílogo mantém os agradecimentos disponíveis");
        CreditsController.Instance.GoToMenu();
        yield return new WaitForSecondsRealtime(.3f);
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameScenes.MainMenu, "Créditos retornam ao menu");
        // Recreate the arena before exercising retry; ending scenes destroy the old stage.
        SceneTransitionManager.Instance.Load(GameScenes.GroundT3);
        yield return new WaitForSecondsRealtime(.4f);
        stage = FindFirstObjectByType<GroundT1Controller>();
        Call(stage, "EnterBossArena");
        yield return null;
        FindFirstObjectByType<VoidDevourerBoss>().ForceStateFlying();
        Call(stage, "RetryBoss");
        yield return new WaitForSecondsRealtime(.15f);
        boss = FindFirstObjectByType<VoidDevourerBoss>();
        Check(boss.Phase == 1 && boss.Health.Max == 1000 && Voice.isPlaying && Voice.clip.name == "BossRetry", "Tentar de novo reinicia primeira forma e fala curta");
        boss.Cutscene.RequestSkip();
        Check(boss.CurrentSpeechText == BossDialogue.RetryPt, "Nova tentativa exibe legenda curta em português");
        boss.Cutscene.RequestSkip();
        yield return new WaitForSecondsRealtime(.6f);
        Check(boss.CurrentState == VoidDevourerBoss.State.Flying && !Voice.isPlaying, "Pular entrada encerra voz sem deixá-la no combate");
    }
}
#endif
