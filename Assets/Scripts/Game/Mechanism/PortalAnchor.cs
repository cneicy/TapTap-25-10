using System;
using UnityEngine;

namespace Game.Mechanism
{
    public class PortalAnchor : MonoBehaviour
    {
        [Tooltip("本落点的唯一ID（跨场景匹配用）。不填会自动生成。")]
        public string PortalId;

        [Tooltip("抵达后可给一个初速度方向（可选）。")]
        public Vector2 exitDirection = Vector2.right;

        [Tooltip("抵达时给的速度大小（可选）。")]
        public float exitSpeed = 0f;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(PortalId))
                PortalId = Guid.NewGuid().ToString("N");
        }
    }
}