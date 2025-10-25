using System.Collections;
using System.Linq;
using Game.Level;
using ScreenEffect;
using UnityEngine;
using UnityEngine.SceneManagement;

// 你的 Player 类所在命名空间

namespace Game.Mechanism
{
    [RequireComponent(typeof(Collider2D))]
    public class PortalGate : MonoBehaviour
    {
        [Header("目标场景/目标Anchor")]
        public string targetSceneName;   // 留空=同场景
        public string targetPortalId;

        [Header("行为")]
        public bool preserveVelocity = true;
        public float arrivalImmunity = 0.25f;

        private bool _busy;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_busy) return;
            var player = other.GetComponentInParent<Player.Player>();
            if (player == null) return;
            StartCoroutine(TeleportRoutine(player));
        }

        private IEnumerator TeleportRoutine(Player.Player player)
        {
            _busy = true;

            var rb = player.GetComponent<Rigidbody2D>();
            Vector2? carryVel = (preserveVelocity && rb) ? rb.linearVelocity : (Vector2?)null;

            // 同场景：直接移动
            if (string.IsNullOrWhiteSpace(targetSceneName) ||
                targetSceneName == SceneManager.GetActiveScene().name)
            {
                //传送动画开始
                PlaceInCurrentScene(player, targetPortalId, carryVel, arrivalImmunity);
                //传送动画结束
                _busy = false;
                yield break;
            }

            // 跨场景：写小票 → 切场景
            TeleportTicket.Set(targetSceneName, targetPortalId, carryVel, arrivalImmunity);

            //切场景传送动画
            RectTransitionController.Instance.StartTransition();
            yield return new WaitForSeconds(0.25f);
            var op = LevelManager.Instance.SwitchLevel(targetSceneName);
            while (!op.IsCompleted) yield return null;

            // 黑幕在新场景的解析器里淡出
            _busy = false;
        }

        private void PlaceInCurrentScene(Player.Player player, string portalId, Vector2? carryVel, float immunity)
        {
            var anchor = FindObjectsByType<PortalAnchor>(FindObjectsSortMode.None).FirstOrDefault(a => a.PortalId == portalId);
            if (!anchor) { Debug.LogError($"未找到 PortalAnchor: {portalId}"); return; }

            player.transform.position = anchor.transform.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                if (carryVel.HasValue) rb.linearVelocity = carryVel.Value;
                if (anchor.exitSpeed > 0f) rb.linearVelocity = anchor.exitDirection.normalized * anchor.exitSpeed;
            }

            var traveler = player.GetComponent<PortalTraveler>() ?? player.gameObject.AddComponent<PortalTraveler>();
            traveler.GiveArrivalImmunity(immunity);
        }
    }
}
