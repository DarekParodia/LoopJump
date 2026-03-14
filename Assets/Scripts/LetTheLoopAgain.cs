using UnityEngine;
using UnityEngine.SceneManagement;
public class LetTheLoopAgain : MonoBehaviour
{
   public void Again()
   {
      SceneManager.LoadScene("Game");
   }

   public void Quit()
   {
      Application.Quit();
   }
}
