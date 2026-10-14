using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class player_dead_loadscene : MonoBehaviour
{
    private GameObject time_finish, enemy_finish;
    public player_health ph;
    
    [Header("Death Screen Reference")]
    public DeathScreenUI deathScreen; // Kéo thả đối tượng DeathScreen vào đây

    [Header("UI Elements to Hide on Death")]
    public GameObject playerHealthBar; // Kéo thả đối tượng player_health_bar vào đây
    public GameObject pLpspUiCanvas;   // Kéo thả đối tượng P_LPSP_UI_Canvas vào đây

    [Header("Player Object")]
    public GameObject playerObject;    // Kéo nhân vật (Player) của bạn vào đây (chỉ 1 ô duy nhất này thôi)
  
    void Update()
    {
        time_finish = GameObject.FindGameObjectWithTag("time_finish");
        enemy_finish = GameObject.FindGameObjectWithTag("enemy_finish");

        // Khi người chơi chết hoặc hết giờ mà chưa diệt hết quái -> Game Over
        if ((ph.player_dead == true && time_finish == null && enemy_finish == null) ||
            (ph.player_dead == false && time_finish != null && enemy_finish == null))
        { 
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        // 1. Hiển thị bảng DeathScreen
        if (deathScreen != null)
        {
            deathScreen.showDeadScreen = true;
        }

        // 2. Tắt thanh máu và toàn bộ canvas giao diện (bao gồm MobileButton, P_LPSP_UI_Canvas, Minimap...)
        if (playerHealthBar != null) playerHealthBar.SetActive(false);
        if (pLpspUiCanvas != null) pLpspUiCanvas.SetActive(false);
        HideAllGameplayHUDs(deathScreen != null ? deathScreen.gameObject : null);

        // 3. Tự động vô hiệu hóa tất cả các script nằm trên Player (di chuyển, bắn súng, lựu đạn...)
        if (playerObject != null)
        {
            // Lấy tất cả các script đang gắn trên người Player và tắt chúng đi
            MonoBehaviour[] scripts = playerObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                // Không tự tắt script quản lý chết này
                if (script != this)
                {
                    script.enabled = false;
                }
            }
        }

        // 4. Mở khóa con trỏ chuột
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Ẩn toàn bộ các UI Canvas và HUD điều khiển gameplay (MobileButton, P_LPSP_UI_Canvas, minimap...)
    /// ngoại trừ màn hình hiển thị được chỉ định (như DeathScreen hoặc WinCanvas).
    /// </summary>
    public static void HideAllGameplayHUDs(GameObject screenToKeep = null)
    {
        // 1. Tắt MobileButton
        var mobileInput = Object.FindAnyObjectByType<MobileInputController>();
        if (mobileInput != null && (screenToKeep == null || !mobileInput.transform.IsChildOf(screenToKeep.transform)))
        {
            mobileInput.gameObject.SetActive(false);
        }
        GameObject mobileBtnObj = GameObject.Find("MobileButton");
        if (mobileBtnObj != null && mobileBtnObj != screenToKeep)
        {
            mobileBtnObj.SetActive(false);
        }

        // 2. Tắt P_LPSP_UI_Canvas
        GameObject plpspCanvas = GameObject.Find("P_LPSP_UI_Canvas");
        if (plpspCanvas != null && plpspCanvas != screenToKeep)
        {
            plpspCanvas.SetActive(false);
        }

        // 3. Tắt thanh máu
        GameObject healthBar = GameObject.Find("player_health_bar");
        if (healthBar != null && healthBar != screenToKeep)
        {
            healthBar.SetActive(false);
        }

        // 4. Tắt ZoomMiniMap
        GameObject miniMap = GameObject.Find("ZoomMiniMap");
        if (miniMap != null && miniMap != screenToKeep)
        {
            miniMap.SetActive(false);
        }

        // 5. Quét tất cả các canvas con trong Main Canvas hoặc toàn Scene
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c == null) continue;
            if (screenToKeep != null && (c.gameObject == screenToKeep || c.transform.IsChildOf(screenToKeep.transform) || screenToKeep.transform.IsChildOf(c.transform)))
            {
                continue;
            }

            // Nếu canvas này là canvas riêng của HUD thì tắt đi
            if (c.name.Contains("Mobile") || c.name.Contains("LPSP") || c.name.Contains("HUD") || c.name.Contains("MiniMap"))
            {
                c.gameObject.SetActive(false);
            }
        }
    }
}