using System.Collections;
using UnityEngine;

namespace NullProtocol.Animations
{
    public class Knife : GunBase
    {
        [Header("Knife / Melee Settings")]
        [SerializeField] private float lightAttackDamage = 35f;   // Sát thương chém (Chuột trái)
        [SerializeField] private float heavyAttackDamage = 75f;   // Sát thương đâm (Chuột phải)
        [SerializeField] private float attackRange = 2.5f;        // Tầm chém cận chiến (m)
        [SerializeField] private float attackRate = 0.4f;         // Tốc độ vung dao

        [Header("Knife Audio Clips")]
        [SerializeField] private AudioClip swingSound;           // Tiếng vung dao (chém gió)
        [SerializeField] private AudioClip hitSound;             // Tiếng chém trúng mục tiêu

        protected override void Awake()
        {
            base.Awake();
            // Dao không dùng hệ thống đạn
            magazineSize = 0;
            currentAmmo = 0;
            reserveAmmo = 0;
            reloadDuration = 0f;
            range = attackRange;
        }

        protected override void HandleShooting()
        {
            // Dao không nạp đạn -> Bỏ qua phím R

            // 1. CHÉM THƯỜNG (Chuột trái)
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + attackRate;
                StartCoroutine(PerformMeleeAttack(lightAttackDamage));
            }
            // 2. ĐÂM MẠNH (Chuột phải)
            else if (Input.GetMouseButtonDown(1) && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + (attackRate * 1.5f);
                StartCoroutine(PerformMeleeAttack(heavyAttackDamage));
            }
        }

        private IEnumerator PerformMeleeAttack(float attackDamage)
        {
            // 1. Chạy Animation Bắn/Chém mặc định ("Fire")
            if (anim != null && anim["Fire"] != null)
            {
                anim.Rewind("Fire");
                anim.Play("Fire");
            }

            // 2. Phát âm thanh vung dao
            if (swingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(swingSound);
            }

            yield return new WaitForSeconds(0.1f); // Độ trễ ngắn cho vệt chém

            // 3. Kiểm tra trúng mục tiêu & phát tiếng Hit
            Transform origin = (shootPoint != null) ? shootPoint : Camera.main.transform;

            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, attackRange))
            {
                Debug.Log($"[Knife] Chém trúng: {hit.transform.name} | Sát thương: {attackDamage}");

                if (hitSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
            }
        }
    }
}