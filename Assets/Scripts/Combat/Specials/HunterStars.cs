using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sete Estrelas do Caçador: salto, 7 flechas teleguiadas e explosão atrasada.
public static class HunterStars
{
    public const int ArrowCount = 7;
    public const int MaxMarksPerTarget = 3;
    public const float ExplodeDelay = 1.2f;

    public static IEnumerator Run(Transform owner, CharacterData hero, PlayerController player, PlayerCombat combat)
    {
        if (owner == null)
            yield break;

        var body = owner.GetComponent<Rigidbody2D>();
        if (player != null)
            player.SetLocked(false);
        if (body != null)
            body.linearVelocity = new Vector2(0f, 10f);
        if (combat != null)
            combat.RechargeDash();

        yield return new WaitForSeconds(0.25f);

        var targets = new List<EnemyController>();
        var all = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var enemy = all[i];
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                continue;
            if (!OnScreen(enemy.transform.position))
                continue;
            targets.Add(enemy);
        }

        targets.Sort((a, b) => b.Health.Current.CompareTo(a.Health.Current));

        float agility = hero != null ? hero.Agility : 100f;
        float hit = agility * 0.25f;
        var launched = new Dictionary<EnemyController, int>();

        if (targets.Count > 0)
        {
            int arrows = 0;
            int guard = 0;
            while (arrows < ArrowCount && guard < targets.Count * MaxMarksPerTarget + 4)
            {
                var target = targets[guard % targets.Count];
                guard++;
                if (target == null)
                    continue;
                int already = 0;
                launched.TryGetValue(target, out already);
                if (already >= MaxMarksPerTarget)
                    continue;

                launched[target] = already + 1;
                HomingStarArrow.Launch(owner.position + Vector3.up * 0.85f, target, hit, arrows);
                arrows++;
                yield return new WaitForSeconds(0.04f);
            }

            float wait = 0f;
            while (HomingStarArrow.Alive > 0 && wait < 2.2f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(ExplodeDelay);

        float power = hero != null ? hero.Power : 65f;
        float burst = power * 1.2f + agility * 0.4f;
        var gold = new Color(1f, 0.839f, 0.251f);
        int marked = 0;

        var markedEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < markedEnemies.Length; i++)
        {
            var enemy = markedEnemies[i];
            if (enemy == null || enemy.VitalMarks <= 0)
                continue;
            marked++;
            int marks = enemy.VitalMarks;
            Vector3 at = enemy.transform.position + Vector3.up * 0.8f;
            for (int k = 0; k < marks; k++)
                enemy.ReceiveDamage(burst);
            PixelBurst.Spawn(at, gold, 5);
            enemy.ClearVitalMarks();
        }

        if (marked >= 3)
            SupremeFx.Popup(owner.position + Vector3.up * 1.6f, "…Vocês já caíram.", gold);
    }

    static bool OnScreen(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null)
            return true;
        var view = cam.WorldToViewportPoint(world + Vector3.up * 0.6f);
        return view.z > 0f && view.x > -0.08f && view.x < 1.08f && view.y > -0.15f && view.y < 1.15f;
    }
}
