using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acknowledgements
{
    public class DanmakuItem : MonoBehaviour
    {
        public enum SpeedUnit
        {
            LocalUnitsPerSec,   // 使用父节点（Viewport）本地单位/秒（原逻辑）
            ScreenPixelsPerSec  // 使用屏幕像素/秒（World Space 更直观）
        }

        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI textLabel; // 子：TMP
        [SerializeField] private Image background;          // 子：背景 Image（Sliced）

        private RectTransform _rt;     // 根容器（空）
        private RectTransform _bgRt;
        private RectTransform _textRt;

        private RectTransform _parentViewport;
        private float _speedValue;                // 速度值（含单位）
        private SpeedUnit _speedUnit = SpeedUnit.LocalUnitsPerSec;
        private Camera _screenCam;                // 用于像素速度换算
        private Canvas _canvas;                   // 找 worldCamera

        private Action<DanmakuItem> _onDespawn;
        private bool _moving;

        [Header("尺寸/样式")]
        public Vector2 padding = new Vector2(40f, 12f);
        public float  minHeight = 32f;
        public bool   wordWrap  = true;

        public float Width  => _rt.rect.width;
        public float Height => _rt.rect.height;

        void Awake()
        {
            _rt     = GetComponent<RectTransform>();
            _bgRt   = background ? background.rectTransform : null;
            _textRt = textLabel   ? textLabel.rectTransform : null;

            // 根：左中对齐（从右边生成更方便）
            _rt.anchorMin = new Vector2(0f, 0.5f);
            _rt.anchorMax = new Vector2(0f, 0.5f);
            _rt.pivot     = new Vector2(0f, 0.5f);

            // 子：中心锚点
            if (_bgRt)
            {
                _bgRt.anchorMin = _bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                _bgRt.pivot     = new Vector2(0.5f, 0.5f);
                _bgRt.anchoredPosition = Vector2.zero;
            }
            if (_textRt)
            {
                _textRt.anchorMin = _textRt.anchorMax = new Vector2(0.5f, 0.5f);
                _textRt.pivot     = new Vector2(0.5f, 0.5f);
                _textRt.anchoredPosition = Vector2.zero;
            }

            if (background) background.type = Image.Type.Sliced;
        }

        /// <summary>
        /// 设置文本并让“背景子物体”自适应文字大小，同时把“根容器”尺寸设置为背景尺寸。
        /// maxWidth<=0：单行；>0：限定最大宽度并按需换行。
        /// </summary>
        public void SetupText(string msg, float maxWidth = -1f)
        {
            if (!textLabel || !_textRt || !_bgRt) return;

            if (msg == null) msg = string.Empty;
            msg = msg.TrimEnd();
            textLabel.text = msg;

            var wrap = wordWrap && maxWidth > 0f;
            textLabel.enableWordWrapping = wrap;
            textLabel.overflowMode       = TextOverflowModes.Overflow;
            textLabel.alignment          = TextAlignmentOptions.Center;
            textLabel.margin             = Vector4.zero;

            float contentW, contentH;
            if (wrap)
            {
                var innerW = Mathf.Max(1f, maxWidth - padding.x);
                var pref = textLabel.GetPreferredValues(msg, innerW, 0f);
                contentW = Mathf.Min(pref.x, innerW);
                contentH = Mathf.Max(pref.y, textLabel.fontSize);
            }
            else
            {
                textLabel.ForceMeshUpdate();
                contentW = textLabel.preferredWidth;
                contentH = Mathf.Max(textLabel.preferredHeight, textLabel.fontSize);
            }

            _textRt.sizeDelta = new Vector2(contentW, contentH);

            var bgW = contentW + padding.x;
            var bgH = Mathf.Max(contentH + padding.y, minHeight);
            _bgRt.sizeDelta = new Vector2(bgW, bgH);

            _rt.sizeDelta = _bgRt.sizeDelta;

            _bgRt.anchoredPosition   = Vector2.zero;
            _textRt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 启动移动。
        /// speedValue: 数值；speedUnit: 速度单位；screenCam: 像素模式下用到的摄像机（可空）。
        /// </summary>
        public void Launch(RectTransform viewport, Vector2 anchoredStartPos, float speedValue,
                           Action<DanmakuItem> onDespawn,
                           SpeedUnit speedUnit = SpeedUnit.LocalUnitsPerSec,
                           Camera screenCam = null)
        {
            _parentViewport = viewport;
            _speedValue     = speedValue;
            _speedUnit      = speedUnit;
            _onDespawn      = onDespawn;
            _screenCam      = screenCam;
            _canvas         = viewport ? viewport.GetComponentInParent<Canvas>() : null;

            _rt.SetParent(viewport, worldPositionStays: false);
            _rt.anchoredPosition = anchoredStartPos;

            _moving = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!_moving) return;

            var dt = Time.unscaledDeltaTime;
            var stepLocalX = (_speedUnit == SpeedUnit.LocalUnitsPerSec)
                ? _speedValue * dt
                : PixelsToLocalUnitsX(_speedValue) * dt; // 像素→本地单位

            var pos = _rt.anchoredPosition;
            pos.x  -= stepLocalX; // 右→左
            _rt.anchoredPosition = pos;

            if (pos.x + _rt.rect.width <= 0f)
            {
                _moving = false;
                _onDespawn?.Invoke(this);
            }
        }

        /// <summary>把“像素/秒”的速度换算为“父Rect本地单位/秒”的X方向步长。</summary>
        private float PixelsToLocalUnitsX(float pixelsPerSec)
        {
            if (_parentViewport == null) return pixelsPerSec; // 退化
            var cam = _screenCam;
            if (!cam)
            {
                if (_canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = _canvas.worldCamera;
                if (!cam) cam = Camera.main;
            }

            // 采样：viewport 本地两点 (0,0) 和 (1,0) 的屏幕距离
            var p0World = _parentViewport.TransformPoint(Vector3.zero);
            var p1World = _parentViewport.TransformPoint(Vector3.right);

            Vector3 p0Screen = RectTransformUtility.WorldToScreenPoint(cam, p0World);
            Vector3 p1Screen = RectTransformUtility.WorldToScreenPoint(cam, p1World);

            var pxPerLocal = Mathf.Abs(p1Screen.x - p0Screen.x);
            if (pxPerLocal < 0.0001f) return pixelsPerSec; // 防除0

            return pixelsPerSec / pxPerLocal; // 像素/秒 ÷ 每本地单位对应像素 = 本地单位/秒
        }

        // —— 可选样式 API —— 
        public void SetTextColor(Color c)   { if (textLabel) textLabel.color = c; }
        public void SetFont(TMP_FontAsset f){ if (textLabel && f) textLabel.font = f; }
        public void SetFontSize(float size) { if (textLabel) textLabel.fontSize = size; }
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
