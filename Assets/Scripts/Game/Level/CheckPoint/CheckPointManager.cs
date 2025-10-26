using Game.Player;
using ScreenEffect;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Game.Level.CheckPoint
{
    [EventBusSubscriber]
    public class CheckPointManager : Singleton<CheckPointManager>
    {
        public Vector3 lastSavedPosition;
        private bool _hasSavedPosition;

        private float _holdTime;
        private const float RequiredHoldTime = 3f;
        private bool _actionTriggered;

        [EventSubscribe]
        public void OnLevelLoadedEvent(LevelLoadedEvent evt)
        {
            if (evt.LevelName is "Level_STG" or "Level_Voice") return;
            _hasSavedPosition = false;
            _holdTime = 0f;
            _actionTriggered = false;
        }

        [EventSubscribe]
        public void OnTouchCheckPointEvent(TouchCheckPointEvent evt)
        {
            lastSavedPosition = FindFirstObjectByType<PlayerController>().transform.position;
            _hasSavedPosition = true;
        }

        public void Update()
        {
            var backAction = InputSystem.actions.FindAction("BackPreviousCheckPoint");
            if (backAction == null)
            {
                print("nu;;");
                return;
            }

            if (backAction.WasPressedThisFrame())
            {
                _holdTime = 0f;
                _actionTriggered = false;
            }

            if (backAction.IsPressed())
            {
                if (!_actionTriggered)
                {
                    _holdTime += Time.deltaTime;

                    if (_holdTime >= RequiredHoldTime)
                    {
                        _actionTriggered = true;
                        LevelManager.Instance.SwitchLevel(LevelManager.Instance.CurrentLevel);
                        RectTransitionController.Instance.StartTransition();
                    }
                }
            }

            if (backAction.WasReleasedThisFrame())
            {
                if (!_actionTriggered && _hasSavedPosition)
                {
                    FindFirstObjectByType<PlayerController>().transform.position = lastSavedPosition;
                }

                _holdTime = 0f;
                _actionTriggered = false;
            }
        }
    }
}