using UnityEngine;

namespace NullProtocol.Animations
{
    public class SniperRifle : GunBase
    {
        [Header("Sniper Specifics")]
        [SerializeField] private float fireCooldown = 1.4f; // Chờ 1.4s kéo khóa nòng mới được bắn tiếp
        [SerializeField] private float zoomFOV = 20f;
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private Camera mainCam;

        private bool isScoped = false;

        protected override void Awake()
        {
            base.Awake();
            if (mainCam == null) mainCam = Camera.main;

            // Thiết lập giá trị mặc định cho L11A3
            magazineSize = 5;
            reserveAmmo = 20;
            damage = 120f;
            reloadDuration = 3.2f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            isScoped = false;
            if (mainCam != null) mainCam.fieldOfView = defaultFOV;
        }

        protected override void HandleShooting()
        {
            // Chuột phải: Bật / Tắt Ống ngắm (ADS Zoom)
            if (Input.GetMouseButtonDown(1))
            {
                isScoped = !isScoped;
                if (mainCam != null) mainCam.fieldOfView = isScoped ? zoomFOV : defaultFOV;
            }

            // Chuột trái: Bắn 1 viên chuẩn xác 100%
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + fireCooldown;

                if (currentAmmo > 0)
                {
                    Transform origin = (shootPoint != null) ? shootPoint : mainCam.transform;
                    FireRaycast(origin.forward);
                }
                else if (emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
            }
        }

        private void OnDisable()
        {
            // Reset lại góc nhìn Camera khi cất súng ngắm
            if (mainCam != null) mainCam.fieldOfView = defaultFOV;
        }
    }
}