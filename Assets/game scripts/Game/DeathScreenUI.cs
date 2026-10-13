using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreenUI : MonoBehaviour
{
    [Header("Fade Elements")]
    public Image targetImage;
    public Text targetText;
    public float fadeDuration = 5f; // Thời gian làm mờ màn hình chết

    [Header("UI Buttons & Canvas Groups")]
    public CanvasGroup replayCanvasGroup; 
    public CanvasGroup menuCanvasGroup;   
    public CanvasGroup quitCanvasGroup;   

    [Header("Scene Settings")]
    public string map1SceneName = "Map1";      
    public string menuSceneName = "MainMenu";  

    public bool showDeadScreen = false;
    private bool hasTriggeredDeath = false;

    void Start()
    {
        // Khi mới vào game, ẩn sạch màn hình chết và các nút đi
        SetAlpha(targetImage, targetText, 0f);
        SetButtonsAlpha(0f);
        
        if (targetImage != null) targetImage.gameObject.SetActive(false);
        if (targetText != null) targetText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Khi biến showDeadScreen được bật lên từ script quản lý chết
        if (showDeadScreen && !hasTriggeredDeath)
        {
            hasTriggeredDeath = true;
            StartCoroutine(DeathSequenceRoutine());
        }
    }

    IEnumerator DeathSequenceRoutine()
    {
        // 1. Kích hoạt hiển thị Image và Text thông báo chết
        if (targetImage != null) targetImage.gameObject.SetActive(true);
        if (targetText != null) targetText.gameObject.SetActive(true);

        // 2. Chạy hiệu ứng mờ dần cho Image và Text
        float elapsedTime = 0f;
        float startAlpha = targetImage != null ? targetImage.color.a : 0f;
        float targetAlphaVal = 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlphaVal, t);

            SetAlpha(targetImage, targetText, currentAlpha);
            yield return null;
        }

        SetAlpha(targetImage, targetText, 1f);

        // 3. Đợi thêm (8 giây - thời gian fade) để tổng thời gian từ lúc chết đúng bằng 8 giây
        // Hoặc bạn có thể chờ đúng 8 giây từ lúc bắt đầu chết:
        yield return new WaitForSecondsRealtime(4f);

        // 4. Hiện dần 3 nút Replay, Menu, Quit lên (Fade In trong 0.5 giây)
        float buttonFadeTime = 1f;
        elapsedTime = 0f;

        while (elapsedTime < buttonFadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / buttonFadeTime;
            SetButtonsAlpha(Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        SetButtonsAlpha(1f);

        // 5. Dừng hẳn thời gian game lại để người chơi bấm nút
        Time.timeScale = 0f;
    }

    private void SetAlpha(Image img, Text txt, float alpha)
    {
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
        if (txt != null)
        {
            Color c = txt.color;
            c.a = alpha;
            txt.color = c;
        }
    }

    private void SetButtonsAlpha(float alpha)
    {
        if (replayCanvasGroup != null) replayCanvasGroup.alpha = alpha;
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = alpha;
        if (quitCanvasGroup != null) quitCanvasGroup.alpha = alpha;
    }

    // Các hàm xử lý nút bấm
    public void ReplayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(map1SceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}