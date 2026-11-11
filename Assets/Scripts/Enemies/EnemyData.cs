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
    // Fase 2 — Horda 1: Sentinela de Escombro (surge da terra com aviso de 0.5s e onda de choque de pedra)
    public static EnemyData CreateRubbleSentinel() => Make("sentinela_escombro", "Sentinela de Escombro", "burrow_slam",
        new Vector2(1.25f, 1.7f), 68f, 2.2f, 18f, 2.6f, 0.50f, 0.90f, new Color(0.55f, 0.48f, 0.68f));
    // Fase 2 — Horda 2: Arauto de Brasa (voador a meia altura: mergulho rasante + bola de brasa diagonal)
    public static EnemyData CreateEmberHerald() => Make("arauto_brasa", "Arauto de Brasa", "ember_flyer",
        new Vector2(1.15f, 1.55f), 52f, 3.1f, 15f, 6.2f, 0.50f, 0.90f, new Color(0.88f, 0.42f, 0.28f));

    // Fase 3 — Cavaleiro Corrompido (140 HP, investida com escudo que reduz 70% de frente + golpe por cima com onda de choque)
    public static EnemyData CreateCorruptedKnight() => Make("cavaleiro_corrompido", "Cavaleiro Corrompido", "knight_charge_slam",
        new Vector2(1.1f, 1.55f), 140f, 2.2f, 18f, 5.0f, 0.5f, 1.0f, new Color(0.45f, 0.25f, 0.65f));

    // Fase 3 — Cuspidor do Vazio (60 HP, mantém distância de 6-8u, cospe ácido em arco gerando poça de 3s)
    public static EnemyData CreateVoidSpitter() => Make("cuspidor_vazio", "Cuspidor do Vazio", "void_spit",
        new Vector2(0.95f, 0.95f), 60f, 2.0f, 14f, 7.5f, 0.45f, 1.2f, new Color(0.65f, 0.25f, 0.85f));

    // Compatibilidade com chamadas anteriores: nunca instancia os ataques antigos.
    public static EnemyData CreateBasic() => CreateAlienSkeleton();
    public static EnemyData CreateSkeleton() => CreateAlienSkeleton();
    public static EnemyData CreateGhoul() => CreateBeast();
    public static EnemyData CreateZombie() => CreateSlateGolem();
}
