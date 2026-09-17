using System.Collections;
using UnityEngine;

namespace NullProtocol.Combat
{
    public class WeaponVFXController : MonoBehaviour
    {
        [Header("Muzzle Flash & Sparks")]
        [SerializeField] private ParticleSystem _muzzleFlashParticles;
        [SerializeField] private Light _muzzleLight;
        [SerializeField] private float _muzzleLightDuration = 0.05f;
        [SerializeField] private ParticleSystem _impactSparksPrefab;

        [Header("Procedural Line Tracer")]
        [SerializeField] private Material _tracerMaterial;
        [SerializeField] private float _tracerDuration = 0.04f;
        [SerializeField] private float _tracerWidth = 0.03f;
        [SerializeField] private Color _tracerColor = new Color(0.2f, 0.85f, 1f, 1f);

        [Header("De-Rez Ripple / Decal")]
        [SerializeField] private GameObject _deRezRipplePrefab;

        public void PlayFireEffects(Vector3 muzzlePosition, Vector3 targetPosition)
        {
            // Procedural muzzle flash
            if (_muzzleFlashParticles != null)
            {
                _muzzleFlashParticles.transform.position = muzzlePosition;
                _muzzleFlashParticles.Play();
            }

            if (_muzzleLight != null)
            {
                StartCoroutine(MuzzleLightFlash());
            }

            // Procedural tracer line
            SpawnTracer(muzzlePosition, targetPosition);
        }

        public void PlayImpactEffects(Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot, bool disintegrated)
        {
            // Physical impact sparks
            if (_impactSparksPrefab != null)
            {
                var sparks = Instantiate(_impactSparksPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                Destroy(sparks.gameObject, 1.5f);
            }

            // Surface UV de-rez ripples
            if (_deRezRipplePrefab != null)
            {
                var ripple = Instantiate(_deRezRipplePrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                Destroy(ripple, 2.0f);
            }
        }

        public void SpawnTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("Tracer_Line");
            tracerObj.transform.position = start;
            var line = tracerObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = _tracerWidth;
            line.endWidth = _tracerWidth * 0.5f;

            if (_tracerMaterial != null)
            {
                line.material = _tracerMaterial;
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    line.material = new Material(shader) { color = _tracerColor };
                }
            }

            StartCoroutine(FadeAndDestroyTracer(tracerObj, _tracerDuration));
        }

        private IEnumerator MuzzleLightFlash()
        {
            _muzzleLight.enabled = true;
            yield return new WaitForSeconds(_muzzleLightDuration);
            if (_muzzleLight != null)
            {
                _muzzleLight.enabled = false;
            }
        }

        private IEnumerator FadeAndDestroyTracer(GameObject tracer, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (tracer != null)
            {
                Destroy(tracer);
            }
        }
    }
}
