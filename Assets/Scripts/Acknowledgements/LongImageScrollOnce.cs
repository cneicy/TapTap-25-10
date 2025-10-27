using System.Collections;
using UnityEngine;

namespace Acknowledgements
{
    public class LongImageScrollHorizontalOnce : MonoBehaviour
    {
        public RectTransform viewport;
        public RectTransform longImage;
        [Header("速度")]
        public float speedPxPerSec = 100f;
        public bool leftToRight = true;
        public bool loop = false;
        public bool autoStart = true;

        Coroutine co;

        void Start()
        {
            if (autoStart) StartScroll();
        }

        public void StartScroll()
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(ScrollRoutine());
        }

        IEnumerator ScrollRoutine()
        {
            float maxOffset = Mathf.Max(0f, longImage.rect.width - viewport.rect.width);
            var pos = longImage.anchoredPosition;
            pos.y = 0f;
            pos.x = leftToRight ? 0f : -maxOffset;
            longImage.anchoredPosition = pos;

            if (maxOffset <= 0f) yield break;

            float dir = leftToRight ? -1f : 1f; // 左→右视觉上图片向左移动（负方向）
            while (true)
            {
                float step = speedPxPerSec * Time.unscaledDeltaTime * dir;
                pos.x = Mathf.Clamp(pos.x + step, -maxOffset, 0f);
                longImage.anchoredPosition = pos;

                bool done = leftToRight ? (pos.x <= -maxOffset) : (pos.x >= 0f);
                if (done) break;
                yield return null;
            }

            if (loop) StartScroll();
        }
    }
}