using UnityEngine;

public class ForceUnlockCursor : MonoBehaviour
{
    private void Update()
    {
        // Ép chuột luôn hiện và tự do di chuyển để test Joystick
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}