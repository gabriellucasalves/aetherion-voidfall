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
                Check(ArcaneZone.ActiveZone != null && FindObjectsByType<ArcaneZone>(FindObjectsSortMode.None).Length == 1,
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
                Check(FindObjectsByType<ArcaneZone>(FindObjectsSortMode.None).Length == 1,
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
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Print 1: Idle do Arqueiro Arcano no cenário (capuz azul-noturno, 27px)
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_idle_cenario");

                // Teste 4 + Prints de Carga: segurar sem soltar (nenhuma flecha nasce; poses 1, 2 e 3 + indicador)
                combat.enabled = false;
                bool started = combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.10f);
                yield return null;
                Check(started && combat.IsCharging && combat.ChargeSegmentCount == 1 &&
                    FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Puxar (<0.25s): pose de carga N1, 1 segmento, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_1_puxar");

                combat.AdvanceArcherCharge(0.40f); // total 0.50s (Carga Média)
                yield return null;
                Check(combat.IsCharging && combat.ChargeSegmentCount == 2 &&
                    FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Carga Média (0.25-0.80s): pose N2, 2 segmentos, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_2_media");

                combat.AdvanceArcherCharge(0.40f); // total 0.90s (Carga Máxima >= 0.80s)
                yield return null;
                Check(combat.IsCharging && combat.ChargeSegmentCount == 3 &&
                    FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length == 0,
                    "Arqueiro Carga Máxima (>=0.80s): pose N3, 3 segmentos, 0 flechas nascem durante hold");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_carga_3_maxima");

                // Teste 6: tomar hit no meio da carga cancela a carga SEM disparar nenhuma flecha
                health.IsInvulnerable = false;
                health.TakeDamage(5f);
                health.Configure(health.Max);
                health.IsInvulnerable = true;
                yield return null;
                Check(!combat.IsCharging && FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length == 0,
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
                int n1Count = FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length;
                Check(n1Count == 1 && combat.LastReleasedTier == 1 && spamBlocked,
                    "Clique curto (<0.25s) dispara EXATAMENTE 1 flecha N1 e bloqueia spam no cooldown");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_n1_voando");
                yield return new WaitForSeconds(0.32f);
                float hpAfterN1 = targetDummy.Health.Current;
                float dmgN1 = 1000f - hpAfterN1;
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Teste 2: Segurar ~0.50s e soltar -> sai exatamente 1 flecha média (N2)
                combat.enabled = true;
                yield return new WaitForSeconds(0.35f);
                combat.enabled = false;
                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.50f);
                combat.ReleaseArcherCharge();
                yield return new WaitForSeconds(0.06f);
                int n2Count = FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length;
                Check(n2Count == 1 && combat.LastReleasedTier == 2,
                    "Carga média (~0.50s) dispara EXATAMENTE 1 flecha N2");
                yield return new WaitForEndOfFrame();
                Capture("arqueiro_n2_voando");
                yield return new WaitForSeconds(0.30f);
                float hpAfterN2 = targetDummy.Health.Current;
                float dmgN2 = hpAfterN1 - hpAfterN2;
                foreach (var a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);

                // Teste 3: Segurar >= 0.80s e soltar -> sai exatamente 1 flecha máxima (N3) com rastro e impacto estelar
                combat.enabled = true;
                yield return new WaitForSeconds(0.35f);
                combat.enabled = false;
                combat.BeginArcherCharge();
                combat.AdvanceArcherCharge(0.88f);
                combat.ReleaseArcherCharge();
                yield return new WaitForSeconds(0.06f);
                int n3Count = FindObjectsByType<Arrow>(FindObjectsSortMode.None).Length;
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
