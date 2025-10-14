using ShrinkEventBus;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Cup
{
    [EventBusSubscriber]
    public class CupDescriptionDialog : MonoBehaviour
    {
        [SerializeField] private InputActionReference mousePosAction;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        private Camera _uiCamera;
        private RectTransform _canvasRectTransform;
        private float _offsetX;
        private float _offsetY;
        private Vector2 _canvasSize;
        private float _halfCanvasWidth;
        private float _halfCanvasHeight;
        private Vector2 _panelSize;

        private void Awake()
        {
            if (!canvas)
                canvas = GetComponentInParent<Canvas>();
            
            _uiCamera = canvas.worldCamera;
            _canvasRectTransform = canvas.GetComponent<RectTransform>();
            _panelSize = rectTransform.rect.size;

            _offsetX = _panelSize.x / 2f;
            _offsetY = -_panelSize.y / 2f;
            
            _canvasSize = _canvasRectTransform.rect.size;
            _halfCanvasWidth = _canvasSize.x / 2f;
            _halfCanvasHeight = _canvasSize.y / 2f;
        }

        [EventSubscribe]
        public void OnMouseEnterCupEvent(MouseEnterCupEvent evt)
        {
            rectTransform.gameObject.SetActive(true);
            nameText.text = evt.Name;
            descriptionText.text = evt.Description;
        }

        [EventSubscribe]
        public void OnMouseExitCupEvent(MouseExitCupEvent evt)
        {
            rectTransform.gameObject.SetActive(false);
            nameText.text = "";
            descriptionText.text = "";
        }

        private void LateUpdate()
        {
            var mousePosition = mousePosAction.action.ReadValue<Vector2>();
            var adjustedPosition = ConvertMousePosition(mousePosition);
            rectTransform.position = adjustedPosition;
        }

        private Vector3 ConvertMousePosition(Vector2 mousePosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                mousePosition,
                _uiCamera,
                out var localPoint
            );
            
            var targetX = localPoint.x + _offsetX;
            var targetY = localPoint.y + _offsetY;

            if (targetX + _panelSize.x / 2f > _halfCanvasWidth)
                targetX = localPoint.x - _panelSize.x / 2f;

            if (targetX - _panelSize.x / 2f < -_halfCanvasWidth)
                targetX = -_halfCanvasWidth + _panelSize.x / 2f;

            if (targetY + _panelSize.y / 2f > _halfCanvasHeight)
                targetY = localPoint.y - _panelSize.y;

            if (targetY - _panelSize.y / 2f < -_halfCanvasHeight)
                targetY = -_halfCanvasHeight + _panelSize.y / 2f;

            return _canvasRectTransform.TransformPoint(new Vector3(targetX, targetY, 0));
        }
    }
}