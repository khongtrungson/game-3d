// Copyright 2021, Infima Games. All Rights Reserved.

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Main Character Component. This component handles the most important functions of the character, and interfaces
    /// with basically every part of the asset, it is the hub where it all converges.
    /// </summary>
    [RequireComponent(typeof(CharacterKinematics))]
    public sealed class Character : CharacterBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Inventory")]
        
        [Tooltip("Inventory.")]
        [SerializeField]
        private InventoryBehaviour inventory;

        [Header("Cameras")]

        [Tooltip("Normal Camera.")]
        [SerializeField]
        private Camera cameraWorld;

        [Header("Animation")]

        [Tooltip("Determines how smooth the locomotion blendspace is.")]
        [SerializeField]
        private float dampTimeLocomotion = 0.15f;

        [Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
        [SerializeField]
        private float dampTimeAiming = 0.3f;
        
        [Header("Animation Procedural")]
        
        [Tooltip("Character Animator.")]
        [SerializeField]
        private Animator characterAnimator;

        #endregion

        #region FIELDS

        /// <summary>
        /// True if the character is aiming.
        /// </summary>
        private bool aiming;
        /// <summary>
        /// True if the character is running.
        /// </summary>
        private bool running;
        /// <summary>
        /// True if the character has its weapon holstered.
        /// </summary>
        private bool holstered;
        
        /// <summary>
        /// Last Time.time at which we shot.
        /// </summary>
        private float lastShotTime;
        
        /// <summary>
        /// Overlay Layer Index. Useful for playing things like firing animations.
        /// </summary>
        private int layerOverlay;
        /// <summary>
        /// Holster Layer Index. Used to play holster animations.
        /// </summary>
        private int layerHolster;
        /// <summary>
        /// Actions Layer Index. Used to play actions like reloading.
        /// </summary>
        private int layerActions;

        /// <summary>
        /// Character Kinematics. Handles all the IK stuff.
        /// </summary>
        private CharacterKinematics characterKinematics;
        
        /// <summary>
        /// The currently equipped weapon.
        /// </summary>
        private WeaponBehaviour equippedWeapon;
        /// <summary>
        /// The equipped weapon's attachment manager.
        /// </summary>
        private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
        
        /// <summary>
        /// The scope equipped on the character's weapon.
        /// </summary>
        private ScopeBehaviour equippedWeaponScope;
        /// <summary>
        /// The magazine equipped on the character's weapon.
        /// </summary>
        private MagazineBehaviour equippedWeaponMagazine;
        
        /// <summary>
        /// True if the character is reloading.
        /// </summary>
        private bool reloading;
        
        /// <summary>
        /// True if the character is inspecting its weapon.
        /// </summary>
        private bool inspecting;

        /// <summary>
        /// True if the character is in the middle of holstering a weapon.
        /// </summary>
        private bool holstering;

        /// <summary>
        /// Look Axis Values.
        /// </summary>
        private Vector2 axisLook;
        /// <summary>
        /// Look Axis Values.
        /// </summary>
        private Vector2 axisMovement;
        
        /// <summary>
        /// True if the player is holding the aiming button.
        /// </summary>
        private bool holdingButtonAim;
        /// <summary>
        /// True if the player is holding the running button.
        /// </summary>
        private bool holdingButtonRun;
        /// <summary>
        /// True if the player is holding the firing button.
        /// </summary>
        private bool holdingButtonFire;

        /// <summary>
        /// If true, the tutorial text should be visible on screen.
        /// </summary>
        private bool tutorialTextVisible;

        /// <summary>
        /// True if the game cursor is locked! Used when pressing "Escape" to allow developers to more easily access the editor.
        /// </summary>
        private bool cursorLocked;
        public GameObject checking;
        private Transform player;
        private GameObject[] bullet_kit;
        private bool reloading_range=false;

        #endregion

        #region CONSTANTS

        /// <summary>
        /// Aiming Alpha Value.
        /// </summary>
        private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");

        /// <summary>
        /// Hashed "Movement".
        /// </summary>
        private static readonly int HashMovement = Animator.StringToHash("Movement");

        #endregion

        #region UNITY

        protected override void Awake()
        {
            #region Lock Cursor

            //Always make sure that our cursor is locked when the game starts!
            cursorLocked = true;
            //Update the cursor's state.
            UpdateCursorState();
            #endregion

            //Cache the CharacterKinematics component.
            characterKinematics = GetComponent<CharacterKinematics>();

            //Initialize Inventory.
            inventory.Init();

            //Refresh!
            RefreshWeaponSetup();
        }
        protected override void Start()
        {
            //Cache a reference to the holster layer's index.
            layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
            //Cache a reference to the action layer's index.
            layerActions = characterAnimator.GetLayerIndex("Layer Actions");
            //Cache a reference to the overlay layer's index.
            layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
        }

        protected override void Update()
        {
            // Nếu game đang pause (Time.timeScale bằng 0) thì không chạy xử lý trong Update
            if (Time.timeScale == 0f) return;

            //Match Aim.
            aiming = holdingButtonAim && CanAim();
            //Match Run.
            running = holdingButtonRun && CanRun();

            //Holding the firing button.
            if (holdingButtonFire)
            {   
                checking = GameObject.FindGameObjectWithTag("checking");
                //Check.
                if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
                {
                    //Has fire rate passed.
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                        if(checking==null){
                        Instantiate(Resources.Load("checking") as GameObject);
                        }
                }   
                else{
                    Destroy(checking);
                }
            }

            //Update Animator.
            UpdateAnimator();
        }

        protected override void LateUpdate()
        {
            if (Time.timeScale == 0f) return;

            //We need a weapon for this!
            if (equippedWeapon == null)
                return;

            //Weapons without a scope should not be a thing! Ironsights are a scope too!
            if (equippedWeaponScope == null)
                return;
            
            //Make sure that we have a kinematics component!
            if(characterKinematics != null)
            {
                //Compute.
                characterKinematics.Compute();
            }
        }
        
        #endregion

        #region GETTERS

        public override Camera GetCameraWorld() => cameraWorld;

        public override InventoryBehaviour GetInventory() => inventory;
        
        public override bool IsCrosshairVisible() => !aiming && !holstered;
        public override bool IsRunning() => running;
        
        public override bool IsAiming() => aiming;
        public override bool IsCursorLocked() => cursorLocked;
        
        public override bool IsTutorialTextVisible() => tutorialTextVisible;
        
        public override Vector2 GetInputMovement() => axisMovement;
        public override Vector2 GetInputLook() => axisLook;

        #endregion

        #region METHODS

        /// <summary>
        /// Updates all the animator properties for this frame.
        /// </summary>
        private void UpdateAnimator()
        {
            //Movement Value. This value affects absolute movement. Aiming movement uses this, as opposed to per-axis movement.
            characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);
            
            //Update the aiming value, but use interpolation. This makes sure that things like firing can transition properly.
            characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);

            //Update Animator Aiming.
            const string boolNameAim = "Aim";
            characterAnimator.SetBool(boolNameAim, aiming);
            
            //Update Animator Running.
            const string boolNameRun = "Running";
            characterAnimator.SetBool(boolNameRun, running);
        }
        
        /// <summary>
        /// Plays the inspect animation.
        /// </summary>
        private void Inspect()
        {
            //State.
            inspecting = true;
            //Play.
            characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
        }
        
        /// <summary>
        /// Fires the character's weapon.
        /// </summary>
        private void Fire()
        {
            //Save the shot time, so we can calculate the fire rate correctly.
            lastShotTime = Time.time;
            //Fire the weapon! Make sure that we also pass the scope's spread multiplier if we're aiming.
            equippedWeapon.Fire();

            //Play firing animation.
            const string stateName = "Fire";
            characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);
        }

        private void PlayReloadAnimation()
        {
            #region Animation
            player = GameObject.FindGameObjectWithTag("Player").transform;
            bullet_kit = GameObject.FindGameObjectsWithTag("bullet_kit");
            foreach(var kit in bullet_kit){
                float dis = Vector3.Distance(player.position, kit.transform.position);
                if(dis < 2.5f){
                    reloading_range = true;
                }
            }
                if(reloading_range){
                    reloading_range = false;
            //Get the name of the animation state to play, which depends on weapon settings, and ammunition!
            string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
            //Play the animation state!
            characterAnimator.Play(stateName, layerActions, 0.0f);

            //Set.
            reloading = true;
            }

            #endregion

            //Reload.
            equippedWeapon.Reload();
        }

        /// <summary>
        /// Equip Weapon Coroutine.
        /// </summary>
        private IEnumerator Equip(int index = 0)
        {
            //Only if we're not holstered, holster. If we are already, we don't need to wait.
            if(!holstered)
            {
                //Holster.
                SetHolstered(holstering = true);
                //Wait.
                yield return new WaitUntil(() => holstering == false);
            }
            //Unholster. We do this just in case we were holstered.
            SetHolstered(false);
            //Play Unholster Animation.
            characterAnimator.Play("Unholster", layerHolster, 0);
            
            //Equip The New Weapon.
            inventory.Equip(index);
            //Refresh.
            RefreshWeaponSetup();
        }

        /// <summary>
        /// Refresh all weapon things to make sure we're all set up!
        /// </summary>
        private void RefreshWeaponSetup()
        {
            //Make sure we have a weapon. We don't want errors!
            if ((equippedWeapon = inventory.GetEquipped()) == null)
                return;
            
            //Update Animator Controller. We do this to update all animations to a specific weapon's set.
            characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

            //Get the attachment manager so we can use it to get all the attachments!
            weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
            if (weaponAttachmentManager == null) 
                return;
            
            //Get equipped scope. We need this one for its settings!
            equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
            //Get equipped magazine. We need this one for its settings!
            equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
        }

        private void FireEmpty()
        {
            lastShotTime = Time.time;
            //Play.
            characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
        }

        /// <summary>
        /// Updates the cursor state based on the value of the cursorLocked variable.
        /// </summary>
        private void UpdateCursorState()
        {
            //Update cursor visibility.
            Cursor.visible = !cursorLocked;
            //Update cursor lock state.
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        /// <summary>
        /// Updates the "Holstered" variable, along with the Character's Animator value.
        /// </summary>
        private void SetHolstered(bool value = true)
        {
            //Update value.
            holstered = value;
            
            //Update Animator.
            const string boolName = "Holstered";
            characterAnimator.SetBool(boolName, holstered); 
        }
        
        #region ACTION CHECKS

        private bool CanPlayAnimationFire()
        {
            if (holstered || holstering)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;

            return true;
        }

        private bool CanPlayAnimationReload()
        {
            if (reloading)
                return false;

            if (inspecting)
                return false;
            
            return true;
        }

        private bool CanPlayAnimationHolster()
        {
            if (reloading)
                return false;

            if (inspecting)
                return false;
            
            return true;
        }

        private bool CanChangeWeapon()
        {
            if (holstering)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;
            
            return true;
        }

        private bool CanPlayAnimationInspect()
        {
            if (holstered || holstering)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;
            
            return true;
        }

        private bool CanAim()
        {
            if (holstered || inspecting)
                return false;

            if (reloading || holstering)
                return false;
            
            return true;
        }
        
        private bool CanRun()
        {
            if (inspecting)
                return false;

            if (reloading || aiming)
                return false;

            if (holdingButtonFire && equippedWeapon.HasAmmunition())
                return false;

            if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
                return false;
            
            return true;
        }

        #endregion

        #region INPUT

        public void OnTryFire(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;

            switch (context)
            {
                case {phase: InputActionPhase.Started}:
                    holdingButtonFire = true;
                    break;
                case {phase: InputActionPhase.Performed}:
                    if (!CanPlayAnimationFire())
                        break;
                    
                    if (equippedWeapon.HasAmmunition())
                    {
                        if (equippedWeapon.IsAutomatic())
                            break;
                            
                        if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                            Fire();
                        if(checking==null){
                        Instantiate(Resources.Load("checking") as GameObject);
                        }   
                        else{
                            Destroy(checking);
                        }
                    }
                    else
                        FireEmpty();
                    break;
                case {phase: InputActionPhase.Canceled}:
                    holdingButtonFire = false;
                    break;
            }
        }

        public void OnTryPlayReload(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;
            
            if (!CanPlayAnimationReload())
                return;
            
            switch (context)
            {
                case {phase: InputActionPhase.Performed}:
                    PlayReloadAnimation();
                    break;
            }
        }

        public void OnTryInspect(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;
            
            if (!CanPlayAnimationInspect())
                return;
            
            switch (context)
            {
                case {phase: InputActionPhase.Performed}:
                    Inspect();
                    break;
            }
        }

        public void OnTryAiming(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;

            switch (context.phase)
            {
                case InputActionPhase.Started:
                    holdingButtonAim = true;
                    break;
                case InputActionPhase.Canceled:
                    holdingButtonAim = false;
                    break;
            }
        }

        public void OnTryHolster(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;
            
            switch (context.phase)
            {
                case InputActionPhase.Performed:
                    if (CanPlayAnimationHolster())
                    {
                        SetHolstered(!holstered);
                        holstering = true;
                    }
                    break;
            }
        }

        public void OnTryRun(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;
            
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    holdingButtonRun = true;
                    break;
                case InputActionPhase.Canceled:
                    holdingButtonRun = false;
                    break;
            }
        }

        public void OnTryInventoryNext(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.timeScale == 0f)
                return;
            
            if (inventory == null)
                return;
            
            switch (context)
            {
                case {phase: InputActionPhase.Performed}:
                    float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;
                    
                    int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetLastIndex();
                    int indexCurrent = inventory.GetEquippedIndex();
                    
                    if (CanChangeWeapon() && (indexCurrent != indexNext))
                        StartCoroutine(nameof(Equip), indexNext);
                    break;
            }
        }
        
        public void OnLockCursor(InputAction.CallbackContext context)
        {
            if (Time.timeScale == 0f) return;

            switch (context)
            {
                case {phase: InputActionPhase.Performed}:
                    cursorLocked = !cursorLocked;
                    UpdateCursorState();
                    break;
            }
        }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            axisMovement = (cursorLocked && Time.timeScale > 0f) ? context.ReadValue<Vector2>() : default;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            axisLook = (cursorLocked && Time.timeScale > 0f) ? context.ReadValue<Vector2>() : default;
        }

        public void OnUpdateTutorial(InputAction.CallbackContext context)
        {
            tutorialTextVisible = context switch
            {
                {phase: InputActionPhase.Started} => true,
                {phase: InputActionPhase.Canceled} => false,
                _ => tutorialTextVisible
            };
        }

        #endregion

        #region ANIMATION EVENTS

        public override void EjectCasing()
        {
            if(equippedWeapon != null)
                equippedWeapon.EjectCasing();
        }
        public override void FillAmmunition(int amount)
        {
            if(equippedWeapon != null)
                equippedWeapon.FillAmmunition(amount);
        }

		// Hàm này cho phép PauseManager gọi để ép khóa lại chuột và cho phép điều khiển khi Resume
        public void ForceLockCursor(bool lockState)
        {
            cursorLocked = lockState;
            UpdateCursorState();
        }
        
        public override void SetActiveMagazine(int active)
        {
            equippedWeaponMagazine.gameObject.SetActive(active != 0);
        }
        
        public override void AnimationEndedReload()
        {
            reloading = false;
        }

        public override void AnimationEndedInspect()
        {
            inspecting = false;
        }
        public override void AnimationEndedHolster()
        {
            holstering = false;
        }

        #endregion

        #endregion
    }
}