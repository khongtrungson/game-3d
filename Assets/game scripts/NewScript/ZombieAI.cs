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

    public enum ZombieState { Idle, Chase, Attack, Dead }

    public Animator animator; // Kéo Animator của Zombie vào đây từ Inspector
    [Header("State")]
    public ZombieState currentState = ZombieState.Idle;

    [Header("Stats & Combat")]
    public float chaseDistance = 10f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float attackDelay = 0.5f; // Thời gian cào (tung đòn) trước khi trừ máu
    public float damage = 15f;
    public int health = 100;

    [Header("Visual Effects")]
    public GameObject bloodScreenEffectPrefab;
    private GameObject instantiatedBloodScreenEffect;

    private bool isAttacking = false;
    private float lastAttackTime;

    void Start()
    {
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        // Tự động tìm Player nếu chưa kéo thả trong Inspector
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

        // Cho phép tấn công ngay khi tiếp cận
        lastAttackTime = -attackCooldown;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Tránh lỗi NullReferenceException nếu Player không tồn tại hoặc đã chết
        if (player == null || currentState == ZombieState.Dead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
                // animations.SetTrigger("Idle");
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", false);

                if (distanceToPlayer <= chaseDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Chase:
                // animations.SetTrigger("Run");
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
                break;

            case ZombieState.Attack:
                animator.SetBool("IsAttacking", true);
                // Dừng di chuyển khi tấn công và xoay về phía Player
                if (navAgent.enabled)
                {
                    navAgent.isStopped = true;
                }
                LookAtPlayer();

                if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
                {
                    StartCoroutine(AttackWithDelay());
                }

                // Nếu Player chạy ra ngoài tầm đánh -> Đuổi theo tiếp
                if (distanceToPlayer > attackDistance && !isAttacking)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Dead:
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsDead", true);

                break;
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Tránh zombie bị nghiêng người lên/xuống
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;

        // Chờ tới thời điểm vung tay trúng Player (dựa theo animation cào)
        yield return new WaitForSeconds(attackDelay);

        // Gây sát thương và tạo hiệu ứng máu lên màn hình nếu Player vẫn ở trong khoảng cách đánh
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackDistance + 0.5f)
        {
            if (playerHealthComponent != null)
            {
                playerHealthComponent.TakeDamage(damage);
            }

            // Kích hoạt hiệu ứng máu khi đánh trúng
            StartCoroutine(ActivateBloodScreenEffect());
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private IEnumerator ActivateBloodScreenEffect()
    {
        InstantiateBloodScreenEffect();
        yield return new WaitForSeconds(0.5f); // Hiệu ứng máu hiển thị trong 0.5 giây
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
        if (currentState == ZombieState.Dead) return;

        health -= damageAmount;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    private void Die()
    {
        currentState = ZombieState.Dead;

        // Xóa hiệu ứng máu nếu Zombie chết khi hiệu ứng đang hiện
        DeleteBloodScreenEffect();

        // Tắt AI Navigation khi Zombie chết để Player đi xuyên qua được
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        // Tắt Collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("Zombie is dead!");

        // Hủy GameObject Zombie sau 5 giây để dọn dẹp bộ nhớ
        Destroy(gameObject, 5f);
    }
}