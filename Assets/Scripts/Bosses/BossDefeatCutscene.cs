using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Final defeat of form three. No fourth form, teleport or space-level branch.
public sealed class BossDefeatCutscene : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    public static void EnsureSprites() { VoidDevourerBoss.EnsureSprites(); }
    VoidDevourerBoss _boss;
    PlayerController _player;
    public Text DialogueText { get; private set; }

    public static void StartCutscene(VoidDevourerBoss boss, PlayerController player, GroundT1Controller stage)
    {
        if (IsPlaying) return;
        var go = new GameObject("BossDefeatCutscene");
        var scene = go.AddComponent<BossDefeatCutscene>();
        scene._boss = boss;
        scene._player = player;
        IsPlaying = true;
        scene.StartCoroutine(scene.Sequence());
    }

    void OnDestroy() { IsPlaying = false; }

    IEnumerator Sequence()
    {
        Time.timeScale = 1f;
        SpecialController.AbortActive();
        CinematicTime.End();
        if (_player != null)
        {
            _player.SetLocked(true);
            _player.GetComponent<PlayerCombat>()?.SetLocked(true);
            var health = _player.GetComponent<HealthSystem>();
            if (health != null) health.IsInvulnerable = true;
            var body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        _boss.CancelActiveAttacks();
        if (_boss.HealthBar != null) _boss.HealthBar.gameObject.SetActive(false);
        foreach (var laser in FindObjectsByType<BossLaser>(FindObjectsSortMode.None)) Destroy(laser.gameObject);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (enemy.gameObject != _boss.gameObject) Destroy(enemy.gameObject);
        SupremeFx.PlayFlash();
        CameraFollow.Shake(.35f, .18f);

        // Collapse the original body; it stays defeated during the entire final line.
        Vector3 start = _boss.transform.position;
        float ground = FindFirstObjectByType<GroundT1Controller>()?.Stage.GroundTop ?? -2f;
        for (float t = 0; t < .8f; t += Time.unscaledDeltaTime)
        {
            _boss.transform.position = Vector3.Lerp(start, new Vector3(start.x, ground + 2.6f, 0f), t / .8f);
            yield return null;
        }
        var canvas = UiKit.CreateCanvas(transform, "BossDefeatCanvas");
        canvas.overrideSorting = true;
        canvas.sortingOrder = 460;
        var shade = UiKit.Panel(canvas.transform, "DefeatShade", new Vector2(2400f, 1400f), Vector2.zero, new Color(0f, 0f, 0f, .42f));
        shade.raycastTarget = false;
        var box = UiKit.Panel(canvas.transform, "DialogueBox", new Vector2(1420f, 290f), new Vector2(0f, -300f), new Color(.06f, .04f, .12f, .97f));
        UiKit.Panel(box.transform, "Trim", new Vector2(1420f, 4f), new Vector2(0f, 144f), new Color(.85f, .25f, .75f));
        var name = UiKit.Label(box.transform, BossDialogue.Name, 28, new Vector2(0, 110f), new Color(1f, .65f, .40f), new Vector2(1300, 40));
        name.font = JapaneseText.Font;
        name.alignment = TextAnchor.MiddleLeft;
        DialogueText = UiKit.Label(box.transform, "", 34, new Vector2(0f, -15f), MenuTheme.SoftIvory, new Vector2(1300f, 205f));
        DialogueText.font = JapaneseText.Font;
        DialogueText.alignment = TextAnchor.UpperLeft;
        DialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        DialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        float voiceLength = GameAudio.Instance != null ? GameAudio.Instance.PlayBossVoice("BossDefeat") : 0f;
        float duration = Mathf.Max(8f, voiceLength + .8f);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(t / (duration - 1.4f) * BossDialogue.Defeat.Length), 0, BossDialogue.Defeat.Length);
            DialogueText.text = BossDialogue.Defeat.Substring(0, count);
            yield return null;
        }
        DialogueText.text = BossDialogue.Defeat;
        box.gameObject.SetActive(false);
        if (_boss != null)
        {
            _boss.IsDissolving = true;
            var renderer = _boss.GetComponent<SpriteRenderer>();
            PixelBurst.Spawn(_boss.transform.position, new Color(.75f, .25f, .85f), 30);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                renderer.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }
            Destroy(_boss.gameObject);
        }
        WorldMapProgress.Complete(3);
        WorldMapProgress.ConfirmFinalBossDefeated();
        GameAudio.Instance?.Finish(true);
        MenuPanels.EndPart1(canvas.transform,
            () => SceneTransitionManager.Instance.Load(GameScenes.WorldMap),
            () => SceneTransitionManager.Instance.Load(GameScenes.MainMenu));
        IsPlaying = false;
    }
}
