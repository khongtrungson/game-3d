using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    public Button normalDifficultyButton;
    public Button hardDifficultyButton;
    public Button saveButton;
    public Slider smoothSpeedSlider;
    public Slider sensitivitySlider;
    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private int zombiePerWave;
    [SerializeField] private int maxWaves;
    [SerializeField] private int totalZombiesSpawned = 0;

    [SerializeField] private float smoothSpeed;
    [SerializeField] private float sensitivity;

    public Button map1Button;
    public Button map2Button;
    public Button continueButton;
    public string selectedMap;

    // ==========================================
    // CÁC BIẾN CHO MÀN HÌNH LOADING PHỤ (ĐÃ VÔ HIỆU HÓA)
    // ==========================================
    [Header("Màn hình Loading khi bấm Continue (Đã tắt)")]
    public GameObject inGameLoadingCanvas; // Không dùng nữa, có thể để trống
    public Slider loadingSlider;             // Không dùng nữa, có thể để trống

    [Header("Audio Settings")]
    public Slider sfxVolumeSlider;       // Kéo Slider SFX vào đây
    public UnityEngine.Audio.AudioMixer audioMixer; // Kéo AudioMixer của bạn vào đây

    void Start()
    {
        smoothSpeedSlider.minValue = 1f;
        smoothSpeedSlider.maxValue = 10f;
        sensitivitySlider.minValue = 1f;
        sensitivitySlider.maxValue = 5f;

        timeBetweenWaves = 15f;
        zombiePerWave = 5;
        maxWaves = 7;
        smoothSpeed = 10f;
        sensitivity = 2f;

        LoadSettings();

        normalDifficultyButton.onClick.AddListener(SetNormalDifficulty);
        hardDifficultyButton.onClick.AddListener(SetHardDifficulty);

        map1Button.onClick.AddListener(() => selectMap("Map1"));
        map2Button.onClick.AddListener(() => selectMap("Map2"));
        
        // Khi bấm nút Continue, gọi trực tiếp hàm load map ngay lập tức
        continueButton.onClick.AddListener(LoadSelectedMap);

        saveButton.onClick.AddListener(SaveSettings);

        // Đảm bảo ẩn canvas loading phụ lúc mới vào game (nếu có gán)
        if (inGameLoadingCanvas != null)
        {
            inGameLoadingCanvas.SetActive(false);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0.0001f; // Tránh lỗi logarit số 0
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("TimeBetweenWaves"))
        {
            timeBetweenWaves = PlayerPrefs.GetFloat("TimeBetweenWaves");
        }
        
        if (PlayerPrefs.HasKey("ZombiePerWave"))
        {
            zombiePerWave = PlayerPrefs.GetInt("ZombiePerWave");
        }

        if (PlayerPrefs.HasKey("MaxWaves"))
        {
            maxWaves = PlayerPrefs.GetInt("MaxWaves");
        }

        if (PlayerPrefs.HasKey("TotalZombiesSpawned"))
        {
            totalZombiesSpawned = PlayerPrefs.GetInt("TotalZombiesSpawned");
        }

        if (PlayerPrefs.HasKey("SmoothSpeed"))
        {
            smoothSpeed = PlayerPrefs.GetFloat("SmoothSpeed");
            smoothSpeedSlider.value = smoothSpeed;
        }

        if (PlayerPrefs.HasKey("Sensitivity"))
        {
            sensitivity = PlayerPrefs.GetFloat("Sensitivity");
            sensitivitySlider.value = sensitivity;
        }

        if (PlayerPrefs.HasKey("SelectedMap"))
        {
            selectedMap = PlayerPrefs.GetString("SelectedMap");
        }

        Debug.Log("Game Loaded");

        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume");
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
            SetSFXVolume(sfxVol); // Áp dụng âm lượng đã lưu
        }
    }

    public void SaveSettings()
    {
        smoothSpeed = smoothSpeedSlider.value;
        sensitivity = sensitivitySlider.value;

        PlayerPrefs.SetFloat("TimeBetweenWaves", timeBetweenWaves);
        PlayerPrefs.SetInt("ZombiePerWave", zombiePerWave);
        PlayerPrefs.SetInt("MaxWaves", maxWaves);
        PlayerPrefs.SetFloat("SmoothSpeed", smoothSpeed);
        PlayerPrefs.SetFloat("Sensitivity", sensitivity);

        PlayerPrefs.Save();

        Debug.Log("Saved");

        if (sfxVolumeSlider != null)
        {
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        }
    }

    private void SetNormalDifficulty()
    {
        timeBetweenWaves = 15f;
        zombiePerWave = 5;
        maxWaves = 7;
    }

    private void SetHardDifficulty()
    {
        timeBetweenWaves = 10f; 
        zombiePerWave = 7;
        maxWaves = 9;    
    }

    public void selectMap(string mapName)
    {
        selectedMap = mapName;
        PlayerPrefs.SetString("SelectedMap", selectedMap);
        Debug.Log("Đã chọn map: " + selectedMap);
    }

    // ==========================================
    // HÀM LOAD MAP KHI BẤM CONTINUE (ĐÃ ĐỔI THÀNH LOAD TRỰC TIẾP)
    // ==========================================
    public void LoadSelectedMap()
    {
        // Nếu chưa chọn map nào trước đó, mặc định vào Map1
        if (string.IsNullOrEmpty(selectedMap))
        {
            selectedMap = "Map1"; 
        }

        // BỎ QUA LOADING PHỤ: Chuyển cảnh thẳng đến map được chọn ngay lập tức
        SceneManager.LoadScene(selectedMap);
        
        /* 
         * ĐOẠN CODE LOADING CŨ (ĐÃ BỊ GỠ BỎ ĐỂ VÀO MAP LUÔN):
         * if (inGameLoadingCanvas != null) { inGameLoadingCanvas.SetActive(true); }
         * StartCoroutine(LoadMapAsync(selectedMap));
         */
    }

    // Coroutine loading bất đồng bộ cũ (Đã được giữ lại bên dưới nhưng không còn dùng tới)
    IEnumerator LoadMapAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20);
        }
        
        PlayerPrefs.SetFloat("SFXVolume", sliderValue);
    }
}