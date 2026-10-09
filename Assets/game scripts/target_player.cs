using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class target_player : MonoBehaviour
{
    public float damage = 10f;
    private float range = 100f;
    public GameObject gun;
    public GameObject checking;

    public GameObject gun_big, hand_gun;

    [Header("Blood Effect Prefab")]
    public GameObject bloodEffectPrefab; // Kéo Prefab máu vào đây từ Inspector (Hoặc dùng Resources nếu muốn)

    private player_health health_player;

    void Start()
    {
        health_player = GetComponent<player_health>();
    }

    void Update()
    {   
        if (gun_big != null && gun_big.activeSelf) {
            range = 18f;
        }
        else {
            range = 8f;
        }

        checking = GameObject.FindGameObjectWithTag("checking");

        if (Input.GetMouseButtonDown(0) && checking != null && health_player != null && health_player.health > 0) {
            shoot();
        }
    }

    private void shoot() {
        RaycastHit hit;
        if (Physics.Raycast(gun.transform.position, gun.transform.forward, out hit, range)) {
            
            // 1. Zombie Đất
            Enemy_health health_script = hit.transform.GetComponent<Enemy_health>();
            if (health_script != null) {
                health_script.enemy_hurting = true;

                // Bắn hiệu ứng máu tại đúng điểm va chạm của viên đạn (hit.point)
                SpawnBloodEffect(hit.point, hit.normal);

                if (hit.collider == health_script.head) {
                    health_script.TakeDamage(80);
                    if (health_script.head_loss != null) {
                        Destroy(health_script.head_loss);
                    }
                }
                else {
                    health_script.TakeDamage(damage);
                }
            }
            // 2. Kẻ địch Bay
            else {
                fly_enemy_health_script fly_health_script = hit.transform.GetComponent<fly_enemy_health_script>();
                if (fly_health_script != null) {
                    fly_health_script.enemy_hurting = true;

                    // Bắn hiệu ứng máu cho quái bay
                    SpawnBloodEffect(hit.point, hit.normal);

                    if (hit.collider == fly_health_script.eye_collider) {
                        fly_health_script.TakeDamage(80);
                    }
                    else {
                        fly_health_script.TakeDamage(damage);
                    }
                }
            }

            // 3. Thùng nổ
            explosion_barrel exp_bar = hit.transform.GetComponent<explosion_barrel>();
            if (exp_bar != null) {
                exp_bar.TakeDamage();
            }
        }
    }

    private void SpawnBloodEffect(Vector3 spawnPosition, Vector3 normal) {
        // Ưu tiên dùng Prefab gán ở Inspector, nếu không có sẽ load từ Assets/Resources/blood
        GameObject bloodPrefab = bloodEffectPrefab;
        if (bloodPrefab == null) {
            bloodPrefab = Resources.Load<GameObject>("blood");
        }

        if (bloodPrefab != null) {
            // Khởi tạo máu tại điểm đạn chạm và xoay theo mặt phẳng va chạm
            GameObject _effect = Instantiate(bloodPrefab, spawnPosition, Quaternion.LookRotation(normal));
            Destroy(_effect, 1f);
        }
        else {
            Debug.LogError("Không tìm thấy Prefab 'blood' trong thư mục Assets/Resources/ !");
        }
    }
}