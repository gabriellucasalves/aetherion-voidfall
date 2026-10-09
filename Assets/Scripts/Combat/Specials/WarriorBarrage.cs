using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Juramento da Muralha Rubra: sentinela de magma, 12 golpes e estocada de escudo.
public static class WarriorBarrage
{
    static readonly List<EnemyController> Hits = new List<EnemyController>(8);
    static readonly Color Magma = new Color(1f, 0.38f, 0.08f, 1f);
    static readonly Color Gold = new Color(1f, 0.84f, 0.27f, 1f);

    public static IEnumerator Run(Transform owner, CharacterData hero, PlayerController player)
    {
        if (owner == null)
            yield break;

        Vector2 facing = player != null ? player.Facing : Vector2.right;
        if (Mathf.Abs(facing.x) < 0.01f)
            facing = Vector2.right;
        facing = new Vector2(Mathf.Sign(facing.x), 0f);

        var sentinel = SpawnSentinel(owner);
        float strength = hero != null ? hero.Strength : 90f;
        float hit = strength * 0.16f;
        var box = new Vector2(3.6f, 2.2f);
        Vector2 center = (Vector2)owner.position + facing * 2.0f + Vector2.up * 0.6f;

        for (int i = 0; i < 12; i++)
        {
            DamageBox(center, box, hit, facing, 0f);
            PixelBurst.Spawn(center + Random.insideUnitCircle * 0.7f, Magma, 3);
            if (i % 3 == 0)
                SupremeFx.Popup(center + Vector2.up * (0.4f + (i % 2) * 0.35f), "ROMPE!", Gold);
            yield return new WaitForSeconds(0.11f);
        }

        DamageBox(center, box * 1.15f, strength * 1.1f, facing, 1.5f);
        PixelBurst.Spawn(center, new Color(0.85f, 0.18f, 0.12f), 8);
        PixelBurst.Spawn(center + Vector3.up * 0.3f, Gold, 4);
        SupremeFx.Popup(owner.position + Vector3.up * 1.7f, "MURALHAAA… RUBRA!", Gold);

        var shield = owner.GetComponent<ShieldSystem>();
        if (shield != null)
            shield.Refill();

        if (sentinel != null)
            Object.Destroy(sentinel, 0.35f);
    }

    static void DamageBox(Vector2 center, Vector2 size, float damage, Vector2 facing, float stun)
    {
        Hits.Clear();
        var cols = Physics2D.OverlapBoxAll(center, size, 0f);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;
            var enemy = cols[i].GetComponentInParent<EnemyController>();
            if (enemy == null || Hits.Contains(enemy))
                continue;
            Hits.Add(enemy);
            enemy.ReceiveDamage(damage);
            if (stun > 0f)
                enemy.Knockback(facing * 6f, stun);
        }
    }

    static GameObject SpawnSentinel(Transform owner)
    {
        var sprite = HeroCutInSprites.Frame("guerreiro", 17);
        if (sprite == null)
            sprite = HeroCutInSprites.PlaceholderBust("guerreiro");

        var go = new GameObject("SentinelaMagma");
        go.transform.SetParent(owner, false);
        // Atrás do herói (local -X). O root já vira com a facing.
        go.transform.localPosition = new Vector3(-0.55f, -0.5f, 0f);
        go.transform.localScale = new Vector3(3f, 3f, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.34f, 0.06f, 0.6f);
        renderer.sortingOrder = 11;
        PixelBurst.Spawn(owner.position + Vector3.up * 0.4f, Magma, 6);
        return go;
    }
}
