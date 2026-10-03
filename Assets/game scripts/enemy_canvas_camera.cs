using UnityEngine;

public class enemy_canvas_camera : MonoBehaviour
{
    public Camera cam; // hoặc public Transform cam;

    void Awake()
    {
        // Tự động tìm Main Camera nếu biến cam bị bỏ trống trong Inspector
        if (cam == null)
        {
            cam = Camera.main; 
            // Nếu bạn dùng kiểu Transform cho cam: cam = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        // Đảm bảo cam không bị null trước khi xử lý
        if (cam != null)
        {
            // Code xoay Canvas theo Camera của bạn ở đây...
            transform.forward = cam.transform.forward;
        }
    }
}