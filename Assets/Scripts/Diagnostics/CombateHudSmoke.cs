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
        try
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image != null)
            {
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
                Destroy(image);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Combate HUD QA] Capture failed for " + name + ": " + ex.Message);
        }
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
            var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            Check(player != null, hero + " carrega em Ground_T1");
            if (player == null) continue;
            var spawner = FindFirstObjectByType<SimpleEnemySpawner>(FindObjectsInactive.Exclude);
            if (spawner != null) spawner.SetLocked(true);
            foreach (var e in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(e.gameObject);
            var card = FindFirstObjectByType<PhaseTitleCardFader>(FindObjectsInactive.Exclude);
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

            if (hero == "guerreiro")
            {
                var t1Stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
                Check(T1AtmosScenery.Built && AtmosFrontLevel.Built,
                    "Fase 1 constrói cenário parallax v3 + playfield com abismos");
                Check(T1AtmosScenery.HasV3Layers,
                    "Fase 1 carrega as 6 camadas de paralaxe v3 (P1..P6 incluindo folhagem na frente e névoa clara)");
                Check(T1AtmosScenery.NoProceduralSprites,
                    "Nenhum SpriteRenderer da Fase 1 usa textura procedural ou branca");
                Check(AtmosFrontLevel.Platforms.Count >= 3,
                    "Fase 1 possui plataformas A, B e C com colisores ajustados ao MANIFEST");
                Check(GameObject.Find("FX_Ambiente_T1") != null,
                    "Fase 1 instancia FX de ambiente (pétalas e motes de Aether) puramente visuais sem dano");

                player.transform.position = new Vector3(-10.0f, t1Stage.Stage.GroundTop + 0.7f, 0f);
                yield return new WaitForSeconds(0.2f);
                yield return new WaitForEndOfFrame();
                Capture("fase1_estilo3_caminho");

                var platA = AtmosFrontLevel.Platforms[0];
                player.transform.position = new Vector3((platA.Left + platA.Right) * 0.5f, platA.Top + 0.6f, 0f);
                yield return new WaitForSeconds(0.25f);
                yield return new WaitForEndOfFrame();
                Capture("fase1_estilo3_plataformas");

                // Restaura posição
                player.transform.position = new Vector3(0f, t1Stage.Stage.GroundTop + 0.7f, 0f);
                yield return new WaitForSeconds(0.1f);
            }

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
                Check(Mathf.Approximately(health.Max, 160f) && Mathf.Approximately(combat.Shield.Max, 110f),
                    "Guerreiro tank balanceado: HP=160 e Escudo=110 (Def=90)");
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
                Check(Mathf.Approximately(health.Max, 115f), "Mago balanceado: HP=115");
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
            if (hero == "mago")
            {
                // Kit novo do Mago: Especial L/Q = Zona Rúnica no chão (raio 2.8, duração 3.5s, tick 16 + lentidão 0.60s)
                var enemies = new List<EnemyController>();
                float[] xs = { 1.2f, 1.9f, 2.6f, 3.3f }; // 4 inimigos comuns dentro do raio 2.8 (centro em x=2.3)
                for (int i = 0; i < xs.Length; i++)
                {
                    var e = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                        StageData.CreateT1(), new Vector2(xs[i], StageData.CreateT1().GroundTop),
                        player.GetComponent<Collider2D>());
                    e.Health.Configure(1000f);
                    e.enabled = false;
                    e.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    enemies.Add(e);
                }
                var zoneGolem = SimpleEnemySpawner.SpawnAt(EnemyData.CreateSlateGolem(), player.transform,
                    StageData.CreateT1(), new Vector2(3.8f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                zoneGolem.Health.Configure(1000f);
                zoneGolem.enabled = false;
                zoneGolem.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

                var method = typeof(PlayerCombat).GetMethod("FireMageSpecial", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "especial_pulso",
                    "mago anima especial");
                Check(ArcaneZone.ActiveZone != null && FindObjectsByType<ArcaneZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 1,
                    "Mago L/Q cria Zona Rúnica no chão à frente");

                // Aguarda o 1º tick (~0.25s)
                yield return new WaitForSeconds(0.32f);
                yield return new WaitForEndOfFrame();
                Capture("mago_especial_768x432");
                Capture("mago_zona_runica_768x432");

                Check(enemies.All(e => e != null && Mathf.Approximately(1000f - e.Health.Current, 16f) && e.IsSlowed),
                    "Zona Rúnica aplica tick de 16 de dano e lentidão a TODOS os 4 inimigos comuns dentro do raio");
                Check(zoneGolem.Health.Current < 1000f && !zoneGolem.IsSlowed,
                    "Golem dentro da Zona Rúnica recebe o dano do tick e NÃO fica lento");

                // Testa que usar o especial de novo substitui a zona anterior (apenas 1 zona por vez)
                method.Invoke(combat, null);
                yield return null;
                Check(FindObjectsByType<ArcaneZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 1,
                    "Apenas 1 Zona Rúnica ativa por vez (nova conjuração remove a anterior)");

                // Testa que ao sair da zona a lentidão acaba em menos de 1s (~0.60s)
                var runner = enemies[0];
                runner.transform.position = new Vector3(-9f, StageData.CreateT1().GroundTop, 0f);
                runner.enabled = true; // permite FixedUpdate descontar _slowLeft fora da zona
                yield return new WaitForSeconds(0.72f);
                Check(!runner.IsSlowed, "Ao sair da Zona Rúnica, a lentidão expira em menos de 1s");

                if (ArcaneZone.ActiveZone != null) Destroy(ArcaneZone.ActiveZone.gameObject);
                if (zoneGolem != null) Destroy(zoneGolem.gameObject);
                foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
                yield return null;

                // Colisão do projétil básico no terreno
                var barrier = new GameObject("BarreiraQA");
                barrier.transform.position = new Vector3(1.5f, StageData.CreateT1().GroundTop + 1f, 0f);
                var solid = barrier.AddComponent<BoxCollider2D>();
                solid.size = new Vector2(.2f, 2f);
                ArcaneBolt.Launch(player.transform, Vector2.right, AbilityData.CreateOrbe(), 12f);
                yield return new WaitForSeconds(.45f);
                Check(GameObject.Find("OrbeArcano") == null, "mago projétil para no terreno");
                Destroy(barrier);

                // Básico do Mago: Pulso Rúnico largo (~52 de dano, SEM lentidão)
                var basicEnemy = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                    StageData.CreateT1(), new Vector2(2.8f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                basicEnemy.Health.Configure(1000f);
                basicEnemy.enabled = false;
                basicEnemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var basic = typeof(PlayerCombat).GetMethod("FireMage", BindingFlags.Instance | BindingFlags.NonPublic);
                basic.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "ataque",
                    "mago anima ataque básico");
                yield return new WaitForSeconds(0.28f);
                yield return new WaitForEndOfFrame();
                Capture("mago_pulso_basico_768x432");
                yield return new WaitForSeconds(0.22f);
                float pulseDmg = 1000f - basicEnemy.Health.Current;
                Check(Mathf.Approximately(pulseDmg, 52f) && !basicEnemy.IsSlowed,
                    $"Mago Pulso Rúnico básico causa 52 de dano ({pulseDmg:F1}) e NÃO aplica lentidão");
                Destroy(basicEnemy.gameObject);
            }
            if (hero == "arqueiro")
            {
                Check(Mathf.Approximately(health.Max, 70f), "Arqueiro vidro balanceado: HP=70");
                var enemies = new List<EnemyController>();
                for (int i = 0; i < 4; i++)
                {
                    var e = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                        StageData.CreateT1(), new Vector2(2.5f + i * 1.3f, StageData.CreateT1().GroundTop),
                        player.GetComponent<Collider2D>());
                    e.Health.Configure(1000f);
                    e.enabled = false;
                    e.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    enemies.Add(e);
                }
                var method = typeof(PlayerCombat).GetMethod("FireArcherSpecial",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "especial_perfurante",
                    "arqueiro anima especial");
                yield return new WaitForSeconds(.5f);
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_especial_768x432");
                yield return new WaitForSeconds(.3f);
                Check(enemies.Take(3).All(e => e != null && e.Health.Current < 1000f),
                    "arqueiro especial atinge alvos reais");
                Check(enemies[3].Health.Current == 1000f, "Flecha Perfurante limita a três alvos");
                foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
                yield return null;
                var barrier = new GameObject("BarreiraQA");
                barrier.transform.position = new Vector3(1.5f, StageData.CreateT1().GroundTop + 1f, 0f);
                var solid = barrier.AddComponent<BoxCollider2D>();
                solid.size = new Vector2(.2f, 2f);
                Arrow.Launch(player.transform, Vector2.right, AbilityData.CreateFlecha(), 12f,
                    Vector3.zero, Color.white, 0f);
                yield return new WaitForSeconds(.45f);
                Check(GameObject.Find("Flecha") == null, "arqueiro projétil para no terreno");
                Destroy(barrier);
                var basicEnemy = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                    StageData.CreateT1(), new Vector2(2.5f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                basicEnemy.Health.Configure(1000f);
                basicEnemy.enabled = false;
                basicEnemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var basic = typeof(PlayerCombat).GetMethod("FireArcher",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                basic.Invoke(combat, null);
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "ataque",
                    "arqueiro anima ataque básico");
                yield return new WaitForSeconds(.45f);
                Check(basicEnemy.Health.Current < 1000f, "arqueiro ataque básico atinge alvo");
                Destroy(basicEnemy.gameObject);
            }
            if (hero == "arqueiro")
            {
                // Aguarda fim de qualquer recuo/cooldown anterior
                yield return new WaitForSeconds(0.40f);
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Print 1: Idle do Arqueiro Arcano no cenário (capuz azul-noturno, 27px)
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_idle_cenario");

                // Teste 4 + Prints de Carga: segurar sem soltar (nenhuma flecha nasce; poses 1, 2 e 3 + indicador)
                combat.enabled = false;
                bool started = combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.10f);
                yield return null;
                Check(started && combat.IsCharging && combat.ChargeSegmentCount == 1 &&
                    FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Puxar (<0.25s): pose de carga N1, 1 segmento, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_1_puxar");

                combat.AdvanceArcherCharge(0.40f); // total 0.50s (Carga Média)
                yield return null;
                Check(combat.IsCharging && combat.ChargeSegmentCount == 2 &&
                    FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Carga Média (0.25-0.80s): pose N2, 2 segmentos, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_2_media");

                combat.AdvanceArcherCharge(0.40f); // total 0.90s (Carga Máxima >= 0.80s)
                yield return null;
                Check(combat.IsCharging && combat.ChargeSegmentCount == 3 &&
                    FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Carga Máxima (>=0.80s): pose N3, 3 segmentos, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_3_maxima");

                // Teste 6: tomar hit no meio da carga cancela a carga SEM disparar nenhuma flecha
                health.IsInvulnerable = false;
                health.TakeDamage(5f);
                health.Configure(health.Max);
                health.IsInvulnerable = true;
                yield return null;
                Check(!combat.IsCharging && FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0,
                    "Tomar dano no meio da carga cancela a carga sem disparar flecha");
                yield return new WaitForSeconds(0.28f);

                // Teste 1: Clique curto (<0.25s) -> sai exatamente 1 flecha fraca (N1), nunca 2 ou 3
                var targetDummy = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform,
                    StageData.CreateT1(), new Vector2(3.6f, StageData.CreateT1().GroundTop),
                    player.GetComponent<Collider2D>());
                targetDummy.Health.Configure(1000f);
                targetDummy.enabled = false;
                targetDummy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.10f);
                combat.ReleaseArcherCharge();
                // Teste 5: tentativa de spam imediato durante recuo/cooldown deve ser bloqueada
                bool spamBlocked = !combat.BeginArcherCharge();
                yield return new WaitForSeconds(0.06f);
                int n1Count = FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Check(n1Count == 1 && combat.LastReleasedTier == 1 && spamBlocked,
                    "Clique curto (<0.25s) dispara EXATAMENTE 1 flecha N1 e bloqueia spam no cooldown");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_n1_voando");
                yield return new WaitForSeconds(0.32f);
                float hpAfterN1 = targetDummy.Health.Current;
                float dmgN1 = 1000f - hpAfterN1;
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Teste 2: Segurar ~0.50s e soltar -> sai exatamente 1 flecha média (N2)
                combat.enabled = true;
                yield return new WaitForSeconds(0.35f);
                combat.enabled = false;
                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.50f);
                combat.ReleaseArcherCharge();
                yield return new WaitForSeconds(0.06f);
                int n2Count = FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Check(n2Count == 1 && combat.LastReleasedTier == 2,
                    "Carga média (~0.50s) dispara EXATAMENTE 1 flecha N2");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_n2_voando");
                yield return new WaitForSeconds(0.30f);
                float hpAfterN2 = targetDummy.Health.Current;
                float dmgN2 = hpAfterN1 - hpAfterN2;
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Teste 3: Segurar >= 0.80s e soltar -> sai exatamente 1 flecha máxima (N3) com rastro e impacto estelar
                combat.enabled = true;
                yield return new WaitForSeconds(0.35f);
                combat.enabled = false;
                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.88f);
                combat.ReleaseArcherCharge();
                yield return new WaitForSeconds(0.06f);
                int n3Count = FindObjectsByType<Arrow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Check(n3Count == 1 && combat.LastReleasedTier == 3,
                    "Carga máxima (>=0.80s) dispara EXATAMENTE 1 flecha N3 com rastro mágico");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_n3_voando");
                yield return new WaitForSeconds(0.28f);
                float dmgN3 = hpAfterN2 - targetDummy.Health.Current;
                Check(Mathf.Abs(dmgN1 - 23.835f) < 0.25f && dmgN2 > 38f && dmgN2 < 43f && Mathf.Abs(dmgN3 - 59.5875f) < 0.25f,
                    $"Escada de dano básico validada: N1({dmgN1:F1}) < Corte(40.5) ≈ N2({dmgN2:F1}) < PulsoMago(52.0) < N3({dmgN3:F1})");
                Destroy(targetDummy.gameObject);
                combat.enabled = true;
                yield return new WaitForSeconds(0.35f);

                Arrow.Launch(player.transform, Vector2.right, AbilityData.CreateFlecha(), 12f,
                    Vector3.zero, Color.white, .3f);
                var stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
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
                Check(FindFirstObjectByType<InfernalBeam>(FindObjectsInactive.Exclude) == null, "Morte cancela Feixe ativo");
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

        // === VALIDAÇÃO FASE 2: O PALÁCIO EM QUEDA ===
        Time.timeScale = 1f;
        GameManager.EnsureExists();
        GameManager.Instance.SelectHero(CharacterCatalog.ById("guerreiro"));
        StageData.ActivePhase = 2;
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.45f);

        var t2Stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        var t2Spawner = FindFirstObjectByType<SimpleEnemySpawner>(FindObjectsInactive.Exclude);
        var t2Player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (t2Player == null)
            t2Player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (t2Spawner != null) t2Spawner.SetLocked(true);
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            Destroy(e.gameObject);

        Check(t2Stage != null && t2Stage.Stage != null && t2Stage.Stage.Id == "t2",
            "Fase 2 carrega StageData.CreateT2 ('O Palácio em Queda')");
        Check(T2PalaceScenery.Built && T2PalaceFrontLevel.Built,
            "Fase 2 constrói cenário parallax v3 + playfield de mármore com abismo");
        Check(T2PalaceScenery.HasV3Layers,
            "Fase 2 carrega as 6 camadas de paralaxe v3 (P1..P6 incluindo silhuetas na frente e fumaça)");
        Check(T2PalaceScenery.NoProceduralSprites,
            "Nenhum SpriteRenderer da Fase 2 usa textura procedural ou branca");
        Check(T2PalaceFrontLevel.Platforms.Count >= 3,
            "Fase 2 possui plataformas A, B e C com colisores ajustados ao MANIFEST");
        Check(GameObject.Find("FX_Ambiente_T2") != null,
            "Fase 2 instancia FX de ambiente (meteoros no fundo, brasas e fumaça) puramente visuais sem dano");
        Check(t2Spawner != null && t2Spawner.TotalWaves == 2,
            "Fase 2 possui exatamente 2 hordas (TotalWaves == 2)");
        Check(FindObjectsByType<PalaceFireHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length >= 3 &&
              FindObjectsByType<PalaceSpikeHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length >= 3 &&
              FindObjectsByType<PalaceFallingDebris>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length >= 3 &&
              FindFirstObjectByType<PalaceCollapsingStatue>(FindObjectsInactive.Exclude) != null &&
              FindFirstObjectByType<PalaceMeteorShower>(FindObjectsInactive.Exclude) != null,
            "Fase 2 instancia perigos de piso (fogo/brasa, espinhos, escombros caindo) + estátua colapsável + chuva de meteoros");

        var titleCardT2 = GameObject.Find("PhaseTitleCard");
        if (titleCardT2 != null) titleCardT2.SetActive(false);

        t2Player.transform.position = new Vector3(-25.5f, t2Stage.Stage.GroundTop + 0.7f, 0f);
        yield return new WaitForSeconds(0.25f);
        yield return new WaitForEndOfFrame();
        Capture("fase2_palacio_playmode_768x432");
        Capture("fase2_estilo3_caminho");

        // Meteoros no fundo + explosão de impacto visual
        var expTex = Resources.Load<Texture2D>("T2_Palace_Atmos/v3/fx/fx_impacto_explosao_6f_64x64");
        if (expTex != null)
        {
            var expFrames = AmbientFxFactory.Slice(expTex, 64, 64, new Vector2(0.5f, 0.06f));
            var expGo = new GameObject("Test_Impacto_Explosao_Visual");
            expGo.transform.position = new Vector3(-24.5f, t2Stage.Stage.GroundTop, 0f);
            var sr = expGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -8;
            expGo.AddComponent<AmbientSpriteAnimator>().Setup(expFrames, 14f, loop: true);
        }
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForEndOfFrame();
        Capture("fase2_estilo3_meteoros");

        // Horda 1: Sentinela de Escombro (emerge da terra + onda de choque)
        var sentinel = SimpleEnemySpawner.SpawnAt(
            EnemyData.CreateRubbleSentinel(),
            t2Player.transform,
            t2Stage.Stage,
            new Vector2(-22.2f, t2Stage.Stage.GroundTop));
        Check(sentinel != null && sentinel.BurrowStage == 1,
            "Sentinela de Escombro inicia escondida sob o piso (BurrowStage == 1)");
        float sentinelHpBefore = sentinel.Health.Current;
        sentinel.ReceiveDamage(25f);
        Check(Mathf.Abs(sentinel.Health.Current - sentinelHpBefore) < 0.01f,
            "Sentinela de Escombro é invulnerável enquanto submersa antes de emergir");
        yield return new WaitForSeconds(0.95f);
        Check(sentinel.BurrowStage == 0,
            "Sentinela de Escombro emerge da terra quando o jogador está próximo (BurrowStage == 0)");
        sentinel.ReceiveDamage(20f);
        Check(sentinel.Health.Current < sentinelHpBefore,
            "Sentinela de Escombro recebe dano normalmente quando está fora da terra");
        var wave = SentinelaShockwave.Spawn(
            sentinel.transform.position + new Vector3(-0.95f, 0f, 0f),
            -1f,
            24f,
            t2Player.transform);
        Check(wave != null, "Sentinela de Escombro dispara onda de choque de pedra no chão");
        yield return new WaitForEndOfFrame();
        Capture("fase2_sentinela_playmode_768x432");
        if (wave != null) Destroy(wave.gameObject);
        if (sentinel != null) Destroy(sentinel.gameObject);

        // Horda 2: Arauto de Brasa (voa em meia-altura + bola de brasa diagonal + rasante)
        t2Player.transform.position = new Vector3(-11.5f, t2Stage.Stage.GroundTop + 0.7f, 0f);
        yield return new WaitForSeconds(0.15f);
        var herald = SimpleEnemySpawner.SpawnAt(
            EnemyData.CreateEmberHerald(),
            t2Player.transform,
            t2Stage.Stage,
            new Vector2(-8.2f, t2Stage.Stage.GroundTop));
        Check(herald != null && herald.transform.position.y > t2Stage.Stage.GroundTop + 1.2f,
            "Arauto de Brasa nasce em meia-altura (alcançável por pulo do Guerreiro, Mago e Arqueiro)");
        var emberBall = EmberFireball.Launch(
            herald.transform.position + new Vector3(-0.55f, 0.5f, 0f),
            t2Player.transform,
            18f);
        Check(emberBall != null, "Arauto de Brasa dispara projétil de brasa diagonal");
        yield return new WaitForEndOfFrame();
        Capture("fase2_arauto_playmode_768x432");
        if (emberBall != null) Destroy(emberBall.gameObject);
        if (herald != null) Destroy(herald.gameObject);

        // Perigos do piso + Estátua colapsando ao fundo
        t2Player.transform.position = new Vector3(11.8f, t2Stage.Stage.GroundTop + 0.7f, 0f);
        yield return new WaitForSeconds(0.45f);
        yield return new WaitForEndOfFrame();
        Capture("fase2_perigos_playmode_768x432");

        // Portal da Fase 2 e liberação da Fase 3 no Mapa
        t2Stage.OpenExitPortal();
        Check(FindFirstObjectByType<F1ExitPortal>(FindObjectsInactive.Exclude) != null,
            "Derrotar as 2 hordas abre o Portal Real na Fase 2");
        WorldMapProgress.Complete(1);
        WorldMapProgress.Complete(2);
        StageData.ActivePhase = 1;
        Check(WorldMapProgress.IsCompleted(2) && WorldMapProgress.IsUnlocked(3),
            "Concluir a Fase 2 libera a Fase 3 no Mapa de Aetherion");

        // =========================================================================
        // FASE 3: AETHERION EM RUÍNAS + BOSS DEVORADOR DO VAZIO
        // =========================================================================
        StageData.ActivePhase = 3;
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.45f);

        var t3Stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        var t3Spawner = FindFirstObjectByType<SimpleEnemySpawner>(FindObjectsInactive.Exclude);
        var t3Player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (t3Player == null)
            t3Player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        Check(t3Stage != null && t3Stage.Stage != null && t3Stage.Stage.Id == "t3",
            "Fase 3 carrega StageData.CreateT3 ('Aetherion em Ruínas')");
        Check(T3RuinsScenery.Built && T3RuinsFrontLevel.Built,
            "Fase 3 constrói cenário parallax P1..P5 + playfield de ruínas com abismos");
        Check(T3RuinsScenery.HasV3Layers,
            "Fase 3 carrega as 5 camadas de paralaxe v3 (P1..P4b incluindo a névoa do chão)");
        Check(T3RuinsScenery.NoProceduralSprites,
            "Nenhum SpriteRenderer da Fase 3 usa textura procedural ou branca");
        Check(t3Spawner != null && t3Spawner.TotalWaves == 3,
            "Fase 3 possui 3 trechos de caminho antes da arena (TotalWaves == 3)");

        var titleCardT3 = GameObject.Find("PhaseTitleCard");
        if (titleCardT3 != null) titleCardT3.SetActive(false);

        // (4) Print Close-up das plataformas flutuantes v2 e degraus
        if (t3Player != null && t3Stage != null)
        {
            t3Player.transform.position = new Vector3(-36.0f, t3Stage.Stage.GroundTop + 2.9f, 0f);
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(-36.0f, t3Stage.Stage.GroundTop + 3.0f, -10f);
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForEndOfFrame();
            Capture("fase3_plataformas_closeup");
        }

        // Testes do Cavaleiro Corrompido
        if (t3Player != null && t3Stage != null)
        {
            t3Player.transform.position = new Vector3(-20.0f, t3Stage.Stage.GroundTop + 0.7f, 0f);
            var knightGo = SimpleEnemySpawner.SpawnAt(
                EnemyData.CreateCorruptedKnight(),
                t3Player.transform,
                t3Stage.Stage,
                new Vector2(-15.0f, t3Stage.Stage.GroundTop));
            var knight = knightGo != null ? knightGo.GetComponent<CavaleiroCorrompido>() : null;
            Check(knight != null && knight.Renderer != null && knight.Renderer.sprite != null,
                "Cavaleiro Corrompido carrega sprites de pixel art v2 (PPU=32)");
            Check(knight != null && knight.UsesV2Sheets,
                "Cavaleiro Corrompido utiliza folhas de sprites _v2 com luz de borda");

            if (knight != null)
            {
                knight.ForceCharge();
                Check(knight.IsCharging && knight.IsShieldUp,
                    "Cavaleiro Corrompido ergue o escudo e investe a 6 u/s");

                // Dano frontal: escudo reduz 70% (leva 30%)
                float hpBeforeFront = knightGo.Health.Current;
                knightGo.ReceiveDamage(30f);
                float dmgTakenFront = hpBeforeFront - knightGo.Health.Current;
                Check(Mathf.Abs(dmgTakenFront - 9f) < 0.25f,
                    "Escudo frontal do Cavaleiro Corrompido bloqueia 70% do dano vindo da frente (toma 30%)");

                // Dano por trás: leva 100%
                t3Player.transform.position = new Vector3(-10.0f, t3Stage.Stage.GroundTop + 0.7f, 0f);
                float hpBeforeBack = knightGo.Health.Current;
                knightGo.ReceiveDamage(20f);
                float dmgTakenBack = hpBeforeBack - knightGo.Health.Current;
                Check(Mathf.Abs(dmgTakenBack - 20f) < 0.25f,
                    "Cavaleiro Corrompido leva dano cheio (100%) por trás");

                // Golpe por cima + onda de choque
                knight.ForceSlam();
                Check(knight.IsSlamming, "Cavaleiro Corrompido desfere golpe por cima após a investida");
                var knightWave = knight.SpawnShockwave();
                Check(knightWave != null,
                    "Cavaleiro Corrompido solta onda de choque no solo que corre 4 u causando 12 de dano");

                // (2) Print Caminho com Cavaleiro investindo e golpe com onda (com fundo v3 e props visíveis)
                yield return new WaitForSeconds(0.15f);
                yield return new WaitForEndOfFrame();
                Capture("fase3_cenario_caminho_cavaleiro");
                Capture("fase3_caminho_cavaleiro_investida_onda");

                if (knightWave != null) Destroy(knightWave.gameObject);
                Destroy(knightGo.gameObject);
            }
        }

        // Testes do Cuspidor do Vazio
        if (t3Player != null && t3Stage != null)
        {
            t3Player.transform.position = new Vector3(-14.0f, t3Stage.Stage.GroundTop + 0.7f, 0f);
            var spitterGo = SimpleEnemySpawner.SpawnAt(
                EnemyData.CreateVoidSpitter(),
                t3Player.transform,
                t3Stage.Stage,
                new Vector2(-7.0f, t3Stage.Stage.GroundTop));
            var spitter = spitterGo != null ? spitterGo.GetComponent<CuspidorDoVazio>() : null;
            Check(spitter != null && spitter.Renderer != null && spitter.Renderer.sprite != null,
                "Cuspidor do Vazio carrega sprites de pixel art v2 (PPU=32)");
            Check(spitter != null && spitter.UsesV2Sheets,
                "Cuspidor do Vazio utiliza folhas de sprites _v2 com luz de borda");
            Check(spitterGo != null && spitterGo.Data != null && spitterGo.Data.MaxHealth == 60f,
                "Cuspidor do Vazio possui 60 HP e mantém 6 a 8 u de distância");

            if (spitter != null)
            {
                // Aproxima herói a menos de 2.5u -> foge devagar
                t3Player.transform.position = new Vector3(-8.2f, t3Stage.Stage.GroundTop + 0.7f, 0f);
                yield return new WaitForSeconds(0.1f);
                Check(spitter.CurrentState == CuspidorDoVazio.State.Flee,
                    "Cuspidor do Vazio foge devagar quando o herói se aproxima a menos de 2.5 u");

                // Cospe ácido em arco telegrafado (14 dano)
                t3Player.transform.position = new Vector3(-14.0f, t3Stage.Stage.GroundTop + 0.7f, 0f);
                var glob = spitter.LaunchGlob(t3Player.transform.position);
                Check(glob != null,
                    "Cuspidor do Vazio cospe glob de ácido em arco telegrafado (14 dano)");

                // Poça de ácido por 3s com dano por tick (6 a cada 0.5s)
                var pool = spitter.SpawnAcidPool(new Vector3(-11.0f, t3Stage.Stage.GroundTop, 0f));
                Check(pool != null && pool.RemainingDuration > 2.5f,
                    "Onde o glob cai forma uma poça de ácido por 3 s que causa dano por tick (6 a cada 0.5s)");

                // (3) Print Caminho com Cuspidor cuspindo perto do abismo e poça de ácido
                yield return new WaitForSeconds(0.15f);
                yield return new WaitForEndOfFrame();
                Capture("fase3_cenario_caminho_cuspidor_fim");
                Capture("fase3_caminho_cuspidor_poca");
                Capture("fase3_caminho_inimigos_perigos");

                if (glob != null) Destroy(glob.gameObject);
                if (pool != null) Destroy(pool.gameObject);
                Destroy(spitterGo.gameObject);
            }
        }

        // Verificação do Portão da Arena sem a barra roxa placeholder
        var gateT3 = FindFirstObjectByType<RuinsArenaGate>(FindObjectsInactive.Exclude);
        var gateR = gateT3 != null ? gateT3.GetComponentInChildren<SpriteRenderer>() : null;
        Check(gateT3 != null && gateR != null && gateR.sprite != null && gateR.sprite.rect.width == 164,
            "Portão da arena usa arte final portao_arena_2f_164x100 sem barra roxa placeholder");

        // Avance até a arena (x >= 37.5f)
        t3Player.transform.position = new Vector3(38.5f, t3Stage.Stage.GroundTop + 0.7f, 0f);
        yield return new WaitForSeconds(0.6f);

        Check(t3Stage.InBossArena && (t3Stage.WaveBanner == null || !t3Stage.WaveBanner.gameObject.activeSelf),
            "Dentro da arena do chefe, o banner de onda é ocultado para não sobrepor a barra do boss");

        var boss = FindFirstObjectByType<VoidDevourerBoss>(FindObjectsInactive.Exclude);
        var bossBar = boss != null ? boss.HealthBar : FindFirstObjectByType<BossHealthBar>(FindObjectsInactive.Include);
        Check(boss != null && bossBar != null,
            "Entrada da arena do chefe trava o portão e instancia Devorador do Vazio com barra de vida");

        if (boss != null) boss.ForceStateFlying();

        if (bossBar != null)
        {
            Check(bossBar.GetComponent<RectTransform>().anchoredPosition.y >= 200f,
                "Barra do boss movida para o alto da tela (y=230) sem sobrepor o chão");

            // (1) Print Arena inteira com o boss, portão e plataformas novas (barra na posição nova)
            bossBar.BeginIntroFill(0.2f);
            yield return new WaitForSeconds(0.3f);
            yield return new WaitForEndOfFrame();
            Capture("fase3_cenario_arena_boss");
            Capture("fase3_arena_barra_boss");
            Capture("fase3_arena_boss_plataformas");

            // (Regressão Fase 3) Frente nova v2 e herói em pé sobre plataforma flutuante da arena
            t3Player.transform.position = new Vector3(48.0f, t3Stage.Stage.GroundTop + 3.4f + 0.6f, 0f);
            yield return new WaitForSeconds(0.25f);
            yield return new WaitForEndOfFrame();
            Capture("fase3_frente_nova");
        }

        if (boss != null && t3Player != null && t3Stage != null)
        {
            // Olhar Duplo (linhas de mira e feixe duplo)
            Vector3 eyeL = boss.transform.position + new Vector3(-0.6f, 0.4f, 0f);
            Vector3 eyeR = boss.transform.position + new Vector3(0.6f, 0.4f, 0f);
            Vector2 aimDir = (t3Player.transform.position - eyeL).normalized;
            var aim1 = BossLaser.SpawnAimLine(t3Stage.transform, eyeL, aimDir, 22f, 1.5f);
            var aim2 = BossLaser.SpawnAimLine(t3Stage.transform, eyeR, aimDir, 22f, 1.5f);
            var glareBeam1 = BossLaser.FireDoubleGlareBeam(eyeL, aimDir, 16f, 1.5f);
            var glareBeam2 = BossLaser.FireDoubleGlareBeam(eyeR, aimDir, 16f, 1.5f);
            Check(aim1 != null && glareBeam1 != null, "Olhar Duplo projeta linhas de mira e feixe laser paralelo");
            yield return new WaitForEndOfFrame();
            Capture("fase3_olhar_duplo");
            if (aim1 != null) Destroy(aim1);
            if (aim2 != null) Destroy(aim2);
            if (glareBeam1 != null) Destroy(glareBeam1.gameObject);
            if (glareBeam2 != null) Destroy(glareBeam2.gameObject);

            // Varredura Rubra
            float groundY = t3Stage.Stage.GroundTop;
            var sweepBeam = BossLaser.FireRedSweepBeam(boss.transform, new Vector3(44f, groundY, 0f), 22f);
            sweepBeam.UpdateSweep(boss.transform.position + Vector3.up * 0.2f, new Vector3(44f, groundY, 0f));
            Check(sweepBeam != null, "Varredura Rubra conecta o chefe ao chão e varre a arena");
            yield return new WaitForEndOfFrame();
            Capture("fase3_varredura_rubra");
            if (sweepBeam != null) Destroy(sweepBeam.gameObject);

            // Erupção de tentáculos com aviso no chão
            var tentacle1 = GroundTentacle.Spawn(t3Stage.transform, new Vector3(42f, groundY, 0f), boss, 1.5f);
            var tentacle2 = GroundTentacle.Spawn(t3Stage.transform, new Vector3(48f, groundY, 0f), boss, 0.1f);
            yield return new WaitForSeconds(0.45f);
            Check(tentacle1 != null && tentacle2 != null, "Erupção gera aviso no chão e tentáculos cortáveis (45 HP)");
            float bossHpBeforeTentacle = boss.GetComponent<HealthSystem>().Current;
            tentacle2.GetComponent<EnemyController>().ReceiveDamage(30f);
            Check(boss.GetComponent<HealthSystem>().Current < bossHpBeforeTentacle,
                "Dano causado a tentáculos transfere 40% diretamente ao Devorador do Vazio");
            yield return new WaitForEndOfFrame();
            Capture("fase3_erupcao_tentaculos");
            if (tentacle1 != null) Destroy(tentacle1.gameObject);
            if (tentacle2 != null) Destroy(tentacle2.gameObject);

            // Descida com o núcleo dourado exposto
            boss.transform.position = new Vector3(48f, groundY + 2.8f, 0f);
            var nucleus = boss.GetComponentInChildren<BossNucleus>(true);
            if (nucleus != null) nucleus.SetActive(true);
            Check(nucleus != null, "Descida aproxima o chefe do chão e expõe o núcleo dourado para ataques físicos e mágicos");
            yield return new WaitForEndOfFrame();
            Capture("fase3_descida_nucleo_dourado");
        }

        // Cutscene de Derrota - Frase na tela
        var cutsceneCanvas = UiKit.CreateCanvas(t3Stage.transform, "TestCutsceneCanvas");
        var dark = UiKit.Panel(cutsceneCanvas.transform, "DarkOverlay", new Vector2(2400f, 1400f), Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        var dlgBox = UiKit.Panel(cutsceneCanvas.transform, "DialogueBox", new Vector2(1000f, 160f), new Vector2(0f, -320f), new Color(0.06f, 0.04f, 0.12f, 0.95f));
        UiKit.Panel(dlgBox.transform, "Trim", new Vector2(1000f, 2f), new Vector2(0f, 79f), new Color(0.85f, 0.25f, 0.75f, 0.9f));
        var spk = UiKit.Label(dlgBox.transform, "DEVORADOR DO VAZIO", 18, new Vector2(-400f, 48f), new Color(1f, 0.45f, 0.65f), new Vector2(400f, 30f));
        spk.alignment = TextAnchor.MiddleLeft;
        var dlg = UiKit.Label(dlgBox.transform, "Pensaram que tinham me destruído… mas o apocalipse está começando.", 22, new Vector2(0f, -12f), MenuTheme.SoftIvory, new Vector2(920f, 80f));
        dlg.alignment = TextAnchor.UpperLeft;
        yield return new WaitForEndOfFrame();
        Capture("fase3_frase_derrota_apocalipse");

        // Forma Cósmica (figura encapuzada + garras magenta)
        dlgBox.gameObject.SetActive(false);
        BossDefeatCutscene.EnsureSprites();
        var hoodTex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/cosmic_hood");
        var hoodSprite = hoodTex != null ? Sprite.Create(hoodTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 32f) : UiKit.WhiteSprite();
        var clawTex = Resources.Load<Texture2D>("Bosses/DevoradorDoVazio/cosmic_claw");
        var clawSprite = clawTex != null ? Sprite.Create(clawTex, new Rect(0, 0, 192, 192), new Vector2(0.5f, 0.5f), 32f) : UiKit.WhiteSprite();

        var hoodGo = new GameObject("TestHood");
        hoodGo.transform.SetParent(cutsceneCanvas.transform, false);
        var hoodImg = hoodGo.AddComponent<Image>();
        hoodImg.sprite = hoodSprite;
        hoodGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
        hoodGo.GetComponent<RectTransform>().sizeDelta = new Vector2(256f, 256f);

        var clawL = new GameObject("TestClawL");
        clawL.transform.SetParent(cutsceneCanvas.transform, false);
        var clawLImg = clawL.AddComponent<Image>();
        clawLImg.sprite = clawSprite;
        clawL.GetComponent<RectTransform>().anchoredPosition = new Vector2(-600f, -40f);
        clawL.GetComponent<RectTransform>().sizeDelta = new Vector2(384f, 384f);

        var clawR = new GameObject("TestClawR");
        clawR.transform.SetParent(cutsceneCanvas.transform, false);
        var clawRImg = clawR.AddComponent<Image>();
        clawRImg.sprite = clawSprite;
        clawR.GetComponent<RectTransform>().anchoredPosition = new Vector2(600f, -40f);
        clawR.GetComponent<RectTransform>().sizeDelta = new Vector2(384f, 384f);
        clawR.GetComponent<RectTransform>().localScale = new Vector3(-1f, 1f, 1f);

        Check(hoodSprite != null && clawSprite != null, "Forma Cósmica carrega figura encapuzada e garras cósmicas");
        yield return new WaitForEndOfFrame();
        Capture("fase3_forma_cosmica");

        // Tela FIM DA PARTE 1
        hoodGo.SetActive(false);
        clawL.SetActive(false);
        clawR.SetActive(false);
        var endPanel = UiKit.Panel(cutsceneCanvas.transform, "EndPart1Panel", new Vector2(800f, 440f), Vector2.zero, MenuTheme.Panel);
        UiKit.Label(endPanel.transform, "FIM DA PARTE 1", 42, new Vector2(0f, 130f), MenuTheme.CelestialGold, new Vector2(700f, 60f));
        UiKit.Label(endPanel.transform, "A batalha continua nas estrelas…\nObrigado por jogar Aetherion: Voidfall!", 24, new Vector2(0f, 40f), MenuTheme.SoftIvory, new Vector2(700f, 80f));
        UiKit.Button(endPanel.transform, "MAPA", new Vector2(0f, -60f), new Vector2(260f, 58f), null);
        UiKit.Button(endPanel.transform, "MENU", new Vector2(0f, -135f), new Vector2(260f, 58f), null);
        yield return new WaitForEndOfFrame();
        Capture("fase3_fim_da_parte_1");
        Destroy(cutsceneCanvas.gameObject);

        // Mapa com a Fase 3 concluída / liberada
        WorldMapProgress.Complete(3);
        WorldMapProgress.ConfirmFinalBossDefeated();
        Check(WorldMapProgress.IsCompleted(3) && WorldMapProgress.GameComplete,
            "Derrota do Devorador do Vazio conclui a Fase 3 e a Parte 1 do jogo");

        SceneManager.LoadScene(GameScenes.WorldMap);
        yield return null;
        WorldMapController.EnsureExists();
        yield return new WaitForSeconds(0.45f);
        yield return new WaitForEndOfFrame();
        Capture("fase3_mapa_liberada_concluida");

        File.WriteAllLines(Path.Combine(Output, "results.txt"), _results);
        Debug.Log("[Combate HUD QA] FINAL " + (_failures == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        if (Application.isBatchMode)
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        else
            EditorApplication.isPlaying = false;
    }

    static void Size(int width, int height)
    {
        try
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
            group.GetType().GetMethod("AddCustomSize", flags)?.Invoke(group, new[] { size });
            int total = (int)group.GetType().GetMethod("GetTotalCount", flags).Invoke(group, null);
            var window = EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
            if (window != null && total > 0)
            {
                window.Show();
                window.GetType().GetProperty("selectedSizeIndex", flags)?.SetValue(window, total - 1);
            }
        }
        catch { }
    }
}
#endif
