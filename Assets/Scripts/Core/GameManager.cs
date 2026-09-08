using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] string _selectedHeroId;
    CharacterData _selectedHero;

    public string SelectedHeroId => _selectedHeroId;
    public CharacterData SelectedHero => _selectedHero;

    public static GameManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var root = new GameObject("GameManager");
        root.AddComponent<SceneTransitionManager>();
        return root.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GetComponent<SceneTransitionManager>() == null)
            gameObject.AddComponent<SceneTransitionManager>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SelectHero(CharacterData hero)
    {
        _selectedHero = hero;
        _selectedHeroId = hero != null ? hero.Id : null;
    }
}
