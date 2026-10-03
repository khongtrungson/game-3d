using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class score_kill : MonoBehaviour
{
    public GameObject[] enemies;
    public GameObject[] initial_enemies;
    public TMP_Text kill, grenade;
    public front_canvas_score_kill_script fpsk;
    public TMP_Text timer;
    private GameObject[] grenades;
    private GameObject time_finish, enemy_finish;
    private float initial_timevalue = 1200;
    public float timevalue = 1200;

    void Start()
    {
        // 1. Tự động tìm component fpsk nếu chưa gán qua Inspector
        if (fpsk == null)
        {
            fpsk = FindFirstObjectByType<front_canvas_score_kill_script>();
            // Nếu dùng Unity bản cũ (2022 trở xuống), thay dòng trên bằng:
            // fpsk = FindObjectOfType<front_canvas_score_kill_script>();
        }

        // 2. Lấy danh sách kẻ địch ban đầu
        initial_enemies = GameObject.FindGameObjectsWithTag("enemy");

        // 3. Kiểm tra an toàn trước khi gán
        if (fpsk != null)
        {
            fpsk.initial_enemies_length = initial_enemies.Length;
        }
        else
        {
            //Debug.LogError("Chưa gán hoặc không tìm thấy 'front_canvas_score_kill_script' trong Scene!");
        }
    }

    void Update()
    {
        enemies = GameObject.FindGameObjectsWithTag("enemy");
        
        // Kiểm tra fpsk null trước khi cập nhật dữ liệu liên tục
        if (fpsk != null)
        {
            fpsk.enemies_length = enemies.Length;
            fpsk.time_left = initial_timevalue - timevalue;
        }

        grenades = GameObject.FindGameObjectsWithTag("grenade");

        if (grenade != null)
        {
            grenade.text = grenades.Length.ToString() + "/8";
        }
        else
        {
            Debug.LogWarning("Chưa kéo thả UI Text vào ô 'grenade' trong Inspector!");
        }

        if (kill != null)
        {
            kill.text = "Kill:" + (initial_enemies.Length - enemies.Length).ToString() + "/" + initial_enemies.Length.ToString();
        }

        if (enemies.Length == 0)
        {
            enemy_finish = GameObject.FindGameObjectWithTag("enemy_finish");
            if (enemy_finish == null)
            {
                Instantiate(Resources.Load("enemy_finish") as GameObject);
            }
        }

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

        if (timevalue < 30 && timer != null)
        {
            timer.color = new Color32(255, 0, 27, 255);
        }

        if (timer != null)
        {
            DisplayTime(timevalue);
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
        timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}