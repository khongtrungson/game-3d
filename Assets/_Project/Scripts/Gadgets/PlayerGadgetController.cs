using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Player controller for managing and deploying fortification gadgets (FR-16, FR-18, FR-20).
    /// - Hard-Light Barricades (2 charges, key 1 or G)
    /// - Null-Cloud Smoke (2 charges, key 2 or F)
    /// - Logic-Trip Mines (1 charge, key 3 or X)
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerGadgetController : MonoBehaviour, IResettable
    {
        [Header("Gadget Charges (FR-16, FR-18, FR-20)")]
        [SerializeField] private int _barricadeCharges = 2;
        [SerializeField] private int _smokeCharges = 2;
        [SerializeField] private int _tripMineCharges = 1;

        [Header("Origins & References")]
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private Camera _playerCamera;

        [Header("Prefabs")]
        [SerializeField] private DeployableBarricade _barricadePrefab;
        [SerializeField] private BarricadePuck _puckPrefab;
        [SerializeField] private NullCloudSmoke _smokePrefab;
        [SerializeField] private SmokeCanister _smokeCanisterPrefab;
        [SerializeField] private LogicTripMine _tripMinePrefab;

        [Header("Event Channels")]
        [SerializeField] private GadgetEventChannelSO _gadgetChannel;

        [Header("Input Asset (Optional)")]
        [SerializeField] private InputActionAsset _inputActions;

        // Internal State
        private const int INITIAL_BARRICADE_CHARGES = 2;
        private const int INITIAL_SMOKE_CHARGES = 2;
        private const int INITIAL_TRIP_MINE_CHARGES = 1;

        public event Action<GadgetType, int> OnChargesChanged;

        public int BarricadeCharges => _barricadeCharges;
        public int SmokeCharges => _smokeCharges;
        public int TripMineCharges => _tripMineCharges;

        private InputAction _barricadeAction;
        private InputAction _smokeAction;
        private InputAction _tripMineAction;

        public InputActionAsset InputActions
        {
            get => _inputActions;
            set
            {
                if (_inputActions != value)
                {
                    DisableInputActions();
                    _inputActions = value;
                    InitializeInputActions();
                    if (isActiveAndEnabled)
                    {
                        EnableInputActions();
                    }
                }
            }
        }

        private void InitializeInputActions()
        {
            if (_inputActions == null) return;
            var playerMap = _inputActions.FindActionMap("Player");
            if (playerMap == null) return;

            _barricadeAction = playerMap.FindAction("GadgetBarricade");
            _smokeAction = playerMap.FindAction("GadgetSmoke");
            _tripMineAction = playerMap.FindAction("GadgetTripMine");
        }

        private void EnableInputActions()
        {
            if (_barricadeAction != null)
            {
                _barricadeAction.performed += OnBarricadeAction;
                _barricadeAction.Enable();
            }
            if (_smokeAction != null)
            {
                _smokeAction.performed += OnSmokeAction;
                _smokeAction.Enable();
            }
            if (_tripMineAction != null)
            {
                _tripMineAction.performed += OnTripMineAction;
                _tripMineAction.Enable();
            }
        }

        private void DisableInputActions()
        {
            if (_barricadeAction != null)
            {
                _barricadeAction.performed -= OnBarricadeAction;
                _barricadeAction.Disable();
            }
            if (_smokeAction != null)
            {
                _smokeAction.performed -= OnSmokeAction;
                _smokeAction.Disable();
            }
            if (_tripMineAction != null)
            {
                _tripMineAction.performed -= OnTripMineAction;
                _tripMineAction.Disable();
            }
        }

        private void OnBarricadeAction(InputAction.CallbackContext ctx)
        {
            TryDeployBarricade();
        }

        private void OnSmokeAction(InputAction.CallbackContext ctx)
        {
            if (!InteractionState.IsTerminalActive())
            {
                TryDeploySmoke();
            }
        }

        private void OnTripMineAction(InputAction.CallbackContext ctx)
        {
            TryDeployTripMine();
        }

        private void Awake()
        {
            if (_playerCamera == null)
            {
                _playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
            }

            if (_throwOrigin == null && _playerCamera != null)
            {
                _throwOrigin = _playerCamera.transform;
            }
            else if (_throwOrigin == null)
            {
                _throwOrigin = transform;
            }

            _barricadeCharges = INITIAL_BARRICADE_CHARGES;
            _smokeCharges = INITIAL_SMOKE_CHARGES;
            _tripMineCharges = INITIAL_TRIP_MINE_CHARGES;

            InitializeInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();

            if (_gadgetChannel != null)
            {
                _gadgetChannel.OnGadgetRefunded += HandleGadgetRefunded;
            }
        }

        private void OnDisable()
        {
            DisableInputActions();

            if (_gadgetChannel != null)
            {
                _gadgetChannel.OnGadgetRefunded -= HandleGadgetRefunded;
            }
        }

        private void Update()
        {
            // Fallback direct keyboard input for standalone usage when actions are not bound
            if (_barricadeAction == null && _smokeAction == null && _tripMineAction == null)
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // FR-16: Key 1 or G -> Hard-Light Barricade
            if (keyboard[Key.Digit1].wasPressedThisFrame || keyboard[Key.G].wasPressedThisFrame)
            {
                TryDeployBarricade();
            }

            // FR-18: Key 2 or F -> Null-Cloud Smoke (F only if not interacting with Data Core terminal)
            if (keyboard[Key.Digit2].wasPressedThisFrame || (keyboard[Key.F].wasPressedThisFrame && !InteractionState.IsTerminalActive()))
            {
                TryDeploySmoke();
            }

            // FR-20: Key 3 or X -> Logic-Trip Mine
            if (keyboard[Key.Digit3].wasPressedThisFrame || keyboard[Key.X].wasPressedThisFrame)
            {
                TryDeployTripMine();
            }
        }

        public bool TryDeployBarricade()
        {
            if (_barricadeCharges <= 0)
            {
                NullLog.Info("PlayerGadgets", "No Hard-Light Barricade charges remaining.");
                return false;
            }

            _barricadeCharges--;
            NullLog.Info("PlayerGadgets", $"Deploying Hard-Light Barricade! Remaining charges: {_barricadeCharges} (FR-16)");
            _gadgetChannel?.RaiseGadgetUsed(GadgetType.HardLightBarricade, _barricadeCharges);
            OnChargesChanged?.Invoke(GadgetType.HardLightBarricade, _barricadeCharges);

            ExecuteBarricadeToss();
            return true;
        }

        private void ExecuteBarricadeToss()
        {
            Vector3 origin = _throwOrigin != null ? _throwOrigin.position : transform.position;
            Vector3 direction = _playerCamera != null ? _playerCamera.transform.forward : transform.forward;

            // Create or instantiate barricade instance
            DeployableBarricade barricadeInstance = null;
            if (_barricadePrefab != null)
            {
                barricadeInstance = Instantiate(_barricadePrefab);
                barricadeInstance.gameObject.SetActive(false);
            }
            else
            {
                var go = new GameObject("Deployed_HardLightBarricade");
                barricadeInstance = go.AddComponent<DeployableBarricade>();
                go.SetActive(false);
            }

            // Launch puck
            if (_puckPrefab != null)
            {
                var puck = Instantiate(_puckPrefab, origin, Quaternion.identity);
                puck.Launch(origin, direction, barricadeInstance);
            }
            else
            {
                // Fallback puck
                var puckGo = new GameObject("Barricade_Puck");
                var puck = puckGo.AddComponent<BarricadePuck>();
                puck.Launch(origin, direction, barricadeInstance);
            }
        }

        public bool TryDeploySmoke()
        {
            if (_smokeCharges <= 0)
            {
                NullLog.Info("PlayerGadgets", "No Null-Cloud Smoke charges remaining.");
                return false;
            }

            _smokeCharges--;
            NullLog.Info("PlayerGadgets", $"Deploying Null-Cloud Smoke! Remaining charges: {_smokeCharges} (FR-18)");
            _gadgetChannel?.RaiseGadgetUsed(GadgetType.NullCloudSmoke, _smokeCharges);
            OnChargesChanged?.Invoke(GadgetType.NullCloudSmoke, _smokeCharges);

            ExecuteSmokeToss();
            return true;
        }

        private void ExecuteSmokeToss()
        {
            Vector3 origin = _throwOrigin != null ? _throwOrigin.position : transform.position;
            Vector3 direction = _playerCamera != null ? _playerCamera.transform.forward : transform.forward;

            NullCloudSmoke smokeInstance = null;
            if (_smokePrefab != null)
            {
                smokeInstance = Instantiate(_smokePrefab);
                smokeInstance.gameObject.SetActive(false);
            }
            else
            {
                var go = new GameObject("Deployed_NullCloudSmoke");
                smokeInstance = go.AddComponent<NullCloudSmoke>();
                go.SetActive(false);
            }

            if (_smokeCanisterPrefab != null)
            {
                var canister = Instantiate(_smokeCanisterPrefab, origin, Quaternion.identity);
                canister.Launch(origin, direction, smokeInstance);
            }
            else
            {
                var canisterGo = new GameObject("Smoke_Canister");
                var canister = canisterGo.AddComponent<SmokeCanister>();
                canister.Launch(origin, direction, smokeInstance);
            }
        }

        public bool TryDeployTripMine()
        {
            if (_tripMineCharges <= 0)
            {
                NullLog.Info("PlayerGadgets", "No Logic-Trip Mine charges remaining.");
                return false;
            }

            Vector3 origin = _throwOrigin != null ? _throwOrigin.position : transform.position;
            Vector3 direction = _playerCamera != null ? _playerCamera.transform.forward : transform.forward;

            // Check if player is aiming at a wall/doorframe surface within 3.5m
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 3.5f, Layers.Environment, QueryTriggerInteraction.Ignore))
            {
                _tripMineCharges--;
                NullLog.Info("PlayerGadgets", $"Deploying Logic-Trip Mine onto surface! Remaining charges: {_tripMineCharges} (FR-20)");
                _gadgetChannel?.RaiseGadgetUsed(GadgetType.LogicTripMine, _tripMineCharges);
                OnChargesChanged?.Invoke(GadgetType.LogicTripMine, _tripMineCharges);

                MountTripMine(hit.point, hit.normal);
                return true;
            }
            else
            {
                // Also support placement slightly ahead on nearest surface if within reach
                Vector3 placePoint = origin + direction * 1.5f;
                _tripMineCharges--;
                NullLog.Info("PlayerGadgets", $"Deploying Logic-Trip Mine in front! Remaining charges: {_tripMineCharges} (FR-20)");
                _gadgetChannel?.RaiseGadgetUsed(GadgetType.LogicTripMine, _tripMineCharges);
                OnChargesChanged?.Invoke(GadgetType.LogicTripMine, _tripMineCharges);

                MountTripMine(placePoint, -direction);
                return true;
            }
        }

        private void MountTripMine(Vector3 position, Vector3 normal)
        {
            LogicTripMine mineInstance = null;
            if (_tripMinePrefab != null)
            {
                mineInstance = Instantiate(_tripMinePrefab);
            }
            else
            {
                var go = new GameObject("Mounted_LogicTripMine");
                mineInstance = go.AddComponent<LogicTripMine>();
            }

            mineInstance.Mount(position, normal);
        }

        public void HandleGadgetRefunded(GadgetType type, int currentCharges)
        {
            switch (type)
            {
                case GadgetType.HardLightBarricade:
                    _barricadeCharges = currentCharges;
                    break;
                case GadgetType.NullCloudSmoke:
                    _smokeCharges = currentCharges;
                    break;
                case GadgetType.LogicTripMine:
                    _tripMineCharges = currentCharges;
                    break;
            }

            NullLog.Info("PlayerGadgets", $"Gadget refunded: {type}. Current count: {currentCharges}");
            OnChargesChanged?.Invoke(type, currentCharges);
        }

        public void ResetState()
        {
            _barricadeCharges = INITIAL_BARRICADE_CHARGES;
            _smokeCharges = INITIAL_SMOKE_CHARGES;
            _tripMineCharges = INITIAL_TRIP_MINE_CHARGES;

            OnChargesChanged?.Invoke(GadgetType.HardLightBarricade, _barricadeCharges);
            OnChargesChanged?.Invoke(GadgetType.NullCloudSmoke, _smokeCharges);
            OnChargesChanged?.Invoke(GadgetType.LogicTripMine, _tripMineCharges);
        }
    }
}
