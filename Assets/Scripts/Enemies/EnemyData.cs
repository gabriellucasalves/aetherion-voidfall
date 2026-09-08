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

    // ataque especial: "axe" (machado arremessado), "acid" (bola ácida) ou "explode" (kamikaze)
    public string AttackKind = "axe";
    public float AttackRange = 1.5f;
    public float AttackDamage = 12f;
    public float AttackWindup = 0.45f;
    public float AttackRecover = 0.6f;

    // resistência: multiplica o dano recebido (1 = normal, <1 = tanque, >1 = frágil)
    public float DamageTaken = 1f;

    public static EnemyData CreateBasic()
    {
        return CreateSkeleton();
    }

    // básico — arremessa machados giratórios de média distância
    public static EnemyData CreateSkeleton()
    {
        var data = CreateInstance<EnemyData>();
        data.Id = "esqueleto";
        data.DisplayName = "Esqueleto Guerreiro";
        data.MaxHealth = 36f;
        data.MoveSpeed = 2.35f;
        data.TouchDamage = 9f;
        data.TouchCooldown = 0.85f;
        data.Size = new Vector2(1.0f, 1.7f);
        data.Color = new Color(0.58f, 0.54f, 0.45f);
        data.AttackKind = "axe";
        data.AttackRange = 6.5f;   // arremessa de média distância
        data.AttackDamage = 11f;
        data.AttackWindup = 0.45f;
        data.AttackRecover = 1.3f;
        data.DamageTaken = 1f;     // resistência normal
        return data;
    }

    // veloz (o "sapo") — frágil, chega rápido e cospe bolas ácidas
    public static EnemyData CreateGhoul()
    {
        var data = CreateInstance<EnemyData>();
        data.Id = "ghoul";
        data.DisplayName = "Ghoul Veloz";
        data.MaxHealth = 22f;
        data.MoveSpeed = 3.7f;
        data.TouchDamage = 7f;
        data.TouchCooldown = 0.7f;
        data.Size = new Vector2(1.7f, 1.0f);
        data.Color = new Color(0.3f, 0.25f, 0.34f);
        data.AttackKind = "acid";
        data.AttackRange = 5.5f;   // cospe em arco de média distância
        data.AttackDamage = 9f;
        data.AttackWindup = 0.35f; // infla antes de cuspir
        data.AttackRecover = 1.1f;
        data.DamageTaken = 1.4f;   // frágil: morre rápido se você acertar
        return data;
    }

    // resistente — tanque kamikaze: chega perto, pisca 0.5s e explode
    public static EnemyData CreateZombie()
    {
        var data = CreateInstance<EnemyData>();
        data.Id = "zumbi";
        data.DisplayName = "Zumbi Corrompido";
        data.MaxHealth = 72f;
        data.MoveSpeed = 1.4f;
        data.TouchDamage = 13f;
        data.TouchCooldown = 1.05f;
        data.Size = new Vector2(1.2f, 1.7f);
        data.Color = new Color(0.24f, 0.28f, 0.2f);
        data.AttackKind = "explode";
        data.AttackRange = 1.7f;   // distância que arma a explosão
        data.AttackDamage = 24f;   // dano em área (raio 2.1)
        data.AttackWindup = 0.5f;  // pisca vermelho antes de estourar
        data.AttackRecover = 0f;   // não sobra nada pra recuperar
        data.DamageTaken = 0.65f;  // tanque: aguenta muito mais golpes
        return data;
    }
}
