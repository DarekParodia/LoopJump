using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{

    public GameObject credits;

    public void Start()
    {
        credits.SetActive(false);
    }
    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void OpenCredits()
    {
        credits.SetActive(true);
    }
    public void CloseGame()
    {
        

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
    public void ExitPanels()
    {
        credits.SetActive(false);
    }

}

