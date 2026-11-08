#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombateHudSmoke : MonoBehaviour
{
    const string Output = "/tmp/aetherion-combate-hud-local";
    readonly List<string> _results = new List<string>();
    int _failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("CombateHudQA.Enabled", false)) return;
        SessionState.SetBool("CombateHudQA.Enabled", false);
        var go = new GameObject("CombateHudSmoke");
        DontDestroyOnLoad(go);
        go.AddComponent<CombateHudSmoke>();
    }

    void Check(bool yes, string message)
    {
        _results.Add((yes ? "PASS " : "FAIL ") + message);
        if (!yes) _failures++;
        Debug.Log("[Combate HUD QA] " + _results.Last());
    }

    void Capture(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
        Destroy(image);
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);
        foreach (string hero in new[] { "guerreiro", "mago", "arqueiro" })
        {
            Time.timeScale = 1f;
            GameManager.EnsureExists().SelectHero(CharacterCatalog.ById(hero));
            SceneManager.LoadScene(GameScenes.GroundT1);
            yield return null;
            GroundT1Controller.EnsureExists();
            yield return new WaitForSeconds(.5f);
            var player = FindFirstObjectByType<PlayerController>();
            Check(player != null, hero + " carrega em Ground_T1");
            if (player == null) continue;
            var spawner = FindFirstObjectByType<SimpleEnemySpawner>();
            if (spawner != null) spawner.SetLocked(true);
            foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(e.gameObject);
            var card = FindFirstObjectByType<PhaseTitleCardFader>();
            if (card != null) Destroy(card.gameObject);
            player.GetComponent<HealthSystem>().IsInvulnerable = true;
            player.transform.position = new Vector3(0f, StageData.CreateT1().GroundTop + .7f, 0f);
            yield return new WaitForSeconds(.25f);
            var visual = GameObject.Find(hero == "guerreiro" ? "GuerreiroPixel" : hero == "mago" ? "MagoPixel" : "ArqueiroPixel");
            var sprite = visual != null ? visual.GetComponent<SpriteRenderer>()?.sprite : null;
            Check(sprite != null && sprite.texture.name == "idle", hero + " usa sheet idle do pack");
            var hud = GameObject.Find("HudHorizontalV1");
            Check(hud != null, hero + " usa HUD horizontal");
            if (hud != null)
            {
                var images = hud.GetComponentsInChildren<Image>();
                Check(images.Any(i => i.name == "icon_" + hero + "_20x20" && i.sprite != null), hero + " ícone importado");
                Check(images.Any(i => i.name == "bar_fill_vida_68x6" && i.rectTransform.rect.width > 0), hero + " VIDA vinculada");
                Check(images.Any(i => i.name == "bar_fill_escudo_68x6") == (hero != "arqueiro") &&
                    images.Any(i => i.name == "bar_frame_desabilitada_72x10") == (hero == "arqueiro"),
                    hero + " defesa correta");
            }
            yield return new WaitForEndOfFrame();
            Capture(hero + "_768x432");
            Size(384, 216);
            yield return new WaitForSeconds(.15f);
            yield return new WaitForEndOfFrame();
            Capture(hero + "_384x216");
            Size(768, 432);
            yield return new WaitForSeconds(.15f);
            var combat = player.GetComponent<PlayerCombat>();
            var health = player.GetComponent<HealthSystem>();
            float lifeWidth = hud.GetComponentsInChildren<Image>()
                .First(i => i.name == "bar_fill_vida_68x6").rectTransform.rect.width;
            health.IsInvulnerable = false;
            health.TakeDamage(health.Max * .25f);
            yield return null;
            Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "dano",
                hero + " anima dano");
            float partialWidth = hud.GetComponentsInChildren<Image>()
                .First(i => i.name == "bar_fill_vida_68x6").rectTransform.rect.width;
            Check(partialWidth > 0f && partialWidth < lifeWidth, hero + " HUD mostra VIDA parcial");
            health.Configure(health.Max);
            health.IsInvulnerable = true;
            yield return new WaitForSeconds(.25f);
            var body = player.GetComponent<Rigidbody2D>();
            player.enabled = false;
            body.linearVelocity = new Vector2(2.5f, 0f);
            yield return new WaitForSeconds(.05f);
            Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "andar", hero + " anima caminhada");
            body.linearVelocity = new Vector2(6f, 0f);
            yield return new WaitForSeconds(.05f);
            Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "correr", hero + " anima corrida");
            player.transform.position = new Vector3(0f, StageData.CreateT1().GroundTop + 3f, 0f);
            body.linearVelocity = new Vector2(0f, 4f);
            yield return new WaitForSeconds(.05f);
            Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "pulo", hero + " anima pulo");
            body.linearVelocity = Vector2.zero;
            player.transform.position = new Vector3(0f, StageData.CreateT1().GroundTop + .7f, 0f);
            player.enabled = true;
            yield return new WaitForSeconds(.1f);
            if (hero == "guerreiro")
            {
                var defenseFill = hud.GetComponentsInChildren<Image>()
                    .First(i => i.name == "bar_fill_escudo_68x6").rectTransform;
                combat.Shield.Absorb(combat.Shield.Max * .5f);
                yield return null;
                Check(defenseFill.rect.width > 0f && defenseFill.rect.width < 340f,
                    "ESCUDO parcial usa valor real");
                combat.Shield.Absorb(combat.Shield.Max);
                yield return null;
                Check(defenseFill.rect.width < 1f, "ESCUDO vazio usa valor real");
                combat.Shield.Refill();
            }
            if (hero == "mago")
            {
                var defenseFill = hud.GetComponentsInChildren<Image>()
                    .First(i => i.name == "bar_fill_escudo_68x6").rectTransform;
                combat.enabled = false;
                combat.Ward.SetHolding(true);
                yield return new WaitForSeconds(.3f);
                Check(combat.Ward.IsActive && defenseFill.rect.width > 0f &&
                    defenseFill.rect.width < 340f, "Campo preservado e barra parcial real");
                combat.Ward.Mitigate(1000f);
                yield return null;
                Check(defenseFill.rect.width < 1f, "Campo vazio usa valor real");
                combat.Ward.Configure();
                combat.enabled = true;
            }
            if (hero == "guerreiro")
            {
                var golem = SimpleEnemySpawner.SpawnAt(EnemyData.CreateSlateGolem(), player.transform,
                    StageData.CreateT1(), new Vector2(4f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                yield return new WaitForSeconds(.18f);
                Check(golem != null && golem.IsWindingUp, "Golem telegrapha alvo alcançável");
                yield return new WaitForEndOfFrame();
                Capture("golem_telegraph_768x432");
                float before = golem.transform.position.x;
                player.transform.position = new Vector3(6f, player.transform.position.y, 0f);
                yield return new WaitForSeconds(.82f);
                Check(golem != null && golem.transform.position.x < before, "Golem mantém direção da investida");
                golem.ApplySlow(1.5f);
                Check(!golem.IsSlowed, "Pulso não imobiliza Golem");
                Destroy(golem.gameObject);
                player.transform.position = new Vector3(-24f, player.transform.position.y, 0f);
                var edgeGolem = SimpleEnemySpawner.SpawnAt(EnemyData.CreateSlateGolem(), player.transform,
                    StageData.CreateT1(), new Vector2(-29.2f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                yield return new WaitForSeconds(1.1f);
                Check(edgeGolem != null && edgeGolem.transform.position.x < -28.5f &&
                    !edgeGolem.IsWindingUp && !edgeGolem.IsCharging,
                    "Golem evita gap e não investe em alvo sem caminho");
                Destroy(edgeGolem.gameObject);
                player.transform.position = new Vector3(3f, StageData.CreateT1().GroundTop + .7f, 0f);
                var wall = new GameObject("ParedeGolemQA");
                wall.transform.position = new Vector3(1.5f, StageData.CreateT1().GroundTop + 1f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.25f, 2f);
                var wallGolem = SimpleEnemySpawner.SpawnAt(EnemyData.CreateSlateGolem(), player.transform,
                    StageData.CreateT1(), new Vector2(0f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                yield return new WaitForSeconds(.9f);
                Check(wallGolem != null && wallGolem.transform.position.x < 1f &&
                    !wallGolem.IsCharging, "Golem respeita parede e linha de visão");
                Destroy(wallGolem.gameObject);
                Destroy(wall);
                player.transform.position = new Vector3(1.5f, StageData.CreateT1().GroundTop + 3.1f, 0f);
                var highGolem = SimpleEnemySpawner.SpawnAt(EnemyData.CreateSlateGolem(), player.transform,
                    StageData.CreateT1(), new Vector2(0f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                yield return new WaitForSeconds(.4f);
                Check(highGolem != null && !highGolem.IsWindingUp && !highGolem.IsCharging,
                    "Golem não investe em plataforma inalcançável");
                Destroy(highGolem.gameObject);
                player.transform.position = new Vector3(0f, StageData.CreateT1().GroundTop + .7f, 0f);
                var meter = player.GetComponent<ResonanceMeter>();
                var specialFill = hud.GetComponentsInChildren<Image>()
                    .First(i => i.name == "bar_fill_especial_68x6").rectTransform;
                meter.Add(50f);
                yield return null;
                Check(specialFill.rect.width > 0f && specialFill.rect.width < 340f,
                    "ESPECIAL parcial acompanha Ressonância");
                meter.Add(1000f);
                yield return null;
                Check(specialFill.rect.width >= 339f, "ESPECIAL cheio acompanha Ressonância");
                bool fired = combat.TryFireWarriorSpecial();
                Check(fired && meter.Current == 0f && !combat.TryFireWarriorSpecial(),
                    "Feixe consome barra exatamente uma vez");
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "especial_feixe",
                    "Guerreiro anima preparo do Feixe");
                yield return new WaitForSeconds(.2f);
                yield return new WaitForEndOfFrame();
                Capture("guerreiro_feixe_768x432");
            }
            if (hero == "mago" || hero == "arqueiro")
            {
                var enemies = new List<EnemyController>();
                int count = hero == "arqueiro" ? 4 : 1;
                for (int i = 0; i < count; i++)
                {
                    var e = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                        StageData.CreateT1(), new Vector2(2.5f + i * 1.3f, StageData.CreateT1().GroundTop),
                        player.GetComponent<Collider2D>());
                    e.Health.Configure(1000f);
                    e.enabled = false;
                    e.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    enemies.Add(e);
                }
                var method = typeof(PlayerCombat).GetMethod(hero == "mago" ? "FireMageSpecial" : "FireArcherSpecial",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name ==
                    (hero == "mago" ? "especial_pulso" : "especial_perfurante"),
                    hero + " anima especial");
                yield return new WaitForSeconds(hero == "mago" ? .36f : .5f);
                yield return new WaitForEndOfFrame();
                Capture(hero + "_especial_768x432");
                yield return new WaitForSeconds(hero == "mago" ? .44f : .3f);
                Check(enemies.Take(hero == "arqueiro" ? 3 : 1).All(e => e != null && e.Health.Current < 1000f),
                    hero + " especial atinge alvos reais");
                if (hero == "arqueiro")
                    Check(enemies[3].Health.Current == 1000f, "Flecha Perfurante limita a três alvos");
                if (hero == "mago") Check(enemies[0].IsSlowed, "Pulso Rúnico aplica lentidão curta");
                foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
                yield return null;
                var barrier = new GameObject("BarreiraQA");
                barrier.transform.position = new Vector3(1.5f, StageData.CreateT1().GroundTop + 1f, 0f);
                var solid = barrier.AddComponent<BoxCollider2D>();
                solid.size = new Vector2(.2f, 2f);
                if (hero == "arqueiro")
                    Arrow.Launch(player.transform, Vector2.right, AbilityData.CreateFlecha(), 12f,
                        Vector3.zero, Color.white, 0f);
                else
                    ArcaneBolt.Launch(player.transform, Vector2.right, AbilityData.CreateOrbe(), 12f);
                yield return new WaitForSeconds(.45f);
                Check(GameObject.Find(hero == "arqueiro" ? "Flecha" : "OrbeArcano") == null,
                    hero + " projétil para no terreno");
                Destroy(barrier);
                var basicEnemy = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                    StageData.CreateT1(), new Vector2(2.5f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                basicEnemy.Health.Configure(1000f);
                basicEnemy.enabled = false;
                basicEnemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var basic = typeof(PlayerCombat).GetMethod(hero == "arqueiro" ? "FireArcher" : "FireMage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                basic.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "ataque",
                    hero + " anima ataque básico");
                yield return new WaitForSeconds(.45f);
                Check(basicEnemy.Health.Current < 1000f, hero + " ataque básico atinge alvo");
                Destroy(basicEnemy.gameObject);
            }
            if (hero == "arqueiro")
            {
                Arrow.Launch(player.transform, Vector2.right, AbilityData.CreateFlecha(), 12f,
                    Vector3.zero, Color.white, .3f);
                var stage = FindFirstObjectByType<GroundT1Controller>();
                stage.RequestPause();
                Check(Time.timeScale == 0f, "Pausa preservada");
                typeof(GroundT1Controller).GetMethod("TogglePause", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(stage, null);
                Check(Time.timeScale == 1f, "Retorno da pausa preservado");
            }
            player.GetComponent<HealthSystem>().Kill();
            yield return new WaitForSecondsRealtime(.1f);
            if (hero == "arqueiro")
                Check(GameObject.Find("Flecha") == null, "Morte cancela disparo pendente");
            if (hero == "guerreiro")
                Check(FindFirstObjectByType<InfernalBeam>() == null, "Morte cancela Feixe ativo");
            yield return new WaitForSecondsRealtime(.9f);
            var deathSprite = visual.GetComponent<SpriteRenderer>().sprite;
            Check(deathSprite != null && deathSprite.texture.name == "morte",
                hero + " anima morte com tempo pausado");
            Check(Time.timeScale == 0f && GameObject.Find("Derrota")?.activeSelf == true,
                hero + " mantém tela VOCÊ CAIU");
            if (hero == "arqueiro")
            {
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_morte_768x432");
            }
        }
        File.WriteAllLines(Path.Combine(Output, "results.txt"), _results);
        Debug.Log("[Combate HUD QA] FINAL " + (_failures == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        EditorApplication.Exit(_failures == 0 ? 0 : 1);
    }

    static void Size(int width, int height)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance", flags).GetValue(null);
        var group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes,
            new object[] { sizesType.GetProperty("currentGroupType", flags).GetValue(sizes) });
        var type = asm.GetType("UnityEditor.GameViewSize");
        var enumType = asm.GetType("UnityEditor.GameViewSizeType");
        var size = System.Activator.CreateInstance(type, flags, null,
            new object[] { System.Enum.ToObject(enumType, 1), width, height, "Combate HUD QA " + width + "x" + height }, null);
        int index = (int)group.GetType().GetMethod("GetTotalCount", flags).Invoke(group, null);
        group.GetType().GetMethod("AddCustomSize", flags).Invoke(group, new[] { size });
        var window = EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        window.Show();
        window.GetType().GetProperty("selectedSizeIndex", flags).SetValue(window, index);
    }
}
#endif
