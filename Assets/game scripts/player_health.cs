using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player_health : MonoBehaviour
{
    [Header("Health Settings")]
    public float health = 100f;
    private float max_health = 100f;
    public bool player_dead = false;

    [Header("UI Settings")]
    public health_bar_script hbs;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip player_hurt;
    public AudioClip player_dying;

    // Private References
    private Transform player;
    private bool isPlayingAnim = false;
    private bool recharge_health = false;
    private GameObject used_kit;

    void Start()
    {
        player = transform; // Lấy trực tiếp Transform của Player

        // Khởi tạo thanh máu
        if (hbs != null)
        {
            hbs.setmaxhealth(max_health);
            hbs.sethealth(health);
        }
    }

    public void TakeDamage(float amount)
    {
        if (health > 0)
        {
            health -= amount;
            
            if (audioSource != null && player_hurt != null)
            {
                audioSource.PlayOneShot(player_hurt);
            }

            if (health <= 0)
            {
                health = 0;
                if (!isPlayingAnim)
                {
                    StartCoroutine(PlayAnim());
                }
            }
        }
    }

    private IEnumerator PlayAnim()
    {
        isPlayingAnim = true;
        if (audioSource != null && player_dying != null)
        {
            audioSource.PlayOneShot(player_dying);
        }
        
        yield return new WaitForSeconds(3f);
        player_dead = true;
        isPlayingAnim = false;
    }

    void Update()
    {
        // 1. Cập nhật thanh máu UI
        if (hbs != null)
        {
            hbs.sethealth(health);
        }

        // 2. Xử lý nhặt Medikit (Chỉ quét tìm Medikit khi ấn phím CapsLock)
        if (Input.GetKeyDown(KeyCode.CapsLock) && health < max_health)
        {
            GameObject[] medikits = GameObject.FindGameObjectsWithTag("medikit");
            foreach (var kit in medikits)
            {
                if (kit == null) continue;

                float dis = Vector3.Distance(player.position, kit.transform.position);
                if (dis < 2.5f)
                {
                    used_kit = kit;
                    recharge_health = true;
                    break;
                }
            }
        }

        // 3. Hồi máu
        if (recharge_health)
        {
            health = max_health;
            if (used_kit != null)
            {
                Destroy(used_kit);
            }
            recharge_health = false;
        }
    }
}