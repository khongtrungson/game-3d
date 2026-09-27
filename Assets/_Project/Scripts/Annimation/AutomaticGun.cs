using UnityEngine;

namespace NullProtocol.Animations
{
    public class AutomaticGun : GunBase
    {
        [Header("Automatic Specifics")]
        [SerializeField] private float fireRate = 0.08f;
        [SerializeField] private float baseSpread = 0.01f;
        [SerializeField] private float maxSpread = 0.05f;
        [SerializeField] private float spreadIncreaseRate = 0.008f;

        private float currentSpread;

        protected override void Awake()
        {
            base.Awake();
            // Thiết lập giá trị mặc định cho UMP_40
            magazineSize = 30;
            reserveAmmo = 120;
            damage = 18f;
            reloadDuration = 1.8f;
        }

        protected override void Start()
        {
            base.Start();
            currentSpread = baseSpread;
        }

        protected override void HandleShooting()
        {
            // Giữ chuột để xả đạn liên tục
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + fireRate;

                if (currentAmmo > 0)
                {
                    // Tăng độ lắc/xòe đạn khi sấy
                    currentSpread = Mathf.Min(currentSpread + spreadIncreaseRate, maxSpread);

                    Transform origin = (shootPoint != null) ? shootPoint : Camera.main.transform;
                    Vector3 fireDir = origin.forward;
                    fireDir.x += Random.Range(-currentSpread, currentSpread);
                    fireDir.y += Random.Range(-currentSpread, currentSpread);

                    FireRaycast(fireDir);
                }
                else if (Input.GetButtonDown("Fire1") && emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
            }

            // Nhả chuột thì reset lại độ chính xác
            if (!Input.GetButton("Fire1"))
            {
                currentSpread = baseSpread;
            }
        }
    }
}