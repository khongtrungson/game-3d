using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaypointZombieAI : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent navAgent;
    public Transform player;
    private player_health playerHealthComponent;

    public enum ZombieState { Idle, Patrol, Chase, Attack, Dead }

    public Animator animator; 
    [Header("State")]
    public ZombieState currentState = ZombieState.Idle;

    [Header("Waypoint Patrol Settings")]
    public Transform[] waypoints; 
    private int currentWaypointIndex = 0;
    public float waypointReachDistance = 1.5f;
    public float patrolSpeed = 2.0f; 

    [Header("Stats & Combat")]
    public float chaseSpeed = 5.0f;  
    public float chaseDistance = 10f;
    public float loseSightDistance = 15f; 
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float attackDelay = 0.5f; 
    public float damage = 15f;
    public int health = 100;

    [Header("Visual Effects")]
    public GameObject bloodScreenEffectPrefab;
    private GameObject instantiatedBloodScreenEffect;

    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime;

    void Start()
    {
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealthComponent = playerObj.GetComponent<player_health>();
            }
        }
        else
        {
            playerHealthComponent = player.GetComponent<player_health>();
        }

        currentState = (waypoints != null && waypoints.Length > 0) ? ZombieState.Patrol : ZombieState.Idle;
        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        // Nếu zombie đã chết hoặc không có player thì dừng xử lý AI
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
                SetAnimationParameters(false, false, false);

                if (waypoints != null && waypoints.Length > 0)
                {
                    currentState = ZombieState.Patrol;
                }

                if (distanceToPlayer <= chaseDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Patrol:
                SetAnimationParameters(true, false, false);

                if (navAgent.enabled)
                {
                    navAgent.speed = patrolSpeed;
                }

                PatrolBehavior();

                if (distanceToPlayer <= chaseDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Chase:
                SetAnimationParameters(false, true, false); 

                if (navAgent.enabled)
                {
                    navAgent.speed = chaseSpeed;
                    navAgent.isStopped = false;
                    navAgent.SetDestination(player.position);
                }

                if (distanceToPlayer <= attackDistance)
                {
                    currentState = ZombieState.Attack;
                }
                else if (distanceToPlayer > loseSightDistance)
                {
                    currentState = (waypoints != null && waypoints.Length > 0) ? ZombieState.Patrol : ZombieState.Idle;
                }
                break;

            case ZombieState.Attack:
                SetAnimationParameters(false, false, true);
                
                if (navAgent.enabled)
                {
                    navAgent.isStopped = true;
                }
                LookAtPlayer();

                if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
                {
                    StartCoroutine(AttackWithDelay());
                }

                if (distanceToPlayer > attackDistance && !isAttacking)
                {
                    currentState = ZombieState.Chase;
                }
                break;
        }
    }

    // Hàm phụ trợ giúp gọi Animator gọn gàng hơn, tránh lặp code lặp lại nhiều lần
    private void SetAnimationParameters(bool isWalking, bool isChasing, bool isAttackingState)
    {
        if (animator == null) return;
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsChasing", isChasing);
        animator.SetBool("IsAttacking", isAttackingState);
    }

    private void PatrolBehavior()
    {
        if (waypoints == null || waypoints.Length == 0 || !navAgent.enabled) return;

        navAgent.isStopped = false;
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        navAgent.SetDestination(targetWaypoint.position);

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= waypointReachDistance)
        {
            if (waypoints.Length <= 1) return;

            int nextWaypointIndex;
            do
            {
                nextWaypointIndex = Random.Range(0, waypoints.Length);
            } 
            while (nextWaypointIndex == currentWaypointIndex);

            currentWaypointIndex = nextWaypointIndex;
        }
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;

        yield return new WaitForSeconds(attackDelay);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackDistance + 0.5f)
        {
            if (playerHealthComponent != null)
            {
                playerHealthComponent.TakeDamage(damage);
            }

            StartCoroutine(ActivateBloodScreenEffect());
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private IEnumerator ActivateBloodScreenEffect()
    {
        InstantiateBloodScreenEffect();
        yield return new WaitForSeconds(0.5f); 
        DeleteBloodScreenEffect();
    }

    private void InstantiateBloodScreenEffect()
    {
        if (bloodScreenEffectPrefab != null && instantiatedBloodScreenEffect == null)
        {
            instantiatedBloodScreenEffect = Instantiate(bloodScreenEffectPrefab);
        }
    }

    private void DeleteBloodScreenEffect()
    {
        if (instantiatedBloodScreenEffect != null)
        {
            Destroy(instantiatedBloodScreenEffect);
            instantiatedBloodScreenEffect = null;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        health -= damageAmount;

        if (currentState == ZombieState.Patrol || currentState == ZombieState.Idle)
        {
            currentState = ZombieState.Chase;
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
        currentState = ZombieState.Dead;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsDead", true);
        }

        DeleteBloodScreenEffect();

        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("Zombie is dead!");

        Destroy(gameObject, 7f);
    }
}