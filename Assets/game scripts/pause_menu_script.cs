using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class pause_menu_script : MonoBehaviour
{
    [Header("UI Cửa sổ Pause")]
    public GameObject pauseMenuUI; // Kéo Panel Pause vào đây

    [Header("Nút Pause trên Mobile")]
    public Button mobilePauseButton; // Kéo nút II trên màn hình mobile vào đây

    [Header("Tên Scene Main Menu")]
    public string menuSceneName = "MainMenu"; // Đổi tên cho khớp với scene menu của bạn

    private bool isPaused = false;

    void Start()
    {
        // Đảm bảo lúc đầu game chạy bình thường và ẩn bảng pause đi
        ResumeGame();

        // Lắng nghe sự kiện bấm nút pause trên mobile (nếu có gán)
        if (mobilePauseButton != null)
        {
            mobilePauseButton.onClick.AddListener(TogglePause);
        }
    }

    void Update()
    {
        // 1. Kiểm tra phím ESC trên PC hoặc nút quay lại trên Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // 2. Kiểm tra click chuột trái vào màn hình (chỉ áp dụng khi đang chơi và bạn muốn bấm màn hình để pause - tùy chọn)
        // Lưu ý: Nếu muốn dùng tính năng click màn hình PC để pause, hãy chắc chắn nó không xung đột với việc bắn súng.
        /*
        if (Input.GetMouseButtonDown(0) && !isPaused)
        {
            // Có thể thêm điều kiện kiểm tra nếu không bấm trúng UI thì mới pause
        }
        */
    }

    // Hàm chuyển đổi trạng thái Pause / Resume
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);

        // Đóng băng thời gian trong game (Dừng chuyển động, enemy, vật lý...)
        Time.timeScale = 0f;

        // Hiện con trỏ chuột trên PC để người chơi bấm nút giao diện
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        // Khôi phục thời gian game về bình thường
        Time.timeScale = 1f;

        // Ẩn con trỏ chuột nếu game của bạn là góc nhìn thứ nhất (FPS)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    // --- CÁC HÀM CHO CÁC NÚT TRONG BẢNG PAUSE ---

    public void ReplayGame()
    {
        Time.timeScale = 1f; // Phục hồi thời gian trước khi load lại scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Phục hồi thời gian trước khi chuyển scene
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }
}