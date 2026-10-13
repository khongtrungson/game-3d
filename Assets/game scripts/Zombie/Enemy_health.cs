using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class Enemy_health : MonoBehaviour
{
    [Header("Health Settings")]
    public float max_health = 100f;
    public float health = 100f;
    public float recharge_health = 3f;
    public float cooling_rate = 5f;
    private float cooldown_time = 0f;

    [Header("Components")]
    public Animator anim;
    private NavMeshAgent nvm;
    private AudioSource audioSource;
    public health_bar_script hbs;

    [Header("Audio & FX")]
    public AudioClip enemy_hurt;
    public AudioClip enemy_dying;
    public Transform blood_point;
    public Collider head;
    public GameObject head_loss;

    [Header("Status")]
    public bool isDead = false;
    private bool is_attacking = false;
    public bool enemy_hurting;
    private float false_rest = 3f;

    // Tham chiếu đến script AI để đồng bộ hóa hành vi chết
    private WaypointZombieAI zombieAI;

    void Start()
    {
        // Tối ưu: Lấy các component 1 lần duy nhất khi game bắt đầu thay vì gọi trong Update
        health = max_health;
        audioSource = GetComponent<AudioSource>();
        nvm = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        zombieAI = GetComponent<WaypointZombieAI>();

        // Tải sẵn Audio từ thư mục Resources để tránh lag khung hình
        enemy_hurt = Resources.Load<AudioClip>("enemy_hurt");

        Transform own = transform;
    }

    void Update()
    {
        if (isDead) return;

        enemy_hurting = enemy_hurt_false(enemy_hurting);

        // Cập nhật thanh máu UI
        if (hbs != null)
        {
            hbs.setmaxhealth(max_health);
            hbs.sethealth(health);
        }

        // Hồi máu theo thời gian nếu chưa chết và không tấn công
        if (health > 0 && health + recharge_health <= max_health && Time.time > cooldown_time && !is_attacking)
        {
            health += recharge_health;
            cooldown_time = Time.time + cooling_rate;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        is_attacking = false;

        if (audioSource != null && enemy_hurt != null)
        {
            audioSource.PlayOneShot(enemy_hurt);
        }

        // Đồng thời báo cho script AI biết để chuyển trạng thái đuổi theo nếu đang đi tuần
        if (zombieAI != null)
        {
            zombieAI.TakeDamage(Mathf.RoundToInt(amount));
        }

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        enemy_hurting = false;

        // Vô hiệu hóa NavMeshAgent và Collider nếu có
        if (nvm != null) nvm.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Phát âm thanh chết và chạy animation
        if (audioSource != null && enemy_dying != null)
        {
            audioSource.PlayOneShot(enemy_dying);
        }

        if (anim != null)
        {
            // Nếu Animator của bạn dùng Trigger "die"
            //anim.SetTrigger("die");
            
            // Hoặc nếu bạn muốn dùng chung Boolean "IsDead" giống WaypointZombieAI, hãy bật dòng dưới:
            anim.SetBool("IsDead", true);
        }

        // Hủy GameObject sau 5 giây (hoặc chỉnh lại thời lượng tùy ý)
        Destroy(gameObject, 7f);
    }

    private bool enemy_hurt_false(bool x)
    {
        if (Time.time > false_rest)
        {
            x = false;
            false_rest = Time.time + 1f;
        }
        return x;
    }
}