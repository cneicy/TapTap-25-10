using UnityEngine;

namespace Game.Mechanism
{
    public static class TeleportTicket
    {
        public static string TargetScene;             // 目的场景（可校验用）
        public static string DestinationPortalId;     // 要落到哪个 PortalAnchor
        public static Vector2? CarryVelocity;         // 可选：把进门时速度带过去
        public static float ArrivalImmunitySeconds = 0.25f;

        public static bool HasPending => !string.IsNullOrEmpty(DestinationPortalId);

        public static void Set(string targetScene, string portalId, Vector2? carryVel,
            float immunitySeconds = 0.25f)
        {
            TargetScene = targetScene;
            DestinationPortalId = portalId;
            CarryVelocity = carryVel;
            ArrivalImmunitySeconds = Mathf.Max(0f, immunitySeconds);
        }

        public static void Clear()
        {
            TargetScene = null;
            DestinationPortalId = null;
            CarryVelocity = null;
            ArrivalImmunitySeconds = 0.25f;
        }
    }
}