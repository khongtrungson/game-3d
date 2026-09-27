using System.Collections;
using UnityEngine;

namespace NullProtocol.Animations
{
    public abstract class GunBase : MonoBehaviour
    {
        [Header("Ammo System")]
        [SerializeField] protected int magazineSize = 30;
        [SerializeField] protected int currentAmmo;
        [SerializeField] protected int reserveAmmo = 120;

        [Header("Base Weapon Stats")]
        [SerializeField] protected float damage = 20f;
        [SerializeField] protected float range = 100f;
        [SerializeField] protected float reloadDuration = 2f;

        [Header("Audio & VFX")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip shootSound;      // Tiếng bắn
        [SerializeField] protected AudioClip emptySound;      // Tiếng hết đạn (cạch)
        [SerializeField] protected AudioClip reloadSound;     // Tiếng nạp đạn (MỚI)
        [SerializeField] protected AudioClip takeInSound;     // Tiếng rút súng out (MỚI)
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected Transform shootPoint;

        protected Animation anim;
        protected bool isReloading = false;
        protected float nextTimeToFire = 0f;

        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => magazineSize;
        public int ReserveAmmo => reserveAmmo;

        protected virtual void Awake()
        {
            anim = GetComponent<Animation>();
            if (audioSource == null) 
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        protected virtual void Start()
        {
            currentAmmo = magazineSize;
        }

        protected virtual void OnEnable()
        {
            isReloading = false;
            
            // 1. Chạy Animation Rút súng
            if (anim != null && anim["Take_In"] != null)
            {
                anim.CrossFade("Take_In");
            }

            // 2. Phát âm thanh Rút súng (Take In)
            if (takeInSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(takeInSound);
            }
        }

        protected virtual void LateUpdate()
        {
            if (anim == null) return;

            // NẠP ĐẠN BẰNG PHÍM R
            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                if (currentAmmo < magazineSize && reserveAmmo > 0)
                {
                    StartCoroutine(ReloadCoroutine(reloadDuration));
                }
            }

            if (isReloading) return;

            HandleShooting();

            if (!Input.GetButton("Fire1") && !anim.IsPlaying("Fire") && !anim.IsPlaying("Take_In"))
            {
                anim.CrossFade("Idle");
            }
        }

        protected abstract void HandleShooting();

        protected virtual void FireRaycast(Vector3 direction)
        {
            currentAmmo--;

            if (anim != null && anim["Fire"] != null)
            {
                anim.Rewind("Fire");
                anim.Play("Fire");
            }

            if (shootSound != null && audioSource != null)
                audioSource.PlayOneShot(shootSound);

            if (muzzleFlash != null)
                muzzleFlash.Play();

            Transform origin = (shootPoint != null) ? shootPoint : Camera.main.transform;

            if (Physics.Raycast(origin.position, direction, out RaycastHit hit, range))
            {
                Debug.Log($"[{gameObject.name}] Trúng: {hit.transform.name} | Damage: {damage}");
            }
        }

        protected virtual IEnumerator ReloadCoroutine(float duration)
        {
            isReloading = true;

            // 1. Chạy Animation Nạp đạn
            if (anim != null && anim["Reload"] != null)
            {
                anim.CrossFade("Reload");
            }

            // 2. Phát âm thanh Nạp đạn (Reload)
            if (reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }

            yield return new WaitForSeconds(duration);

            int ammoNeeded = magazineSize - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;

            isReloading = false;
        }
    }
}