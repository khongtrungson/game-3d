using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Quản lý điều khiển giao diện cảm ứng (Mobile UI Controls) cho người chơi.
/// Tự động kết nối các nút bấm trên Canvas với các hành vi của nhân vật:
/// - Btn_shoot: Bắn (Chạm bắn 1 phát, Giữ bắn liên thanh)
/// - Btn_Scope: Bật/Tắt chế độ ngắm (ADS / Scope)
/// - Btn_Reload: Nạp đạn (tương ứng phím R trên PC)
/// - Btn_Mekit: Sử dụng hộp cứu thương (tương ứng phím CapsLock trên PC)
/// - Btn_Jump: Nhảy (tương ứng phím Space trên PC)
/// - Btn_Bomb (tùy chọn): Ném lựu đạn (tương ứng phím G)
/// - Btn_Handgun (tùy chọn): Đổi vũ khí
/// </summary>
public class MobileInputController : MonoBehaviour
{
    private static MobileInputController instance;
    public static MobileInputController Instance => instance;

    [Header("=== Các nút bấm điều khiển Mobile ===")]
    [Tooltip("Nút bắn (Shoot)")]
    public Button btnShoot;

    [Tooltip("Nút ngắm (Scope / ADS)")]
    public Button btnScope;

    [Tooltip("Nút nạp đạn (Reload)")]
    public Button btnReload;

    [Tooltip("Nút hộp cứu thương (Medkit)")]
    public Button btnMekit;

    [Tooltip("Nút nhảy (Jump)")]
    public Button btnJump;

    [Header("=== Nút mở rộng (Tùy chọn) ===")]
    [Tooltip("Nút ném bom / lựu đạn")]
    public Button btnBomb;

    [Tooltip("Nút súng lục (Handgun - Weapon Index 1)")]
    public Button btnHandgun;

    [Tooltip("Nút súng trường (AR - Weapon Index 0)")]
    public Button btnAR;

    [Header("=== Tham chiếu Component Người Chơi ===")]
    public Character playerCharacter;
    public Movement playerMovement;
    public player_health playerHealth;
    public throw_grenade playerGrenade;

    [Header("=== Cài đặt giao diện ===")]
    [Tooltip("Màu đổi khi đang bật ngắm Scope")]
    public Color scopeActiveColor = new Color(0.3f, 1f, 0.4f, 1f);
    private Color scopeNormalColor = Color.white;
    private Image scopeImage;

    [Header("=== Điều khiển xoay Camera (Touch Look) ===")]
    [Tooltip("Độ nhạy xoay camera theo trục ngang (Yaw) khi vuốt nửa phải màn hình")]
    public float touchSensitivityX = 0.45f;
    [Tooltip("Độ nhạy xoay camera theo trục dọc (Pitch) khi vuốt nửa phải màn hình")]
    public float touchSensitivityY = 0.35f;

    private int rightTouchFingerId = -1;
    private Vector2 lastTouchPos;

    private Coroutine weaponClickCoroutine;
    private int pendingWeaponIndex = -1;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }

        // Tự động tìm tham chiếu nếu chưa được gán trong Inspector
        AutoFindReferences();
    }

    private void Start()
    {
        // Gắn lắng nghe sự kiện cho tất cả các nút
        SetupButtonListeners();
    }

    private void Update()
    {
        // Cập nhật trạng thái hiển thị màu nút Scope khi đang ngắm
        UpdateScopeVisual();

        // Xử lý vuốt nửa bên phải màn hình điện thoại để xoay camera 360 độ
        HandleTouchLook();
    }

    /// <summary>
    /// Xử lý vuốt ở nửa bên phải màn hình để xoay camera tự do 360 độ trên Mobile
    /// </summary>
    private void HandleTouchLook()
    {
        if (playerCharacter == null || Time.timeScale == 0f) return;

        // 1. Cảm ứng trên thiết bị Mobile thật
        if (Input.touchCount > 0)
        {
            Vector2 totalDelta = Vector2.zero;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Chỉ nhận ngón tay nằm ở nửa bên phải màn hình (> 45% chiều rộng)
                if (touch.position.x > Screen.width * 0.45f)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        // Kiểm tra không bắt đầu trên các nút UI
                        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        {
                            rightTouchFingerId = touch.fingerId;
                            lastTouchPos = touch.position;
                        }
                    }
                    else if (touch.fingerId == rightTouchFingerId)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            Vector2 delta = touch.deltaPosition;
                            totalDelta += new Vector2(delta.x * touchSensitivityX, delta.y * touchSensitivityY);
                            lastTouchPos = touch.position;
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            rightTouchFingerId = -1;
                        }
                    }
                }
            }

            if (totalDelta != Vector2.zero)
            {
                playerCharacter.AddMobileLookDelta(totalDelta);
            }
        }
        else
        {
            // 2. Hỗ trợ test bằng chuột trong Unity Editor khi kéo ở nửa phải màn hình
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.x > Screen.width * 0.45f)
                {
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                    {
                        lastTouchPos = Input.mousePosition;
                        rightTouchFingerId = 9999;
                    }
                }
            }
            else if (Input.GetMouseButton(0) && rightTouchFingerId == 9999)
            {
                Vector2 curPos = Input.mousePosition;
                Vector2 delta = curPos - lastTouchPos;
                lastTouchPos = curPos;

                if (delta != Vector2.zero)
                {
                    playerCharacter.AddMobileLookDelta(new Vector2(delta.x * touchSensitivityX, delta.y * touchSensitivityY));
                }
            }
            else if (Input.GetMouseButtonUp(0) && rightTouchFingerId == 9999)
            {
                rightTouchFingerId = -1;
            }
            #endif
        }
    }

    /// <summary>
    /// Tự động tìm kiếm các Button và Player Components trong Scene nếu chưa gán
    /// </summary>
    public void AutoFindReferences()
    {
        // 1. Tìm các Button theo tên
        if (btnShoot == null) btnShoot = FindButtonByName("Btn_shoot", "shoot", "BtnShoot");
        if (btnScope == null) btnScope = FindButtonByName("Btn_Scope", "scope", "BtnScope");
        if (btnReload == null) btnReload = FindButtonByName("Btn_Reload", "reload", "BtnReload");
        if (btnMekit == null) btnMekit = FindButtonByName("Btn_Mekit", "mekit", "medkit", "BtnMekit");
        if (btnJump == null) btnJump = FindButtonByName("Btn_Jump", "jump", "BtnJump");
        if (btnBomb == null) btnBomb = FindButtonByName("Btn_Bomb", "bomb", "grenade", "BtnBomb");
        if (btnHandgun == null) btnHandgun = FindButtonByName("Btn_Handgun", "Handgun", "Btn_Pistol", "Pistol");
        if (btnAR == null) btnAR = FindButtonByName("AR Body (Btn_AR)", "AR Body", "Btn_AR", "ARbody", "AR_Body", "AR");

        // 2. Tìm các Component của Player
        if (playerCharacter == null)
        {
            playerCharacter = FindAnyObjectByType<Character>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<Movement>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<player_health>();
        }

        if (playerGrenade == null)
        {
            playerGrenade = FindAnyObjectByType<throw_grenade>();
        }

        if (btnScope != null)
        {
            scopeImage = btnScope.GetComponent<Image>();
            if (scopeImage != null)
            {
                scopeNormalColor = scopeImage.color;
            }
        }
    }

    private Button FindButtonByName(params string[] names)
    {
        foreach (var btnName in names)
        {
            // 1. Ưu tiên tìm trong các con của GameObject hiện tại
            Transform childTransform = transform.Find(btnName);
            if (childTransform != null)
            {
                Button btn = childTransform.GetComponent<Button>() ?? childTransform.gameObject.AddComponent<Button>();
                var graphic = childTransform.GetComponent<Graphic>();
                if (graphic != null) graphic.raycastTarget = true;
                return btn;
            }

            // 2. Tìm kiếm trong Scene kể cả inactive objects
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.hideFlags != HideFlags.None && obj.hideFlags != HideFlags.NotEditable) continue;
                if (obj.scene.name == null) continue; // Bỏ qua prefab ngoài assets

                if (string.Equals(obj.name, btnName, StringComparison.OrdinalIgnoreCase) ||
                    obj.name.IndexOf(btnName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Button btn = obj.GetComponent<Button>() ?? obj.AddComponent<Button>();
                    var graphic = obj.GetComponent<Graphic>();
                    if (graphic != null) graphic.raycastTarget = true;
                    return btn;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Thiết lập sự kiện cho từng nút bấm
    /// </summary>
    private void SetupButtonListeners()
    {
        // 1. Nút Bắn (Shoot)
        if (btnShoot != null)
        {
            // Gắn component xử lý giữ nút để bắn liên thanh và nhả nút để ngừng
            MobileHoldButton holdHandler = btnShoot.GetComponent<MobileHoldButton>() ?? btnShoot.gameObject.AddComponent<MobileHoldButton>();

            holdHandler.onHoldChanged = (isHolding) =>
            {
                OnShootHold(isHolding);
            };

            btnShoot.onClick.RemoveListener(OnShootClick);
            btnShoot.onClick.AddListener(OnShootClick);
        }

        // 2. Nút Ngắm (Scope) - Ấn giữ để ngắm, nhả giữ thì hết ngắm
        if (btnScope != null)
        {
            btnScope.onClick.RemoveAllListeners();

            MobileHoldButton scopeHold = btnScope.GetComponent<MobileHoldButton>() ?? btnScope.gameObject.AddComponent<MobileHoldButton>();
            scopeHold.onHoldChanged = (isHolding) =>
            {
                OnScopeHold(isHolding);
            };
        }

        // 3. Nút Nạp Đạn (Reload - Phím R trên PC)
        if (btnReload != null)
        {
            btnReload.onClick.RemoveListener(OnReloadClick);
            btnReload.onClick.AddListener(OnReloadClick);
        }

        // 4. Nút Hồi Máu (Medkit - Phím CapsLock trên PC)
        if (btnMekit != null)
        {
            btnMekit.onClick.RemoveListener(OnMekitClick);
            btnMekit.onClick.AddListener(OnMekitClick);
        }

        // 5. Nút Nhảy (Jump - Phím Space trên PC)
        if (btnJump != null)
        {
            btnJump.onClick.RemoveListener(OnJumpClick);
            btnJump.onClick.AddListener(OnJumpClick);
        }

        // 6. Nút Ném Bom (Bomb/Grenade - Phím G trên PC)
        if (btnBomb != null)
        {
            btnBomb.onClick.RemoveListener(OnBombClick);
            btnBomb.onClick.AddListener(OnBombClick);
        }

        // 7. Nút Súng Trường (AR - Weapon Index 0)
        if (btnAR != null)
        {
            btnAR.onClick.RemoveAllListeners();
            btnAR.onClick.AddListener(() => OnWeaponButtonClicked(0));
        }

        // 8. Nút Súng Lục (Handgun - Weapon Index 1)
        if (btnHandgun != null)
        {
            btnHandgun.onClick.RemoveAllListeners();
            btnHandgun.onClick.AddListener(() => OnWeaponButtonClicked(1));
        }
    }

    #region HÀNH ĐỘNG NÚT BẤM (BUTTON ACTIONS)

    /// <summary>
    /// Bắn khi giữ/nhả nút (Hỗ trợ bắn liên thanh)
    /// </summary>
    public void OnShootHold(bool isHolding)
    {
        if (playerCharacter != null)
        {
            playerCharacter.MobileShoot(isHolding);
        }
    }

    /// <summary>
    /// Bắn 1 phát đơn (Khi tap nhanh nút bắn)
    /// </summary>
    public void OnShootClick()
    {
        if (playerCharacter != null)
        {
            playerCharacter.MobileSingleShoot();
        }
    }

    /// <summary>
    /// Ấn giữ để ngắm, nhả giữ thì hết ngắm (Hold to ADS)
    /// </summary>
    public void OnScopeHold(bool isHolding)
    {
        if (playerCharacter != null)
        {
            playerCharacter.SetAim(isHolding);
        }
    }

    /// <summary>
    /// Click nút ngắm (Scope / ADS)
    /// </summary>
    public void OnScopeClick()
    {
        // Cơ chế chính là ấn giữ (OnScopeHold)
    }

    /// <summary>
    /// Nạp đạn súng và nạp đầy lựu đạn (nade) nếu đứng gần hộp đạn bullet_kit (Reload - Phím R trên PC)
    /// </summary>
    public void OnReloadClick()
    {
        if (playerCharacter != null)
        {
            playerCharacter.TryReload();
        }

        // Đồng thời nạp lựu đạn (nade) khi đứng gần hộp tiếp tế bullet_kit
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            can_throw_grenade ctg = playerObj.GetComponent<can_throw_grenade>();
            if (ctg != null)
            {
                ctg.RefillGrenades();
            }
        }
    }

    /// <summary>
    /// Bơm máu / Nhặt hộp cứu thương (Medkit - Phím CapsLock trên PC)
    /// </summary>
    public void OnMekitClick()
    {
        if (playerHealth != null)
        {
            playerHealth.UseMedkit();
        }
    }

    /// <summary>
    /// Nhảy (Jump - Phím Space trên PC)
    /// </summary>
    public void OnJumpClick()
    {
        if (playerMovement != null)
        {
            playerMovement.MobileJump();
        }
    }

    /// <summary>
    /// Ném lựu đạn (Tương ứng phím G trên PC)
    /// </summary>
    public void OnBombClick()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            can_throw_grenade ctg = playerObj.GetComponent<can_throw_grenade>();
            if (ctg != null && ctg.can_throw)
            {
                if (playerGrenade == null)
                {
                    playerGrenade = playerObj.GetComponent<throw_grenade>();
                }

                if (playerGrenade != null)
                {
                    playerGrenade.throw_gre();
                    ctg.destro_one();
                }
            }
        }
    }

    /// <summary>
    /// Bấm nút AR (Súng trường)
    /// </summary>
    public void OnARClick()
    {
        OnWeaponButtonClicked(0);
    }

    /// <summary>
    /// Bấm nút Handgun (Súng lục)
    /// </summary>
    public void OnHandgunClick()
    {
        OnWeaponButtonClicked(1);
    }

    /// <summary>
    /// Xử lý bấm nút súng (AR hoặc Handgun).
    /// - Khi ấn vào súng nào thì hiện ra súng đó (nếu chưa trang bị hoặc đang cất).
    /// - Khi đang dùng súng này mà ấn tiếp 1 lần thì cất súng (tương ứng phím H).
    /// - Nhấn đúp nhanh 2 lần thì về trạng thái ngắm/kiểm tra súng (tương ứng phím T).
    /// - Sau đó ấn lại súng nào thì rút súng đó ra.
    /// </summary>
    public void OnWeaponButtonClicked(int weaponIndex)
    {
        if (playerCharacter == null) return;

        int currentEquipped = playerCharacter.GetEquippedWeaponIndex();
        bool isHolstered = playerCharacter.IsHolstered();

        // Trường hợp 1: Đang cầm súng khác -> Đổi sang súng này ngay lập tức
        if (currentEquipped != weaponIndex)
        {
            if (weaponClickCoroutine != null)
            {
                StopCoroutine(weaponClickCoroutine);
                weaponClickCoroutine = null;
                pendingWeaponIndex = -1;
            }

            playerCharacter.MobileEquipWeaponIndex(weaponIndex);
            return;
        }

        // Trường hợp 2: Đang trang bị súng này nhưng súng đang BỊ CẤT -> Rút súng ra ngay
        if (isHolstered)
        {
            if (weaponClickCoroutine != null)
            {
                StopCoroutine(weaponClickCoroutine);
                weaponClickCoroutine = null;
                pendingWeaponIndex = -1;
            }

            playerCharacter.MobileUnholster();
            return;
        }

        // Trường hợp 3: Đang cầm súng này trên tay -> Kiểm tra Single Tap hay Double Tap
        if (weaponClickCoroutine != null && pendingWeaponIndex == weaponIndex)
        {
            // Nhấn đúp nhanh 2 lần (Double Tap) -> Ngắm/Kiểm tra súng (tương ứng phím T trên PC)
            StopCoroutine(weaponClickCoroutine);
            weaponClickCoroutine = null;
            pendingWeaponIndex = -1;

            playerCharacter.MobileInspect();
        }
        else
        {
            // Click lần 1 -> Bắt đầu đếm thời gian chờ cú nhấp thứ 2
            if (weaponClickCoroutine != null)
            {
                StopCoroutine(weaponClickCoroutine);
            }
            pendingWeaponIndex = weaponIndex;
            weaponClickCoroutine = StartCoroutine(WaitWeaponSingleClickRoutine(weaponIndex));
        }
    }

    private System.Collections.IEnumerator WaitWeaponSingleClickRoutine(int weaponIndex)
    {
        yield return new WaitForSeconds(0.25f);

        weaponClickCoroutine = null;
        pendingWeaponIndex = -1;

        // Quá thời gian mà không có click thứ 2 -> Single Tap -> Cất súng (tương ứng phím H trên PC)
        if (playerCharacter != null)
        {
            playerCharacter.MobileToggleHolster();
        }
    }

    #endregion

    private void UpdateScopeVisual()
    {
        if (scopeImage == null || playerCharacter == null) return;

        bool isAiming = playerCharacter.IsAiming();
        scopeImage.color = isAiming ? scopeActiveColor : scopeNormalColor;
    }

    /// <summary>
    /// Tự động khởi tạo sau khi Scene được nạp (đảm bảo luôn hoạt động dù chưa kéo thả script)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (FindAnyObjectByType<MobileInputController>() == null)
        {
            GameObject btnShootObj = GameObject.Find("Btn_shoot");
            if (btnShootObj != null)
            {
                // Thêm vào cha chứa các nút (GameObject) hoặc chính nút đó
                GameObject parentContainer = btnShootObj.transform.parent != null
                    ? btnShootObj.transform.parent.gameObject
                    : btnShootObj;

                if (parentContainer.GetComponent<MobileInputController>() == null)
                {
                    parentContainer.AddComponent<MobileInputController>();
                }
            }
        }
    }
}

/// <summary>
/// Helper Component bắt sự kiện PointerDown và PointerUp từ hệ thống EventSystem của Unity UI
/// </summary>
public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action<bool> onHoldChanged;

    public void OnPointerDown(PointerEventData eventData)
    {
        onHoldChanged?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onHoldChanged?.Invoke(false);
    }
}
