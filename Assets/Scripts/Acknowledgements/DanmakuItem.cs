using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI textLabel;
    [SerializeField] private Image background; // 根物体的 Image（可选，但建议有）

    private RectTransform _rt;
    private RectTransform _parentViewport;
    private float _speedPxPerSec;
    private System.Action<DanmakuItem> _onDespawn;
    private bool _moving;

    [Header("尺寸/样式")]
    public Vector2 padding = new Vector2(40f, 12f); // 水平/垂直内边距（像素）
    public float minHeight = 32f;                   // 最小高度
    public bool  wordWrap = true;                   // 是否允许换行

    public float Width  => _rt.rect.width;
    public float Height => _rt.rect.height;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        // 根物体：左中对齐，便于从右边生成
        _rt.anchorMin = new Vector2(0f, 0.5f);
        _rt.anchorMax = new Vector2(0f, 0.5f);
        _rt.pivot     = new Vector2(0f, 0.5f);
    }

    /// <summary>
    /// 设置文本并让根物体随文字自适应。
    /// maxWidth<=0 表示不限制宽度；>0 则在该宽度内换行并自适应高度。
    /// </summary>
    public void SetupText(string msg, float maxWidth = -1f)
    {
        textLabel.enableWordWrapping = wordWrap && maxWidth > 0f;
        textLabel.text = msg;

        // 计算在 maxWidth 约束下的首选尺寸（TMP 建议使用 GetPreferredValues）
        Vector2 pref;
        if (maxWidth > 0f)
        {
            float innerW = Mathf.Max(0f, maxWidth - padding.x);
            pref = textLabel.GetPreferredValues(msg, innerW, 0f);
            // 宽度受限但高度根据换行增长
            float w = Mathf.Min(pref.x + padding.x, maxWidth);
            float h = Mathf.Max(pref.y + padding.y, minHeight);
            _rt.sizeDelta = new Vector2(w, h);
        }
        else
        {
            // 不限宽度：一行展示
            textLabel.ForceMeshUpdate();
            pref = new Vector2(textLabel.preferredWidth, textLabel.preferredHeight);
            float w = pref.x + padding.x;
            float h = Mathf.Max(pref.y + padding.y, minHeight);
            _rt.sizeDelta = new Vector2(w, h);
        }

        // 根物体有 Image 时，设置为 Sliced 可避免拉伸变形（需有 9-slice 边框）
        if (background) background.type = Image.Type.Sliced;
    }

    public void Launch(RectTransform viewport, Vector2 anchoredStartPos, float speedPxPerSec, System.Action<DanmakuItem> onDespawn)
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

        float dx = _speedPxPerSec * Time.unscaledDeltaTime;
        var pos = _rt.anchoredPosition;
        pos.x -= dx;                 // 右 -> 左
        _rt.anchoredPosition = pos;

        if (pos.x + _rt.rect.width <= 0f)
        {
            _moving = false;
            _onDespawn?.Invoke(this);
        }
    }

    // —— 可选样式 API —— 
    public void SetTextColor(Color c) => textLabel.color = c;
    public void SetFont(TMP_FontAsset f) { if (f) textLabel.font = f; }
    public void SetFontSize(float size) => textLabel.fontSize = size;
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
