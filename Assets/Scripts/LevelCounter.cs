using UnityEngine;

public class LevelCounter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int _currentLevel = 0;

    public void nextLevel()
    {
        _currentLevel++;   
        Debug.Log(_currentLevel);
    }

    public int GetCurrentLevel()
    {
        return _currentLevel;
    }
}
