using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool spawnPlayerOnStart = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (spawnPlayerOnStart)
            {
            SpawnPlayer();
            }
    }

    public void SpawnPlayer()
    {
        Instantiate(playerPrefab, transform.position, Quaternion.identity);
    }
}
