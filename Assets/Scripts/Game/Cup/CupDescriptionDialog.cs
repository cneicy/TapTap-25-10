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
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

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

        private Vector2 ConvertMousePosition(Vector2 mousePosition)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            var panelSize = rectTransform.rect.size * rectTransform.lossyScale;

            var offsetX = panelSize.x / 2f;
            var offsetY = panelSize.y / 2f;

            var targetX = mousePosition.x + offsetX;
            var targetY = mousePosition.y - offsetY;

            if (targetX + panelSize.x / 2f > screenWidth)
                targetX = mousePosition.x - panelSize.x/2f;

            if (targetX - panelSize.x / 2f < 0)
                targetX = mousePosition.x/2;

            if (targetY + panelSize.y / 2f > screenHeight)
                targetY = mousePosition.y - panelSize.y;

            if (targetY - panelSize.y / 2f < 0)
                targetY = mousePosition.y + panelSize.y / 2f;

            return new Vector2(targetX, targetY);
        }
    }
}
