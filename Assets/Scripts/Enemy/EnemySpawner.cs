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
        // Stop any ongoing spawn routine from previous wave
        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);

        currentLevel = level;
        defeated = 0;
        toSpawn = baseEnemyCount + extraPerLevel * (level - 1);
        alive.RemoveAll(e => e == null);

        Debug.Log(
            $"[EnemySpawner] Starting level {level} — "
                + $"spawning {toSpawn} enemies"
        );

        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        int spawned = 0;

        while (spawned < toSpawn)
        {
            alive.RemoveAll(e => e == null);

            if (alive.Count < maxAlive)
            {
                var prefab = PickEnemy();
                if (prefab != null)
                {
                    Vector2 rnd = Random.insideUnitCircle * spawnRadius;
                    Vector3 pos =
                        transform.position
                        + new Vector3(rnd.x, 0f, rnd.y);

                    var enemy =
                        Instantiate(prefab, pos, Quaternion.identity);
                    alive.Add(enemy);
                    spawned++;
                    OnEnemySpawned?.Invoke(enemy);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void EnemyDefeated(GameObject enemy)
    {
        alive.Remove(enemy);
        defeated++;

        Debug.Log(
            $"[EnemySpawner] Enemy defeated: {defeated}/{toSpawn}"
        );

        if (defeated >= toSpawn)
        {
            Debug.Log(
                $"[EnemySpawner] Level {currentLevel} complete!"
            );
            OnLevelComplete?.Invoke();
        }
    }

    private GameObject PickEnemy()
    {
        List<EnemyEntry> valid = enemies.FindAll(e =>
            e.prefab != null && currentLevel >= e.minLevel
        );

        if (valid.Count == 0)
            return null;

        float total = 0f;
        foreach (var e in valid)
            total += e.spawnChance;

        float roll = Random.Range(0f, total);
        float sum = 0f;

        foreach (var e in valid)
        {
            sum += e.spawnChance;
            if (roll <= sum)
                return e.prefab;
        }

        return valid[^1].prefab;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}