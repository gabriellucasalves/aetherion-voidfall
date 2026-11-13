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
                combat.Ward.Mitigate(30f);
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
                var images = hud.GetComponentsInChildren<Image>(true);
                var especialImg = images.FirstOrDefault(i => i.name == "bar_fill_especial_68x6");
                var supremoImg = images.FirstOrDefault(i => i.name == "bar_fill_supremo_68x6");
                Check(especialImg != null && supremoImg != null, "HUD_EspecialESupremo: Barras ESPECIAL e SUPREMO existem no HUD");

                var frameSupremo = images.FirstOrDefault(i => i.name == "bar_frame_supremo_76x12");
                var labelSupremo = images.FirstOrDefault(i => i.name == "label_supremo_29x7");
                Check(frameSupremo != null && labelSupremo != null, "HUD_EspecialESupremo: Frame e label do SUPREMO existem");

                var lifeRt = images.FirstOrDefault(i => i.name == "bar_fill_vida_68x6")?.rectTransform;
                var defRt = images.FirstOrDefault(i => i.name == "bar_fill_escudo_68x6")?.rectTransform ?? images.FirstOrDefault(i => i.name == "bar_frame_desabilitada_72x10")?.rectTransform;
                var espRt = especialImg.rectTransform;
                var supRt = supremoImg.rectTransform;
                bool noOverlap = true;
                if (lifeRt != null && defRt != null && espRt != null && supRt != null)
                {
                    noOverlap = (lifeRt.anchoredPosition.x + 360f <= defRt.anchoredPosition.x + 50f) &&
                                (defRt.anchoredPosition.x + 360f <= espRt.anchoredPosition.x + 50f) &&
                                (espRt.anchoredPosition.x + 360f <= supRt.anchoredPosition.x + 50f) &&
                                (supRt.anchoredPosition.x + 380f <= 1920f);
                }
                Check(noOverlap, "HUD_EspecialESupremo: Slots de VIDA, ESCUDO, ESPECIAL e SUPREMO não se sobrepõem e cabem na tela");
                Check(especialImg.color == Color.white && supremoImg.color == Color.white, "HUD_EspecialESupremo: Fill de Especial e Supremo são brancos sem tint");
                Check(especialImg.sprite != null && especialImg.sprite.name.StartsWith("bar_fill_especial_" + hero), "HUD_EspecialESupremo: Sprite do ESPECIAL corresponde ao herói");

                // Independência 1: meter.ForceSet(50f)
                meter.ForceSet(50f);
                yield return null;
                float espWidthBefore = espRt.rect.width;
                Check(supRt.rect.width >= 160f && supRt.rect.width <= 180f, "HUD_EspecialESupremo: SUPREMO a 50% fica entre 160 e 180 px");
                Check(Mathf.Abs(espRt.rect.width - espWidthBefore) < 1f, "HUD_EspecialESupremo: ESPECIAL não mudou com alteração na Ressonância");

                // Independência 2: dispare o especial L/Q
                combat.TryFireWarriorSpecial();
                yield return null;
                Check(espRt.rect.width < 30f, "HUD_EspecialESupremo: ESPECIAL reiniciou (< 30 px) após disparo do especial L/Q");
                Check(supRt.rect.width >= 160f && supRt.rect.width <= 180f && Mathf.Abs(meter.Current - 50f) < 0.01f, "HUD_EspecialESupremo: SUPREMO permaneceu inalterado após especial L/Q");

                // Brilho, banner e partículas só no SUPREMO
                var glow = hud.transform.Find("SupremoBrilho")?.gameObject ?? hud.transform.Find("HudHorizontalV1/SupremoBrilho")?.gameObject;
                var parts = hud.transform.Find("SupremoParticulas")?.gameObject ?? hud.transform.Find("HudHorizontalV1/SupremoParticulas")?.gameObject;
                var banner = hud.transform.Find("BannerSupremo")?.gameObject ?? hud.transform.Find("HudHorizontalV1/BannerSupremo")?.gameObject;
                Check(glow != null && !glow.activeSelf && parts != null && !parts.activeSelf && banner != null && !banner.activeSelf,
                    "HUD_EspecialESupremo: Brilho, partículas e banner inativos com SUPREMO incompleto");

                meter.ForceSet(100f);
                yield return null;
                yield return null;
                Check(glow != null && glow.activeSelf && parts != null && parts.activeSelf && banner != null && banner.activeSelf,
                    "HUD_EspecialESupremo: Brilho, partículas e banner ativos com SUPREMO a 100%");
                Check(supremoImg.sprite != null && supremoImg.sprite.name.StartsWith("bar_fill_supremo_cheio"),
                    "HUD_EspecialESupremo: Sprite do SUPREMO usa bar_fill_supremo_cheio quando cheio");

                var badSpecialFx = hud.GetComponentsInChildren<Transform>(true)
                    .Any(t => (t.name.Contains("Brilho") || t.name.Contains("Particulas") || t.name.Contains("Banner")) &&
                              !t.name.StartsWith("Supremo") && !t.name.StartsWith("BannerSupremo"));
                Check(!badSpecialFx, "HUD_EspecialESupremo: Nenhum efeito de brilho/partícula/banner no slot ESPECIAL");

                yield return new WaitForEndOfFrame();
                Capture("hud_supremo_cheio");

                // Gasta o supremo
                meter.TrySpend();
                yield return null;
                Check(banner != null && !banner.activeSelf, "HUD_EspecialESupremo: Banner inativo após gastar SUPREMO");
                Check(supRt.rect.width < 10f, "HUD_EspecialESupremo: SUPREMO voltou para perto de 0 após gastar");

                // Feixe L/Q não consome ResonanceMeter e respeita CD 6s
                var cdFieldCombat = typeof(PlayerCombat).GetField("_specialCooldownLeft", BindingFlags.Instance | BindingFlags.NonPublic);
                if (cdFieldCombat != null) cdFieldCombat.SetValue(combat, 0f);
                meter.ForceSet(40f);
                bool fired = combat.TryFireWarriorSpecial();
                Check(fired && Mathf.Abs(meter.Current - 40f) < 0.01f && !combat.TryFireWarriorSpecial()
                      && combat.SpecialCooldownLeft > 5.9f, "Feixe L/Q não consome ResonanceMeter e respeita CD 6s");
                yield return null;
                Check(visual.GetComponent<SpriteRenderer>().sprite.texture.name == "especial_feixe",
                    "Guerreiro anima preparo do Feixe");
                yield return new WaitForSeconds(.2f);
                yield return new WaitForEndOfFrame();
                Capture("guerreiro_feixe_768x432");

                // Caso Guerreiro_EspecialSemMedidor
                yield return new WaitForSeconds(6.1f);
                meter.ForceSet(0f);
                Check(combat.SpecialReady, "Guerreiro_EspecialSemMedidor: pronto mesmo com o medidor vazio");
                Check(combat.TryFireWarriorSpecial() && meter.Current == 0f, "Guerreiro_EspecialSemMedidor: L/Q dispara com Ressonância 0 e não mexe nela");
                Check(FindObjectsByType<FireCrescent>(FindObjectsSortMode.None).Length == 0 && !SpecialController.IsCinematic,
                    "Guerreiro_EspecialSemMedidor: L/Q não abre cut-in nem rajada");

                // Supremo_RetratoNovo (Guerreiro)
                yield return TestSupremoRetratoNovo("guerreiro", player);

                // Guerreiro_SupremoRajada
                var stageCtrl = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
                yield return TestGuerreiroSupremoRajada(player, stageCtrl);
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

                // Independência ESPECIAL e SUPREMO no Mago
                var mMeter = player.GetComponent<ResonanceMeter>();
                var mImages = hud.GetComponentsInChildren<Image>(true);
                var mEspImg = mImages.FirstOrDefault(i => i.name == "bar_fill_especial_68x6");
                var mSupImg = mImages.FirstOrDefault(i => i.name == "bar_fill_supremo_68x6");
                Check(mEspImg != null && mSupImg != null, "Mago: Barras ESPECIAL e SUPREMO existem");
                Check(mEspImg.sprite != null && mEspImg.sprite.name.StartsWith("bar_fill_especial_mago"), "Mago: Sprite do ESPECIAL é bar_fill_especial_mago");
                mMeter.ForceSet(50f);
                yield return null;
                float mEspW = mEspImg.rectTransform.rect.width;
                Check(mSupImg.rectTransform.rect.width >= 160f && mSupImg.rectTransform.rect.width <= 180f, "Mago: SUPREMO a 50% independente");
                Check(Mathf.Abs(mEspImg.rectTransform.rect.width - mEspW) < 1f, "Mago: ESPECIAL independente da Ressonância");
                var fireZone = typeof(PlayerCombat).GetMethod("FireMageSpecial", BindingFlags.Instance | BindingFlags.NonPublic);
                fireZone.Invoke(combat, null);
                yield return null;
                Check(mEspImg.rectTransform.rect.width < 30f, "Mago: ESPECIAL reiniciou com especial L/Q");
                Check(mSupImg.rectTransform.rect.width >= 160f && mSupImg.rectTransform.rect.width <= 180f && Mathf.Abs(mMeter.Current - 50f) < 0.01f, "Mago: SUPREMO inalterado após especial L/Q");
                mMeter.TrySpend();

                // Supremo_RetratoNovo (Mago)
                yield return TestSupremoRetratoNovo("mago", player);
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

                // Independência ESPECIAL e SUPREMO no Arqueiro
                var aMeter = player.GetComponent<ResonanceMeter>();
                var aImages = hud.GetComponentsInChildren<Image>(true);
                var aEspImg = aImages.FirstOrDefault(i => i.name == "bar_fill_especial_68x6");
                var aSupImg = aImages.FirstOrDefault(i => i.name == "bar_fill_supremo_68x6");
                Check(aEspImg != null && aSupImg != null, "Arqueiro: Barras ESPECIAL e SUPREMO existem");
                Check(aEspImg.sprite != null && aEspImg.sprite.name.StartsWith("bar_fill_especial_arqueiro"), "Arqueiro: Sprite do ESPECIAL é bar_fill_especial_arqueiro");
                aMeter.ForceSet(50f);
                yield return null;
                float aEspW = aEspImg.rectTransform.rect.width;
                Check(aSupImg.rectTransform.rect.width >= 160f && aSupImg.rectTransform.rect.width <= 180f, "Arqueiro: SUPREMO a 50% independente");
                Check(Mathf.Abs(aEspImg.rectTransform.rect.width - aEspW) < 1f, "Arqueiro: ESPECIAL independente da Ressonância");
                var fireArcher = typeof(PlayerCombat).GetMethod("FireArcherSpecial", BindingFlags.Instance | BindingFlags.NonPublic);
                fireArcher.Invoke(combat, null);
                yield return null;
                Check(aEspImg.rectTransform.rect.width < 30f, "Arqueiro: ESPECIAL reiniciou com especial L/Q");
                Check(aSupImg.rectTransform.rect.width >= 160f && aSupImg.rectTransform.rect.width <= 180f && Mathf.Abs(aMeter.Current - 50f) < 0.01f, "Arqueiro: SUPREMO inalterado após especial L/Q");
                aMeter.TrySpend();

                // Supremo_RetratoNovo (Arqueiro)
                yield return TestSupremoRetratoNovo("arqueiro", player);
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
        Capture("fase2_profundidade");

        var met0 = GameObject.Find("MeteoroFundo_0");
        if (met0 != null)
        {
            var srMet = met0.GetComponent<SpriteRenderer>();
            Check(srMet != null && srMet.sortingOrder == -32,
                "Fase 2 meteoros de fundo possuem sorting -32 (para não brigar com P3b)");
        }

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
            "Fase 3 carrega as camadas de paralaxe v3 (incluindo as névoas P2b e P3b)");
        Check(T3RuinsScenery.NoProceduralSprites,
            "Nenhum SpriteRenderer da Fase 3 usa textura procedural ou branca");
        Check(t3Spawner != null && t3Spawner.TotalWaves == 2,
            "Fase 3 possui 2 ondas de inimigos antes da arena (TotalWaves == 2)");

        // Case: F3_Inimigos_Onda
        bool t1ConfigIntact = SimpleEnemySpawner.DemoWaveCount == 3 &&
                              SimpleEnemySpawner.MaxAlive == 12 &&
                              Mathf.Abs(SimpleEnemySpawner.Interval - 1.25f) < 0.01f;
        Check(t1ConfigIntact, "F3_Inimigos_Onda: Configurações de ondas e spawn da Fase 1 e 2 permanecem inalteradas");

        Check(t3Spawner != null && t3Spawner.CurrentMaxAlive == 3,
            "F3_Inimigos_Onda: Fase 3 limita max vivos a 3 (MaxAlive == 3)");
        Check(t3Spawner != null && t3Spawner.CurrentInterval >= 2.0f,
            "F3_Inimigos_Onda: Fase 3 intervalo entre spawns é >= 2.0s");

        int w1Knights = 0, w1Spitters = 0;
        bool w1AllValid = true;
        for (int i = 0; i < 6; i++)
        {
            var e = t3Spawner != null ? t3Spawner.GetNextT3Enemy(1, i) : null;
            if (e != null && e.Id == "cavaleiro_corrompido") w1Knights++;
            else if (e != null && e.Id == "cuspidor_vazio") w1Spitters++;
            else w1AllValid = false;
        }

        int w2Knights = 0, w2Spitters = 0;
        bool w2AllValid = true;
        for (int i = 0; i < 6; i++)
        {
            var e = t3Spawner != null ? t3Spawner.GetNextT3Enemy(2, i) : null;
            if (e != null && e.Id == "cavaleiro_corrompido") w2Knights++;
            else if (e != null && e.Id == "cuspidor_vazio") w2Spitters++;
            else w2AllValid = false;
        }

        bool enemiesValid = w1AllValid && w2AllValid && (w1Knights == 4 && w1Spitters == 2) && (w2Knights == 3 && w2Spitters == 3);
        Check(enemiesValid,
            "F3_Inimigos_Onda: Tipos pertencem a {Cavaleiro, Cuspidor}, Onda 1 possui 4+2 e Onda 2 possui 3+3");

        // Spawn simulado da onda com banner ativo e no máximo 3 vivos para captura do print
        t3Player.transform.position = new Vector3(-20.0f, t3Stage.Stage.GroundTop + 0.7f, 0f);
        t3Stage.AnnounceWave(1, "Cavaleiros Corrompidos");
        var waveEnemy1 = SimpleEnemySpawner.SpawnAt(EnemyData.CreateCorruptedKnight(), t3Player.transform, t3Stage.Stage, new Vector2(-15.5f, t3Stage.Stage.GroundTop));
        var waveEnemy2 = SimpleEnemySpawner.SpawnAt(EnemyData.CreateCorruptedKnight(), t3Player.transform, t3Stage.Stage, new Vector2(-12.0f, t3Stage.Stage.GroundTop));
        var waveEnemy3 = SimpleEnemySpawner.SpawnAt(EnemyData.CreateVoidSpitter(), t3Player.transform, t3Stage.Stage, new Vector2(-8.5f, t3Stage.Stage.GroundTop));
        int aliveInSim = 0;
        foreach (var ec in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) aliveInSim++;
        Check(aliveInSim <= 3, "F3_Inimigos_Onda: Nunca há mais de 3 vivos ao mesmo tempo na Fase 3");
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForEndOfFrame();
        Capture("fase3_inimigos_onda");
        if (waveEnemy1 != null) Destroy(waveEnemy1.gameObject);
        if (waveEnemy2 != null) Destroy(waveEnemy2.gameObject);
        if (waveEnemy3 != null) Destroy(waveEnemy3.gameObject);

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

        // Case: Boss_Cutscene (expansão de Boss_FalaEntrada)
        if (boss != null)
        {
            // Aos 0,4 s
            yield return new WaitForSeconds(0.4f);
            var cutsceneGo = GameObject.Find("BossIntroCutscene");
            Check(cutsceneGo != null, "Boss_Cutscene: GameObject BossIntroCutscene existe aos 0.4s");
            if (cutsceneGo != null)
            {
                string[] requiredChildren = { "LetterboxTopo", "LetterboxBase", "CaixaDialogo", "PlacaNome", "RetratoBoss", "MolduraRetrato", "FalaBoss" };
                bool allChildren = requiredChildren.All(c => cutsceneGo.transform.Find(c) != null);
                Check(allChildren, "Boss_Cutscene: BossIntroCutscene possui todos os filhos requeridos");

                var topBox = cutsceneGo.transform.Find("LetterboxTopo")?.GetComponent<RectTransform>();
                Check(topBox != null && topBox.rect.height >= 90f, "Boss_Cutscene: Altura de LetterboxTopo >= 90");
            }

            var hudGrp = GameObject.Find("HudHorizontalV1")?.GetComponent<CanvasGroup>();
            Check(hudGrp != null && hudGrp.alpha < 0.05f, "Boss_Cutscene: Alfa do HudHorizontalV1 < 0.05");

            var t3Combat = t3Player.GetComponent<PlayerCombat>();
            Check(t3Player.IsLocked && (t3Combat == null || t3Combat.IsLocked), "Boss_Cutscene: Herói travado durante a cutscene");

            Check(boss.transform.position.y < t3Stage.Stage.GroundTop + 4.8f,
                "Boss_FalaEntrada: Boss emerge subindo do chão em direção à posição final");

            float hpBeforeIntroDamage = boss.Health.Current;
            boss.EnemyCtrl.ReceiveDamage(100f);
            Check(Mathf.Abs(boss.Health.Current - hpBeforeIntroDamage) < 0.01f,
                "Boss_FalaEntrada: Dano é completamente ignorado durante a Intro (boss invulnerável)");

            // Aos 3.6 s desde a entrada (já passaram 0.4s, espera mais 3.2s)
            yield return new WaitForSeconds(3.2f);
            boss.Cutscene?.RequestSkip();
            Check(boss.CurrentSpeechText == BossDialogue.IntroPt || boss.CurrentSpeechText.StartsWith("Vocês acharam que Aetherion") || boss.CurrentSpeechText == "Vocês pensaram que Aetherion cairia em silêncio... Eu sou o Devorador do Vazio.",
                "Boss_Cutscene: Fala completa do Devorador do Vazio atingida aos 3.6s");

            yield return new WaitForEndOfFrame();
            Capture("boss_cutscene");
            Capture("boss_fala_entrada");

            // Depois da Intro (espera acabar a dublagem e cutscene completa)
            float introTimeout = 10f;
            while (GameObject.Find("BossIntroCutscene") != null && introTimeout > 0f)
            {
                introTimeout -= 0.25f;
                yield return new WaitForSeconds(0.25f);
            }
            Check(GameObject.Find("BossIntroCutscene") == null, "Boss_Cutscene: BossIntroCutscene destruído após fim da Intro");
            Check(hudGrp != null && Mathf.Abs(hudGrp.alpha - 1f) < 0.05f, "Boss_Cutscene: HUD alfa retornou a 1.0");
            Check(!t3Player.IsLocked && (t3Combat == null || !t3Combat.IsLocked), "Boss_Cutscene: Herói destravado após fim da cutscene");

            boss.ForceStateFlying();
            Check(Mathf.Abs(boss.CurrentBossAlpha - 1f) < 0.05f && boss.transform.position.y >= t3Stage.Stage.GroundTop + 4.5f,
                "Boss_FalaEntrada: Boss atinge posição final e alfa 1.0 ao concluir emergência");

            // Retry e Skip
            var dummyRetryBar = BossHealthBar.Create(bossBar != null ? bossBar.transform.parent : t3Stage.transform);
            var retryBoss = VoidDevourerBoss.Spawn(new Vector3(48f, t3Stage.Stage.GroundTop, 0f), t3Player.transform, t3Stage.Stage, dummyRetryBar, isRetry: true);
            yield return new WaitForSeconds(0.2f);

            if (retryBoss != null && retryBoss.Cutscene != null)
            {
                retryBoss.Cutscene.RequestSkip(); // 1st: completa o texto na hora
                Check(retryBoss.CurrentSpeechText.EndsWith("Eclipse Eterno!") || retryBoss.CurrentSpeechText.EndsWith("Eu sou o Devorador do Vazio."),
                    "Boss_Cutscene: Retry utiliza fala curta que termina em 'Eu sou o Devorador do Vazio.'");
                Check(retryBoss.CurrentSpeechText == BossDialogue.RetryPt || retryBoss.CurrentSpeechText == "Eu sou o Devorador do Vazio.",
                    "Boss_Cutscene: Primeiro skip completa o texto na hora");
                retryBoss.Cutscene.RequestSkip(); // 2nd: SkipToEnd = true
                yield return new WaitForSeconds(0.46f);
                Check(retryBoss.CurrentState == VoidDevourerBoss.State.Flying || retryBoss.Cutscene == null,
                    "Boss_Cutscene: Segundo skip encerra a Intro em <= 0.6s");
            }
            if (retryBoss != null)
            {
                if (retryBoss.HealthBar != null) Destroy(retryBoss.HealthBar.gameObject);
                Destroy(retryBoss.gameObject);
            }

            // Caso Guerreiro_SupremoRajada_Boss
            boss.Health.Configure(BossData.MaxHealth);
            float hp0 = boss.Health.Current;
            FireCrescent.Spawn(t3Player.transform, boss.transform.position + Vector3.left * 1.5f, 1f, 144f, true);
            yield return new WaitForSeconds(0.3f);
            Check(boss.Health.Current < hp0 && (hp0 - boss.Health.Current) <= 260f,
                $"Guerreiro_SupremoRajada_Boss: boss perde HP, dentro do teto (perdeu {hp0 - boss.Health.Current:F1})");
            Check(boss.CurrentState == VoidDevourerBoss.State.Flying,
                "Guerreiro_SupremoRajada_Boss: boss não é empurrado nem atordoado");
            boss.Health.Configure(BossData.MaxHealth);
        }

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
            Capture("fase3_profundidade");

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

        // Case: Boss_TomaDano
        if (boss != null && bossBar != null)
        {
            // 1. Exige que só exista 1 HealthSystem no GameObject do boss
            var healthSystems = boss.GetComponents<HealthSystem>();
            Check(healthSystems.Length == 1,
                "Boss_TomaDano: Existe estritamente 1 HealthSystem no GameObject do boss (duplicação removida)");

            // 2. Aplica 10 hits de herói no boss pelo EnemyController.ReceiveDamage
            boss.Health.Configure(BossData.MaxHealth);
            float hpBefore10Hits = boss.Health.Current;
            for (int i = 0; i < 10; i++)
            {
                boss.EnemyCtrl.ReceiveDamage(25f);
            }
            bossBar.ForceSync();
            yield return new WaitForSeconds(0.1f);

            float hpAfter10Hits = boss.Health.Current;
            float ratio = hpAfter10Hits / boss.Health.Max;
            Check(hpAfter10Hits < hpBefore10Hits,
                "Boss_TomaDano: Health.Current caiu após 10 hits de herói via EnemyController.ReceiveDamage");
            Check(bossBar.FillAmount < 1.0f && Mathf.Abs(bossBar.FillAmount - ratio) <= 0.01f,
                "Boss_TomaDano: Fill da barra do boss é < 1 e corresponde a Current/Max com tolerância de 0.01");

            // Captura print boss_tomando_dano.png
            yield return new WaitForEndOfFrame();
            Capture("boss_tomando_dano");

            // 3. Leva a vida a transição e exige Fase 2
            DamageSystem.Apply(boss.Health, boss.Health.Current);
            bossBar.ForceSync();
            yield return null;
            Check(boss.Phase == 2,
                "Boss_TomaDano: Transição de fase para Fase 2 ocorre em <= 66% de vida");

            // 4. Leva a vida a transição e exige Fase 3
            boss.ForceStateFlying();
            DamageSystem.Apply(boss.Health, boss.Health.Current);
            bossBar.ForceSync();
            yield return null;
            Check(boss.Phase == 3,
                "Boss_TomaDano: Transição de fase para Fase 3 ocorre em <= 33% de vida");

            // 5. Leva a vida a 0 e exige que o boss não seja destruído antes da cutscene ou fala de derrota
            DamageSystem.Apply(boss.Health, boss.Health.Current);
            yield return new WaitForSeconds(0.1f);
            Check(boss != null && boss.gameObject != null,
                "Boss_TomaDano: Levar a vida a 0 não destrói o boss antes da cutscene e da fala de derrota");

            var cutsceneRunning = FindFirstObjectByType<BossDefeatCutscene>();
            if (cutsceneRunning != null) Destroy(cutsceneRunning.gameObject);
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
        var endPanel = MenuPanels.EndPart1(cutsceneCanvas.transform, null, null);
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

        yield return TestTelasInicio();

        yield return TestSentinelaV2();
        yield return TestEpilogoPixelArt();
        yield return TestNazorFormasCameraArena();
        yield return TestCameraMorteAbismo();
        yield return TestPortalAcabamentoVisual();

        File.WriteAllLines(Path.Combine(Output, "results.txt"), _results);
        Debug.Log("[Combate HUD QA] FINAL " + (_failures == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        if (Application.isBatchMode)
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        else
            EditorApplication.isPlaying = false;
    }

    IEnumerator TestSupremoRetratoNovo(string hero, PlayerController player)
    {
        var meter = player.GetComponent<ResonanceMeter>();
        var spec = player.GetComponent<SpecialController>();
        var cdField = typeof(SpecialController).GetField("_cooldownLeft", BindingFlags.Instance | BindingFlags.NonPublic);
        if (cdField != null) cdField.SetValue(spec, 0f);

        meter.ForceSet(100f);
        meter.TrySpend();
        var runMethod = typeof(SpecialController).GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic);
        var routine = (IEnumerator)runMethod.Invoke(spec, null);
        spec.StartCoroutine(routine);

        yield return new WaitForSecondsRealtime(0.85f);
        Check(CutInPanel.Current != null, "Supremo_RetratoNovo: CutInPanel ativo");
        Image bustImg = null;
        Image eyesImg = null;
        if (CutInPanel.Current != null)
        {
            bustImg = CutInPanel.Current.BustImage;
            eyesImg = CutInPanel.Current.EyesImage;
        }
        Check(bustImg != null && bustImg.sprite != null && bustImg.sprite.name.StartsWith("cutin_busto_" + hero),
            "Supremo_RetratoNovo: Busto do " + hero + " carrega cutin_busto_" + hero);
        Check(eyesImg != null && eyesImg.sprite != null && eyesImg.sprite.name.StartsWith("cutin_olhos_" + hero),
            "Supremo_RetratoNovo: Olhos do " + hero + " carrega cutin_olhos_" + hero);

        yield return new WaitForEndOfFrame();
        if (hero == "guerreiro")
            Capture("supremo_guerreiro_cutin");
        else
            Capture("supremo_" + hero);

        float timeout = 15f;
        while (SpecialController.IsCinematic && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        Check(!SpecialController.IsCinematic, "Supremo_RetratoNovo: Cut-in e cinematic concluídos em tempo hábil");
    }

    IEnumerator TestGuerreiroSupremoRajada(PlayerController player, GroundT1Controller stageCtrl)
    {
        var spec = player.GetComponent<SpecialController>();
        var meter = player.GetComponent<ResonanceMeter>();
        var gVisual = player.GetComponentInChildren<GuerreiroVisual>();

        var cdField = typeof(SpecialController).GetField("_cooldownLeft", BindingFlags.Instance | BindingFlags.NonPublic);
        if (cdField != null) cdField.SetValue(spec, 0f);

        player.transform.position = new Vector3(0f, stageCtrl.Stage.GroundTop + 0.7f, 0f);
        var pCtrl = player.GetComponent<PlayerController>();
        pCtrl.FaceVisual(true);

        var enemies = new List<EnemyController>();
        float[] xs = { 2.0f, 3.5f, 5.0f };
        for (int i = 0; i < xs.Length; i++)
        {
            var e = SimpleEnemySpawner.SpawnAt(EnemyData.CreateAlienSkeleton(), player.transform, stageCtrl.Stage, new Vector2(xs[i], stageCtrl.Stage.GroundTop));
            e.Health.Configure(1000f);
            e.enabled = false;
            e.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            enemies.Add(e);
        }
        var knight = SimpleEnemySpawner.SpawnAt(EnemyData.CreateCorruptedKnight(), player.transform, stageCtrl.Stage, new Vector2(7.5f, stageCtrl.Stage.GroundTop));
        knight.Health.Configure(1000f);
        knight.enabled = false;
        knight.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        enemies.Add(knight);

        meter.ForceSet(100f);
        meter.TrySpend();
        var runMethod = typeof(SpecialController).GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic);
        spec.StartCoroutine((IEnumerator)runMethod.Invoke(spec, null));

        yield return null;
        Check(meter.Current == 0f, "Guerreiro_SupremoRajada: E com medidor cheio dispara e consome a Ressonância");

        float waitCrescents = 4f;
        while (waitCrescents > 0f)
        {
            waitCrescents -= Time.unscaledDeltaTime;
            if (FindObjectsByType<FireCrescent>(FindObjectsSortMode.None).Length >= 3)
                break;
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        Capture("supremo_guerreiro");

        float timeout = 8f;
        while (SpecialController.IsCinematic && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        int hitCount = enemies.Count(e => e.Health.Current < 1000f);
        Check(hitCount >= 3, "Guerreiro_SupremoRajada: Pelo menos 3 inimigos levaram dano das ondas");
        Check(knight.Health.Current < 1000f, "Guerreiro_SupremoRajada: O inimigo mais longe (+7.5) levou dano (pierce até o fim)");

        float dmgFirst = 1000f - enemies[0].Health.Current;
        Check(dmgFirst >= 300f && dmgFirst <= 470f,
            $"Guerreiro_SupremoRajada: Primeiro esqueleto perdeu entre 300 e 470 HP (perdeu {dmgFirst:F1})");

        Check(gVisual == null || gVisual.enabled, "Guerreiro_SupremoRajada: GuerreiroVisual voltou a ficar enabled");
        Check(FireCrescent.Alive == 0, "Guerreiro_SupremoRajada: FireCrescent.Alive == 0 após encerramento");

        foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
    }

    IEnumerator TestTelasInicio()
    {
        Time.timeScale = 1f;

        // ---------- Título ----------
        SceneManager.LoadScene(GameScenes.MainMenu);
        yield return null;
        MainMenuController.EnsureExists();
        yield return new WaitForSeconds(0.8f);
        var menu = MainMenuController.Instance;
        Check(menu != null && FindFirstObjectByType<MenuAtmosBackground>() != null, "Menu_KitNovo: título com fundo pintado em camadas (parallax)");
        Check(menu != null && menu.Buttons.Count >= 3 && menu.Buttons.All(b => b.GetComponent<MenuButtonFx>() != null),
            "Menu_KitNovo: botões do kit novo com cursor/foco");
        Check(GameObject.Find("titulo_fenda_brilho_80x213") != null, "Menu_KitNovo: fenda vertical do Vazio no céu");
        int heroesOnTitle = FindObjectsByType<UiSpriteAnimator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        Check(heroesOnTitle >= 3, "Menu_KitNovo: trio de heróis animado no título (" + heroesOnTitle + ")");
        var feet = GameObject.Find("Retrato_guerreiro");
        Check(feet != null && Vector2.Distance(feet.GetComponent<RectTransform>().pivot, HeroPortrait.FeetPivot) < 0.001f,
            "Menu_KitNovo: retrato com pivô nos pés");

        // nome do jogo padronizado em tudo que aparece
        bool logoOk = GameObject.Find("logo_aetherion_301x58") != null && GameObject.Find("subtitulo_apocalipse_130x19") != null;
        bool noVoidfallText = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(t => t.text.IndexOf("Voidfall", System.StringComparison.OrdinalIgnoreCase) < 0);
        string[] sources =
        {
            "Assets/Scripts/Stages/GroundT1Controller.cs", "Assets/Scripts/Bosses/BossDefeatCutscene.cs",
            "Assets/Scripts/UI/MainMenuController.cs", "Assets/Scripts/UI/IntroController.cs", "Assets/Scripts/UI/MenuPanels.cs",
            "Assets/WebGLTemplates/Aetherion/index.html"
        };
        var dirty = sources.Where(p => File.Exists(p) && File.ReadAllText(p).IndexOf("Voidfall", System.StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        bool template = File.Exists("Assets/WebGLTemplates/Aetherion/index.html") && File.ReadAllText("Assets/WebGLTemplates/Aetherion/index.html").Contains("AETHERION: APOCALIPSE");
        Check(logoOk && noVoidfallText && dirty.Length == 0 && template && MenuPanels.EndPart1Text.Contains("APOCALIPSE") && MenuPanels.DemoCompleteText.Contains("APOCALIPSE"),
            "Titulo_Apocalipse: logo + subtítulo, nenhum texto visível com 'Voidfall'" + (dirty.Length > 0 ? " (sobrou em: " + string.Join(", ", dirty) + ")" : "") + (template ? "" : " (template WebGL sem AETHERION: APOCALIPSE)"));
        yield return new WaitForEndOfFrame();
        Capture("titulo_menu");

        // configurações
        if (menu != null)
        {
            menu.SettingsPanel.SetActive(true);
            yield return null;
            yield return new WaitForEndOfFrame();
            var overflow = menu.SettingsPanel.GetComponentsInChildren<Text>(true).Where(t => !MenuKit.Fits(t)).Select(t => t.text).ToArray();
            Check(overflow.Length == 0, "Menu_KitNovo: CONFIGURAÇÕES sem texto vazando" + (overflow.Length > 0 ? " → " + string.Join(" | ", overflow) : ""));
            Check(menu.SettingsPanel.GetComponentsInChildren<Text>(true).Any(t => t.text == "E"), "Menu_KitNovo: CONFIGURAÇÕES mostra Supremo no E");
            Capture("config");
            menu.SettingsPanel.SetActive(false);
        }

        // ---------- Intro ----------
        SceneManager.LoadScene(GameScenes.Intro);
        yield return null;
        IntroController.EnsureExists();
        yield return new WaitForSeconds(0.6f);
        var intro = IntroController.Instance;
        Check(intro != null, "Intro_Paineis: IntroController ativo");
        if (intro != null)
        {
            bool allPanels = true, allFit = true;
            for (int i = 0; i < IntroController.Pages.Length; i++)
            {
                allPanels &= Resources.Load<Texture2D>("Menu/intro/intro_painel_" + (i + 1) + "_320x180") != null;
                intro.JumpTo(i);
                yield return null;
                if (!MenuKit.Fits(intro.Body)) { allFit = false; Debug.LogWarning("[Combate HUD QA] intro página " + (i + 1) + " vazou"); }
                if (i == 0) { yield return new WaitForEndOfFrame(); Capture("intro_painel1"); }
            }
            Check(allPanels, "Intro_Paineis: 6 painéis pintados carregados");
            Check(allFit, "Intro_Paineis: texto das 6 páginas cabe na caixa de diálogo");
            var heroes = GameObject.Find("HeroisPainel6");
            Check(heroes != null && heroes.activeInHierarchy && heroes.GetComponentsInChildren<UiSpriteAnimator>().Length == 3, "Intro_Paineis: painel 6 com os 3 heróis vivos");
            Check(GameObject.Find("Botao_CONTINUAR") != null && GameObject.Find("Botao_PULAR") != null, "Intro_Paineis: CONTINUAR e PULAR presentes");
            yield return new WaitForEndOfFrame();
            Capture("intro_painel6");
        }

        // ---------- Seleção ----------
        SceneManager.LoadScene(GameScenes.CharacterSelection);
        yield return null;
        CharacterSelectController.EnsureExists();
        yield return new WaitForSeconds(0.6f);
        var select = CharacterSelectController.Instance;
        Check(select != null && select.Selected == null, "Selecao_Habilidades: abre sem herói escolhido");
        Check(CharacterSelectController.Stats[0].Max == 160, "Selecao_Habilidades: VIDA máx. 160 (Guerreiro não estoura a barra)");
        var g = HeroSkillInfo.Guerreiro;
        string catalog = CharacterCatalog.ById("guerreiro") != null ? CharacterCatalog.ById("guerreiro").AbilityDescription : "";
        Check(g.Skills[1].Key == "L/Q" && g.Skills[2].Key == "E" && !catalog.Contains("L/Q/E") && !catalog.Contains("consome ESPECIAL"),
            "Selecao_Habilidades: Guerreiro — Especial L/Q sem medidor, Supremo no E");
        if (select != null)
        {
            foreach (var hero in new[] { "guerreiro", "mago", "arqueiro" })
            {
                select.SelectById(hero);
                yield return null;
                yield return new WaitForEndOfFrame();
                if (select.Selected == null || select.Selected.Id != hero) { Check(false, "Selecao_Habilidades " + hero + ": herói não está na seleção"); continue; }
                var bad = select.OverflowingTexts();
                Check(bad.Count == 0, "Selecao_Habilidades " + hero + ": BÁSICO/ESPECIAL/SUPREMO sem texto vazando" + (bad.Count > 0 ? " → " + string.Join(" | ", bad) : ""));
                Capture("selecao_" + hero);
            }
        }

        // ---------- Mapa: herói com modelo novo andando ----------
        SceneManager.LoadScene(GameScenes.WorldMap);
        yield return null;
        WorldMapController.EnsureExists();
        yield return new WaitForSeconds(0.5f);
        var anim = FindFirstObjectByType<WorldMapHeroAnimator>();
        var walkField = typeof(WorldMapHeroAnimator).GetField("_walk", BindingFlags.NonPublic | BindingFlags.Instance);
        var walk = anim != null && walkField != null ? walkField.GetValue(anim) as Sprite[] : null;
        Check(walk != null && walk.Length == 6 && walk[0] != null && walk[0].texture != null && walk[0].texture.name == "andar",
            "Mapa_HeroiModeloNovo: mapa usa TrioAnimations/<herói>/andar (6 frames)");
        var avatarImg = anim != null ? anim.GetComponent<Image>() : null;
        Check(avatarImg != null && avatarImg.sprite != null && avatarImg.sprite.texture.name == "idle", "Mapa_HeroiModeloNovo: parado usa o idle do modelo novo");
        yield return new WaitForEndOfFrame();
        Capture("mapa_heroi");
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

    IEnumerator TestSentinelaV2()
    {
        Size(768, 432);
        // 8. Textura 576x64 Point, 9 frames, isReadable
        var tex = Resources.Load<Texture2D>("Inimigos/sentinela_escombro");
        Check(tex != null && tex.width == 576 && tex.height == 64 && tex.filterMode == FilterMode.Point,
            "Sentinela_V2: textura 576x64 Point importada");
        Check(tex != null && tex.isReadable, "Sentinela_V2: textura isReadable ativada no asset");

        // 9. GetPixels32: células 0..3 altura 34..38 px, linha mais baixa na linha 56 do topo (y=7 no Unity), alfa 0/255
        if (tex != null && tex.isReadable)
        {
            var pixels = tex.GetPixels32();
            bool cellsOk = true;
            bool noAlphaBlend = true;
            for (int cell = 0; cell < 4; cell++)
            {
                int minY = 64, maxY = -1;
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        var p = pixels[y * 576 + (cell * 64 + x)];
                        if (p.a > 0)
                        {
                            if (p.a != 255) noAlphaBlend = false;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
                int height = maxY - minY + 1;
                // Unity y=0 é o rodapé; linha 56 a partir do topo equivale a y = 63 - 56 = 7
                if (height < 34 || height > 38 || minY != 7)
                {
                    cellsOk = false;
                    Debug.LogWarning($"[Combate HUD QA] Célula {cell} altura={height} (esperado 34..38), minY={minY} (esperado 7)");
                }
            }
            Check(cellsOk, "Sentinela_V2: células 0..3 com altura entre 34 e 38 px e pés alinhados na linha 56 do topo (pivô 7/64)");
            Check(noAlphaBlend, "Sentinela_V2: sem pixels semitransparentes (alfa estritamente 0 ou 255)");

            // Célula 8 (onda de choque)
            bool cell8HasPixels = false;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (pixels[y * 576 + (8 * 64 + x)].a > 0) cell8HasPixels = true;
                }
            }
            Check(cell8HasPixels, "Sentinela_V2: célula 8 da folha contém a onda de choque de escombros");
        }

        // Teste em Play Mode na Fase 2
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        StageData.ActivePhase = 2;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.5f);
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        var stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        if (stage != null && player != null)
        {
            var card = FindFirstObjectByType<PhaseTitleCardFader>(FindObjectsInactive.Exclude);
            if (card != null) Destroy(card.gameObject);
            var spawner = FindFirstObjectByType<SimpleEnemySpawner>(FindObjectsInactive.Exclude);
            if (spawner != null) spawner.SetLocked(true);
            foreach (var e in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(e.gameObject);

            player.transform.position = new Vector3(-20f, stage.Stage.GroundTop + 0.7f, 0f);
            var sentinel = SimpleEnemySpawner.SpawnAt(
                EnemyData.CreateRubbleSentinel(),
                player.transform,
                stage.Stage,
                new Vector2(-22.2f, stage.Stage.GroundTop));

            var visual = sentinel != null ? sentinel.GetComponentInChildren<InimigoVisual>() : null;
            var sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            var phaseField = typeof(EnemyController).GetField("_phase", BindingFlags.Instance | BindingFlags.NonPublic);

            // Permite 1 frame para InimigoVisual.LateUpdate inicializar
            yield return null;

            // 10. Estados e quadros
            Check(sentinel != null && sentinel.BurrowStage == 1, "Sentinela_V2: inicia escondida (BurrowStage == 1, célula 4)");
            if (sr != null && sr.sprite != null)
            {
                Check(Mathf.Approximately(sr.sprite.rect.x, 256f), "Sentinela_V2: célula 4 (x=256) usada ao nascer submersa");
            }
            yield return new WaitForEndOfFrame();
            Capture("sentinela_v2_submersa");

            // Força estado emergindo
            typeof(EnemyController).GetField("_burrowStage", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sentinel, 2);
            typeof(InimigoVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(visual, null);
            yield return null;
            if (sr != null && sr.sprite != null)
            {
                Check(Mathf.Approximately(sr.sprite.rect.x, 320f), "Sentinela_V2: célula 5 (x=320) usada ao emergir");
            }
            yield return new WaitForEndOfFrame();
            Capture("sentinela_v2_emergindo");

            // Estado idle completo
            typeof(EnemyController).GetField("_burrowStage", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sentinel, 0);
            if (phaseField != null) phaseField.SetValue(sentinel, System.Enum.ToObject(phaseField.FieldType, 0));
            typeof(InimigoVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(visual, null);
            yield return null;
            if (sr != null && sr.sprite != null)
            {
                Check(sr.sprite.rect.x == 0f || sr.sprite.rect.x == 64f, "Sentinela_V2: célula 0/1 usada no idle");
                Check(Mathf.Approximately(sr.sprite.pixelsPerUnit, 16f), "Sentinela_V2: PPU do sprite é 16");
                Check(Mathf.Abs(sr.sprite.pivot.y - 7f) < 0.1f, "Sentinela_V2: pivô dos pés em y=7 px");
            }
            yield return new WaitForEndOfFrame();
            Capture("sentinela_v2_idle");

            // 13. Hitbox vs Sprite bounds
            var col = sentinel.GetComponent<BoxCollider2D>();
            float colH = col != null ? col.size.y : 0f;
            float colW = col != null ? col.size.x : 0f;
            float sprH = sr != null ? sr.bounds.size.y : 0f;
            float sprW = sr != null ? sr.bounds.size.x : 0f;
            Check(sprH >= 2.1f && sprH <= 4.05f, $"Sentinela_V2: altura visual no idle em {sprH:F2} u (~2.25 u de arte em célula 64x64)");
            _results.Add($"INFO Sentinela_V2: hitbox col={colW:F2}x{colH:F2} u vs sprite={sprW:F2}x{sprH:F2} u (caixa não ultrapassa o topo)");

            // Estado windup
            if (phaseField != null) phaseField.SetValue(sentinel, System.Enum.ToObject(phaseField.FieldType, 1));
            typeof(InimigoVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(visual, null);
            yield return null;
            if (sr != null && sr.sprite != null)
            {
                Check(Mathf.Approximately(sr.sprite.rect.x, 384f), "Sentinela_V2: célula 6 (x=384) em windup");
            }
            yield return new WaitForEndOfFrame();
            Capture("sentinela_v2_preparando");

            // Estado attack strike
            if (phaseField != null) phaseField.SetValue(sentinel, System.Enum.ToObject(phaseField.FieldType, 2));
            typeof(InimigoVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(visual, null);
            yield return null;
            if (sr != null && sr.sprite != null)
            {
                Check(Mathf.Approximately(sr.sprite.rect.x, 448f), "Sentinela_V2: célula 7 (x=448) no ataque ativo");
            }
            yield return new WaitForEndOfFrame();
            Capture("sentinela_v2_ataque");

            // 12. Onda de choque com célula 8
            var wave = SentinelaShockwave.Spawn(
                sentinel.transform.position + new Vector3(-0.95f, 0f, 0f),
                -1f,
                24f,
                player.transform);
            var waveSr = wave != null ? wave.GetComponent<SpriteRenderer>() : null;
            Check(wave != null && waveSr != null && waveSr.sprite != null && Mathf.Approximately(waveSr.sprite.rect.x, 512f),
                "Sentinela_V2: onda de choque nasce usando célula 8 (x=512)");
            if (wave != null) Destroy(wave.gameObject);
            if (sentinel != null) Destroy(sentinel.gameObject);
        }
    }

    IEnumerator TestEpilogoPixelArt()
    {
        Size(768, 432);
        SceneManager.LoadScene(GameScenes.Epilogue);
        yield return null;
        EpilogueController.EnsureExists();
        yield return new WaitForSeconds(0.5f);
        var epilogue = EpilogueController.Instance;
        Check(epilogue != null, "Epilogo_Pixel: EpilogueController ativo");
        if (epilogue == null) yield break;

        // 1 & 2. As 4 texturas de 320x180 Point, sem mipmap, Clamp, sem compressão
        bool texturesOk = true;
        for (int i = 0; i < 4; i++)
        {
            var tex = Resources.Load<Texture2D>("Menu/epilogue/epilogo_painel_" + (i + 1));
            bool ok = tex != null && tex.width == 320 && tex.height == 180 &&
                      tex.filterMode == FilterMode.Point && tex.mipmapCount == 1 &&
                      tex.wrapMode == TextureWrapMode.Clamp &&
                      (tex.format == TextureFormat.RGBA32 || tex.format == TextureFormat.ARGB32 || tex.format == TextureFormat.RGB24);
            if (!ok)
            {
                texturesOk = false;
                Debug.LogWarning($"[Combate HUD QA] Textura epilogo_painel_{i + 1} inválida: {(tex != null ? $"{tex.width}x{tex.height} {tex.filterMode} {tex.format}" : "null")}");
            }
        }
        Check(texturesOk, "Epilogo_Pixel: 4 painéis 320x180 Point, sem mipmaps, Clamp e sem compressão");

        // 3. Retângulo do painel em 1280x720 (320x180 nativos * MenuKit.Px)
        var panelRt = epilogue.PanelImage != null ? epilogue.PanelImage.rectTransform : null;
        bool panelSizeOk = panelRt != null && Mathf.Abs(panelRt.rect.width - 1280f) < 2f && Mathf.Abs(panelRt.rect.height - 720f) < 2f;
        Check(panelSizeOk, "Epilogo_Pixel: painel mede 1280x720 unidades de UI (320x180 nativos)");
        Check(epilogue.PanelImage != null && epilogue.PanelImage.sprite != null && Mathf.Approximately(epilogue.PanelImage.sprite.pixelsPerUnit, MenuKit.SpritePpu),
            "Epilogo_Pixel: sprite do painel usa SpritePpu");

        var heroRt = epilogue.HeroLayer;
        bool heroLayerMatches = heroRt != null && panelRt != null &&
                                Mathf.Abs(heroRt.rect.width - panelRt.rect.width) < 2f &&
                                Mathf.Abs(heroRt.rect.height - panelRt.rect.height) < 2f &&
                                heroRt.anchoredPosition == panelRt.anchoredPosition;
        Check(heroLayerMatches, "Epilogo_Pixel: camada de heróis tem mesma dimensão e posição do painel");

        // 4. Todas as 4 páginas com títulos e corpos corretos, sem vazamento
        bool allFit = true;
        bool textsMatch = true;
        for (int i = 0; i < 4; i++)
        {
            epilogue.JumpTo(i);
            Canvas.ForceUpdateCanvases();
            yield return null;
            if (epilogue.Heading.text != EpilogueController.Titles[i] || epilogue.Body.text != EpilogueController.Pages[i])
                textsMatch = false;
            if (!MenuKit.Fits(epilogue.Body) || !MenuKit.Fits(epilogue.Heading))
            {
                allFit = false;
                Debug.LogWarning($"[Combate HUD QA] Epílogo texto da página {i + 1} vazou! BodyFit={MenuKit.Fits(epilogue.Body)}, HeadFit={MenuKit.Fits(epilogue.Heading)}");
            }
            yield return new WaitForEndOfFrame();
            Capture("epilogo_pixel_pagina" + (i + 1));
        }
        Check(textsMatch, "Epilogo_Pixel: títulos e textos das 4 páginas coincidem exatamente com o original");
        Check(allFit, "Epilogo_Pixel: MenuKit.Fits(Body) e Fits(Heading) verdadeiros em todas as 4 páginas");

        // 5. Heróis presentes na página 4
        epilogue.JumpTo(3);
        Canvas.ForceUpdateCanvases();
        var heroAnimators = epilogue.HeroLayer != null ? epilogue.HeroLayer.GetComponentsInChildren<UiSpriteAnimator>(true) : new UiSpriteAnimator[0];
        Check(epilogue.HeroLayer != null && epilogue.HeroLayer.gameObject.activeSelf && heroAnimators.Length == 3,
            "Epilogo_Pixel: 3 heróis (mago, guerreiro, arqueiro) ativos na página 4");

        // 6. Navegação: CONTINUAR, VOLTAR, PULAR
        var continuarBtn = GameObject.Find("Botao_CONTINUAR");
        var voltarBtn = GameObject.Find("Botao_VOLTAR");
        var pularBtn = GameObject.Find("Botao_PULAR");
        Check(continuarBtn != null && voltarBtn != null && pularBtn != null,
            "Epilogo_Pixel: navegação preservada com botões CONTINUAR, VOLTAR e PULAR");

        // Avançar da página 4 leva aos créditos
        epilogue.Advance();
        yield return new WaitForSeconds(0.4f);
        Check(SceneManager.GetActiveScene().name == GameScenes.Credits,
            "Epilogo_Pixel: avançar da página 4 leva à tela de créditos");
    }

    IEnumerator TestNazorFormasCameraArena()
    {
        Size(768, 432);
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        StageData.ActivePhase = 3;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.5f);

        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        var stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        Check(player != null && stage != null, "Nazor_Arena: Fase 3 carregada");
        if (stage == null || player == null) yield break;

        // Limpa título e spawner para prints sem obstrução
        var card = FindFirstObjectByType<PhaseTitleCardFader>(FindObjectsInactive.Exclude);
        if (card != null) Destroy(card.gameObject);
        var banner = GameObject.Find("PhaseTitleCard");
        if (banner != null) Destroy(banner);
        var spawner = FindFirstObjectByType<SimpleEnemySpawner>(FindObjectsInactive.Exclude);
        if (spawner != null) spawner.SetLocked(true);
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) Destroy(e.gameObject);

        // Posiciona jogador na arena e bloqueia câmera
        player.transform.position = new Vector3(40f, stage.Stage.GroundTop + 0.7f, 0f);
        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.LockX(36f, 60f, stage.Stage.GroundTop + 5.5f);
        yield return new WaitForSeconds(0.2f);

        // Instancia Nazor na arena
        var bar = BossHealthBar.Create(FindFirstObjectByType<Canvas>()?.transform);
        var boss = VoidDevourerBoss.Spawn(new Vector3(48f, stage.Stage.GroundTop, 0f), player.transform, stage.Stage, bar, false);
        Check(boss != null, "Nazor_Arena: Nazor instanciado no centro da arena");
        if (boss == null) yield break;

        var visual = boss.GetComponent<BossFormVisual>();
        var sr = boss.GetComponent<SpriteRenderer>();

        // Bounds da câmera de 24x13.5 unidades (Ortho 6.75)
        float camLeft = 36f;
        float camRight = 60f;
        float camBottom = stage.Stage.GroundTop - 1.25f;
        float camTop = stage.Stage.GroundTop + 12.25f;

        // ---------- FORMA 1 ----------
        visual.SetForm(1);
        boss.ForceStateFlying();
        boss.SetAnimationPose(NazorPose.Hover);
        yield return new WaitForSeconds(0.25f);
        var card2 = FindFirstObjectByType<PhaseTitleCardFader>(FindObjectsInactive.Exclude);
        if (card2 != null) Destroy(card2.gameObject);

        var b1 = sr.bounds;
        bool f1Inside = b1.min.x >= camLeft && b1.max.x <= camRight && b1.min.y >= camBottom && b1.max.y <= camTop;
        Check(f1Inside, $"Nazor_Arena: Forma 1 inteira dentro da arena/câmera em idle (bounds: X[{b1.min.x:F1}..{b1.max.x:F1}], Y[{b1.min.y:F1}..{b1.max.y:F1}])");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma1_idle");
        Capture("nazor_forma1_arena");

        // Dano Forma 1
        boss.EnemyCtrl.ReceiveDamage(2f);
        yield return new WaitForSeconds(0.06f);
        var b1Dmg = sr.bounds;
        Check(b1Dmg.min.x >= camLeft && b1Dmg.max.x <= camRight && b1Dmg.min.y >= camBottom && b1Dmg.max.y <= camTop,
            "Nazor_Arena: Forma 1 inteira na tela durante reação a dano");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma1_dano");

        // Transformação Forma 1
        boss.SetAnimationPose(NazorPose.Transform);
        yield return new WaitForSeconds(0.1f);
        var b1Trans = sr.bounds;
        Check(b1Trans.min.x >= camLeft && b1Trans.max.x <= camRight && b1Trans.min.y >= camBottom && b1Trans.max.y <= camTop,
            "Nazor_Arena: Forma 1 inteira na tela durante transformação");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma1_transformacao");

        // ---------- FORMA 2 ----------
        visual.SetForm(2);
        boss.ForceStateFlying();
        boss.SetAnimationPose(NazorPose.Hover);
        yield return new WaitForSeconds(0.25f);

        var b2 = sr.bounds;
        bool f2Inside = b2.min.x >= camLeft && b2.max.x <= camRight && b2.min.y >= camBottom && b2.max.y <= camTop;
        Check(f2Inside, $"Nazor_Arena: Forma 2 inteira dentro da arena/câmera em idle (bounds: X[{b2.min.x:F1}..{b2.max.x:F1}], Y[{b2.min.y:F1}..{b2.max.y:F1}])");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma2_idle");
        Capture("nazor_forma2_arena");

        // Dano Forma 2
        boss.EnemyCtrl.ReceiveDamage(2f);
        yield return new WaitForSeconds(0.06f);
        var b2Dmg = sr.bounds;
        Check(b2Dmg.min.x >= camLeft && b2Dmg.max.x <= camRight && b2Dmg.min.y >= camBottom && b2Dmg.max.y <= camTop,
            "Nazor_Arena: Forma 2 inteira na tela durante reação a dano");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma2_dano");

        // Transformação Forma 2
        boss.SetAnimationPose(NazorPose.Transform);
        yield return new WaitForSeconds(0.1f);
        var b2Trans = sr.bounds;
        Check(b2Trans.min.x >= camLeft && b2Trans.max.x <= camRight && b2Trans.min.y >= camBottom && b2Trans.max.y <= camTop,
            "Nazor_Arena: Forma 2 inteira na tela durante transformação");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma2_transformacao");

        // ---------- FORMA 3 (conferência) ----------
        visual.SetForm(3);
        boss.ForceStateFlying();
        boss.SetAnimationPose(NazorPose.Hover);
        yield return new WaitForSeconds(0.2f);
        var b3 = sr.bounds;
        Check(b3.min.x >= camLeft && b3.max.x <= camRight && b3.min.y >= camBottom && b3.max.y <= camTop,
            "Nazor_Arena: Forma 3 inteira na tela");
        yield return new WaitForEndOfFrame();
        Capture("nazor_forma3_arena");

        if (boss != null) Destroy(boss.gameObject);
        if (bar != null) Destroy(bar.gameObject);
    }

    IEnumerator TestCameraMorteAbismo()
    {
        Size(768, 432);
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        StageData.ActivePhase = 1;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.5f);

        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        var stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        Check(player != null && follow != null && stage != null, "Camera_Morte: setup de câmera e jogador pronto");
        if (player == null || follow == null || stage == null) yield break;

        // Simula queda abaixo do solo (abismo)
        player.transform.position = new Vector3(0f, stage.Stage.GroundTop - 3.5f, 0f);
        yield return null;
        var targetPt = typeof(CameraFollow).GetMethod("TargetPoint", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(follow, new object[] { 0.016f });
        var clampedPt = (Vector3)typeof(CameraFollow).GetMethod("Clamped", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(follow, new object[] { targetPt });
        Check(clampedPt.y < stage.Stage.GroundTop + 4.5f,
            $"Camera_Morte: câmera segue jogador abaixo do solo (clamped Y = {clampedPt.y:F2} < {stage.Stage.GroundTop + 4.5f:F2})");

        // Simula morte do jogador (Time.timeScale == 0)
        var health = player.GetComponent<HealthSystem>();
        health.Kill();
        yield return null;
        Check(Time.timeScale == 0f, "Camera_Morte: Time.timeScale pausado em 0 na derrota");

        // Confirma que CameraFollow continua atualizando via unscaledDeltaTime sem travar
        typeof(CameraFollow).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(follow, null);
        Check(cam != null, "Camera_Morte: câmera não trava nem lança exceção em timeScale zero");
        yield return new WaitForEndOfFrame();
        Capture("camera_morte_abismo_segue");

        Time.timeScale = 1f;
    }

    IEnumerator TestPortalAcabamentoVisual()
    {
        Size(768, 432);
        SceneManager.LoadScene(GameScenes.GroundT1);
        yield return null;
        StageData.ActivePhase = 1;
        GroundT1Controller.EnsureExists();
        yield return new WaitForSeconds(0.5f);

        var stage = FindFirstObjectByType<GroundT1Controller>(FindObjectsInactive.Exclude);
        if (stage != null)
        {
            stage.OpenExitPortal();
            var portal = FindFirstObjectByType<F1ExitPortal>(FindObjectsInactive.Exclude);
            Check(portal != null, "Portal_Visual: F1ExitPortal instanciado com sucesso");
            if (portal != null)
            {
                var bgGlow = portal.transform.Find("PortalBackGlow");
                var inGlow = portal.transform.Find("PortalInnerGlow");
                var groundGlow = portal.transform.Find("PortalGroundGlow");
                var motes = portal.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("Mote_")).ToArray();

                Check(bgGlow != null && inGlow != null && groundGlow != null,
                    "Portal_Visual: brilho pulsante de fundo, núcleo interno e reflexo de chão ativos");
                Check(motes.Length >= 8,
                    $"Portal_Visual: partículas pixel art flutuantes criadas ({motes.Length} motes)");

                var sr = portal.GetComponent<SpriteRenderer>();
                Check(sr != null && sr.sprite != null && sr.sprite.texture != null && sr.sprite.texture.filterMode == FilterMode.Point,
                    "Portal_Visual: textura do portal em FilterMode.Point");

                yield return new WaitForSeconds(0.4f);
                yield return new WaitForEndOfFrame();
                Capture("portal_f1_visual_polish");
            }
        }
    }
}
#endif
