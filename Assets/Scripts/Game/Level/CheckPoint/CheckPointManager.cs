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
        public CheckPointBase currentCheckPoint;

        private float _holdTime;
        private const float RequiredHoldTime = 3f;
        private bool _actionTriggered;

        [EventSubscribe]
        public void OnLevelLoadedEvent(LevelLoadedEvent evt)
        {
            currentCheckPoint = null;
            _holdTime = 0f;
            _actionTriggered = false;
        }

        [EventSubscribe]
        public void OnTouchCheckPointEvent(TouchCheckPointEvent evt)
        {
            currentCheckPoint = evt.CheckPointBase;
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
                if (!_actionTriggered)
                {
                    if (currentCheckPoint)
                        FindFirstObjectByType<PlayerController>().transform.position =
                            currentCheckPoint.transform.position;
                }

                _holdTime = 0f;
                _actionTriggered = false;
            }
        }
    }
}