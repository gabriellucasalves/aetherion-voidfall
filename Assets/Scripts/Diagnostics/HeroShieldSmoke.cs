#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HeroShieldSmoke : MonoBehaviour
{
    readonly List<string> _results = new List<string>();
    int _failures;
    static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("HeroShieldQA.Enabled", false)) return;
        SessionState.SetBool("HeroShieldQA.Enabled", false);
        var root = new GameObject("HeroShieldSmoke");
        DontDestroyOnLoad(root);
        root.AddComponent<HeroShieldSmoke>();
    }

    void Check(bool yes, string label)
    {
        _results.Add((yes ? "PASS " : "FAIL ") + label);
        if (!yes) _failures++;
        Debug.Log("[Shield QA] " + _results.Last());
    }
    static void SetInput(string key, bool value) => typeof(MobileControls).GetField("<" + key + ">k__BackingField", Static).SetValue(null, value);
    static void Touch(GameObject button, bool down)
    {
        var data = new PointerEventData(EventSystem.current) { pointerId = 31 };
        if (down) ExecuteEvents.Execute(button, data, ExecuteEvents.pointerDownHandler);
        else ExecuteEvents.Execute(button, data, ExecuteEvents.pointerUpHandler);
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory("/tmp/aetherion-shield-qa");
        bool touch = (bool)typeof(MobileControls).GetField("_forcedOn", Static).GetValue(null);
        string hero = WorldMapProgress.SelectedHeroId;
        int phase = StageData.ActivePhase;
        typeof(MobileControls).GetField("_forcedOn", Static).SetValue(null, true);
        var run = Run();
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (System.Exception e) { Check(false, e.ToString()); break; }
            yield return current;
        }
        SetInput("BlockHeld", false); SetInput("AttackHeld", false);
        typeof(MobileControls).GetField("_forcedOn", Static).SetValue(null, touch);
        StageData.ActivePhase = phase;
        if (!string.IsNullOrEmpty(hero)) GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(hero));
        else { PlayerPrefs.DeleteKey("aetherion.worldmap.v1.hero"); PlayerPrefs.Save(); }
        Time.timeScale = 1f;
        File.WriteAllLines("/tmp/aetherion-shield-qa/results.txt", _results.Concat(new[] { "FAILURES=" + _failures }));
        Debug.Log("[Shield QA] Finished, failures=" + _failures);
        EditorApplication.isPlaying = false;
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.3f);
        var icons = new List<Sprite>();
        var guardSprites = new List<Sprite>();
        foreach (string id in new[] { "guerreiro", "mago", "arqueiro" })
        {
            GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(id));
            StageData.ActivePhase = 1;
            SceneTransitionManager.Instance.Load(GameScenes.GroundT1);
            yield return new WaitForSecondsRealtime(.5f);
            var stage = FindFirstObjectByType<GroundT1Controller>();
            FindFirstObjectByType<SimpleEnemySpawner>().SetLocked(true);
            foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(e.gameObject);
            var player = FindFirstObjectByType<PlayerController>();
            var health = player.GetComponent<HealthSystem>();
            var combat = player.GetComponent<PlayerCombat>();
            var shield = player.GetComponent<ShieldSystem>();
            var guard = player.GetComponent<HeroGuardVisual>();
            Check(shield != null && shield.Current == shield.Max && shield.Max == (id == "guerreiro" ? 110f : id == "mago" ? 100f : 75f), id + " possui escudo próprio cheio");
            var fill = (Image)typeof(GroundT1Controller).GetField("_shieldFill", Private).GetValue(stage);
            Check(fill != null && fill.gameObject.activeInHierarchy, id + " possui barra de defesa ativa no HUD");
            string buttonName = id == "guerreiro" ? "Escudo" : id == "mago" ? "Campo" : "Égide";
            var button = GameObject.Find(buttonName);
            Check(button != null, id + " possui botão de defesa no mobile");
            icons.Add(((Image)typeof(MobileControls).GetField("_defenseIcon", Static).GetValue(null)).sprite);
            Touch(button, true);
            yield return new WaitForSecondsRealtime(.25f);
            Check(combat.IsBlocking && guard.Raised, id + " ergue a defesa por toque");
            if (id == "mago")
            {
                Check(combat.Ward.IsActive && combat.Ward.Charge == shield.Current, "Campo do mago usa a mesma barra de absorção");
                guardSprites.Add(player.transform.Find("CampoHex").GetComponent<SpriteRenderer>().sprite);
            }
            else
            {
                Check(guard.Front != null && guard.Front.enabled && guard.Front.sprite.name == (id == "guerreiro" ? "EscudoGuerreiroAtual" : "EgideCristalArqueiro"), id + " exibe defesa distinta");
                guardSprites.Add(guard.Front.sprite);
            }
            float maxHealth = health.Current;
            int healthEvents = 0;
            health.Damaged += _ => healthEvents++;
            float expected = shield.Max;
            health.TakeDamage(10f); expected -= 10f;
            Check(Mathf.Approximately(shield.Current, expected) && health.Current == maxHealth, id + " dano direto vai ao escudo antes da vida");
            float raw = 10f * (1f + player.Hero.Defense / 100f);
            DamageSystem.Apply(health, raw, player.Hero.Defense); expected -= 10f;
            Check(Mathf.Abs(shield.Current - expected) < .01f && health.Current == maxHealth, id + " armadura e escudo são aplicados uma única vez");
            EnemyController.DamagePlayer(player.transform, player.transform.position + Vector3.right * 2, raw); expected -= 10f;
            EnemyController.DamagePlayer(player.transform, player.transform.position + Vector3.left * 2, raw); expected -= 10f;
            EnemyController.DamagePlayer(player.transform, player.transform.position + Vector3.up * 3, raw); expected -= 10f;
            Check(Mathf.Abs(shield.Current - expected) < .01f && health.Current == maxHealth && healthEvents == 0,
                id + " bloqueia golpes frontais, traseiros e de cima sem perder vida");
            health.IsInvulnerable = true;
            EnemyController.DamagePlayer(player.transform, player.transform.position, raw);
            health.IsInvulnerable = false;
            Check(Mathf.Abs(shield.Current - expected) < .01f, id + " invulnerabilidade não gasta escudo");
            health.TakeDamage(0); DamageSystem.Apply(health, 0); health.TakeDamage(-10); DamageSystem.Apply(health, -10);
            Check(Mathf.Abs(shield.Current - expected) < .01f && health.Current == maxHealth, id + " dano zero/negativo não altera barras");
            SetInput("AttackHeld", true);
            yield return new WaitForSecondsRealtime(.15f);
            Check(!combat.IsAttacking && !combat.IsCharging, id + " defesa impede ataque e cancela carga");
            SetInput("AttackHeld", false);
            yield return new WaitForSecondsRealtime(1.6f);
            Check(Mathf.Abs(shield.Current - expected) < .01f, id + " não drena nem regenera escudo enquanto segura");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/aetherion-shield-qa/" + id + "-guard.png");
            yield return null;
            shield.Absorb(shield.Current - 4f);
            health.TakeDamage(10f);
            Check(shield.Current == 0f && Mathf.Abs(health.Current - (maxHealth - 6f)) < .01f,
                id + " somente excedente atinge a vida ao quebrar o escudo");
            yield return new WaitForSecondsRealtime(1.6f);
            Check(shield.Current == 0f && !combat.IsBlocking, id + " escudo quebrado exige soltar para recuperar");
            Touch(button, false);
            yield return new WaitForSecondsRealtime(1.6f);
            Check(shield.Current > 0f && !combat.IsGuardHeld, id + " regenera após soltar e aguardar");
            shield.Refill();
            float before = health.Current;
            health.TakeDamage(10f);
            Check(Mathf.Abs(health.Current - (before - 10f)) < .01f && shield.Current == shield.Max,
                id + " sem defender o dano vai à vida");
            Touch(button, true);
            yield return new WaitForSecondsRealtime(.2f);
            combat.SetLocked(true);
            yield return null;
            Check(!combat.IsBlocking && (combat.Ward == null || !combat.Ward.IsActive), id + " pausa/cinemática interrompe defesa");
            combat.SetLocked(false);
            Touch(button, false);
            health.Kill();
            Check(health.IsDead, id + " queda no abismo continua fatal mesmo com escudo");
            Time.timeScale = 1f;
        }
        Check(icons.Distinct().Count() == 3, "Três ícones mobile distintos para as defesas");
        Check(guardSprites.Distinct().Count() == 3, "Três desenhos de defesa distintos");
    }
}
#endif
