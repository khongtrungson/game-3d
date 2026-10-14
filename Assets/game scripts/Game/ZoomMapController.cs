using UnityEngine;
using UnityEngine.UI;
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
    [Tooltip("Tốc độ zoom khi cuộn chuột (PC)")]
    public float scrollZoomSpeed = 5f;

    [Tooltip("Tốc độ zoom bằng 2 ngón tay (Mobile)")]
    public float touchZoomSpeed = 0.05f;

    [Tooltip("Giới hạn zoom nhỏ nhất (nhìn gần)")]
    public float minZoomSize = 10f;

    [Tooltip("Giới hạn zoom lớn nhất (nhìn xa)")]
    public float maxZoomSize = 80f;

    [Header("Tùy chọn khác")]
    [Tooltip("Có dừng thời gian (pause game) khi bật full map không?")]
    public bool pauseGameWhenOpenMap = false;

    private bool isFullMapOpen = false;

    void Start()
    {
        if (smallMinimapUI != null) smallMinimapUI.SetActive(true);
        if (fullMapUI != null) fullMapUI.SetActive(false);
        if (backgroundBlurUI != null) backgroundBlurUI.SetActive(false);

        SetupUIListeners();
    }

    private void SetupUIListeners()
    {
        // 1. Tự động tìm kiếm nếu chưa gán tham chiếu
        if (smallMinimapUI == null)
        {
            GameObject found = GameObject.Find("MiniMap") ?? GameObject.Find("smallMinimapUI") ?? GameObject.Find("Minimap");
            if (found != null) smallMinimapUI = found;
        }

        // 2. Gắn sự kiện click mở Full Map khi bấm vào minimap nhỏ ở góc màn hình
        if (smallMinimapUI != null)
        {
            AttachClickListener(smallMinimapUI, OpenFullMap);
        }

        // 3. Gắn sự kiện click đóng Full Map khi bấm vào background che mờ ngoài bản đồ
        if (backgroundBlurUI != null)
        {
            AttachClickListener(backgroundBlurUI, CloseFullMap);
        }

        // 4. Tìm các nút đóng bản đồ nếu có trong fullMapUI (nút Close, Exit, Back)
        if (fullMapUI != null)
        {
            Button[] buttons = fullMapUI.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string bName = b.name.ToLowerInvariant();
                if (bName.Contains("close") || bName.Contains("back") || bName.Contains("exit") || bName.Contains("btn_close"))
                {
                    b.onClick.RemoveListener(CloseFullMap);
                    b.onClick.AddListener(CloseFullMap);
                }
            }
        }
    }

    private void AttachClickListener(GameObject target, UnityEngine.Events.UnityAction action)
    {
        if (target == null) return;

        // Đảm bảo Graphic cho phép nhận Raycast cảm ứng
        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            g.raycastTarget = true;
        }

        // Gắn hoặc lấy component Button
        Button btn = target.GetComponent<Button>();
        if (btn == null)
        {
            btn = target.AddComponent<Button>();
        }
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);

        // Gắn IPointerClickHandler trực tiếp để bắt touch chắc chắn 100%
        MinimapTouchTrigger trigger = target.GetComponent<MinimapTouchTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<MinimapTouchTrigger>();
        }
        trigger.onPointerClickAction = action;
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
            // 1. XỬ LÝ ZOOM TRÊN PC: Cuộn chuột (ScrollWheel)
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                ApplyCameraZoom(-scrollInput * scrollZoomSpeed);
            }

            // 2. XỬ LÝ ZOOM TRÊN MOBILE (Pinch to Zoom - vuốt 2 ngón tay)
            if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                // Tìm vị trí khung hình trước đó của 2 ngón tay
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                // Tính khoảng cách giữa 2 ngón tay ở frame trước và frame hiện tại
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Tính độ chênh lệch khoảng cách (dương: ngón tay thu lại gần -> zoom out / nhìn xa hơn)
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // Thay đổi độ zoom của Camera theo khoảng cách 2 ngón tay
                ApplyCameraZoom(deltaMagnitudeDiff * touchZoomSpeed);
            }
        }
    }

    private void ApplyCameraZoom(float delta)
    {
        if (fullMapCamera == null) return;

        if (fullMapCamera.orthographic)
        {
            fullMapCamera.orthographicSize += delta;
            fullMapCamera.orthographicSize = Mathf.Clamp(fullMapCamera.orthographicSize, minZoomSize, maxZoomSize);
        }
        else
        {
            fullMapCamera.fieldOfView += delta;
            fullMapCamera.fieldOfView = Mathf.Clamp(fullMapCamera.fieldOfView, minZoomSize, maxZoomSize);
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

        if (fullMapCamera != null)
        {
            fullMapCamera.gameObject.SetActive(true);
        }

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

/// <summary>
/// Helper bắt touch/click cho minimap UI
/// </summary>
public class MinimapTouchTrigger : MonoBehaviour, IPointerClickHandler
{
    public UnityEngine.Events.UnityAction onPointerClickAction;

    public void OnPointerClick(PointerEventData eventData)
    {
        onPointerClickAction?.Invoke();
    }
}