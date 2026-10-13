using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomMapController : MonoBehaviour
{
    [Header("UI & Camera References")]
    [Tooltip("Panel hoặc GameObject chứa minimap ở góc nhỏ")]
    public GameObject smallMinimapUI;

    [Tooltip("Panel hoặc GameObject chứa bản đồ toàn màn hình")]
    public GameObject fullMapUI;

    [Tooltip("Tấm image/panel che mờ nền bên ngoài bản đồ")]
    public GameObject backgroundBlurUI;

    [Tooltip("Camera chuyên dùng cho bản đồ lớn (Full Map Camera)")]
    public Camera fullMapCamera; // Kéo camera full map vào đây để zoom

    [Header("Zoom Settings")]
    [Tooltip("Tốc độ zoom khi giữ Z + cuộn chuột (PC)")]
    public float scrollZoomSpeed = 5f;

    [Tooltip("Tốc độ zoom bằng 2 ngón tay (Mobile)")]
    public float touchZoomSpeed = 0.1f;

    [Tooltip("Giới hạn zoom nhỏ nhất (nhìn gần)")]
    public float minZoomSize = 10f;

    [Tooltip("Giới hạn zoom lớn nhất (nhìn xa)")]
    public float maxZoomSize = 50f;

    [Header("Tùy chọn khác")]
    [Tooltip("Có dừng thời gian (pause game) khi bật full map không?")]
    public bool pauseGameWhenOpenMap = false;

    private bool isFullMapOpen = false;

    void Start()
    {
        if (smallMinimapUI != null) smallMinimapUI.SetActive(true);
        if (fullMapUI != null) fullMapUI.SetActive(false);
        if (backgroundBlurUI != null) backgroundBlurUI.SetActive(false);
    }

    void Update()
    {
        // Trên PC: Bấm phím 'M' để bật/tắt bản đồ lớn
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleFullMap();
        }

        // Nếu bản đồ lớn đang mở, xử lý tính năng Zoom
        if (isFullMapOpen && fullMapCamera != null)
        {
            // 1. XỬ LÝ ZOOM TRÊN PC: Giữ phím 'Z' + Lăn chuột
            if (Input.GetKey(KeyCode.LeftShift))
            {
                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                if (scrollInput != 0f)
                {
                    fullMapCamera.orthographicSize -= scrollInput * scrollZoomSpeed;
                    // Giới hạn kích thước zoom trong khoảng cho phép
                    fullMapCamera.orthographicSize = Mathf.Clamp(fullMapCamera.orthographicSize, minZoomSize, maxZoomSize);
                }
            }

            // 2. XỬ LÝ ZOOM TRÊN MOBILE (Pinch to Zoom - 2 ngón tay)
            if (Application.isMobilePlatform && Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                // Tìm vị trí khung hình trước đó của 2 ngón tay
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                // Tính khoảng cách giữa 2 ngón tay ở frame trước và frame hiện tại
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Tính độ chênh lệch khoảng cách
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // Thay đổi orthographicSize dựa trên khoảng cách 2 ngón tay
                fullMapCamera.orthographicSize += deltaMagnitudeDiff * touchZoomSpeed;
                fullMapCamera.orthographicSize = Mathf.Clamp(fullMapCamera.orthographicSize, minZoomSize, maxZoomSize);
            }
        }
    }

    public void ToggleFullMap()
    {
        isFullMapOpen = !isFullMapOpen;

        if (isFullMapOpen)
        {
            OpenFullMap();
        }
        else
        {
            CloseFullMap();
        }
    }

    public void OpenFullMap()
    {
        isFullMapOpen = true;
        if (smallMinimapUI != null) smallMinimapUI.SetActive(false);
        if (fullMapUI != null) fullMapUI.SetActive(true);
        if (backgroundBlurUI != null) backgroundBlurUI.SetActive(true);

        if (pauseGameWhenOpenMap)
        {
            Time.timeScale = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseFullMap()
    {
        isFullMapOpen = false;
        if (smallMinimapUI != null) smallMinimapUI.SetActive(true);
        if (fullMapUI != null) fullMapUI.SetActive(false);
        if (backgroundBlurUI != null) backgroundBlurUI.SetActive(false);

        if (pauseGameWhenOpenMap)
        {
            Time.timeScale = 1f;
        }

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnClickOutsideMap()
    {
        if (isFullMapOpen)
        {
            CloseFullMap();
        }
    }
}