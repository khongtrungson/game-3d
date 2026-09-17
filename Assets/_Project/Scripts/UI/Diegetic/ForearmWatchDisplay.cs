using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Gadgets;

namespace NullProtocol.UI
{
    /// <summary>
    /// FR-35: In-world OLED wrist display on player's left forearm.
    /// Renders player health points and available gadget charges (Barricades, Smoke, Mines).
    /// Uses MaterialPropertyBlock to update OLED mesh and emissive indicators efficiently.
    /// </summary>
    [DisallowMultipleComponent]
    public class ForearmWatchDisplay : MonoBehaviour
    {
        [Header("Watch OLED Screen Renderer")]
        [SerializeField] private Renderer _oledScreenRenderer;
        [SerializeField] private int _materialIndex = 0;

        [Header("Shader Property Names")]
        [SerializeField] private string _healthPropName = "_Health";
        [SerializeField] private string _maxHealthPropName = "_MaxHealth";
        [SerializeField] private string _healthNormalizedPropName = "_HealthNormalized";
        [SerializeField] private string _barricadeChargesPropName = "_BarricadeCharges";
        [SerializeField] private string _smokeChargesPropName = "_SmokeCharges";
        [SerializeField] private string _tripMineChargesPropName = "_TripMineCharges";
        [SerializeField] private string _screenEmissiveColorPropName = "_EmissionColor";

        [Header("OLED Color Themes")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _healthyColor = new Color(0f, 0.9f, 1f, 1f) * 2.0f; // Cyan OLED HDR

        [ColorUsage(true, true)]
        [SerializeField] private Color _warningColor = new Color(1f, 0.6f, 0f, 1f) * 2.5f; // Amber OLED HDR

        [ColorUsage(true, true)]
        [SerializeField] private Color _criticalColor = new Color(1f, 0.05f, 0.05f, 1f) * 3.0f; // Red OLED HDR

        [Header("Thresholds")]
        [SerializeField] private float _warningThreshold = 0.5f;
        [SerializeField] private float _criticalThreshold = 0.25f;

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;
        [SerializeField] private GadgetEventChannelSO _gadgetEvents;

        [Header("Optional Direct Controller Reference")]
        [SerializeField] private PlayerGadgetController _gadgetController;

        // Cached Property IDs
        private int _healthPropId;
        private int _maxHealthPropId;
        private int _healthNormalizedPropId;
        private int _barricadeChargesPropId;
        private int _smokeChargesPropId;
        private int _tripMineChargesPropId;
        private int _screenEmissiveColorPropId;

        private MaterialPropertyBlock _propBlock;
        private int _currentHealth = 100;
        private int _maxHealth = 100;
        private int _barricadeCharges = 2;
        private int _smokeCharges = 2;
        private int _tripMineCharges = 1;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int BarricadeCharges => _barricadeCharges;
        public int SmokeCharges => _smokeCharges;
        public int TripMineCharges => _tripMineCharges;

        private void Awake()
        {
            if (_oledScreenRenderer == null)
            {
                _oledScreenRenderer = GetComponent<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            _healthPropId = Shader.PropertyToID(_healthPropName);
            _maxHealthPropId = Shader.PropertyToID(_maxHealthPropName);
            _healthNormalizedPropId = Shader.PropertyToID(_healthNormalizedPropName);
            _barricadeChargesPropId = Shader.PropertyToID(_barricadeChargesPropName);
            _smokeChargesPropId = Shader.PropertyToID(_smokeChargesPropName);
            _tripMineChargesPropId = Shader.PropertyToID(_tripMineChargesPropName);
            _screenEmissiveColorPropId = Shader.PropertyToID(_screenEmissiveColorPropName);

            UpdateWatchDisplay();
        }

        private void OnEnable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged += HandleHealthChanged;
            }

            if (_gadgetEvents != null)
            {
                _gadgetEvents.OnGadgetUsed += HandleGadgetChanged;
                _gadgetEvents.OnGadgetRefunded += HandleGadgetChanged;
            }

            if (_gadgetController != null)
            {
                _gadgetController.OnChargesChanged += HandleGadgetChanged;
                _barricadeCharges = _gadgetController.BarricadeCharges;
                _smokeCharges = _gadgetController.SmokeCharges;
                _tripMineCharges = _gadgetController.TripMineCharges;
                UpdateWatchDisplay();
            }
        }

        private void OnDisable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged -= HandleHealthChanged;
            }

            if (_gadgetEvents != null)
            {
                _gadgetEvents.OnGadgetUsed -= HandleGadgetChanged;
                _gadgetEvents.OnGadgetRefunded -= HandleGadgetChanged;
            }

            if (_gadgetController != null)
            {
                _gadgetController.OnChargesChanged -= HandleGadgetChanged;
            }
        }

        public void SetHealth(int currentHealth, int maxHealth)
        {
            _currentHealth = Mathf.Max(0, currentHealth);
            _maxHealth = Mathf.Max(1, maxHealth);
            UpdateWatchDisplay();
        }

        public void SetGadgetCharges(int barricades, int smokes, int tripMines)
        {
            _barricadeCharges = Mathf.Max(0, barricades);
            _smokeCharges = Mathf.Max(0, smokes);
            _tripMineCharges = Mathf.Max(0, tripMines);
            UpdateWatchDisplay();
        }

        public void SetGadgetCharge(GadgetType type, int charges)
        {
            switch (type)
            {
                case GadgetType.HardLightBarricade:
                    _barricadeCharges = Mathf.Max(0, charges);
                    break;
                case GadgetType.NullCloudSmoke:
                    _smokeCharges = Mathf.Max(0, charges);
                    break;
                case GadgetType.LogicTripMine:
                    _tripMineCharges = Mathf.Max(0, charges);
                    break;
            }
            UpdateWatchDisplay();
        }

        private void HandleHealthChanged(int current, int max)
        {
            SetHealth(current, max);
        }

        private void HandleGadgetChanged(GadgetType type, int charges)
        {
            SetGadgetCharge(type, charges);
        }

        public Color GetCurrentHealthColor()
        {
            float norm = (_maxHealth > 0) ? (float)_currentHealth / _maxHealth : 0f;
            if (norm <= _criticalThreshold) return _criticalColor;
            if (norm <= _warningThreshold) return _warningColor;
            return _healthyColor;
        }

        public void UpdateWatchDisplay()
        {
            if (_oledScreenRenderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _oledScreenRenderer.GetPropertyBlock(_propBlock, _materialIndex);

            float healthNorm = (_maxHealth > 0) ? (float)_currentHealth / _maxHealth : 0f;
            Color emissiveColor = GetCurrentHealthColor();

            _propBlock.SetFloat(_healthPropId, _currentHealth);
            _propBlock.SetFloat(_maxHealthPropId, _maxHealth);
            _propBlock.SetFloat(_healthNormalizedPropId, healthNorm);
            _propBlock.SetFloat(_barricadeChargesPropId, _barricadeCharges);
            _propBlock.SetFloat(_smokeChargesPropId, _smokeCharges);
            _propBlock.SetFloat(_tripMineChargesPropId, _tripMineCharges);
            _propBlock.SetColor(_screenEmissiveColorPropId, emissiveColor);

            _oledScreenRenderer.SetPropertyBlock(_propBlock, _materialIndex);
        }
    }
}
