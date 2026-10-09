using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Supernova Arcana: congela inimigos, guarda o dano e estoura até 7 estrelas.
public static class VoidTimeStop
{
    public const float FreezeDuration = 2.5f;
    public const float FreezeRadius = 8f;
    public const int MaxStars = 7;

    public static bool ProjectilesHeld { get; private set; }

    static readonly List<EnemyController> Frozen = new List<EnemyController>(16);

    public static bool TickFreeze(Rigidbody2D body, ref bool held, ref Vector2 velocity, ref float gravity)
    {
        if (ProjectilesHeld)
        {
            if (!held && body != null)
            {
                held = true;
                velocity = body.linearVelocity;
                gravity = body.gravityScale;
                body.linearVelocity = Vector2.zero;
                body.gravityScale = 0f;
            }
            return true;
        }

        if (held && body != null)
        {
            held = false;
            body.gravityScale = gravity;
            body.linearVelocity = velocity;
        }
        return false;
    }

    public static void ReleaseAll()
    {
        ProjectilesHeld = false;
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].IsFrozen)
                enemies[i].SetFrozen(false);
        }
        Frozen.Clear();
    }

    public static IEnumerator Run(Transform owner, CharacterData hero)
    {
        if (owner == null)
            yield break;

        ProjectilesHeld = true;
        Frozen.Clear();

        var all = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var enemy = all[i];
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                continue;
            if (Vector2.Distance(owner.position, enemy.transform.position) > FreezeRadius)
                continue;
            enemy.SetFrozen(true);
            Frozen.Add(enemy);
        }

        PixelBurst.Spawn(owner.position + Vector3.up * 0.6f, new Color(0.65f, 0.45f, 1f), 8);

        float duration = FreezeDuration;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 origin = owner.position;
        Frozen.Sort((a, b) =>
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;
            int byStored = b.StoredDamage.CompareTo(a.StoredDamage);
            if (byStored != 0)
                return byStored;
            float da = Vector2.Distance(origin, a.transform.position);
            float db = Vector2.Distance(origin, b.transform.position);
            return da.CompareTo(db);
        });

        int stars = Mathf.Min(MaxStars, Frozen.Count);
        var gold = new Color(1f, 0.84f, 0.35f);
        var nova = new Color(0.86f, 0.55f, 1f);
        for (int i = 0; i < stars; i++)
        {
            var a = Frozen[i];
            var b = Frozen[(i + 1) % stars];
            if (a != null && b != null && a != b)
                SupremeFx.Link(a.transform.position, b.transform.position, gold);
        }

        SupremeFx.Popup(owner.position + Vector3.up * 1.8f, "SUPERNOVA… ARCANA!", nova);
        yield return new WaitForSeconds(0.12f);

        // As estrelas estouram ainda no gelo: o dano fica guardado e entra junto quando o tempo solta.
        float damage = (hero != null ? hero.Power : 100f) * 1.4f;
        for (int i = 0; i < stars; i++)
        {
            var enemy = Frozen[i];
            if (enemy == null)
                continue;
            ArcaneNova.Detonate(enemy.transform.position + Vector3.up * 0.55f, 2.2f, damage, nova);
            yield return new WaitForSeconds(0.06f);
        }

        yield return new WaitForSeconds(0.25f);
        ProjectilesHeld = false;
        for (int i = 0; i < Frozen.Count; i++)
        {
            if (Frozen[i] != null)
                Frozen[i].SetFrozen(false);
        }

        Frozen.Clear();
        ProjectilesHeld = false;
    }
}
