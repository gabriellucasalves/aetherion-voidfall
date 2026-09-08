using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Aetherion/Character")]
public class CharacterData : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string Role;
    public string Description;
    public int MaxHealth;
    public int Strength;
    public int Power;
    public int Defense;
    public int Agility;
    public int AttackSpeed;
    public string AbilityName;
    public string AbilityDescription;
    public Color Accent = Color.white;
}
