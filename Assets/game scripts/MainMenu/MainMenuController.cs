using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject loadingCanvas;   // Kéo đối tượng Loading vào đây
    public GameObject mainCanvas;   // Kéo Canv_Main hoặc Canv_Options vào đây

    [Header("Loading Slider")]
    public Slider loadingSlider;       // Kéo thanh Slider vào đây
    public float fillTime = 5f;        // Thời gian chạy đầy thanh (3 giây)
    private float timer = 0f;
    private bool isLoadingComplete = false;

    void Start()
    {
        // Khi game vừa chạy: hiện Loading, ẩn Menu chính đi
        if (loadingCanvas != null) loadingCanvas.SetActive(true);
        if (mainCanvas != null) mainCanvas.SetActive(false);

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = 0f;
        }
    }

    void Update()
    {
        if (!isLoadingComplete && loadingSlider != null)
        {
            timer += Time.deltaTime;
            loadingSlider.value = timer / fillTime;

            if (timer >= fillTime)
            {
                isLoadingComplete = true; // Đã chạy xong 100%

                // Tự động tắt màn hình Loading và bật Menu chính lên ngay lập tức
                if (loadingCanvas != null) loadingCanvas.SetActive(false);
                if (mainCanvas != null) mainCanvas.SetActive(true);
            }
        }
    }
}
