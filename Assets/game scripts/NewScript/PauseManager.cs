using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack; // Thêm namespace của asset vào đầu file

public class PauseManager : MonoBehaviour
{
    [Header("UI Cửa sổ Pause")]
    public GameObject pauseMenuUI; 

    [Header("Nút Pause trên Mobile")]
    public Button mobilePauseButton; 

    [Header("Tên Scene Main Menu")]
    public string menuSceneName = "MainMenu"; 

    private bool isPaused = false;

    void Start()
    {
        ResumeGame();

        if (mobilePauseButton != null)
        {
            mobilePauseButton.onClick.AddListener(TogglePause);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

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

        Time.timeScale = 0f;

        // Mở khóa chuột để bấm UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;

        // Chỉ khóa chuột và gọi lệnh character trên PC, tránh ảnh hưởng đến cảm ứng mobile
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Character player = FindObjectOfType<Character>();
            if (player != null)
            {
                player.ForceLockCursor(true);
            }
        }
    }
    public void ReplayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}