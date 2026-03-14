using UnityEngine;
using System.Collections;
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
   
   void Start()
   {
      StartCoroutine(Loadscene());
   }

   IEnumerator Loadscene()
   {
      yield return new WaitForSeconds(3f);
      SceneManager.LoadScene("Game");
   }
   
}

