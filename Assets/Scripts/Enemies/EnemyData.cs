using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Aetherion/Enemy")]
public class EnemyData : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public float MaxHealth = 35f;
    public float MoveSpeed = 2.3f;
    public float TouchDamage = 10f;
    public float TouchCooldown = 0.85f;
    public Vector2 Size = new Vector2(0.7f, 0.7f);
    public Color Color = new Color(0.42f, 0.16f, 0.48f);
    // bone = distância; claws = salto/garras; charge = investida telegrafada.
    public string AttackKind = "bone";
    public float AttackRange = 1.5f;
    public float AttackDamage = 12f;
    public float AttackWindup = 0.45f;
    public float AttackRecover = 0.6f;
    public float DamageTaken = 1f;

    static EnemyData Make(string id, string name, string attack, Vector2 size,
        float health, float speed, float damage, float range, float windup, float recovery, Color color)
    {
        var d = CreateInstance<EnemyData>();
        d.Id = id; d.DisplayName = name; d.AttackKind = attack; d.Size = size;
        d.MaxHealth = health; d.MoveSpeed = speed; d.AttackDamage = damage;
        d.AttackRange = range; d.AttackWindup = windup; d.AttackRecover = recovery; d.Color = color;
        return d;
    }
    public static EnemyData CreateAlienSkeleton() => Make("esqueleto_alien", "Esqueleto Alien", "bone",
        new Vector2(0.9f, 1.7f), 42f, 2.5f, 11f, 7f, 0.45f, 1.2f, new Color(0.48f, 0.78f, 0.76f));
    public static EnemyData CreateBeast() => Make("bicho", "Bicho", "claws",
        new Vector2(1.25f, 1.25f), 34f, 3.8f, 14f, 2.4f, 0.3f, 0.7f, new Color(0.53f, 0.32f, 0.62f));
    public static EnemyData CreateSlateGolem()
    {
        var d = Make("golem_ardosia", "Golem de Ardósia", "charge", new Vector2(1.45f, 1.7f),
            100f, 1.65f, 23f, 5f, 0.7f, 1.1f, new Color(0.32f, 0.42f, 0.54f));
        d.DamageTaken = 0.65f;
        return d;
    }
    // Compatibilidade com chamadas anteriores: nunca instancia os ataques antigos.
    public static EnemyData CreateBasic() => CreateAlienSkeleton();
    public static EnemyData CreateSkeleton() => CreateAlienSkeleton();
    public static EnemyData CreateGhoul() => CreateBeast();
    public static EnemyData CreateZombie() => CreateSlateGolem();
}
