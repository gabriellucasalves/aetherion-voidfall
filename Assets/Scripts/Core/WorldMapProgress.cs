using UnityEngine;

/// <summary>Small, additive save for the world map. PlayerPrefs uses browser storage in WebGL.</summary>
public static class WorldMapProgress
{
    const string Prefix = "aetherion.worldmap.v1.";
    const string CompletedKey = Prefix + "completed";
    const string HeroKey = Prefix + "hero";
    const string NodeKey = Prefix + "node";
    const string GameCompleteKey = Prefix + "game_complete";

    public static int HighestCompleted => Mathf.Clamp(PlayerPrefs.GetInt(CompletedKey, 0), 0, 3);
    public static string SelectedHeroId => PlayerPrefs.GetString(HeroKey, "");
    public static int CurrentNode => Mathf.Clamp(PlayerPrefs.GetInt(NodeKey, 1), 1, 3);
    public static bool GameComplete => PlayerPrefs.GetInt(GameCompleteKey, 0) == 1;

    public static bool IsCompleted(int phase) => phase >= 1 && phase <= HighestCompleted;
    public static bool IsUnlocked(int phase) => phase >= 1 && phase <= Mathf.Min(3, HighestCompleted + 1);

    public static void Complete(int phase)
    {
        // Only the next available phase advances progress; revisits cannot skip content.
        if (phase < 1 || phase > 3 || !IsUnlocked(phase)) return;
        if (phase > HighestCompleted)
        {
            PlayerPrefs.SetInt(CompletedKey, phase);
        }
        // A replay returns to the phase just completed, even if a later node was saved.
        // Unlock progress remains monotonic, while the return position follows this run.
        PlayerPrefs.SetInt(NodeKey, phase);
        PlayerPrefs.Save();
    }

    // Called only after the third boss form and its final defeat dialogue.
    public static void ConfirmFinalBossDefeated()
    {
        if (!IsCompleted(3)) return;
        PlayerPrefs.SetInt(GameCompleteKey, 1);
        PlayerPrefs.Save();
    }

    public static void RememberNode(int node)
    {
        if (!IsUnlocked(node)) return;
        PlayerPrefs.SetInt(NodeKey, node);
        PlayerPrefs.Save();
    }

    public static void RememberHero(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        PlayerPrefs.SetString(HeroKey, id);
        PlayerPrefs.Save();
    }
}
