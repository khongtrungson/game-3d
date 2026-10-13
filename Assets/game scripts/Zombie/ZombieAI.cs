using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent navAgent;
    public Transform player;
    private player_health playerHealthComponent;
    private CapsuleCollider capsuleCollider;
    public enum ZombieState { Idle, Chase, Attack, Dead }

    public Animator animator; 
    [Header("State")]
    public ZombieState currentState = ZombieState.Idle;

    [Header("Stats & Combat")]
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
    private bool isDead = false; // THÊM BIẾN ISDEAD ĐỂ ĐỒNG BỘ
    private float lastAttackTime;

     void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealthComponent = playerObj.GetComponent<player_health>();
            }
        
            else
            {
                Debug.LogWarning("Player object not found.");
            }

        capsuleCollider = GetComponent<CapsuleCollider>();
        navAgent = GetComponent<NavMeshAgent>();
        lastAttackTime = -attackCooldown;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Sử dụng isDead để ngắt AI ngay lập tức khi chết, tránh lỗi
        if (player == null || isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", false);

                if (distanceToPlayer <= chaseDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Chase:
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsAttacking", false);

                if (navAgent.enabled)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(player.position);
                }

                if (distanceToPlayer <= attackDistance)
                {
                    currentState = ZombieState.Attack;
                }
                else if (distanceToPlayer > loseSightDistance)
                {
                    currentState = ZombieState.Idle;
                }
                break;

            case ZombieState.Attack:
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", true);

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

    private void LookAtPlayer()
    {
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
        
        if (currentState == ZombieState.Idle)
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