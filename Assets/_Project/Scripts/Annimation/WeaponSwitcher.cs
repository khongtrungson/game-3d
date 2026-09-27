using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Chỉ số súng ban đầu (0 = súng đầu tiên)")]
    [SerializeField] private int currentWeaponIndex = 0;

    private void Start()
    {
        // Bật súng mặc định khi bắt đầu game
        SelectWeapon(currentWeaponIndex);
    }

    private void Update()
    {
        int previousSelected = currentWeaponIndex;

        // Lấy giá trị cuộn chuột (Lên > 0, Xuống < 0)
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= transform.childCount)
            {
                currentWeaponIndex = 0; // Cuộn hết danh sách thì vòng lại súng đầu tiên
            }
        }
        else if (scroll < 0f)
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0)
            {
                currentWeaponIndex = transform.childCount - 1; // Lùi quá thì về súng cuối
            }
        }

        // Hỗ trợ bấm phím số (1, 2, 3...) để đổi nhanh súng
        if (Input.GetKeyDown(KeyCode.Alpha1) && transform.childCount >= 1) currentWeaponIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2) currentWeaponIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3) currentWeaponIndex = 2;

        // Nếu chỉ số súng thay đổi thì gọi hàm cập nhật ẩn/hiện
        if (previousSelected != currentWeaponIndex)
        {
            SelectWeapon(currentWeaponIndex);
        }
    }

    private void SelectWeapon(int index)
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            // Bật GameObject súng khớp với index, ẩn tất cả súng khác
            weapon.gameObject.SetActive(i == index);
            i++;
        }
    }
}