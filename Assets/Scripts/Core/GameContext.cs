using UnityEngine;

public class GameContext : MonoBehaviour
{
    public GameConfig config;
    public FactionDatabase factions;
    public FactionService factionService;
    public TimeManager timeManager;

    void Awake()
    {
        if (factionService != null) factionService.Init();
        if (timeManager != null) timeManager.config = config;
    }
}
