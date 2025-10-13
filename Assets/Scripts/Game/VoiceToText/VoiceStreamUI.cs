using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.VoiceToText
{
    /// <summary>
    /// 流式语音识别UI控制器
    /// </summary>
    public class VoiceStreamUI : MonoBehaviour
    {
        [Header("引用")]
        public VoiceStreamManager streamManager;

        [Header("UI组件 可选")] [CanBeNull] public Button stopButton;
        [CanBeNull] public TMP_Text realtimeText;
        [CanBeNull] public TMP_Text accumulatedText;
        [CanBeNull] public TMP_Dropdown microphoneDropdown;

        [Header("显示设置")]
        public int maxRealtimeLength = 100;

        private void Start()
        {
            if (!streamManager)
            {
                Debug.LogError("未设置 VoiceStreamManager 引用！");
                return;
            }

            InitializeUI();
            RegisterEvents();
        }

        private void InitializeUI()
        {
            // 初始化麦克风下拉列表
            if (microphoneDropdown)
            {
                InitializeMicrophoneDropdown();
            }
            
            if (stopButton)
                stopButton.onClick.AddListener(OnStopButtonClicked);
            
            UpdateButtonStates(false);
            if (realtimeText) realtimeText.text = "";
            if (accumulatedText) accumulatedText.text = "等待语音输入...";
        }

        private void InitializeMicrophoneDropdown()
        {
            if (!microphoneDropdown) return;
            microphoneDropdown.ClearOptions();
            var mics = streamManager.GetAllMicrophones();

            if (mics.Length == 0)
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("❌ 未找到麦克风"));
                microphoneDropdown.interactable = false;
            }
            else
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("默认麦克风"));
                foreach (var mic in mics)
                {
                    microphoneDropdown.options.Add(new TMP_Dropdown.OptionData(mic));
                }

                microphoneDropdown.value = 0;
                microphoneDropdown.RefreshShownValue();
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        private void RegisterEvents()
        {
            streamManager.OnStreamStarted += OnStreamStarted;
            streamManager.OnStreamStopped += OnStreamStopped;
            streamManager.OnSegmentReceived += OnSegmentReceived;
            streamManager.OnFinalResult += OnFinalResult;
            streamManager.OnError += OnError;
        }

        private void OnStopButtonClicked()
        {
            streamManager.StopStream();
        }

        private void OnMicrophoneChanged(int index)
        {
            streamManager.SetMicrophone(index - 1);
        }

        private void OnStreamStarted()
        {
            UpdateButtonStates(true);
            if (accumulatedText)
                accumulatedText.text = "";
        }

        private void OnStreamStopped()
        {
            UpdateButtonStates(false);
            if (realtimeText != null)
                realtimeText.text = "";
        }

        private void OnSegmentReceived(string text)
        {
            if (!accumulatedText) return;
            accumulatedText.text = streamManager.GetAccumulatedText();
                
            Canvas.ForceUpdateCanvases();
        }

        private void OnFinalResult(string text)
        {
            if (accumulatedText)
            {
                accumulatedText.text = text;
            }
        }

        private void OnError(string error)
        {
            UpdateButtonStates(false);
        }

        private void UpdateButtonStates(bool isStreaming)
        {
            if (stopButton)
                stopButton.interactable = isStreaming;

            if (microphoneDropdown)
                microphoneDropdown.interactable = !isStreaming;
        }

        private void Update()
        {
            if (!streamManager.IsStreaming || !realtimeText) return;
            var currentText = streamManager.GetAccumulatedText();
            
            if (currentText.Length > maxRealtimeLength)
            {
                currentText = "..." + currentText[^maxRealtimeLength..];
            }
                
            realtimeText.text = currentText;
        }

        private void OnDestroy()
        {
            if (streamManager)
            {
                streamManager.OnStreamStarted -= OnStreamStarted;
                streamManager.OnStreamStopped -= OnStreamStopped;
                streamManager.OnSegmentReceived -= OnSegmentReceived;
                streamManager.OnFinalResult -= OnFinalResult;
                streamManager.OnError -= OnError;
            }

            if (stopButton)
                stopButton.onClick.RemoveListener(OnStopButtonClicked);

            if (microphoneDropdown)
                microphoneDropdown.onValueChanged.RemoveListener(OnMicrophoneChanged);
        }
    }
}