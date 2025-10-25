using UnityEngine;

namespace Game.Mechanism
{
    public static class TeleportContext
    {
        public static string DestinationPortalId;
        public static Vector2? PendingVelocity;
        public static float ArrivalImmunitySeconds = 0.25f;

        public static bool HasPending => !string.IsNullOrEmpty(DestinationPortalId);

        public static void Set(string portalId, Vector2? keepVelocity = null, float immunity = 0.25f)
        {
            DestinationPortalId = portalId;
            PendingVelocity = keepVelocity;
            ArrivalImmunitySeconds = Mathf.Max(0f, immunity);
        }

        public static void Clear()
        {
            DestinationPortalId = null;
            PendingVelocity = null;
            ArrivalImmunitySeconds = 0.25f;
        }
    }
}