using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// 你项目里的 Player.Player

namespace Game.Mechanism
{
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour
    {
        [Header("目标")]
        [Tooltip("目标场景名。留空=同场景传送")]
        public string targetSceneName;

        [Tooltip("目标场景中的 PortalAnchor.PortalId")]
        public string targetPortalId;

        [Header("行为")]
        [Tooltip("抵达后多长时间内不再触发传送（避免来回穿）")]
        public float arrivalImmunity = 0.25f;

        [Tooltip("是否保留玩家当前速度（跨场景时也会带过去）")]
        public bool preserveVelocity = true;

        private bool _busy;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_busy) return;

            // 玩家判定：根据你项目中的玩家脚本
            var player = other.GetComponentInParent<Player.Player>();
            if (player == null) return;

            var traveler = player.GetComponent<PortalTraveler>();
            if (traveler != null && traveler.IsImmune) return;

            StartCoroutine(TeleportRoutine(player));
        }

        private IEnumerator TeleportRoutine(Player.Player player)
        {
            _busy = true;

            var rb = player.GetComponent<Rigidbody2D>();
            Vector2? keepVel = (preserveVelocity && rb != null) ? rb.linearVelocity : (Vector2?)null;

            // 同场景
            if (string.IsNullOrWhiteSpace(targetSceneName)
                || targetSceneName == SceneManager.GetActiveScene().name)
            {
                
                PlacePlayerToPortalIdInCurrentScene(player, targetPortalId, keepVel, arrivalImmunity);
                
                _busy = false;
                yield break;
            }

            // 跨场景
            TeleportContext.Set(targetPortalId, keepVel, arrivalImmunity);

            

            // 确保玩家跨场景存活（保留身上的状态/物品/BUFF）
            var playerRoot = player.transform.root.gameObject;
            DontDestroyOnLoad(playerRoot);

            var op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            // 把玩家对象归属到新场景（可选，但更干净）
            SceneManager.MoveGameObjectToScene(playerRoot, SceneManager.GetActiveScene());

            // 交给 PortalSpawnResolver 落位；这里等一帧，确保 Awake/Start 执行
            yield return null;

            
            _busy = false;
        }

        private void PlacePlayerToPortalIdInCurrentScene(Player.Player player, string portalId,
            Vector2? keepVel, float immunitySecs)
        {
            var anchor = FindObjectsOfType<PortalAnchor>().FirstOrDefault(a => a.PortalId == portalId);
            if (anchor == null)
            {
                Debug.LogError($"[PortalGate] 未找到 PortalAnchor: {portalId}");
                return;
            }

            player.transform.position = anchor.transform.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (keepVel.HasValue) rb.linearVelocity = keepVel.Value;
                if (anchor.exitSpeed > 0f)
                    rb.linearVelocity = anchor.exitDirection.normalized * anchor.exitSpeed;
            }

            var traveler = player.GetComponent<PortalTraveler>();
            if (traveler == null) traveler = player.gameObject.AddComponent<PortalTraveler>();
            traveler.GiveArrivalImmunity(immunitySecs);
        }
    }
}
