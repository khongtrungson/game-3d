using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class score_kill : MonoBehaviour
{
    public GameObject[] enemies;
    public GameObject[] initial_enemies; // Khai báo lại initial_enemies để xem trên Inspector
    
    public TMP_Text kill, grenade;
    public front_canvas_score_kill_script fpsk;
    public TMP_Text timer;
    
    private GameObject[] grenades;
    private GameObject time_finish;
    
    [Header("Win Screen UI")]
    public GameObject winCanvas;             
    public TMP_Text winKillsText;            
    public TMP_Text winTimeText;             
    public Button replayButton;              
    public Button menuButton;                
    public Button quitButton;                

    [Header("Fade Settings for Buttons")]
    public CanvasGroup[] buttonCanvasGroups; 
    public float fadeDuration = 1.0f;   

    [Header("HUD Elements cần ẩn khi Win/Dead")]
    public GameObject[] hudElementsToHide;     

    private float initial_timevalue = 1200;
    public float timevalue = 1200;
    private bool isGameEnded = false;

    [Header("Wave System Reference")]
    public ZombieWaveSystem waveSystem;

    // Biến lưu tổng số lượng zombie (initial)
    public int initial = 0; 

    void Start()
    {
        if (fpsk == null)
        {
            fpsk = FindObjectOfType<front_canvas_score_kill_script>();
        }

        // Lấy số quái ban đầu làm mốc khởi đầu cho initial
        initial_enemies = GameObject.FindGameObjectsWithTag("Zombie");
        initial = initial_enemies.Length;

        if (winCanvas != null)
        {
            winCanvas.SetActive(false);
        }

        if (replayButton != null) replayButton.onClick.AddListener(ReplayGame);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (isGameEnded) return;

        // 1. Cập nhật danh sách enemies hiện tại trên map (Zombie còn sống)
        enemies = GameObject.FindGameObjectsWithTag("Zombie");
        
        if (fpsk != null)
        {
            fpsk.enemies_length = enemies.Length;
        }

        grenades = GameObject.FindGameObjectsWithTag("grenade");
        if (grenade != null)
        {
            grenade.text = grenades.Length.ToString() + "/8";
        }

        // 2. Logic cập nhật INITIAL:
        // Khi wave sinh quái mới khiến enemies.Length lớn hơn mốc initial cũ -> Cập nhật initial tăng lên
        if (enemies.Length > initial)
        {
            initial = enemies.Length;
            initial_enemies = enemies; // Cập nhật mảng initial_enemies để theo dõi
        }

        // 3. Tính số Kills theo đúng công thức: (initial - enemies)
        int currentKills = initial - enemies.Length;
        if (currentKills < 0) currentKills = 0;

        // Hiển thị dạng Kill: (initial - enemies) / initial
        if (kill != null)
        {
            kill.text = "Kill: " + currentKills.ToString() + " / " + initial.ToString();
        }

        // 4. ĐIỀU KIỆN THẮNG:
        // - Đã có zombie xuất hiện (initial > 0)
        // - Đã kết thúc tất cả các wave (waveNumber > maxWaves)
        // - Số quái trên map bằng 0 (enemies.Length == 0)
        bool isAllWavesDone = true;
        if (waveSystem != null)
        {
            if (waveSystem.GetWaveNumber() <= waveSystem.GetMaxWaves())
            {
                isAllWavesDone = false;
            }
        }

        if (isAllWavesDone && initial > 0 && enemies.Length == 0 && timevalue > 0)
        {
            TriggerWin(currentKills);
        }

        // --- Phần đếm ngược thời gian ---
        if (timevalue > 0)
        {
            timevalue -= Time.deltaTime;
        }
        else
        {
            timevalue = 0;
            time_finish = GameObject.FindGameObjectWithTag("time_finish");
            if (time_finish == null)
            {
                Instantiate(Resources.Load("time_finish") as GameObject);
            }
        }

        if (timer != null)
        {
            if (timevalue < 30)
            {
                timer.color = new Color32(255, 0, 27, 255);
            }
            DisplayTime(timevalue);
        }

        if (fpsk != null)
        {
            fpsk.time_left = initial_timevalue - timevalue;
        }
    }

    void DisplayTime(float timetodisplay)
    {
        if (timetodisplay < 0)
        {
            timetodisplay = 0;
        }
        float minutes = Mathf.FloorToInt(timetodisplay / 60);
        float seconds = Mathf.FloorToInt(timetodisplay % 60);

        if (timer != null)
        {
            timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void TriggerWin(int finalKills)
    {
        if (isGameEnded) return;
        isGameEnded = true;
        Debug.Log("THẮNG GAME - Đã kích hoạt điều kiện Win!");

        StartCoroutine(FadeInButtonsRoutine());

        float timeSpent = initial_timevalue - timevalue;
        float minutes = Mathf.FloorToInt(timeSpent / 60f);
        float seconds = Mathf.FloorToInt(timeSpent % 60f);

        // Hiển thị thông tin Kills và Time lên bảng Win
        if (winKillsText != null)
        {
            winKillsText.text = "Kills: " + finalKills;
        }

        if (winTimeText != null)
        {
            winTimeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }

        // 1. Hiển thị bảng Win Canvas
        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ Chưa kéo gán winCanvas vào script score_kill trong Inspector!");
        }

        // 2. Ẩn các HUD được cấu hình trong mảng hudElementsToHide
        if (hudElementsToHide != null)
        {
            foreach (GameObject hud in hudElementsToHide)
            {
                if (hud != null)
                {
                    hud.SetActive(false);
                }
            }
        }

        // Ẩn các thành phần Text HUD chính
        if (kill != null) kill.gameObject.SetActive(false);
        if (timer != null) timer.gameObject.SetActive(false);
        if (grenade != null) grenade.gameObject.SetActive(false);

        // Tùy chọn: Tự động ẩn tất cả các đối tượng UI khác (nếu nằm chung Canvas gameplay) ngoại trừ WinCanvas
        // Giúp giải quyết triệt để vấn đề sót lại các nút bấm di chuyển/bắn súng trên mobile
        foreach (Transform child in transform.root)
        {
            // Nếu đối tượng là Canvas gameplay (không phải WinCanvas) thì tắt đi
            Canvas c = child.GetComponent<Canvas>();
            if (c != null && child.gameObject != winCanvas)
            {
                child.gameObject.SetActive(false);
            }
        }

        // 3. Khóa/Mở khóa chuột để hiện con trỏ bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Dừng thời gian game
        Time.timeScale = 0f;

        // 5. Chạy hiệu ứng hiện nút bấm
        
    }

    IEnumerator FadeInButtonsRoutine()
    {
        foreach (CanvasGroup cg in buttonCanvasGroups)
        {
            if (cg != null) cg.alpha = 0f;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alphaValue = Mathf.Clamp01(elapsedTime / fadeDuration);

            foreach (CanvasGroup cg in buttonCanvasGroups)
            {
                if (cg != null) cg.alpha = alphaValue;
            }

            yield return null;
        }

        foreach (CanvasGroup cg in buttonCanvasGroups)
        {
            if (cg != null) cg.alpha = 1f;
        }
    }

    public void ReplayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); 
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game...");
        Application.Quit();
    }
}