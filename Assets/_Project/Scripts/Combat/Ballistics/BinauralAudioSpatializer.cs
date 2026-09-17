using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    /// <summary>
    /// FR-39: Implements 3D HRTF binaural spatialization for footsteps, weapon discharges,
    /// ricochets, and synthetic vocalizations.
    /// Manages 3D audio configuration (spatialBlend = 1.0, logarithmic/custom rolloff, Doppler dampening).
    /// </summary>
    public static class BinauralAudioSpatializer
    {
        /// <summary>
        /// Configures an AudioSource to adhere to FR-39 3D HRTF binaural spatialization standards.
        /// </summary>
        public static void ConfigureBinauralSource(
            AudioSource source,
            float minDistance = 1.5f,
            float maxDistance = 35.0f,
            AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
            bool bypassEffects = false)
        {
            if (source == null) return;

            source.spatialBlend = 1.0f; // 100% 3D spatialized
            source.spatialize = true;   // Activates native/plugin HRTF spatializer engine
            source.spatializePostEffects = false;
            source.rolloffMode = rolloffMode;
            source.minDistance = Mathf.Max(0.1f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 1f, maxDistance);
            source.dopplerLevel = 0.2f; // Dampened Doppler for tactical readability
            source.spread = 0f;         // Point source precision for accurate binaural localization
            source.bypassEffects = bypassEffects;
            source.playOnAwake = false;
        }

        /// <summary>
        /// Plays a one-shot 3D binaural sound at world position.
        /// </summary>
        public static AudioSource PlayClipAtPointBinaural(
            AudioClip clip,
            Vector3 position,
            float volume = 1.0f,
            float pitch = 1.0f,
            float minDistance = 2.0f,
            float maxDistance = 30.0f)
        {
            if (clip == null) return null;

            GameObject tempGO = new GameObject($"BinauralAudio_{clip.name}");
            tempGO.transform.position = position;

            AudioSource source = tempGO.AddComponent<AudioSource>();
            ConfigureBinauralSource(source, minDistance, maxDistance);
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;

            source.Play();
            Object.Destroy(tempGO, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f);
            return source;
        }
    }
}
