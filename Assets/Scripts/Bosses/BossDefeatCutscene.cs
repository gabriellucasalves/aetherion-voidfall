using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BossDefeatCutscene : MonoBehaviour
{
    static Sprite _hoodSprite;
    static Sprite _clawSprite;
    static Sprite _teleportBeamSprite;

    public static void EnsureSprites()
    {
        if (_hoodSprite != null) return;
        _hoodSprite = LoadSprite("cosmic_hood", 128, 128, new Vector2(0.5f, 0.5f));
        _clawSprite = LoadSprite("cosmic_claw", 192, 192, new Vector2(0.5f, 0.5f));
        _teleportBeamSprite = LoadSprite("teleport_beam", 64, 192, new Vector2(0.5f, 0f));
    }

    static Sprite LoadSprite(string name, int w, int h, Vector2 pivot)
    {
        var tex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/" + name);
        if (tex == null) return UiKit.WhiteSprite();
        tex.filterMode = FilterMode.Point;
        int sw = Mathf.Min(w > 0 ? w : tex.width, tex.width);
        int sh = Mathf.Min(h > 0 ? h : tex.height, tex.height);
        return Sprite.Create(tex, new Rect(0, 0, sw, sh), pivot, 32f);
    }

    public static void StartCutscene(VoidDevourerBoss boss, PlayerController player, GroundT1Controller stageController)
    {
        EnsureSprites();
        var go = new GameObject("BossDefeatCutscene");
        var cutscene = go.AddComponent<BossDefeatCutscene>();
        cutscene._boss = boss;
        cutscene._player = player;
        cutscene._stage = stageController;
        cutscene.StartCoroutine(cutscene.Sequence());
    }

    VoidDevourerBoss _boss;
    PlayerController _player;
    GroundT1Controller _stage;

    IEnumerator Sequence()
    {
        Time.timeScale = 1f;
        SpecialController.AbortActive();
        CinematicTime.End();

        // 0.0s: Lock player, abort all hazards, hide boss bar
        if (_player != null)
        {
            _player.SetLocked(true);
            var combat = _player.GetComponent<PlayerCombat>();
            if (combat != null) combat.SetLocked(true);
            var health = _player.GetComponent<HealthSystem>();
            if (health != null) health.IsInvulnerable = true;
        }

        if (_boss != null && _boss.HealthBar != null)
        {
            _boss.HealthBar.gameObject.SetActive(false);
        }

        // Dissolve any active minions
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (enemy != null && enemy.gameObject != _boss.gameObject)
            {
                PixelBurst.Spawn(enemy.transform.position + Vector3.up * 0.5f, new Color(0.65f, 0.25f, 0.85f), 10);
                Destroy(enemy.gameObject);
            }
        }

        // Dissolve tentacles
        foreach (var tentacle in FindObjectsByType<GroundTentacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tentacle != null)
            {
                PixelBurst.Spawn(tentacle.transform.position + Vector3.up * 1f, new Color(0.65f, 0.25f, 0.85f), 8);
                Destroy(tentacle.gameObject);
            }
        }

        // Flash and hitstop
        SupremeFx.PlayFlash();
        yield return new WaitForSecondsRealtime(0.25f);

        // 0.25s .. 1.6s: Boss convulses violently, eyes burst one by one
        float convulseTime = 1.35f;
        for (float t = 0f; t < convulseTime; t += Time.unscaledDeltaTime)
        {
            if (_boss != null)
            {
                _boss.transform.position += (Vector3)(Random.insideUnitCircle * 0.12f);
                if (Random.value < 0.28f)
                {
                    Vector3 burstPos = _boss.transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
                    PixelBurst.Spawn(burstPos, new Color(1f, 0.2f, 0.25f), 6);
                    PixelBurst.Spawn(burstPos, new Color(0.72f, 0.22f, 0.95f), 5);
                }
            }
            yield return null;
        }

        // 1.6s .. 2.4s: Collapse to arena floor
        float collapseY = _stage != null ? _stage.Stage.GroundTop + 1.2f : -0.8f;
        float startY = _boss != null ? _boss.transform.position.y : 3.5f;
        for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
        {
            if (_boss != null)
            {
                float y = Mathf.Lerp(startY, collapseY, t / 0.8f);
                _boss.transform.position = new Vector3(_boss.transform.position.x, y, 0f);
            }
            yield return null;
        }

        // 2.4s .. 3.0s: Darken screen overlay
        var canvas = UiKit.CreateCanvas(transform, "CutsceneCanvas");
        var darkOverlay = UiKit.Panel(canvas.transform, "DarkOverlay", new Vector2(2400f, 1400f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
        darkOverlay.raycastTarget = false;

        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.72f, t / 0.6f));
            yield return null;
        }

        // 3.0s .. 7.5s: Typewriter dialogue box
        var dialogueBox = UiKit.Panel(canvas.transform, "DialogueBox", new Vector2(1000f, 160f), new Vector2(0f, -320f), new Color(0.06f, 0.04f, 0.12f, 0.95f));
        var trim = UiKit.Panel(dialogueBox.transform, "Trim", new Vector2(1000f, 2f), new Vector2(0f, 79f), new Color(0.85f, 0.25f, 0.75f, 0.9f));
        trim.raycastTarget = false;

        var speakerLabel = UiKit.Label(dialogueBox.transform, "DEVORADOR DO VAZIO", 18, new Vector2(-400f, 48f), new Color(1f, 0.45f, 0.65f), new Vector2(400f, 30f));
        speakerLabel.alignment = TextAnchor.MiddleLeft;

        string fullSentence = "Pensaram que tinham me destruído… mas o apocalipse está começando.";
        var dialogueText = UiKit.Label(dialogueBox.transform, "", 22, new Vector2(0f, -12f), MenuTheme.SoftIvory, new Vector2(920f, 80f));
        dialogueText.alignment = TextAnchor.UpperLeft;

        float typeDuration = 3.5f;
        int len = fullSentence.Length;
        for (float t = 0f; t < typeDuration; t += Time.unscaledDeltaTime)
        {
            int chars = Mathf.Clamp(Mathf.FloorToInt((t / typeDuration) * len), 0, len);
            dialogueText.text = fullSentence.Substring(0, chars);
            yield return null;
        }
        dialogueText.text = fullSentence;

        // Hold dialogue until 7.5s
        yield return new WaitForSecondsRealtime(1.0f);

        // 7.5s .. 10.0s: Transformation — Boss body dissolves, Cosmic Hood + Claws appear!
        dialogueBox.gameObject.SetActive(false);

        if (_boss != null)
        {
            PixelBurst.Spawn(_boss.transform.position, new Color(0.95f, 0.25f, 0.85f), 18);
            PixelBurst.Spawn(_boss.transform.position + Vector3.up * 1f, new Color(0.55f, 0.15f, 0.75f), 15);
            Destroy(_boss.gameObject);
        }

        SupremeFx.PlayFlash();

        // Cosmic Hood at center of screen
        var hoodGo = new GameObject("CosmicHood");
        hoodGo.transform.SetParent(canvas.transform, false);
        var hoodImg = hoodGo.AddComponent<Image>();
        hoodImg.sprite = _hoodSprite;
        hoodImg.raycastTarget = false;
        var hoodRt = hoodGo.GetComponent<RectTransform>();
        hoodRt.anchoredPosition = new Vector2(0f, 40f);
        hoodRt.sizeDelta = new Vector2(256f, 256f);

        // Two Cosmic Claws at screen edges
        var leftClawGo = new GameObject("CosmicClawLeft");
        leftClawGo.transform.SetParent(canvas.transform, false);
        var leftClawImg = leftClawGo.AddComponent<Image>();
        leftClawImg.sprite = _clawSprite;
        leftClawImg.raycastTarget = false;
        var leftClawRt = leftClawGo.GetComponent<RectTransform>();
        leftClawRt.anchoredPosition = new Vector2(-680f, -40f);
        leftClawRt.sizeDelta = new Vector2(384f, 384f);

        var rightClawGo = new GameObject("CosmicClawRight");
        rightClawGo.transform.SetParent(canvas.transform, false);
        var rightClawImg = rightClawGo.AddComponent<Image>();
        rightClawImg.sprite = _clawSprite;
        rightClawImg.raycastTarget = false;
        var rightClawRt = rightClawGo.GetComponent<RectTransform>();
        rightClawRt.anchoredPosition = new Vector2(680f, -40f);
        rightClawRt.sizeDelta = new Vector2(384f, 384f);
        rightClawRt.localScale = new Vector3(-1f, 1f, 1f); // Mirror

        // Claws pulse and creep inward
        for (float t = 0f; t < 2.5f; t += Time.unscaledDeltaTime)
        {
            float pulse = 1f + 0.05f * Mathf.Sin(t * 12f);
            hoodRt.localScale = new Vector3(pulse, pulse, 1f);
            float clawX = Mathf.Lerp(680f, 580f, t / 2.5f);
            leftClawRt.anchoredPosition = new Vector2(-clawX, -40f);
            rightClawRt.anchoredPosition = new Vector2(clawX, -40f);
            yield return null;
        }

        // 10.0s .. 11.5s: Aetherion breaks apart
        hoodGo.SetActive(false);
        leftClawGo.SetActive(false);
        rightClawGo.SetActive(false);

        // Spawn floating rubble
        if (_stage != null)
        {
            for (int i = 0; i < 8; i++)
            {
                PixelBurst.Spawn(new Vector3(40f + i * 2.5f, _stage.Stage.GroundTop + 0.5f, 0f),
                    new Color(0.72f, 0.45f, 0.85f), 6);
            }
        }
        yield return new WaitForSecondsRealtime(1.5f);

        // 11.5s .. 13.0s: Teleport beam engulfs hero
        Vector3 heroPos = _player != null ? _player.transform.position : new Vector3(48f, 0f, 0f);
        var beamGo = new GameObject("TeleportBeam");
        beamGo.transform.position = heroPos + Vector3.down * 0.5f;
        var beamSr = beamGo.AddComponent<SpriteRenderer>();
        beamSr.sprite = _teleportBeamSprite;
        beamSr.sortingOrder = 25;
        beamSr.color = new Color(0.35f, 0.95f, 1f, 0.95f);
        beamGo.transform.localScale = new Vector3(2.5f, 8f, 1f);

        // Pull hero upward
        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
        {
            if (_player != null)
            {
                _player.transform.position += Vector3.up * 4.5f * Time.unscaledDeltaTime;
            }
            yield return null;
        }

        // 13.0s .. 14.0s: Fade to complete black
        for (float t = 0f; t < 1.0f; t += Time.unscaledDeltaTime)
        {
            darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.72f, 1f, t / 1.0f));
            yield return null;
        }
        darkOverlay.color = Color.black;

        // Progress Save
        WorldMapProgress.Complete(3);
        WorldMapProgress.ConfirmFinalBossDefeated();

        // Check if SpaceLevel exists and can be loaded
        if (Application.CanStreamedLevelBeLoaded(GameScenes.SpaceLevel))
        {
            SceneTransitionManager.Instance.Load(GameScenes.SpaceLevel);
            yield break;
        }

        // Show "FIM DA PARTE 1" victory panel
        ShowEndDemoPanel(canvas.transform);
    }

    void ShowEndDemoPanel(Transform canvas)
    {
        MenuPanels.EndPart1(canvas,
            () => SceneTransitionManager.Instance.Load(GameScenes.WorldMap),
            () => SceneTransitionManager.Instance.Load(GameScenes.MainMenu));
    }
}

