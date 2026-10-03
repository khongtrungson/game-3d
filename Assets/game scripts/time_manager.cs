using UnityEngine;

public class time_manager : MonoBehaviour {

	public float slowdownFactor = 0.05f;
	public float slowdownLength = 2f;
    private GameObject[] enemies,in_enemy;
    private int count=0;

void Start(){
    in_enemy=GameObject.FindGameObjectsWithTag("enemy");
}
	void Update()
{
    enemies = GameObject.FindGameObjectsWithTag("enemy");

    // 1. Kiểm tra nếu không có enemy nào thì dừng, tránh truy cập enemies[0] gây lỗi IndexOutOfRangeException
    if (enemies == null || enemies.Length == 0) return;

    if (count == 1)
    {
        Invoke("time_normal", 0.8f);
    }

    // 2. Chỉ kiểm tra Slowmotion khi đúng thời điểm còn lại duy nhất 1 enemy và chưa kích hoạt slow (count == 0)
    if (enemies.Length == 1 && count == 0)
    {
        GameObject enemy = enemies[0];
        
        if (enemy != null)
        {
            Enemy_health eh = enemy.GetComponent<Enemy_health>();
            fly_enemy_health_script eh2 = enemy.GetComponent<fly_enemy_health_script>();

            // Kiểm tra máu của Enemy_health
            if (eh != null && eh.health <= 5)
            {
                DoSlowmotion();
                count += 1;
            }
            // Kiểm tra máu của fly_enemy_health_script
            else if (eh2 != null && eh2.health <= 5)
            {
                DoSlowmotion();
                count += 1;
            }
        }
    }
}

	public void DoSlowmotion ()
	{
		Time.timeScale = slowdownFactor;
		Time.fixedDeltaTime = Time.timeScale * .02f;
	}
 
 private void time_normal(){
            Time.timeScale=1f;
 }
}