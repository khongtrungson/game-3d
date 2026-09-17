using UnityEngine;

namespace NullProtocol.Combat
{
    public class ProceduralRecoilSpring : MonoBehaviour
    {
        [Header("Spring Physics Parameters")]
        [Tooltip("Spring stiffness / snap-back speed")]
        [SerializeField] private float _snappiness = 20.0f;

        [Tooltip("Return speed to center rest position")]
        [SerializeField] private float _returnSpeed = 12.0f;

        // Current rotational displacement and velocity
        private Vector3 _currentRotation;
        private Vector3 _targetRotation;

        public Vector3 CurrentRotation => _currentRotation;
        public Vector3 TargetRotation => _targetRotation;

        public void ApplyRecoilKick(float pitchKick, float yawSpread, float rollShockwave = 0f)
        {
            float randomYaw = Random.Range(-yawSpread, yawSpread);
            _targetRotation += new Vector3(-pitchKick, randomYaw, rollShockwave);
        }

        public void TickSpring(float deltaTime)
        {
            // Damp target back to zero
            _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, _returnSpeed * deltaTime);

            // Slerp / Lerp current rotation towards target
            _currentRotation = Vector3.Lerp(_currentRotation, _targetRotation, _snappiness * deltaTime);
        }

        public void ResetSpring()
        {
            _currentRotation = Vector3.zero;
            _targetRotation = Vector3.zero;
        }
    }
}
