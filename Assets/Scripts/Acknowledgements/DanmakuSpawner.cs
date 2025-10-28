using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Acknowledgements
{
    public class DanmakuSpawner : MonoBehaviour
    {
        [Header("必填")]
        public RectTransform viewport;   // 有 RectMask2D 的窗口
        public DanmakuItem  itemPrefab;  // 弹幕条预制体（根Rect左中对齐，子：背景+TMP）

        [Header("发送与速度")]
        [Tooltip("固定间隔（秒）")]
        public float intervalSeconds = 0.8f;
        [Tooltip("[min,max] 随机间隔（若 min>=0 启用）")]
        public Vector2 randomInterval = new Vector2(-1f, -1f);

        [Tooltip("弹幕速度数值；单位由 speedUnit 决定")]
        public float speedValue = 220f;

        public DanmakuItem.SpeedUnit speedUnit = DanmakuItem.SpeedUnit.LocalUnitsPerSec;

        [Tooltip("当 speedUnit=ScreenPixelsPerSec 时用于换算的相机（可留空=Canvas.worldCamera/Main）")]
        public Camera screenCamera;

        [Header("生成区域（Viewport 内随机Y）")]
        [Tooltip("距离 Viewport 上/下边的留白（本地单位）")]
        public float verticalPadding = 8f;

        [Header("启动与循环")]
        public bool autoStart = true;
        public bool loopInitialMessages = true;

        [Header("初始循环的弹幕内容（可留空）")]
        [TextArea(3, 10)]
        public List<string> initialMessages = new();

        // —— 对象池与队列 ——
        private readonly Stack<DanmakuItem> _pool  = new Stack<DanmakuItem>();
        private readonly Queue<string>      _queue = new Queue<string>();
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

        public void Send(string msg) => _queue.Enqueue(msg);
        public void SendMany(IEnumerable<string> msgs) { foreach (var m in msgs) _queue.Enqueue(m); }

        IEnumerator SendLoop()
        {
            if (initialMessages != null && initialMessages.Count > 0)
                foreach (var m in initialMessages) _queue.Enqueue(m);

            while (true)
            {
                if (_queue.Count == 0 && loopInitialMessages && initialMessages.Count > 0)
                    foreach (var m in initialMessages) _queue.Enqueue(m);

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
            item.SetupText(msg); // 如需限制最大宽度，可传 viewport.rect.width * 0.9f

            // —— 垂直随机：以视口中心为0，任何 pivot/anchor 都安全 —— 
            float vh = viewport.rect.height;
            float halfSpan = Mathf.Max(0f, (vh - item.Height) * 0.5f - verticalPadding);
            float randY = (halfSpan > 0f) ? Random.Range(-halfSpan, halfSpan) : 0f;

            // 从右边界外贴边生成（x = 视口宽度）
            var startPos = new Vector2(viewport.rect.width, randY);

            item.Launch(viewport, startPos, speedValue, ReturnToPool, speedUnit, screenCamera);
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
            item.transform.SetParent(transform, worldPositionStays: false);
            _pool.Push(item);
        }
    }
}
