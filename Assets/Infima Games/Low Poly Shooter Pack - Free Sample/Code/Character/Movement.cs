// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        [Tooltip("Lực nhảy của nhân vật"), SerializeField]
        private float jumpForce = 25f;

        [Header("Mobile Joystick Reference")]
        [SerializeField]
        private Joystick mobileJoystick;

        #endregion

        #region PROPERTIES

        private Vector3 Velocity
        {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSource;
        
        private bool grounded;
        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;
        
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        protected override void Awake()
        {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        protected override void Start()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            capsule = GetComponent<CapsuleCollider>();

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            // Tự động tìm joystick trong scene nếu chưa kéo thả vào Inspector
            if (mobileJoystick == null)
            {
                mobileJoystick = FindObjectOfType<Joystick>();
            }
        }

        private void OnCollisionStay()
        {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = extents.x - 0.01f;
            
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            
            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule)) 
                return;
            
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            grounded = true;
        }
            
        protected override void FixedUpdate()
        {
            MoveCharacter();
            grounded = false;
        }

        protected override void Update()
        {
            if (playerCharacter != null && playerCharacter.GetInventory() != null)
            {
                equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            }
            
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        private void MoveCharacter()
        {
            #region Calculate Movement Velocity

            // 1. Lấy input từ joystick Android
            Vector2 frameInput = Vector2.zero;
            bool usingJoystick = false;

            if (mobileJoystick != null && mobileJoystick.Direction.sqrMagnitude > 0.001f)
            {
                frameInput = mobileJoystick.Direction;
                usingJoystick = true;
            }

            // 2. Nếu không sử dụng joystick thì lấy input bàn phím gốc (PC)
            if (!usingJoystick)
            {
                frameInput = playerCharacter.GetInputMovement();
            }

            // 3. Giới hạn input tối đa để tránh tăng tốc thô bạo khi di chuyển chéo
            frameInput = Vector2.ClampMagnitude(frameInput, 1f);

            // 4. Xác định đi bộ hoặc chạy
            bool isRunning;

            if (usingJoystick)
            {
                isRunning = frameInput.sqrMagnitude >= 0.6f * 0.6f;
            }
            else
            {
                isRunning = playerCharacter.IsRunning();
            }

            // 5. Tính tốc độ di chuyển cơ bản và tỉ lệ độ lớn lực đẩy joystick
            float moveSpeed = isRunning ? speedRunning : speedWalking;
            float inputMagnitude = usingJoystick ? frameInput.magnitude : 1f;

            Vector3 movement = new Vector3(
                frameInput.x,
                0f,
                frameInput.y
            );

            movement *= moveSpeed * inputMagnitude;

            // 6. Tạo độ rung lắc nhẹ (bobbing) mô phỏng bước chân cho Joystick
            if (grounded && usingJoystick && frameInput.sqrMagnitude > 0.01f)
            {
                float bobFrequency = isRunning ? 60f : 10f;
                float bobAmplitude = isRunning ? 0.2f : 0.02f;
                
                float waveSlice = Mathf.Sin(Time.time * bobFrequency);
                movement.z += waveSlice * bobAmplitude * speedWalking;
            }

            // 7. Chuyển hướng di chuyển sang tọa độ thế giới
            movement = transform.TransformDirection(movement);

            // 8. Nhảy bằng phím Space trên PC hoặc nút ảo Mobile
            if (Input.GetKeyDown(KeyCode.Space) && grounded)
            {
                MobileJump();
            }

            #endregion

            // 9. Cập nhật vận tốc cho Rigidbody
            if (grounded)
            {
                Velocity = new Vector3(movement.x, 0f, movement.z);
            }
            else
            {
                // Giữ lại vận tốc theo trục Y khi đang nhảy hoặc rơi tự do
                Velocity = new Vector3(movement.x, rigidBody.linearVelocity.y, movement.z);
            }
        }

        // 10. Hàm nhảy dành cho nút bấm ảo trên Android (Gắn vào sự kiện OnClick của UI Button)
        public void MobileJump()
        {
            if (!grounded)
                return;

            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            grounded = false;
        }

        private void PlayFootstepSounds()
        {
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        #endregion
    }
}