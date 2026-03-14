using UnityEngine;

public class LevelDoor : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int minimalWave = 1;
    [SerializeField] private string levelCounterTag = "LevelCounter";

    private LevelCounter _levelCounter;

    void Start()
    {
        GameObject lc = GameObject.FindWithTag(levelCounterTag);

        if (lc == null)
        {
            Debug.LogError($"[LevelDoor:{gameObject.name}] No object found with tag '{levelCounterTag}'.");
            enabled = false;
            return;
        }

        _levelCounter = lc.GetComponent<LevelCounter>();
        if (_levelCounter == null)
        {
            Debug.LogError($"[LevelDoor:{gameObject.name}] Object '{lc.name}' has no LevelCounter component.");
            enabled = false;
            return;
        }

        UpdateDoorState();
    }

    void Update()
    {
        UpdateDoorState();
    }

    private void UpdateDoorState()
    {
        if (_levelCounter == null) return;

        if (_levelCounter.currentLevel >= minimalWave)
        {
            gameObject.SetActive(false);
        }
    }
}
