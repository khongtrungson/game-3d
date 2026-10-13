using System.Collections.Generic;
using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    [Header("Danh sách các loại Zombie")]
    [Tooltip("Kéo 4 prefab zombie (Zombie 1, 2, 3, 4) vào đây")]
    public GameObject[] zombiePrefabs;

    [Header("Mục chứa các điểm Spawn")]
    [Tooltip("Kéo GameObject cha chứa 100 điểm spawn vào đây")]
    public Transform spawnPointsParent;

    // Danh sách điểm spawn sẽ được tự động lấy
    private Transform[] spawnPoints;

    void Start()
    {
        // Tự động lấy tất cả các điểm con bên trong mục cha
        AutoGatherSpawnPoints();

        // Tiến hành spawn zombie
        SpawnZombiesAtAllPoints();
    }

    void AutoGatherSpawnPoints()
    {
        if (spawnPointsParent == null)
        {
            Debug.LogWarning("Chưa gán mục chứa Spawn Points (Parent)!");
            spawnPoints = new Transform[0];
            return;
        }

        // Lấy tất cả Transform con bên trong mục cha (bỏ qua chính đối tượng cha)
        int childCount = spawnPointsParent.childCount;
        spawnPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            spawnPoints[i] = spawnPointsParent.GetChild(i);
        }

        Debug.Log($"Đã tự động quét được {spawnPoints.Length} điểm spawn từ mục cha!");
    }

    void SpawnZombiesAtAllPoints()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán Prefab zombie nào vào Spawner!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy điểm spawn nào để chạy!");
            return;
        }

        // Duyệt qua tất cả các điểm spawn đã tự động quét được
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform currentPoint = spawnPoints[i];
            if (currentPoint == null) continue;

            // Random ngẫu nhiên 1 trong 4 con zombie
            int randomIndex = Random.Range(0, zombiePrefabs.Length);
            GameObject selectedZombiePrefab = zombiePrefabs[randomIndex];

            if (selectedZombiePrefab != null)
            {
                Instantiate(selectedZombiePrefab, currentPoint.position, currentPoint.rotation);
            }
        }

        Debug.Log($"Đã spawn thành công zombie tại {spawnPoints.Length} điểm!");
    }
}