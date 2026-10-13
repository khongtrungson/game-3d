using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Đảm bảo bạn đang dùng NavMesh

public class SpawnZombie : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints; // Mảng chứa các điểm tuần tra
    private int currentWaypointIndex = 0;

    [Header("References")]
    private NavMeshAgent agent;

    [Header("Behavior Settings")]
    public float patrolSpeed = 2f;
    public float reachThreshold = 1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Nếu mảng waypoints bị bỏ trống hoặc chưa gán, tự động tìm tất cả các object có Tag là "Waypoint" trong Scene
        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
            if (waypointObjects.Length > 0)
            {
                waypoints = new Transform[waypointObjects.Length];
                for (int i = 0; i < waypointObjects.Length; i++)
                {
                    waypoints[i] = waypointObjects[i].transform;
                }
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Waypoint nào có Tag 'Waypoint' trên Scene!", this);
            }
        }

        // Bắt đầu di chuyển tới điểm đầu tiên nếu có waypoint
        if (waypoints != null && waypoints.Length > 0)
        {
            agent.speed = patrolSpeed;
            SetNextWaypoint();
        }
    }

    void Update()
    {
        PatrolBehavior();
    }

    void PatrolBehavior()
    {
        // Dòng 100: Kiểm tra nếu có waypoint thì mới chạy logic tuần tra
        if (waypoints == null || waypoints.Length == 0) return;

        // Dòng 165: Kiểm tra khoảng cách tới waypoint hiện tại để đổi điểm đến
        if (!agent.pathPending && agent.remainingDistance < reachThreshold)
        {
            CurrentWaypointIndexIncrement();
            SetNextWaypoint();
        }
    }

    void SetNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void CurrentWaypointIndexIncrement()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }
}