using UnityEngine;

namespace Game.Mechanism
{
    public class PortalAnchor : MonoBehaviour
    {
        [Tooltip("场景内唯一ID")]
        public string PortalId;

        [Tooltip("抵达后给的方向（可选）")]
        public Vector2 exitDirection = Vector2.right;

        [Tooltip("抵达后给的速度；0表示不改动")]
        public float exitSpeed = 0f;
        
    }
}