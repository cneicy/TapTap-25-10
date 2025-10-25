using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace ScreenEffect
{
    [RequireComponent(typeof(RawImage))]
    public class RectTransitionController : Singleton<RectTransitionController>
    {
        [Header("动画参数")]
        [SerializeField] private float gapCloseTime = 0.5f;
        [SerializeField] private float holdTime = 0.3f;
        [SerializeField] private float expandTime = 0.8f;
        [SerializeField] private float maxGapSize = 0.05f;
    
        [Header("网格参数")]
        [SerializeField] private int rows = 8;
        [SerializeField] private int columns = 12;
    
        [Header("颜色")]
        [SerializeField] private Color color1 = Color.black;
        [SerializeField] private Color color2 = Color.white;
    
        private RawImage _rawImage;
        private Material _material;
    
        private static readonly int RowsID = Shader.PropertyToID("_Rows");
        private static readonly int ColumnsID = Shader.PropertyToID("_Columns");
        private static readonly int GapSizeID = Shader.PropertyToID("_GapSize");
        private static readonly int ExpandProgressID = Shader.PropertyToID("_ExpandProgress");
        private static readonly int Color1ID = Shader.PropertyToID("_Color1");
        private static readonly int Color2ID = Shader.PropertyToID("_Color2");

        protected override void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            if (_rawImage.material)
            {
                _material = new Material(_rawImage.material);
                _rawImage.material = _material;
            }
            else
            {
                Debug.LogError("请先为RawImage指定使用RectTransition Shader的材质！");
            }

            _rawImage.enabled = false;
        }

        private void OnDestroy()
        {
            if (_material)
            {
                Destroy(_material);
            }
        }
    
        /// <summary>
        /// 开始转场动画
        /// </summary>
        public void StartTransition()
        {
            StartCoroutine(TransitionCoroutine());
        }
    
        /// <summary>
        /// 开始转场动画（带回调）
        /// </summary>
        public void StartTransition(System.Action onTransitionMiddle, System.Action onTransitionComplete)
        {
            StartCoroutine(TransitionCoroutine(onTransitionMiddle, onTransitionComplete));
        }
    
        private IEnumerator TransitionCoroutine(System.Action onMiddle = null, System.Action onComplete = null)
        {
            if (!_material) yield break;

            _rawImage.enabled = true;

            _material.SetFloat(RowsID, rows);
            _material.SetFloat(ColumnsID, columns);
            _material.SetColor(Color1ID, color1);
            _material.SetColor(Color2ID, color2);
            _material.SetFloat(ExpandProgressID, 0f);

            var elapsed = 0f;
            while (elapsed < gapCloseTime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / gapCloseTime;
                t = EaseInOut(t);
            
                var currentGap = Mathf.Lerp(maxGapSize, 0f, t);
                _material.SetFloat(GapSizeID, currentGap);
            
                yield return null;
            }
        
            _material.SetFloat(GapSizeID, 0f);

            yield return new WaitForSeconds(holdTime);

            onMiddle?.Invoke();

            elapsed = 0f;
            while (elapsed < expandTime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / expandTime;
                t = EaseIn(t, 2f);
            
                _material.SetFloat(ExpandProgressID, t);
            
                yield return null;
            }
        
            _material.SetFloat(ExpandProgressID, 1f);

            _rawImage.enabled = false;
            SoundManager.Instance.Play("levelloaded");
            onComplete?.Invoke();

            _material.SetFloat(GapSizeID, maxGapSize);
            _material.SetFloat(ExpandProgressID, 0f);
        }

        private float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private float EaseIn(float t, float power)
        {
            return Mathf.Pow(t, power);
        }

        private void OnValidate()
        {
            if (!_material) return;
            _material.SetFloat(RowsID, rows);
            _material.SetFloat(ColumnsID, columns);
            _material.SetColor(Color1ID, color1);
            _material.SetColor(Color2ID, color2);
        }
    }
}