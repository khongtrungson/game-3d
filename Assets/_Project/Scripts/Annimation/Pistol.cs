using UnityEngine;

namespace NullProtocol.Animations
{
    public class Pistol : GunBase
    {
        [Header("Pistol Specifics")]
        [SerializeField] private float fireRate = 0.15f; // Thời gian chờ tối thiểu giữa 2 lần nhấp

        protected override void Awake()
        {
            base.Awake();
            // Thiết lập giá trị mặc định cho Súng Lục
            magazineSize = 12;
            reserveAmmo = 48;
            damage = 25f;
            reloadDuration = 1.2f;
        }

        protected override void HandleShooting()
        {
            // Bắt buộc nhấp chuột từng phát
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + fireRate;

                if (currentAmmo > 0)
                {
                    Transform origin = (shootPoint != null) ? shootPoint : Camera.main.transform;
                    FireRaycast(origin.forward); // Đạn đi thẳng chính xác
                }
                else if (emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
            }
        }
    }
}