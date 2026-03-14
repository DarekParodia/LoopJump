using UnityEngine;

public class LevelCounter : MonoBehaviour
{
    public int currentLevel = 1;
    public EnemySpawner spawner;

    void Awake()
    {
        if (spawner == null)
            spawner = FindObjectOfType<EnemySpawner>();
    }

    void Start()
    {
        currentLevel = 1;
        spawner.StartLevel(currentLevel);
    }

    public void nextLevel()
    {
        currentLevel++;
        Debug.Log($"[LevelCounter] Level → {currentLevel}");
        spawner.StartLevel(currentLevel);
    }
}