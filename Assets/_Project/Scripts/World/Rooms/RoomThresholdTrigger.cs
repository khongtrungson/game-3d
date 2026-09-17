using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;

namespace NullProtocol.World
{
    /// <summary>
    /// Trigger placed at room entry thresholds that activates room tracking and baseline recording.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class RoomThresholdTrigger : MonoBehaviour
    {
        [SerializeField] private RoomController _room;

        public RoomController Room => _room;

        private void Awake()
        {
            if (_room == null)
            {
                _room = GetComponentInParent<RoomController>();
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tags.Player) || other.GetComponent<TacticalLocomotionController>() != null)
            {
                if (_room != null)
                {
                    _room.HandlePlayerEnter();

                    if (MemoryDumpManager.Instance != null)
                    {
                        MemoryDumpManager.Instance.SetActiveRoom(_room);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(Tags.Player) || other.GetComponent<TacticalLocomotionController>() != null)
            {
                if (_room != null)
                {
                    _room.HandlePlayerExit();
                }
            }
        }
    }
}
