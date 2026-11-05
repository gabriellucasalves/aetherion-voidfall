#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Smoke tests com física real em Play Mode, sem framework ou código de teste no WebGL.
public class F1PlayModeSmoke : MonoBehaviour
{
    readonly List<string> _results = new List<string>();
    int _failed;
    PlayerController _player;
    Rigidbody2D _playerBody;
    StageData _stage;
    const string Output = "/tmp/aetherion-f1-qa";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!SessionState.GetBool("F1Smoke.Enabled", false)) return;
        SessionState.SetBool("F1Smoke.Enabled", false);
        new GameObject("F1PlayModeSmoke").AddComponent<F1PlayModeSmoke>();
    }

    IEnumerator Start()
    {
        var routine = Run();
        while (true)
        {
            object current = null;
            bool next = false;
            try { next = routine.MoveNext(); if (next) current = routine.Current; }
            catch (System.Exception e) { Check(false, "Exceção: " + e); Finish(); yield break; }
            if (!next) break;
            yield return current;
        }
        Finish();
    }

    void Check(bool pass, string name)
    {
        _results.Add((pass ? "PASS " : "FAIL ") + name);
        if (!pass) _failed++;
        Debug.Log("[F1 QA] " + _results[_results.Count - 1]);
    }
    void Finish()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Output + "/playmode-results.txt", _results);
        EditorPrefs.SetBool(T1HybridScenery.EditorPrefKey, SessionState.GetBool("F1Smoke.OldHybrid", true));
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey, SessionState.GetBool("F1Smoke.OldAtmos", true));
        Debug.Log("[F1 QA] FINAL " + (_failed == 0 ? "PASS" : "FAIL") + " cases=" + _results.Count);
        EditorApplication.Exit(_failed == 0 ? 0 : 1);
    }
    EnemyController Enemy(EnemyData data, float x, float feetY, bool idle = true)
    {
        var e = SimpleEnemySpawner.SpawnAt(data, _player.transform, _stage, new Vector2(x, feetY), _player.GetComponent<Collider2D>());
        e.Health.Configure(1000f);
        if (idle) { e.enabled = false; e.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; }
        return e;
    }
    void ClearEnemies()
    {
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(e.gameObject);
        foreach (var p in FindObjectsByType<AetherBone>(FindObjectsSortMode.None)) Destroy(p.gameObject);
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(0.15f);
        _stage = StageData.CreateT1();
        _player = FindFirstObjectByType<PlayerController>();
        Check(_player != null && T1FrontLevel.Built, "Ground_T1 híbrido inicia em Play Mode");
        if (_player == null) yield break;
        FindFirstObjectByType<SimpleEnemySpawner>().SetLocked(true);
        _playerBody = _player.GetComponent<Rigidbody2D>();
        _playerBody.bodyType = RigidbodyType2D.Kinematic;
        _player.enabled = false;
        _player.GetComponent<HealthSystem>().IsInvulnerable = true;
        _player.transform.position = new Vector3(0, _stage.GroundTop + 0.575f, 0);
        var combat = _player.GetComponent<PlayerCombat>();
        var meter = _player.GetComponent<ResonanceMeter>();
        var shield = _player.GetComponent<ShieldSystem>();
        Check(!combat.TryFireWarriorSpecial(), "Feixe rejeita barra vazia");
        var near = Enemy(EnemyData.CreateAlienSkeleton(), 3, _stage.GroundTop);
        var behind = Enemy(EnemyData.CreateAlienSkeleton(), -3, _stage.GroundTop);
        var above = Enemy(EnemyData.CreateAlienSkeleton(), 3, _stage.GroundTop + 2);
        var far = Enemy(EnemyData.CreateAlienSkeleton(), 9, _stage.GroundTop);
        Physics2D.SyncTransforms();
        meter.Add(1000);
        float shieldBefore = shield.Current;
        Check(combat.TryFireWarriorSpecial() && meter.Current == 0, "Feixe consome 100% de ESPECIAL");
        Check(shield.Current == shieldBefore && !SpecialController.IsCinematic && Time.timeScale == 1,
            "Feixe sem custo de escudo, cutscene ou alteração de tempo");
        Check(!combat.TryFireWarriorSpecial(), "Não duplica disparo durante cooldown");
        yield return new WaitForSeconds(0.08f);
        Check(near.Health.Current == 1000f, "Startup não causa dano");
        yield return new WaitForSeconds(0.16f);
        float firstHit = near.Health.Current;
        Check(Mathf.Abs(firstHit - (1000f - 90f * InfernalBeam.DamageScale)) < 0.1f, "Dano frontal do Feixe");
        Check(behind.Health.Current == 1000 && above.Health.Current == 1000 && far.Health.Current == 1000,
            "Feixe não atinge atrás, plataforma acima ou fora de alcance");
        Directory.CreateDirectory(Output);
        yield return new WaitForSeconds(0.4f);
        Check(near.Health.Current == firstHit, "Um hit por inimigo, mesmo com dois colisores");
        Check(FindFirstObjectByType<InfernalBeam>() == null, "Feixe encerra após janela ativa");
        InfernalBeam.Launch(_player.transform, Vector2.left, 10);
        yield return new WaitForSeconds(0.25f);
        Check(behind.Health.Current == 990 && near.Health.Current == firstHit, "Facing esquerdo respeitado");
        yield return new WaitForSeconds(0.4f);
        ClearEnemies(); yield return null;

        foreach (var data in new[] { EnemyData.CreateAlienSkeleton(), EnemyData.CreateBeast(), EnemyData.CreateSlateGolem() })
        {
            Check(Resources.Load<Texture2D>("Inimigos/" + data.Id) != null, data.DisplayName + " tem sheet nativa");
            Check(EnemyTraversal.TrySpawnPoint(31, _stage, data.Size, out var top) && top.y > _stage.GroundTop + 3.5f,
                data.DisplayName + " spawn com apoio em ilha alta");
            Check(!EnemyTraversal.TrySpawnPoint(-25, _stage, data.Size, out _), data.DisplayName + " rejeita spawn em gap");
            foreach (var pair in new[] { new Vector2(-26,-24),new Vector2(-17,-14),new Vector2(-7,-4),new Vector2(13,16),new Vector2(24,27) })
            {
                _player.transform.position = new Vector3(pair.y + 5, _stage.GroundTop + 0.575f, 0);
                var e = Enemy(data, pair.x - 1.1f, _stage.GroundTop + 0.02f, false);
                e.enabled = false; // Testa o navegador sem ataque interferir na medição.
                var nav = e.GetComponent<EnemyTraversal>();
                bool reached = false;
                for (float t = 0; t < 3.5f; t += Time.fixedDeltaTime)
                {
                    nav.Move(1, data.MoveSpeed, false);
                    yield return new WaitForFixedUpdate();
                    if (e == null) break;
                    if (e.transform.position.x > pair.y + 0.35f && e.transform.position.y >= _stage.GroundTop - 0.1f) { reached = true; break; }
                }
                Check(reached, data.DisplayName + " atravessa gap " + pair.x + ".." + pair.y);
                if (e != null) Destroy(e.gameObject);
                yield return null;
            }
            // Subida 0 -> 2 -> 3,75 por colisores reais.
            var climber = Enemy(data, -18, _stage.GroundTop + 0.02f, false);
            climber.enabled = false;
            var traversal = climber.GetComponent<EnemyTraversal>();
            bool climbed = false;
            for (float t = 0; t < 6f; t += Time.fixedDeltaTime)
            {
                traversal.Move(1, data.MoveSpeed, true);
                yield return new WaitForFixedUpdate();
                if (climber == null) break;
                if (climber.transform.position.y > _stage.GroundTop + 3.6f && traversal.Grounded) { climbed = true; break; }
            }
            Check(climbed, data.DisplayName + " sobe pelas ilhas 2,0 e 3,75");
            if (climber != null) Destroy(climber.gameObject);
            yield return null;
        }
        ClearEnemies(); yield return null;
        _player.transform.position = new Vector3(0, _stage.GroundTop + 0.575f, 0);
        _player.transform.position = new Vector3(5, _stage.GroundTop + 0.575f, 0);
        var skeleton = Enemy(EnemyData.CreateAlienSkeleton(), 0, _stage.GroundTop + 0.02f, false);
        yield return new WaitForSeconds(0.65f);
        Check(FindFirstObjectByType<AetherBone>() != null, "Esqueleto Alien dispara osso Aether");
        ClearEnemies(); yield return null;
        _player.transform.position = new Vector3(0, _stage.GroundTop + 0.575f, 0);
        skeleton = Enemy(EnemyData.CreateAlienSkeleton(), -1.5f, _stage.GroundTop + 0.02f, false);
        yield return new WaitForSeconds(0.2f);
        Check(skeleton.transform.position.x < -1.6f, "Esqueleto Alien recua para manter distância");
        ClearEnemies(); yield return null;
        var beast = Enemy(EnemyData.CreateBeast(), -2, _stage.GroundTop + 0.02f, false);
        yield return new WaitForSeconds(0.4f);
        Check(beast.GetComponent<Rigidbody2D>().linearVelocity.y > 0.5f, "Bicho executa salto de ataque");
        ClearEnemies(); yield return null;
        var golem = Enemy(EnemyData.CreateSlateGolem(), -3, _stage.GroundTop + 0.02f, false);
        yield return new WaitForSeconds(0.2f);
        Check(golem.IsWindingUp && Mathf.Abs(golem.GetComponent<Rigidbody2D>().linearVelocity.x) < 0.1f,
            "Golem telegrafa parado antes da investida");
        yield return new WaitForSeconds(0.6f);
        Check(golem != null && golem.GetComponent<Rigidbody2D>().linearVelocity.x > 7f && golem.Health.Current == 1000,
            "Golem investe sem explodir nem se autodestruir");
        Check(Mathf.Abs(golem.Data.Size.y - 1.7f) < 0.01f, "Golem mantém altura base do Guerreiro, com largura maior");
        ClearEnemies(); yield return null;
        var stage = FindFirstObjectByType<GroundT1Controller>();
        stage.OpenExitPortal();
        yield return null;
        Check(FindFirstObjectByType<F1ExitPortal>() != null && Time.timeScale == 1, "Portal abre sem encerrar a fase imediatamente");
        _playerBody.position = new Vector2(32.5f, _stage.GroundTop + 0.575f);
        yield return new WaitForSecondsRealtime(0.2f);
        Check(Time.timeScale == 0, "Atravessar portal conclui Fase I");
    }
}
#endif
