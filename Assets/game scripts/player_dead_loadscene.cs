using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class player_dead_loadscene : MonoBehaviour
{
    private GameObject time_finish, enemy_finish;
    public player_health ph;
    
    [Header("Death Screen Reference")]
    public DeathScreen deathScreen; // Kéo thả đối tượng DeathScreen vào đây

    void Update()
    {
        time_finish = GameObject.FindGameObjectWithTag("time_finish");
        enemy_finish = GameObject.FindGameObjectWithTag("enemy_finish");

        if (ph.player_dead == true && time_finish == null && enemy_finish == null)
        { 
            TriggerGameOver();
        }
        else if (ph.player_dead == false && time_finish == null && enemy_finish != null)
        {
            Invoke("load_scene", 10);
        }
        else if (ph.player_dead == false && time_finish != null && enemy_finish == null)
        {
            TriggerGameOver();
        }
        else if (ph.player_dead == false && time_finish != null && enemy_finish != null)
        {
            Invoke("load_scene", 10);
        }
    }

    private void TriggerGameOver()
    {
        if (deathScreen != null)
        {
            deathScreen.showDeadScreen = true;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void load_scene()
    {
        SceneManager.LoadScene("won scene 1");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}