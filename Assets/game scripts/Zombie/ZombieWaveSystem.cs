using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieWaveSystem : MonoBehaviour
{
    public GameObject[] zombiePrefabs;
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 10f;
    [SerializeField] private float waveTimer = 0f;
    private int waveNumber = 1;
    public int zombiesPerWave = 4;

    [Header("Wave Limit")]
    public int maxWaves = 3;

    [Header("Tracking Stats")]
    [SerializeField] private int totalZombiesSpawned = 0;

    void Start()
    {
        // --- ĐỌC THÔNG SỐ ĐỘ KHÓ TỪ SETTINGMANAGER (PLAYERPREFS) ---
        if (PlayerPrefs.HasKey("TimeBetweenWaves"))
        {
            timeBetweenWaves = PlayerPrefs.GetFloat("TimeBetweenWaves");
        }

        if (PlayerPrefs.HasKey("ZombiePerWave"))
        {
            zombiesPerWave = PlayerPrefs.GetInt("ZombiePerWave");
        }

        if (PlayerPrefs.HasKey("MaxWaves"))
        {
            maxWaves = PlayerPrefs.GetInt("MaxWaves");
        }
        // -----------------------------------------------------------
    }

    void Update()
    {
        if(waveNumber > maxWaves)
            return;

        waveTimer += Time.deltaTime;

        if(waveTimer >= timeBetweenWaves)
        {
            StartNewWave();
        }
    }

    void StartNewWave()
    {
        if(waveNumber > maxWaves)
            return;

        waveTimer = 0f;

        zombiesPerWave += 2;

        float minDistance = 4f;

        for(int i = 0; i < zombiesPerWave; i++)
        {
            int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomSpawnIndex];

            GameObject randomZombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];

            Vector3 spawnPosition = spawnPoint.position + Random.insideUnitSphere * minDistance;
            spawnPosition.y = spawnPoint.position.y;

            Instantiate(randomZombiePrefab, spawnPosition, spawnPoint.rotation);

            totalZombiesSpawned++;
        }

        waveNumber++;
    }

    public int GetTotalZombiesSpawned()
    {
        return totalZombiesSpawned;
    }

    public int GetWaveNumber()
    {
        return waveNumber;
    }

    public int GetMaxWaves()
    {
        return maxWaves;
    }

    public float GetTimeBetweenWaves()
    {
        return timeBetweenWaves;
    }
}