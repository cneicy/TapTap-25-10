using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Mechanism
{
    public class TeleportResolver : MonoBehaviour
    {

        private void Start()
        {
            if (!TeleportTicket.HasPending) return;
            if (!string.IsNullOrEmpty(TeleportTicket.TargetScene) &&
                TeleportTicket.TargetScene != SceneManager.GetActiveScene().name) return;

            StartCoroutine(ResolveWhenPlayerReady());
        }

        private IEnumerator ResolveWhenPlayerReady()
        {
            // 等“你的新玩家预制体生成”完成
            Player.Player player = null;
            for (var i = 0; i < 300; i++) // ~5s @60fps
            {
                player = Object.FindObjectOfType<Player.Player>();
                if (player) break;
                yield return null;
            }
            if (!player)
            {
                Debug.LogError("[TeleportResolver] 未在新场景找到玩家实例。");
                TeleportTicket.Clear();
                yield break;
            }

            var anchor = Object.FindObjectsOfType<PortalAnchor>()
                .FirstOrDefault(a => a.PortalId == TeleportTicket.DestinationPortalId);

            if (!anchor)
            {
                Debug.LogError($"[TeleportResolver] 目标落点不存在: {TeleportTicket.DestinationPortalId}");
                TeleportTicket.Clear();
                yield break;
            }

            // 放置到落点 & 速度/免疫
            player.transform.position = anchor.transform.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                if (TeleportTicket.CarryVelocity.HasValue)
                    rb.linearVelocity = TeleportTicket.CarryVelocity.Value;
                if (anchor.exitSpeed > 0f)
                    rb.linearVelocity = anchor.exitDirection.normalized * anchor.exitSpeed;
            }

            var traveler = player.GetComponent<PortalTraveler>() ?? player.gameObject.AddComponent<PortalTraveler>();
            traveler.GiveArrivalImmunity(TeleportTicket.ArrivalImmunitySeconds);
            
        }
    }
}
