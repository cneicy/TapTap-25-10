using Data;
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

        [SerializeField] private AudioClip holdClip;
        [SerializeField] private AudioClip tapClip;
        [SerializeField] private AudioSource audioSource;

        private CRTScanlineFilter _crtFilter;
        private float _targetBrightness = 1f;
        private float _brightnessChangeSpeed = 2f;

        [EventSubscribe]
        public void OnLevelLoadedEvent(LevelLoadedEvent evt)
        {
            if (evt.LevelName is "Level_STG" or "Level_Voice") return;
            _hasSavedPosition = false;
            _holdTime = 0f;
            _actionTriggered = false;
            
            _crtFilter = FindFirstObjectByType<CRTScanlineFilter>();
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
                return;
            }

            if (backAction.WasPressedThisFrame())
            {
                _holdTime = 0f;
                _actionTriggered = false;
                if (tapClip && audioSource)
                {
                    audioSource.PlayOneShot(tapClip);
                }
            }

            if (backAction.IsPressed())
            {
                if (!_actionTriggered)
                {
                    _holdTime += Time.deltaTime;

                    _targetBrightness = Mathf.Lerp(1f, 0.5f, _holdTime / RequiredHoldTime);
                    
                    if (_holdTime >= RequiredHoldTime)
                    {
                        _actionTriggered = true;
                        LevelManager.Instance.SwitchLevel(LevelManager.Instance.CurrentLevel);
                        RectTransitionController.Instance.StartTransition();
                    }
                    else if (_holdTime > 0.1f && holdClip != null && audioSource != null)
                    {
                        if (!audioSource.isPlaying || audioSource.clip != holdClip)
                        {
                            audioSource.clip = holdClip;
                            audioSource.Play();
                        }
                    }
                }
            }

            if (backAction.WasReleasedThisFrame())
            {
                _targetBrightness = 1f;
                
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
                
                if (!_actionTriggered && _hasSavedPosition)
                {
                    FindFirstObjectByType<PlayerController>().transform.position = lastSavedPosition;
                }
                if(!DataManager.Instance.GetData<bool>("IsNotFirstReset"))
                {
                    SoundManager.Instance.Play("meta 1-3.1");
                    DataManager.Instance.SetData("IsNotFirstReset", true, true);
                }
                _holdTime = 0f;
                _actionTriggered = false;
            }
            UpdateCRTBrightness();
        }

        private void UpdateCRTBrightness()
        {
            if (!_crtFilter) return;
            var currentBrightness = _crtFilter.GetBrightness();
            var newBrightness = Mathf.Lerp(currentBrightness, _targetBrightness, Time.deltaTime * _brightnessChangeSpeed);
            _crtFilter.SetBrightness(newBrightness);
        }
    }
}