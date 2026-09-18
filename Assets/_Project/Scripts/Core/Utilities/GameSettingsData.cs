using System;
using UnityEngine;

namespace NullProtocol.Core
{
    /// <summary>
    /// FR-46: Visual accessibility wireframe themes.
    /// </summary>
    public enum WireframeTheme
    {
        DefaultCyanOrange = 0,
        HighContrastYellowBlue = 1,
        ProtanopiaDeuteranopiaMode = 2
    }

    /// <summary>
    /// Color palette definition for a wireframe theme.
    /// </summary>
    [Serializable]
    public struct WireframeThemePalette
    {
        public Color PrimaryColor;   // Environment outlines / Friendly / Healthy
        public Color SecondaryColor; // Threat / Highlight / Ammo / Reticle
        public Color CriticalColor;  // Hazard / Low Ammo / Danger

        public WireframeThemePalette(Color primary, Color secondary, Color critical)
        {
            PrimaryColor = primary;
            SecondaryColor = secondary;
            CriticalColor = critical;
        }

        public static WireframeThemePalette GetPalette(WireframeTheme theme)
        {
            switch (theme)
            {
                case WireframeTheme.HighContrastYellowBlue:
                    // Vibrant yellow outlines with deep cobalt blue accents & magenta criticals
                    return new WireframeThemePalette(
                        new Color(1f, 0.92f, 0.016f, 1f),  // Bright Yellow
                        new Color(0.12f, 0.53f, 1f, 1f),   // Vivid Blue
                        new Color(1f, 0.08f, 0.58f, 1f)    // Bright Magenta
                    );

                case WireframeTheme.ProtanopiaDeuteranopiaMode:
                    // Specially tuned for red-green color blindness: Blue primary with Gold/Amber secondary and White highlight
                    return new WireframeThemePalette(
                        new Color(0.15f, 0.65f, 1f, 1f),   // Blue
                        new Color(0.95f, 0.75f, 0.1f, 1f),  // Gold/Amber
                        new Color(0.95f, 0.95f, 0.95f, 1f)  // High-lum White
                    );

                case WireframeTheme.DefaultCyanOrange:
                default:
                    // Signature Null-Protocol styling: Cyan primary with Orange secondary and Red critical
                    return new WireframeThemePalette(
                        new Color(0f, 0.9f, 1f, 1f),       // Cyan
                        new Color(1f, 0.5f, 0f, 1f),       // Orange
                        new Color(1f, 0.1f, 0.1f, 1f)      // Red
                    );
            }
        }
    }

    /// <summary>
    /// Settings data model serialized into ProfileData (FR-44, FR-45, FR-46).
    /// </summary>
    [Serializable]
    public class GameSettingsData
    {
        // FR-45: Controls & Mouse Settings
        public bool RawMouseInput = true;
        public float MouseSensitivity = 1.0f; // Range: 0.1 to 10.0
        public bool MouseAcceleration = false; // Default OFF
        public bool InvertY = false;

        // FR-46: Camera & Visuals
        public float FieldOfView = 90.0f; // Range: 80° to 110°, default 90°
        public WireframeTheme Theme = WireframeTheme.DefaultCyanOrange;

        // FR-46: Audio Volume Sliders (0.0 to 1.0)
        public float MasterVolume = 1.0f;
        public float SfxVolume = 1.0f;
        public float RadioVoiceVolume = 1.0f;
        public float UiVolume = 1.0f;

        // FR-44: Serialized Input Overrides (JSON string from InputBinding overrides)
        public string InputOverridesJson = string.Empty;

        public GameSettingsData()
        {
            RawMouseInput = true;
            MouseSensitivity = 1.0f;
            MouseAcceleration = false;
            InvertY = false;
            FieldOfView = 90.0f;
            Theme = WireframeTheme.DefaultCyanOrange;
            MasterVolume = 1.0f;
            SfxVolume = 1.0f;
            RadioVoiceVolume = 1.0f;
            UiVolume = 1.0f;
            InputOverridesJson = string.Empty;
        }

        public void ValidateAndClamp()
        {
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.1f, 10.0f);
            FieldOfView = Mathf.Clamp(FieldOfView, 80.0f, 110.0f);
            MasterVolume = Mathf.Clamp01(MasterVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            RadioVoiceVolume = Mathf.Clamp01(RadioVoiceVolume);
            UiVolume = Mathf.Clamp01(UiVolume);
        }
    }
}
