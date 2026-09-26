using System.Collections;
using UnityEngine;

namespace NullProtocol.VFX
{
    /// <summary>
    /// Controls individual physics voxel debris shard: applies Voronoi dissolve cutoff over lifetime.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelDissolvePhysics : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propBlock;
        private static readonly int DissolveAmountProp = Shader.PropertyToID("_DissolveAmount");

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propBlock = new MaterialPropertyBlock();
        }

        public void BeginDissolve(float duration)
        {
            StartCoroutine(RoutineDissolve(duration));
        }

        private IEnumerator RoutineDissolve(float duration)
        {
            float elapsed = 0.0f;
            // Add subtle random per-voxel delay jitter for natural disintegration
            float jitterDelay = Random.Range(0.0f, 0.25f);
            yield return new WaitForSeconds(jitterDelay);

            float effectiveDuration = Mathf.Max(0.5f, duration - jitterDelay);

            while (elapsed < effectiveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / effectiveDuration);
                // Voronoi alpha cutoff progression
                float dissolve = Mathf.SmoothStep(0.0f, 1.0f, t);

                if (meshRenderer != null)
                {
                    meshRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetFloat(DissolveAmountProp, dissolve);
                    meshRenderer.SetPropertyBlock(propBlock);
                }

                yield return null;
            }

            // Once fully dissolved, disable physics and mesh
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            if (meshRenderer != null) meshRenderer.enabled = false;
        }
    }
}
