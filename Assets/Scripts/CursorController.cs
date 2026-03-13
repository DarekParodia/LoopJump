using UnityEngine;

public class CursorController : MonoBehaviour
{
    private void Awake(){
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
