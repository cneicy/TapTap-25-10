using UnityEngine;

namespace Game.Item
{
    [DisallowMultipleComponent]
    public class BulletTrail2D : MonoBehaviour
    {
        [Header("Trail 外观")]
        [Tooltip("拖尾总时长（越大越长）")]
        public float trailTime = 0.18f;

        [Tooltip("拖尾起始宽度")]
        public float startWidth = 0.08f;

        [Tooltip("拖尾末端宽度（通常为 0）")]
        public float endWidth = 0f;

        [Tooltip("相邻顶点最小距离，越大越省性能")]
        public float minVertexDistance = 0.03f;

        [Header("材质（可选）")]
        [Tooltip("URP 推荐使用 Particles/Unlit + Additive，颜色设为 HDR 白色以配合 Bloom")]
        public Material trailMaterial;

        [Header("命中/销毁时处理")]
        [Tooltip("命中或禁用时从子弹上分离拖尾并保留一段时间")]
        public bool detachAndLingerOnDisable = true;

        [Tooltip("分离后最少保留淡出时间（秒）")]
        public float lingerTime = 0.15f;

        private GameObject _trailGO;
        private TrailRenderer _tr;

        void Awake()
        {
            _trailGO = new GameObject("Trail");
            _trailGO.transform.SetParent(transform, false);
            _trailGO.transform.localPosition = Vector3.zero;

            _tr = _trailGO.AddComponent<TrailRenderer>();
            _tr.time = trailTime;
            _tr.minVertexDistance = minVertexDistance;
            _tr.textureMode = LineTextureMode.Stretch;
            _tr.alignment = LineAlignment.View;
            _tr.numCapVertices = 4;
            _tr.numCornerVertices = 2;
            _tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _tr.receiveShadows = false;
            _tr.autodestruct = true;

            if (trailMaterial) _tr.material = trailMaterial;
            
            var width = new AnimationCurve();
            width.AddKey(0f, startWidth);
            width.AddKey(1f, endWidth);
            _tr.widthCurve = width;
            
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            _tr.colorGradient = g;

            MatchSortingToSprite();
        }

        private void MatchSortingToSprite()
        {
            // 让拖尾与精灵在同一图层/顺序，避免被遮挡
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr)
            {
                _tr.sortingLayerID = sr.sortingLayerID;
                _tr.sortingOrder   = sr.sortingOrder - 1; // 习惯让拖尾在精灵下一层
            }
        }

        /// <summary>
        /// 在命中/禁用/销毁前调用：让拖尾自然淡出。
        /// </summary>
        public void DetachAndLinger()
        {
            if (!_tr || !_trailGO) return;

            // 停止继续生成新段，但保留已有段
            _tr.emitting = false;

            // 确保有充足时间淡出
            if (lingerTime > 0f)
                _tr.time = Mathf.Max(_tr.time, lingerTime);

            // 从子弹上分离出来，独立存在到淡出结束
            _trailGO.transform.SetParent(null, true);
        }

        private void OnDisable()
        {
            if (detachAndLingerOnDisable && _tr && _trailGO && _trailGO.transform.parent != null)
                DetachAndLinger();
        }
    }
}
