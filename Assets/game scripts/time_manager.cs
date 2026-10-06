using UnityEngine;

public class time_manager : MonoBehaviour 
{
    public float slowdownFactor = 0.05f;
    public float slowdownLength = 2f;
    private GameObject[] enemies, in_enemy;
    private int count = 0;

    void Start()
    {
        in_enemy = GameObject.FindGameObjectsWithTag("enemy");
    }

    void Update()
    {
        enemies = GameObject.FindGameObjectsWithTag("enemy");

        // 1. Kiểm tra nếu không có enemy nào thì dừng xử lý để tránh lỗi mảng rỗng
        if (enemies == null || enemies.Length == 0) 
            return;

        // Chỉ truy cập phần tử khi mảng chắc chắn có ít nhất 1 enemy
        GameObject enemy = enemies[0];
        if (enemy == null) 
            return;

        Enemy_health eh = enemy.GetComponent<Enemy_health>();
        fly_enemy_health_script eh2 = enemy.GetComponent<fly_enemy_health_script>();

        // 2. Sửa lỗi kiểm tra lượng máu an toàn cho cả 2 loại Enemy
        float currentHealth = 999f;
        if (eh != null) 
        {
            currentHealth = eh.health;
        } 
        else if (eh2 != null) 
        {
            currentHealth = eh2.health;
        }

        // 3. Kích hoạt Slow motion khi còn đúng 1 enemy và máu <= 5
        if (enemies.Length == 1 && currentHealth <= 5 && count == 0)
        {
            DoSlowmotion();
            count = 1; // Đánh dấu đã kích hoạt Slowmotion

            // Chỉ gọi Invoke duy nhất 1 lần tại đây
            Invoke("time_normal", 0.8f); 
        }
    }

    public void DoSlowmotion()
    {
        Time.timeScale = slowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
    }

    private void time_normal()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // Khôi phục lại fixedDeltaTime chuẩn của Unity
    }
}