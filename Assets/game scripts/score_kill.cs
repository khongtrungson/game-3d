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
        // Tự động tìm fpsk nếu chưa kéo thả vào Inspector
        if (fpsk == null)
        {
            fpsk = FindObjectOfType<front_canvas_score_kill_script>();
        }

        initial_enemies = GameObject.FindGameObjectsWithTag("enemy");

        if (fpsk != null)
        {
            fpsk.initial_enemies_length = initial_enemies.Length;
        }
        else
        {
            Debug.LogWarning("Chưa gán fpsk (front_canvas_score_kill_script)!");
        }
    }

    void Update()
    {
        enemies = GameObject.FindGameObjectsWithTag("enemy");
        
        if (fpsk != null)
        {
            fpsk.enemies_length = enemies.Length;
        }

        grenades = GameObject.FindGameObjectsWithTag("grenade");

        // 1. Kiểm tra UI Grenade
        if (grenade != null)
        {
            grenade.text = grenades.Length.ToString() + "/8";
        }

        // 2. Kiểm tra UI Kill
        if (kill != null)
        {
            kill.text = "Kill:" + (initial_enemies.Length - enemies.Length).ToString() + "/" + initial_enemies.Length.ToString();
        }

        // Xử lý khi diệt hết quái
        if (enemies.Length == 0)
        {
            enemy_finish = GameObject.FindGameObjectWithTag("enemy_finish");
            if (enemy_finish == null)
            {
                Instantiate(Resources.Load("enemy_finish") as GameObject);
            }
        }

        // Xử lý đếm ngược thời gian
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

        // 3. Kiểm tra UI Timer trước khi đổi màu (Tránh lỗi dòng 43)
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
}