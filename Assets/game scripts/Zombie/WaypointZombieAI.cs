using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaypointZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Patrol, Chase, Attack, Dead }

    [Header("Components & Settings")]
    public NavMeshAgent navAgent;
    public Animator animator;
    public ZombieState currentState = ZombieState.Idle;

    [Header("Patrol & Combat")]
    public Transform[] waypoints;
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 5.0f;
    public float chaseDistance = 10f;
    public float loseSightDistance = 15f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float damage = 15f;
    public int health = 100;

    [Header("Visual Effects")]
    public GameObject bloodScreenEffectPrefab;
    private GameObject instantiatedBloodScreenEffect;

    private Transform player;
    private player_health playerHealthComponent;
    private int currentWaypointIndex = 0;
    private float lastAttackTime;
    private bool isAttacking, isDead;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        // Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealthComponent = playerObj.GetComponent<player_health>();
        }

        // Kiểm tra xem mảng waypoints có bị trống hoặc có ô nào bị None không
        bool needsWaypoints = waypoints == null || waypoints.Length == 0;
        if (!needsWaypoints)
        {
            foreach (var wp in waypoints)
            {
                if (wp == null) { needsWaypoints = true; break; }
            }
        }

        // Tự động quét tìm theo Tag nếu thiếu waypoint
        if (needsWaypoints)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Waypoint");
            if (objs.Length > 0)
            {
                waypoints = new Transform[objs.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] != null)
                    {
                        waypoints[i] = objs[i].transform;
                    }
                }
            }
        }

        // Reset NavMeshAgent cho clone để không bị kẹt
        if (navAgent != null)
        {
            navAgent.enabled = false;
            transform.position = transform.position;
            navAgent.enabled = true;
            navAgent.isStopped = false;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
                SetAnim(false, false, false);
                if (waypoints.Length > 0) currentState = ZombieState.Patrol;
                if (dist <= chaseDistance) currentState = ZombieState.Chase;
                break;

            case ZombieState.Patrol:
                SetAnim(true, false, false);
                if (navAgent.enabled) navAgent.speed = patrolSpeed;
                PatrolBehavior();
                if (dist <= chaseDistance) currentState = ZombieState.Chase;
                break;

            case ZombieState.Chase:
                SetAnim(false, true, false);
                if (navAgent.enabled)
                {
                    navAgent.speed = chaseSpeed;
                    navAgent.isStopped = false;
                    navAgent.SetDestination(player.position);
                }
                if (dist <= attackDistance) currentState = ZombieState.Attack;
                else if (dist > loseSightDistance) currentState = waypoints.Length > 0 ? ZombieState.Patrol : ZombieState.Idle;
                break;

            case ZombieState.Attack:
                SetAnim(false, false, true);
                if (navAgent.enabled) navAgent.isStopped = true;
                LookAtPlayer();

                if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
                    StartCoroutine(AttackRoutine());

                if (dist > attackDistance && !isAttacking) 
                    currentState = ZombieState.Chase;
                break;
        }
    }

    void SetAnim(bool walk, bool chase, bool attack)
    {
        if (animator == null) return;
        animator.SetBool("IsWalking", walk);
        animator.SetBool("IsChasing", chase);
        animator.SetBool("IsAttacking", attack);
    }

    void PatrolBehavior()
    {
        if (navAgent == null || !navAgent.enabled || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        navAgent.isStopped = false;
        navAgent.SetDestination(target.position);

        if (!navAgent.pathPending && navAgent.remainingDistance <= 1.5f)
        {
            if (waypoints.Length <= 1) return;
            int next;
            do { next = Random.Range(0, waypoints.Length); } while (next == currentWaypointIndex);
            currentWaypointIndex = next;
        }
    }

    void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(0.5f); // Độ trễ khớp với animation đánh

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackDistance + 0.5f)
        {
            playerHealthComponent?.TakeDamage(damage);
            StartCoroutine(ActivateBloodScreenEffect());
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    IEnumerator ActivateBloodScreenEffect()
    {
        if (bloodScreenEffectPrefab != null && instantiatedBloodScreenEffect == null)
        {
            instantiatedBloodScreenEffect = Instantiate(bloodScreenEffectPrefab);
        }
        yield return new WaitForSeconds(0.5f); 
        
        if (instantiatedBloodScreenEffect != null)
        {
            Destroy(instantiatedBloodScreenEffect);
            instantiatedBloodScreenEffect = null;
        }
    }

    public void TakeDamage(int amt)
    {
        if (isDead) return;
        health -= amt;
        if (currentState == ZombieState.Patrol || currentState == ZombieState.Idle) 
            currentState = ZombieState.Chase;

        if (health <= 0) { health = 0; Die(); }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = ZombieState.Dead;

        SetAnim(false, false, false);
        if (animator != null) animator.SetBool("IsDead", true);

        if (instantiatedBloodScreenEffect != null)
        {
            Destroy(instantiatedBloodScreenEffect);
            instantiatedBloodScreenEffect = null;
        }

        if (navAgent != null) navAgent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 7f);
    }
}