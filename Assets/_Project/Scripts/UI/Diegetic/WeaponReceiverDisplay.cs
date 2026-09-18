using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.UI
{
    /// <summary>
    /// FR-34: Weapon magazine ammunition count and firing mode rendered directly on weapon receiver mesh
    /// via glowing emissive digits.
    /// Uses MaterialPropertyBlock to prevent runtime material instancing and preserve SRP batching.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponReceiverDisplay : MonoBehaviour
    {
        [Header("Receiver Renderer & Materials")]
        [SerializeField] private Renderer _receiverRenderer;
        [SerializeField] private int _materialIndex = 0;

        [Header("Shader Property Names")]
        [SerializeField] private string _ammoCountPropName = "_AmmoCount";
        [SerializeField] private string _maxAmmoPropName = "_MaxAmmo";
        [SerializeField] private string _fireModePropName = "_FireMode"; // 0 = SemiAuto, 1 = Burst, 2 = Charge
        [SerializeField] private string _emissiveColorPropName = "_EmissionColor";
        [SerializeField] private string _isReloadingPropName = "_IsReloading";

        [Header("Color States")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _normalEmissiveColor = new Color(0f, 0.9f, 1f, 1f) * 2.5f; // Cyan HDR

        [ColorUsage(true, true)]
        [SerializeField] private Color _lowAmmoEmissiveColor = new Color(1f, 0.7f, 0f, 1f) * 3.0f; // Amber HDR

        [ColorUsage(true, true)]
        [SerializeField] private Color _criticalAmmoEmissiveColor = new Color(1f, 0.1f, 0.1f, 1f) * 3.5f; // Red HDR

        [ColorUsage(true, true)]
        [SerializeField] private Color _reloadingEmissiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Low Ammo Thresholds")]
        [SerializeField] private float _lowAmmoPercentage = 0.33f;
        [SerializeField] private int _criticalAmmoCount = 1;

        public void ApplyWireframeTheme(WireframeTheme theme)
        {
            var palette = WireframeThemePalette.GetPalette(theme);
            _normalEmissiveColor = palette.PrimaryColor * 2.5f;
            _lowAmmoEmissiveColor = palette.SecondaryColor * 3.0f;
            _criticalAmmoEmissiveColor = palette.CriticalColor * 3.5f;
            UpdateDisplay();
        }

        // Cached Property IDs
        private int _ammoPropId;
        private int _maxAmmoPropId;
        private int _fireModePropId;
        private int _emissionColorPropId;
        private int _isReloadingPropId;

        private MaterialPropertyBlock _propBlock;
        private int _currentAmmo = 12;
        private int _maxAmmo = 12;
        private int _fireMode = 0; // 0=Semi, 1=Burst, 2=Charge
        private bool _isReloading = false;

        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => _maxAmmo;
        public int FireMode => _fireMode;
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            if (_receiverRenderer == null)
            {
                _receiverRenderer = GetComponent<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            _ammoPropId = Shader.PropertyToID(_ammoCountPropName);
            _maxAmmoPropId = Shader.PropertyToID(_maxAmmoPropName);
            _fireModePropId = Shader.PropertyToID(_fireModePropName);
            _emissionColorPropId = Shader.PropertyToID(_emissiveColorPropName);
            _isReloadingPropId = Shader.PropertyToID(_isReloadingPropName);

            UpdateDisplay();
        }

        public void SetAmmo(int currentAmmo, int maxAmmo)
        {
            _currentAmmo = Mathf.Max(0, currentAmmo);
            _maxAmmo = Mathf.Max(1, maxAmmo);
            UpdateDisplay();
        }

        public void SetFireMode(int fireModeIndex)
        {
            _fireMode = fireModeIndex;
            UpdateDisplay();
        }

        public void SetReloading(bool reloading)
        {
            _isReloading = reloading;
            UpdateDisplay();
        }

        public Color GetCurrentEmissiveColor()
        {
            if (_isReloading) return _reloadingEmissiveColor;
            if (_currentAmmo <= _criticalAmmoCount) return _criticalAmmoEmissiveColor;
            if ((float)_currentAmmo / _maxAmmo <= _lowAmmoPercentage) return _lowAmmoEmissiveColor;
            return _normalEmissiveColor;
        }

        public void UpdateDisplay()
        {
            if (_receiverRenderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _receiverRenderer.GetPropertyBlock(_propBlock, _materialIndex);

            Color emissive = GetCurrentEmissiveColor();
            _propBlock.SetColor(_emissionColorPropId, emissive);
            _propBlock.SetFloat(_ammoPropId, _currentAmmo);
            _propBlock.SetFloat(_maxAmmoPropId, _maxAmmo);
            _propBlock.SetFloat(_fireModePropId, _fireMode);
            _propBlock.SetFloat(_isReloadingPropId, _isReloading ? 1f : 0f);

            _receiverRenderer.SetPropertyBlock(_propBlock, _materialIndex);
        }
    }
}
