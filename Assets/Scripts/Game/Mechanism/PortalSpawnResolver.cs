using System.Linq;
using UnityEngine;

namespace Game.Mechanism
{
    public class PortalSpawnResolver : MonoBehaviour
    {
        private void Awake()
        {
            if (!TeleportContext.HasPending) return;

            var player = FindObjectOfType<Player.Player>();
            if (player == null)
            {
                Debug.LogWarning("[PortalSpawnResolver] 未找到玩家，无法落位。");
                TeleportContext.Clear();
                return;
            }

            var anchor = FindObjectsOfType<PortalAnchor>()
                .FirstOrDefault(a => a.PortalId == TeleportContext.DestinationPortalId);

            if (anchor == null)
            {
                Debug.LogError($"[PortalSpawnResolver] 目标 PortalAnchor 不存在: {TeleportContext.DestinationPortalId}");
                TeleportContext.Clear();
                return;
            }

            // 传送落位
            player.transform.position = anchor.transform.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (TeleportContext.PendingVelocity.HasValue)
                    rb.linearVelocity = TeleportContext.PendingVelocity.Value;

                if (anchor.exitSpeed > 0f)
                    rb.linearVelocity = anchor.exitDirection.normalized * anchor.exitSpeed;
            }

            var traveler = player.GetComponent<PortalTraveler>();
            if (traveler == null) traveler = player.gameObject.AddComponent<PortalTraveler>();
            traveler.GiveArrivalImmunity(TeleportContext.ArrivalImmunitySeconds);

            TeleportContext.Clear();
        }
    }
}