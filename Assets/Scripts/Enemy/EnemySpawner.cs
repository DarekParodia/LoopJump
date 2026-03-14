using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EnemyEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnChance = 1f;
    public int minLevel = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    public int currentLevel = 1;
    public int baseEnemyCount = 1;
    public int extraPerLevel = 2;
    public float spawnInterval = 1.5f;
    public float spawnRadius = 10f;
    public int maxAlive = 10;

    [Header("Enemies")]
    public List<EnemyEntry> enemies = new();

    [Header("Events")]
    public UnityEvent OnLevelComplete;
    public UnityEvent<GameObject> OnEnemySpawned;

    private int toSpawn;
    private int defeated;
    private List<GameObject> alive = new();
    private Coroutine _spawnCoroutine;

    public void StartLevel(int level)
    {
        Debug.Log($"[EnemySpawner] ========== StartLevel({level}) called ==========");

        if (_spawnCoroutine != null)
        {
            Debug.Log($"[EnemySpawner] Stopping previous spawn coroutine before starting level {level}");
            StopCoroutine(_spawnCoroutine);
        }

        currentLevel = level;
        defeated = 0;
        toSpawn = baseEnemyCount + extraPerLevel * (level - 1);

        Debug.Log($"[EnemySpawner] Level {level} config — baseEnemyCount={baseEnemyCount}, extraPerLevel={extraPerLevel}");
        Debug.Log($"[EnemySpawner] Will spawn {toSpawn} enemies this wave (formula: {baseEnemyCount} + {extraPerLevel} * ({level}-1))");

        alive.RemoveAll(e => e == null);
        Debug.Log($"[EnemySpawner] Alive list cleaned — {alive.Count} enemies still alive from previous wave");

        if (enemies == null || enemies.Count == 0)
            Debug.LogError("[EnemySpawner] enemies list is EMPTY — nothing will spawn!");
        else
            Debug.Log($"[EnemySpawner] Enemy pool has {enemies.Count} entry/entries");

        _spawnCoroutine = StartCoroutine(SpawnRoutine());
        Debug.Log($"[EnemySpawner] SpawnRoutine coroutine started for level {level}");
    }

    private IEnumerator SpawnRoutine()
    {
        int spawned = 0;
        Debug.Log($"[EnemySpawner] SpawnRoutine — BEGIN. toSpawn={toSpawn}");

        while (spawned < toSpawn)
        {
            alive.RemoveAll(e => e == null);
            Debug.Log($"[EnemySpawner] SpawnRoutine — tick | spawned={spawned}/{toSpawn} | alive={alive.Count}/{maxAlive}");

            if (alive.Count < maxAlive)
            {
                var prefab = PickEnemy();
                if (prefab != null)
                {
                    Vector2 rnd = Random.insideUnitCircle * spawnRadius;
                    Vector3 pos = transform.position + new Vector3(rnd.x, 0f, rnd.y);

                    Debug.Log($"[EnemySpawner] Spawning '{prefab.name}' at {pos} (spawned {spawned + 1}/{toSpawn})");

                    var enemy = Instantiate(prefab, pos, Quaternion.identity);
                    alive.Add(enemy);
                    spawned++;

                    Debug.Log($"[EnemySpawner] '{enemy.name}' instantiated. alive.Count={alive.Count}");
                    OnEnemySpawned?.Invoke(enemy);
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawner] PickEnemy() returned null — no valid enemy for level {currentLevel}. Skipping spawn tick.");
                }
            }
            else
            {
                Debug.Log($"[EnemySpawner] SpawnRoutine — maxAlive cap hit ({alive.Count}/{maxAlive}), waiting...");
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"[EnemySpawner] SpawnRoutine — all {toSpawn} enemies spawned. Waiting for defeats...");
    }

    public void EnemyDefeated(GameObject enemy)
    {
        alive.Remove(enemy);
        defeated++;

        Debug.Log($"[EnemySpawner] EnemyDefeated — '{enemy.name}' reported dead. defeated={defeated}/{toSpawn} | alive.Count={alive.Count}");

        if (defeated >= toSpawn)
        {
            Debug.Log($"[EnemySpawner] *** ALL {toSpawn} ENEMIES DEFEATED — firing OnLevelComplete for level {currentLevel} ***");
            OnLevelComplete?.Invoke();
            Debug.Log($"[EnemySpawner] OnLevelComplete invoked. Listener count: {OnLevelComplete.GetPersistentEventCount()} persistent listeners");
        }
        else
        {
            Debug.Log($"[EnemySpawner] {toSpawn - defeated} enemies remaining before wave complete");
        }
    }

    private GameObject PickEnemy()
    {
        List<EnemyEntry> valid = enemies.FindAll(e =>
            e.prefab != null && currentLevel >= e.minLevel);

        Debug.Log($"[EnemySpawner] PickEnemy — {valid.Count}/{enemies.Count} entries valid for level {currentLevel}");

        if (valid.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] PickEnemy — no valid enemies for level {currentLevel}! Check minLevel settings.");
            return null;
        }

        float total = 0f;
        foreach (var e in valid) total += e.spawnChance;

        float roll = Random.Range(0f, total);
        float sum = 0f;

        Debug.Log($"[EnemySpawner] PickEnemy — total weight={total:F2}, roll={roll:F2}");

        foreach (var e in valid)
        {
            sum += e.spawnChance;
            if (roll <= sum)
            {
                Debug.Log($"[EnemySpawner] PickEnemy — selected '{e.prefab.name}'");
                return e.prefab;
            }
        }

        Debug.Log($"[EnemySpawner] PickEnemy — fell through to last entry '{valid[^1].prefab.name}'");
        return valid[^1].prefab;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}