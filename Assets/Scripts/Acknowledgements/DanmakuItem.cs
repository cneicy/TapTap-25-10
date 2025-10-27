using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acknowledgements
{
    public class DanmakuItem : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI textLabel; // 子物体：TMP 文本
        [SerializeField] private Image background;          // 根物体的 Image（建议 Type=Sliced）

        private RectTransform _rt;
        private RectTransform _parentViewport;
        private float _speedPxPerSec;
        private Action<DanmakuItem> _onDespawn;
        private bool _moving;

        [Header("尺寸/样式")]
        [Tooltip("左右/上下的总内边距（像素）。根物体尺寸=文字首选尺寸+padding")]
        public Vector2 padding = new Vector2(40f, 12f);
        public float  minHeight = 32f;
        [Tooltip("是否允许换行（仅在传入了 maxWidth 时会生效）")]
        public bool   wordWrap  = true;

        public float Width  => _rt.rect.width;
        public float Height => _rt.rect.height;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();

            // 根物体左中对齐，便于从右边生成（x 从右边界开始递减）
            _rt.anchorMin = new Vector2(0f, 0.5f);
            _rt.anchorMax = new Vector2(0f, 0.5f);
            _rt.pivot     = new Vector2(0f, 0.5f);
        }

        /// <summary>
        /// 设置文本并让根物体随文字自适应。
        /// maxWidth<=0：单行展示；>0：在该宽度内换行并自适应高度。
        /// </summary>
        public void SetupText(string msg, float maxWidth = -1f)
        {
            if (msg == null) msg = string.Empty;
            msg = msg.TrimEnd(); // 去掉尾部空格/不可见字符，避免看起来偏右
            textLabel.text = msg;

            bool wrap = wordWrap && maxWidth > 0f;
            textLabel.enableWordWrapping = wrap;
            textLabel.overflowMode       = TextOverflowModes.Overflow;

            // 计算首选尺寸
            if (wrap)
            {
                float innerW = Mathf.Max(0f, maxWidth - padding.x);
                Vector2 pref = textLabel.GetPreferredValues(msg, innerW, 0f);

                float w = Mathf.Min(pref.x + padding.x, maxWidth);              // 宽度受限
                float h = Mathf.Max(pref.y + padding.y, minHeight);             // 高度随换行增长
                _rt.sizeDelta = new Vector2(w, h);
            }
            else
            {
                textLabel.ForceMeshUpdate();
                Vector2 pref = new Vector2(textLabel.preferredWidth, textLabel.preferredHeight);

                float w = pref.x + padding.x;                                   // 单行不限制宽度
                float h = Mathf.Max(pref.y + padding.y, minHeight);
                _rt.sizeDelta = new Vector2(w, h);
            }

            // 背景图用九宫格，放大不拉花
            if (background) background.type = Image.Type.Sliced;

            // —— 让文本“真正居中”：文字Rect充满父物体，再用offset做内边距 ——
            var tr = textLabel.rectTransform;
            tr.anchorMin = new Vector2(0f, 0f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot     = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(padding.x * 0.5f, padding.y * 0.5f);
            tr.offsetMax = new Vector2(-padding.x * 0.5f, -padding.y * 0.5f);

            textLabel.alignment = TextAlignmentOptions.Center; // 水平+垂直居中
            textLabel.margin    = Vector4.zero;

            textLabel.ForceMeshUpdate();
        }

        /// <summary>作为 Viewport 的子物体放到起点并启动移动</summary>
        public void Launch(RectTransform viewport, Vector2 anchoredStartPos, float speedPxPerSec, Action<DanmakuItem> onDespawn)
        {
            _parentViewport = viewport;
            _speedPxPerSec  = speedPxPerSec;
            _onDespawn      = onDespawn;

            _rt.SetParent(viewport, worldPositionStays: false);
            _rt.anchoredPosition = anchoredStartPos;

            _moving = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!_moving) return;

            float dx = _speedPxPerSec * Time.unscaledDeltaTime; // 忽略 Time.timeScale
            var pos  = _rt.anchoredPosition;
            pos.x   -= dx;                                      // 右 -> 左
            _rt.anchoredPosition = pos;

            // 完全移出左侧（左边界 + 自身宽度 <= 0）
            if (pos.x + _rt.rect.width <= 0f)
            {
                _moving = false;
                _onDespawn?.Invoke(this);
            }
        }

        // —— 可选样式 API —— 
        public void SetTextColor(Color c)         { textLabel.color = c; }
        public void SetFont(TMP_FontAsset f)      { if (f) textLabel.font = f; }
        public void SetFontSize(float size)       { textLabel.fontSize = size; }
        public void SetBackground(Sprite s, Color c, bool enable = true)
        {
            if (!background) return;
            background.enabled = enable;
            if (!enable) return;
            background.sprite = s;
            background.color  = c;
            background.type   = Image.Type.Sliced;
        }
    }
}
