// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// Interface Element.
    /// </summary>
    public abstract class Element : MonoBehaviour
    {
        #region FIELDS
        
        /// <summary>
        /// Game Mode Service.
        /// </summary>
        protected IGameModeService gameModeService;
        
        /// <summary>
        /// Player Character.
        /// </summary>
        protected CharacterBehaviour playerCharacter;
        /// <summary>
        /// Player Character Inventory.
        /// </summary>
        protected InventoryBehaviour playerCharacterInventory;

        /// <summary>
        /// Equipped Weapon.
        /// </summary>
        protected WeaponBehaviour equippedWeapon;
        // private pause_menu_script pms;
        
        [Header("Weapon Binding")]
        [Tooltip("Weapon index to track. -1 to track currently equipped weapon, 0 for AR, 1 for Handgun.")]
        [SerializeField]
        protected int targetWeaponIndex = -1;

        [Tooltip("If true, auto-detect target weapon index from hierarchy names (e.g. 'Handgun' -> 1, 'AR'/'Ammo' -> 0).")]
        [SerializeField]
        protected bool autoDetectWeaponIndex = true;
        
        #endregion

        #region UNITY

        /// <summary>
        /// Awake.
        /// </summary>
        protected virtual void Awake()
        {
            //Get Game Mode Service. Very useful to get Game Mode references.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            
            //Get Player Character.
            playerCharacter = gameModeService.GetPlayerCharacter();
            //Get Player Character Inventory.
            playerCharacterInventory = playerCharacter != null ? playerCharacter.GetInventory() : null;
        }

        /// <summary>
        /// Start. Auto-detect weapon binding based on parent hierarchy.
        /// </summary>
        protected virtual void Start()
        {
            if (autoDetectWeaponIndex && targetWeaponIndex == -1)
            {
                Transform curr = transform;
                while (curr != null)
                {
                    string n = curr.name.ToLowerInvariant();
                    if (n.Contains("handgun") || n.Contains("pistol"))
                    {
                        targetWeaponIndex = 1;
                        break;
                    }
                    else if (n.Contains("ar") || n.Contains("rifle") || n == "ammo")
                    {
                        targetWeaponIndex = 0;
                        break;
                    }
                    curr = curr.parent;
                }
            }
        }
        
        /// <summary>
        /// Update.
        /// </summary>
        private void Update()
        {
            //Ensure we have Inventory reference.
            if (Equals(playerCharacterInventory, null))
            {
                if (playerCharacter != null)
                    playerCharacterInventory = playerCharacter.GetInventory();
                else if (gameModeService != null)
                {
                    playerCharacter = gameModeService.GetPlayerCharacter();
                    if (playerCharacter != null)
                        playerCharacterInventory = playerCharacter.GetInventory();
                }

                if (Equals(playerCharacterInventory, null))
                    return;
            }

            //Get Target or Equipped Weapon.
            if (targetWeaponIndex >= 0)
            {
                equippedWeapon = playerCharacterInventory.GetWeapon(targetWeaponIndex);
                if (equippedWeapon == null)
                    equippedWeapon = playerCharacterInventory.GetEquipped();
            }
            else
            {
                equippedWeapon = playerCharacterInventory.GetEquipped();
            }

            if (equippedWeapon == null)
                return;
            
            //Tick.
            Tick();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Tick.
        /// </summary>
        // protected virtual void pause_menu_script() {}
        protected virtual void Tick() {}

        #endregion
    }
}