using UnityEngine;

public class LevelCounter : MonoBehaviour
{
    public int currentLevel = 1;
    public EnemySpawner spawner;

    private bool EnsureSpawner()
    {
        if (spawner != null) return true;

        spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            Debug.Log($"[LevelCounter] Found EnemySpawner via FindObjectOfType: '{spawner.gameObject.name}'");
            return true;
        }

        Debug.LogError("[LevelCounter] ERROR - No EnemySpawner found in scene!");
        return false;
    }

    void Awake()
    {
        Debug.Log($"[LevelCounter] Awake — spawner assigned in Inspector: {(spawner != null ? spawner.gameObject.name : "NULL")}");
        EnsureSpawner();
    }

    void Start()
    {
        currentLevel = 1;
        Debug.Log($"[LevelCounter] Start — kicking off level {currentLevel}");

        if (!EnsureSpawner())
        {
            Debug.LogError("[LevelCounter] Start aborted because spawner is still null.");
            return;
        }

        spawner.StartLevel(currentLevel);
    }

    public void nextLevel()
    {
        Debug.Log($"[LevelCounter] nextLevel() called — incrementing from {currentLevel} to {currentLevel + 1}");

        if (!EnsureSpawner())
        {
            Debug.LogError("[LevelCounter] nextLevel() aborted because spawner is null.");
            return;
        }

        currentLevel++;
        Debug.Log($"[LevelCounter] currentLevel is now {currentLevel} — calling spawner.StartLevel({currentLevel})");
        spawner.StartLevel(currentLevel);
        Debug.Log($"[LevelCounter] spawner.StartLevel({currentLevel}) returned successfully");
    }
}