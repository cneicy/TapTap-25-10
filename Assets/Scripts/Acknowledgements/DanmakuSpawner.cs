using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Acknowledgements
{
    public class DanmakuSpawner : MonoBehaviour
    {
        [Header("必填")]
        public RectTransform viewport;     // 有 RectMask2D 的窗口
        public DanmakuItem itemPrefab;     // 弹幕条预制体（RectTransform 左中对齐）

        [Header("发送与速度")]
        [Tooltip("固定间隔（秒）")]
        public float intervalSeconds = 0.8f;
        [Tooltip("在 [min,max] 内随机间隔（覆盖固定间隔），若 min>=0 则启用")]
        public Vector2 randomInterval = new Vector2(-1f, -1f);
        [Tooltip("弹幕水平速度，像素/秒（受 CanvasScaler）")]
        public float speedPxPerSec = 220f;

        [Header("生成区域（在 Viewport 内随机Y）")]
        [Tooltip("距离 Viewport 上/下边的留白像素")]
        public float verticalPadding = 8f;

        [Header("启动与循环")]
        public bool autoStart = true;
        public bool loopInitialMessages = true;

        [Header("初始循环的弹幕内容（可留空）")]
        [TextArea(3, 10)]
        public List<string> initialMessages = new List<string>()
        {
            
        };

        // —— 简单对象池 ——
        private readonly Stack<DanmakuItem> _pool = new Stack<DanmakuItem>();
        private readonly Queue<string> _queue     = new Queue<string>();
        private Coroutine _co;

        void Start()
        {
            if (autoStart) StartSending();
        }

        public void StartSending()
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(SendLoop());
        }

        public void StopSending()
        {
            if (_co != null) StopCoroutine(_co);
            _co = null;
        }

        /// <summary>外部随时添加一条弹幕文本</summary>
        public void Send(string msg) => _queue.Enqueue(msg);

        /// <summary>外部批量添加</summary>
        public void SendMany(IEnumerable<string> msgs)
        {
            foreach (var m in msgs) _queue.Enqueue(m);
        }

        IEnumerator SendLoop()
        {
            // 先把初始内容入队（循环模式下会重复）
            if (initialMessages != null && initialMessages.Count > 0)
            {
                foreach (var m in initialMessages) _queue.Enqueue(m);
            }

            while (true)
            {
                // 取一条消息（若队列空且允许循环，继续循环初始列表）
                if (_queue.Count == 0 && loopInitialMessages && initialMessages.Count > 0)
                {
                    foreach (var m in initialMessages) _queue.Enqueue(m);
                }

                if (_queue.Count > 0)
                {
                    string msg = _queue.Dequeue();
                    SpawnOne(msg);
                }

                float wait = intervalSeconds;
                if (randomInterval.x >= 0f && randomInterval.y > randomInterval.x)
                    wait = Random.Range(randomInterval.x, randomInterval.y);

                yield return new WaitForSecondsRealtime(wait);
            }
        }

        void SpawnOne(string msg)
        {
            var item = Get();
            // 先设置文本，拿到真实尺寸
            item.SetupText(msg);

            float vh = viewport.rect.height;
            float yMin = verticalPadding;
            float yMax = Mathf.Max(yMin, vh - verticalPadding - item.Height);
            float randY = Random.Range(yMin, yMax);

            // 从右边界贴边生成：起点 x = Viewport 宽度
            var startPos = new Vector2(viewport.rect.width, randY);

            item.Launch(viewport, startPos, speedPxPerSec, ReturnToPool);
        }

        DanmakuItem Get()
        {
            DanmakuItem item = _pool.Count > 0 ? _pool.Pop() : Instantiate(itemPrefab);
            item.gameObject.SetActive(true);
            return item;
        }

        void ReturnToPool(DanmakuItem item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(transform, worldPositionStays: false); // 收回到管理器下
            _pool.Push(item);
        }
    }
}
